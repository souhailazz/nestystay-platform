using Microsoft.AspNetCore.Mvc;
using NestyStay.Application.Services;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/platform")]
public sealed class PlatformPricebookController(IPricebookService pricebookService) : ControllerBase
{
    [HttpGet("pricebook")]
    public IActionResult GetPricebook() => Ok(pricebookService.GetDefaultPricebook());
}
