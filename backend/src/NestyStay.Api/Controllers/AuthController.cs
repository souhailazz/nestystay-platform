using Microsoft.AspNetCore.Mvc;
using NestyStay.Application.PhaseOne;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IPhaseOneStore phaseOneStore) : ControllerBase
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
}
