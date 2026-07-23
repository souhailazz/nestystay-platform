using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using NestyStay.Api.Configuration;
using NestyStay.Domain;

namespace NestyStay.Api.Tests;

public sealed class SignedAccessTokenSecurityTests : IClassFixture<NestyStayApiFactory>
{
    private const string TokenPrefix = "nsty.v1.";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly NestyStayApiFactory _factory;

    public SignedAccessTokenSecurityTests(NestyStayApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProtectedEndpointsRejectMissingLegacyExpiredAndTamperedTokens()
    {
        using var client = _factory.CreateClient();
        var travelerId = Guid.NewGuid();
        var route = $"/api/spec/traveler/{travelerId}";

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(route)).StatusCode);

        client.DefaultRequestHeaders.Authorization = Bearer($"local-phase1-token-{travelerId:N}");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(route)).StatusCode);

        client.DefaultRequestHeaders.Authorization = Bearer($"local-google-token-{travelerId:N}");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(route)).StatusCode);

        var now = DateTimeOffset.UtcNow;
        client.DefaultRequestHeaders.Authorization = Bearer(
            NestyStayApiFactory.UserTokenWithLifetime(travelerId, now.AddHours(-2), now.AddHours(-1)));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(route)).StatusCode);

        var validToken = NestyStayApiFactory.UserToken(travelerId);
        client.DefaultRequestHeaders.Authorization = Bearer(ChangeLastCharacter(validToken));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(route)).StatusCode);

        client.DefaultRequestHeaders.Authorization = Bearer(RewritePayloadKeepingSignature(validToken));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(route)).StatusCode);

        client.DefaultRequestHeaders.Authorization = Bearer(validToken);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(route)).StatusCode);
    }

    [Fact]
    public async Task AdminEndpointsRejectSignedNonAdminTokensAndAcceptConfiguredAdminToken()
    {
        using var client = _factory.CreateClient();

        client.DefaultRequestHeaders.Authorization = Bearer(NestyStayApiFactory.UserToken(Guid.NewGuid(), UserRole.Guest));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/spec/admin/operations")).StatusCode);

        client.DefaultRequestHeaders.Authorization = Bearer(NestyStayApiFactory.UserToken(Guid.NewGuid(), UserRole.PropertyManager));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/spec/admin/operations")).StatusCode);

        client.DefaultRequestHeaders.Authorization = Bearer(NestyStayApiFactory.AdminToken);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/spec/admin/operations")).StatusCode);
    }

    [Fact]
    public void ProductionValidatorRequiresStrongSessionTokenSecretOnlyInProduction()
    {
        WithoutProductionEnvironmentVariables(() =>
        {
            var production = new TestHostEnvironment(Environments.Production);
            Assert.Throws<InvalidOperationException>(() =>
                ProductionIntegrationValidator.Validate(BuildProductionConfig(sessionTokenSecret: null), production));

            Assert.Throws<InvalidOperationException>(() =>
                ProductionIntegrationValidator.Validate(BuildProductionConfig("short-secret"), production));

            ProductionIntegrationValidator.Validate(
                BuildProductionConfig("strong-production-session-token-secret-32-plus"),
                production);

            ProductionIntegrationValidator.Validate(
                new ConfigurationBuilder().Build(),
                new TestHostEnvironment(Environments.Development));
        });
    }

    private static AuthenticationHeaderValue Bearer(string token) => new("Bearer", token);

    private static string ChangeLastCharacter(string token) =>
        token[..^1] + (token[^1] == 'A' ? "B" : "A");

    private static string RewritePayloadKeepingSignature(string token)
    {
        var body = token[TokenPrefix.Length..];
        var parts = body.Split('.', 2);
        var payload = JsonSerializer.Serialize(new
        {
            sub = Guid.NewGuid(),
            roles = new[] { UserRole.Guest.ToString() },
            iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
        }, JsonOptions);
        var payloadSegment = Base64UrlEncode(Encoding.UTF8.GetBytes(payload));
        return $"{TokenPrefix}{payloadSegment}.{parts[1]}";
    }

    private static IConfiguration BuildProductionConfig(string? sessionTokenSecret)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Security:AdminTokenSha256"] = new('a', 64),
            ["Webhooks:SharedSecret"] = "webhook-shared-secret",
            ["Integrations:StripeSecretKey"] = "sk_test_local",
            ["Integrations:AlibabaEkycTransactionUrlBase"] = "https://example.test/ekyc",
            ["Integrations:CloudflareR2UploadUrlBase"] = "https://example.test/r2",
            ["Integrations:InsuraGuestApiBaseUrl"] = "https://example.test/insuraguest"
        };

        if (sessionTokenSecret is not null)
        {
            settings["Security:SessionTokenSecret"] = sessionTokenSecret;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    }

    private static void WithoutProductionEnvironmentVariables(Action action)
    {
        string[] keys =
        [
            "NESTYSTAY_ADMIN_TOKEN_SHA256",
            "NESTYSTAY_SESSION_TOKEN_SECRET",
            "NESTYSTAY_WEBHOOK_SHARED_SECRET",
            "STRIPE_SECRET_KEY",
            "ALIBABA_EKYC_TRANSACTION_URL_BASE",
            "CLOUDFLARE_R2_UPLOAD_URL_BASE",
            "INSURAGUEST_API_BASE_URL"
        ];
        var previous = keys.ToDictionary(key => key, Environment.GetEnvironmentVariable);

        try
        {
            foreach (var key in keys)
            {
                Environment.SetEnvironmentVariable(key, null);
            }

            action();
        }
        finally
        {
            foreach (var (key, value) in previous)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "NestyStay.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
