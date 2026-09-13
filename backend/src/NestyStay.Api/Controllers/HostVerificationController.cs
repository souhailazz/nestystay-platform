using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestyStay.Api.Auth;
using NestyStay.Application.Admin;
using NestyStay.Application.PhaseOne;
using NestyStay.Application.SpecCompletion;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/host-verification")]
[Authorize]
public sealed class HostVerificationController(
    IPhaseOneStore phaseOneStore,
    IResourceAuthorizationService authorization,
    IPrivilegedAuditStore auditStore) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Host")]
    public async Task<ActionResult<HostVerificationDto>> Get(CancellationToken cancellationToken)
    {
        var store = phaseOneStore as IHostVerificationStore
            ?? throw new InvalidOperationException("Host verification store is unavailable.");
        return Ok(await store.GetHostVerificationAsync(authorization.RequireHost(), cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = "Host")]
    public async Task<ActionResult<HostVerificationDto>> Submit(SubmitHostVerificationRequest request, CancellationToken cancellationToken)
    {
        var store = phaseOneStore as IHostVerificationStore
            ?? throw new InvalidOperationException("Host verification store is unavailable.");
        return Ok(await store.SubmitHostVerificationAsync(authorization.RequireHost(), request, cancellationToken));
    }

    [HttpGet("queue")]
    [Authorize(Policy = AdminAuthorizationPolicies.UserManagement)]
    public async Task<ActionResult<IReadOnlyList<HostVerificationQueueItemDto>>> Queue(CancellationToken cancellationToken)
    {
        var store = phaseOneStore as IHostVerificationStore
            ?? throw new InvalidOperationException("Host verification store is unavailable.");
        return Ok(await store.GetHostVerificationQueueAsync(cancellationToken));
    }

    [HttpPost("{hostUserId:guid}/decision")]
    [Authorize(Policy = AdminAuthorizationPolicies.UserManagement)]
    public async Task<ActionResult<HostVerificationQueueItemDto>> Review(
        Guid hostUserId,
        HostVerificationDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var store = phaseOneStore as IHostVerificationStore
            ?? throw new InvalidOperationException("Host verification store is unavailable.");
        // Legacy development admin tokens intentionally have no persisted user
        // id; use Guid.Empty for that audit actor while production sessions
        // provide the administrator id.
        var adminUserId = authorization.TryGetSignedInUser() ?? Guid.Empty;
        var reviewed = await store.ReviewHostVerificationAsync(adminUserId, hostUserId, request, cancellationToken);
        if (reviewed is null) return NotFound();

        await auditStore.RecordPrivilegedAuditAsync(
            new PrivilegedAuditRecord(
                new AuditActorContext(adminUserId, "Admin", AdminPermissionCatalog.UserManagement, HttpContext.TraceIdentifier),
                "HostVerificationReviewed",
                "User",
                reviewed.UserId,
                request.Reason ?? $"Host verification marked {reviewed.Status}.",
                null,
                reviewed),
            cancellationToken);
        return Ok(reviewed);
    }
}
