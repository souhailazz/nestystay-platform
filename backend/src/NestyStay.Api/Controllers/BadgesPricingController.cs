using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestyStay.Api.Auth;
using NestyStay.Application.PhaseTwo;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/badges-pricing")]
public sealed class BadgesPricingController(IPhaseTwoStore phaseTwoStore) : ControllerBase
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
    [Authorize(Policy = AdminTokenAuthenticationHandler.AdminPolicyName)]
    public IActionResult UpdatePricebookItem(string key, UpdatePricebookItemRequest request) =>
        Ok(phaseTwoStore.UpdatePricebookItem(key, request));

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
    [Authorize(Policy = AdminTokenAuthenticationHandler.AdminPolicyName)]
    public IActionResult ExpireBadge(Guid assignmentId) =>
        Ok(phaseTwoStore.ExpireBadge(assignmentId));

    [HttpPost("badges/assignments/{assignmentId:guid}/suspend")]
    [Authorize(Policy = AdminTokenAuthenticationHandler.AdminPolicyName)]
    public IActionResult SuspendBadge(Guid assignmentId) =>
        Ok(phaseTwoStore.SuspendBadge(assignmentId));

    [HttpGet("renewals")]
    public IActionResult GetRenewals([FromQuery] Guid? assignmentId = null) =>
        Ok(phaseTwoStore.GetRenewals(assignmentId));

    [HttpPost("renewals/{assignmentId:guid}/pay")]
    public IActionResult PayRenewal(Guid assignmentId) =>
        Ok(phaseTwoStore.PayRenewal(assignmentId));

    [HttpGet("campaigns")]
    public IActionResult GetCampaigns() => Ok(phaseTwoStore.GetCampaigns());

    [HttpPost("campaigns")]
    [Authorize(Policy = AdminTokenAuthenticationHandler.AdminPolicyName)]
    public IActionResult CreateCampaign(CreateCampaignRequest request) =>
        Ok(phaseTwoStore.CreateCampaign(request));

    [HttpPost("campaigns/{campaignKey}/enroll")]
    public IActionResult EnrollCampaign(string campaignKey, EnrollCampaignRequest request) =>
        Ok(phaseTwoStore.EnrollCampaign(campaignKey, request));

    [HttpPost("founding-benefits")]
    [Authorize(Policy = AdminTokenAuthenticationHandler.AdminPolicyName)]
    public IActionResult UpsertFoundingBenefit(FoundingBenefitRequest request) =>
        Ok(phaseTwoStore.UpsertFoundingBenefit(request));

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
}
