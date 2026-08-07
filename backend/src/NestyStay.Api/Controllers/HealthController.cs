using Microsoft.AspNetCore.Mvc;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    [HttpHead]
    public IActionResult Get() => Ok(new
    {
        service = "NestyStay API",
        status = "ok",
        architecture = "Vite React frontend + ASP.NET Core backend",
        database = "PostgreSQL",
        openApi = "/openapi/v1.json"
    });
}
