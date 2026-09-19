using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NestyStay.Infrastructure.Persistence;
using NestyStay.Infrastructure.Persistence.Milestones;

namespace NestyStay.Api.Services;

/// <summary>
/// Durable local worker for recurring utility runs and internal expiry notices.
/// Each result is keyed by manager/property/type/period, so multiple instances
/// can safely retry without duplicating a bill or notification.
/// </summary>
public sealed class PropertyManagerProfessionalWorker(IServiceScopeFactory scopeFactory, TimeProvider timeProvider, ILogger<PropertyManagerProfessionalWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunOnceAsync(stoppingToken); }
            catch (Exception exception) { logger.LogError(exception, "Property Manager professional worker failed; retrying."); }
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NestyStayDbContext>();
        var now = timeProvider.GetUtcNow();
        var period = now.ToString("yyyy-MM");
        var schedules = await db.MilestoneManagerUtilitySchedules.Where(x => x.IsActive && !x.IsDeleted && (x.LastRunAt == null || x.LastRunAt < now.AddHours(-20))).Take(500).ToListAsync(cancellationToken);
        var created = 0;
        foreach (var schedule in schedules)
        {
            var key = $"utility-run:{schedule.ManagerUserId:N}:{schedule.PropertyId:N}:{schedule.UtilityType}:{period}";
            var exists = await db.MilestonePmProfessionalRecords.AnyAsync(x => x.ManagerUserId == schedule.ManagerUserId && x.IdempotencyKey == key && !x.IsDeleted, cancellationToken);
            if (!exists)
            {
                db.MilestonePmProfessionalRecords.Add(new MilestonePmProfessionalRecord
                {
                    ManagerUserId = schedule.ManagerUserId,
                    OwnerUserId = schedule.OwnerUserId,
                    PropertyId = schedule.PropertyId,
                    Area = "utilities",
                    ResourceType = "RECURRING_RUN",
                    Status = "AWAITING_EVIDENCE",
                    Currency = "JMD",
                    IdempotencyKey = key,
                    SearchText = $"recurring utility {schedule.UtilityType} {period}".ToLowerInvariant(),
                    PayloadJson = JsonSerializer.Serialize(new { schedule.UtilityType, schedule.Rate, schedule.DayOfMonth, billingPeriod = period, requiresEvidence = true, generatedAt = now }),
                    CreatedByUserId = schedule.ManagerUserId,
                    UpdatedByUserId = schedule.ManagerUserId
                });
                schedule.LastRunAt = now;
                db.MilestoneAuditEvents.Add(new MilestoneAuditEvent { ManagerUserId = schedule.ManagerUserId, ActorUserId = schedule.ManagerUserId, ActorRole = "System", Action = "UtilityRecurringRunQueued", SubjectType = "UtilitySchedule", SubjectId = schedule.Id, Reason = "Recurring utility run awaiting meter/bill evidence", MetadataJson = JsonSerializer.Serialize(new { period, key }) });
                created++;
            }
        }
        if (created > 0) await db.SaveChangesAsync(cancellationToken);
        return created;
    }
}
