using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NestyStay.Application.Abstractions;

namespace NestyStay.Infrastructure.Storage;

/// <summary>
/// Private server-local object storage.
///
/// Objects are written below an operator-configured directory that must not be
/// inside the web root. Uploads still enter through the existing authorized API
/// endpoints, while download DTOs receive short-lived HMAC-signed API URLs.
/// No object is exposed through static-file middleware or a public directory.
/// </summary>
public sealed class LocalFileStorageProvider(IConfiguration configuration, IHostEnvironment? hostEnvironment = null) : IStorageProvider
{
    private const int BufferSize = 81920;
    private const int HeaderByteLimit = 512;
    private const string DevelopmentSigningSecret = "development-only-local-storage-signing-secret";

    public string ProviderName => "Private server-local file storage";

    public Task<string> CreateUploadUrlAsync(string objectKey, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Direct object uploads are disabled. Use the authorized application upload endpoint.");

    public async Task<StorageProviderReadiness> CheckReadinessAsync(CancellationToken cancellationToken)
    {
        var configuredRoot = ResolveSetting("Integrations:LocalStorageRoot", "NESTYSTAY_STORAGE_LOCAL_ROOT");
        var configuredSecret = ResolveSetting("Integrations:LocalStorageSigningSecret", "NESTYSTAY_STORAGE_SIGNING_SECRET");
        var developmentFallbackAllowed = hostEnvironment?.EnvironmentName is "Development" or "Testing";

        if (!developmentFallbackAllowed && string.IsNullOrWhiteSpace(configuredRoot))
        {
            return new StorageProviderReadiness(false, "BLOCKED_CONFIG", "Private storage root is not configured.");
        }

        if (!developmentFallbackAllowed && string.IsNullOrWhiteSpace(configuredSecret))
        {
            return new StorageProviderReadiness(false, "BLOCKED_CONFIG", "Private storage signing secret is not configured.");
        }

        if (!developmentFallbackAllowed && Encoding.UTF8.GetByteCount(configuredSecret!) < 32)
        {
            return new StorageProviderReadiness(false, "BLOCKED_CONFIG", "Private storage signing secret is too short.");
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var root = ResolveLocalStorageRoot();
            if (hostEnvironment is not null && IsSameOrChildPath(root, hostEnvironment.ContentRootPath))
            {
                return new StorageProviderReadiness(false, "BLOCKED_CONFIG", "Private storage root is inside the application directory.");
            }

            Directory.CreateDirectory(root);
            RestrictPermissions(root, isDirectory: true);
            var probePath = Path.Combine(root, $".nesty-storage-readiness-{Guid.NewGuid():N}.tmp");
            try
            {
                await File.WriteAllBytesAsync(probePath, [0x4E, 0x53], cancellationToken);
            }
            finally
            {
                if (File.Exists(probePath)) File.Delete(probePath);
            }

            return new StorageProviderReadiness(true, "CONFIGURED", "Private storage root is writable and signing is configured.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return new StorageProviderReadiness(false, "UNAVAILABLE", "Private storage root is not writable.");
        }
    }

    public async Task<StorageObjectWriteResult> SaveObjectAsync(StorageObjectWriteRequest request, Stream content, CancellationToken cancellationToken)
    {
        if (request.MaximumBytes <= 0)
        {
            throw new InvalidOperationException("Upload size limit is invalid.");
        }

        var root = ResolveLocalStorageRoot();
        Directory.CreateDirectory(root);
        RestrictPermissions(root, isDirectory: true);

        var targetPath = ResolveObjectPath(root, request.ObjectKey);
        var parentDirectory = Path.GetDirectoryName(targetPath)!;
        Directory.CreateDirectory(parentDirectory);
        RestrictPermissions(parentDirectory, isDirectory: true);

        var temporaryPath = $"{targetPath}.{Guid.NewGuid():N}.tmp";
        var success = false;
        long totalBytes = 0;
        var headerBytes = new List<byte>(HeaderByteLimit);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[BufferSize];

        try
        {
            await using (var output = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                BufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                while (true)
                {
                    var bytesRead = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    totalBytes += bytesRead;
                    if (totalBytes > request.MaximumBytes)
                    {
                        throw new InvalidOperationException("Attachment exceeds the allowed size.");
                    }

                    var headerRemaining = HeaderByteLimit - headerBytes.Count;
                    if (headerRemaining > 0)
                    {
                        headerBytes.AddRange(buffer.AsSpan(0, Math.Min(bytesRead, headerRemaining)).ToArray());
                    }

                    hash.AppendData(buffer.AsSpan(0, bytesRead));
                    await output.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                }

                if (totalBytes == 0)
                {
                    throw new InvalidOperationException("Attachment upload cannot be empty.");
                }

                await output.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, targetPath, overwrite: true);
            RestrictPermissions(targetPath, isDirectory: false);
            success = true;
        }
        finally
        {
            if (!success && File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }

        return new StorageObjectWriteResult(
            ProviderName,
            request.ObjectKey,
            request.ContentType.Trim().ToLowerInvariant(),
            totalBytes,
            Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant(),
            headerBytes.ToArray());
    }

    public Task<string> CreateDownloadUrlAsync(string objectKey, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        var key = CanonicalObjectKey(objectKey);
        var expires = expiresAt.ToUnixTimeSeconds();
        var token = CreateAccessToken("GET", key, expires);
        var encodedKey = Base64UrlEncode(Encoding.UTF8.GetBytes(key));
        var url = $"/api/storage/objects?key={Uri.EscapeDataString(encodedKey)}&expires={expires}&token={Uri.EscapeDataString(token)}";
        return Task.FromResult(url);
    }

    public bool ValidateAccessToken(string method, string objectKey, long expiresUnixSeconds, string token)
    {
        if (string.IsNullOrWhiteSpace(method) || string.IsNullOrWhiteSpace(token) || expiresUnixSeconds < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            return false;
        }

        try
        {
            var key = CanonicalObjectKey(objectKey);
            var expected = Base64UrlDecode(CreateAccessToken(method.ToUpperInvariant(), key, expiresUnixSeconds));
            var actual = Base64UrlDecode(token);
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken)
    {
        var root = ResolveLocalStorageRoot();
        var localPath = ResolveObjectPath(root, objectKey);
        if (!File.Exists(localPath))
        {
            throw new FileNotFoundException("The requested storage object was not found.", objectKey);
        }

        Stream stream = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);
        return Task.FromResult(stream);
    }

    private string CreateAccessToken(string method, string objectKey, long expiresUnixSeconds)
    {
        var payload = $"{method}\n{objectKey}\n{expiresUnixSeconds}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ResolveSigningSecret()));
        return Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }

    private string ResolveSigningSecret() =>
        ResolveSetting("Integrations:LocalStorageSigningSecret", "NESTYSTAY_STORAGE_SIGNING_SECRET") ??
        ResolveSetting("Security:SessionTokenSecret", "NESTYSTAY_SESSION_TOKEN_SECRET") ??
        DevelopmentSigningSecret;

    private string ResolveLocalStorageRoot()
    {
        var configured = ResolveSetting("Integrations:LocalStorageRoot", "NESTYSTAY_STORAGE_LOCAL_ROOT");
        return Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Path.GetTempPath(), "nestystay-platform-storage")
            : configured);
    }

    private string? ResolveSetting(string configurationKey, string environmentKey)
    {
        var configured = configuration[configurationKey];
        return string.IsNullOrWhiteSpace(configured)
            ? Environment.GetEnvironmentVariable(environmentKey)
            : configured;
    }

    private string ResolveObjectPath(string root, string objectKey)
    {
        var key = CanonicalObjectKey(objectKey);
        var path = root;
        foreach (var segment in key.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            path = Path.Combine(path, segment);
        }

        var rootWithSeparator = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(path);
        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Storage object key is outside the configured storage root.");
        }

        return fullPath;
    }

