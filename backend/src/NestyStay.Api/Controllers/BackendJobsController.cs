using Microsoft.AspNetCore.Mvc;
using NestyStay.Infrastructure.BackgroundJobs;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/backend-jobs")]
public sealed class BackendJobsController(IHostEnvironment environment) : ControllerBase
{
    [HttpGet]
    public IActionResult GetJobs() => IsDevelopmentOrTesting() ? Ok(BackendJobCatalog.Jobs) : NotFound();

    private bool IsDevelopmentOrTesting() =>
        environment.IsDevelopment() || environment.IsEnvironment("Testing");
}
