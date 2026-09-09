using System.Net;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using NestyStay.Api.Controllers;
using NestyStay.Infrastructure.Persistence;
using NestyStay.Infrastructure.Persistence.Milestones;

namespace NestyStay.Api.Services;

/// <summary>
/// Pulls due external ICS feeds without requiring a host to keep the calendar
/// page open. A successful pull schedules the next attempt in 15 minutes;
/// failures remain visible on the feed and retry with a short backoff. Feed
/// blocks are replaced atomically only after the complete document parses.
/// </summary>
public sealed class CalendarSyncMaintenanceService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<CalendarSyncMaintenanceService> logger) : BackgroundService
{
    private static readonly TimeSpan Cadence = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan SuccessInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ErrorInterval = TimeSpan.FromMinutes(5);
    private const int BatchSize = 25;
    private const int MaximumCalendarBytes = 5 * 1024 * 1024;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(Cadence, timeProvider, stoppingToken);
                await SynchronizeDueFeedsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Calendar maintenance failed; due feeds will be retried on the next cadence.");
            }
        }
    }

    private async Task SynchronizeDueFeedsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NestyStayDbContext>();
        var clientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var now = timeProvider.GetUtcNow();
        var feeds = await db.MilestoneCalendarFeeds
            .Where(feed => !feed.IsDeleted && (feed.NextSyncAt == null || feed.NextSyncAt <= now))
            .OrderBy(feed => feed.NextSyncAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var feed in feeds)
        {
            await SynchronizeFeedAsync(db, clientFactory, feed, now, cancellationToken);
        }

        if (feeds.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Calendar maintenance processed {Count} due feed(s).", feeds.Count);
        }
    }

    private async Task SynchronizeFeedAsync(
        NestyStayDbContext db,
        IHttpClientFactory clientFactory,
        MilestoneCalendarFeed feed,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var syncEvent = new MilestoneCalendarSyncEvent
        {
            Id = Guid.NewGuid(),
            FeedId = feed.Id,
            PropertyId = feed.PropertyId,
            Status = "Started",
            StartedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = feed.HostUserId,
            UpdatedByUserId = feed.HostUserId
        };
        feed.LastSyncAttemptAt = now;
        feed.UpdatedAt = now;
        db.MilestoneCalendarSyncEvents.Add(syncEvent);

        try
        {
            CalendarController.ValidateFeedUrl(feed.FeedUrl);
            using var request = new HttpRequestMessage(HttpMethod.Get, feed.FeedUrl);
            if (!string.IsNullOrWhiteSpace(feed.ETag)) request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(feed.ETag));
            if (feed.LastModifiedAt is not null) request.Headers.IfModifiedSince = feed.LastModifiedAt;
            using var response = await clientFactory.CreateClient().SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                feed.Status = "Healthy";
                feed.LastSyncAt = now;
                feed.NextSyncAt = now.Add(SuccessInterval);
                feed.LastError = null;
                syncEvent.Status = "NotModified";
                syncEvent.BlockCount = await db.MilestoneCalendarBlocks.CountAsync(block => block.FeedId == feed.Id && !block.IsDeleted, cancellationToken);
                syncEvent.CompletedAt = now;
                syncEvent.UpdatedAt = now;
                return;
            }

            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is > MaximumCalendarBytes)
                throw new InvalidOperationException("Calendar feed is larger than the 5 MB safety limit.");
            if (response.Headers.ETag is not null) feed.ETag = response.Headers.ETag.Tag;
            if (response.Content.Headers.LastModified is not null) feed.LastModifiedAt = response.Content.Headers.LastModified;
            var ics = await response.Content.ReadAsStringAsync(cancellationToken);
            if (ics.Length > MaximumCalendarBytes) throw new InvalidOperationException("Calendar feed is larger than the 5 MB safety limit.");

            var blocks = CalendarController.ParseEvents(ics, feed.Id, feed.PropertyId, now);
            if (blocks.Count > 5000) throw new InvalidOperationException("Calendar feed contains too many events.");
            var oldBlocks = await db.MilestoneCalendarBlocks.Where(block => block.FeedId == feed.Id && !block.IsDeleted).ToListAsync(cancellationToken);
            db.MilestoneCalendarBlocks.RemoveRange(oldBlocks);
            db.MilestoneCalendarBlocks.AddRange(blocks);
            feed.Status = "Healthy";
            feed.LastSyncAt = now;
            feed.NextSyncAt = now.Add(SuccessInterval);
            feed.LastError = null;
            syncEvent.Status = "Healthy";
            syncEvent.BlockCount = blocks.Count;
            syncEvent.CompletedAt = now;
            syncEvent.UpdatedAt = now;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or FormatException or InvalidOperationException)
        {
            feed.Status = "Error";
            feed.LastError = exception.Message.Length > 500 ? exception.Message[..500] : exception.Message;
            feed.NextSyncAt = now.Add(ErrorInterval);
            syncEvent.Status = "Error";
            syncEvent.Error = feed.LastError;
            syncEvent.CompletedAt = now;
            syncEvent.UpdatedAt = now;
        }
    }
}
