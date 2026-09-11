using NestyStay.Domain;
using NestyStay.Domain.Common;

namespace NestyStay.Infrastructure.Persistence.Milestones;

public sealed class MilestoneUser : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public byte[] TwoFactorSecret { get; set; } = [];
    public bool IsTwoFactorEnabled { get; set; } = true;
    public string Status { get; set; } = "Active";
    public string AdminPermissionsJson { get; set; } = "[]";
    public string RolesJson { get; set; } = "[]";
    public int FailedLoginAttempts { get; set; }
    public DateTimeOffset? LockoutEndsAt { get; set; }
    public DateTimeOffset? SessionInvalidatedAt { get; set; }
    public long? LastAcceptedTotpCounter { get; set; }
    public string? PendingTwoFactorEnrollmentId { get; set; }
    public byte[]? PendingTwoFactorSecret { get; set; }
    public DateTimeOffset? PendingTwoFactorExpiresAt { get; set; }
}

public sealed class MilestoneUserProfilePhoto : BaseEntity
{
    public Guid UserId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string SafeFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string ObjectKey { get; set; } = string.Empty;
    public string UploadUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "PendingUpload";
    public string StorageProviderName { get; set; } = string.Empty;
    public string? VerifiedContentType { get; set; }
    public long? UploadedSizeBytes { get; set; }
    public string? Sha256Hash { get; set; }
    public string ScanStatus { get; set; } = "PendingScan";
    public string? ScanProviderName { get; set; }
    public DateTimeOffset? ScanCheckedAt { get; set; }
    public string? ThumbnailObjectKey { get; set; }
    public DateTimeOffset UploadExpiresAt { get; set; }
    public DateTimeOffset? UploadedAt { get; set; }
    public bool IsCurrent { get; set; }
}

