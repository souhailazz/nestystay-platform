using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NestyStay.Api.Auth;
using NestyStay.Api.Configuration;
using NestyStay.Application.Admin;
using NestyStay.Application.PhaseOne;
using NestyStay.Application.PhaseTwo;
using NestyStay.Application.SpecCompletion;
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
    NestyStayDbContext db,
    IPrivilegedAuditStore auditStore) : ControllerBase
{
    [HttpGet]
    public IActionResult GetProperties(
        [FromQuery] string? search,
        [FromQuery] DateOnly? checkIn,
        [FromQuery] DateOnly? checkOut,
        [FromQuery] int adults = 1,
        [FromQuery] int children = 0)
    {
        if (checkIn.HasValue != checkOut.HasValue)
        {
            return BadRequest("Both check-in and check-out are required for date search.");
        }

        if (adults < 1 || children < 0)
        {
            return BadRequest("Guest counts are invalid.");
        }

        if (checkIn.HasValue && checkOut <= checkIn)
        {
            return BadRequest("Check-out must be after check-in.");
        }

        var normalizedSearch = search?.Trim();
        var properties = phaseOneStore.GetProperties()
            .Where(property => property.ModerationStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase))
            .Where(property => string.IsNullOrWhiteSpace(normalizedSearch) ||
                property.Title.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                property.Location.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                property.Country.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                property.Parish.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
            .Where(property => property.MaxGuests >= adults + children)
            .ToList();

        if (checkIn.HasValue && checkOut.HasValue)
        {
            var bookings = phaseOneStore.GetBookings()
                .Where(booking => !booking.Status.Equals("REJECTED", StringComparison.OrdinalIgnoreCase) && !booking.Status.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase))
                .ToList();
            properties = properties
                .Where(property => bookings.All(booking => booking.PropertyId != property.Id || booking.CheckIn >= checkOut.Value || checkIn.Value >= booking.CheckOut))
                .ToList();
        }

        return Ok(properties);
    }

    [Authorize(Policy = AdminAuthorizationPolicies.PropertyModeration)]
    [HttpGet("moderation")]
    public async Task<IActionResult> GetModerationQueue(CancellationToken cancellationToken)
    {
        var store = phaseOneStore as IPropertyModerationStore
            ?? throw new InvalidOperationException("Property moderation store is unavailable.");
        return Ok(await store.GetModerationQueueAsync(cancellationToken));
    }

    [Authorize(Policy = AdminAuthorizationPolicies.PropertyModeration)]
    [HttpPost("{id:guid}/moderate")]
    public async Task<IActionResult> ModerateProperty(Guid id, PropertyModerationRequest request, CancellationToken cancellationToken)
    {
        var store = phaseOneStore as IPropertyModerationStore
            ?? throw new InvalidOperationException("Property moderation store is unavailable.");
        var actor = authorization.TryGetSignedInUser() ?? Guid.Empty;
        var previous = phaseOneStore.GetProperties(actor).SingleOrDefault(item => item.Id == id) ?? phaseOneStore.GetProperty(id);
        var property = await store.ModeratePropertyAsync(actor, id, request, cancellationToken);
        if (property is null) return NotFound();

        await auditStore.RecordPrivilegedAuditAsync(
            new PrivilegedAuditRecord(
                new AuditActorContext(actor, "Admin", AdminPermissionCatalog.PropertyModeration, HttpContext.TraceIdentifier),
                "PropertyModerated",
                "Property",
                property.Id,
                request.Reason ?? $"Property status changed to {request.Status}.",
                previous,
                property),
            cancellationToken);
        return Ok(property);
    }

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
        if (!await db.MilestoneProperties.AsNoTracking().AnyAsync(item => item.Id == id && !item.IsDeleted && !item.IsArchived, cancellationToken)) return NotFound();
        var start = from ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var end = to ?? start.AddDays(60);
        if (end <= start || end.DayNumber - start.DayNumber > 366) return BadRequest("Availability range must be between 1 and 366 days.");
        var bookings = phaseOneStore.GetBookings().Where(item => item.PropertyId == id && !item.Status.Equals("REJECTED", StringComparison.OrdinalIgnoreCase) && !item.Status.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase)).ToList();
        var blocks = await db.MilestoneCalendarBlocks.AsNoTracking().Where(item => item.PropertyId == id && !item.IsDeleted && item.EndsOn > start && item.StartsOn < end).ToListAsync(cancellationToken);
        var manualBlocks = await db.MilestoneCalendarManualBlocks.AsNoTracking().Where(item => item.PropertyId == id && item.Status == "ACTIVE" && !item.IsDeleted && item.EndsOn > start && item.StartsOn < end).ToListAsync(cancellationToken);
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
            if (block is not null) return new PropertyAvailabilityDayDto(date, "BLOCKED", "ExternalCalendar", block.Summary);
            var manual = manualBlocks.FirstOrDefault(item => item.StartsOn <= date && item.EndsOn > date);
            return manual is null
                ? new PropertyAvailabilityDayDto(date, "AVAILABLE", "Available")
                : new PropertyAvailabilityDayDto(date, "BLOCKED", "Manual", manual.Reason);
        }).ToList();
        return Ok(new PropertyAvailabilityDto(id, start, end, days));
    }

    [HttpGet("{id:guid}/reviews")]
    public async Task<ActionResult<IReadOnlyList<PropertyReviewDto>>> GetReviews(Guid id, CancellationToken cancellationToken)
    {
        var property = phaseOneStore.GetProperty(id);
        if (property is null || !property.ModerationStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase)) return NotFound();
        var reviews = await db.MilestoneReviews.AsNoTracking()
            .Where(review => review.PropertyId == id && !review.IsDeleted && review.Status == "Published")
            .OrderByDescending(review => review.CreatedAt)
            .ToListAsync(cancellationToken);
        var userIds = reviews.Select(review => review.UserId).Distinct().ToArray();
        var names = await db.MilestoneUsers.AsNoTracking().Where(user => userIds.Contains(user.Id)).ToDictionaryAsync(user => user.Id, user => user.DisplayName, cancellationToken);
        return Ok(reviews.Select(review => new PropertyReviewDto(review.Id, names.GetValueOrDefault(review.UserId, "Nesty traveler"), review.Rating, review.Text, review.CreatedAt, review.HostReply)).ToList());
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
        if (property is not null && !property.ModerationStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase))
        {
            var actor = authorization.TryGetSignedInUser();
            var canPreview = actor == property.HostUserId || authorization.IsInRole(UserRole.Admin);
            if (!canPreview) return NotFound();
        }
        return property is null ? NotFound() : Ok(property);
    }

}

public sealed record DuplicatePropertyRequest(string? Title);
public sealed record BulkPropertyArchiveRequest(IReadOnlyCollection<Guid> PropertyIds, bool IsArchived = true);
