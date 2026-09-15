using Microsoft.AspNetCore.Mvc;
using NestyStay.Application.Services;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/platform")]
public sealed class PlatformController(
    IPlatformBlueprintService blueprintService,
    IBookingWorkflowService bookingWorkflowService,
    IPricebookService pricebookService) : ControllerBase
{
    [HttpGet("modules")]
    public IActionResult GetModules() => Ok(blueprintService.GetModules());

    [HttpGet("portals")]
    public IActionResult GetPortals() => Ok(blueprintService.GetPortals());

    [HttpGet("vendors")]
    public IActionResult GetVendors() => Ok(blueprintService.GetVendorAdapters());

    [HttpGet("booking-workflow")]
    public IActionResult GetBookingWorkflow() => Ok(bookingWorkflowService.GetPendingVerificationFlow());

    [HttpGet("pricebook")]
    public IActionResult GetPricebook() => Ok(pricebookService.GetDefaultPricebook());
}
