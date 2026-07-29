namespace NestyStay.Application.PhaseOne;

public sealed class RateLimitExceededException : Exception
{
    public const string StableCode = "rate_limit_exceeded";

    public RateLimitExceededException(string message, TimeSpan? retryAfter = null)
        : base(message)
    {
        RetryAfter = retryAfter;
    }

    public string Code => StableCode;

    public TimeSpan? RetryAfter { get; }

    public int? RetryAfterSeconds =>
        RetryAfter is null
            ? null
            : Math.Max(1, (int)Math.Ceiling(RetryAfter.Value.TotalSeconds));
}
