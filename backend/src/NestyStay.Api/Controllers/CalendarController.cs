using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NestyStay.Api.Auth;
using NestyStay.Api.Services;
using NestyStay.Application.PhaseOne;
using NestyStay.Domain;
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
    [HttpGet("blocks")]
    public async Task<ActionResult<IReadOnlyList<CalendarManualBlockDto>>> GetManualBlocks(Guid propertyId, CancellationToken cancellationToken)
    {
        var hostUserId = RequireOwnedProperty(propertyId);
        var blocks = await db.MilestoneCalendarManualBlocks.AsNoTracking()
            .Where(block => block.PropertyId == propertyId && block.HostUserId == hostUserId && !block.IsDeleted)
            .OrderBy(block => block.StartsOn)
            .Select(block => ToDto(block))
            .ToListAsync(cancellationToken);
        return Ok(blocks);
    }

    [Authorize(Roles = "Host")]
    [HttpPost("blocks")]
    public async Task<ActionResult<CalendarManualBlockDto>> CreateManualBlock(Guid propertyId, CreateCalendarManualBlockRequest request, CancellationToken cancellationToken)
    {
        var hostUserId = RequireOwnedProperty(propertyId);
        ValidateDateRange(request.StartsOn, request.EndsOn);
        var conflicts = await HasCalendarConflictAsync(propertyId, request.StartsOn, request.EndsOn, null, cancellationToken);
        if (conflicts) return Conflict("The requested dates overlap an existing booking or calendar block.");
        var now = timeProvider.GetUtcNow();
        var block = new MilestoneCalendarManualBlock
        {
            Id = Guid.NewGuid(), PropertyId = propertyId, HostUserId = hostUserId,
            StartsOn = request.StartsOn, EndsOn = request.EndsOn,
            Reason = RequireText(request.Reason, "Block reason"), Status = "ACTIVE",
            CreatedAt = now, UpdatedAt = now, CreatedByUserId = hostUserId, UpdatedByUserId = hostUserId
        };
        db.MilestoneCalendarManualBlocks.Add(block);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(block));
    }

    [Authorize(Roles = "Host")]
    [HttpPatch("blocks/{blockId:guid}")]
    public async Task<ActionResult<CalendarManualBlockDto>> UpdateManualBlock(Guid propertyId, Guid blockId, UpdateCalendarManualBlockRequest request, CancellationToken cancellationToken)
    {
        var hostUserId = RequireOwnedProperty(propertyId);
        var block = await db.MilestoneCalendarManualBlocks.SingleOrDefaultAsync(item => item.Id == blockId && item.PropertyId == propertyId && item.HostUserId == hostUserId && !item.IsDeleted, cancellationToken);
        if (block is null) return NotFound();
        ValidateDateRange(request.StartsOn, request.EndsOn);
        if (await HasCalendarConflictAsync(propertyId, request.StartsOn, request.EndsOn, blockId, cancellationToken)) return Conflict("The requested dates overlap an existing booking or calendar block.");
        block.StartsOn = request.StartsOn; block.EndsOn = request.EndsOn; block.Reason = RequireText(request.Reason, "Block reason"); block.UpdatedAt = timeProvider.GetUtcNow(); block.UpdatedByUserId = hostUserId;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(block));
    }

    [Authorize(Roles = "Host")]
    [HttpDelete("blocks/{blockId:guid}")]
    public async Task<IActionResult> DeleteManualBlock(Guid propertyId, Guid blockId, CancellationToken cancellationToken)
    {
        var hostUserId = RequireOwnedProperty(propertyId);
        var block = await db.MilestoneCalendarManualBlocks.SingleOrDefaultAsync(item => item.Id == blockId && item.PropertyId == propertyId && item.HostUserId == hostUserId && !item.IsDeleted, cancellationToken);
        if (block is null) return NotFound();
        block.IsDeleted = true; block.Status = "CANCELLED"; block.UpdatedAt = timeProvider.GetUtcNow(); block.UpdatedByUserId = hostUserId;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Host")]
    [HttpPost("export-token")]
    public async Task<ActionResult<CalendarExportTokenDto>> RotateExportToken(Guid propertyId, CancellationToken cancellationToken)
    {
        var hostUserId = RequireOwnedProperty(propertyId);
        var now = timeProvider.GetUtcNow();
        var current = await db.MilestoneCalendarExportTokens.SingleOrDefaultAsync(item => item.PropertyId == propertyId && item.HostUserId == hostUserId && item.RevokedAt == null, cancellationToken);
        if (current is not null) { current.RevokedAt = now; current.UpdatedAt = now; current.UpdatedByUserId = hostUserId; }
        var rawToken = CreateExportToken();
        var token = new MilestoneCalendarExportToken
        {
            Id = Guid.NewGuid(), PropertyId = propertyId, HostUserId = hostUserId,
            TokenHash = HashExportToken(rawToken), CreatedAt = now, UpdatedAt = now,
            CreatedByUserId = hostUserId, UpdatedByUserId = hostUserId
        };
        db.MilestoneCalendarExportTokens.Add(token);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new CalendarExportTokenDto(BuildExportUrl(rawToken), token.CreatedAt, null, true));
    }

    [Authorize(Roles = "Host")]
    [HttpDelete("export-token")]
    public async Task<IActionResult> RevokeExportToken(Guid propertyId, CancellationToken cancellationToken)
    {
        var hostUserId = RequireOwnedProperty(propertyId);
        var token = await db.MilestoneCalendarExportTokens.SingleOrDefaultAsync(item => item.PropertyId == propertyId && item.HostUserId == hostUserId && item.RevokedAt == null, cancellationToken);
        if (token is null) return NoContent();
        token.RevokedAt = timeProvider.GetUtcNow(); token.UpdatedAt = timeProvider.GetUtcNow(); token.UpdatedByUserId = hostUserId;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
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
                await CalendarFeedSafety.EnsurePublicDestinationAsync(feed.FeedUrl, cancellationToken);
                using var httpRequest = new HttpRequestMessage(HttpMethod.Get, feed.FeedUrl);
                if (!string.IsNullOrWhiteSpace(feed.ETag)) httpRequest.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(feed.ETag));
                if (feed.LastModifiedAt is not null) httpRequest.Headers.IfModifiedSince = feed.LastModifiedAt;
                using var response = await httpClientFactory.CreateClient("calendar-feed").SendAsync(httpRequest, cancellationToken);
                if ((int)response.StatusCode is >= 300 and <= 399)
                    throw new InvalidOperationException("Calendar feed redirects are not allowed.");
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
                if (response.Content.Headers.ContentLength is > CalendarFeedSafety.MaximumCalendarBytes)
                    throw new InvalidOperationException("Calendar feed is larger than the 5 MB safety limit.");
                if (response.Headers.ETag is not null) feed.ETag = response.Headers.ETag.Tag;
                if (response.Content.Headers.LastModified is not null) feed.LastModifiedAt = response.Content.Headers.LastModified;
                ics = await CalendarFeedSafety.ReadTextWithLimitAsync(response.Content, cancellationToken);
            }
            var blocks = ParseEvents(ics!, feed.Id, propertyId, now);
            if (blocks.Count > 5000) throw new InvalidOperationException("Calendar feed contains too many events.");
            await ApplyImportedBlocksAsync(db, feed, blocks, now, cancellationToken);
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
            feed.NextSyncAt = now.AddMinutes(5);
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
        var manualBlocks = await db.MilestoneCalendarManualBlocks.AsNoTracking()
            .Where(block => block.PropertyId == propertyId && block.HostUserId == hostUserId && !block.IsDeleted && block.Status == "ACTIVE")
            .OrderBy(block => block.StartsOn)
            .ToListAsync(cancellationToken);
        var bookings = phaseOneStore.GetBookings().Where(booking => booking.PropertyId == propertyId && booking.HostUserId == hostUserId && !booking.Status.Contains("Rejected", StringComparison.OrdinalIgnoreCase) && !booking.Status.Contains("Cancelled", StringComparison.OrdinalIgnoreCase));
        var events = blocks.Select(block => new CalendarIcsEvent($"external-{block.FeedId:N}-{block.ExternalId}", block.StartsOn, block.EndsOn, block.Summary, "CONFIRMED", block.UpdatedAt))
            .Concat(manualBlocks.Select(block => new CalendarIcsEvent($"manual-{block.Id:N}", block.StartsOn, block.EndsOn, $"NestyStay manual block: {block.Reason}", "CONFIRMED", block.UpdatedAt)))
            .Concat(bookings.Select(booking => new CalendarIcsEvent($"booking-{booking.Id:N}", booking.CheckIn, booking.CheckOut, "NestyStay booking", "CONFIRMED")))
            .ToList();
        return Content(CalendarIcsBuilder.Build(events, timeProvider.GetUtcNow()), "text/calendar; charset=utf-8");
    }

    private Guid RequireOwnedProperty(Guid propertyId)
    {
        var host = authorization.RequireHost();
        var property = phaseOneStore.GetProperties(host).SingleOrDefault(item => item.Id == propertyId);
        if (property is null || property.HostUserId != host)
            throw new UnauthorizedAccessException("Calendar is not available for this property.");
        return host;
    }

    public static string ValidateFeedUrl(string value) => CalendarFeedSafety.ValidateUrl(value);

    internal static List<MilestoneCalendarBlock> ParseEvents(string ics, Guid feedId, Guid propertyId, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(ics) || !ics.Contains("BEGIN:VCALENDAR", StringComparison.OrdinalIgnoreCase) || !ics.Contains("END:VCALENDAR", StringComparison.OrdinalIgnoreCase))
            throw new FormatException("Calendar feed is not a valid iCalendar document.");
        var events = new List<MilestoneCalendarBlock>();
        string? uid = null;
        string? summary = null;
        DateOnly? start = null;
        DateOnly? end = null;
        string? startTimeZone = null;
        string? endTimeZone = null;
        var cancelled = false;
        var lines = UnfoldIcsLines(ics);
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Equals("BEGIN:VEVENT", StringComparison.OrdinalIgnoreCase)) { uid = null; summary = null; start = null; end = null; startTimeZone = null; endTimeZone = null; cancelled = false; continue; }
            if (line.Equals("END:VEVENT", StringComparison.OrdinalIgnoreCase))
            {
                if (!cancelled && start is not null && end is not null && end > start)
                    events.Add(new MilestoneCalendarBlock { Id = Guid.NewGuid(), FeedId = feedId, PropertyId = propertyId, ExternalId = RequireText(uid, "Calendar event UID"), StartsOn = start.Value, EndsOn = end.Value, Summary = string.IsNullOrWhiteSpace(summary) ? "Unavailable" : summary[..Math.Min(summary.Length, 200)], CreatedAt = now, UpdatedAt = now });
                continue;
            }
            var separator = line.IndexOf(':');
            if (separator <= 0) continue;
            var property = line[..separator];
            var key = property.Split(';')[0].ToUpperInvariant();
            var value = line[(separator + 1)..].Trim();
            if (key == "UID") uid = value;
            else if (key == "SUMMARY") summary = UnescapeText(value);
            else if (key == "DTSTART") { startTimeZone = GetParameter(property, "TZID"); start = ParseDate(value, startTimeZone); }
            else if (key == "DTEND") { endTimeZone = GetParameter(property, "TZID"); end = ParseDate(value, endTimeZone); }
            else if (key == "STATUS") cancelled = value.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase);
        }
        return events.GroupBy(item => item.ExternalId, StringComparer.OrdinalIgnoreCase).Select(group => group.Last()).ToList();
    }

    private static DateOnly ParseDate(string value, string? timeZoneId = null)
    {
        var normalized = value.Trim();
        if (normalized.Length == 8 && normalized.All(char.IsDigit))
            return DateOnly.ParseExact(normalized[..8], "yyyyMMdd", CultureInfo.InvariantCulture);
        if (DateTime.TryParseExact(normalized, "yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var utcDateTime))
            return DateOnly.FromDateTime(utcDateTime);
        if (DateTime.TryParseExact(normalized, "yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var localDateTime))
        {
            if (!string.IsNullOrWhiteSpace(timeZoneId))
            {
                try
                {
                    var zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
                    localDateTime = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified), zone);
                }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
            }
            return DateOnly.FromDateTime(localDateTime);
        }
        throw new FormatException("Calendar event date is invalid.");
    }

    private static string? GetParameter(string property, string name) => property.Split(';').Skip(1)
        .Select(parameter => parameter.Split('=', 2))
        .Where(parts => parts.Length == 2 && parts[0].Equals(name, StringComparison.OrdinalIgnoreCase))
        .Select(parts => parts[1].Trim('"'))
        .FirstOrDefault();

    private static string UnescapeText(string value) => value
        .Replace("\\n", "\n", StringComparison.OrdinalIgnoreCase)
        .Replace("\\N", "\n", StringComparison.Ordinal)
        .Replace("\\,", ",", StringComparison.Ordinal)
        .Replace("\\;", ";", StringComparison.Ordinal)
        .Replace("\\\\", "\\", StringComparison.Ordinal);

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

    internal static async Task ApplyImportedBlocksAsync(NestyStayDbContext db, MilestoneCalendarFeed feed, IReadOnlyList<MilestoneCalendarBlock> imported, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var existing = await db.MilestoneCalendarBlocks.Where(block => block.FeedId == feed.Id).ToListAsync(cancellationToken);
        var byUid = existing.ToDictionary(block => block.ExternalId, StringComparer.OrdinalIgnoreCase);
        var seen = imported.Select(block => block.ExternalId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var old in existing.Where(block => !block.IsDeleted && !seen.Contains(block.ExternalId)))
        {
            old.IsDeleted = true; old.UpdatedAt = now;
        }
        foreach (var incoming in imported)
        {
            if (byUid.TryGetValue(incoming.ExternalId, out var current))
            {
                current.StartsOn = incoming.StartsOn; current.EndsOn = incoming.EndsOn; current.Summary = incoming.Summary; current.IsDeleted = false; current.UpdatedAt = now;
            }
            else
            {
                db.MilestoneCalendarBlocks.Add(incoming);
            }
        }
    }

    private async Task<bool> HasCalendarConflictAsync(Guid propertyId, DateOnly startsOn, DateOnly endsOn, Guid? ignoredBlockId, CancellationToken cancellationToken) =>
        await db.MilestoneBookings.AnyAsync(booking => booking.PropertyId == propertyId && !booking.IsDeleted && booking.Status != BookingStatus.Rejected && booking.Status != BookingStatus.Cancelled && booking.CheckIn < endsOn && startsOn < booking.CheckOut, cancellationToken) ||
        await db.MilestoneCalendarBlocks.AnyAsync(block => block.PropertyId == propertyId && !block.IsDeleted && block.StartsOn < endsOn && startsOn < block.EndsOn, cancellationToken) ||
        await db.MilestoneCalendarManualBlocks.AnyAsync(block => block.PropertyId == propertyId && !block.IsDeleted && block.Status == "ACTIVE" && block.Id != ignoredBlockId && block.StartsOn < endsOn && startsOn < block.EndsOn, cancellationToken);

    private static void ValidateDateRange(DateOnly startsOn, DateOnly endsOn)
    {
        if (endsOn <= startsOn || endsOn.DayNumber - startsOn.DayNumber > 366) throw new InvalidOperationException("Calendar block must be between 1 and 366 days.");
    }

    private static string RequireText(string? value, string label) => string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException($"{label} is required.") : value.Trim();
    private string BuildExportUrl(string rawToken) => $"{Request.Scheme}://{Request.Host}/api/calendar/export/{Uri.EscapeDataString(rawToken)}.ics";
    internal static string CreateExportToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    internal static string HashExportToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
    private static CalendarManualBlockDto ToDto(MilestoneCalendarManualBlock block) => new(block.Id, block.PropertyId, block.StartsOn, block.EndsOn, block.Reason, block.Status, "Manual");
    private static CalendarFeedDto ToDto(MilestoneCalendarFeed feed, int blockCount) => new(feed.Id, feed.PropertyId, feed.FeedUrl, feed.Status, feed.LastSyncAttemptAt, feed.LastSyncAt, feed.LastError, blockCount, feed.NextSyncAt, feed.ETag, ResolveChannel(feed.FeedUrl));
    private static string ResolveChannel(string url) => url.Contains("airbnb", StringComparison.OrdinalIgnoreCase) ? "Airbnb" : url.Contains("booking", StringComparison.OrdinalIgnoreCase) ? "Booking" : url.Contains("vrbo", StringComparison.OrdinalIgnoreCase) ? "VRBO" : "Custom";
}

public sealed record ConnectCalendarFeedRequest(string FeedUrl, string? Channel = null);
public sealed record SyncCalendarFeedRequest(string? IcsContent = null);
public sealed record CreateCalendarManualBlockRequest(DateOnly StartsOn, DateOnly EndsOn, string Reason);
public sealed record UpdateCalendarManualBlockRequest(DateOnly StartsOn, DateOnly EndsOn, string Reason);
public sealed record CalendarManualBlockDto(Guid Id, Guid PropertyId, DateOnly StartsOn, DateOnly EndsOn, string Reason, string Status, string SourceType);
public sealed record CalendarExportTokenDto(string Url, DateTimeOffset CreatedAt, DateTimeOffset? RevokedAt, bool IsActive);
public sealed record CalendarFeedDto(Guid Id, Guid PropertyId, string FeedUrl, string Status, DateTimeOffset? LastSyncAttemptAt, DateTimeOffset? LastSyncAt, string? LastError, int BlockCount, DateTimeOffset? NextSyncAt = null, string? ETag = null, string Channel = "Custom");
public sealed record CalendarSyncEventDto(Guid Id, string Status, int BlockCount, string? Error, DateTimeOffset StartedAt, DateTimeOffset? CompletedAt);
