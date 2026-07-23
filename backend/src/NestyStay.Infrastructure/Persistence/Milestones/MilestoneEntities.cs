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
    public string RolesJson { get; set; } = "[]";
    public int FailedLoginAttempts { get; set; }
    public DateTimeOffset? LockoutEndsAt { get; set; }
    public DateTimeOffset? SessionInvalidatedAt { get; set; }
    public long? LastAcceptedTotpCounter { get; set; }
}

public sealed class MilestoneTwoFactorChallenge : BaseEntity
{
    public string ChallengeId { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public int FailedAttempts { get; set; }
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
    public string PriceBreakdownJson { get; set; } = "[]";
    public string NotificationsJson { get; set; } = "[]";
    public string TimelineJson { get; set; } = "[]";
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

public sealed class MilestoneTravelerPaymentMethod : BaseEntity
{
    public Guid UserId { get; set; }
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

public sealed class MilestoneDirectoryProvider : BaseEntity
{
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

public sealed class MilestoneAuditEvent : BaseEntity
{
    public Guid? ActorUserId { get; set; }
    public string ActorRole { get; set; } = "System";
    public string Action { get; set; } = string.Empty;
    public string SubjectType { get; set; } = string.Empty;
    public Guid? SubjectId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = "{}";
}
