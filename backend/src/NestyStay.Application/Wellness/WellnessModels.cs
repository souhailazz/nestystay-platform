namespace NestyStay.Application.Wellness;

public interface IWellnessStore
{
    Task<WellnessOfficerDto> OnboardOfficerAsync(OnboardOfficerRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<WellnessOfficerDto>> GetOfficersAsync(string? status, CancellationToken cancellationToken);
    Task<WellnessOfficerDto?> GetOfficerAsync(Guid officerId, CancellationToken cancellationToken);
    Task<WellnessOfficerDto?> GetOfficerForUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<WellnessSubscriptionDto?> GetSubscriptionAsync(Guid hostUserId, CancellationToken cancellationToken);
    Task<WellnessSubscriptionDto> StartSubscriptionAsync(Guid hostUserId, CancellationToken cancellationToken);
    Task<WellnessSubscriptionDto> RenewSubscriptionAsync(Guid hostUserId, CancellationToken cancellationToken);
    Task<WellnessSubscriptionDto?> CancelSubscriptionAsync(Guid hostUserId, CancellationToken cancellationToken);
    Task<IReadOnlyList<WellnessOfficerDto>> GetAvailableOfficersAsync(string parish, DateTimeOffset scheduledAt, CancellationToken cancellationToken);
    Task<WellnessOfficerDto?> ApproveOfficerAsync(Guid officerId, AdminOfficerReviewRequest request, CancellationToken cancellationToken);
    Task<WellnessOfficerDto?> RejectOfficerAsync(Guid officerId, AdminOfficerReviewRequest request, CancellationToken cancellationToken);
    Task<WellnessOfficerDto?> SuspendOfficerAsync(Guid officerId, AdminOfficerReviewRequest request, CancellationToken cancellationToken);
    Task<WellnessOfficerDto?> ReactivateOfficerAsync(Guid officerId, AdminOfficerReviewRequest request, CancellationToken cancellationToken);
    Task<WellnessQuoteDto> QuoteVisitAsync(WellnessQuoteRequest request, CancellationToken cancellationToken);
    Task<WellnessVisitDto> CreateVisitAsync(CreateWellnessVisitRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<WellnessVisitDto>> GetVisitsAsync(Guid? hostUserId, Guid? propertyId, Guid? officerId, CancellationToken cancellationToken);
    Task<WellnessVisitDto?> GetVisitAsync(Guid visitId, CancellationToken cancellationToken);
    Task<WellnessReportDto?> GetReportAsync(Guid visitId, CancellationToken cancellationToken);
    Task<WellnessVisitDto?> AssignOfficerAsync(Guid visitId, AssignOfficerRequest request, CancellationToken cancellationToken);
    Task<WellnessVisitDto?> CancelVisitAsync(Guid visitId, CancelWellnessVisitRequest request, CancellationToken cancellationToken);
    Task<WellnessVisitDto?> RescheduleVisitAsync(Guid visitId, RescheduleWellnessVisitRequest request, CancellationToken cancellationToken);
    Task<WellnessReportPhotoUploadDto> PrepareReportPhotoUploadAsync(Guid visitId, PrepareWellnessReportPhotoUploadRequest request, bool adminOverride, CancellationToken cancellationToken);
    Task<WellnessReportPhotoUploadDto> UploadReportPhotoContentAsync(Guid visitId, Guid photoId, string officerBadgeNumber, string contentType, long sizeBytes, Stream content, bool adminOverride, CancellationToken cancellationToken);
    Task<WellnessVisitDto?> SubmitReportAsync(Guid visitId, SubmitWellnessReportRequest request, bool adminOverride, CancellationToken cancellationToken);
    Task<WellnessPayoutDto?> MarkPayoutPaidAsync(Guid visitId, MarkPayoutPaidRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<WellnessPayoutDto>> GetPayoutsAsync(string? status, CancellationToken cancellationToken);
    Task<WellnessAdminDashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken);
}

public interface IWellnessEnhancementStore
{
    Task<IReadOnlyList<WellnessOfficerDocumentDto>> ListOfficerDocumentsAsync(Guid officerId, Guid actorUserId, bool isAdmin, CancellationToken cancellationToken);
    Task<WellnessOfficerDocumentUploadDto> PrepareOfficerDocumentUploadAsync(Guid officerId, Guid actorUserId, PrepareWellnessOfficerDocumentUploadRequest request, bool isAdmin, CancellationToken cancellationToken);
    Task<WellnessOfficerDocumentUploadDto> UploadOfficerDocumentContentAsync(Guid officerId, Guid documentId, Guid actorUserId, string contentType, long sizeBytes, Stream content, bool isAdmin, CancellationToken cancellationToken);
    Task<WellnessOfficerDocumentDto?> ReviewOfficerDocumentAsync(Guid documentId, Guid actorUserId, ReviewWellnessOfficerDocumentRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<WellnessReportTemplateDto>> ListReportTemplatesAsync(bool activeOnly, CancellationToken cancellationToken);
    Task<WellnessReportTemplateDto> SaveReportTemplateAsync(Guid actorUserId, SaveWellnessReportTemplateRequest request, CancellationToken cancellationToken);
    Task<WellnessReportCollaborationDto> GetReportCollaborationAsync(Guid reportId, Guid actorUserId, bool isAdmin, CancellationToken cancellationToken);
    Task<WellnessReportPdfDto> RenderReportPdfAsync(Guid reportId, Guid actorUserId, bool isAdmin, CancellationToken cancellationToken);
    Task<WellnessReportCommentDto> AddReportCommentAsync(Guid reportId, Guid actorUserId, AddWellnessReportCommentRequest request, bool isAdmin, CancellationToken cancellationToken);
    Task<WellnessReportAcknowledgementDto> AcknowledgeReportAsync(Guid reportId, Guid actorUserId, CancellationToken cancellationToken);
    Task<WellnessFollowUpTaskDto> CreateFollowUpTaskAsync(Guid reportId, Guid actorUserId, CreateWellnessFollowUpTaskRequest request, bool isAdmin, CancellationToken cancellationToken);
    Task<IReadOnlyList<WellnessFollowUpTaskDto>> ListFollowUpTasksAsync(Guid actorUserId, bool isAdmin, CancellationToken cancellationToken);
    Task<WellnessPayoutStatementDto> GetPayoutStatementAsync(Guid actorUserId, bool isAdmin, DateOnly? from, DateOnly? to, string format, CancellationToken cancellationToken);
    Task<WellnessPayoutDisputeDto> CreatePayoutDisputeAsync(Guid payoutId, Guid actorUserId, CreateWellnessPayoutDisputeRequest request, CancellationToken cancellationToken);
    Task<WellnessPayoutDisputeDto?> ResolvePayoutDisputeAsync(Guid disputeId, Guid actorUserId, ResolveWellnessPayoutDisputeRequest request, CancellationToken cancellationToken);
}

public sealed record OnboardOfficerRequest(
    Guid? UserId,
    string BadgeNumber,
    string Parish,
    string CoverageArea,
    bool IsActiveOffDuty,
    bool IsRetired,
    string? VerificationMetadata = null,
    decimal? Latitude = null,
    decimal? Longitude = null,
    decimal? ServiceRadiusKm = null);

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
    string? AdminReviewSummary,
    decimal? Latitude = null,
    decimal? Longitude = null,
    decimal? ServiceRadiusKm = null);

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

public sealed record WellnessSubscriptionDto(
    Guid Id,
    Guid HostUserId,
    string PlanKey,
    decimal MonthlyAmount,
    string Currency,
    string Status,
    DateTimeOffset CurrentPeriodStart,
    DateTimeOffset CurrentPeriodEnd,
    int IncludedVisits,
    int UsedVisits,
    int RemainingVisits,
    string PaymentProvider,
    string PaymentReference);

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
    DateTimeOffset UpdatedAt,
    string ScheduledTimeZone = "America/Jamaica");

public sealed record AssignOfficerRequest(Guid OfficerId);

public sealed record CancelWellnessVisitRequest(string? Reason = null);

public sealed record RescheduleWellnessVisitRequest(
    DateTimeOffset ScheduledAt,
    string TimeZone = "America/Jamaica",
    string? Reason = null);

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

public sealed record PrepareWellnessOfficerDocumentUploadRequest(string DocumentType, string FileName, string ContentType, long SizeBytes, DateOnly? ExpiresOn = null);
public sealed record WellnessOfficerDocumentDto(Guid Id, Guid OfficerId, string DocumentType, string FileName, string ContentType, long SizeBytes, string Status, string ScanStatus, DateOnly? ExpiresOn, string ReviewStatus, string? ReviewReason, DateTimeOffset CreatedAt, DateTimeOffset? UploadedAt);
public sealed record WellnessOfficerDocumentUploadDto(Guid Id, Guid OfficerId, string DocumentType, string FileName, string ContentType, long SizeBytes, string ObjectKey, string UploadUrl, string Status, string ScanStatus, DateTimeOffset ExpiresAt, DateOnly? ExpiresOn, string? Sha256Hash = null);
public sealed record ReviewWellnessOfficerDocumentRequest(string Decision, string? Reason = null);
public sealed record WellnessReportTemplateDto(Guid Id, string Name, int Version, string DefinitionJson, bool IsActive, DateTimeOffset CreatedAt, Guid CreatedByUserId);
public sealed record SaveWellnessReportTemplateRequest(string Name, string DefinitionJson, bool IsActive = true);
public sealed record WellnessReportCommentDto(Guid Id, Guid ReportId, Guid AuthorUserId, string Body, DateTimeOffset CreatedAt);
public sealed record WellnessReportAcknowledgementDto(Guid ReportId, Guid AcknowledgedByUserId, DateTimeOffset AcknowledgedAt);
public sealed record WellnessFollowUpTaskDto(Guid Id, Guid ReportId, Guid VisitId, Guid PropertyId, string Title, string Description, string Priority, Guid? AssigneeUserId, DateTimeOffset? DueAt, string Status, DateTimeOffset CreatedAt);
public sealed record AddWellnessReportCommentRequest(string Body);
public sealed record CreateWellnessFollowUpTaskRequest(string Title, string Description, string Priority = "Normal", Guid? AssigneeUserId = null, DateTimeOffset? DueAt = null);
public sealed record WellnessReportCollaborationDto(Guid ReportId, IReadOnlyList<WellnessReportCommentDto> Comments, WellnessReportAcknowledgementDto? Acknowledgement, IReadOnlyList<WellnessFollowUpTaskDto> FollowUpTasks);
public sealed record WellnessReportPdfDto(string FileName, string ContentType, byte[] Content, DateTimeOffset GeneratedAt);
public sealed record WellnessPayoutStatementRowDto(Guid PayoutId, Guid VisitId, DateTimeOffset? EligibleAt, decimal GrossAmount, decimal PlatformFee, decimal OfficerAmount, string Currency, string Status, DateTimeOffset? PaidAt, string? ProviderReference);
public sealed record WellnessPayoutStatementDto(Guid OfficerUserId, DateOnly From, DateOnly To, decimal GrossTotal, decimal PlatformFeeTotal, decimal OfficerTotal, IReadOnlyList<WellnessPayoutStatementRowDto> Rows, string Format, string? DownloadFileName = null, string? DownloadBase64 = null);
public sealed record CreateWellnessPayoutDisputeRequest(string Reason, string? EvidenceJson = null);
public sealed record ResolveWellnessPayoutDisputeRequest(string Decision, string? Notes = null);
public sealed record WellnessPayoutDisputeDto(Guid Id, Guid PayoutId, Guid OfficerId, string Reason, string? EvidenceJson, string Status, string? Decision, string? DecisionNotes, Guid? DecidedByUserId, DateTimeOffset CreatedAt, DateTimeOffset? ResolvedAt);
