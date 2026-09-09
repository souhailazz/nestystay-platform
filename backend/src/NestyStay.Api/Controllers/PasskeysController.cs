using System.Text.Json;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NestyStay.Api.Auth;
using NestyStay.Application.Abstractions;
using NestyStay.Application.PhaseOne;
using NestyStay.Infrastructure.Persistence;
using NestyStay.Infrastructure.Persistence.Milestones;

namespace NestyStay.Api.Controllers;

/// <summary>
/// WebAuthn/passkey ceremonies. Challenges and credential counters are stored
/// in PostgreSQL and are single-use; signature verification is delegated to
/// the maintained Fido2 library rather than implemented in application code.
/// </summary>
[ApiController]
[Route("api/auth/passkeys")]
public sealed class PasskeysController(
    NestyStayDbContext db,
    IPhaseOneStore phaseOneStore,
    IAccessTokenService accessTokenService,
    CurrentUserContext currentUser,
    IConfiguration configuration,
    IHostEnvironment environment) : ControllerBase
{
    private static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(5);

    [Authorize]
    [HttpPost("register/options")]
    public async Task<IActionResult> RegistrationOptions(CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var profile = await phaseOneStore.GetUserProfileAsync(userId, cancellationToken);
        var fido2 = CreateFido2();
        var existingIds = await db.MilestonePasskeyCredentials.AsNoTracking()
            .Where(item => item.UserId == userId && item.RevokedAt == null && !item.IsDeleted)
            .Select(item => item.CredentialId)
            .ToListAsync(cancellationToken);
        var existing = existingIds.Select(id => new PublicKeyCredentialDescriptor(Convert.FromBase64String(id))).ToList();
        var options = fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = new Fido2User
            {
                Id = userId.ToByteArray(),
                Name = profile.Email,
                DisplayName = profile.DisplayName
            },
            ExcludeCredentials = existing,
            AuthenticatorSelection = AuthenticatorSelection.Default,
            AttestationPreference = AttestationConveyancePreference.None,
            PubKeyCredParams = [
                new PubKeyCredParam(COSE.Algorithm.ES256, PublicKeyCredentialType.PublicKey),
                new PubKeyCredParam(COSE.Algorithm.RS256, PublicKeyCredentialType.PublicKey)
            ]
        });
        var challenge = new MilestonePasskeyChallenge
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ChallengeId = Guid.NewGuid().ToString("N"),
            Challenge = Convert.ToBase64String(options.Challenge),
            Purpose = "Registration",
            OptionsJson = options.ToJson(),
            ExpiresAt = DateTimeOffset.UtcNow.Add(ChallengeLifetime),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = userId,
            UpdatedByUserId = userId
        };
        db.MilestonePasskeyChallenges.Add(challenge);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { challengeId = challenge.ChallengeId, options = JsonDocument.Parse(options.ToJson()).RootElement, expiresAt = challenge.ExpiresAt });
    }

    [Authorize]
    [HttpPost("register/complete")]
    public async Task<ActionResult<PasskeyDto>> RegistrationComplete(PasskeyRegistrationRequest request, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var challenge = await db.MilestonePasskeyChallenges.SingleOrDefaultAsync(item => item.ChallengeId == request.ChallengeId && item.UserId == userId && item.Purpose == "Registration" && item.ConsumedAt == null && !item.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Passkey registration challenge is invalid or expired.");
        if (challenge.ExpiresAt <= DateTimeOffset.UtcNow) throw new InvalidOperationException("Passkey registration challenge has expired.");
        var fido2 = CreateFido2();
        var options = CredentialCreateOptions.FromJson(challenge.OptionsJson);
        var result = await fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
        {
            AttestationResponse = request.Response,
            OriginalOptions = options,
            IsCredentialIdUniqueToUserCallback = async (parameters, ct) => !await db.MilestonePasskeyCredentials.AnyAsync(item => item.CredentialIdHash == Hash(parameters.CredentialId) && !item.IsDeleted, ct)
        }, cancellationToken);
        var credentialId = Convert.ToBase64String(result.Id);
        var entity = new MilestonePasskeyCredential
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CredentialIdHash = Hash(result.Id),
            CredentialId = credentialId,
            PublicKey = Convert.ToBase64String(result.PublicKey),
            SignCount = result.SignCount,
            TransportsJson = JsonSerializer.Serialize(result.Transports?.Select(item => item.ToString()).ToArray() ?? []),
            Label = string.IsNullOrWhiteSpace(request.Label) ? "Passkey" : request.Label.Trim()[..Math.Min(80, request.Label.Trim().Length)],
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = userId,
            UpdatedByUserId = userId
        };
        challenge.ConsumedAt = DateTimeOffset.UtcNow;
        challenge.UpdatedAt = DateTimeOffset.UtcNow;
        db.MilestonePasskeyCredentials.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(entity));
    }

    [HttpPost("assertion/options")]
    public async Task<IActionResult> AssertionOptions(PasskeyAssertionOptionsRequest request, CancellationToken cancellationToken)
    {
        MilestoneUser? user = null;
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = request.Email.Trim().ToLowerInvariant();
            user = await db.MilestoneUsers.AsNoTracking().SingleOrDefaultAsync(item => item.NormalizedEmail == email && !item.IsDeleted, cancellationToken);
        }
        IReadOnlyList<string> credentialIds = user is null
            ? Array.Empty<string>()
            : await db.MilestonePasskeyCredentials.AsNoTracking().Where(item => item.UserId == user.Id && item.RevokedAt == null && !item.IsDeleted).Select(item => item.CredentialId).ToListAsync(cancellationToken);
        var descriptors = credentialIds.Select(id => new PublicKeyCredentialDescriptor(Convert.FromBase64String(id))).ToList();
        var options = CreateFido2().GetAssertionOptions(new GetAssertionOptionsParams
        {
            AllowedCredentials = descriptors,
            UserVerification = UserVerificationRequirement.Preferred
        });
        var challenge = new MilestonePasskeyChallenge
        {
            Id = Guid.NewGuid(),
            UserId = user?.Id,
            ChallengeId = Guid.NewGuid().ToString("N"),
            Challenge = Convert.ToBase64String(options.Challenge),
            Purpose = "Authentication",
            OptionsJson = options.ToJson(),
            ExpiresAt = DateTimeOffset.UtcNow.Add(ChallengeLifetime),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.MilestonePasskeyChallenges.Add(challenge);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { challengeId = challenge.ChallengeId, options = JsonDocument.Parse(options.ToJson()).RootElement, expiresAt = challenge.ExpiresAt });
    }

    [HttpPost("assertion/complete")]
    public async Task<IActionResult> AssertionComplete(PasskeyAssertionRequest request, CancellationToken cancellationToken)
    {
        var challenge = await db.MilestonePasskeyChallenges.SingleOrDefaultAsync(item => item.ChallengeId == request.ChallengeId && item.Purpose == "Authentication" && item.ConsumedAt == null && !item.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Passkey authentication challenge is invalid or expired.");
        if (challenge.ExpiresAt <= DateTimeOffset.UtcNow) throw new InvalidOperationException("Passkey authentication challenge has expired.");
        var credentialId = request.Response.RawId ?? throw new InvalidOperationException("Passkey credential id is required.");
        var credential = await db.MilestonePasskeyCredentials.SingleOrDefaultAsync(item => item.CredentialIdHash == Hash(credentialId) && item.RevokedAt == null && !item.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Passkey is not registered.");
        if (challenge.UserId.HasValue && challenge.UserId != credential.UserId) throw new InvalidOperationException("Passkey does not belong to this account.");
        var result = await CreateFido2().MakeAssertionAsync(new MakeAssertionParams
        {
            AssertionResponse = request.Response,
            OriginalOptions = Fido2NetLib.AssertionOptions.FromJson(challenge.OptionsJson),
            StoredPublicKey = Convert.FromBase64String(credential.PublicKey),
            StoredSignatureCounter = credential.SignCount,
            IsUserHandleOwnerOfCredentialIdCallback = (parameters, _) => Task.FromResult(parameters.UserHandle is null || parameters.UserHandle.SequenceEqual(credential.UserId.ToByteArray()))
        }, cancellationToken);
        if (result.SignCount < credential.SignCount && credential.SignCount != 0) throw new InvalidOperationException("Passkey sign counter regression detected.");
        credential.SignCount = result.SignCount;
        credential.LastUsedAt = DateTimeOffset.UtcNow;
        credential.UpdatedAt = DateTimeOffset.UtcNow;
        challenge.ConsumedAt = DateTimeOffset.UtcNow;
        challenge.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        var profile = await phaseOneStore.GetUserProfileAsync(credential.UserId, cancellationToken);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(8);
        var token = accessTokenService.Issue(profile.UserId, profile.Roles, expiresAt);
        if (phaseOneStore is ISessionActivityStore sessionStore && accessTokenService.Validate(token) is { } session)
        {
            await sessionStore.RecordSessionAsync(profile.UserId, session.TokenId, session.IssuedAt, expiresAt, "Passkey browser", Request.Headers.UserAgent.ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), true, cancellationToken);
        }
        if (SessionCookieAuth.IsCookieMode(Request)) SessionCookieAuth.Issue(Response, token, expiresAt, configuration.GetValue<bool?>("Security:SessionCookieSecure") ?? environment.IsProduction(), configuration["Security:SessionCookieDomain"]);
        return Ok(new PasskeyAuthenticationResponse(profile.UserId, profile.Email, profile.DisplayName, SessionCookieAuth.IsCookieMode(Request) ? string.Empty : token, expiresAt, profile.Roles));
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PasskeyDto>>> List(CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        return Ok(await db.MilestonePasskeyCredentials.AsNoTracking().Where(item => item.UserId == userId && !item.IsDeleted).OrderByDescending(item => item.CreatedAt).Select(item => ToDto(item)).ToListAsync(cancellationToken));
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var credential = await db.MilestonePasskeyCredentials.SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId && item.RevokedAt == null && !item.IsDeleted, cancellationToken);
        if (credential is null) return NotFound();
        credential.RevokedAt = DateTimeOffset.UtcNow;
        credential.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private Fido2 CreateFido2()
    {
        var rpId = configuration["WebAuthn:RpId"] ?? "localhost";
        var appUrl = configuration["PublicAppUrl"] ?? "http://localhost:5173";
        var origins = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { appUrl.TrimEnd('/') };
        if (environment.IsDevelopment() || environment.IsEnvironment("Testing")) origins.Add("http://localhost:5173");
        return new Fido2(new Fido2Configuration
        {
            ServerDomain = rpId,
            ServerName = "NestyStay",
            Origins = origins,
            Timeout = 60000,
            ChallengeSize = 32,
            TimestampDriftTolerance = 300
        }, new EmptyMetadataService());
    }

    private Guid RequireUserId() => currentUser.UserId ?? throw new UnauthorizedAccessException("A signed session bearer token is required.");
    private static string Hash(byte[] value) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(value)).ToLowerInvariant();
    private static PasskeyDto ToDto(MilestonePasskeyCredential item) => new(item.Id, item.Label, item.CreatedAt, item.LastUsedAt, item.RevokedAt is null);

    private sealed class EmptyMetadataService : IMetadataService
    {
        public Task<MetadataBLOBPayloadEntry?> GetEntryAsync(Guid aaguid, CancellationToken cancellationToken) => Task.FromResult<MetadataBLOBPayloadEntry?>(null);
        public bool ConformanceTesting() => false;
    }
}

public sealed record PasskeyRegistrationRequest(string ChallengeId, AuthenticatorAttestationRawResponse Response, string? Label = null);
public sealed record PasskeyAssertionOptionsRequest(string? Email = null);
public sealed record PasskeyAssertionRequest(string ChallengeId, AuthenticatorAssertionRawResponse Response);
public sealed record PasskeyDto(Guid Id, string Label, DateTimeOffset CreatedAt, DateTimeOffset? LastUsedAt, bool IsActive);
public sealed record PasskeyAuthenticationResponse(Guid UserId, string Email, string DisplayName, string AccessToken, DateTimeOffset ExpiresAt, IReadOnlyList<NestyStay.Domain.UserRole> Roles);
