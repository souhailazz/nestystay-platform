using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestyStay.Api.Auth;
using NestyStay.Application.PhaseOne;
using NestyStay.Domain;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public sealed class BookingsController(IPhaseOneStore phaseOneStore, CurrentUserContext currentUser) : ControllerBase
{
    [Authorize]
    [HttpGet]
    public IActionResult List()
    {
        if (User.IsInRole(UserRole.Admin.ToString()))
        {
            return Ok(phaseOneStore.GetBookings());
        }

        var userId = RequireAuthenticatedUserId();
        var bookings = phaseOneStore.GetBookings()
            .Where(booking =>
                booking.GuestUserId == userId ||
                (User.IsInRole(UserRole.Host.ToString()) && booking.HostUserId == userId))
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

        return CanAccessBooking(booking) ? Ok(booking) : NotFound();
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
        var guestUserId = RequireAuthenticatedUserId();
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

        if (!User.IsInRole(UserRole.Admin.ToString()) &&
            !(User.IsInRole(UserRole.Host.ToString()) && currentUser.UserId == existing.HostUserId))
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
        if (booking is null || !CanAccessBooking(booking))
        {
            return NotFound();
        }

        var document = await loadDocument(id, cancellationToken);
        return document is null
            ? NotFound()
            : File(document.Content, document.ContentType, document.FileName);
    }

    private Guid RequireAuthenticatedUserId() =>
        currentUser.UserId ?? throw new UnauthorizedAccessException("Authenticated user id is required.");

    private bool CanAccessBooking(BookingDto booking)
    {
        if (User.IsInRole(UserRole.Admin.ToString()))
        {
            return true;
        }

        var userId = RequireAuthenticatedUserId();
        return booking.GuestUserId == userId ||
            (User.IsInRole(UserRole.Host.ToString()) && booking.HostUserId == userId);
    }
}