public sealed class MilestoneTwoFactorChallenge : BaseEntity
{
    public string ChallengeId { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public int FailedAttempts { get; set; }
}

/// <summary>
/// A single signed session.  The bearer token itself is never persisted; only
/// a hash of its JTI is stored so a user can revoke one device without
/// invalidating every other device.
/// </summary>
public sealed class MilestoneUserSession : BaseEntity
{
    public Guid UserId { get; set; }
    public string TokenIdHash { get; set; } = string.Empty;
    public string DeviceName { get; set; } = "Unknown device";
    public string Browser { get; set; } = "Unknown browser";
    public string? ApproximateLocation { get; set; }
    public string? IpAddressHash { get; set; }
    public DateTimeOffset IssuedAt { get; set; }
    public DateTimeOffset LastUsedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset? TrustedUntil { get; set; }
}

/// <summary>WebAuthn credential metadata and public key material.</summary>
public sealed class MilestonePasskeyCredential : BaseEntity
{
    public Guid UserId { get; set; }
    public string CredentialIdHash { get; set; } = string.Empty;
    public string CredentialId { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public uint SignCount { get; set; }
    public string TransportsJson { get; set; } = "[]";
    public string Label { get; set; } = "Passkey";
    public DateTimeOffset? LastUsedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}

public sealed class MilestonePasskeyChallenge : BaseEntity
{
    public Guid? UserId { get; set; }
    public string ChallengeId { get; set; } = string.Empty;
    public string Challenge { get; set; } = string.Empty;
    public string Purpose { get; set; } = "Registration";
    public string OptionsJson { get; set; } = "{}";
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
}

public sealed class MilestoneProperty : BaseEntity
{
    public Guid HostUserId { get; set; }
    public string HostName { get; set; } = string.Empty;
    public string HostEmail { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal NightlyRate { get; set; }
    public string Currency { get; set; } = "USD";
    public BadgeLevel BadgeLevel { get; set; }
    public bool GuestVerificationEnabled { get; set; }
    public bool InsuraGuestEnabled { get; set; }
    public string CancellationPolicy { get; set; } = string.Empty;
    public string HighlightsJson { get; set; } = "[]";
    public bool IsArchived { get; set; }
    public bool IsDraft { get; set; }
}

public sealed class MilestonePropertyRevision : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Guid HostUserId { get; set; }
    public int Version { get; set; }
    public string SnapshotJson { get; set; } = "{}";
}

public sealed class MilestoneCalendarFeed : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Guid HostUserId { get; set; }
    public string FeedUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "Connected";
    public string? ETag { get; set; }
    public DateTimeOffset? LastModifiedAt { get; set; }
    public DateTimeOffset? NextSyncAt { get; set; }
    public DateTimeOffset? LastSyncAttemptAt { get; set; }
    public DateTimeOffset? LastSyncAt { get; set; }
    public string? LastError { get; set; }
}

public sealed class MilestoneCalendarSyncEvent : BaseEntity
{
    public Guid FeedId { get; set; }
    public Guid PropertyId { get; set; }
    public string Status { get; set; } = "Started";
    public int BlockCount { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public sealed class MilestoneCalendarBlock : BaseEntity
{
    public Guid FeedId { get; set; }
    public Guid PropertyId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public string Summary { get; set; } = "Unavailable";
}

public sealed class MilestonePropertyPhoto : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Guid HostUserId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string SafeFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public int SortOrder { get; set; }
    public string ObjectKey { get; set; } = string.Empty;
    public string UploadUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "PendingUpload";
    public string StorageProviderName { get; set; } = string.Empty;
    public string? VerifiedContentType { get; set; }
    public long? UploadedSizeBytes { get; set; }
    public string? Sha256Hash { get; set; }
    public string ScanStatus { get; set; } = "PendingScan";
    public string? ScanProviderName { get; set; }
    public DateTimeOffset? ScanCheckedAt { get; set; }
    public string? ThumbnailObjectKey { get; set; }
    public DateTimeOffset UploadExpiresAt { get; set; }
    public DateTimeOffset? UploadedAt { get; set; }
}

public sealed class MilestoneBooking : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Guid HostUserId { get; set; }
    public string HostName { get; set; } = string.Empty;
    public string HostEmail { get; set; } = string.Empty;
    public Guid GuestUserId { get; set; }
    public string GuestEmail { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public BookingStatus Status { get; set; }
    public VerificationStatus VerificationStatus { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public bool RequiresGuestVerification { get; set; }
    public DateTimeOffset? HoldExpiresAt { get; set; }
    public int Nights { get; set; }
    public decimal NightlyRate { get; set; }
    public decimal StaySubtotal { get; set; }
    public decimal GuestPlatformFee { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public string? PropertyTitle { get; set; }
    public string? EkycProvider { get; set; }
    public string? EkycTransactionId { get; set; }
    public string? EkycTransactionUrl { get; set; }
    public string? PaymentProvider { get; set; }
    public string? PaymentAuthorizationReference { get; set; }
    public string? PaymentClientSecret { get; set; }
    public string? PaymentCaptureReference { get; set; }
    public string? PaymentRefundReference { get; set; }
    public decimal RefundedAmount { get; set; }
    public string? RefundReason { get; set; }
    public DateTimeOffset? RefundedAt { get; set; }
    public string PriceBreakdownJson { get; set; } = "[]";
    public string NotificationsJson { get; set; } = "[]";
    public string TimelineJson { get; set; } = "[]";
}

public sealed class MilestonePaymentAttempt : BaseEntity
{
    public Guid BookingId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string ProviderReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string FailureReason { get; set; } = string.Empty;
    public DateTimeOffset? CompletedAt { get; set; }
}

public sealed class MilestoneBookingCreationRateLimit : BaseEntity
{
    public Guid GuestUserId { get; set; }
    public DateTimeOffset WindowStartedAt { get; set; }
    public DateTimeOffset WindowEndsAt { get; set; }
    public int RequestCount { get; set; }
    public DateTimeOffset LastRequestAt { get; set; }
}

public sealed class MilestonePricebookEntry : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Cadence { get; set; } = string.Empty;
    public string AppliesTo { get; set; } = string.Empty;
    public bool IsConfigurable { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? ActiveFrom { get; set; }
    public DateTimeOffset? ActiveTo { get; set; }
}

public sealed class MilestoneBadgeDefinition : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public BadgeLevel Level { get; set; }
    public string AppliesTo { get; set; } = string.Empty;
    public string PricebookKey { get; set; } = string.Empty;
    public string UnlocksJson { get; set; } = "[]";
}

public sealed class MilestoneBadgeAssignment : BaseEntity
{
    public Guid BadgeDefinitionId { get; set; }
    public string BadgeKey { get; set; } = string.Empty;
    public BadgeLevel Level { get; set; }
    public string SubjectType { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public BadgeAssignmentStatus Status { get; set; }
    public DateTimeOffset EarnedAt { get; set; }
    public DateTimeOffset PaidThrough { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public decimal AmountCharged { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentStatus PaymentStatus { get; set; }
    public string PaymentReference { get; set; } = string.Empty;
    public string UnlocksJson { get; set; } = "[]";
}

public sealed class MilestoneBadgeRenewal : BaseEntity
{
    public Guid BadgeAssignmentId { get; set; }
    public DateTimeOffset ReminderDueAt { get; set; }
    public DateTimeOffset? PaymentAttemptedAt { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public decimal AmountDue { get; set; }
    public string Currency { get; set; } = "USD";
}

public sealed class MilestoneCampaign : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CampaignType { get; set; } = string.Empty;
    public decimal? OverrideAmount { get; set; }
    public string? AppliesTo { get; set; }
    public DateTimeOffset? OpensAt { get; set; }
    public DateTimeOffset? ClosesAt { get; set; }
    public bool IsActive { get; set; }
}

public sealed class MilestoneCampaignEnrollment : BaseEntity
{
    public string CampaignKey { get; set; } = string.Empty;
    public string SubjectType { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public DateTimeOffset EnrolledAt { get; set; }
}

public sealed class MilestoneFoundingBenefit : BaseEntity
{
    public Guid PropertyId { get; set; }
    public FoundingTier Tier { get; set; }
    public decimal GuestFlatFee { get; set; }
    public decimal HostCommissionPercent { get; set; }
    public bool IsLifetimeGuestFee { get; set; }
    public bool IsTransferableWithProperty { get; set; }
    public bool IsForfeited { get; set; }
}

public sealed class MilestoneWellnessOfficer : BaseEntity
{
    public Guid? UserId { get; set; }
    public string BadgeNumber { get; set; } = string.Empty;
    public string Parish { get; set; } = string.Empty;
    public string CoverageArea { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public decimal? ServiceRadiusKm { get; set; }
    public bool IsActiveOffDuty { get; set; }
    public bool IsRetired { get; set; }
    public string VerificationStatus { get; set; } = "Pending";
    public string OnboardingStatus { get; set; } = "Pending";
    public string AvailabilityStatus { get; set; } = "Inactive";
    public string VerificationMetadataJson { get; set; } = "{}";
    public string AdminReviewMetadataJson { get; set; } = "{}";
    public string FreeBadgesJson { get; set; } = "[]";
    public string NotificationEventsJson { get; set; } = "[]";
}

public sealed class MilestoneWellnessVisit : BaseEntity
{
    public Guid HostUserId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid? OfficerId { get; set; }
    public string OfficerBadgeNumber { get; set; } = string.Empty;
    public string Parish { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string VisitType { get; set; } = string.Empty;
    public DateTimeOffset ScheduledAt { get; set; }
    public string ScheduledTimeZone { get; set; } = "America/Jamaica";
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal OfficerPayoutAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public string PaymentStatus { get; set; } = "Pending";
    public string VisitStatus { get; set; } = "Requested";
    public string ReportStatus { get; set; } = "Missing";
    public string PaymentProvider { get; set; } = string.Empty;
    public string PaymentAuthorizationReference { get; set; } = string.Empty;
    public string PaymentClientSecret { get; set; } = string.Empty;
    public string PaymentCaptureReference { get; set; } = string.Empty;
    public string TimelineJson { get; set; } = "[]";
    public string NotificationEventsJson { get; set; } = "[]";
}

public sealed class MilestoneWellnessReport : BaseEntity
{
    public Guid VisitId { get; set; }
    public Guid OfficerId { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public string ReportStatus { get; set; } = "Submitted";
    public string Notes { get; set; } = string.Empty;
    public string PhotosJson { get; set; } = "[]";
    public string LocationMetadataJson { get; set; } = "{}";
}

public sealed class MilestoneWellnessReportPhoto : BaseEntity
{
    public Guid VisitId { get; set; }
    public Guid OfficerId { get; set; }
    public Guid? ReportId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string SafeFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string ObjectKey { get; set; } = string.Empty;
    public string UploadUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "PendingUpload";
    public string StorageProviderName { get; set; } = string.Empty;
    public string? VerifiedContentType { get; set; }
    public long? UploadedSizeBytes { get; set; }
    public string? Sha256Hash { get; set; }
    public string ScanStatus { get; set; } = "PendingScan";
    public string? ScanProviderName { get; set; }
    public DateTimeOffset? ScanCheckedAt { get; set; }
    public string? ThumbnailObjectKey { get; set; }
    public DateTimeOffset UploadExpiresAt { get; set; }
    public DateTimeOffset? UploadedAt { get; set; }
}

public sealed class MilestoneWellnessOfficerDocument : BaseEntity
{
    public Guid OfficerId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string SafeFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string ObjectKey { get; set; } = string.Empty;
    public string UploadUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "PendingUpload";
    public string StorageProviderName { get; set; } = string.Empty;
    public string ScanStatus { get; set; } = "PendingScan";
    public string? Sha256Hash { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public string ReviewStatus { get; set; } = "Pending";
    public string? ReviewReason { get; set; }
    public DateTimeOffset UploadExpiresAt { get; set; }
    public DateTimeOffset? UploadedAt { get; set; }
}

public sealed class MilestoneWellnessReportTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
    public string DefinitionJson { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
}

public sealed class MilestoneWellnessReportComment : BaseEntity
{
    public Guid ReportId { get; set; }
    public Guid AuthorUserId { get; set; }
    public string Body { get; set; } = string.Empty;
}

public sealed class MilestoneWellnessReportAcknowledgement : BaseEntity
{
    public Guid ReportId { get; set; }
    public Guid AcknowledgedByUserId { get; set; }
    public DateTimeOffset AcknowledgedAt { get; set; }
}

public sealed class MilestoneWellnessFollowUpTask : BaseEntity
{
    public Guid ReportId { get; set; }
    public Guid VisitId { get; set; }
    public Guid PropertyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "Normal";
    public Guid? AssigneeUserId { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public string Status { get; set; } = "Open";
}

public sealed class MilestoneWellnessPayout : BaseEntity
{
    public Guid VisitId { get; set; }
    public Guid OfficerId { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal OfficerAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = "Pending";
    public DateTimeOffset? EligibleAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public string ProviderReference { get; set; } = string.Empty;
    public string LedgerNotes { get; set; } = string.Empty;
}

public sealed class MilestoneWellnessPayoutDispute : BaseEntity
{
    public Guid PayoutId { get; set; }
    public Guid OfficerId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? EvidenceJson { get; set; }
    public string Status { get; set; } = "Open";
    public string? Decision { get; set; }
    public string? DecisionNotes { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
}

public sealed class MilestoneAuthFlow : BaseEntity
{
    public Guid? UserId { get; set; }
    public string FlowType { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string NormalizedDestination { get; set; } = string.Empty;
    public string DestinationHash { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public string SecretSalt { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string DeliveryChannel { get; set; } = string.Empty;
    public string RequestIpHash { get; set; } = string.Empty;
    public int FailedAttempts { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? LastSentAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? InvalidatedAt { get; set; }
    public string MetadataJson { get; set; } = "{}";
}

public sealed class MilestoneRecoveryCode : BaseEntity
{
    public Guid UserId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public string SecretSalt { get; set; } = string.Empty;
    public DateTimeOffset? UsedAt { get; set; }
}

public sealed class MilestonePublicContentPage : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string SectionsJson { get; set; } = "[]";
    public string LinksJson { get; set; } = "[]";
    public bool IsPublished { get; set; } = true;
}

public sealed class MilestoneContactRequest : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
}

public sealed class MilestoneExperience : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Parish { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public int DurationMinutes { get; set; }
    public decimal Rating { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImagesJson { get; set; } = "[]";
    public string IncludedJson { get; set; } = "[]";
    public string RulesJson { get; set; } = "[]";
    public string AvailabilityJson { get; set; } = "[]";
    public bool IsPublished { get; set; } = true;
}

public sealed class MilestoneJournalArticle : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTimeOffset PublishedAt { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string TagsJson { get; set; } = "[]";
    public string RelatedSlugsJson { get; set; } = "[]";
    public bool IsPublished { get; set; } = true;
}

public sealed class MilestoneHostProfile : BaseEntity
{
    public Guid HostUserId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Parish { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string ResponseTime { get; set; } = string.Empty;
    public string BadgesJson { get; set; } = "[]";
    public string ListingIdsJson { get; set; } = "[]";
    public decimal Rating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsPublic { get; set; } = true;
    public string HighlightsJson { get; set; } = "[]";
}

public sealed class MilestoneWishlistCollection : BaseEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public sealed class MilestoneWishlistItem : BaseEntity
{
    public Guid CollectionId { get; set; }
    public Guid UserId { get; set; }
    public Guid PropertyId { get; set; }
    public string PropertyTitle { get; set; } = string.Empty;
    public string Status { get; set; } = "Available";
    public int SortOrder { get; set; }
}

/// <summary>
/// Durable, explainable recommendation inputs.  Interactions are append-only
/// so a dismissed recommendation can be restored without losing the reason it
/// was shown.  The recommendation service never stores a guest's raw search
/// query or sensitive profile data.
/// </summary>
public sealed class MilestoneTravelerRecommendationInteraction : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid PropertyId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class MilestoneTravelerPreference : BaseEntity
{
    public Guid UserId { get; set; }
    public string? PreferredParish { get; set; }
    public decimal? MaximumNightlyRate { get; set; }
    public string? PreferredBadgeLevel { get; set; }
    public string PreferredHighlightsJson { get; set; } = "[]";
}

public sealed class MilestoneHostPayout : BaseEntity
{
    public Guid BookingId { get; set; }
    public Guid HostUserId { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal NetAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = "Pending";
    public DateTimeOffset? EligibleAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public string SettlementReference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class MilestoneTravelerPaymentMethod : BaseEntity
{
    public Guid UserId { get; set; }
    public string ProviderName { get; set; } = "Stripe";
    public string ProviderPaymentMethodReference { get; set; } = string.Empty;
    public string SetupIntentReference { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Last4 { get; set; } = string.Empty;
    public int ExpMonth { get; set; }
    public int ExpYear { get; set; }
    public bool IsDefault { get; set; }
}

public sealed class MilestoneReview : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? BookingId { get; set; }
    public string SubjectTitle { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Status { get; set; } = "Published";
    public string? HostReply { get; set; }
    public DateTimeOffset EditableUntil { get; set; }
}

public sealed class MilestoneTravelerNotification : BaseEntity
{
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string DeepLink { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}

public sealed class MilestoneIdentityDocumentUpload : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? IdentityDocumentId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string SafeFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string ObjectKey { get; set; } = string.Empty;
    public string UploadUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "PendingUpload";
    public string StorageProviderName { get; set; } = string.Empty;
    public string? VerifiedContentType { get; set; }
    public long? UploadedSizeBytes { get; set; }
    public string? Sha256Hash { get; set; }
    public string ScanStatus { get; set; } = "PendingScan";
    public string? ScanProviderName { get; set; }
    public DateTimeOffset? ScanCheckedAt { get; set; }
    public string? ThumbnailObjectKey { get; set; }
    public DateTimeOffset UploadExpiresAt { get; set; }
    public DateTimeOffset? UploadedAt { get; set; }
    public string IssuingCountry { get; set; } = string.Empty;
    public DateOnly? ExpiresOn { get; set; }
}

public sealed class MilestoneConversation : BaseEntity
{
    public string Subject { get; set; } = string.Empty;
    public Guid? BookingId { get; set; }
    public bool IsSupportThread { get; set; }
}

public sealed class MilestoneConversationParticipant : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTimeOffset? LastReadAt { get; set; }
    public string OnlineStatus { get; set; } = "Offline";
}

public sealed class MilestoneMessage : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Guid SenderUserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = "Delivered";
    public DateTimeOffset SentAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
    public string AttachmentsJson { get; set; } = "[]";
}

public sealed class MilestoneMessageAttachment : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Guid? MessageId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string SafeFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string ObjectKey { get; set; } = string.Empty;
    public string UploadUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "PendingUpload";
    public string StorageProviderName { get; set; } = string.Empty;
    public string? VerifiedContentType { get; set; }
    public long? UploadedSizeBytes { get; set; }
    public string? Sha256Hash { get; set; }
    public string ScanStatus { get; set; } = "PendingScan";
    public string? ScanProviderName { get; set; }
    public DateTimeOffset? ScanCheckedAt { get; set; }
    public string? ThumbnailObjectKey { get; set; }
    public DateTimeOffset UploadExpiresAt { get; set; }
    public DateTimeOffset? UploadedAt { get; set; }
    public DateTimeOffset? AttachedAt { get; set; }
}

public sealed class MilestoneDirectoryProvider : BaseEntity
{
    public Guid? OwnerUserId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Parish { get; set; } = string.Empty;
    public string BadgeLevel { get; set; } = "Verified";
    public string Description { get; set; } = string.Empty;
    public string AvailabilitySummary { get; set; } = string.Empty;
    public string ContactMode { get; set; } = "Platform messaging only";
    public decimal Rating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsActive { get; set; } = true;
    public string VerificationStatus { get; set; } = "Pending";
    public string Status { get; set; } = "PendingReview";
    public bool IsBrickAndMortar { get; set; }
    public string? PoliceBadgeNumber { get; set; }
    public string ServicesJson { get; set; } = "[]";
    public string? OpeningHours { get; set; }
    public bool EmergencyAvailable { get; set; }
    public decimal? ServiceRadiusKm { get; set; }
    /// <summary>Structured, provider-managed business information.</summary>
    public string WeeklyHoursJson { get; set; } = "{}";
    public string HolidayClosuresJson { get; set; } = "[]";
    public string PromotionsJson { get; set; } = "[]";
    public string? AccessibilityInfo { get; set; }
}

public sealed class MilestoneDirectoryQuote : BaseEntity
{
    public Guid ProviderId { get; set; }
    public Guid RequesterUserId { get; set; }
    public string Scope { get; set; } = string.Empty;
    public DateTimeOffset? PreferredAt { get; set; }
    public decimal? Budget { get; set; }
    public decimal? ResponseAmount { get; set; }
    public string Status { get; set; } = "OPEN";
    public string? Message { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }
}

public sealed class MilestoneDirectoryReview : BaseEntity
{
    public Guid ProviderId { get; set; }
    public Guid ReviewerUserId { get; set; }
    public int Rating { get; set; }
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = "PUBLISHED";
    public string? ProviderResponse { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }
}

/// <summary>Privacy-scoped provider history for signed-in directory users.</summary>
public sealed class MilestoneDirectoryRecentView : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ProviderId { get; set; }
    public DateTimeOffset ViewedAt { get; set; }
}

/// <summary>
/// Moderation-safe provider documents.  The binary is stored through the
/// configured IStorageProvider; this row keeps only scoped metadata and the
/// verification/scan state needed by the provider and admin workflows.
/// </summary>
public sealed class MilestoneDirectoryProviderDocument : BaseEntity
{
    public Guid ProviderId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string DocumentType { get; set; } = "BUSINESS_DOCUMENT";
    public string OriginalFileName { get; set; } = string.Empty;
    public string SafeFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string ObjectKey { get; set; } = string.Empty;
    public string UploadUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "PendingUpload";
    public string StorageProviderName { get; set; } = string.Empty;
    public string? VerifiedContentType { get; set; }
    public long? UploadedSizeBytes { get; set; }
    public string? Sha256Hash { get; set; }
    public string ScanStatus { get; set; } = "PendingScan";
    public string? ScanProviderName { get; set; }
    public DateTimeOffset? ScanCheckedAt { get; set; }
    public DateTimeOffset UploadExpiresAt { get; set; }
    public DateTimeOffset? UploadedAt { get; set; }
}

public sealed class MilestoneWellnessSubscription : BaseEntity
{
    public Guid HostUserId { get; set; }
    public string PlanKey { get; set; } = "wellness-monthly";
    public decimal MonthlyAmount { get; set; } = 19m;
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = "Active";
    public DateTimeOffset CurrentPeriodStart { get; set; }
    public DateTimeOffset CurrentPeriodEnd { get; set; }
    public int IncludedVisits { get; set; } = 1;
    public int UsedVisits { get; set; }
    public string PaymentProvider { get; set; } = string.Empty;
    public string PaymentReference { get; set; } = string.Empty;
    public DateTimeOffset? CancelledAt { get; set; }
}

public sealed class MilestoneHostPricingRule : BaseEntity
{
    public Guid HostUserId { get; set; }
    public Guid PropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public decimal NightlyRate { get; set; }
    public int MinimumStay { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class MilestoneHostPromotion : BaseEntity
{
    public Guid HostUserId { get; set; }
    public Guid PropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DiscountPercent { get; set; }
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public int MinimumNights { get; set; }
    public string BadgeLevel { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class MilestoneAdminCase : BaseEntity
{
    public string CaseType { get; set; } = string.Empty;
    public string SubjectType { get; set; } = string.Empty;
    public Guid? SubjectId { get; set; }
    public string Status { get; set; } = "Open";
    public string Priority { get; set; } = "Normal";
    public string Reason { get; set; } = string.Empty;
    public string AssignedTo { get; set; } = string.Empty;
    public string ResolutionNotes { get; set; } = string.Empty;
    public DateTimeOffset? ResolvedAt { get; set; }
}

public sealed class MilestoneAdminCaseEvidence : BaseEntity
{
    public Guid CaseId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string SafeFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string ObjectKey { get; set; } = string.Empty;
    public string UploadUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "PendingUpload";
    public string StorageProviderName { get; set; } = string.Empty;
    public string? VerifiedContentType { get; set; }
    public long? UploadedSizeBytes { get; set; }
    public string? Sha256Hash { get; set; }
    public string ScanStatus { get; set; } = "PendingScan";
    public string? ScanProviderName { get; set; }
    public DateTimeOffset? ScanCheckedAt { get; set; }
    public string? ThumbnailObjectKey { get; set; }
    public DateTimeOffset UploadExpiresAt { get; set; }
    public DateTimeOffset? UploadedAt { get; set; }
}

public sealed class MilestoneAuditEvent : BaseEntity
{
    public Guid? ManagerUserId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string ActorRole { get; set; } = "System";
    public string Action { get; set; } = string.Empty;
    public string SubjectType { get; set; } = string.Empty;
    public Guid? SubjectId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = "{}";
}

// Phase 5 Property Manager aggregate.  These records deliberately keep the
// manager/owner scope on every row so authorization can be enforced server
// side without relying on client supplied filters.
public sealed class MilestonePropertyManager : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string SubscriptionTier { get; set; } = "Portfolio";
    public decimal MonthlyAmount { get; set; }
    public string SubscriptionStatus { get; set; } = "Active";
    public DateTimeOffset NextBillingAt { get; set; }
    public string? PendingSubscriptionTier { get; set; }
    public DateTimeOffset? PendingSubscriptionEffectiveAt { get; set; }
    public bool AutoRenew { get; set; } = true;
    public string BillingProviderStatus { get; set; } = "LOCAL_TEST";
    public string? CancellationReason { get; set; }
}

public sealed class MilestoneManagerOwner : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = "PENDING";
    public string InvitationStatus { get; set; } = "INVITED";
    public Guid? CommunityId { get; set; }
}

public sealed class MilestoneManagerProperty : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid? CommunityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Status { get; set; } = "ACTIVE";
    public string OccupancyStatus { get; set; } = "VACANT";
    /// <summary>Optional link to the M1/M2 rental listing used for bookings and availability.</summary>
    public Guid? RentalListingId { get; set; }
    public DateTimeOffset? RentalListingLinkedAt { get; set; }
}

public sealed class MilestoneManagerPropertyAssignmentHistory : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid? PreviousOwnerUserId { get; set; }
    public Guid NewOwnerUserId { get; set; }
    public Guid ActorUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid BatchId { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
}

public sealed class MilestoneManagerInvoice : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid? PropertyId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateOnly IssueDate { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = "ISSUED";
}

public sealed class MilestoneManagerInvoiceLine : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitAmount { get; set; }
    public decimal Amount { get; set; }
}

public sealed class MilestoneManagerPayment : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Provider { get; set; } = "Local/Stripe";
    public string ProviderReference { get; set; } = string.Empty;
    public string Status { get; set; } = "CAPTURED";
    public string? SavedPaymentMethodReference { get; set; }
    public decimal RefundedAmount { get; set; }
    public string? RefundReason { get; set; }
    public DateTimeOffset? RefundedAt { get; set; }
    public string ReconciliationStatus { get; set; } = "PENDING";
    public string? ReconciliationReference { get; set; }
    public int AttemptNumber { get; set; } = 1;
}

public sealed class MilestoneManagerPaymentMethod : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Provider { get; set; } = "Local/Stripe";
    public string ProviderReference { get; set; } = string.Empty;
    public string Brand { get; set; } = "card";
    public string Last4 { get; set; } = string.Empty;
    public int ExpMonth { get; set; }
    public int ExpYear { get; set; }
    public bool IsDefault { get; set; }
}

public sealed class MilestoneManagerPaymentAttempt : BaseEntity
{
    public Guid PaymentId { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid ManagerUserId { get; set; }
    public string Status { get; set; } = "PENDING";
    public string? FailureReason { get; set; }
    public string? ProviderReference { get; set; }
    public int AttemptNumber { get; set; }
    public string? IdempotencyKey { get; set; }
}

public sealed class MilestoneManagerLedgerEntry : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? InvoiceId { get; set; }
    public string EntryType { get; set; } = "CHARGE";
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly OccurredOn { get; set; }
}

public sealed class MilestoneManagerUtilityCharge : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid PropertyId { get; set; }
    public string UtilityType { get; set; } = string.Empty;
    public string BillingPeriod { get; set; } = string.Empty;
    public decimal Usage { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public Guid? InvoiceId { get; set; }
    public string Status { get; set; } = "ALLOCATED";
}

public sealed class MilestoneManagerMeterReading : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid PropertyId { get; set; }
    public string UtilityType { get; set; } = string.Empty;
    public string BillingPeriod { get; set; } = string.Empty;
    public decimal PreviousReading { get; set; }
    public decimal CurrentReading { get; set; }
    public decimal Usage { get; set; }
    public string? PhotoObjectKey { get; set; }
    public string? BillObjectKey { get; set; }
    public string ReadingHash { get; set; } = string.Empty;
    public bool IsAnomaly { get; set; }
    public string Status { get; set; } = "RECORDED";
}

public sealed class MilestoneManagerUtilitySchedule : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid PropertyId { get; set; }
    public string UtilityType { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public int DayOfMonth { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastRunAt { get; set; }
}

public sealed class MilestoneManagerUtilityDispute : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid UtilityChargeId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? EvidenceObjectKey { get; set; }
    public string Status { get; set; } = "OPEN";
    public string? Decision { get; set; }
    public decimal AdjustmentAmount { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
}

public sealed class MilestoneManagerVendor : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string Contact { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = "PENDING";
    public bool IsActive { get; set; } = true;
    public string Notes { get; set; } = string.Empty;
    public string ServiceAreasJson { get; set; } = "[]";
    public string AvailabilityJson { get; set; } = "{}";
    public decimal? Rate { get; set; }
    public decimal Rating { get; set; }
    public bool IsPreferred { get; set; }
    public bool IsSuspended { get; set; }
    public int CompletedJobCount { get; set; }
    public decimal SpendTotal { get; set; }
}

public sealed class MilestoneManagerVendorDocument : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid VendorId { get; set; }
    public string DocumentType { get; set; } = "LICENSE";
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public string StorageKey { get; set; } = string.Empty;
    public DateOnly? ExpiresOn { get; set; }
    public string Status { get; set; } = "ACTIVE";
}

public sealed class MilestoneManagerMaintenance : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid? VendorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string Urgency { get; set; } = "NORMAL";
    public string Status { get; set; } = "OPEN";
    public DateTimeOffset? ScheduledAt { get; set; }
    public decimal Cost { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTimeOffset? SlaDueAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
}

public sealed class MilestoneManagerMaintenanceActivity : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid MaintenanceId { get; set; }
    public Guid ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

public sealed class MilestoneManagerMaintenanceAttachment : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid MaintenanceId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string Status { get; set; } = "UPLOADED";
}

public sealed class MilestoneManagerNotice : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid? CommunityId { get; set; }
    public Guid? TargetOwnerUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset PublishAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool IsPinned { get; set; }
    public bool IsArchived { get; set; }
    public string Category { get; set; } = "GENERAL";
    public string AudienceRolesJson { get; set; } = "[]";
    public string AudienceOwnerIdsJson { get; set; } = "[]";
    public DateTimeOffset? AcknowledgementDueAt { get; set; }
}

public sealed class MilestoneManagerNoticeComment : BaseEntity
{
    public Guid NoticeId { get; set; }
    public Guid AuthorUserId { get; set; }
    public string Body { get; set; } = string.Empty;
}

public sealed class MilestoneManagerNoticeAcknowledgement : BaseEntity
{
    public Guid NoticeId { get; set; }
    public Guid OwnerUserId { get; set; }
    public DateTimeOffset AcknowledgedAt { get; set; }
}

public sealed class MilestoneManagerProposal : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid? CommunityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset OpensAt { get; set; }
    public DateTimeOffset ClosesAt { get; set; }
    public string Status { get; set; } = "OPEN";
    public bool IsAnonymous { get; set; }
    public int? Quorum { get; set; }
    public string ResultJson { get; set; } = "{}";
    public DateTimeOffset? ClosedAt { get; set; }
    public string? ResultProofHash { get; set; }
}

public sealed class MilestoneManagerProposalAttachment : BaseEntity
{
    public Guid ProposalId { get; set; }
    public Guid ManagerUserId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public string StorageKey { get; set; } = string.Empty;
}

public sealed class MilestoneManagerProposalDiscussion : BaseEntity
{
    public Guid ProposalId { get; set; }
    public Guid AuthorUserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsModerated { get; set; }
}

public sealed class MilestoneManagerEligibleVoter : BaseEntity
{
    public Guid ProposalId { get; set; }
    public Guid OwnerUserId { get; set; }
    public decimal Weight { get; set; } = 1m;
    public bool HasVoted { get; set; }
}

public sealed class MilestoneManagerVote : BaseEntity
{
    public Guid ProposalId { get; set; }
    public string BallotHash { get; set; } = string.Empty;
    public string Choice { get; set; } = "ABSTAIN";
    public bool CastByProxy { get; set; }
    public Guid? ProxyId { get; set; }
}

public sealed class MilestoneManagerProxy : BaseEntity
{
    public Guid ProposalId { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid ProxyUserId { get; set; }
    public string Status { get; set; } = "PENDING";
    public DateTimeOffset ValidUntil { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
}

public sealed class MilestoneManagerDocument : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid? OwnerUserId { get; set; }
    public Guid? PropertyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "COMMUNITY";
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public long SizeBytes { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string AccessScope { get; set; } = "OWNER";
    public bool IsArchived { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public int CurrentVersion { get; set; } = 1;
}

public sealed class MilestoneManagerDocumentVersion : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Guid ManagerUserId { get; set; }
    public int Version { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public long SizeBytes { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public new Guid CreatedByUserId { get; set; }
}

public sealed class MilestoneManagerDocumentAccessEvent : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Guid ActorUserId { get; set; }
    public string Action { get; set; } = "VIEW";
}

public sealed class MilestoneManagerDocumentExport : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public string DocumentIdsJson { get; set; } = "[]";
    public string Status { get; set; } = "QUEUED";
    public string? ObjectKey { get; set; }
    public string? FileName { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}

public sealed class MilestoneManagerGateMessage : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid? CommunityId { get; set; }
    public Guid? PropertyId { get; set; }
    public string Recipient { get; set; } = "GATE";
    public string Message { get; set; } = string.Empty;
    public string VisitorType { get; set; } = "VISITOR";
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset ValidUntil { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed class MilestoneManagerGateDeliveryAttempt : BaseEntity
{
    public Guid GateMessageId { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Status { get; set; } = "QUEUED";
    public string? ProviderReference { get; set; }
    public int AttemptNumber { get; set; }
    public string? FailureReason { get; set; }
}

public sealed class MilestoneManagerQrAccess : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid? OwnerUserId { get; set; }
    public Guid? PropertyId { get; set; }
    public string SubjectType { get; set; } = "OWNER";
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset ValidUntil { get; set; }
    public bool IsRevoked { get; set; }
    public string? RevokeReason { get; set; }
    public DateTimeOffset? LastValidatedAt { get; set; }
    public int ValidationCount { get; set; }
}

public sealed class MilestoneManagerQrScan : BaseEntity
{
    public Guid QrAccessId { get; set; }
    public Guid? GateGuardUserId { get; set; }
    public Guid? PropertyId { get; set; }
    public string Result { get; set; } = string.Empty;
    public DateTimeOffset ScannedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class MilestoneManagerSubscriptionEvent : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string FromTier { get; set; } = string.Empty;
    public string ToTier { get; set; } = string.Empty;
    public string Status { get; set; } = "COMPLETED";
    public string? Reason { get; set; }
    public DateTimeOffset EffectiveAt { get; set; }
}

public sealed class MilestoneManagerInvitationEvent : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? ProviderReference { get; set; }
}

public sealed class MilestoneManagerOwnerVerification : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Requirement { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING";
    public string? Reason { get; set; }
    public string? DocumentKey { get; set; }
    public Guid ActorUserId { get; set; }
}

public sealed class MilestoneManagerDashboardPreference : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public string KpiOrderJson { get; set; } = "[]";
    public string VisibleKpisJson { get; set; } = "[]";
    public string SavedFiltersJson { get; set; } = "{}";
    public string SavedViewsJson { get; set; } = "[]";
}

public sealed class MilestoneManagementAgreement : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid? PropertyId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string FeeRuleJson { get; set; } = "{}";
    public decimal MaintenanceApprovalLimit { get; set; }
    public decimal ExpenseApprovalLimit { get; set; }
    public string? SignedDocumentKey { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public int Version { get; set; } = 1;
}

public sealed class MilestoneManagementFeeRule : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid? PropertyId { get; set; }
    public string RuleType { get; set; } = "PERCENTAGE";
    public decimal Percentage { get; set; }
    public decimal FixedAmount { get; set; }
    public decimal CleaningMarkup { get; set; }
    public decimal MaintenanceMarkup { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
}

public sealed class MilestoneOwnerPayout : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid OwnerUserId { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "PAYABLE";
    public string? ProviderReference { get; set; }
    public string? FailureReason { get; set; }
}

public sealed class MilestoneOwnerApproval : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid? PropertyId { get; set; }
    public string ApprovalType { get; set; } = "MAINTENANCE";
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Limit { get; set; }
    public string Status { get; set; } = "PENDING";
    public string? DecisionReason { get; set; }
    public Guid? DecidedByUserId { get; set; }
}

public sealed class MilestoneManagerStaff : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid StaffUserId { get; set; }
    public string Role { get; set; } = "ASSISTANT";
    public string PropertyScopeJson { get; set; } = "[]";
    public string OwnerScopeJson { get; set; } = "[]";
    public bool CanManageFinance { get; set; }
    public decimal ApprovalLimit { get; set; }
    public string Status { get; set; } = "INVITED";
}

public sealed class MilestoneManagerCalendarEvent : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? OwnerUserId { get; set; }
    public string EventType { get; set; } = "BOOKING";
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public string Status { get; set; } = "CONFIRMED";
    public string SourceType { get; set; } = "MANUAL";
    public Guid? SourceId { get; set; }
}

public sealed class MilestoneWorkOrder : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid? VendorId { get; set; }
    public string WorkOrderNumber { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string Status { get; set; } = "REQUEST";
    public decimal? QuoteAmount { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public decimal LaborAmount { get; set; }
    public decimal PartsAmount { get; set; }
    public decimal OtherAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public decimal OwnerResponsibility { get; set; }
    public decimal ManagerResponsibility { get; set; }
    public decimal VendorResponsibility { get; set; }
    public string Currency { get; set; } = "JMD";
    public DateTimeOffset? SlaDueAt { get; set; }
    public DateTimeOffset? ScheduledAt { get; set; }
    public Guid? OwnerApprovalId { get; set; }
    public Guid? SelectedQuoteId { get; set; }
    public string PostingStatus { get; set; } = "UNPOSTED";
    public Guid? FinancialJournalId { get; set; }
    public Guid? FinancialReversalJournalId { get; set; }
    public Guid? ReplacementFinancialJournalId { get; set; }
    public int CorrectionCount { get; set; }
    public Guid? SourceInspectionId { get; set; }
    public long RowVersion { get; set; } = 1;
}

public sealed class MilestoneCleaningTask : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid PropertyId { get; set; }
    public string Status { get; set; } = "OPEN";
    public DateTimeOffset DueAt { get; set; }
    public Guid? AssignedStaffUserId { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class MilestoneInspection : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid PropertyId { get; set; }
    public string Status { get; set; } = "INSPECTION_REQUIRED";
    public string ChecklistJson { get; set; } = "[]";
    public string? IssuesJson { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
