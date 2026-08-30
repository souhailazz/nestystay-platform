using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestyStay.Api.Auth;
using NestyStay.Application.Admin;
using NestyStay.Application.PhaseTwo;
using NestyStay.Application.SpecCompletion;
using NestyStay.Domain;

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
    [Authorize(Policy = AdminAuthorizationPolicies.SystemConfiguration)]
    public IActionResult GetBadgeEligibility(PurchaseBadgeRequest request) =>
        Ok(phaseTwoStore.GetBadgeEligibility(request));

    [HttpPost("badges/purchase")]
    [Authorize(Policy = AdminAuthorizationPolicies.SystemConfiguration)]
    public async Task<IActionResult> PurchaseBadge(PurchaseBadgeRequest request, CancellationToken cancellationToken)
    {
        var assignment = phaseTwoStore.PurchaseBadge(request);
        await RecordSystemAuditAsync(
            "BadgeAssignmentCreated",
            "BadgeAssignment",
            assignment.Id,
            $"Administrator assigned {assignment.Level} badge to {assignment.SubjectType} {assignment.SubjectId}.",
            null,
            assignment,
            cancellationToken);
        return Ok(assignment);
    }

    [HttpGet("badges/assignments")]
    [Authorize]
    public IActionResult GetBadgeAssignments([FromQuery] string? subjectType = null, [FromQuery] Guid? subjectId = null)
    {
        if (!authorization.IsInRole(UserRole.Admin))
        {
            if (subjectId is null || string.IsNullOrWhiteSpace(subjectType))
            {
                throw new ForbiddenAccessException("Hosts may only read their own badge assignments.");
            }

            RequireBadgeSubjectAccess(subjectType, subjectId.Value);
        }

        return Ok(phaseTwoStore.GetBadgeAssignments(subjectType, subjectId));
    }

    [HttpGet("badges/features/{subjectType}/{subjectId:guid}")]
    [Authorize]
    public IActionResult GetFeatureAccess(string subjectType, Guid subjectId)
    {
        RequireBadgeSubjectAccess(subjectType, subjectId);
        return Ok(phaseTwoStore.GetFeatureAccess(subjectType, subjectId));
    }

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
    [Authorize]
    public IActionResult GetRenewals([FromQuery] Guid? assignmentId = null)
    {
        if (!authorization.IsInRole(UserRole.Admin))
        {
            if (assignmentId is null)
            {
                throw new ForbiddenAccessException("Hosts may only read renewals for their own badge assignment.");
            }

            RequireAssignmentAccess(assignmentId.Value);
        }

        return Ok(phaseTwoStore.GetRenewals(assignmentId));
    }

    [HttpPost("renewals/{assignmentId:guid}/pay")]
    [Authorize(Policy = AdminAuthorizationPolicies.SystemConfiguration)]
    public async Task<IActionResult> PayRenewal(Guid assignmentId, CancellationToken cancellationToken)
    {
        var previous = FindAssignment(assignmentId);
        var assignment = phaseTwoStore.PayRenewal(assignmentId);
        await RecordSystemAuditAsync("BadgeRenewalRecorded", "BadgeAssignment", assignmentId, "Administrator recorded a successful badge renewal.", previous, assignment, cancellationToken);
        return Ok(assignment);
    }

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
    [Authorize]
    public IActionResult EnrollCampaign(string campaignKey, EnrollCampaignRequest request)
    {
        RequireBadgeSubjectAccess(request.SubjectType, request.SubjectId);
        return Ok(phaseTwoStore.EnrollCampaign(campaignKey, request));
    }

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
    [Authorize]
    public IActionResult GetFoundingBenefit(Guid propertyId)
    {
        if (!authorization.IsInRole(UserRole.Admin))
        {
            var hostUserId = authorization.RequireHost();
            if (!authorization.HostOwnsProperty(hostUserId, propertyId))
            {
                throw new ForbiddenAccessException("Hosts may only read founding benefits for their own properties.");
            }
        }

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

    private void RequireAssignmentAccess(Guid assignmentId)
    {
        var assignment = FindAssignment(assignmentId)
            ?? throw new InvalidOperationException("Badge assignment not found.");
        RequireBadgeSubjectAccess(assignment.SubjectType, assignment.SubjectId);
    }

    private void RequireBadgeSubjectAccess(string subjectType, Guid subjectId)
    {
        if (authorization.IsInRole(UserRole.Admin))
        {
            return;
        }

        var hostUserId = authorization.RequireHost();
        if (!subjectType.Equals("Host", StringComparison.OrdinalIgnoreCase) || subjectId != hostUserId)
        {
            throw new ForbiddenAccessException("Hosts may only access badge data assigned to their own host account.");
        }
    }

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
