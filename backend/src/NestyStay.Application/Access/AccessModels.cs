using NestyStay.Application.PhaseOne;

namespace NestyStay.Application.Access;

public interface IQrAccessStore
{
    Task<QrIssueResult> IssueForBookingAsync(Guid bookingId, Guid actorUserId, CancellationToken cancellationToken);
    Task<QrAccessDto?> GetAsync(Guid qrId, Guid actorUserId, CancellationToken cancellationToken);
    Task<QrAccessDto?> RevokeAsync(Guid qrId, Guid actorUserId, CancellationToken cancellationToken);
    Task<QrValidationResult> ValidateAsync(string token, Guid propertyId, string? deviceMetadata, CancellationToken cancellationToken);
}

public sealed record QrIssueResult(
    Guid Id,
    Guid BookingId,
    Guid PropertyId,
    DateTimeOffset ValidFrom,
    DateTimeOffset ExpiresAt,
    string Status,
    string Token,
    string ValidationUrl);

public sealed record QrAccessDto(
    Guid Id,
    Guid BookingId,
    Guid PropertyId,
    DateTimeOffset ValidFrom,
    DateTimeOffset ExpiresAt,
    bool IsRevoked,
    int ValidationCount,
    DateTimeOffset? LastValidatedAt,
    string Status,
    string? Token = null,
    string? ValidationUrl = null);

public sealed record QrValidationResult(
    bool Valid,
    string Result,
    Guid? BookingId,
    Guid? PropertyId,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ExpiresAt,
    int ValidationCount,
    string Message);
