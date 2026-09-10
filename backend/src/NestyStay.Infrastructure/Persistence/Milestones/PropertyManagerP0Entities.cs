using NestyStay.Domain.Common;

namespace NestyStay.Infrastructure.Persistence.Milestones;

/// <summary>
/// P0 property-manager authority and accounting records. These records are
/// additive to the milestone tables so existing M1-M4 contracts keep their
/// response shapes while the manager workspace gains an auditable ledger.
/// </summary>
public sealed class MilestoneP0OwnerProfile : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public string LegalName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string BillingAddress { get; set; } = string.Empty;
    public string PreferredCurrency { get; set; } = "JMD";
    public string TimeZone { get; set; } = "America/Jamaica";
    /// <summary>Non-sensitive billing preferences and invoice metadata.</summary>
    public string BillingMetadataJson { get; set; } = "{}";
    /// <summary>Provider customer reference only; card/bank data stays with the provider.</summary>
    public string? PaymentProviderCustomerReference { get; set; }
    public string OperationalMetadataJson { get; set; } = "{}";
    public string Notes { get; set; } = string.Empty;
    public DateTimeOffset? ActivatedAt { get; set; }
    public DateTimeOffset? SuspendedAt { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public long Version { get; set; } = 1;
}

public sealed class MilestoneP0OwnerLifecycleEvent : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid ActorUserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = "{}";
}

public sealed class MilestoneP0ManagementAgreement : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid? PropertyId { get; set; }
    public int Version { get; set; } = 1;
    public Guid? SupersedesAgreementId { get; set; }
    public string Status { get; set; } = "DRAFT";
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string Currency { get; set; } = "JMD";
    public string TermsJson { get; set; } = "{}";
    public string FeeRuleJson { get; set; } = "[]";
    public decimal MaintenanceApprovalLimit { get; set; }
    public decimal ExpenseApprovalLimit { get; set; }
    public Guid? DocumentId { get; set; }
    public string? DocumentKey { get; set; }
    public string? DocumentHash { get; set; }
    public DateTimeOffset? ActivatedAt { get; set; }
    public DateTimeOffset? TerminatedAt { get; set; }
    public string? TerminationReason { get; set; }
    public long RowVersion { get; set; } = 1;
}

public sealed class MilestoneP0ManagementFeeRule : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid? PropertyId { get; set; }
    public string Category { get; set; } = "MANAGEMENT";
    public string RuleType { get; set; } = "PERCENTAGE";
    public string CalculationBasis { get; set; } = "COLLECTED_RENT";
    public string Currency { get; set; } = "JMD";
    public decimal Percentage { get; set; }
    public decimal FixedAmount { get; set; }
    public decimal MinimumAmount { get; set; }
    public decimal CleaningMarkup { get; set; }
    public decimal MaintenanceMarkup { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public long RowVersion { get; set; } = 1;
}

public sealed class MilestoneP0Account : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid? OwnerUserId { get; set; }
    public Guid? PropertyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AccountType { get; set; } = "ASSET";
    public string Currency { get; set; } = "JMD";
    public bool IsClientMoney { get; set; }
    public bool IsPmMoney { get; set; }
    public bool IsThirdParty { get; set; }
    public string Status { get; set; } = "ACTIVE";
}

public sealed class MilestoneP0Journal : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public string JournalNumber { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public Guid? SourceId { get; set; }
    public string? IdempotencyKey { get; set; }
    public string Currency { get; set; } = "JMD";
    public DateOnly AccountingDate { get; set; }
    public string Memo { get; set; } = string.Empty;
    public string Status { get; set; } = "POSTED";
    public string ReconciliationStatus { get; set; } = "UNRECONCILED";
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public Guid? ReversalOfJournalId { get; set; }
    public Guid? PostedByUserId { get; set; }
    public DateTimeOffset? PostedAt { get; set; }
}

public sealed class MilestoneP0JournalLine : BaseEntity
{
    public Guid JournalId { get; set; }
    public Guid AccountId { get; set; }
    public Guid? OwnerUserId { get; set; }
    public Guid? PropertyId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string Description { get; set; } = string.Empty;
    public string SourceReference { get; set; } = string.Empty;
}

public sealed class MilestoneP0Reconciliation : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid JournalId { get; set; }
    public string ExternalReference { get; set; } = string.Empty;
    public string Status { get; set; } = "MATCHED";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "JMD";
    public string Reason { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public DateTimeOffset ReconciledAt { get; set; }
}

public sealed class MilestoneP0StatementSnapshot : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid? PropertyId { get; set; }
    public string Currency { get; set; } = "JMD";
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public string Status { get; set; } = "FINAL";
    public decimal OpeningBalance { get; set; }
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal ManagementFees { get; set; }
    public decimal Payouts { get; set; }
    public decimal ClosingBalance { get; set; }
    public string? IdempotencyKey { get; set; }
    public string SnapshotJson { get; set; } = "{}";
    public string ContentHash { get; set; } = string.Empty;
    public Guid FinalizedByUserId { get; set; }
    public DateTimeOffset FinalizedAt { get; set; }
    public long RowVersion { get; set; } = 1;
}

public sealed class MilestoneP0PayoutBatch : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Currency { get; set; } = "JMD";
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public decimal Amount { get; set; }
    public decimal ReservedAmount { get; set; }
    public string Status { get; set; } = "DRAFT";
    public string? IdempotencyKey { get; set; }
    public string? ProviderReference { get; set; }
    public string? FailureReason { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public Guid? StatementSnapshotId { get; set; }
    public long RowVersion { get; set; } = 1;
}

public sealed class MilestoneP0PayoutItem : BaseEntity
{
    public Guid BatchId { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid? PropertyId { get; set; }
    public decimal Amount { get; set; }
    public Guid? StatementSnapshotId { get; set; }
    public string SourceJson { get; set; } = "{}";
}

public sealed class MilestoneP0PayoutEvent : BaseEntity
{
    public Guid BatchId { get; set; }
    public Guid ActorUserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? ProviderReference { get; set; }
}

public sealed class MilestoneP0Approval : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? AgreementId { get; set; }
    public Guid? FeeRuleId { get; set; }
    public string ApprovalType { get; set; } = "EXPENSE";
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Threshold { get; set; }
    public string Currency { get; set; } = "JMD";
    public string Status { get; set; } = "REQUIRED";
    public string EvidenceJson { get; set; } = "[]";
    public string? DecisionReason { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public long RowVersion { get; set; } = 1;
}

public sealed class MilestoneP0ApprovalEvent : BaseEntity
{
    public Guid ApprovalId { get; set; }
    public Guid ActorUserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string EvidenceJson { get; set; } = "[]";
}

public sealed class MilestoneP0StaffMembership : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid StaffUserId { get; set; }
    public string Role { get; set; } = "OPERATIONS";
    public string PropertyScopeJson { get; set; } = "[]";
    public string OwnerScopeJson { get; set; } = "[]";
    public bool CanManageFinance { get; set; }
    public bool CanApprovePayouts { get; set; }
    public decimal ApprovalLimit { get; set; }
    public string Status { get; set; } = "INVITED";
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset? SuspendedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public long RowVersion { get; set; } = 1;
}

public sealed class MilestoneP0StaffEvent : BaseEntity
{
    public Guid MembershipId { get; set; }
    public Guid ActorUserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = "{}";
}
