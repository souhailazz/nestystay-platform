using NestyStay.Domain.Common;

namespace NestyStay.Domain.Payments;

public sealed class PaymentAccount : BaseEntity
{
    public Guid UserId { get; set; }
    public string Provider { get; set; } = "Stripe";
    public string ExternalAccountId { get; set; } = string.Empty;
    public bool IsPayoutEnabled { get; set; }
}

public sealed class PaymentIntentRecord : BaseEntity
{
    public Guid? BookingId { get; set; }
    public string Provider { get; set; } = "Stripe";
    public string ExternalIntentId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
}

public sealed class PaymentTransaction : BaseEntity
{
    public Guid PaymentIntentRecordId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string? ExternalTransactionId { get; set; }
}

public sealed class EscrowHold : BaseEntity
{
    public string SubjectType { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public Guid RecipientUserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public EscrowStatus Status { get; set; } = EscrowStatus.Held;
    public DateTimeOffset? AutoReleaseAt { get; set; }
}

public sealed class Payout : BaseEntity
{
    public Guid RecipientUserId { get; set; }
    public string Provider { get; set; } = "Stripe";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = "Pending";
    public string? ExternalTransferId { get; set; }
}

public sealed class Subscription : BaseEntity
{
    public string SubjectType { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public string SubscriptionType { get; set; } = string.Empty;
    public string Provider { get; set; } = "Stripe";
    public string Status { get; set; } = "Active";
    public DateTimeOffset RenewsAt { get; set; }
}

public sealed class Invoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public string SubjectType { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = "Draft";
}

public sealed class InvoiceLine : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
}
