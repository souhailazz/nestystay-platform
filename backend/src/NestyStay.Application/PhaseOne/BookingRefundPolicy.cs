using System.Globalization;

namespace NestyStay.Application.PhaseOne;

public static class BookingRefundPolicy
{
    private const int MaximumReasonLength = 240;
    private const int MaximumIdempotencyKeyLength = 200;

    public static decimal ResolveAmount(decimal? requestedAmount, decimal totalAmount, decimal alreadyRefunded)
    {
        var remaining = decimal.Round(totalAmount - alreadyRefunded, 2, MidpointRounding.AwayFromZero);
        if (remaining <= 0m)
        {
            throw new InvalidOperationException("Booking payment has already been fully refunded.");
        }

        var amount = decimal.Round(requestedAmount ?? remaining, 2, MidpointRounding.AwayFromZero);
        if (amount <= 0m)
        {
            throw new InvalidOperationException("Refund amount must be greater than zero.");
        }

        if (amount > remaining)
        {
            throw new InvalidOperationException("Refund amount cannot exceed the remaining captured amount.");
        }

        return amount;
    }

    public static string NormalizeReason(string? reason)
    {
        var normalized = string.IsNullOrWhiteSpace(reason)
            ? "Requested by admin"
            : reason.Trim();

        return normalized.Length <= MaximumReasonLength
            ? normalized
            : normalized[..MaximumReasonLength];
    }

    public static string ResolveIdempotencyKey(Guid bookingId, decimal amount, string? suppliedKey)
    {
        if (!string.IsNullOrWhiteSpace(suppliedKey))
        {
            var key = suppliedKey.Trim();
            if (key.Length > MaximumIdempotencyKeyLength)
            {
                throw new InvalidOperationException("Refund idempotency key is too long.");
            }

            return key;
        }

        return $"booking:{bookingId:N}:refund:{amount.ToString("0.00", CultureInfo.InvariantCulture)}";
    }

    public static bool IsFullyRefunded(decimal totalAmount, decimal refundedAmount) =>
        decimal.Round(refundedAmount, 2, MidpointRounding.AwayFromZero) >=
        decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero);
}
