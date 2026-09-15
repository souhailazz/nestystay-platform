using NestyStay.Domain.Common;

namespace NestyStay.Infrastructure.Persistence.Milestones;

/// <summary>Extensible but relationally scoped records for the remaining PMS workflows.</summary>
public sealed class MilestonePmProfessionalRecord : BaseEntity
{
    public Guid ManagerUserId { get; set; }
    public Guid? OwnerUserId { get; set; }
    public Guid? PropertyId { get; set; }
    public string Area { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string Status { get; set; } = "OPEN";
    public string PayloadJson { get; set; } = "{}";
    public string? Currency { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? SearchText { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public long RowVersion { get; set; } = 1;
}

public sealed class MilestonePmProfessionalRecordEvent : BaseEntity
{
    public Guid RecordId { get; set; }
    public Guid ManagerUserId { get; set; }
    public Guid ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = "{}";
    public string? IdempotencyKey { get; set; }
}
