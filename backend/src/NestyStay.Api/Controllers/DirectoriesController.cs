using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestyStay.Api.Auth;
using NestyStay.Application.Admin;
using NestyStay.Application.Directories;
using NestyStay.Application.PhaseTwo;
using NestyStay.Application.SpecCompletion;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/directories")]
public sealed class DirectoriesController(
    IDirectoryModerationStore directoryStore,
    IResourceAuthorizationService authorization,
    IPhaseTwoStore phaseTwoStore,
    IPrivilegedAuditStore auditStore) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("providers")]
    public async Task<ActionResult<IReadOnlyList<DirectoryProviderRecord>>> List(
        [FromQuery] string? kind,
        [FromQuery] string? category,
        [FromQuery] string? parish,
        [FromQuery] string? query,
        CancellationToken cancellationToken)
    {
        RequireDirectoryAccess(kind);
        return Ok(await directoryStore.GetProvidersAsync(kind, category, parish, query, includeUnpublished: false, cancellationToken));
    }

    [AllowAnonymous]
    [HttpGet("providers/{slug}")]
    public async Task<ActionResult<DirectoryProviderRecord>> Get(string slug, CancellationToken cancellationToken)
    {
        var provider = await directoryStore.GetProviderAsync(slug, includeUnpublished: false, cancellationToken);
        if (provider is null) return NotFound();
        RequireDirectoryAccess(provider.Kind);
        return Ok(provider);
    }

    // Moderators need the complete queue, including pending and rejected records.
    // Public directory reads intentionally remain limited to published providers.
    [Authorize(Policy = AdminAuthorizationPolicies.PropertyModeration)]
    [HttpGet("providers/moderation")]
    public async Task<ActionResult<IReadOnlyList<DirectoryProviderRecord>>> ModerationQueue(
        [FromQuery] string? kind,
        [FromQuery] string? status,
        [FromQuery] string? query,
        CancellationToken cancellationToken)
    {
        var records = await directoryStore.GetProvidersAsync(kind, null, null, query, includeUnpublished: true, cancellationToken);
        if (!string.IsNullOrWhiteSpace(status))
        {
            records = records.Where(item => item.Status.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        }
        return Ok(records);
    }

    [Authorize]
    [HttpGet("providers/mine", Order = -10)]
    public async Task<ActionResult<IReadOnlyList<DirectoryProviderRecord>>> Mine(CancellationToken cancellationToken)
    {
        var actor = authorization.RequireSignedInUser();
        return Ok((await directoryStore.GetProvidersAsync(null, null, null, null, includeUnpublished: true, cancellationToken))
            .Where(item => item.OwnerUserId == actor)
            .ToList());
    }

    [Authorize]
    [HttpPost("providers")]
    public async Task<ActionResult<DirectoryProviderRecord>> Save(SaveDirectoryProviderRequest request, CancellationToken cancellationToken)
    {
        var isAdmin = authorization.IsInRole(NestyStay.Domain.UserRole.Admin) && AdminAuthorizationPolicies.HasPermission(User, AdminPermissionCatalog.PropertyModeration);
        var actor = isAdmin ? (authorization.TryGetSignedInUser() ?? Guid.Empty) : authorization.RequireSignedInUser();
        return Ok(await directoryStore.SaveProviderAsync(request, actor, isAdmin, cancellationToken));
    }

    [Authorize(Policy = AdminAuthorizationPolicies.PropertyModeration)]
    [HttpPost("providers/{slug}/moderate")]
    public async Task<ActionResult<DirectoryProviderRecord>> Moderate(string slug, ModerateDirectoryProviderRequest request, CancellationToken cancellationToken)
    {
        var actor = authorization.TryGetSignedInUser() ?? Guid.Empty;
        var previous = await directoryStore.GetProviderAsync(slug, includeUnpublished: true, cancellationToken);
        var provider = await directoryStore.ModerateAsync(slug, request.Status, actor, request.Reason, cancellationToken);
        if (provider is null) return NotFound();

        await auditStore.RecordPrivilegedAuditAsync(
            new PrivilegedAuditRecord(
                new AuditActorContext(actor, "Admin", AdminPermissionCatalog.PropertyModeration, HttpContext.TraceIdentifier),
                "DirectoryProviderModerated",
                "DirectoryProvider",
                provider.Id,
                request.Reason ?? $"Provider status changed to {provider.Status}.",
                previous,
                provider),
            cancellationToken);
        return Ok(provider);
    }

    public sealed record ModerateDirectoryProviderRequest(string Status, string? Reason = null);

    private void RequireDirectoryAccess(string? kind)
    {
        var requiredBadge = kind?.Trim() switch
        {
            "Custodian" => NestyStay.Domain.BadgeLevel.Verified,
            "Trades" => NestyStay.Domain.BadgeLevel.Trusted,
            "LocalBusiness" => NestyStay.Domain.BadgeLevel.Verified,
            "Police" => NestyStay.Domain.BadgeLevel.Wellness,
            _ => (NestyStay.Domain.BadgeLevel?)null
        };
        if (requiredBadge is null) return;
        if (authorization.IsInRole(NestyStay.Domain.UserRole.Admin)) return;
        var isPolice = string.Equals(kind, "Police", StringComparison.OrdinalIgnoreCase);
        if (User.Identity?.IsAuthenticated != true)
        {
            if (isPolice) throw new UnauthorizedAccessException("A signed session bearer token is required.");
            return;
        }
        if (!authorization.IsInRole(NestyStay.Domain.UserRole.Host))
        {
            throw new ForbiddenAccessException(isPolice ? "Police directory access is limited to Wellness hosts." : "A host account is required to use this directory.");
        }
        var actor = authorization.RequireSignedInUser();
        if (phaseTwoStore.GetFeatureAccess("Host", actor).ActiveLevel < requiredBadge.Value)
        {
            throw new ForbiddenAccessException($"A {requiredBadge.Value} badge is required to view the {kind} directory.");
        }
    }
}
