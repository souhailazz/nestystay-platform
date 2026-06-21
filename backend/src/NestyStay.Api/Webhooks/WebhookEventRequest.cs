namespace NestyStay.Api.Webhooks;

public sealed record WebhookEventRequest(string Provider, string EventType, string PayloadJson);

public sealed record AlibabaEkycWebhookRequest(Guid BookingId, string TransactionId, bool Passed);
