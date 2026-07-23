namespace NestyStay.Application.Wellness;

public interface IWellnessStore
{
    Task<WellnessOfficerDto> OnboardOfficerAsync(OnboardOfficerRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<WellnessOfficerDto>> GetOfficersAsync(string? status, CancellationToken cancellationToken);
    Task<WellnessOfficerDto?> GetOfficerAsync(Guid officerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<WellnessOfficerDto>> GetAvailableOfficersAsync(string parish, DateTimeOffset scheduledAt, CancellationToken cancellationToken);
    Task<WellnessOfficerDto?> ApproveOfficerAsync(Guid officerId, AdminOfficerReviewRequest request, CancellationToken cancellationToken);
    Task<WellnessOfficerDto?> RejectOfficerAsync(Guid officerId, AdminOfficerReviewRequest request, CancellationToken cancellationToken);
    Task<WellnessOfficerDto?> SuspendOfficerAsync(Guid officerId, AdminOfficerReviewRequest request, CancellationToken cancellationToken);
    Task<WellnessOfficerDto?> ReactivateOfficerAsync(Guid officerId, AdminOfficerReviewRequest request, CancellationToken cancellationToken);
    Task<WellnessQuoteDto> QuoteVisitAsync(WellnessQuoteRequest request, CancellationToken cancellationToken);
    Task<WellnessVisitDto> CreateVisitAsync(CreateWellnessVisitRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<WellnessVisitDto>> GetVisitsAsync(Guid? hostUserId, Guid? propertyId, Guid? officerId, CancellationToken cancellationToken);
    Task<WellnessVisitDto?> GetVisitAsync(Guid visitId, CancellationToken cancellationToken);
    Task<WellnessVisitDto?> AssignOfficerAsync(Guid visitId, AssignOfficerRequest request, CancellationToken cancellationToken);
    Task<WellnessVisitDto?> CancelVisitAsync(Guid visitId, CancelWellnessVisitRequest request, CancellationToken cancellationToken);
    Task<WellnessReportPhotoUploadDto> PrepareReportPhotoUploadAsync(Guid visitId, PrepareWellnessReportPhotoUploadRequest request, bool adminOverride, CancellationToken cancellationToken);
    Task<WellnessReportPhotoUploadDto> UploadReportPhotoContentAsync(Guid visitId, Guid photoId, string officerBadgeNumber, string contentType, long sizeBytes, Stream content, bool adminOverride, CancellationToken cancellationToken);
    Task<WellnessVisitDto?> SubmitReportAsync(Guid visitId, SubmitWellnessReportRequest request, bool adminOverride, CancellationToken cancellationToken);
    Task<WellnessPayoutDto?> MarkPayoutPaidAsync(Guid visitId, MarkPayoutPaidRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<WellnessPayoutDto>> GetPayoutsAsync(string? status, CancellationToken cancellationToken);
    Task<WellnessAdminDashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken);
}

public sealed record OnboardOfficerRequest(
    Guid? UserId,
    string BadgeNumber,
    string Parish,
    string CoverageArea,
    bool IsActiveOffDuty,
    bool IsRetired,
    string? VerificationMetadata = null);

public sealed record AdminOfficerReviewRequest(string? Reason = null, string? ReviewedBy = null);

public sealed record WellnessOfficerDto(
    Guid Id,
    Guid? UserId,
    string BadgeNumber,
    string Parish,
    string CoverageArea,
    bool IsActiveOffDuty,
    bool IsRetired,
    string VerificationStatus,
    string OnboardingStatus,
    string AvailabilityStatus,
    IReadOnlyList<string> FreeBadges,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? AdminReviewSummary);

public sealed record WellnessQuoteRequest(
    Guid HostUserId,
    Guid PropertyId,
    string VisitType,
    DateTimeOffset ScheduledAt,
    string Parish,
    string? Area = null);

public sealed record WellnessQuoteDto(
    Guid HostUserId,
    Guid PropertyId,
    string VisitType,
    DateTimeOffset ScheduledAt,
    int DurationMinutes,
    decimal Price,
    decimal PlatformFee,
    decimal OfficerPayoutAmount,
    string Currency,
    bool Eligible,
    IReadOnlyList<string> MissingRequirements,
    string EmergencyNumber);

public sealed record CreateWellnessVisitRequest(
    Guid HostUserId,
    Guid PropertyId,
    string VisitType,
    DateTimeOffset ScheduledAt,
    string Parish,
    string? Area = null);

public sealed record WellnessVisitDto(
    Guid Id,
    Guid HostUserId,
    Guid PropertyId,
    Guid? OfficerId,
    string? OfficerBadgeNumber,
    string Parish,
    string Area,
    string VisitType,
    DateTimeOffset ScheduledAt,
    int DurationMinutes,
    decimal Price,
    decimal PlatformFee,
    decimal OfficerPayoutAmount,
    string Currency,
    string PaymentStatus,
    string VisitStatus,
    string ReportStatus,
    string? PaymentAuthorizationReference,
    string? PaymentCaptureReference,
    IReadOnlyList<string> Timeline,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AssignOfficerRequest(Guid OfficerId);

public sealed record CancelWellnessVisitRequest(string? Reason = null);

public sealed record PrepareWellnessReportPhotoUploadRequest(
    string OfficerBadgeNumber,
    string FileName,
    string ContentType,
    long SizeBytes);

public sealed record WellnessReportPhotoUploadDto(
    Guid Id,
    Guid VisitId,
    Guid OfficerId,
    string FileName,
    string ContentType,
    long SizeBytes,
    string ObjectKey,
    string UploadUrl,
    string Status,
    string ScanStatus,
    DateTimeOffset ExpiresAt,
    string? Sha256Hash = null);

public sealed record SubmitWellnessReportRequest(
    string OfficerBadgeNumber,
    string Notes,
    IReadOnlyList<string>? Photos = null,
    string? LocationMetadata = null);

public sealed record MarkPayoutPaidRequest(string? ProviderReference = null, string? Notes = null);

public sealed record WellnessReportDto(
    Guid Id,
    Guid VisitId,
    Guid OfficerId,
    DateTimeOffset SubmittedAt,
    string ReportStatus,
    string Notes,
    IReadOnlyList<string> Photos);

public sealed record WellnessPayoutDto(
    Guid Id,
    Guid VisitId,
    Guid OfficerId,
    decimal GrossAmount,
    decimal PlatformFee,
    decimal OfficerAmount,
    string Currency,
    string Status,
    DateTimeOffset? EligibleAt,
    DateTimeOffset? PaidAt,
    string? ProviderReference);

public sealed record WellnessAdminDashboardDto(
    int PendingOfficers,
    int VerifiedOfficers,
    int RequestedVisits,
    int ScheduledVisits,
    int CompletedVisits,
    int PendingPayouts,
    decimal PendingPayoutAmount,
    IReadOnlyList<WellnessOfficerDto> OfficerQueue,
    IReadOnlyList<WellnessVisitDto> RecentVisits,
    IReadOnlyList<WellnessPayoutDto> Payouts);
