using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NestyStay.Application.Abstractions;
using NestyStay.Domain;

namespace NestyStay.Api.Auth;

public sealed class SignedAccessTokenService(IConfiguration configuration, IHostEnvironment environment) : IAccessTokenService
{
    private const string Prefix = "nsty.v1.";
    private const int MinimumSecretLength = 32;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string Issue(Guid userId, IReadOnlyList<UserRole> roles, DateTimeOffset expiresAt)
    {
        var issuedAt = DateTimeOffset.UtcNow;
        var payload = new AccessTokenPayload(
            userId,
            roles.Select(role => role.ToString()).Distinct(StringComparer.Ordinal).Order().ToArray(),
            issuedAt.ToUnixTimeSeconds(),
            expiresAt.ToUnixTimeSeconds());
        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
        var payloadSegment = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        var signature = Sign(payloadSegment);
        return $"{Prefix}{payloadSegment}.{signature}";
    }

    public AccessTokenValidationResult? Validate(string token)
    {
        if (!token.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return null;
        }

        var parts = token[Prefix.Length..].Split('.', 2);
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
        {
            return null;
        }

        var expectedSignature = Sign(parts[0]);
        if (!FixedTimeEquals(parts[1], expectedSignature))
        {
            return null;
        }

        AccessTokenPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<AccessTokenPayload>(Base64UrlDecode(parts[0]), JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (FormatException)
        {
            return null;
        }

        if (payload is null || payload.Sub == Guid.Empty || payload.Iat <= 0 || payload.Exp <= payload.Iat)
        {
            return null;
        }

        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(payload.Iat);
        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(payload.Exp);
        var now = DateTimeOffset.UtcNow;
        if (expiresAt <= now || issuedAt > now.AddMinutes(1))
        {
            return null;
        }

        var roles = payload.Roles
            .Select(role => Enum.TryParse<UserRole>(role, ignoreCase: false, out var parsed) ? parsed : (UserRole?)null)
            .Where(role => role.HasValue)
            .Select(role => role!.Value)
            .Distinct()
            .ToArray();

        if (roles.Length == 0)
        {
            return null;
        }

        return new AccessTokenValidationResult(payload.Sub, roles, issuedAt, expiresAt);
    }

    private string Sign(string payloadSegment)
    {
        using var hmac = new HMACSHA256(ResolveSecret());
        return Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadSegment)));
    }

    private byte[] ResolveSecret()
    {
        var configured = configuration["Security:SessionTokenSecret"];
        var secret = string.IsNullOrWhiteSpace(configured)
            ? Environment.GetEnvironmentVariable("NESTYSTAY_SESSION_TOKEN_SECRET")
            : configured;

        if (string.IsNullOrWhiteSpace(secret))
        {
            if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
            {
                secret = "development-testing-session-secret-change-before-production";
            }
            else
            {
                throw new InvalidOperationException("Session token signing secret is not configured.");
            }
        }

        if (secret.Length < MinimumSecretLength)
        {
            throw new InvalidOperationException("Session token signing secret must be at least 32 characters.");
        }

        return Encoding.UTF8.GetBytes(secret);
    }

    private static bool FixedTimeEquals(string first, string second)
    {
        var firstBytes = Encoding.ASCII.GetBytes(first);
        var secondBytes = Encoding.ASCII.GetBytes(second);
        return firstBytes.Length == secondBytes.Length && CryptographicOperations.FixedTimeEquals(firstBytes, secondBytes);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        return Convert.FromBase64String(padded);
    }

    private sealed record AccessTokenPayload(Guid Sub, string[] Roles, long Iat, long Exp);
}
