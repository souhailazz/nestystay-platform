using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using NestyStay.Api.Auth;
using NestyStay.Api.Configuration;
using NestyStay.Domain;

namespace NestyStay.Api.Tests;

public sealed class SignedAccessTokenSecurityTests : IClassFixture<NestyStayApiFactory>
{
    private const string TokenPrefix = "nsty.v1.";
    private const string TestingSessionSecret = "development-testing-session-secret-change-before-production";
    private const string TestingKeyId = "dev";
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
    public async Task ProtectedEndpointsRejectInvalidTokenClaimsAndMalformedPayloads()
    {
        using var client = _factory.CreateClient();
        var travelerId = Guid.NewGuid();
        var route = $"/api/spec/traveler/{travelerId}";
        var now = DateTimeOffset.UtcNow;

        client.DefaultRequestHeaders.Authorization = Bearer(SignedToken(travelerId, ["Guest", "Root"], now, now.AddHours(1)));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(route)).StatusCode);

        client.DefaultRequestHeaders.Authorization = Bearer(SignedToken(travelerId, ["Guest"], now.AddMinutes(5), now.AddHours(1)));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(route)).StatusCode);

        client.DefaultRequestHeaders.Authorization = Bearer(SignedToken(travelerId, ["Guest"], now, now.AddHours(13)));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(route)).StatusCode);

        client.DefaultRequestHeaders.Authorization = Bearer(SignedToken(travelerId, ["Guest"], now, now.AddHours(1), jti: null));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(route)).StatusCode);

        client.DefaultRequestHeaders.Authorization = Bearer(SignedToken(travelerId, ["Guest"], now, now.AddHours(1), keyId: "unknown"));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(route)).StatusCode);

        client.DefaultRequestHeaders.Authorization = Bearer($"{TokenPrefix}{TestingKeyId}.not-base64.signature");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(route)).StatusCode);
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

            Assert.Throws<InvalidOperationException>(() =>
                ProductionIntegrationValidator.Validate(
                    BuildProductionConfig("strong-production-session-token-secret-32-plus", totpProtectionKey: "short"),
                    production));

            ProductionIntegrationValidator.Validate(
                BuildProductionConfig("strong-production-session-token-secret-32-plus"),
                production);

            ProductionIntegrationValidator.Validate(
                new ConfigurationBuilder().Build(),
                new TestHostEnvironment(Environments.Development));
        });
    }

    [Fact]
    public void SignedTokenServiceUsesCurrentKeyAndAcceptsConfiguredRotationKeys()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:SessionTokenKeyId"] = "new",
                ["Security:SessionTokenKeys:old"] = "old-session-token-secret-that-is-long-enough",
                ["Security:SessionTokenKeys:new"] = "new-session-token-secret-that-is-long-enough"
            })
            .Build();
        var service = new SignedAccessTokenService(config, new TestHostEnvironment(Environments.Production));
        var userId = Guid.NewGuid();
        var issued = service.Issue(userId, [UserRole.Guest], DateTimeOffset.UtcNow.AddHours(1));

        Assert.StartsWith($"{TokenPrefix}new.", issued, StringComparison.Ordinal);
        var issuedResult = service.Validate(issued);
        Assert.NotNull(issuedResult);
        Assert.Equal("new", issuedResult.KeyId);
        Assert.NotEmpty(issuedResult.TokenId);

        var previousKeyToken = SignedToken(
            userId,
            ["Guest"],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddHours(1),
            keyId: "old",
            secret: "old-session-token-secret-that-is-long-enough");
        var previousResult = service.Validate(previousKeyToken);
        Assert.NotNull(previousResult);
        Assert.Equal("old", previousResult.KeyId);
    }

    private static AuthenticationHeaderValue Bearer(string token) => new("Bearer", token);

    private static string ChangeLastCharacter(string token) =>
        token[..^1] + (token[^1] == 'A' ? "B" : "A");

    private static string RewritePayloadKeepingSignature(string token)
    {
        var body = token[TokenPrefix.Length..];
        var parts = body.Split('.', 3);
        var payload = JsonSerializer.Serialize(new
        {
            sub = Guid.NewGuid(),
            roles = new[] { UserRole.Guest.ToString() },
            jti = Guid.NewGuid().ToString("N"),
            iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
        }, JsonOptions);
        var payloadSegment = Base64UrlEncode(Encoding.UTF8.GetBytes(payload));
        return $"{TokenPrefix}{parts[0]}.{payloadSegment}.{parts[2]}";
    }

    private static string SignedToken(
        Guid userId,
        string[] roles,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt,
        string? jti = "generated",
        string keyId = TestingKeyId,
        string secret = TestingSessionSecret)
    {
        var payload = jti is null
            ? JsonSerializer.Serialize(new
            {
                sub = userId,
                roles,
                iat = issuedAt.ToUnixTimeSeconds(),
                exp = expiresAt.ToUnixTimeSeconds()
            }, JsonOptions)
            : JsonSerializer.Serialize(new
            {
                sub = userId,
                roles,
                jti = jti == "generated" ? Guid.NewGuid().ToString("N") : jti,
                iat = issuedAt.ToUnixTimeSeconds(),
                exp = expiresAt.ToUnixTimeSeconds()
            }, JsonOptions);
        var payloadSegment = Base64UrlEncode(Encoding.UTF8.GetBytes(payload));
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{keyId}.{payloadSegment}")));
        return $"{TokenPrefix}{keyId}.{payloadSegment}.{signature}";
    }

    private static IConfiguration BuildProductionConfig(
        string? sessionTokenSecret,
        string totpProtectionKey = "strong-production-totp-protection-key-32-plus")
    {
        var settings = new Dictionary<string, string?>
        {
            ["Security:AdminTokenSha256"] = new('a', 64),
            ["Security:TotpSecretProtectionKey"] = totpProtectionKey,
            ["Webhooks:SharedSecret"] = "webhook-shared-secret",
            ["Webhooks:StripeSigningSecret"] = "whsec_test",
            ["Integrations:StripeSecretKey"] = "sk_test_local",
            ["Integrations:StripePublishableKey"] = "pk_test_local",
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
            "NESTYSTAY_TOTP_SECRET_PROTECTION_KEY",
            "NESTYSTAY_WEBHOOK_SHARED_SECRET",
            "STRIPE_WEBHOOK_SECRET",
            "STRIPE_SECRET_KEY",
            "STRIPE_PUBLISHABLE_KEY",
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
