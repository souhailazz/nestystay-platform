using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NestyStay.Api.Auth;
using NestyStay.Application.PhaseOne;
using NestyStay.Infrastructure.Persistence;
using NestyStay.Infrastructure.Persistence.Milestones;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/properties/{propertyId:guid}/calendar")]
public sealed class CalendarController(
    NestyStayDbContext db,
    IPhaseOneStore phaseOneStore,
    IResourceAuthorizationService authorization,
    IHttpClientFactory httpClientFactory,
    TimeProvider timeProvider) : ControllerBase
{
    [Authorize(Roles = "Host")]
    [HttpGet("feeds")]
    public async Task<ActionResult<IReadOnlyList<CalendarFeedDto>>> GetFeeds(Guid propertyId, CancellationToken cancellationToken)
    {
        var hostUserId = RequireOwnedProperty(propertyId);
        var feeds = await db.MilestoneCalendarFeeds.AsNoTracking()
            .Where(feed => feed.PropertyId == propertyId && feed.HostUserId == hostUserId && !feed.IsDeleted)
            .OrderByDescending(feed => feed.CreatedAt)
            .Select(feed => ToDto(feed, 0))
            .ToListAsync(cancellationToken);
        return Ok(feeds);
    }

    [Authorize(Roles = "Host")]
    [HttpPost("feeds")]
    public async Task<ActionResult<CalendarFeedDto>> ConnectFeed(Guid propertyId, ConnectCalendarFeedRequest request, CancellationToken cancellationToken)
    {
        var hostUserId = RequireOwnedProperty(propertyId);
        var url = ValidateFeedUrl(request.FeedUrl);
        var now = timeProvider.GetUtcNow();
        var existing = await db.MilestoneCalendarFeeds.SingleOrDefaultAsync(
            feed => feed.PropertyId == propertyId && feed.HostUserId == hostUserId && feed.FeedUrl == url && !feed.IsDeleted,
            cancellationToken);
        if (existing is not null)
        {
            existing.Status = "Connected";
            existing.LastError = null;
            existing.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
            return Ok(ToDto(existing, await db.MilestoneCalendarBlocks.CountAsync(block => block.FeedId == existing.Id && !block.IsDeleted, cancellationToken)));
        }

        var entity = new MilestoneCalendarFeed
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            HostUserId = hostUserId,
            FeedUrl = url,
            Status = "Connected",
            CreatedAt = now,
            UpdatedAt = now
        };
        db.MilestoneCalendarFeeds.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(entity, 0));
    }

    [Authorize(Roles = "Host")]
    [HttpPost("feeds/{feedId:guid}/sync")]
    public async Task<ActionResult<CalendarFeedDto>> SyncFeed(Guid propertyId, Guid feedId, SyncCalendarFeedRequest request, CancellationToken cancellationToken)
    {
        var hostUserId = RequireOwnedProperty(propertyId);
        var feed = await db.MilestoneCalendarFeeds.SingleOrDefaultAsync(
            item => item.Id == feedId && item.PropertyId == propertyId && item.HostUserId == hostUserId && !item.IsDeleted,
            cancellationToken);
        if (feed is null) return NotFound();

        var now = timeProvider.GetUtcNow();
        feed.LastSyncAttemptAt = now;
        var syncEvent = new MilestoneCalendarSyncEvent
        {
            Id = Guid.NewGuid(), FeedId = feed.Id, PropertyId = propertyId, Status = "Started", StartedAt = now,
            CreatedAt = now, UpdatedAt = now, CreatedByUserId = hostUserId, UpdatedByUserId = hostUserId
        };
        db.MilestoneCalendarSyncEvents.Add(syncEvent);
        try
        {
            string ics;
            if (!string.IsNullOrWhiteSpace(request.IcsContent))
            {
                ics = request.IcsContent;
            }
            else
            {
                using var httpRequest = new HttpRequestMessage(HttpMethod.Get, feed.FeedUrl);
                if (!string.IsNullOrWhiteSpace(feed.ETag)) httpRequest.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(feed.ETag));
                if (feed.LastModifiedAt is not null) httpRequest.Headers.IfModifiedSince = feed.LastModifiedAt;
                using var response = await httpClientFactory.CreateClient().SendAsync(httpRequest, cancellationToken);
                if (response.StatusCode == HttpStatusCode.NotModified)
                {
                    feed.Status = "Healthy";
                    feed.LastSyncAt = now;
                    feed.NextSyncAt = now.AddMinutes(15);
                    feed.LastError = null;
                    syncEvent.Status = "NotModified";
                    syncEvent.CompletedAt = now;
                    syncEvent.UpdatedAt = now;
                    await db.SaveChangesAsync(cancellationToken);
                    return Ok(ToDto(feed, await db.MilestoneCalendarBlocks.CountAsync(block => block.FeedId == feed.Id && !block.IsDeleted, cancellationToken)));
                }
                response.EnsureSuccessStatusCode();
                if (response.Headers.ETag is not null) feed.ETag = response.Headers.ETag.Tag;
                if (response.Content.Headers.LastModified is not null) feed.LastModifiedAt = response.Content.Headers.LastModified;
                ics = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            var blocks = ParseEvents(ics!, feed.Id, propertyId, now);
            if (blocks.Count > 5000) throw new InvalidOperationException("Calendar feed contains too many events.");
            var oldBlocks = await db.MilestoneCalendarBlocks.Where(block => block.FeedId == feed.Id && !block.IsDeleted).ToListAsync(cancellationToken);
            db.MilestoneCalendarBlocks.RemoveRange(oldBlocks);
            db.MilestoneCalendarBlocks.AddRange(blocks);
            feed.Status = "Healthy";
            feed.LastSyncAt = now;
            feed.NextSyncAt = now.AddMinutes(15);
            feed.LastError = null;
            feed.UpdatedAt = now;
            syncEvent.Status = "Healthy";
            syncEvent.BlockCount = blocks.Count;
            syncEvent.CompletedAt = now;
            syncEvent.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
            return Ok(ToDto(feed, blocks.Count));
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or FormatException or InvalidOperationException)
        {
            feed.Status = "Error";
            feed.LastError = exception.Message.Length > 500 ? exception.Message[..500] : exception.Message;
            feed.UpdatedAt = now;
            syncEvent.Status = "Error";
            syncEvent.Error = feed.LastError;
            syncEvent.CompletedAt = now;
            syncEvent.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
            return UnprocessableEntity(ToDto(feed, await db.MilestoneCalendarBlocks.CountAsync(block => block.FeedId == feed.Id && !block.IsDeleted, cancellationToken)));
        }
    }

    [Authorize(Roles = "Host")]
    [HttpDelete("feeds/{feedId:guid}")]
    public async Task<IActionResult> DisconnectFeed(Guid propertyId, Guid feedId, CancellationToken cancellationToken)
    {
        var hostUserId = RequireOwnedProperty(propertyId);
        var feed = await db.MilestoneCalendarFeeds.SingleOrDefaultAsync(item => item.Id == feedId && item.PropertyId == propertyId && item.HostUserId == hostUserId && !item.IsDeleted, cancellationToken);
        if (feed is null) return NotFound();
        feed.IsDeleted = true;
        feed.Status = "Disconnected";
        feed.UpdatedAt = timeProvider.GetUtcNow();
        var blocks = await db.MilestoneCalendarBlocks.Where(item => item.FeedId == feedId && !item.IsDeleted).ToListAsync(cancellationToken);
        foreach (var block in blocks) { block.IsDeleted = true; block.UpdatedAt = feed.UpdatedAt; }
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Host")]
    [HttpGet("feeds/{feedId:guid}/history")]
    public async Task<ActionResult<IReadOnlyList<CalendarSyncEventDto>>> GetHistory(Guid propertyId, Guid feedId, CancellationToken cancellationToken)
    {
        var hostUserId = RequireOwnedProperty(propertyId);
        var exists = await db.MilestoneCalendarFeeds.AnyAsync(item => item.Id == feedId && item.PropertyId == propertyId && item.HostUserId == hostUserId, cancellationToken);
        if (!exists) return NotFound();
        var history = await db.MilestoneCalendarSyncEvents.AsNoTracking().Where(item => item.FeedId == feedId && !item.IsDeleted).OrderByDescending(item => item.StartedAt).Take(50).Select(item => new CalendarSyncEventDto(item.Id, item.Status, item.BlockCount, item.Error, item.StartedAt, item.CompletedAt)).ToListAsync(cancellationToken);
        return Ok(history);
    }

    [Authorize(Roles = "Host")]
    [HttpGet("export.ics")]
    public async Task<IActionResult> Export(Guid propertyId, CancellationToken cancellationToken)
    {
        var hostUserId = RequireOwnedProperty(propertyId);
        var blocks = await db.MilestoneCalendarBlocks.AsNoTracking()
            .Where(block => block.PropertyId == propertyId && !block.IsDeleted)
            .OrderBy(block => block.StartsOn)
            .ToListAsync(cancellationToken);
        var bookings = phaseOneStore.GetBookings().Where(booking => booking.PropertyId == propertyId && booking.HostUserId == hostUserId && !booking.Status.Contains("Rejected", StringComparison.OrdinalIgnoreCase) && !booking.Status.Contains("Cancelled", StringComparison.OrdinalIgnoreCase));
        var lines = new List<string> { "BEGIN:VCALENDAR", "VERSION:2.0", "PRODID:-//NestyStay//Calendar//EN", "CALSCALE:GREGORIAN" };
        foreach (var block in blocks)
            AddEvent(lines, $"feed-{block.Id:N}", block.StartsOn, block.EndsOn, block.Summary);
        foreach (var booking in bookings)
            AddEvent(lines, $"booking-{booking.Id:N}", booking.CheckIn, booking.CheckOut, "NestyStay booking");
        lines.Add("END:VCALENDAR");
        return Content(string.Join("\r\n", lines) + "\r\n", "text/calendar; charset=utf-8");
    }

    private Guid RequireOwnedProperty(Guid propertyId)
    {
        var host = authorization.RequireHost();
        var property = phaseOneStore.GetProperty(propertyId);
        if (property is null || property.HostUserId != host)
            throw new UnauthorizedAccessException("Calendar is not available for this property.");
        return host;
    }

    internal static string ValidateFeedUrl(string value)
    {
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") || string.IsNullOrWhiteSpace(uri.Host))
            throw new InvalidOperationException("Calendar feed must be an HTTP(S) URL.");
        if (IPAddress.TryParse(uri.Host, out var address) && (IPAddress.IsLoopback(address) || address.IsPrivateNetwork()))
            throw new InvalidOperationException("Private calendar feed addresses are not allowed.");
        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Private calendar feed addresses are not allowed.");
        return uri.ToString();
    }

    internal static List<MilestoneCalendarBlock> ParseEvents(string ics, Guid feedId, Guid propertyId, DateTimeOffset now)
    {
        var events = new List<MilestoneCalendarBlock>();
        string? uid = null;
        string? summary = null;
        DateOnly? start = null;
        DateOnly? end = null;
        var lines = UnfoldIcsLines(ics);
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Equals("BEGIN:VEVENT", StringComparison.OrdinalIgnoreCase)) { uid = null; summary = null; start = null; end = null; continue; }
            if (line.Equals("END:VEVENT", StringComparison.OrdinalIgnoreCase))
            {
                if (start is not null && end is not null && end > start)
                    events.Add(new MilestoneCalendarBlock { Id = Guid.NewGuid(), FeedId = feedId, PropertyId = propertyId, ExternalId = uid ?? Guid.NewGuid().ToString("N"), StartsOn = start.Value, EndsOn = end.Value, Summary = string.IsNullOrWhiteSpace(summary) ? "Unavailable" : summary[..Math.Min(summary.Length, 200)], CreatedAt = now, UpdatedAt = now });
                continue;
            }
            var separator = line.IndexOf(':');
            if (separator <= 0) continue;
            var key = line[..separator].Split(';')[0].ToUpperInvariant();
            var value = line[(separator + 1)..].Trim();
            if (key == "UID") uid = value;
            else if (key == "SUMMARY") summary = value.Replace("\\,", ",", StringComparison.Ordinal);
            else if (key == "DTSTART") start = ParseDate(value);
            else if (key == "DTEND") end = ParseDate(value);
        }
        return events;
    }

    private static DateOnly ParseDate(string value)
    {
        var dateValue = value.Length >= 8 ? value[..8] : value;
        return DateOnly.ParseExact(dateValue, "yyyyMMdd", CultureInfo.InvariantCulture);
    }

    private static IReadOnlyList<string> UnfoldIcsLines(string ics)
    {
        var unfolded = new List<string>();
        foreach (var line in ics.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n'))
        {
            if (line.StartsWith(' ') || line.StartsWith('\t'))
            {
                if (unfolded.Count > 0) unfolded[^1] += line[1..];
            }
            else unfolded.Add(line);
        }
        return unfolded;
    }

    private static void AddEvent(List<string> lines, string uid, DateOnly start, DateOnly end, string summary)
    {
        lines.Add("BEGIN:VEVENT");
        lines.Add($"UID:{uid}");
        lines.Add($"DTSTART;VALUE=DATE:{start:yyyyMMdd}");
        lines.Add($"DTEND;VALUE=DATE:{end:yyyyMMdd}");
        lines.Add($"SUMMARY:{summary.Replace(",", "\\,", StringComparison.Ordinal)}");
        lines.Add("END:VEVENT");
    }

    private static CalendarFeedDto ToDto(MilestoneCalendarFeed feed, int blockCount) => new(feed.Id, feed.PropertyId, feed.FeedUrl, feed.Status, feed.LastSyncAttemptAt, feed.LastSyncAt, feed.LastError, blockCount, feed.NextSyncAt, feed.ETag);
}

public sealed record ConnectCalendarFeedRequest(string FeedUrl);
public sealed record SyncCalendarFeedRequest(string? IcsContent = null);
public sealed record CalendarFeedDto(Guid Id, Guid PropertyId, string FeedUrl, string Status, DateTimeOffset? LastSyncAttemptAt, DateTimeOffset? LastSyncAt, string? LastError, int BlockCount, DateTimeOffset? NextSyncAt = null, string? ETag = null);
public sealed record CalendarSyncEventDto(Guid Id, string Status, int BlockCount, string? Error, DateTimeOffset StartedAt, DateTimeOffset? CompletedAt);

internal static class IpAddressExtensions
{
    public static bool IsPrivateNetwork(this IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
               (bytes[0] == 10 || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) || (bytes[0] == 192 && bytes[1] == 168) || (bytes[0] == 169 && bytes[1] == 254));
    }
}
