using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NestyStay.Application.Abstractions;
using NestyStay.Domain;

namespace NestyStay.Api.Auth;

public sealed class SignedAccessTokenService(IConfiguration configuration, IHostEnvironment environment) : IAccessTokenService
{
    private const string Prefix = "nsty.v1.";
    private const string DevelopmentKeyId = "dev";
    private const string DefaultKeyId = "v1";
    private const int MinimumSecretBytes = 32;
    private static readonly TimeSpan MaximumTokenLifetime = TimeSpan.FromHours(12);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string Issue(Guid userId, IReadOnlyList<UserRole> roles, DateTimeOffset expiresAt)
    {
        var issuedAt = DateTimeOffset.UtcNow;
        if (expiresAt <= issuedAt || expiresAt - issuedAt > MaximumTokenLifetime)
        {
            throw new InvalidOperationException("Session token lifetime is invalid.");
        }

        var key = ResolveSigningKeys().Current;
        var payload = new AccessTokenPayload(
            userId,
            roles.Select(role => role.ToString()).Distinct(StringComparer.Ordinal).Order().ToArray(),
            Guid.NewGuid().ToString("N"),
            issuedAt.ToUnixTimeSeconds(),
            expiresAt.ToUnixTimeSeconds());
        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
        var payloadSegment = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        var signature = Sign(key.Secret, $"{key.Id}.{payloadSegment}");
        return $"{Prefix}{key.Id}.{payloadSegment}.{signature}";
    }

    public AccessTokenValidationResult? Validate(string token)
    {
        if (!token.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return null;
        }

        var parts = token[Prefix.Length..].Split('.', 3);
        if (parts.Length != 3 ||
            string.IsNullOrWhiteSpace(parts[0]) ||
            string.IsNullOrWhiteSpace(parts[1]) ||
            string.IsNullOrWhiteSpace(parts[2]))
        {
            return null;
        }

        var keyId = parts[0];
        var payloadSegment = parts[1];
        var signature = parts[2];
        var keys = ResolveSigningKeys();
        if (!keys.ById.TryGetValue(keyId, out var key))
        {
            return null;
        }

        var expectedSignature = Sign(key.Secret, $"{keyId}.{payloadSegment}");
        if (!FixedTimeEquals(signature, expectedSignature))
        {
            return null;
        }

        AccessTokenPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<AccessTokenPayload>(Base64UrlDecode(payloadSegment), JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (FormatException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }

        if (payload is null ||
            payload.Sub == Guid.Empty ||
            string.IsNullOrWhiteSpace(payload.Jti) ||
            payload.Roles is not { Length: > 0 } ||
            payload.Iat <= 0 ||
            payload.Exp <= payload.Iat)
        {
            return null;
        }

        DateTimeOffset issuedAt;
        DateTimeOffset expiresAt;
        try
        {
            issuedAt = DateTimeOffset.FromUnixTimeSeconds(payload.Iat);
            expiresAt = DateTimeOffset.FromUnixTimeSeconds(payload.Exp);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        if (expiresAt <= now ||
            issuedAt > now.AddMinutes(1) ||
            expiresAt - issuedAt > MaximumTokenLifetime)
        {
            return null;
        }

        var roles = new List<UserRole>();
        foreach (var role in payload.Roles)
        {
            if (string.IsNullOrWhiteSpace(role) ||
                !Enum.TryParse<UserRole>(role, ignoreCase: false, out var parsed))
            {
                return null;
            }

            roles.Add(parsed);
        }

        var distinctRoles = roles.Distinct().ToArray();
        if (distinctRoles.Length == 0)
        {
            return null;
        }

        return new AccessTokenValidationResult(payload.Sub, distinctRoles, issuedAt, expiresAt, payload.Jti, keyId);
    }

    private static string Sign(byte[] secret, string signedContent)
    {
        using var hmac = new HMACSHA256(secret);
        return Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(signedContent)));
    }

    private SigningKeys ResolveSigningKeys()
    {
        var configuredKeys = configuration.GetSection("Security:SessionTokenKeys")
            .GetChildren()
            .Where(child => !string.IsNullOrWhiteSpace(child.Value))
            .Select(child => new SigningKey(child.Key, EncodeSecret(child.Value!)))
            .ToDictionary(key => key.Id, StringComparer.Ordinal);

        if (configuredKeys.Count > 0)
        {
            var currentKeyId = configuration["Security:SessionTokenKeyId"] ?? configuredKeys.Keys.Order(StringComparer.Ordinal).Last();
            if (!configuredKeys.TryGetValue(currentKeyId, out var currentKey))
            {
                throw new InvalidOperationException("Current session token signing key is not configured.");
            }

            return new SigningKeys(currentKey, configuredKeys);
        }

        var configured = configuration["Security:SessionTokenSecret"];
        var secret = string.IsNullOrWhiteSpace(configured)
            ? Environment.GetEnvironmentVariable("NESTYSTAY_SESSION_TOKEN_SECRET")
            : configured;
        var keyId = configuration["Security:SessionTokenKeyId"] ?? DefaultKeyId;

        if (string.IsNullOrWhiteSpace(secret))
        {
            if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
            {
                throw new InvalidOperationException("Session token signing secret is not configured.");
            }

            secret = "development-testing-session-secret-change-before-production";
            keyId = DevelopmentKeyId;
        }

        var key = new SigningKey(keyId, EncodeSecret(secret));
        return new SigningKeys(key, new Dictionary<string, SigningKey>(StringComparer.Ordinal)
        {
            [key.Id] = key
        });
    }

    private static byte[] EncodeSecret(string secret)
    {
        var bytes = Encoding.UTF8.GetBytes(secret);
        if (bytes.Length < MinimumSecretBytes)
        {
            throw new InvalidOperationException("Session token signing secret must be at least 32 bytes.");
        }

        return bytes;
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

    private sealed record SigningKey(string Id, byte[] Secret);

    private sealed record SigningKeys(SigningKey Current, IReadOnlyDictionary<string, SigningKey> ById);

    private sealed record AccessTokenPayload(Guid Sub, string[] Roles, string Jti, long Iat, long Exp);
}
