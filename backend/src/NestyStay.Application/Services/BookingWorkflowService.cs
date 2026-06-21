using NestyStay.Domain;

namespace NestyStay.Application.Services;

public interface IBookingWorkflowService
{
    IReadOnlyList<BookingWorkflowStep> GetPendingVerificationFlow();
}

public sealed class BookingWorkflowService : IBookingWorkflowService
{
    public IReadOnlyList<BookingWorkflowStep> GetPendingVerificationFlow() =>
    [
        new(1, BookingStatus.Draft, "View listing", "Guest", "Guest reviews listing, dates, price, badges, insurance, and QR access rules."),
        new(2, BookingStatus.Draft, "Book Now popup", "Guest", "Popup shows property summary, dates, fees, payment options, and verification requirements."),
        new(3, BookingStatus.PendingVerification, "Identity check", "Guest", "Alibaba eKYC starts when guest verification is required by the property or selected flow."),
        new(4, BookingStatus.PendingVerification, "Hold dates", "System", "Dates are held for one hour while verification is pending to prevent double booking."),
        new(5, BookingStatus.Approved, "Approve booking", "System", "Passed eKYC moves booking to approved and notifies guest and host."),
        new(6, BookingStatus.PaymentCaptured, "Capture payment", "Payment gateway", "Payment is captured only after approval, using the selected split-payment schedule."),
        new(7, BookingStatus.Confirmed, "Issue access", "System", "Full payment unlocks QR gate entry, messaging code, and entry details."),
        new(8, BookingStatus.Rejected, "Release dates", "System", "Failed eKYC cancels the booking, releases dates, and prevents payment capture.")
    ];
}
