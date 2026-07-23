using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using NestyStay.Application.PhaseOne;
using NestyStay.Api.Webhooks;
using NestyStay.Domain;

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
    public async Task<IActionResult> Receive(string provider, WebhookEventRequest request, CancellationToken cancellationToken)
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

        var booking = IsStripeProvider(provider)
            ? await ApplyStripeWebhookAsync(request, cancellationToken)
            : null;

        return Accepted(new
        {
            provider,
            request.EventType,
            accepted = true,
            bookingId = booking?.Id
        });
    }

    private async Task<BookingDto?> ApplyStripeWebhookAsync(WebhookEventRequest request, CancellationToken cancellationToken)
    {
        if (TryCreateStripePaymentUpdate(request) is not { } update)
        {
            return null;
        }

        return await phaseOneStore.ApplyPaymentWebhookAsync(update, cancellationToken);
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

    private static PaymentWebhookUpdateRequest? TryCreateStripePaymentUpdate(WebhookEventRequest request)
    {
        try
        {
            using var document = JsonDocument.Parse(request.PayloadJson);
            var root = document.RootElement;
            var eventType = root.TryGetProperty("type", out var typeElement) && !string.IsNullOrWhiteSpace(typeElement.GetString())
                ? typeElement.GetString()!
                : request.EventType;
            var eventId = ResolveWebhookEventId(request);
            var payload = root.TryGetProperty("data", out var data) &&
                data.TryGetProperty("object", out var dataObject)
                    ? dataObject
                    : root;

            return eventType switch
            {
                "payment_intent.succeeded" => CreatePaymentIntentUpdate(eventId, eventType, payload, PaymentStatus.Captured),
                "payment_intent.payment_failed" => CreatePaymentIntentUpdate(eventId, eventType, payload, PaymentStatus.Failed),
                "payment_intent.canceled" => CreatePaymentIntentUpdate(eventId, eventType, payload, PaymentStatus.Cancelled),
                "charge.refunded" => CreateRefundUpdate(eventId, eventType, payload, "amount_refunded"),
                "refund.succeeded" => CreateRefundUpdate(eventId, eventType, payload, "amount"),
                _ => null
            };
        }
        catch (JsonException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static PaymentWebhookUpdateRequest? CreatePaymentIntentUpdate(string eventId, string eventType, JsonElement payload, PaymentStatus status)
    {
        var paymentIntentReference = payload.TryGetProperty("id", out var id) ? id.GetString() : null;
        if (string.IsNullOrWhiteSpace(paymentIntentReference))
        {
            return null;
        }

        return new PaymentWebhookUpdateRequest(
            "Stripe",
            eventId,
            eventType,
            paymentIntentReference,
            status,
            ResolveMinorUnitAmount(payload, "amount_received") ?? ResolveMinorUnitAmount(payload, "amount"),
            ResolveCurrency(payload),
            ResolveString(payload, "latest_charge"),
            ResolveString(payload, "cancellation_reason"),
            ResolveCreatedAt(payload));
    }

    private static PaymentWebhookUpdateRequest? CreateRefundUpdate(string eventId, string eventType, JsonElement payload, string amountPropertyName)
    {
        var paymentIntentReference = ResolveString(payload, "payment_intent");
        if (string.IsNullOrWhiteSpace(paymentIntentReference))
        {
            return null;
        }

        return new PaymentWebhookUpdateRequest(
            "Stripe",
            eventId,
            eventType,
            paymentIntentReference,
            PaymentStatus.Refunded,
            ResolveMinorUnitAmount(payload, amountPropertyName),
            ResolveCurrency(payload),
            ResolveString(payload, "id"),
            ResolveString(payload, "reason"),
            ResolveCreatedAt(payload));
    }

    private static string? ResolveString(JsonElement payload, string propertyName) =>
        payload.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static string? ResolveCurrency(JsonElement payload) =>
        ResolveString(payload, "currency")?.ToUpperInvariant();

    private static decimal? ResolveMinorUnitAmount(JsonElement payload, string propertyName) =>
        payload.TryGetProperty(propertyName, out var property) && property.TryGetInt64(out var minorUnits)
            ? decimal.Round(minorUnits / 100m, 2, MidpointRounding.AwayFromZero)
            : null;

    private static DateTimeOffset? ResolveCreatedAt(JsonElement payload) =>
        payload.TryGetProperty("created", out var created) && created.TryGetInt64(out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;

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
