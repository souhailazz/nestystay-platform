using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestyStay.Api.Auth;
using NestyStay.Application.PhaseOne;
using NestyStay.Domain;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/properties")]
public sealed class PropertiesController(IPhaseOneStore phaseOneStore, IResourceAuthorizationService authorization) : ControllerBase
{
    [HttpGet]
    public IActionResult GetProperties() => Ok(phaseOneStore.GetProperties());

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateProperty(CreatePropertyRequest request, CancellationToken cancellationToken)
    {
        var hostUserId = authorization.RequireHost();
        return Ok(await phaseOneStore.CreatePropertyAsync(request with { HostUserId = hostUserId }, cancellationToken));
    }

    [Authorize(Roles = "Host")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProperty(Guid id, UpdatePropertyRequest request, CancellationToken cancellationToken)
    {
        var hostUserId = authorization.RequireHost();
        return Ok(await phaseOneStore.UpdatePropertyAsync(hostUserId, id, request, cancellationToken));
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

    [HttpGet("{id:guid}")]
    public IActionResult GetProperty(Guid id)
    {
        var property = phaseOneStore.GetProperty(id);
        return property is null ? NotFound() : Ok(property);
    }

}
