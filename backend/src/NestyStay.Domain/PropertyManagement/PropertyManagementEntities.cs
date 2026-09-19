using NestyStay.Domain.Common;

namespace NestyStay.Domain.PropertyManagement;

public sealed class Community : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public GovernanceMode GovernanceMode { get; set; } = GovernanceMode.LicensedManager;
    public bool HasLicensedManager { get; set; } = true;
}

public sealed class CommunityMembership : BaseEntity
{
    public Guid CommunityId { get; set; }
    public Guid UserId { get; set; }
    public UserRole Role { get; set; }
    public bool CanViewTenantContent { get; set; }
}

public sealed class OwnerUnit : BaseEntity
{
    public Guid OwnerUserId { get; set; }
    public Guid PropertyUnitId { get; set; }
    public DateTimeOffset OwnershipStartedAt { get; set; }
    public DateTimeOffset? OwnershipEndedAt { get; set; }
}

public sealed class MaintenanceRequest : BaseEntity
{
    public Guid CommunityId { get; set; }
    public Guid? PropertyUnitId { get; set; }
    public Guid SubmittedByUserId { get; set; }
    public string Issue { get; set; } = string.Empty;
    public string Priority { get; set; } = "Normal";
    public string Status { get; set; } = "Open";
}

public sealed class UtilityBill : BaseEntity
{
    public Guid CommunityId { get; set; }
    public Guid StorageObjectId { get; set; }
    public string UtilityType { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public string AllocationMethod { get; set; } = "PerUnit";
}

public sealed class ManagerStatement : BaseEntity
{
    public Guid CommunityId { get; set; }
    public Guid? OwnerUserId { get; set; }
    public string StatementType { get; set; } = string.Empty;
    public string StatementJson { get; set; } = "{}";
}

public sealed class ArrearsRecord : BaseEntity
{
    public Guid CommunityId { get; set; }
    public Guid OwnerUserId { get; set; }
    public decimal AmountOverdue { get; set; }
    public string Currency { get; set; } = "USD";
    public int MonthsOverdue { get; set; }
    public DateTimeOffset? LastReminderAt { get; set; }
}

public sealed class CommunityAnnouncement : BaseEntity
{
    public Guid CommunityId { get; set; }
    public Guid PostedByUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public sealed class StaffAssignment : BaseEntity
{
    public Guid CommunityId { get; set; }
    public Guid StaffUserId { get; set; }
    public string StaffType { get; set; } = string.Empty;
    public string ScheduleJson { get; set; } = "{}";
}
