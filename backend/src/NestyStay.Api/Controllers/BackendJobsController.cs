using Microsoft.AspNetCore.Mvc;
using NestyStay.Infrastructure.BackgroundJobs;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/backend-jobs")]
public sealed class BackendJobsController(IHostEnvironment environment) : ControllerBase
{
    [HttpGet]
    public IActionResult GetJobs() => environment.IsProduction() ? NotFound() : Ok(BackendJobCatalog.Jobs);
}
