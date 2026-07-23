using Microsoft.AspNetCore.Mvc;
using NestyStay.Application.PhaseOne;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IPhaseOneStore phaseOneStore,
    IHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterUserRequest request, CancellationToken cancellationToken) =>
        Ok(await phaseOneStore.RegisterAsync(request, cancellationToken));

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken) =>
        Ok(await phaseOneStore.LoginAsync(request, cancellationToken));

    [HttpPost("google")]
    public async Task<IActionResult> Google(GoogleSignInRequest request, CancellationToken cancellationToken) =>
        Ok(await phaseOneStore.GoogleSignInAsync(request, cancellationToken));

    [HttpPost("2fa/verify")]
    public async Task<IActionResult> VerifyTwoFactor(VerifyTwoFactorRequest request, CancellationToken cancellationToken) =>
        Ok(await phaseOneStore.VerifyTwoFactorAsync(request, cancellationToken));

    [HttpPost("password-reset/request")]
    public async Task<IActionResult> RequestPasswordReset(PasswordResetRequest request, CancellationToken cancellationToken) =>
        Ok(await phaseOneStore.RequestPasswordResetAsync(request with { RequestIp = ResolveRequesterIp() }, cancellationToken));

    [HttpPost("password-reset/complete")]
    public async Task<IActionResult> CompletePasswordReset(CompletePasswordResetRequest request, CancellationToken cancellationToken) =>
        Ok(await phaseOneStore.CompletePasswordResetAsync(request, cancellationToken));

    [HttpGet("development/challenges/{challengeId}")]
    public async Task<IActionResult> GetDevelopmentChallengeCode(string challengeId, CancellationToken cancellationToken)
    {
        if (environment.IsProduction() ||
            (!environment.IsEnvironment("Testing") && !configuration.GetValue<bool>("Security:EnableDevelopmentAuthCodes")))
        {
            return NotFound();
        }

        return await phaseOneStore.GetDevelopmentTwoFactorCodeAsync(challengeId, cancellationToken) is { } code
            ? Ok(code)
            : NotFound();
    }

    [HttpGet("development/password-resets/{requestId}")]
    public async Task<IActionResult> GetDevelopmentPasswordResetToken(string requestId, CancellationToken cancellationToken)
    {
        if (environment.IsProduction() ||
            (!environment.IsEnvironment("Testing") && !configuration.GetValue<bool>("Security:EnableDevelopmentAuthCodes")))
        {
            return NotFound();
        }

        return await phaseOneStore.GetDevelopmentPasswordResetTokenAsync(requestId, cancellationToken) is { } token
            ? Ok(token)
            : NotFound();
    }

    private string ResolveRequesterIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString() ??
        Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim() ??
        "unknown";
}
