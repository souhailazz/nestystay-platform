using NestyStay.Domain.Common;

namespace NestyStay.Domain.Notifications;

public sealed class NotificationTemplate : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
}

public sealed class NotificationQueueItem : BaseEntity
{
    public Guid? RecipientUserId { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; } = NotificationStatus.Queued;
    public DateTimeOffset? SentAt { get; set; }
    /// <summary>Provider-neutral email delivery state. Kept separate from the legacy notification status.</summary>
    public string DeliveryStatus { get; set; } = "PENDING";
    public int AttemptCount { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }
    public string? LastError { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? TextBody { get; set; }
    public string? HtmlBody { get; set; }
    public string? ReplyToEmail { get; set; }
    public string? ReplyToName { get; set; }
}
