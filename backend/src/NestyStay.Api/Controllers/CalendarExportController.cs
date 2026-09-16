using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NestyStay.Api.Services;
using NestyStay.Application.PhaseOne;
using NestyStay.Domain;
using NestyStay.Infrastructure.Persistence;

namespace NestyStay.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/calendar/export")]
public sealed class CalendarExportController(
    NestyStayDbContext db,
    IPhaseOneStore phaseOneStore,
    TimeProvider timeProvider) : ControllerBase
{
    [HttpGet("{token}.ics")]
    public async Task<IActionResult> Export(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length < 32) return NotFound();
        var hash = CalendarController.HashExportToken(token);
        var exportToken = await db.MilestoneCalendarExportTokens.AsNoTracking()
            .SingleOrDefaultAsync(item => item.TokenHash == hash && item.RevokedAt == null && !item.IsDeleted, cancellationToken);
        if (exportToken is null) return NotFound();

        var blocks = await db.MilestoneCalendarBlocks.AsNoTracking()
            .Where(item => item.PropertyId == exportToken.PropertyId && !item.IsDeleted)
            .ToListAsync(cancellationToken);
        var manualBlocks = await db.MilestoneCalendarManualBlocks.AsNoTracking()
            .Where(item => item.PropertyId == exportToken.PropertyId && item.HostUserId == exportToken.HostUserId && item.Status == "ACTIVE" && !item.IsDeleted)
            .ToListAsync(cancellationToken);
        var bookings = phaseOneStore.GetBookings()
            .Where(item => item.PropertyId == exportToken.PropertyId && item.HostUserId == exportToken.HostUserId && item.Status is not ("REJECTED" or "CANCELLED"));

        var events = blocks.Select(item => new CalendarIcsEvent($"external-{item.FeedId:N}-{item.ExternalId}", item.StartsOn, item.EndsOn, item.Summary, "CONFIRMED", item.UpdatedAt))
            .Concat(manualBlocks.Select(item => new CalendarIcsEvent($"manual-{item.Id:N}", item.StartsOn, item.EndsOn, $"NestyStay manual block: {item.Reason}", "CONFIRMED", item.UpdatedAt)))
            .Concat(bookings.Select(item => new CalendarIcsEvent($"booking-{item.Id:N}", item.CheckIn, item.CheckOut, "NestyStay booking", "CONFIRMED")));

        Response.Headers.CacheControl = "private, max-age=300";
        return Content(CalendarIcsBuilder.Build(events, timeProvider.GetUtcNow()), "text/calendar; charset=utf-8");
    }
}
