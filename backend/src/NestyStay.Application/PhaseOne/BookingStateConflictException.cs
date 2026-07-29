using NestyStay.Domain;

namespace NestyStay.Application.PhaseOne;

public sealed class BookingStateConflictException : InvalidOperationException
{
    public BookingStateConflictException(
        string message,
        string operation,
        BookingStatus? currentBookingStatus = null,
        PaymentStatus? currentPaymentStatus = null)
        : base(message)
    {
        Operation = operation;
        CurrentBookingStatus = currentBookingStatus;
        CurrentPaymentStatus = currentPaymentStatus;
    }

    public string Code { get; } = "booking_state_conflict";
    public string Operation { get; }
    public BookingStatus? CurrentBookingStatus { get; }
    public PaymentStatus? CurrentPaymentStatus { get; }
}
