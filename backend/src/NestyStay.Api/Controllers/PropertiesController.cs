using Microsoft.AspNetCore.Mvc;
using NestyStay.Application.PhaseOne;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/properties")]
public sealed class PropertiesController(IPhaseOneStore phaseOneStore) : ControllerBase
{
    [HttpGet]
    public IActionResult GetProperties() => Ok(phaseOneStore.GetProperties());

    [HttpPost]
    public async Task<IActionResult> CreateProperty(CreatePropertyRequest request, CancellationToken cancellationToken) =>
        Ok(await phaseOneStore.CreatePropertyAsync(request, cancellationToken));

    [HttpGet("{id:guid}")]
    public IActionResult GetProperty(Guid id)
    {
        var property = phaseOneStore.GetProperty(id);
        return property is null ? NotFound() : Ok(property);
    }
}
