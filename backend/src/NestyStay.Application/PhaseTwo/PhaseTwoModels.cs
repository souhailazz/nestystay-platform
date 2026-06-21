using NestyStay.Domain;

namespace NestyStay.Application.PhaseTwo;

public sealed record PhaseTwoPricebookItemDto(
    string Key,
    string Label,
    decimal Amount,
    string Currency,
    string Cadence,
    string AppliesTo,
    bool IsConfigurable,
    bool IsActive,
    DateTimeOffset? ActiveFrom,
    DateTimeOffset? ActiveTo);

public sealed record UpdatePricebookItemRequest(
    decimal Amount,
    string? Currency = null,
    string? Cadence = null,
    DateTimeOffset? ActiveFrom = null,
    DateTimeOffset? ActiveTo = null,
    bool? IsActive = null);

public sealed record BadgeDefinitionDto(
    Guid Id,
    string Key,
    BadgeLevel Level,
    string AppliesTo,
    decimal AnnualPrice,
    string Currency,
    IReadOnlyList<string> Unlocks);

public sealed record PurchaseBadgeRequest(
    string SubjectType,
    Guid SubjectId,
    BadgeLevel Level,
    string? CampaignKey = null,
    bool HostVerificationPassed = false,
    int CompletedApprovedBookings = 0,
    bool HasPropertyAddress = false,
    bool HasWellnessSubscription = false,
    bool PaymentSucceeded = true);

public sealed record BadgeAssignmentDto(
    Guid Id,
    string BadgeKey,
    BadgeLevel Level,
    string SubjectType,
    Guid SubjectId,
    string Status,
    DateTimeOffset EarnedAt,
    DateTimeOffset PaidThrough,
    DateTimeOffset ExpiresAt,
    decimal AmountCharged,
    string Currency,
    string PaymentStatus,
    string PaymentReference,
    IReadOnlyList<string> Unlocks);

public sealed record BadgeRenewalDto(
    Guid Id,
    Guid BadgeAssignmentId,
    DateTimeOffset ReminderDueAt,
    DateTimeOffset? PaymentAttemptedAt,
    string PaymentStatus,
    decimal AmountDue,
    string Currency);

public sealed record CreateCampaignRequest(
    string Key,
    string Name,
    string CampaignType,
    decimal? OverrideAmount = null,
    string? AppliesTo = null,
    DateTimeOffset? OpensAt = null,
    DateTimeOffset? ClosesAt = null,
    bool IsActive = true);

public sealed record CampaignDto(
    Guid Id,
    string Key,
    string Name,
    string CampaignType,
    decimal? OverrideAmount,
    string? AppliesTo,
    DateTimeOffset? OpensAt,
    DateTimeOffset? ClosesAt,
    bool IsActive);

public sealed record EnrollCampaignRequest(string SubjectType, Guid SubjectId);

public sealed record CampaignEnrollmentDto(Guid Id, string CampaignKey, string SubjectType, Guid SubjectId, DateTimeOffset EnrolledAt);

public sealed record BadgeEligibilityDto(BadgeLevel Level, bool Eligible, IReadOnlyList<string> MissingRequirements);

public sealed record BadgeFeatureAccessDto(
    string SubjectType,
    Guid SubjectId,
    BadgeLevel ActiveLevel,
    IReadOnlyList<string> UnlockedFeatures,
    IReadOnlyList<string> LockedFeatures);

public sealed record FoundingBenefitRequest(Guid PropertyId, FoundingTier Tier, bool IsEligible = true);

public sealed record FoundingBenefitDto(
    Guid PropertyId,
    FoundingTier Tier,
    decimal GuestFlatFee,
    decimal HostCommissionPercent,
    bool IsLifetimeGuestFee,
    bool IsTransferableWithProperty,
    bool IsForfeited);

public sealed record FoundingTransferEvaluationRequest(
    bool PreviousOwnerVerified,
    bool PreviousOwnerTrusted,
    bool HasPropertyId,
    bool HasCurrentTaxReceipt);

public sealed record FoundingTransferEvaluationDto(bool CanTransfer, IReadOnlyList<string> MissingRequirements);

public sealed record CommissionQuoteRequest(
    decimal BookingValue,
    int Nights,
    FoundingTier Tier = FoundingTier.Standard);

public sealed record CommissionQuoteDto(
    decimal BookingValue,
    int Nights,
    decimal HostCommissionPercent,
    decimal HostCommissionAmount,
    decimal GuestFeeAmount,
    string GuestFeeDescription,
    decimal NestyStayRevenue);
