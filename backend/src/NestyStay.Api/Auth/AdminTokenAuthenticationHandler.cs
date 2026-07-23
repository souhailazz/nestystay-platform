using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using NestyStay.Application.Abstractions;
using NestyStay.Domain;

namespace NestyStay.Api.Auth;

public sealed class AdminTokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration,
    IAccessTokenService accessTokenService) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "NestyStayAdminToken";
    public const string AdminPolicyName = "AdminOnly";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = ReadBearerToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (MatchesConfiguredHash(token, ResolveSecretHash("Security:AdminTokenSha256", "NESTYSTAY_ADMIN_TOKEN_SHA256")))
        {
            return Task.FromResult(Success(UserRole.Admin));
        }

        if (MatchesConfiguredHash(token, ResolveSecretHash("Security:OperatorTokenSha256", "NESTYSTAY_OPERATOR_TOKEN_SHA256")))
        {
            return Task.FromResult(Success(UserRole.PropertyManager));
        }

        if (accessTokenService.Validate(token) is { } session)
        {
            return Task.FromResult(Success(session.UserId, session.Roles));
        }

        return Task.FromResult(AuthenticateResult.Fail("Invalid NestyStay bearer token."));
    }

    public static string ComputeSha256Hex(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private AuthenticateResult Success(UserRole role) =>
        Success($"{role.ToString().ToLowerInvariant()}-token", [role]);

    private AuthenticateResult Success(Guid userId, IReadOnlyList<UserRole> roles) =>
        Success(userId.ToString(), roles);

    private AuthenticateResult Success(string nameIdentifier, IReadOnlyList<UserRole> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, nameIdentifier)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role.ToString())));
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    private string? ReadBearerToken()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = authorization["Bearer ".Length..].Trim();
        return token.Length == 0 ? null : token;
    }

    private string? ResolveSecretHash(string configurationKey, string environmentKey)
    {
        var configured = configuration[configurationKey];
        return string.IsNullOrWhiteSpace(configured)
            ? Environment.GetEnvironmentVariable(environmentKey)
            : configured;
    }

    private static bool MatchesConfiguredHash(string token, string? configuredHash)
    {
        if (string.IsNullOrWhiteSpace(configuredHash))
        {
            return false;
        }

        try
        {
            var normalizedHash = configuredHash.Trim().Replace(":", string.Empty, StringComparison.Ordinal);
            var expected = Convert.FromHexString(normalizedHash.ToUpper(CultureInfo.InvariantCulture));
            var actual = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return expected.Length == actual.Length && CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
