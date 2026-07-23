using NestyStay.Domain;

namespace NestyStay.Application.Abstractions;

public sealed class DevelopmentAccessTokenService : IAccessTokenService
{
    public static DevelopmentAccessTokenService Instance { get; } = new();

    private DevelopmentAccessTokenService()
    {
    }

    public string Issue(Guid userId, IReadOnlyList<UserRole> roles, DateTimeOffset expiresAt) =>
        $"test-session-token-{userId:N}-{expiresAt.ToUnixTimeSeconds()}";

    public AccessTokenValidationResult? Validate(string token) => null;
}
