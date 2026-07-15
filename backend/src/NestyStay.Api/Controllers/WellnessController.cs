using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestyStay.Api.Auth;
using NestyStay.Application.Wellness;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/wellness")]
public sealed class WellnessController(IWellnessStore wellnessStore) : ControllerBase
{
    [HttpPost("officers")]
    public async Task<IActionResult> OnboardOfficer(OnboardOfficerRequest request, CancellationToken cancellationToken) =>
        Ok(await wellnessStore.OnboardOfficerAsync(request, cancellationToken));

    [HttpGet("officers")]
    [Authorize(Policy = AdminTokenAuthenticationHandler.AdminPolicyName)]
    public async Task<IActionResult> GetOfficers([FromQuery] string? status, CancellationToken cancellationToken) =>
        Ok(await wellnessStore.GetOfficersAsync(status, cancellationToken));

    [HttpGet("officers/{officerId:guid}")]
    [Authorize(Policy = AdminTokenAuthenticationHandler.AdminPolicyName)]
    public async Task<IActionResult> GetOfficer(Guid officerId, CancellationToken cancellationToken)
    {
        var officer = await wellnessStore.GetOfficerAsync(officerId, cancellationToken);
        return officer is null ? NotFound() : Ok(officer);
    }

    [HttpGet("officers/available")]
    [Authorize(Policy = AdminTokenAuthenticationHandler.AdminPolicyName)]
    public async Task<IActionResult> GetAvailableOfficers(
        [FromQuery] string parish,
        [FromQuery] DateTimeOffset scheduledAt,
        CancellationToken cancellationToken) =>
        Ok(await wellnessStore.GetAvailableOfficersAsync(parish, scheduledAt, cancellationToken));

    [HttpPost("officers/{officerId:guid}/approve")]
    [Authorize(Policy = AdminTokenAuthenticationHandler.AdminPolicyName)]
    public async Task<IActionResult> ApproveOfficer(Guid officerId, AdminOfficerReviewRequest request, CancellationToken cancellationToken)
    {
        var officer = await wellnessStore.ApproveOfficerAsync(officerId, request, cancellationToken);
        return officer is null ? NotFound() : Ok(officer);
    }

    [HttpPost("officers/{officerId:guid}/reject")]
    [Authorize(Policy = AdminTokenAuthenticationHandler.AdminPolicyName)]
    public async Task<IActionResult> RejectOfficer(Guid officerId, AdminOfficerReviewRequest request, CancellationToken cancellationToken)
    {
        var officer = await wellnessStore.RejectOfficerAsync(officerId, request, cancellationToken);
        return officer is null ? NotFound() : Ok(officer);
    }

    [HttpPost("officers/{officerId:guid}/suspend")]
    [Authorize(Policy = AdminTokenAuthenticationHandler.AdminPolicyName)]
    public async Task<IActionResult> SuspendOfficer(Guid officerId, AdminOfficerReviewRequest request, CancellationToken cancellationToken)
    {
        var officer = await wellnessStore.SuspendOfficerAsync(officerId, request, cancellationToken);
        return officer is null ? NotFound() : Ok(officer);
    }

    [HttpPost("officers/{officerId:guid}/reactivate")]
    [Authorize(Policy = AdminTokenAuthenticationHandler.AdminPolicyName)]
    public async Task<IActionResult> ReactivateOfficer(Guid officerId, AdminOfficerReviewRequest request, CancellationToken cancellationToken)
    {
        var officer = await wellnessStore.ReactivateOfficerAsync(officerId, request, cancellationToken);
        return officer is null ? NotFound() : Ok(officer);
    }

    [HttpPost("quote")]
    public async Task<IActionResult> QuoteVisit(WellnessQuoteRequest request, CancellationToken cancellationToken) =>
        Ok(await wellnessStore.QuoteVisitAsync(request, cancellationToken));

    [HttpPost("visits")]
    public async Task<IActionResult> CreateVisit(CreateWellnessVisitRequest request, CancellationToken cancellationToken) =>
        Ok(await wellnessStore.CreateVisitAsync(request, cancellationToken));

    [HttpGet("visits")]
    public async Task<IActionResult> GetVisits(
        [FromQuery] Guid? hostUserId,
        [FromQuery] Guid? propertyId,
        [FromQuery] Guid? officerId,
        CancellationToken cancellationToken) =>
        Ok(await wellnessStore.GetVisitsAsync(hostUserId, propertyId, officerId, cancellationToken));

    [HttpGet("visits/{visitId:guid}")]
    public async Task<IActionResult> GetVisit(Guid visitId, CancellationToken cancellationToken)
    {
        var visit = await wellnessStore.GetVisitAsync(visitId, cancellationToken);
        return visit is null ? NotFound() : Ok(visit);
    }

    [HttpPost("visits/{visitId:guid}/assign")]
    [Authorize(Policy = AdminTokenAuthenticationHandler.AdminPolicyName)]
    public async Task<IActionResult> AssignOfficer(Guid visitId, AssignOfficerRequest request, CancellationToken cancellationToken)
    {
        var visit = await wellnessStore.AssignOfficerAsync(visitId, request, cancellationToken);
        return visit is null ? NotFound() : Ok(visit);
    }

    [HttpPost("visits/{visitId:guid}/cancel")]
    [Authorize(Policy = AdminTokenAuthenticationHandler.AdminPolicyName)]
    public async Task<IActionResult> CancelVisit(Guid visitId, CancelWellnessVisitRequest request, CancellationToken cancellationToken)
    {
        var visit = await wellnessStore.CancelVisitAsync(visitId, request, cancellationToken);
        return visit is null ? NotFound() : Ok(visit);
    }

    [HttpPost("visits/{visitId:guid}/report")]
    public async Task<IActionResult> SubmitReport(Guid visitId, SubmitWellnessReportRequest request, CancellationToken cancellationToken)
    {
        var visit = await wellnessStore.SubmitReportAsync(visitId, request, adminOverride: false, cancellationToken);
        return visit is null ? NotFound() : Ok(visit);
    }

    [HttpPost("visits/{visitId:guid}/complete")]
    [Authorize(Policy = AdminTokenAuthenticationHandler.AdminPolicyName)]
    public async Task<IActionResult> CompleteVisit(Guid visitId, SubmitWellnessReportRequest request, CancellationToken cancellationToken)
    {
        var visit = await wellnessStore.SubmitReportAsync(visitId, request, adminOverride: true, cancellationToken);
        return visit is null ? NotFound() : Ok(visit);
    }

    [HttpPost("visits/{visitId:guid}/payout")]
    [Authorize(Policy = AdminTokenAuthenticationHandler.AdminPolicyName)]
    public async Task<IActionResult> MarkPayoutPaid(Guid visitId, MarkPayoutPaidRequest request, CancellationToken cancellationToken)
    {
        var payout = await wellnessStore.MarkPayoutPaidAsync(visitId, request, cancellationToken);
        return payout is null ? NotFound() : Ok(payout);
    }

    [HttpGet("payouts")]
    [Authorize(Policy = AdminTokenAuthenticationHandler.AdminPolicyName)]
    public async Task<IActionResult> GetPayouts([FromQuery] string? status, CancellationToken cancellationToken) =>
        Ok(await wellnessStore.GetPayoutsAsync(status, cancellationToken));

    [HttpGet("admin/dashboard")]
    [Authorize(Policy = AdminTokenAuthenticationHandler.AdminPolicyName)]
    public async Task<IActionResult> GetAdminDashboard(CancellationToken cancellationToken) =>
        Ok(await wellnessStore.GetAdminDashboardAsync(cancellationToken));
}
