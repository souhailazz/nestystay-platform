using NestyStay.Domain.Common;

namespace NestyStay.Domain.Badges;

public sealed class BadgeDefinition : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public BadgeLevel Level { get; set; }
    public string AppliesTo { get; set; } = string.Empty;
    public string UnlocksJson { get; set; } = "[]";
}

public sealed class BadgeAssignment : BaseEntity
{
    public Guid BadgeDefinitionId { get; set; }
    public string SubjectType { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public BadgeAssignmentStatus Status { get; set; } = BadgeAssignmentStatus.Active;
    public DateTimeOffset? EarnedAt { get; set; }
    public DateTimeOffset? PaidThrough { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}

public sealed class BadgeRenewal : BaseEntity
{
    public Guid BadgeAssignmentId { get; set; }
    public DateTimeOffset ReminderDueAt { get; set; }
    public DateTimeOffset? PaymentAttemptedAt { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
}

public sealed class RatingPolicy : BaseEntity
{
    public string SubjectType { get; set; } = string.Empty;
    public decimal MinimumReviewsBeforeEnforcement { get; set; }
    public decimal TopRatedMinimum { get; set; }
    public decimal GoodStandingMinimum { get; set; }
    public decimal WarningMinimum { get; set; }
    public decimal FinalWarningMinimum { get; set; }
}
