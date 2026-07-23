using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestyStay.Api.Auth;
using NestyStay.Application.PhaseOne;
using NestyStay.Domain;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public sealed class BookingsController(IPhaseOneStore phaseOneStore, IResourceAuthorizationService authorization) : ControllerBase
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

    [Authorize(Policy = AdminTokenAuthenticationHandler.AdminPolicyName)]
    [HttpPost("{id:guid}/verification-result")]
    public async Task<IActionResult> ResolveVerification(Guid id, ResolveVerificationRequest request, CancellationToken cancellationToken)
    {
        var booking = await phaseOneStore.ResolveVerificationAsync(id, request, cancellationToken);
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

        var booking = await phaseOneStore.CapturePaymentAsync(id, cancellationToken);
        return booking is null ? NotFound() : Ok(booking);
    }

    [Authorize(Policy = AdminTokenAuthenticationHandler.AdminPolicyName)]
    [HttpPost("{id:guid}/refund-payment")]
    public async Task<IActionResult> RefundPayment(Guid id, RefundBookingRequest request, CancellationToken cancellationToken)
    {
        var booking = await phaseOneStore.RefundPaymentAsync(id, request, cancellationToken);
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

}
