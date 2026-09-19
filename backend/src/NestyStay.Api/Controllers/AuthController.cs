using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NestyStay.Api.Configuration;
using NestyStay.Api.Auth;
using NestyStay.Application.PhaseOne;
using NestyStay.Application.SpecCompletion;
using NestyStay.Application.Abstractions;
using NestyStay.Infrastructure.Persistence;
using NestyStay.Infrastructure.Persistence.Milestones;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IPhaseOneStore phaseOneStore,
    IHostEnvironment environment,
    IConfiguration configuration,
    CurrentUserContext currentUser,
    ISpecCompletionStore specCompletionStore,
    NestyStayDbContext db,
    IAccessTokenService accessTokenService) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    public async Task<IActionResult> Register(RegisterUserRequest request, CancellationToken cancellationToken) =>
        Ok(await phaseOneStore.RegisterAsync(request, cancellationToken));

    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await phaseOneStore.LoginAsync(request with
        {
            UserAgent = Request.Headers.UserAgent.ToString(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        }, cancellationToken);
        if (SessionCookieAuth.IsCookieMode(Request) && result.AccessToken is not null && result.ExpiresAt is not null)
        {
            SessionCookieAuth.Issue(Response, result.AccessToken, result.ExpiresAt.Value, IsSecureCookie(), ResolveCookieDomain(), ResolveCookieSameSite());
        }

        return Ok(SanitizeForBrowser(result));
    }

    [HttpPost("google")]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    public async Task<IActionResult> Google(GoogleSignInRequest request, CancellationToken cancellationToken)
    {
        var result = await phaseOneStore.GoogleSignInAsync(request, cancellationToken);
        if (SessionCookieAuth.IsCookieMode(Request)) SessionCookieAuth.Issue(Response, result.AccessToken, result.ExpiresAt, IsSecureCookie(), ResolveCookieDomain(), ResolveCookieSameSite());
        return Ok(SanitizeForBrowser(result));
    }

    [HttpPost("2fa/verify")]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    public async Task<IActionResult> VerifyTwoFactor(VerifyTwoFactorRequest request, CancellationToken cancellationToken)
    {
        var result = await phaseOneStore.VerifyTwoFactorAsync(request with
        {
            UserAgent = Request.Headers.UserAgent.ToString(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        }, cancellationToken);
        if (SessionCookieAuth.IsCookieMode(Request)) SessionCookieAuth.Issue(Response, result.AccessToken, result.ExpiresAt, IsSecureCookie(), ResolveCookieDomain(), ResolveCookieSameSite());
        return Ok(SanitizeForBrowser(result));
    }

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
    [HttpPatch("profile")]
    public async Task<IActionResult> UpdateProfile(UpdateUserProfileRequest request, CancellationToken cancellationToken) =>
        Ok(await phaseOneStore.UpdateUserProfileAsync(RequireUserId(), request, cancellationToken));

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken) =>
        LogoutAndClearCookies(await phaseOneStore.LogoutAsync(RequireUserId(), cancellationToken));

    [Authorize]
    [HttpGet("sessions")]
    public async Task<ActionResult<IReadOnlyList<UserSessionDto>>> GetSessions(CancellationToken cancellationToken)
    {
        var store = phaseOneStore as ISessionActivityStore
            ?? throw new InvalidOperationException("Session management is unavailable.");
        var sessions = await store.GetSessionsAsync(RequireUserId(), currentUser.SessionTokenId, cancellationToken);
        return Ok(sessions);
    }

    [Authorize]
    [HttpDelete("sessions/{sessionId:guid}")]
    public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken cancellationToken)
    {
        var store = phaseOneStore as ISessionActivityStore
            ?? throw new InvalidOperationException("Session management is unavailable.");
        var result = await store.RevokeSessionAsync(RequireUserId(), sessionId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize]
    [HttpPost("sessions/revoke-others")]
    public async Task<IActionResult> RevokeOtherSessions(CancellationToken cancellationToken)
    {
        var store = phaseOneStore as ISessionActivityStore
            ?? throw new InvalidOperationException("Session management is unavailable.");
        var count = await store.RevokeOtherSessionsAsync(RequireUserId(), null, currentUser.SessionTokenId, cancellationToken);
        return Ok(new { revoked = count });
    }

    [HttpPost("2fa/sms/request")]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    public async Task<ActionResult<SmsTwoFactorChallengeDto>> RequestSmsFallback(SmsTwoFactorRequest request, CancellationToken cancellationToken)
    {
        var challenge = await db.MilestoneTwoFactorChallenges.SingleOrDefaultAsync(item => item.ChallengeId == request.ChallengeId && item.ExpiresAt > DateTimeOffset.UtcNow, cancellationToken);
        if (challenge is null) throw new InvalidOperationException("Invalid or expired 2FA challenge.");
        var user = await db.MilestoneUsers.AsNoTracking().SingleOrDefaultAsync(item => item.Id == challenge.UserId, cancellationToken);
        if (user is null || string.IsNullOrWhiteSpace(user.Phone)) throw new InvalidOperationException("SMS fallback is not available for this account.");
        var flow = await specCompletionStore.StartAuthFlowAsync(new StartAuthFlowRequest(user.Id, "OneTimePasscode", user.Phone, ResolveRequesterIp()), cancellationToken);
        return Ok(new SmsTwoFactorChallengeDto(flow.Id, challenge.ChallengeId, MaskPhone(user.Phone), flow.ExpiresAt, flow.AttemptsRemaining));
    }

    [HttpPost("2fa/sms/verify")]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    public async Task<IActionResult> VerifySmsFallback(VerifySmsTwoFactorRequest request, CancellationToken cancellationToken)
    {
        var challenge = await db.MilestoneTwoFactorChallenges.SingleOrDefaultAsync(item => item.ChallengeId == request.ChallengeId && item.ExpiresAt > DateTimeOffset.UtcNow, cancellationToken);
        if (challenge is null) throw new InvalidOperationException("Invalid or expired 2FA challenge.");
        var flow = await specCompletionStore.CompleteAuthFlowAsync(new CompleteAuthFlowRequest(request.FlowId, request.Code), cancellationToken);
        if (flow.UserId != challenge.UserId || !flow.FlowType.Equals("OneTimePasscode", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("SMS challenge does not match the login attempt.");
        db.MilestoneTwoFactorChallenges.Remove(challenge);
        await db.SaveChangesAsync(cancellationToken);
        var profile = await phaseOneStore.GetUserProfileAsync(challenge.UserId, cancellationToken);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(8);
        var accessToken = accessTokenService.Issue(profile.UserId, profile.Roles, expiresAt);
        if (phaseOneStore is ISessionActivityStore sessionStore && accessTokenService.Validate(accessToken) is { } session)
        {
            await sessionStore.RecordSessionAsync(profile.UserId, session.TokenId, session.IssuedAt, expiresAt,
                string.IsNullOrWhiteSpace(request.DeviceName) ? "SMS fallback browser" : request.DeviceName,
                Request.Headers.UserAgent.ToString(),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                request.RememberDevice,
                cancellationToken);
        }
        if (SessionCookieAuth.IsCookieMode(Request)) SessionCookieAuth.Issue(Response, accessToken, expiresAt, IsSecureCookie(), ResolveCookieDomain(), ResolveCookieSameSite());
        var response = new VerifyTwoFactorResponse(profile.UserId, accessToken, expiresAt, profile.Roles);
        return Ok(SanitizeForBrowser(response));
    }

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
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    public async Task<IActionResult> RequestPasswordReset(PasswordResetRequest request, CancellationToken cancellationToken) =>
        Ok(await phaseOneStore.RequestPasswordResetAsync(request with { RequestIp = ResolveRequesterIp() }, cancellationToken));

    [HttpPost("password-reset/complete")]
    [EnableRateLimiting(RateLimitPolicies.Authentication)]
    public async Task<IActionResult> CompletePasswordReset(CompletePasswordResetRequest request, CancellationToken cancellationToken) =>
        Ok(await phaseOneStore.CompletePasswordResetAsync(request, cancellationToken));

    [HttpGet("development/challenges/{challengeId}")]
    public async Task<IActionResult> GetDevelopmentChallengeCode(string challengeId, CancellationToken cancellationToken)
    {
        var developmentCodesAllowed =
            environment.IsEnvironment("Testing") ||
            (environment.IsDevelopment() && configuration.GetValue<bool>("Security:EnableDevelopmentAuthCodes"));
        if (!developmentCodesAllowed)
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
        var developmentCodesAllowed =
            environment.IsEnvironment("Testing") ||
            (environment.IsDevelopment() && configuration.GetValue<bool>("Security:EnableDevelopmentAuthCodes"));
        if (!developmentCodesAllowed)
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

    private static string MaskPhone(string phone)
    {
        var normalized = phone.Trim();
        return normalized.Length <= 4 ? "••••" : $"{new string('•', Math.Max(0, normalized.Length - 4))}{normalized[^4..]}";
    }

    private bool IsSecureCookie() =>
        configuration.GetValue<bool?>("Security:SessionCookieSecure") ?? environment.IsProduction();

    private string? ResolveCookieDomain() =>
        configuration["Security:SessionCookieDomain"] ??
        Environment.GetEnvironmentVariable("NESTYSTAY_SESSION_COOKIE_DOMAIN");

    private Microsoft.AspNetCore.Http.SameSiteMode ResolveCookieSameSite()
    {
        var configured = configuration["Security:SessionCookieSameSite"] ??
                         Environment.GetEnvironmentVariable("NESTYSTAY_SESSION_COOKIE_SAMESITE");
        return Enum.TryParse<Microsoft.AspNetCore.Http.SameSiteMode>(configured, true, out var sameSite)
            ? sameSite
            : Microsoft.AspNetCore.Http.SameSiteMode.Lax;
    }

    private IActionResult LogoutAndClearCookies(object result)
    {
        SessionCookieAuth.Clear(Response, ResolveCookieDomain());
        return Ok(result);
    }

    private object SanitizeForBrowser(LoginResponse result) =>
        SessionCookieAuth.IsCookieMode(Request) ? result with { AccessToken = null } : result;

    private object SanitizeForBrowser(GoogleSignInResponse result) =>
        SessionCookieAuth.IsCookieMode(Request) ? result with { AccessToken = string.Empty } : result;

    private object SanitizeForBrowser(VerifyTwoFactorResponse result) =>
        SessionCookieAuth.IsCookieMode(Request) ? result with { AccessToken = string.Empty } : result;
}

public sealed record SmsTwoFactorRequest(string ChallengeId);
public sealed record VerifySmsTwoFactorRequest(
    string ChallengeId,
    Guid FlowId,
    string Code,
    string? DeviceName = null,
    bool RememberDevice = false,
    string? UserAgent = null,
    string? IpAddress = null);
public sealed record SmsTwoFactorChallengeDto(Guid FlowId, string ChallengeId, string MaskedPhone, DateTimeOffset ExpiresAt, int AttemptsRemaining);
