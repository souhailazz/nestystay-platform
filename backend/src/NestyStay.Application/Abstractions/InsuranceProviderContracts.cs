namespace NestyStay.Application.Abstractions;

public sealed record InsurancePlan(
    string Code,
    string Market,
    decimal MonthlyAmount,
    string Currency,
    decimal PropertyDamageCoverage,
    decimal AccidentalMedicalCoverage,
    string CoverageSummary);

public sealed record InsuranceProviderResult(
    string ProviderName,
    string Status,
    string? ProviderReference = null,
    string? FailureReason = null,
    DateTimeOffset? EffectiveAt = null,
    DateTimeOffset? RenewsAt = null);

public interface IInsuranceProvider
{
    string ProviderName { get; }
    Task<IReadOnlyList<InsurancePlan>> GetAvailablePlansAsync(CancellationToken cancellationToken);
    Task<InsuranceProviderResult> ActivateAsync(Guid propertyId, InsurancePlan plan, string idempotencyKey, CancellationToken cancellationToken);
    Task<InsuranceProviderResult> CancelAsync(string providerReference, string idempotencyKey, CancellationToken cancellationToken);
    Task<InsuranceProviderResult> RenewAsync(string providerReference, InsurancePlan plan, string idempotencyKey, CancellationToken cancellationToken);
}
