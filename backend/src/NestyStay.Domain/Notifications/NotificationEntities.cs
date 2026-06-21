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
}
