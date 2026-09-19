using Microsoft.EntityFrameworkCore;
using NestyStay.Application.Abstractions;
using NestyStay.Infrastructure.Persistence;

namespace NestyStay.Api.Services;

/// <summary>
/// Queues deterministic, idempotent document-expiry reminders. The worker only
/// writes to the notification outbox; delivery and provider retries remain the
/// responsibility of the configured email transport.
/// </summary>
public sealed class PropertyManagerDocumentExpiryService(
    IServiceScopeFactory scopeFactory,
    IEmailSender emailSender,
    TimeProvider timeProvider,
    ILogger<PropertyManagerDocumentExpiryService> logger) : BackgroundService
{
    private static readonly TimeSpan Cadence = TimeSpan.FromHours(1);
    private static readonly int[] ReminderWindows = [30, 7, 1, 0];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Avoid a reminder burst during deployment. The first run is still
        // deterministic and bounded, then repeats on the hourly cadence.
        try { await Task.Delay(Cadence, timeProvider, stoppingToken); }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var queued = await RunOnceAsync(stoppingToken);
                if (queued > 0) logger.LogInformation("Queued {Count} property-manager document expiry reminders.", queued);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Property-manager document expiry reminders failed; the next hourly run will retry.");
            }

            try { await Task.Delay(Cadence, timeProvider, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
        }
    }

    /// <summary>Runs one bounded reminder pass and is public for deterministic integration tests.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NestyStayDbContext>();
        var now = timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var latest = today.AddDays(ReminderWindows.Max());
        var rows = await (from document in db.MilestoneManagerDocuments.AsNoTracking()
                          join manager in db.MilestoneUsers.AsNoTracking() on document.ManagerUserId equals manager.Id
                          where !document.IsDeleted && !document.IsArchived && document.ExpiresOn.HasValue && document.ExpiresOn.Value <= latest && document.ExpiresOn.Value >= today.AddDays(-1) && manager.Status == "Active" && manager.Email != ""
                          select new { document.Id, document.Title, document.ExpiresOn, manager.Email })
            .Take(500)
            .ToListAsync(cancellationToken);

        var queued = 0;
        foreach (var row in rows)
        {
            if (row.ExpiresOn is not { } expiresOn) continue;
            var days = expiresOn.DayNumber - today.DayNumber;
            var window = days <= 0 ? 0 : days <= 1 ? 1 : days <= 7 ? 7 : days <= 30 ? 30 : -1;
            if (window < 0) continue;
            var windowKey = window == 0 ? "expired" : $"{window}d";
            var expiryMessage = days < 0 ? $"expired {Math.Abs(days)} day(s) ago" : days == 0 ? "expires today" : $"expires in {days} day(s)";
            var idempotencyKey = $"pm-document-expiry:{row.Id:N}:{expiresOn:yyyyMMdd}:{windowKey}";
            if (await db.NotificationQueue.AsNoTracking().AnyAsync(item => item.Channel == "Email" && item.IdempotencyKey == idempotencyKey && !item.IsDeleted, cancellationToken)) continue;
            try
            {
                await emailSender.QueueAsync(new EmailMessage(
                    row.Email,
                    "A NestyStay document needs attention",
                    $"The document {row.Title} in your property-management workspace {expiryMessage}. Review it in NestyStay before the deadline.",
                    CorrelationId: row.Id,
                    TemplateKey: "document-expiry",
                    IdempotencyKey: idempotencyKey,
                    TemplateValues: new Dictionary<string, string>
                    {
                        ["documentTitle"] = row.Title,
                        ["expiryMessage"] = expiryMessage
                    }), cancellationToken);
                queued++;
            }
            catch (ArgumentException)
            {
                // A malformed legacy address must not prevent reminders for
                // the rest of the manager portfolio.
            }
        }

        return queued;
    }
}
