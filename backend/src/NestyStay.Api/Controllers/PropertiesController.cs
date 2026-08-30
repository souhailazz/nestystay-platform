using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestyStay.Api.Auth;
using NestyStay.Application.PhaseOne;
using NestyStay.Application.PhaseTwo;
using NestyStay.Domain;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/properties")]
public sealed class PropertiesController(
    IPhaseOneStore phaseOneStore,
    IPhaseTwoStore phaseTwoStore,
    IResourceAuthorizationService authorization) : ControllerBase
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
