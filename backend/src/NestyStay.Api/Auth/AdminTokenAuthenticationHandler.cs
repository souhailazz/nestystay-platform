using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using NestyStay.Application.Admin;
using NestyStay.Application.Abstractions;
using NestyStay.Application.PhaseOne;
using NestyStay.Domain;

namespace NestyStay.Api.Auth;

public sealed class AdminTokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration,
    IHostEnvironment environment,
    IAccessTokenService accessTokenService,
    IPhaseOneStore phaseOneStore) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "NestyStayAdminToken";
    public const string AdminPolicyName = "AdminOnly";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = ReadBearerToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.NoResult();
        }

        if (AcceptsLegacyAdminTokens() &&
            MatchesConfiguredHash(token, ResolveSecretHash("Security:AdminTokenSha256", "NESTYSTAY_ADMIN_TOKEN_SHA256")))
        {
            return Success(UserRole.Admin, AdminPermissionCatalog.All);
        }

        if (AcceptsLegacyAdminTokens() &&
            MatchesConfiguredHash(token, ResolveSecretHash("Security:OperatorTokenSha256", "NESTYSTAY_OPERATOR_TOKEN_SHA256")))
        {
            return Success(UserRole.PropertyManager);
        }

        if (accessTokenService.Validate(token) is { } session)
        {
            if (session.Roles.Contains(UserRole.Admin))
            {
                var administrator = await phaseOneStore.GetAdministratorSessionAsync(
                    session.UserId,
                    session.IssuedAt,
                    Context.RequestAborted);
                if (administrator is null)
                {
                    return AuthenticateResult.Fail("NestyStay administrator session is not active.");
                }

                return Success(administrator.UserId, administrator.Roles, administrator.Permissions);
            }

            if (!await phaseOneStore.IsSessionActiveAsync(session.UserId, session.IssuedAt, Context.RequestAborted))
            {
                return AuthenticateResult.Fail("NestyStay session has been invalidated.");
            }

            return Success(session.UserId, session.Roles);
        }

        return AuthenticateResult.Fail("Invalid NestyStay bearer token.");
    }

    public static string ComputeSha256Hex(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private AuthenticateResult Success(UserRole role, IReadOnlyList<string>? permissions = null) =>
        Success($"{role.ToString().ToLowerInvariant()}-token", [role], permissions);

    private AuthenticateResult Success(Guid userId, IReadOnlyList<UserRole> roles) =>
        Success(userId.ToString(), roles, null);

    private AuthenticateResult Success(Guid userId, IReadOnlyList<UserRole> roles, IReadOnlyList<string>? permissions) =>
        Success(userId.ToString(), roles, permissions);

    private AuthenticateResult Success(string nameIdentifier, IReadOnlyList<UserRole> roles, IReadOnlyList<string>? permissions)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, nameIdentifier)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role.ToString())));
        claims.AddRange((permissions ?? []).Select(permission => new Claim(AdminAuthorizationPolicies.PermissionClaimType, permission)));
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    private bool AcceptsLegacyAdminTokens() =>
        !environment.IsProduction() ||
        configuration.GetValue<bool>("Security:AllowLegacyAdminTokens");

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
