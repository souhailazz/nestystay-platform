using NestyStay.Domain.Common;

namespace NestyStay.Domain.Insurance;

public static class InsurancePolicyStatuses
{
    public const string PlanSelected = "PLAN_SELECTED";
    public const string Pending = "PENDING";
    public const string Active = "ACTIVE";
    public const string Failed = "FAILED";
    public const string Cancelled = "CANCELLED";
    public const string RenewalDue = "RENEWAL_DUE";
}

public static class InsuranceClaimStatuses
{
    public const string Draft = "DRAFT";
    public const string Submitted = "SUBMITTED";
    public const string UnderReview = "UNDER_REVIEW";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
    public const string Closed = "CLOSED";
}

public sealed class InsurancePolicy : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Guid HostUserId { get; set; }
    public string Provider { get; set; } = "InsuraGuest";
    public string PlanCode { get; set; } = string.Empty;
    public string Market { get; set; } = "NON_US";
    public decimal MonthlyAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal PropertyDamageCoverage { get; set; }
    public decimal AccidentalMedicalCoverage { get; set; }
    public string Status { get; set; } = InsurancePolicyStatuses.Pending;
    public string? ProviderReference { get; set; }
    public string? LastIdempotencyKey { get; set; }
    public DateTimeOffset? EffectiveAt { get; set; }
    public DateTimeOffset? RenewsAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset? LastProviderEventAt { get; set; }
    public string? FailureReason { get; set; }
}

public sealed class InsurancePolicyEvent : BaseEntity
{
    public Guid PolicyId { get; set; }
    public Guid PropertyId { get; set; }
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
}

public sealed class InsuranceClaim : BaseEntity
{
    public Guid PolicyId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid HostUserId { get; set; }
    public Guid? BookingId { get; set; }
    public DateTimeOffset IncidentAt { get; set; }
    public string Description { get; set; } = string.Empty;
    public string EvidenceJson { get; set; } = "[]";
    public string Status { get; set; } = InsuranceClaimStatuses.Submitted;
    public string? ProviderReference { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
}
