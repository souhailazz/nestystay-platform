using NestyStay.Domain;

namespace NestyStay.Application.Abstractions;

public interface IAccessTokenService
{
    string Issue(Guid userId, IReadOnlyList<UserRole> roles, DateTimeOffset expiresAt);
    AccessTokenValidationResult? Validate(string token);
}

public sealed record AccessTokenValidationResult(
    Guid UserId,
    IReadOnlyList<UserRole> Roles,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt);
