using Microsoft.AspNetCore.Mvc;
using NestyStay.Infrastructure.BackgroundJobs;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/backend-jobs")]
public sealed class BackendJobsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetJobs() => Ok(BackendJobCatalog.Jobs);
}
