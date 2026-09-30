using Microsoft.AspNetCore.Mvc;
using NestyStay.Application.Services;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/platform")]
public sealed class PlatformController(
    IPlatformBlueprintService blueprintService) : ControllerBase
{
    [HttpGet("modules")]
    public IActionResult GetModules() => Ok(blueprintService.GetModules());

    [HttpGet("portals")]
    public IActionResult GetPortals() => Ok(blueprintService.GetPortals());

    [HttpGet("vendors")]
    public IActionResult GetVendors() => Ok(blueprintService.GetVendorAdapters());

}
