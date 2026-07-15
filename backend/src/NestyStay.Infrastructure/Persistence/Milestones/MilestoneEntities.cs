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
}

public sealed class MilestoneTwoFactorChallenge : BaseEntity
{
    public string ChallengeId { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
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
