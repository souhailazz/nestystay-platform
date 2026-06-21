using NestyStay.Domain.Common;

namespace NestyStay.Domain.Access;

public sealed class QrAccessCode : BaseEntity
{
    public string SubjectType { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
}

public sealed class QrScanLog : BaseEntity
{
    public Guid QrAccessCodeId { get; set; }
    public Guid? GateGuardUserId { get; set; }
    public DateTimeOffset ScannedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Result { get; set; } = string.Empty;
    public string? DeviceMetadataJson { get; set; }
}

public sealed class VisitorLog : BaseEntity
{
    public Guid? CommunityId { get; set; }
    public Guid? UnitId { get; set; }
    public string VisitorName { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public DateTimeOffset LoggedAt { get; set; } = DateTimeOffset.UtcNow;
}
