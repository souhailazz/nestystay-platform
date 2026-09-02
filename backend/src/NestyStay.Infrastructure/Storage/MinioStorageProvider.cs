using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;
using NestyStay.Application.Abstractions;

namespace NestyStay.Infrastructure.Storage;

/// <summary>
/// Small dependency-free S3 Signature V4 client for the private MinIO bucket.
/// Keeping this adapter on the existing IStorageProvider boundary means all
/// authorization, file safety and metadata handling remains in the stores.
/// </summary>
public sealed class MinioStorageProvider(IConfiguration configuration, IHttpClientFactory httpClientFactory) : IStorageProvider
{
    private const int HeaderByteLimit = 512;
    private const int BufferSize = 81920;
    private readonly HttpClient client = httpClientFactory.CreateClient("object-storage");

    public string ProviderName => "MinIO S3-compatible object storage";

    public Task<string> CreateUploadUrlAsync(string objectKey, CancellationToken cancellationToken) =>
        Task.FromResult(CreatePresignedUrl(HttpMethod.Put, objectKey, DateTimeOffset.UtcNow.AddMinutes(15)));

    public async Task<StorageObjectWriteResult> SaveObjectAsync(StorageObjectWriteRequest request, Stream content, CancellationToken cancellationToken)
    {
        if (request.MaximumBytes <= 0) throw new InvalidOperationException("Upload size limit is invalid.");
        // Validate the key before any network call so traversal/namespace
        // mistakes cannot probe the storage service.
        _ = CanonicalObjectPath(request.ObjectKey);

        await using var bufferStream = new MemoryStream();
        var headerBytes = new List<byte>(HeaderByteLimit);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[BufferSize];
        long totalBytes = 0;
        while (true)
        {
            var bytesRead = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (bytesRead == 0) break;
            totalBytes += bytesRead;
            if (totalBytes > request.MaximumBytes) throw new InvalidOperationException("Attachment exceeds the allowed size.");
            var remaining = HeaderByteLimit - headerBytes.Count;
            if (remaining > 0) headerBytes.AddRange(buffer.AsSpan(0, Math.Min(bytesRead, remaining)).ToArray());
            hash.AppendData(buffer.AsSpan(0, bytesRead));
            await bufferStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        }
        if (totalBytes == 0) throw new InvalidOperationException("Attachment upload cannot be empty.");

        var payload = bufferStream.ToArray();
        var payloadHash = Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
        await EnsureBucketAsync(cancellationToken);
        using var requestMessage = CreateSignedRequest(HttpMethod.Put, request.ObjectKey, payloadHash, DateTimeOffset.UtcNow);
        requestMessage.Content = new ByteArrayContent(payload);
        requestMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(request.ContentType.Trim().ToLowerInvariant());
        requestMessage.Content.Headers.ContentLength = payload.LongLength;
        using var response = await client.SendAsync(requestMessage, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"MinIO object upload failed ({(int)response.StatusCode}): {Trim(detail)}");
        }

