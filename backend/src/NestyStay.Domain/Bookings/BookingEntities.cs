using NestyStay.Domain.Common;

namespace NestyStay.Domain.Bookings;

public sealed class Booking : BaseEntity
{
    public Guid GuestUserId { get; set; }
    public Guid PropertyId { get; set; }
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Draft;
    public bool RequiresGuestVerification { get; set; }
    public DateTimeOffset? HoldExpiresAt { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public string Currency { get; set; } = "USD";
}

public sealed class BookingGuest : BaseEntity
{
    public Guid BookingId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}

public sealed class BookingPriceLine : BaseEntity
{
    public Guid BookingId { get; set; }
    public string LineType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public bool IsRefundable { get; set; }
}

public sealed class BookingPaymentSchedule : BaseEntity
{
    public Guid BookingId { get; set; }
    public PaymentScheduleType ScheduleType { get; set; }
    public DateTimeOffset DueAt { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
}

public sealed class BookingStatusEvent : BaseEntity
{
    public Guid BookingId { get; set; }
    public BookingStatus FromStatus { get; set; }
    public BookingStatus ToStatus { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class BookingCancellation : BaseEntity
{
    public Guid BookingId { get; set; }
    public CancellationPolicyType PolicyType { get; set; }
    public decimal RefundAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Reason { get; set; } = string.Empty;
}

public sealed class BookingDispute : BaseEntity
{
    public Guid BookingId { get; set; }
    public Guid OpenedByUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public string? Resolution { get; set; }
}
