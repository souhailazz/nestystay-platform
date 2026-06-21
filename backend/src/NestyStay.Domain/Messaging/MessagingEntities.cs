using NestyStay.Domain.Common;

namespace NestyStay.Domain.Messaging;

public sealed class ConversationThread : BaseEntity
{
    public string ThreadType { get; set; } = string.Empty;
    public Guid? BookingId { get; set; }
    public string? BookingCode { get; set; }
    public DateTimeOffset? RetentionExpiresAt { get; set; }
}

public sealed class Message : BaseEntity
{
    public Guid ConversationThreadId { get; set; }
    public Guid SenderUserId { get; set; }
    public string Channel { get; set; } = "InApp";
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset? RetentionExpiresAt { get; set; }
}
