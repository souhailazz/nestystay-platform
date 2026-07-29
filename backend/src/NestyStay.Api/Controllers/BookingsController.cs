using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestyStay.Api.Auth;
using NestyStay.Application.Admin;
using NestyStay.Application.PhaseOne;
using NestyStay.Application.SpecCompletion;
using NestyStay.Domain;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public sealed class BookingsController(
    IPhaseOneStore phaseOneStore,
    IResourceAuthorizationService authorization,
    IPrivilegedAuditStore auditStore) : ControllerBase
{
    [Authorize]
    [HttpGet]
    public IActionResult List()
    {
        if (authorization.IsInRole(UserRole.Admin))
        {
            return Ok(phaseOneStore.GetBookings());
        }

        var userId = authorization.RequireSignedInUser("Authenticated user id is required.");
        var bookings = phaseOneStore.GetBookings()
            .Where(booking =>
                booking.GuestUserId == userId ||
                (authorization.IsInRole(UserRole.Host) && booking.HostUserId == userId))
            .ToList();
        return Ok(bookings);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public IActionResult Get(Guid id)
    {
        var booking = phaseOneStore.GetBooking(id);
        if (booking is null)
        {
            return NotFound();
        }

        return authorization.CanAccessBooking(booking) ? Ok(booking) : NotFound();
    }

    [Authorize]
    [HttpGet("{id:guid}/invoice")]
    public Task<IActionResult> DownloadInvoice(Guid id, CancellationToken cancellationToken) =>
        DownloadBookingDocumentAsync(id, phaseOneStore.GetBookingInvoiceAsync, cancellationToken);

    [Authorize]
    [HttpGet("{id:guid}/receipt")]
    public Task<IActionResult> DownloadReceipt(Guid id, CancellationToken cancellationToken) =>
        DownloadBookingDocumentAsync(id, phaseOneStore.GetBookingReceiptAsync, cancellationToken);

    [HttpPost("quote")]
    public async Task<IActionResult> Quote(BookingQuoteRequest request, CancellationToken cancellationToken) =>
        Ok(await phaseOneStore.QuoteBookingAsync(request, cancellationToken));

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var guestUserId = authorization.RequireSignedInUser("Authenticated user id is required.");
        return Ok(await phaseOneStore.CreateBookingAsync(request with { GuestUserId = guestUserId }, cancellationToken));
    }

    [Authorize(Policy = AdminAuthorizationPolicies.BookingManagement)]
    [HttpPost("{id:guid}/verification-result")]
    public async Task<IActionResult> ResolveVerification(Guid id, ResolveVerificationRequest request, CancellationToken cancellationToken)
    {
        var previous = phaseOneStore.GetBooking(id);
        var booking = await phaseOneStore.ResolveVerificationAsync(id, request, cancellationToken);
        if (booking is not null)
        {
            await auditStore.RecordPrivilegedAuditAsync(
                new PrivilegedAuditRecord(
                    AuditActor(AdminPermissionCatalog.BookingManagement),
                    "BookingVerificationOverridden",
                    "Booking",
                    booking.Id,
                    request.Passed ? "Booking verification marked passed." : "Booking verification marked failed.",
                    previous is null ? null : SnapshotBooking(previous),
                    SnapshotBooking(booking)),
                cancellationToken);
        }

        return booking is null ? NotFound() : Ok(booking);
    }

    [Authorize]
    [HttpPost("{id:guid}/capture-payment")]
    public async Task<IActionResult> CapturePayment(Guid id, CancellationToken cancellationToken)
    {
        var existing = phaseOneStore.GetBooking(id);
        if (existing is null)
        {
            return NotFound();
        }

        if (!authorization.CanCaptureBooking(existing))
        {
            return Forbid();
        }

        var isAdmin = authorization.IsInRole(UserRole.Admin);
        if (isAdmin && !AdminAuthorizationPolicies.HasPermission(User, AdminPermissionCatalog.PaymentManagement))
        {
            return Forbid();
        }

        var booking = await phaseOneStore.CapturePaymentAsync(id, cancellationToken);
        if (booking is not null)
        {
            await auditStore.RecordPrivilegedAuditAsync(
                new PrivilegedAuditRecord(
                    AuditActor(isAdmin ? AdminPermissionCatalog.PaymentManagement : null),
                    "PaymentCaptured",
                    "Booking",
                    booking.Id,
                    "Payment capture requested.",
                    SnapshotBooking(existing),
                    SnapshotBooking(booking)),
                cancellationToken);
        }

        return booking is null ? NotFound() : Ok(booking);
    }

    [Authorize(Policy = AdminAuthorizationPolicies.RefundManagement)]
    [HttpPost("{id:guid}/refund-payment")]
    public async Task<IActionResult> RefundPayment(Guid id, RefundBookingRequest request, CancellationToken cancellationToken)
    {
        var previous = phaseOneStore.GetBooking(id);
        var booking = await phaseOneStore.RefundPaymentAsync(id, request, cancellationToken);
        if (booking is not null)
        {
            await auditStore.RecordPrivilegedAuditAsync(
                new PrivilegedAuditRecord(
                    AuditActor(AdminPermissionCatalog.RefundManagement),
                    "PaymentRefunded",
                    "Booking",
                    booking.Id,
                    string.IsNullOrWhiteSpace(request.Reason) ? "Refund requested." : request.Reason,
                    previous is null ? null : SnapshotBooking(previous),
                    SnapshotBooking(booking)),
                cancellationToken);
        }

        return booking is null ? NotFound() : Ok(booking);
    }

    private async Task<IActionResult> DownloadBookingDocumentAsync(
        Guid id,
        Func<Guid, CancellationToken, Task<BookingDocumentDto?>> loadDocument,
        CancellationToken cancellationToken)
    {
        var booking = phaseOneStore.GetBooking(id);
        if (booking is null || !authorization.CanAccessBooking(booking))
        {
            return NotFound();
        }

        var document = await loadDocument(id, cancellationToken);
        return document is null
            ? NotFound()
            : File(document.Content, document.ContentType, document.FileName);
    }

    private AuditActorContext AuditActor(string? effectivePermission) => new(
        authorization.TryGetSignedInUser(),
        authorization.IsInRole(UserRole.Admin) ? "Admin" : authorization.IsInRole(UserRole.Host) ? "Host" : "User",
        effectivePermission,
        HttpContext.TraceIdentifier);

    private static object SnapshotBooking(BookingDto booking) => new
    {
        booking.Id,
        booking.PropertyId,
        booking.HostUserId,
        booking.GuestUserId,
        booking.Status,
        booking.VerificationStatus,
        booking.PaymentStatus,
        booking.TotalAmount,
        booking.Currency,
        booking.PaymentAuthorizationReference,
        booking.PaymentCaptureReference,
        booking.PaymentRefundReference,
        booking.RefundedAmount,
        booking.RefundReason,
        booking.RefundedAt
    };
}
