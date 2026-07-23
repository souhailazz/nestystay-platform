using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NestyStay.Api.Auth;
using NestyStay.Infrastructure.Persistence;
using NestyStay.Domain;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NestyStay.Api.Tests;

public sealed class NestyStayApiFactory : WebApplicationFactory<Program>
{
    public const string AdminToken = "test-admin-token";
    public const string OperatorToken = "test-operator-token";
    private const string TestingSessionSecret = "development-testing-session-secret-change-before-production";
    private const string SessionTokenPrefix = "nsty.v1.";
    private readonly string _databaseName = $"nestystay-api-tests-{Guid.NewGuid():N}";
    private readonly ServiceProvider _inMemoryProvider = new ServiceCollection()
        .AddEntityFrameworkInMemoryDatabase()
        .BuildServiceProvider();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:AdminTokenSha256"] = AdminTokenAuthenticationHandler.ComputeSha256Hex(AdminToken),
                ["Security:OperatorTokenSha256"] = AdminTokenAuthenticationHandler.ComputeSha256Hex(OperatorToken)
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<NestyStayDbContext>();
            services.RemoveAll<DbContextOptions<NestyStayDbContext>>();
            services.AddDbContext<NestyStayDbContext>(options => options
                .UseInMemoryDatabase(_databaseName)
                .UseInternalServiceProvider(_inMemoryProvider));
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inMemoryProvider.Dispose();
        }

        base.Dispose(disposing);
    }

    public static string UserToken(Guid userId, params UserRole[] roles)
    {
        var now = DateTimeOffset.UtcNow;
        return UserTokenWithLifetime(userId, now, now.AddHours(1), roles);
    }

    public static string UserTokenWithLifetime(
        Guid userId,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt,
        params UserRole[] roles)
    {
        var payload = JsonSerializer.Serialize(new
        {
            sub = userId,
            roles = roles.Length == 0 ? [UserRole.Guest.ToString()] : roles.Select(role => role.ToString()).ToArray(),
            iat = issuedAt.ToUnixTimeSeconds(),
            exp = expiresAt.ToUnixTimeSeconds()
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var payloadSegment = Base64UrlEncode(Encoding.UTF8.GetBytes(payload));
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(TestingSessionSecret));
        var signature = Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadSegment)));
        return $"{SessionTokenPrefix}{payloadSegment}.{signature}";
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
