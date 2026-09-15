using NestyStay.Application.PhaseOne;

namespace NestyStay.Application.PhaseTwo;

/// <summary>
/// Owns the provider-facing payment lifecycle for paid badge purchases and
/// renewals. Badge assignments are only finalized by this lifecycle after a
/// trusted provider success or an explicit zero-value free path.
/// </summary>
public interface IBadgePaymentStore
{
    Task<BadgePaymentIntentDto> CreatePurchaseIntentAsync(
        PurchaseBadgeRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<BadgePaymentIntentDto?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken);

    Task<BadgePaymentIntentDto> CreateRenewalIntentAsync(
        Guid assignmentId,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<BadgePaymentIntentDto?> ApplyWebhookAsync(
        PaymentWebhookUpdateRequest request,
        CancellationToken cancellationToken);
}
