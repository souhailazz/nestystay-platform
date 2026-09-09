using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NestyStay.Api.Auth;
using NestyStay.Api.Configuration;
using NestyStay.Application.PhaseOne;
using NestyStay.Application.PhaseTwo;
using NestyStay.Domain;
using NestyStay.Infrastructure.Persistence;
using NestyStay.Infrastructure.Persistence.Milestones;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/properties")]
public sealed class PropertiesController(
    IPhaseOneStore phaseOneStore,
    IPhaseTwoStore phaseTwoStore,
    IResourceAuthorizationService authorization,
    NestyStayDbContext db) : ControllerBase
{
    [HttpGet]
    public IActionResult GetProperties() => Ok(phaseOneStore.GetProperties());

    [Authorize(Roles = "Host")]
    [HttpGet("owned")]
    public IActionResult GetOwnedProperties()
    {
        var hostUserId = authorization.RequireHost();
        return Ok(phaseOneStore.GetProperties(hostUserId));
    }

    [Authorize(Roles = "Host")]
    [HttpPost("{id:guid}/duplicate")]
    public async Task<IActionResult> DuplicateProperty(Guid id, [FromBody] DuplicatePropertyRequest? request, CancellationToken cancellationToken)
    {
        var store = phaseOneStore as IPropertyEnhancementStore
            ?? throw new InvalidOperationException("Property enhancement store is unavailable.");
        return Ok(await store.DuplicatePropertyAsync(authorization.RequireHost(), id, request?.Title, cancellationToken));
    }

    [Authorize(Roles = "Host")]
    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> PublishProperty(Guid id, CancellationToken cancellationToken)
    {
        var store = phaseOneStore as IPropertyEnhancementStore
            ?? throw new InvalidOperationException("Property enhancement store is unavailable.");
        return Ok(await store.PublishPropertyAsync(authorization.RequireHost(), id, cancellationToken));
    }

    [Authorize(Roles = "Host")]
    [HttpPost("bulk/archive")]
    public async Task<IActionResult> BulkArchiveProperties(BulkPropertyArchiveRequest request, CancellationToken cancellationToken)
    {
        var store = phaseOneStore as IPropertyEnhancementStore
            ?? throw new InvalidOperationException("Property enhancement store is unavailable.");
        return Ok(await store.BulkArchivePropertiesAsync(authorization.RequireHost(), request.PropertyIds, request.IsArchived, cancellationToken));
    }

    [Authorize(Roles = "Host")]
    [HttpPost("bulk/edit/preview")]
    public async Task<ActionResult<BulkPropertyEditPreviewDto>> PreviewBulkEdit(BulkPropertyEditRequest request, CancellationToken cancellationToken)
    {
        var store = phaseOneStore as IPropertyEnhancementStore
            ?? throw new InvalidOperationException("Property enhancement store is unavailable.");
        return Ok(await store.PreviewBulkPropertyEditAsync(authorization.RequireHost(), request, cancellationToken));
    }

    [Authorize(Roles = "Host")]
    [HttpPost("bulk/edit")]
    public async Task<ActionResult<IReadOnlyList<PropertyListingDto>>> BulkEdit(BulkPropertyEditRequest request, CancellationToken cancellationToken)
    {
        var store = phaseOneStore as IPropertyEnhancementStore
            ?? throw new InvalidOperationException("Property enhancement store is unavailable.");
        return Ok(await store.BulkEditPropertiesAsync(authorization.RequireHost(), request, cancellationToken));
    }

    [HttpGet("{id:guid}/availability")]
    public async Task<ActionResult<PropertyAvailabilityDto>> GetAvailability(Guid id, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    {
        if (phaseOneStore.GetProperty(id) is null) return NotFound();
        var start = from ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var end = to ?? start.AddDays(60);
        if (end <= start || end.DayNumber - start.DayNumber > 366) return BadRequest("Availability range must be between 1 and 366 days.");
        var bookings = phaseOneStore.GetBookings().Where(item => item.PropertyId == id && !item.Status.Equals("REJECTED", StringComparison.OrdinalIgnoreCase) && !item.Status.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase)).ToList();
        var blocks = await db.MilestoneCalendarBlocks.AsNoTracking().Where(item => item.PropertyId == id && !item.IsDeleted && item.EndsOn > start && item.StartsOn < end).ToListAsync(cancellationToken);
        var days = Enumerable.Range(0, end.DayNumber - start.DayNumber).Select(offset =>
        {
            var date = start.AddDays(offset);
            var booking = bookings.FirstOrDefault(item => item.CheckIn <= date && item.CheckOut > date);
            if (booking is not null)
            {
                var held = booking.Status.Equals("PENDING", StringComparison.OrdinalIgnoreCase);
                return new PropertyAvailabilityDayDto(date, held ? "HELD" : "BOOKED", "Booking", held ? "Held while verification completes" : "Confirmed booking");
            }
            var block = blocks.FirstOrDefault(item => item.StartsOn <= date && item.EndsOn > date);
            return block is null
                ? new PropertyAvailabilityDayDto(date, "AVAILABLE", "Available")
                : new PropertyAvailabilityDayDto(date, "BLOCKED", "ExternalCalendar", block.Summary);
        }).ToList();
        return Ok(new PropertyAvailabilityDto(id, start, end, days));
    }

    [Authorize(Roles = "Host")]
    [HttpGet("{id:guid}/revisions")]
    public async Task<IActionResult> GetPropertyRevisions(Guid id, CancellationToken cancellationToken)
    {
        var store = phaseOneStore as IPropertyEnhancementStore
            ?? throw new InvalidOperationException("Property enhancement store is unavailable.");
        return Ok(await store.GetPropertyRevisionsAsync(authorization.RequireHost(), id, cancellationToken));
    }

    [Authorize(Roles = "Host")]
    [HttpPost("{id:guid}/revisions/{revisionId:guid}/restore")]
    public async Task<IActionResult> RestorePropertyRevision(Guid id, Guid revisionId, CancellationToken cancellationToken)
    {
        var store = phaseOneStore as IPropertyEnhancementStore
            ?? throw new InvalidOperationException("Property enhancement store is unavailable.");
        return Ok(await store.RestorePropertyRevisionAsync(authorization.RequireHost(), id, revisionId, cancellationToken));
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateProperty(CreatePropertyRequest request, CancellationToken cancellationToken)
    {
        var hostUserId = authorization.RequireHost();
        var hostName = request.HostName;
        var hostEmail = request.HostEmail;
        var activeBadge = request.BadgeLevel;
        try
        {
            var profile = await phaseOneStore.GetUserProfileAsync(hostUserId, cancellationToken);
            hostName = profile.DisplayName;
            hostEmail = profile.Email;
            activeBadge = phaseTwoStore.GetFeatureAccess("Host", hostUserId).ActiveLevel;
        }
        catch (UnauthorizedAccessException)
        {
            // Legacy test-only signed principals may not have a persisted profile.
            // Registered production sessions always take the profile/active-badge path above.
        }
        return Ok(await phaseOneStore.CreatePropertyAsync(
            request with
            {
                HostUserId = hostUserId,
                HostName = hostName,
                HostEmail = hostEmail,
                BadgeLevel = activeBadge
            },
            cancellationToken));
    }

    [Authorize(Roles = "Host")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProperty(Guid id, UpdatePropertyRequest request, CancellationToken cancellationToken)
    {
        var hostUserId = authorization.RequireHost();
        var hostName = request.HostName;
        var hostEmail = request.HostEmail;
        var activeBadge = request.BadgeLevel;
        try
        {
            var profile = await phaseOneStore.GetUserProfileAsync(hostUserId, cancellationToken);
            hostName = profile.DisplayName;
            hostEmail = profile.Email;
            activeBadge = phaseTwoStore.GetFeatureAccess("Host", hostUserId).ActiveLevel;
        }
        catch (UnauthorizedAccessException)
        {
            // See the create endpoint: this is only for legacy test-only principals.
        }
        return Ok(await phaseOneStore.UpdatePropertyAsync(
            hostUserId,
            id,
            request with { HostName = hostName, HostEmail = hostEmail, BadgeLevel = activeBadge },
            cancellationToken));
    }

    [Authorize(Roles = "Host")]
    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> ArchiveProperty(Guid id, CancellationToken cancellationToken)
    {
        var hostUserId = authorization.RequireHost();
        return Ok(await phaseOneStore.ArchivePropertyAsync(hostUserId, id, true, cancellationToken));
    }

    [Authorize(Roles = "Host")]
    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> RestoreProperty(Guid id, CancellationToken cancellationToken)
    {
        var hostUserId = authorization.RequireHost();
        return Ok(await phaseOneStore.ArchivePropertyAsync(hostUserId, id, false, cancellationToken));
    }

    [Authorize(Roles = "Host")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProperty(Guid id, CancellationToken cancellationToken)
    {
        var hostUserId = authorization.RequireHost();
        await phaseOneStore.DeletePropertyAsync(hostUserId, id, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Host")]
    [HttpPost("{id:guid}/photos/uploads")]
    public async Task<IActionResult> PreparePropertyPhotoUpload(Guid id, PreparePropertyPhotoUploadRequest request, CancellationToken cancellationToken)
    {
        var hostUserId = authorization.RequireHost();
        return Ok(await phaseOneStore.PreparePropertyPhotoUploadAsync(hostUserId, id, request, cancellationToken));
    }

    [Authorize(Roles = "Host")]
    [HttpPut("{id:guid}/photos/{photoId:guid}/content")]
    [EnableRateLimiting(RateLimitPolicies.Upload)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadPropertyPhotoContent(Guid id, Guid photoId, CancellationToken cancellationToken)
    {
        var hostUserId = authorization.RequireHost();
        return Ok(await phaseOneStore.UploadPropertyPhotoContentAsync(
            hostUserId,
            id,
            photoId,
            Request.ContentType ?? string.Empty,
            Request.ContentLength ?? 0,
            Request.Body,
            cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public IActionResult GetProperty(Guid id)
    {
        var property = phaseOneStore.GetProperty(id);
        return property is null ? NotFound() : Ok(property);
    }

}

public sealed record DuplicatePropertyRequest(string? Title);
public sealed record BulkPropertyArchiveRequest(IReadOnlyCollection<Guid> PropertyIds, bool IsArchived = true);
