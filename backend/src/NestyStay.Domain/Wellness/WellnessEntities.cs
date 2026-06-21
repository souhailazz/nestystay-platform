using NestyStay.Domain.Common;

namespace NestyStay.Domain.Wellness;

public sealed class Officer : BaseEntity
{
    public Guid UserId { get; set; }
    public bool IsActiveJcf { get; set; }
    public bool IsRetired { get; set; }
    public string CurrentNestyStayId { get; set; } = string.Empty;
    public string EligibilityStatus { get; set; } = "Pending";
}

public sealed class OfficerIdHistory : BaseEntity
{
    public Guid OfficerId { get; set; }
    public string NestyStayId { get; set; } = string.Empty;
    public int Year { get; set; }
    public bool IsRetiredIdentifier { get; set; }
}

public sealed class WellnessVisitTypeDefinition : BaseEntity
{
    public WellnessVisitType VisitType { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MinimumDurationMinutes { get; set; }
    public string Description { get; set; } = string.Empty;
}

public sealed class WellnessVisit : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Guid HostUserId { get; set; }
    public Guid OfficerId { get; set; }
    public WellnessVisitType VisitType { get; set; }
    public DateTimeOffset ScheduledAt { get; set; }
    public decimal OfficerRate { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = "Scheduled";
}

public sealed class WellnessReport : BaseEntity
{
    public Guid WellnessVisitId { get; set; }
    public Guid SubmittedByOfficerId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string PhotoStorageObjectIdsJson { get; set; } = "[]";
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class WellnessBadge : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Guid WellnessVisitId { get; set; }
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset ValidThrough { get; set; }
}

public sealed class WellnessEscrowEvent : BaseEntity
{
    public Guid WellnessVisitId { get; set; }
    public EscrowStatus Status { get; set; }
    public string Reason { get; set; } = string.Empty;
}