        return new StorageObjectWriteResult(ProviderName, request.ObjectKey, request.ContentType.Trim().ToLowerInvariant(), totalBytes, payloadHash, headerBytes.ToArray());
    }

    public Task<string> CreateDownloadUrlAsync(string objectKey, DateTimeOffset expiresAt, CancellationToken cancellationToken) =>
        Task.FromResult(CreatePresignedUrl(HttpMethod.Get, objectKey, expiresAt));

    private async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        using var request = CreateSignedRequest(HttpMethod.Put, string.Empty, EmptySha256, DateTimeOffset.UtcNow);
        using var response = await client.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Conflict) return;
        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException($"MinIO bucket is unavailable ({(int)response.StatusCode}): {Trim(detail)}");
    }

    private HttpRequestMessage CreateSignedRequest(HttpMethod method, string objectKey, string payloadHash, DateTimeOffset now)
    {
        var endpoint = Endpoint;
        var canonicalUri = CanonicalObjectPath(objectKey);
        var request = new HttpRequestMessage(method, new Uri(endpoint, canonicalUri));
        var host = endpoint.Host + (endpoint.IsDefaultPort ? string.Empty : $":{endpoint.Port}");
        var amzDate = now.UtcDateTime.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
        var date = now.UtcDateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        request.Headers.Host = host;
        request.Headers.TryAddWithoutValidation("x-amz-content-sha256", payloadHash);
        request.Headers.TryAddWithoutValidation("x-amz-date", amzDate);
        var canonicalHeaders = $"host:{host}\nx-amz-content-sha256:{payloadHash}\nx-amz-date:{amzDate}\n";
        var signedHeaders = "host;x-amz-content-sha256;x-amz-date";
        var canonicalRequest = string.Join("\n", method.Method, canonicalUri, string.Empty, canonicalHeaders, signedHeaders, payloadHash);
        var scope = $"{date}/{Region}/s3/aws4_request";
        var stringToSign = $"AWS4-HMAC-SHA256\n{amzDate}\n{scope}\n{Sha256(canonicalRequest)}";
        var signature = HmacHex(SigningKey(date), stringToSign);
        request.Headers.TryAddWithoutValidation("Authorization", $"AWS4-HMAC-SHA256 Credential={AccessKey}/{scope}, SignedHeaders={signedHeaders}, Signature={signature}");
        return request;
    }

    private string CreatePresignedUrl(HttpMethod method, string objectKey, DateTimeOffset expiresAt)
    {
        var endpoint = Endpoint;
        var canonicalUri = CanonicalObjectPath(objectKey);
        var now = DateTimeOffset.UtcNow;
        var amzDate = now.UtcDateTime.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
        var date = now.UtcDateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var scope = $"{date}/{Region}/s3/aws4_request";
        var expires = Math.Clamp((int)Math.Ceiling((expiresAt - now).TotalSeconds), 1, 604800);
        var host = endpoint.Host + (endpoint.IsDefaultPort ? string.Empty : $":{endpoint.Port}");
        var query = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["X-Amz-Algorithm"] = "AWS4-HMAC-SHA256",
            ["X-Amz-Credential"] = $"{AccessKey}/{scope}",
            ["X-Amz-Date"] = amzDate,
            ["X-Amz-Expires"] = expires.ToString(CultureInfo.InvariantCulture),
            ["X-Amz-SignedHeaders"] = "host"
        };
        var canonicalQuery = string.Join("&", query.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
        var canonicalRequest = string.Join("\n", method.Method, canonicalUri, canonicalQuery, $"host:{host}\n", "host", UnsignedPayload);
        var stringToSign = $"AWS4-HMAC-SHA256\n{amzDate}\n{scope}\n{Sha256(canonicalRequest)}";
        query["X-Amz-Signature"] = HmacHex(SigningKey(date), stringToSign);
        var finalQuery = string.Join("&", query.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
        return new Uri(endpoint, canonicalUri + "?" + finalQuery).ToString();
    }

    private Uri Endpoint
    {
        get
        {
            var endpoint = new Uri(Resolve("Integrations:MinioEndpoint", "MINIO_ENDPOINT") ?? "http://minio:9000");
            if (!bool.TryParse(Resolve("Integrations:MinioUseSsl", "MINIO_USE_SSL"), out var useSsl) || !useSsl || endpoint.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                return endpoint;

            var builder = new UriBuilder(endpoint) { Scheme = Uri.UriSchemeHttps };
            if (endpoint.Port == 80 || endpoint.Port == -1) builder.Port = 443;
            return builder.Uri;
        }
    }
    private string Bucket => Resolve("Integrations:MinioBucket", "MINIO_BUCKET") ?? "nesty-objects";
    private string AccessKey => Resolve("Integrations:MinioAccessKey", "MINIO_ACCESS_KEY") ?? Resolve("Integrations:MinioRootUser", "MINIO_ROOT_USER") ?? string.Empty;
    private string SecretKey => Resolve("Integrations:MinioSecretKey", "MINIO_SECRET_KEY") ?? Resolve("Integrations:MinioRootPassword", "MINIO_ROOT_PASSWORD") ?? string.Empty;
    private string Region => Resolve("Integrations:MinioRegion", "MINIO_REGION") ?? "us-east-1";

    private string? Resolve(string key, string environmentKey) =>
        string.IsNullOrWhiteSpace(configuration[key]) ? Environment.GetEnvironmentVariable(environmentKey) : configuration[key];

    private string CanonicalObjectPath(string objectKey)
    {
        var key = objectKey.Replace('\\', '/').Trim('/');
        if (key.Contains("..", StringComparison.Ordinal))
            throw new InvalidOperationException("Storage object key is invalid.");
        return string.IsNullOrWhiteSpace(key)
            ? "/" + Uri.EscapeDataString(Bucket)
            : "/" + Uri.EscapeDataString(Bucket) + "/" + string.Join('/', key.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString));
    }

    private byte[] SigningKey(string date) =>
        Hmac(Hmac(Hmac(Hmac(Encoding.UTF8.GetBytes("AWS4" + SecretKey), date), Region), "s3"), "aws4_request");

    private static byte[] Hmac(byte[] key, string value) => new HMACSHA256(key).ComputeHash(Encoding.UTF8.GetBytes(value));
    private static string HmacHex(byte[] key, string value) => Convert.ToHexString(Hmac(key, value)).ToLowerInvariant();
    private static string Sha256(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static string Trim(string value) => value.Length > 500 ? value[..500] : value;
    private static readonly string EmptySha256 = Sha256(string.Empty);
    private const string UnsignedPayload = "UNSIGNED-PAYLOAD";
}
