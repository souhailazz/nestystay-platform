using Microsoft.AspNetCore.Mvc;
using NestyStay.Application.Services;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/platform")]
public sealed class PlatformBookingWorkflowController(IBookingWorkflowService bookingWorkflowService) : ControllerBase
{
    [HttpGet("booking-workflow")]
    public IActionResult GetBookingWorkflow() => Ok(bookingWorkflowService.GetPendingVerificationFlow());
}
