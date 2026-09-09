using System.Globalization;
using System.Net;
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
        try
        {
            var ics = string.IsNullOrWhiteSpace(request.IcsContent)
                ? await httpClientFactory.CreateClient().GetStringAsync(feed.FeedUrl, cancellationToken)
                : request.IcsContent;
            var blocks = ParseEvents(ics!, feed.Id, propertyId, now);
            var oldBlocks = await db.MilestoneCalendarBlocks.Where(block => block.FeedId == feed.Id && !block.IsDeleted).ToListAsync(cancellationToken);
            db.MilestoneCalendarBlocks.RemoveRange(oldBlocks);
            db.MilestoneCalendarBlocks.AddRange(blocks);
            feed.Status = "Healthy";
            feed.LastSyncAt = now;
            feed.LastError = null;
            feed.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
            return Ok(ToDto(feed, blocks.Count));
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or FormatException or InvalidOperationException)
        {
            feed.Status = "Error";
            feed.LastError = exception.Message.Length > 500 ? exception.Message[..500] : exception.Message;
            feed.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
            return UnprocessableEntity(ToDto(feed, await db.MilestoneCalendarBlocks.CountAsync(block => block.FeedId == feed.Id && !block.IsDeleted, cancellationToken)));
        }
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

    private static string ValidateFeedUrl(string value)
    {
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") || string.IsNullOrWhiteSpace(uri.Host))
            throw new InvalidOperationException("Calendar feed must be an HTTP(S) URL.");
        if (IPAddress.TryParse(uri.Host, out var address) && (IPAddress.IsLoopback(address) || address.IsPrivateNetwork()))
            throw new InvalidOperationException("Private calendar feed addresses are not allowed.");
        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Private calendar feed addresses are not allowed.");
        return uri.ToString();
    }

    private static List<MilestoneCalendarBlock> ParseEvents(string ics, Guid feedId, Guid propertyId, DateTimeOffset now)
    {
        var events = new List<MilestoneCalendarBlock>();
        string? uid = null;
        string? summary = null;
        DateOnly? start = null;
        DateOnly? end = null;
        foreach (var raw in ics.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
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

    private static void AddEvent(List<string> lines, string uid, DateOnly start, DateOnly end, string summary)
    {
        lines.Add("BEGIN:VEVENT");
        lines.Add($"UID:{uid}");
        lines.Add($"DTSTART;VALUE=DATE:{start:yyyyMMdd}");
        lines.Add($"DTEND;VALUE=DATE:{end:yyyyMMdd}");
        lines.Add($"SUMMARY:{summary.Replace(",", "\\,", StringComparison.Ordinal)}");
        lines.Add("END:VEVENT");
    }

    private static CalendarFeedDto ToDto(MilestoneCalendarFeed feed, int blockCount) => new(feed.Id, feed.PropertyId, feed.FeedUrl, feed.Status, feed.LastSyncAttemptAt, feed.LastSyncAt, feed.LastError, blockCount);
}

public sealed record ConnectCalendarFeedRequest(string FeedUrl);
public sealed record SyncCalendarFeedRequest(string? IcsContent = null);
public sealed record CalendarFeedDto(Guid Id, Guid PropertyId, string FeedUrl, string Status, DateTimeOffset? LastSyncAttemptAt, DateTimeOffset? LastSyncAt, string? LastError, int BlockCount);

internal static class IpAddressExtensions
{
    public static bool IsPrivateNetwork(this IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
               (bytes[0] == 10 || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) || (bytes[0] == 192 && bytes[1] == 168) || (bytes[0] == 169 && bytes[1] == 254));
    }
}
