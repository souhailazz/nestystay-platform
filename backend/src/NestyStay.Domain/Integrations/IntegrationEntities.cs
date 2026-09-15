using NestyStay.Domain.Common;

namespace NestyStay.Domain.Integrations;

public sealed class ProviderConfig : BaseEntity
{
    public ProviderKind Kind { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public string EncryptedConfigReference { get; set; } = string.Empty;
}

public sealed class ProviderEvent : BaseEntity
{
    public ProviderKind Kind { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public string PayloadSha256 { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Status { get; set; } = "Received";
    public string SubjectType { get; set; } = string.Empty;
    public Guid? SubjectId { get; set; }
    public string ProcessingResult { get; set; } = string.Empty;
    public DateTimeOffset? ProcessedAt { get; set; }
}

public sealed class IntegrationFailover : BaseEntity
{
    public ProviderKind Kind { get; set; }
    public string FromProvider { get; set; } = string.Empty;
    public string ToProvider { get; set; } = string.Empty;
    public Guid SwitchedByUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
