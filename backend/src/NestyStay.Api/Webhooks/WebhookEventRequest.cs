namespace NestyStay.Api.Webhooks;

public sealed record WebhookEventRequest(string Provider, string EventType, string PayloadJson, string? EventId = null);

public sealed record AlibabaEkycWebhookRequest(Guid BookingId, string TransactionId, bool Passed);
