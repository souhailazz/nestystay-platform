using NestyStay.Domain.Common;

namespace NestyStay.Domain.Verification;

public sealed class VerificationCheck : BaseEntity
{
    public VerificationSubjectType SubjectType { get; set; }
    public Guid SubjectId { get; set; }
    public string Provider { get; set; } = "AlibabaCloud";
    public VerificationStatus Status { get; set; } = VerificationStatus.NotStarted;
    public decimal CostAmount { get; set; }
    public string CostCurrency { get; set; } = "USD";
    public string? DocumentType { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public sealed class VerificationEvent : BaseEntity
{
    public Guid VerificationCheckId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class IdentityDocument : BaseEntity
{
    public VerificationSubjectType SubjectType { get; set; }
    public Guid SubjectId { get; set; }
    public Guid StorageObjectId { get; set; }
    public string EncryptedMetadataJson { get; set; } = "{}";
    public string? IssuingCountry { get; set; }
    public DateOnly? ExpiresOn { get; set; }
}
