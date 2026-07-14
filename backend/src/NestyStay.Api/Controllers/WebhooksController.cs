using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using NestyStay.Application.PhaseOne;
using NestyStay.Api.Webhooks;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
public sealed class WebhooksController(
    IPhaseOneStore phaseOneStore,
    IConfiguration configuration,
    IHostEnvironment environment) : ControllerBase
{
    [HttpPost("alibaba-ekyc")]
    public async Task<IActionResult> ReceiveAlibabaEkyc(AlibabaEkycWebhookRequest request, CancellationToken cancellationToken)
    {
        if (!IsWebhookAuthorized())
        {
            return Unauthorized(new { message = "Webhook shared secret is missing or invalid." });
        }

        var booking = await phaseOneStore.ResolveVerificationAsync(
            request.BookingId,
            new ResolveVerificationRequest(request.Passed, request.TransactionId),
            cancellationToken);

        return booking is null ? NotFound() : Accepted(booking);
    }

    [HttpPost("{provider}")]
    public IActionResult Receive(string provider, WebhookEventRequest request)
    {
        if (!IsWebhookAuthorized())
        {
            return Unauthorized(new { message = "Webhook shared secret is missing or invalid." });
        }

        return Accepted(new
        {
            provider,
            request.EventType,
            accepted = true
        });
    }

    private bool IsWebhookAuthorized()
    {
        if (!environment.IsProduction())
        {
            return true;
        }

        var expectedSecret = ResolveSecret("Webhooks:SharedSecret", "NESTYSTAY_WEBHOOK_SHARED_SECRET");
        var providedSecret = Request.Headers["X-NestyStay-Webhook-Secret"].ToString();
        if (string.IsNullOrWhiteSpace(expectedSecret) || string.IsNullOrWhiteSpace(providedSecret))
        {
            return false;
        }

        var expected = Encoding.UTF8.GetBytes(expectedSecret);
        var actual = Encoding.UTF8.GetBytes(providedSecret);
        return expected.Length == actual.Length && CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private string? ResolveSecret(string configurationKey, string environmentKey)
    {
        var configured = configuration[configurationKey];
        return string.IsNullOrWhiteSpace(configured)
            ? Environment.GetEnvironmentVariable(environmentKey)
            : configured;
    }
}
