using Microsoft.AspNetCore.Mvc;
using NestyStay.Application.PhaseOne;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public sealed class BookingsController(IPhaseOneStore phaseOneStore) : ControllerBase
{
    [HttpGet]
    public IActionResult List([FromQuery] Guid? guestUserId = null) =>
        Ok(phaseOneStore.GetBookings(guestUserId));

    [HttpGet("{id:guid}")]
    public IActionResult Get(Guid id)
    {
        var booking = phaseOneStore.GetBooking(id);
        return booking is null ? NotFound() : Ok(booking);
    }

    [HttpPost("quote")]
    public async Task<IActionResult> Quote(BookingQuoteRequest request, CancellationToken cancellationToken) =>
        Ok(await phaseOneStore.QuoteBookingAsync(request, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(CreateBookingRequest request, CancellationToken cancellationToken) =>
        Ok(await phaseOneStore.CreateBookingAsync(request, cancellationToken));

    [HttpPost("{id:guid}/verification-result")]
    public async Task<IActionResult> ResolveVerification(Guid id, ResolveVerificationRequest request, CancellationToken cancellationToken)
    {
        var booking = await phaseOneStore.ResolveVerificationAsync(id, request, cancellationToken);
        return booking is null ? NotFound() : Ok(booking);
    }

    [HttpPost("{id:guid}/capture-payment")]
    public async Task<IActionResult> CapturePayment(Guid id, CancellationToken cancellationToken)
    {
        var booking = await phaseOneStore.CapturePaymentAsync(id, cancellationToken);
        return booking is null ? NotFound() : Ok(booking);
    }
}