    private static string CanonicalObjectKey(string objectKey)
    {
        var key = objectKey.Replace('\\', '/').Trim('/');
        var segments = key.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            throw new InvalidOperationException("Storage object key is required.");
        }

        var invalidCharacters = Path.GetInvalidFileNameChars();
        foreach (var segment in segments)
        {
            if (segment is "." or ".." || segment.IndexOfAny(invalidCharacters) >= 0 || segment.Any(char.IsControl))
            {
                throw new InvalidOperationException("Storage object key is invalid.");
            }
        }

        return string.Join('/', segments);
    }

    private static void RestrictPermissions(string path, bool isDirectory)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            File.SetUnixFileMode(path, isDirectory
                ? UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                : UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch (PlatformNotSupportedException)
        {
            // The deployment checklist enforces ownership/permissions on hosts
            // where Unix mode APIs are unavailable to the process.
        }
    }

    private static bool IsSameOrChildPath(string candidate, string parent)
    {
        var normalizedCandidate = Path.GetFullPath(candidate).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedParent = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return normalizedCandidate.Equals(normalizedParent, StringComparison.OrdinalIgnoreCase) ||
               normalizedCandidate.StartsWith(normalizedParent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - padded.Length % 4) % 4);
        return Convert.FromBase64String(padded);
    }
}
