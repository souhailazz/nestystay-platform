using NestyStay.Application.PhaseTwo;

namespace NestyStay.Api.Services;

public sealed class MilestoneMaintenanceService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<MilestoneMaintenanceService> logger) : BackgroundService
{
    private static readonly TimeSpan Cadence = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(Cadence, timeProvider, stoppingToken);
            try
            {
                using var scope = scopeFactory.CreateScope();
                var result = scope.ServiceProvider.GetRequiredService<IPhaseTwoStore>().RunBadgeMaintenance();
                if (result.AssignmentsExpired > 0 || result.RenewalRemindersDue > 0)
                {
                    logger.LogInformation(
                        "Badge maintenance reviewed {Reviewed} assignments, expired {Expired}, and found {Reminders} renewal reminders due.",
                        result.AssignmentsReviewed,
                        result.AssignmentsExpired,
                        result.RenewalRemindersDue);
                }
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Badge maintenance failed; the next scheduled run will retry.");
            }

        }
    }
}
