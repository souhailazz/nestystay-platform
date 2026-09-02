using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using NestyStay.Api.Auth;
using NestyStay.Api.Configuration;
using NestyStay.Application.Admin;
using NestyStay.Application.SpecCompletion;
using NestyStay.Application.Wellness;
using NestyStay.Domain;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/wellness")]
public sealed class WellnessController(
    IWellnessStore wellnessStore,
    IResourceAuthorizationService authorization,
    IPrivilegedAuditStore auditStore) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("officers")]
    [EnableRateLimiting(RateLimitPolicies.PublicWrite)]
    public async Task<IActionResult> OnboardOfficer(OnboardOfficerRequest request, CancellationToken cancellationToken)
    {
        if (request.UserId is { } requestedUserId)
        {
            var actor = authorization.TryGetSignedInUser();
            if (actor != requestedUserId && !authorization.IsInRole(UserRole.Admin))
            {
                throw new ForbiddenAccessException("An officer account may only bind its own user identity.");
            }
        }
        return Ok(await wellnessStore.OnboardOfficerAsync(request, cancellationToken));
    }

    [HttpGet("officers")]
    [Authorize(Policy = AdminAuthorizationPolicies.OfficerManagement)]
    public async Task<IActionResult> GetOfficers([FromQuery] string? status, CancellationToken cancellationToken) =>
        Ok(await wellnessStore.GetOfficersAsync(status, cancellationToken));

    [HttpGet("officers/{officerId:guid}")]
    [Authorize(Policy = AdminAuthorizationPolicies.OfficerManagement)]
    public async Task<IActionResult> GetOfficer(Guid officerId, CancellationToken cancellationToken)
    {
        var officer = await wellnessStore.GetOfficerAsync(officerId, cancellationToken);
        return officer is null ? NotFound() : Ok(officer);
    }

    [HttpGet("officers/available")]
    [Authorize]
    public async Task<IActionResult> GetAvailableOfficers(
        [FromQuery] string parish,
        [FromQuery] DateTimeOffset scheduledAt,
        CancellationToken cancellationToken)
    {
        if (!authorization.IsInRole(UserRole.Admin) && !authorization.IsInRole(UserRole.Host))
        {
            throw new ForbiddenAccessException("Host or admin role is required.");
        }
        return Ok(await wellnessStore.GetAvailableOfficersAsync(parish, scheduledAt, cancellationToken));
    }

    [HttpPost("officers/{officerId:guid}/approve")]
    [Authorize(Policy = AdminAuthorizationPolicies.OfficerManagement)]
    public async Task<IActionResult> ApproveOfficer(Guid officerId, AdminOfficerReviewRequest request, CancellationToken cancellationToken)
    {
        var previous = await wellnessStore.GetOfficerAsync(officerId, cancellationToken);
        var officer = await wellnessStore.ApproveOfficerAsync(officerId, request, cancellationToken);
        await RecordOfficerAuditAsync("OfficerApproved", officerId, request.Reason ?? "Officer approved.", previous, officer, cancellationToken);
        return officer is null ? NotFound() : Ok(officer);
    }

    [HttpPost("officers/{officerId:guid}/reject")]
    [Authorize(Policy = AdminAuthorizationPolicies.OfficerManagement)]
    public async Task<IActionResult> RejectOfficer(Guid officerId, AdminOfficerReviewRequest request, CancellationToken cancellationToken)
    {
        var previous = await wellnessStore.GetOfficerAsync(officerId, cancellationToken);
        var officer = await wellnessStore.RejectOfficerAsync(officerId, request, cancellationToken);
        await RecordOfficerAuditAsync("OfficerRejected", officerId, request.Reason ?? "Officer rejected.", previous, officer, cancellationToken);
        return officer is null ? NotFound() : Ok(officer);
    }

    [HttpPost("officers/{officerId:guid}/suspend")]
    [Authorize(Policy = AdminAuthorizationPolicies.OfficerManagement)]
    public async Task<IActionResult> SuspendOfficer(Guid officerId, AdminOfficerReviewRequest request, CancellationToken cancellationToken)
    {
        var previous = await wellnessStore.GetOfficerAsync(officerId, cancellationToken);
        var officer = await wellnessStore.SuspendOfficerAsync(officerId, request, cancellationToken);
        await RecordOfficerAuditAsync("OfficerSuspended", officerId, request.Reason ?? "Officer suspended.", previous, officer, cancellationToken);
        return officer is null ? NotFound() : Ok(officer);
    }

    [HttpPost("officers/{officerId:guid}/reactivate")]
    [Authorize(Policy = AdminAuthorizationPolicies.OfficerManagement)]
    public async Task<IActionResult> ReactivateOfficer(Guid officerId, AdminOfficerReviewRequest request, CancellationToken cancellationToken)
    {
        var previous = await wellnessStore.GetOfficerAsync(officerId, cancellationToken);
        var officer = await wellnessStore.ReactivateOfficerAsync(officerId, request, cancellationToken);
        await RecordOfficerAuditAsync("OfficerReactivated", officerId, request.Reason ?? "Officer reactivated.", previous, officer, cancellationToken);
        return officer is null ? NotFound() : Ok(officer);
    }

    [HttpPost("quote")]
    public async Task<IActionResult> QuoteVisit(WellnessQuoteRequest request, CancellationToken cancellationToken) =>
        Ok(await wellnessStore.QuoteVisitAsync(request, cancellationToken));

    [Authorize]
    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscription(CancellationToken cancellationToken)
    {
        var actor = authorization.RequireSignedInUser();
        if (!authorization.IsInRole(UserRole.Admin) && !authorization.IsInRole(UserRole.Host)) throw new ForbiddenAccessException("Host or admin role is required.");
        return Ok(await wellnessStore.GetSubscriptionAsync(actor, cancellationToken));
    }

    [Authorize]
    [HttpPost("subscriptions")]
    public async Task<IActionResult> StartSubscription(CancellationToken cancellationToken)
    {
        var actor = authorization.RequireSignedInUser();
        if (!authorization.IsInRole(UserRole.Admin)) authorization.RequireHostOwner(actor);
        return Ok(await wellnessStore.StartSubscriptionAsync(actor, cancellationToken));
    }

    [Authorize]
    [HttpPost("subscriptions/renew")]
    public async Task<IActionResult> RenewSubscription(CancellationToken cancellationToken)
    {
        var actor = authorization.RequireSignedInUser();
        if (!authorization.IsInRole(UserRole.Admin)) authorization.RequireHostOwner(actor);
        return Ok(await wellnessStore.RenewSubscriptionAsync(actor, cancellationToken));
    }

    [Authorize]
    [HttpPost("subscriptions/cancel")]
    public async Task<IActionResult> CancelSubscription(CancellationToken cancellationToken)
    {
        var actor = authorization.RequireSignedInUser();
        if (!authorization.IsInRole(UserRole.Admin)) authorization.RequireHostOwner(actor);
        return await wellnessStore.CancelSubscriptionAsync(actor, cancellationToken) is { } subscription ? Ok(subscription) : NotFound();
    }

    [Authorize]
    [HttpPost("visits")]
    public async Task<IActionResult> CreateVisit(CreateWellnessVisitRequest request, CancellationToken cancellationToken)
    {
        if (!authorization.IsInRole(UserRole.Admin))
        {
            authorization.RequireHostOwner(request.HostUserId);
        }

        return Ok(await wellnessStore.CreateVisitAsync(request, cancellationToken));
    }

    [Authorize]
    [HttpGet("visits")]
    public async Task<IActionResult> GetVisits(
        [FromQuery] Guid? hostUserId,
        [FromQuery] Guid? propertyId,
        [FromQuery] Guid? officerId,
        CancellationToken cancellationToken)
    {
        if (!authorization.IsInRole(UserRole.Admin))
        {
            if (authorization.IsInRole(UserRole.Host))
            {
                hostUserId = authorization.RequireSignedInUser();
                officerId = null;
            }
            else if (authorization.IsInRole(UserRole.Officer))
            {
                var officer = await wellnessStore.GetOfficerForUserAsync(authorization.RequireSignedInUser(), cancellationToken)
                    ?? throw new ForbiddenAccessException("An approved wellness officer profile is required.");
                officerId = officer.Id;
                hostUserId = null;
            }
            else
            {
                throw new ForbiddenAccessException("Host or admin role is required.");
            }
        }

        return Ok(await wellnessStore.GetVisitsAsync(hostUserId, propertyId, officerId, cancellationToken));
    }

    [Authorize]
    [HttpGet("visits/{visitId:guid}")]
    public async Task<IActionResult> GetVisit(Guid visitId, CancellationToken cancellationToken)
    {
        var visit = await wellnessStore.GetVisitAsync(visitId, cancellationToken);
        if (visit is null) return NotFound();
        if (authorization.IsInRole(UserRole.Admin)) return Ok(visit);
        if (authorization.IsInRole(UserRole.Officer))
        {
            var officer = await wellnessStore.GetOfficerForUserAsync(authorization.RequireSignedInUser(), cancellationToken);
            return officer?.Id == visit.OfficerId ? Ok(visit) : Forbid();
        }
        var actor = authorization.RequireHostOwner(visit.HostUserId);
        return actor == visit.HostUserId ? Ok(visit) : Forbid();
    }

    [Authorize]
    [HttpGet("visits/{visitId:guid}/report")]
    public async Task<IActionResult> GetReport(Guid visitId, CancellationToken cancellationToken)
    {
        var visit = await wellnessStore.GetVisitAsync(visitId, cancellationToken);
        if (visit is null) return NotFound();
        if (!authorization.IsInRole(UserRole.Admin))
        {
            var actor = authorization.RequireSignedInUser();
            var officer = authorization.IsInRole(UserRole.Officer) ? await wellnessStore.GetOfficerForUserAsync(actor, cancellationToken) : null;
            if (visit.HostUserId != actor && officer?.Id != visit.OfficerId) return Forbid();
        }
        return await wellnessStore.GetReportAsync(visitId, cancellationToken) is { } report ? Ok(report) : NotFound();
    }

    [HttpPost("visits/{visitId:guid}/assign")]
    [Authorize(Policy = AdminAuthorizationPolicies.OfficerManagement)]
    public async Task<IActionResult> AssignOfficer(Guid visitId, AssignOfficerRequest request, CancellationToken cancellationToken)
    {
        var previous = await wellnessStore.GetVisitAsync(visitId, cancellationToken);
        var visit = await wellnessStore.AssignOfficerAsync(visitId, request, cancellationToken);
        await RecordVisitAuditAsync("WellnessOfficerAssigned", visitId, "Officer assigned to wellness visit.", previous, visit, cancellationToken);
        return visit is null ? NotFound() : Ok(visit);
    }

    [Authorize]
    [HttpPost("visits/{visitId:guid}/cancel")]
    public async Task<IActionResult> CancelVisit(Guid visitId, CancelWellnessVisitRequest request, CancellationToken cancellationToken)
    {
        var previous = await wellnessStore.GetVisitAsync(visitId, cancellationToken);
        if (previous is null) return NotFound();
        if (!authorization.IsInRole(UserRole.Admin)) authorization.RequireHostOwner(previous.HostUserId);
        var visit = await wellnessStore.CancelVisitAsync(visitId, request, cancellationToken);
        await RecordVisitAuditAsync("WellnessVisitCancelled", visitId, request.Reason ?? "Wellness visit cancelled.", previous, visit, cancellationToken);
        return visit is null ? NotFound() : Ok(visit);
    }

    [Authorize]
    [HttpPost("visits/{visitId:guid}/report/photos/uploads")]
    public async Task<IActionResult> PrepareReportPhotoUpload(Guid visitId, PrepareWellnessReportPhotoUploadRequest request, CancellationToken cancellationToken) =>
        Ok(await PrepareOfficerResultAsync(visitId, request.OfficerBadgeNumber, () => wellnessStore.PrepareReportPhotoUploadAsync(visitId, request, adminOverride: false, cancellationToken), cancellationToken));

    [Authorize]
    [HttpPut("visits/{visitId:guid}/report/photos/{photoId:guid}/content")]
    [EnableRateLimiting(RateLimitPolicies.Upload)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadReportPhotoContent(
        Guid visitId,
        Guid photoId,
        [FromQuery] string officerBadgeNumber,
        CancellationToken cancellationToken) =>
        Ok(await PrepareOfficerResultAsync(visitId, officerBadgeNumber, () => wellnessStore.UploadReportPhotoContentAsync(
            visitId, photoId, officerBadgeNumber, Request.ContentType ?? string.Empty, Request.ContentLength ?? 0,
            Request.Body, adminOverride: false, cancellationToken), cancellationToken));

    [HttpPost("visits/{visitId:guid}/complete/photos/uploads")]
    [Authorize(Policy = AdminAuthorizationPolicies.OfficerManagement)]
    public async Task<IActionResult> PrepareAdminReportPhotoUpload(Guid visitId, PrepareWellnessReportPhotoUploadRequest request, CancellationToken cancellationToken) =>
        Ok(await AuditResultAsync(
            await wellnessStore.PrepareReportPhotoUploadAsync(visitId, request, adminOverride: true, cancellationToken),
            "WellnessAdminReportPhotoUploadPrepared",
            "WellnessVisit",
            visitId,
            "Admin report photo upload URL issued.",
            AdminPermissionCatalog.OfficerManagement,
            null,
            cancellationToken));

    [HttpPut("visits/{visitId:guid}/complete/photos/{photoId:guid}/content")]
    [Authorize(Policy = AdminAuthorizationPolicies.OfficerManagement)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadAdminReportPhotoContent(Guid visitId, Guid photoId, CancellationToken cancellationToken) =>
        Ok(await AuditResultAsync(
            await wellnessStore.UploadReportPhotoContentAsync(
                visitId,
                photoId,
                string.Empty,
                Request.ContentType ?? string.Empty,
                Request.ContentLength ?? 0,
                Request.Body,
                adminOverride: true,
                cancellationToken),
            "WellnessAdminReportPhotoUploaded",
            "WellnessVisit",
            visitId,
            "Admin report photo uploaded.",
            AdminPermissionCatalog.OfficerManagement,
            null,
            cancellationToken));

    [Authorize]
    [HttpPost("visits/{visitId:guid}/report")]
    public async Task<IActionResult> SubmitReport(Guid visitId, SubmitWellnessReportRequest request, CancellationToken cancellationToken)
    {
        await RequireOfficerActorAsync(visitId, request.OfficerBadgeNumber, cancellationToken);
        var visit = await wellnessStore.SubmitReportAsync(visitId, request, adminOverride: false, cancellationToken);
        return visit is null ? NotFound() : Ok(visit);
    }

    [HttpPost("visits/{visitId:guid}/complete")]
    [Authorize(Policy = AdminAuthorizationPolicies.OfficerManagement)]
    public async Task<IActionResult> CompleteVisit(Guid visitId, SubmitWellnessReportRequest request, CancellationToken cancellationToken)
    {
        var previous = await wellnessStore.GetVisitAsync(visitId, cancellationToken);
        var visit = await wellnessStore.SubmitReportAsync(visitId, request, adminOverride: true, cancellationToken);
        await RecordVisitAuditAsync("WellnessVisitCompletedByAdmin", visitId, "Wellness visit completed by admin override.", previous, visit, cancellationToken);
        return visit is null ? NotFound() : Ok(visit);
    }

    [HttpPost("visits/{visitId:guid}/payout")]
    [Authorize(Policy = AdminAuthorizationPolicies.FinancialReporting)]
    public async Task<IActionResult> MarkPayoutPaid(Guid visitId, MarkPayoutPaidRequest request, CancellationToken cancellationToken)
    {
        var previous = (await wellnessStore.GetPayoutsAsync(null, cancellationToken)).FirstOrDefault(item => item.VisitId == visitId);
        var payout = await wellnessStore.MarkPayoutPaidAsync(visitId, request, cancellationToken);
        if (payout is not null)
        {
            await auditStore.RecordPrivilegedAuditAsync(
                new PrivilegedAuditRecord(
                    AuditActor(AdminPermissionCatalog.FinancialReporting),
                    "WellnessPayoutMarkedPaid",
                    "WellnessPayout",
                    payout.Id,
                    request.Notes ?? "Wellness payout marked paid.",
                    previous,
                    payout),
                cancellationToken);
        }

        return payout is null ? NotFound() : Ok(payout);
    }

    [HttpGet("payouts")]
    [Authorize(Policy = AdminAuthorizationPolicies.FinancialReporting)]
    public async Task<IActionResult> GetPayouts([FromQuery] string? status, CancellationToken cancellationToken) =>
        Ok(await wellnessStore.GetPayoutsAsync(status, cancellationToken));

    [HttpGet("admin/dashboard")]
    [Authorize(Policy = AdminAuthorizationPolicies.FinancialReporting)]
    public async Task<IActionResult> GetAdminDashboard(CancellationToken cancellationToken) =>
        Ok(await wellnessStore.GetAdminDashboardAsync(cancellationToken));

    private async Task RecordOfficerAuditAsync(
        string action,
        Guid officerId,
        string reason,
        WellnessOfficerDto? previous,
        WellnessOfficerDto? current,
        CancellationToken cancellationToken)
    {
        if (current is null)
        {
            return;
        }

        await auditStore.RecordPrivilegedAuditAsync(
            new PrivilegedAuditRecord(
                AuditActor(AdminPermissionCatalog.OfficerManagement),
                action,
                "WellnessOfficer",
                officerId,
                reason,
                previous,
                current),
            cancellationToken);
    }

    private async Task RecordVisitAuditAsync(
        string action,
        Guid visitId,
        string reason,
        WellnessVisitDto? previous,
        WellnessVisitDto? current,
        CancellationToken cancellationToken)
    {
        if (current is null)
        {
            return;
        }

        await auditStore.RecordPrivilegedAuditAsync(
            new PrivilegedAuditRecord(
                AuditActor(AdminPermissionCatalog.OfficerManagement),
                action,
                "WellnessVisit",
                visitId,
                reason,
                previous,
                current),
            cancellationToken);
    }

    private async Task<T> AuditResultAsync<T>(
        T result,
        string action,
        string subjectType,
        Guid subjectId,
        string reason,
        string effectivePermission,
        object? previousState,
        CancellationToken cancellationToken)
    {
        await auditStore.RecordPrivilegedAuditAsync(
            new PrivilegedAuditRecord(
                AuditActor(effectivePermission),
                action,
                subjectType,
                subjectId,
                reason,
                previousState,
                result),
            cancellationToken);

        return result;
    }

    private AuditActorContext AuditActor(string effectivePermission) => new(
        authorization.TryGetSignedInUser(),
        "Admin",
        effectivePermission,
        HttpContext.TraceIdentifier);

    private async Task RequireOfficerActorAsync(Guid visitId, string badgeNumber, CancellationToken cancellationToken)
    {
        if (authorization.IsInRole(UserRole.Admin)) return;
        if (!authorization.IsInRole(UserRole.Officer)) throw new ForbiddenAccessException("Only the assigned officer can submit a wellness report.");
        var actor = authorization.RequireSignedInUser();
        var officer = await wellnessStore.GetOfficerForUserAsync(actor, cancellationToken);
        var visit = await wellnessStore.GetVisitAsync(visitId, cancellationToken);
        if (officer is null || officer.Id != visit?.OfficerId || !string.Equals(officer.BadgeNumber, badgeNumber, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenAccessException("Only the assigned officer can submit this wellness report.");
        }
    }

    private async Task<T> PrepareOfficerResultAsync<T>(Guid visitId, string badgeNumber, Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        await RequireOfficerActorAsync(visitId, badgeNumber, cancellationToken);
        return await operation();
    }
}
