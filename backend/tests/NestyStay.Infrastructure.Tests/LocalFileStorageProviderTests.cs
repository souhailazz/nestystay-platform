using Microsoft.Extensions.Configuration;
using NestyStay.Application.Abstractions;
using NestyStay.Infrastructure.Storage;

namespace NestyStay.Infrastructure.Tests;

public sealed class LocalFileStorageProviderTests
{
    [Fact]
    public async Task StoresAndReadsObjectsOnlyInsideConfiguredPrivateRoot()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var provider = Create(root);
            var objectKey = $"tests/{Guid.NewGuid():N}/photo.png";
            var payload = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

            var result = await provider.SaveObjectAsync(
                new StorageObjectWriteRequest(objectKey, "image/png", 1024),
                new MemoryStream(payload),
                CancellationToken.None);

            Assert.Equal("Private server-local file storage", result.ProviderName);
            Assert.Equal(payload.Length, result.SizeBytes);
            Assert.Equal(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(payload)).ToLowerInvariant(), result.Sha256Hash);

            await using var stored = await provider.OpenReadAsync(objectKey, CancellationToken.None);
            using var copy = new MemoryStream();
            await stored.CopyToAsync(copy);
            Assert.Equal(payload, copy.ToArray());

            var url = await provider.CreateDownloadUrlAsync(objectKey, DateTimeOffset.UtcNow.AddMinutes(2), CancellationToken.None);
            var uri = new Uri("https://localhost" + url);
            var encodedKey = QueryValue(uri, "key");
            var token = QueryValue(uri, "token");
            var expires = long.Parse(QueryValue(uri, "expires"), System.Globalization.CultureInfo.InvariantCulture);

            Assert.True(provider.ValidateAccessToken("GET", objectKey, expires, token));
            Assert.False(provider.ValidateAccessToken("GET", "other/file.png", expires, token));
            Assert.False(provider.ValidateAccessToken("GET", objectKey, DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds(), token));
            Assert.NotEmpty(encodedKey);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    [Fact]
    public async Task RejectsTraversalAndOversizedOrEmptyObjects()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var provider = Create(root);
            await Assert.ThrowsAsync<InvalidOperationException>(() => provider.CreateDownloadUrlAsync("../outside", DateTimeOffset.UtcNow.AddMinutes(1), CancellationToken.None));
            await Assert.ThrowsAsync<InvalidOperationException>(() => provider.SaveObjectAsync(new StorageObjectWriteRequest("tests/empty", "image/png", 1), new MemoryStream(Array.Empty<byte>()), CancellationToken.None));
            await Assert.ThrowsAsync<InvalidOperationException>(() => provider.SaveObjectAsync(new StorageObjectWriteRequest("tests/large", "image/png", 1), new MemoryStream(new byte[] { 1, 2 }), CancellationToken.None));
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private static LocalFileStorageProvider Create(string root)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Integrations:LocalStorageRoot"] = root,
            ["Integrations:LocalStorageSigningSecret"] = "unit-test-local-storage-signing-secret-which-is-long-enough"
        }).Build();
        return new LocalFileStorageProvider(config);
    }

    private static string CreateTemporaryRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "nesty-local-storage-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void DeleteTemporaryRoot(string root)
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }

    private static string QueryValue(Uri uri, string name) => uri.Query
        .TrimStart('?')
        .Split('&', StringSplitOptions.RemoveEmptyEntries)
        .Select(part => part.Split('=', 2))
        .Where(parts => parts.Length == 2 && string.Equals(Uri.UnescapeDataString(parts[0]), name, StringComparison.Ordinal))
        .Select(parts => Uri.UnescapeDataString(parts[1]))
        .Single();
}
