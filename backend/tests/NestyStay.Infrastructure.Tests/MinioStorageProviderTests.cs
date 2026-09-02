using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NestyStay.Application.Abstractions;
using NestyStay.Infrastructure.Storage;

namespace NestyStay.Infrastructure.Tests;

public sealed class MinioStorageProviderTests
{
    [Fact]
    public async Task LocalMinioRoundTripAndAuthorization()
    {
        var endpoint = Environment.GetEnvironmentVariable("MINIO_TEST_ENDPOINT");
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            // The test is intentionally opt-in so normal unit runs stay
            // zero-dependency. CI/staging enables it with a disposable MinIO.
            return;
        }

        var accessKey = Environment.GetEnvironmentVariable("MINIO_TEST_ACCESS_KEY") ?? "nestystay-test";
        var secretKey = Environment.GetEnvironmentVariable("MINIO_TEST_SECRET_KEY") ?? "nestystay-test-password";
        var bucket = Environment.GetEnvironmentVariable("MINIO_TEST_BUCKET") ?? "nesty-test";
        var provider = Create(endpoint, bucket, accessKey, secretKey);
        var objectKey = $"tests/{Guid.NewGuid():N}/duplicate.png";
        var isolatedObjectKey = $"tests/{Guid.NewGuid():N}/isolated.png";
        var payload = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        var first = await provider.SaveObjectAsync(new StorageObjectWriteRequest(objectKey, "image/png", 1024), new MemoryStream(payload), CancellationToken.None);
        var secondPayload = payload.Concat(new byte[] { 0x00 }).ToArray();
        var second = await provider.SaveObjectAsync(new StorageObjectWriteRequest(objectKey, "image/png", 1024), new MemoryStream(secondPayload), CancellationToken.None);
        var isolatedPayload = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x01, 0x02, 0x03, 0x04 };
        await provider.SaveObjectAsync(new StorageObjectWriteRequest(isolatedObjectKey, "image/png", 1024), new MemoryStream(isolatedPayload), CancellationToken.None);
        Assert.Equal("image/png", first.ContentType);
        Assert.Equal(secondPayload.Length, second.SizeBytes);
        Assert.NotEqual(first.Sha256Hash, second.Sha256Hash);

        using var http = new HttpClient();
        var download = await provider.CreateDownloadUrlAsync(objectKey, DateTimeOffset.UtcNow.AddMinutes(2), CancellationToken.None);
        using var response = await http.GetAsync(download);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(secondPayload, await response.Content.ReadAsByteArrayAsync());
        var isolatedDownload = await provider.CreateDownloadUrlAsync(isolatedObjectKey, DateTimeOffset.UtcNow.AddMinutes(2), CancellationToken.None);
        using var isolatedResponse = await http.GetAsync(isolatedDownload);
        Assert.Equal(HttpStatusCode.OK, isolatedResponse.StatusCode);
        Assert.Equal(isolatedPayload, await isolatedResponse.Content.ReadAsByteArrayAsync());

        var unauthorized = Create(endpoint, bucket, accessKey, "wrong-secret");
        var unauthorizedUrl = await unauthorized.CreateDownloadUrlAsync(objectKey, DateTimeOffset.UtcNow.AddMinutes(2), CancellationToken.None);
        using var unauthorizedResponse = await http.GetAsync(unauthorizedUrl);
        Assert.Equal(HttpStatusCode.Forbidden, unauthorizedResponse.StatusCode);
    }

    [Fact]
    public async Task RejectsTraversalAndOversizedObjectsBeforeNetwork()
    {
        var endpoint = Environment.GetEnvironmentVariable("MINIO_TEST_ENDPOINT") ?? "http://127.0.0.1:9000";
        var provider = Create(endpoint, "nesty-test", "test", "test-password");
        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.CreateDownloadUrlAsync("../outside", DateTimeOffset.UtcNow.AddMinutes(1), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.SaveObjectAsync(new StorageObjectWriteRequest("tests/empty", "image/png", 1), new MemoryStream(Array.Empty<byte>()), CancellationToken.None));
    }

    private static MinioStorageProvider Create(string endpoint, string bucket, string accessKey, string secretKey)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Integrations:MinioEndpoint"] = endpoint,
            ["Integrations:MinioBucket"] = bucket,
            ["Integrations:MinioAccessKey"] = accessKey,
            ["Integrations:MinioSecretKey"] = secretKey,
            ["Integrations:MinioRegion"] = "us-east-1"
        }).Build();
        var services = new ServiceCollection().AddHttpClient("object-storage").Services.BuildServiceProvider();
        return new MinioStorageProvider(config, services.GetRequiredService<IHttpClientFactory>());
    }
}
