using NestyStay.Domain;

namespace NestyStay.Application.PhaseOne;

public static class BookingPaymentStateMachine
{
    public static bool IsIdempotentVerificationResolution(
        BookingStatus bookingStatus,
        VerificationStatus verificationStatus,
        bool passed) =>
        (passed && bookingStatus == BookingStatus.Approved && verificationStatus == VerificationStatus.Passed) ||
        (!passed && bookingStatus == BookingStatus.Rejected && verificationStatus is VerificationStatus.Failed or VerificationStatus.Expired);

    public static void EnsureVerificationCanResolve(
        BookingStatus bookingStatus,
        VerificationStatus verificationStatus,
        bool passed)
    {
        if (bookingStatus == BookingStatus.PendingVerification)
        {
            return;
        }

        if (IsIdempotentVerificationResolution(bookingStatus, verificationStatus, passed))
        {
            return;
        }

        throw Conflict(
            "resolve_verification",
            bookingStatus,
            null,
            "Verification can only be resolved while a booking is pending verification.");
    }

    public static void EnsureBookingTransition(BookingStatus currentStatus, BookingStatus nextStatus, string operation)
    {
        if (currentStatus == nextStatus)
        {
            return;
        }

        var allowed = currentStatus switch
        {
            BookingStatus.Draft => nextStatus is BookingStatus.PendingVerification or BookingStatus.Approved or BookingStatus.Cancelled,
            BookingStatus.PendingVerification => nextStatus is BookingStatus.Approved or BookingStatus.Rejected or BookingStatus.Cancelled,
            BookingStatus.Approved => nextStatus is BookingStatus.PaymentCaptured or BookingStatus.Cancelled,
            BookingStatus.PaymentCaptured => nextStatus is BookingStatus.Confirmed or BookingStatus.Cancelled,
            BookingStatus.Confirmed => nextStatus == BookingStatus.Cancelled,
            _ => false
        };

        if (!allowed)
        {
            throw Conflict(
                operation,
                currentStatus,
                null,
                $"Booking cannot move from {currentStatus} to {nextStatus}.");
        }
    }

    public static void EnsureCaptureCanStart(BookingStatus bookingStatus, PaymentStatus paymentStatus)
    {
        if (paymentStatus == PaymentStatus.Captured)
        {
            return;
        }

        if (bookingStatus != BookingStatus.Approved)
        {
            throw Conflict(
                "capture_payment",
                bookingStatus,
                paymentStatus,
                "Stripe payment can only be captured after verification passes and booking is approved.");
        }

        if (paymentStatus is PaymentStatus.Refunded or PaymentStatus.Failed or PaymentStatus.Cancelled)
        {
            throw Conflict(
                "capture_payment",
                bookingStatus,
                paymentStatus,
                $"Stripe payment cannot be captured from {paymentStatus} status.");
        }
    }

    public static void EnsureRefundCanStart(BookingStatus bookingStatus, PaymentStatus paymentStatus)
    {
        if (paymentStatus == PaymentStatus.Refunded)
        {
            return;
        }

        if (paymentStatus != PaymentStatus.Captured)
        {
            throw Conflict(
                "refund_payment",
                bookingStatus,
                paymentStatus,
                "Refunds require a captured payment.");
        }
    }

    public static void EnsurePaymentTransition(
        PaymentStatus currentStatus,
        PaymentStatus nextStatus,
        string operation,
        BookingStatus? bookingStatus = null)
    {
        if (currentStatus == nextStatus)
        {
            return;
        }

        var allowed = currentStatus switch
        {
            PaymentStatus.Pending => nextStatus is PaymentStatus.Authorized or PaymentStatus.Failed or PaymentStatus.Cancelled,
            PaymentStatus.Authorized => nextStatus is PaymentStatus.Captured or PaymentStatus.Failed or PaymentStatus.Cancelled,
            PaymentStatus.Captured => nextStatus == PaymentStatus.Refunded,
            _ => false
        };

        if (!allowed)
        {
            throw Conflict(
                operation,
                bookingStatus,
                currentStatus,
                $"Payment cannot move from {currentStatus} to {nextStatus}.");
        }
    }

    public static bool ShouldApplyWebhookPaymentTransition(PaymentStatus currentStatus, PaymentStatus incomingStatus) =>
        incomingStatus switch
        {
            PaymentStatus.Captured => currentStatus == PaymentStatus.Authorized,
            PaymentStatus.Refunded => currentStatus is PaymentStatus.Captured or PaymentStatus.Refunded,
            PaymentStatus.Failed or PaymentStatus.Cancelled => currentStatus is PaymentStatus.Pending or PaymentStatus.Authorized,
            _ => false
        };

    private static BookingStateConflictException Conflict(
        string operation,
        BookingStatus? bookingStatus,
        PaymentStatus? paymentStatus,
        string message) =>
        new(message, operation, bookingStatus, paymentStatus);
}
