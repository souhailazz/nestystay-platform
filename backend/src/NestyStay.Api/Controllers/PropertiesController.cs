using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestyStay.Api.Auth;
using NestyStay.Application.PhaseOne;
using NestyStay.Domain;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/properties")]
public sealed class PropertiesController(IPhaseOneStore phaseOneStore, CurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    public IActionResult GetProperties() => Ok(phaseOneStore.GetProperties());

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateProperty(CreatePropertyRequest request, CancellationToken cancellationToken)
    {
        if (!User.IsInRole(UserRole.Host.ToString()))
        {
            return Forbid();
        }

        var hostUserId = currentUser.UserId ?? throw new UnauthorizedAccessException("Authenticated host id is required.");
        return Ok(await phaseOneStore.CreatePropertyAsync(request with { HostUserId = hostUserId }, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public IActionResult GetProperty(Guid id)
    {
        var property = phaseOneStore.GetProperty(id);
        return property is null ? NotFound() : Ok(property);
    }
}
