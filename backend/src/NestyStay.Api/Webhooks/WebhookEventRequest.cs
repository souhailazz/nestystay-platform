namespace NestyStay.Api.Webhooks;

public sealed record WebhookEventRequest(string Provider, string EventType, string PayloadJson, string? EventId = null);
