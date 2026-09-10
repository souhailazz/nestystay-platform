using NestyStay.Domain.Common;

namespace NestyStay.Infrastructure.Persistence.Milestones;

/// <summary>Operational M5 records.  These are append-friendly records and never replace the P0 accounting journal.</summary>
public sealed class MilestonePmOwnerBlock : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid PropertyId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public string TimeZone { get; set; } = "America/Jamaica";
    public string Category { get; set; } = "OWNER_STAY";
    public string Reason { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Status { get; set; } = "ACTIVE";
    public Guid? BookingId { get; set; }
    public long RowVersion { get; set; } = 1;
}

public sealed class MilestonePmReservationNote : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid BookingId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid AuthorUserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public string Visibility { get; set; } = "INTERNAL";
}

public sealed class MilestonePmMaintenanceCase : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid? VendorId { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "REQUESTED";
    public string Priority { get; set; } = "NORMAL";
    public decimal? SelectedQuoteAmount { get; set; }
    public decimal ExpenseAmount { get; set; }
    public decimal OwnerCharge { get; set; }
    public decimal ManagerFee { get; set; }
    public DateTimeOffset? ScheduledAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string Currency { get; set; } = "JMD";
    public Guid? OwnerApprovalId { get; set; }
    public long RowVersion { get; set; } = 1;
}

public sealed class MilestonePmMaintenanceQuote : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid MaintenanceId { get; set; }
    public Guid VendorId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "JMD";
    public string Scope { get; set; } = string.Empty;
    public string Status { get; set; } = "RECEIVED";
    public DateTimeOffset? ExpiresAt { get; set; }
}

public sealed class MilestonePmMaintenanceEvent : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid MaintenanceId { get; set; }
    public Guid ActorUserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

public sealed class MilestonePmCleaningReadiness : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? AssignedUserId { get; set; }
    public Guid? VendorId { get; set; }
    public DateTimeOffset DueAt { get; set; }
    public string Status { get; set; } = "NOT_READY";
    public string ChecklistJson { get; set; } = "[]";
    public string PhotosJson { get; set; } = "[]";
    public string Issues { get; set; } = string.Empty;
    public DateTimeOffset? CompletedAt { get; set; }
    public long RowVersion { get; set; } = 1;
}

public sealed class MilestonePmAsset : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid PropertyId { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SerialReference { get; set; } = string.Empty;
    public DateOnly? PurchaseDate { get; set; }
    public decimal? PurchaseCost { get; set; }
    public DateOnly? WarrantyExpiry { get; set; }
    public string Condition { get; set; } = "GOOD";
    public string Category { get; set; } = "GENERAL";
    public string Status { get; set; } = "ACTIVE";
    public int Quantity { get; set; } = 1;
    public string Location { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = "{}";
    public string PhotosJson { get; set; } = "[]";
    public DateTimeOffset? RetiredAt { get; set; }
    public long RowVersion { get; set; } = 1;
}

public sealed class MilestonePmIncident : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid? BookingId { get; set; }
    public string IncidentType { get; set; } = "GENERAL";
    public string Severity { get; set; } = "MEDIUM";
    public DateTimeOffset OccurredAt { get; set; }
    public string Description { get; set; } = string.Empty;
    public string InvolvedPartiesJson { get; set; } = "[]";
    public string EvidenceJson { get; set; } = "[]";
    public string ActionTaken { get; set; } = string.Empty;
    public string FollowUp { get; set; } = string.Empty;
    public decimal FinancialImpact { get; set; }
    public string? InsuranceReference { get; set; }
    public string Status { get; set; } = "OPEN";
    public DateTimeOffset? ResolvedAt { get; set; }
    public long RowVersion { get; set; } = 1;
}

public sealed class MilestonePmInspectionRecord : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid? AssignedUserId { get; set; }
    public string InspectionType { get; set; } = "ROUTINE";
    public DateTimeOffset ScheduledAt { get; set; }
    public string ChecklistJson { get; set; } = "[]";
    public string EvidenceJson { get; set; } = "[]";
    public string FindingsJson { get; set; } = "[]";
    public string Status { get; set; } = "SCHEDULED";
    public DateTimeOffset? SignedOffAt { get; set; }
    public Guid? CorrectiveWorkOrderId { get; set; }
    public long RowVersion { get; set; } = 1;
}

public sealed class MilestonePmTeamEvent : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid MembershipId { get; set; }
    public Guid ActorUserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}
