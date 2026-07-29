using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestyStay.Api.Auth;
using NestyStay.Application.Admin;
using NestyStay.Application.PhaseTwo;
using NestyStay.Application.SpecCompletion;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/badges-pricing")]
public sealed class BadgesPricingController(
    IPhaseTwoStore phaseTwoStore,
    IResourceAuthorizationService authorization,
    IPrivilegedAuditStore auditStore) : ControllerBase
{
    [HttpGet("pricebook")]
    public IActionResult GetPricebook() => Ok(phaseTwoStore.GetPricebook());

    [HttpGet("pricebook/{key}")]
    public IActionResult GetPricebookItem(string key)
    {
        var item = phaseTwoStore.GetPricebookItem(key);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPut("pricebook/{key}")]
    [Authorize(Policy = AdminAuthorizationPolicies.SystemConfiguration)]
    public async Task<IActionResult> UpdatePricebookItem(string key, UpdatePricebookItemRequest request, CancellationToken cancellationToken)
    {
        var previous = phaseTwoStore.GetPricebookItem(key);
        var item = phaseTwoStore.UpdatePricebookItem(key, request);
        await RecordSystemAuditAsync("PricebookItemUpdated", "PricebookItem", null, $"Pricebook item {key} updated.", previous, item, cancellationToken);
        return Ok(item);
    }

    [HttpGet("badges")]
    public IActionResult GetBadgeDefinitions() => Ok(phaseTwoStore.GetBadgeDefinitions());

    [HttpPost("badges/eligibility")]
    public IActionResult GetBadgeEligibility(PurchaseBadgeRequest request) =>
        Ok(phaseTwoStore.GetBadgeEligibility(request));

    [HttpPost("badges/purchase")]
    public IActionResult PurchaseBadge(PurchaseBadgeRequest request) =>
        Ok(phaseTwoStore.PurchaseBadge(request));

    [HttpGet("badges/assignments")]
    public IActionResult GetBadgeAssignments([FromQuery] string? subjectType = null, [FromQuery] Guid? subjectId = null) =>
        Ok(phaseTwoStore.GetBadgeAssignments(subjectType, subjectId));

    [HttpGet("badges/features/{subjectType}/{subjectId:guid}")]
    public IActionResult GetFeatureAccess(string subjectType, Guid subjectId) =>
        Ok(phaseTwoStore.GetFeatureAccess(subjectType, subjectId));

    [HttpPost("badges/assignments/{assignmentId:guid}/expire")]
    [Authorize(Policy = AdminAuthorizationPolicies.SystemConfiguration)]
    public async Task<IActionResult> ExpireBadge(Guid assignmentId, CancellationToken cancellationToken)
    {
        var previous = FindAssignment(assignmentId);
        var assignment = phaseTwoStore.ExpireBadge(assignmentId);
        await RecordSystemAuditAsync("BadgeAssignmentExpired", "BadgeAssignment", assignmentId, "Badge assignment expired by administrator.", previous, assignment, cancellationToken);
        return Ok(assignment);
    }

    [HttpPost("badges/assignments/{assignmentId:guid}/suspend")]
    [Authorize(Policy = AdminAuthorizationPolicies.SystemConfiguration)]
    public async Task<IActionResult> SuspendBadge(Guid assignmentId, CancellationToken cancellationToken)
    {
        var previous = FindAssignment(assignmentId);
        var assignment = phaseTwoStore.SuspendBadge(assignmentId);
        await RecordSystemAuditAsync("BadgeAssignmentSuspended", "BadgeAssignment", assignmentId, "Badge assignment suspended by administrator.", previous, assignment, cancellationToken);
        return Ok(assignment);
    }

    [HttpGet("renewals")]
    public IActionResult GetRenewals([FromQuery] Guid? assignmentId = null) =>
        Ok(phaseTwoStore.GetRenewals(assignmentId));

    [HttpPost("renewals/{assignmentId:guid}/pay")]
    public IActionResult PayRenewal(Guid assignmentId) =>
        Ok(phaseTwoStore.PayRenewal(assignmentId));

    [HttpGet("campaigns")]
    public IActionResult GetCampaigns() => Ok(phaseTwoStore.GetCampaigns());

    [HttpPost("campaigns")]
    [Authorize(Policy = AdminAuthorizationPolicies.SystemConfiguration)]
    public async Task<IActionResult> CreateCampaign(CreateCampaignRequest request, CancellationToken cancellationToken)
    {
        var campaign = phaseTwoStore.CreateCampaign(request);
        await RecordSystemAuditAsync("CampaignCreated", "Campaign", campaign.Id, $"Campaign {campaign.Key} created.", null, campaign, cancellationToken);
        return Ok(campaign);
    }

    [HttpPost("campaigns/{campaignKey}/enroll")]
    public IActionResult EnrollCampaign(string campaignKey, EnrollCampaignRequest request) =>
        Ok(phaseTwoStore.EnrollCampaign(campaignKey, request));

    [HttpPost("founding-benefits")]
    [Authorize(Policy = AdminAuthorizationPolicies.SystemConfiguration)]
    public async Task<IActionResult> UpsertFoundingBenefit(FoundingBenefitRequest request, CancellationToken cancellationToken)
    {
        var previous = phaseTwoStore.GetFoundingBenefit(request.PropertyId);
        var benefit = phaseTwoStore.UpsertFoundingBenefit(request);
        await RecordSystemAuditAsync("FoundingBenefitUpserted", "FoundingBenefit", request.PropertyId, "Founding benefit configuration updated.", previous, benefit, cancellationToken);
        return Ok(benefit);
    }

    [HttpGet("founding-benefits/{propertyId:guid}")]
    public IActionResult GetFoundingBenefit(Guid propertyId)
    {
        var benefit = phaseTwoStore.GetFoundingBenefit(propertyId);
        return benefit is null ? NotFound() : Ok(benefit);
    }

    [HttpPost("founding-benefits/transfer-evaluation")]
    public IActionResult EvaluateFoundingTransfer(FoundingTransferEvaluationRequest request) =>
        Ok(phaseTwoStore.EvaluateFoundingTransfer(request));

    [HttpPost("commission-quote")]
    public IActionResult QuoteCommission(CommissionQuoteRequest request) =>
        Ok(phaseTwoStore.QuoteCommission(request));

    private BadgeAssignmentDto? FindAssignment(Guid assignmentId) =>
        phaseTwoStore.GetBadgeAssignments().FirstOrDefault(item => item.Id == assignmentId);

    private async Task RecordSystemAuditAsync(
        string action,
        string subjectType,
        Guid? subjectId,
        string reason,
        object? previousState,
        object? newState,
        CancellationToken cancellationToken)
    {
        await auditStore.RecordPrivilegedAuditAsync(
            new PrivilegedAuditRecord(
                AuditActor(),
                action,
                subjectType,
                subjectId,
                reason,
                previousState,
                newState),
            cancellationToken);
    }

    private AuditActorContext AuditActor() => new(
        authorization.TryGetSignedInUser(),
        "Admin",
        AdminPermissionCatalog.SystemConfiguration,
        HttpContext.TraceIdentifier);
}
