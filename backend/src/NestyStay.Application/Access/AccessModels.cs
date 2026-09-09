using NestyStay.Application.PhaseOne;

namespace NestyStay.Application.Access;

public interface IQrAccessStore
{
    Task<QrIssueResult> IssueForBookingAsync(Guid bookingId, Guid actorUserId, CancellationToken cancellationToken);
    Task<QrAccessDto?> GetAsync(Guid qrId, Guid actorUserId, CancellationToken cancellationToken);
    Task<QrAccessDto?> RevokeAsync(Guid qrId, Guid actorUserId, string? reason, CancellationToken cancellationToken);
    Task<IReadOnlyList<QrAccessDto>> ListAsync(Guid actorUserId, CancellationToken cancellationToken);
    Task<IReadOnlyList<QrHistoryEventDto>> HistoryAsync(Guid qrId, Guid actorUserId, CancellationToken cancellationToken);
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
    string? RevokeReason = null,
    string? Token = null,
    string? ValidationUrl = null);

public sealed record QrHistoryEventDto(
    Guid Id,
    Guid QrAccessCodeId,
    string EventType,
    string Status,
    DateTimeOffset OccurredAt,
    string? Reason = null,
    string? DeviceMetadata = null);

public sealed record QrValidationResult(
    bool Valid,
    string Result,
    Guid? BookingId,
    Guid? PropertyId,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ExpiresAt,
    int ValidationCount,
    string Message);
