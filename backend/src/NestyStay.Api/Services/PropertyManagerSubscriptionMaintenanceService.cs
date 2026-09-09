using NestyStay.Application.PropertyManager;

namespace NestyStay.Api.Services;

/// <summary>
/// Applies scheduled property-manager subscription changes at the end of the
/// paid period. The store owns the transition and audit event so API reads and
/// the worker are consistent; this process simply provides durable scheduling.
/// </summary>
public sealed class PropertyManagerSubscriptionMaintenanceService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<PropertyManagerSubscriptionMaintenanceService> logger) : BackgroundService
{
    private static readonly TimeSpan Cadence = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(Cadence, timeProvider, stoppingToken); }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<IPropertyManagerStore>();
                var applied = await store.ApplyDueSubscriptionChangesAsync(stoppingToken);
                if (applied > 0) logger.LogInformation("Applied {Count} scheduled property-manager subscription changes.", applied);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Property-manager subscription maintenance failed; the next hourly run will retry.");
            }

            try { await Task.Delay(Cadence, timeProvider, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
        }
    }

    /// <summary>Runs one bounded pass and is available to deterministic tests.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IPropertyManagerStore>().ApplyDueSubscriptionChangesAsync(cancellationToken);
    }
}
