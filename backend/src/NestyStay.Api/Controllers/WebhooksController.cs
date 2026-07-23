using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
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
    private static readonly ConcurrentDictionary<string, DateTimeOffset> ProcessedWebhookEvents = new();
    private static readonly TimeSpan StripeSignatureTolerance = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan WebhookReplayMemory = TimeSpan.FromDays(1);

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
        if (IsStripeProvider(provider) && environment.IsProduction())
        {
            if (!IsStripeWebhookAuthorized(request.PayloadJson))
            {
                return Unauthorized(new { message = "Stripe webhook signature is missing or invalid." });
            }
        }
        else if (!IsWebhookAuthorized())
        {
            return Unauthorized(new { message = "Webhook shared secret is missing or invalid." });
        }

        if (TryRejectReplay(provider, request) is { } replayResult)
        {
            return replayResult;
        }

        return Accepted(new
        {
            provider,
            request.EventType,
            accepted = true
        });
    }

    private IActionResult? TryRejectReplay(string provider, WebhookEventRequest request)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var item in ProcessedWebhookEvents.Where(item => item.Value < now.Subtract(WebhookReplayMemory)).ToArray())
        {
            ProcessedWebhookEvents.TryRemove(item.Key, out _);
        }

        var eventId = ResolveWebhookEventId(request);
        var replayKey = $"{provider.Trim().ToLowerInvariant()}:{eventId}";
        return ProcessedWebhookEvents.TryAdd(replayKey, now)
            ? null
            : Conflict(new { message = "Webhook event has already been processed." });
    }

    private bool IsStripeWebhookAuthorized(string payloadJson)
    {
        var signingSecret = ResolveSecret("Webhooks:StripeSigningSecret", "STRIPE_WEBHOOK_SECRET");
        var signatureHeader = Request.Headers["Stripe-Signature"].ToString();
        if (string.IsNullOrWhiteSpace(signingSecret) || string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        var parts = signatureHeader
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Split('=', 2))
            .Where(part => part.Length == 2)
            .GroupBy(part => part[0], StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(part => part[1]).ToArray(), StringComparer.Ordinal);

        if (!parts.TryGetValue("t", out var timestamps) ||
            !long.TryParse(timestamps.FirstOrDefault(), out var timestampSeconds) ||
            !parts.TryGetValue("v1", out var signatures) ||
            signatures.Length == 0)
        {
            return false;
        }

        var timestamp = DateTimeOffset.FromUnixTimeSeconds(timestampSeconds);
        if (timestamp < DateTimeOffset.UtcNow.Subtract(StripeSignatureTolerance) ||
            timestamp > DateTimeOffset.UtcNow.Add(StripeSignatureTolerance))
        {
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingSecret));
        var signedPayload = Encoding.UTF8.GetBytes($"{timestampSeconds}.{payloadJson}");
        var expected = Convert.ToHexString(hmac.ComputeHash(signedPayload)).ToLowerInvariant();
        return signatures.Any(signature => FixedTimeEqualsHex(expected, signature));
    }

    private static bool FixedTimeEqualsHex(string expected, string actual)
    {
        try
        {
            var expectedBytes = Convert.FromHexString(expected);
            var actualBytes = Convert.FromHexString(actual.Trim());
            return expectedBytes.Length == actualBytes.Length && CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool IsStripeProvider(string provider) =>
        provider.Equals("stripe", StringComparison.OrdinalIgnoreCase);

    private static string ResolveWebhookEventId(WebhookEventRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.EventId))
        {
            return request.EventId.Trim();
        }

        try
        {
            using var document = JsonDocument.Parse(request.PayloadJson);
            if (document.RootElement.TryGetProperty("id", out var id) && !string.IsNullOrWhiteSpace(id.GetString()))
            {
                return id.GetString()!;
            }
        }
        catch (JsonException)
        {
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{request.EventType}:{request.PayloadJson}"))).ToLowerInvariant();
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
