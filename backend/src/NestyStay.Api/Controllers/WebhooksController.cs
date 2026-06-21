using Microsoft.AspNetCore.Mvc;
using NestyStay.Application.PhaseOne;
using NestyStay.Api.Webhooks;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
public sealed class WebhooksController(IPhaseOneStore phaseOneStore) : ControllerBase
{
    [HttpPost("alibaba-ekyc")]
    public async Task<IActionResult> ReceiveAlibabaEkyc(AlibabaEkycWebhookRequest request, CancellationToken cancellationToken)
    {
        var booking = await phaseOneStore.ResolveVerificationAsync(
            request.BookingId,
            new ResolveVerificationRequest(request.Passed, request.TransactionId),
            cancellationToken);

        return booking is null ? NotFound() : Accepted(booking);
    }

    [HttpPost("{provider}")]
    public IActionResult Receive(string provider, WebhookEventRequest request)
    {
        return Accepted(new
        {
            provider,
            request.EventType,
            accepted = true
        });
    }
}
