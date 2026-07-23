using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestyStay.Api.Auth;
using NestyStay.Application.PhaseOne;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IPhaseOneStore phaseOneStore,
    IHostEnvironment environment,
    IConfiguration configuration,
    CurrentUserContext currentUser) : ControllerBase
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

    [Authorize]
    [HttpPost("2fa/enrollments")]
    public async Task<IActionResult> BeginTwoFactorEnrollment(CancellationToken cancellationToken) =>
        Ok(await phaseOneStore.BeginTwoFactorEnrollmentAsync(RequireUserId(), cancellationToken));

    [Authorize]
    [HttpPost("2fa/enrollments/confirm")]
    public async Task<IActionResult> ConfirmTwoFactorEnrollment(ConfirmTwoFactorEnrollmentRequest request, CancellationToken cancellationToken) =>
        Ok(await phaseOneStore.ConfirmTwoFactorEnrollmentAsync(RequireUserId(), request, cancellationToken));

    [Authorize]
    [HttpDelete("2fa")]
    public async Task<IActionResult> DisableTwoFactor(DisableTwoFactorRequest request, CancellationToken cancellationToken) =>
        Ok(await phaseOneStore.DisableTwoFactorAsync(RequireUserId(), request, cancellationToken));

    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken) =>
        Ok(await phaseOneStore.GetUserProfileAsync(RequireUserId(), cancellationToken));

    [Authorize]
    [HttpPost("profile/photo/uploads")]
    public async Task<IActionResult> PrepareProfilePhotoUpload(PrepareProfilePhotoUploadRequest request, CancellationToken cancellationToken) =>
        Ok(await phaseOneStore.PrepareProfilePhotoUploadAsync(RequireUserId(), request, cancellationToken));

    [Authorize]
    [HttpPut("profile/photo/uploads/{photoId:guid}/content")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadProfilePhotoContent(Guid photoId, CancellationToken cancellationToken) =>
        Ok(await phaseOneStore.UploadProfilePhotoContentAsync(
            RequireUserId(),
            photoId,
            Request.ContentType ?? string.Empty,
            Request.ContentLength ?? 0,
            Request.Body,
            cancellationToken));

    [Authorize]
    [HttpGet("profile/photo/{photoId:guid}/download")]
    public async Task<IActionResult> GetProfilePhotoDownload(Guid photoId, CancellationToken cancellationToken) =>
        Ok(await phaseOneStore.GetProfilePhotoDownloadAsync(RequireUserId(), photoId, cancellationToken));

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

    private Guid RequireUserId() =>
        currentUser.UserId ?? throw new UnauthorizedAccessException("A signed session bearer token is required.");
}
