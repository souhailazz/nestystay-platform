using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NestyStay.Api.Auth;
using NestyStay.Application.Abstractions;
using NestyStay.Domain;
using NestyStay.Domain.Insurance;
using NestyStay.Infrastructure.Persistence;
using NestyStay.Infrastructure.Persistence.Milestones;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/insurance")]
public sealed class InsuranceController(
    IInsuranceProvider provider,
    NestyStayDbContext db,
    IResourceAuthorizationService authorization) : ControllerBase
{
    [HttpGet("plans")]
    public async Task<ActionResult<IReadOnlyList<InsurancePlan>>> GetPlans(CancellationToken cancellationToken) =>
        Ok(await provider.GetAvailablePlansAsync(cancellationToken));

    [Authorize]
    [HttpGet("properties/{propertyId:guid}")]
    public async Task<ActionResult<InsurancePropertyDto>> GetProperty(Guid propertyId, CancellationToken cancellationToken)
    {
        var property = await RequireReadablePropertyAsync(propertyId, cancellationToken);
        var policy = await db.InsurancePolicies.AsNoTracking()
            .Where(item => item.PropertyId == propertyId && !item.IsDeleted)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return Ok(ToPropertyDto(property, policy));
    }

    [Authorize]
    [HttpGet("properties/{propertyId:guid}/claims")]
    public async Task<ActionResult<IReadOnlyList<InsuranceClaimDto>>> GetClaims(Guid propertyId, CancellationToken cancellationToken)
    {
        await RequireReadablePropertyAsync(propertyId, cancellationToken);
        var claims = await db.InsuranceClaims.AsNoTracking()
            .Where(item => item.PropertyId == propertyId && !item.IsDeleted)
            .OrderByDescending(item => item.SubmittedAt)
            .ToListAsync(cancellationToken);
        return Ok(claims.Select(ToDto).ToList());
    }

    [Authorize]
    [HttpGet("properties/{propertyId:guid}/policy/events")]
    public async Task<ActionResult<IReadOnlyList<InsurancePolicyEventDto>>> GetPolicyEvents(Guid propertyId, CancellationToken cancellationToken)
    {
        await RequireReadablePropertyAsync(propertyId, cancellationToken);
        var events = await db.InsurancePolicyEvents.AsNoTracking()
            .Where(item => item.PropertyId == propertyId && !item.IsDeleted)
            .OrderBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
        return Ok(events.Select(ToDto).ToList());
    }

    [Authorize(Roles = "Host")]
    [HttpPost("properties/{propertyId:guid}/policy")]
    public async Task<ActionResult<InsurancePolicyDto>> Activate(Guid propertyId, InsuranceActivationRequest request, CancellationToken cancellationToken)
    {
        var hostUserId = authorization.RequireHost();
        var property = await db.MilestoneProperties.SingleOrDefaultAsync(item => item.Id == propertyId && item.HostUserId == hostUserId && !item.IsDeleted, cancellationToken)
            ?? throw new ForbiddenAccessException("The host does not own this property.");
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey)) return BadRequest("An idempotency key is required.");

        var existingByKey = await db.InsurancePolicies.SingleOrDefaultAsync(item => item.LastIdempotencyKey == request.IdempotencyKey && !item.IsDeleted, cancellationToken);
        if (existingByKey is not null) return Ok(ToDto(existingByKey));

        var plan = (await provider.GetAvailablePlansAsync(cancellationToken)).SingleOrDefault(item => item.Code.Equals(request.PlanCode.Trim(), StringComparison.OrdinalIgnoreCase));
        if (plan is null) return BadRequest("The requested InsuraGuest plan is not available.");

        var current = await db.InsurancePolicies
            .Where(item => item.PropertyId == propertyId && !item.IsDeleted && new[] { InsurancePolicyStatuses.PlanSelected, InsurancePolicyStatuses.Active, InsurancePolicyStatuses.Pending, InsurancePolicyStatuses.RenewalDue }.Contains(item.Status))
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (current is not null) return Conflict("This property already has an active or processing InsuraGuest policy.");

        var policy = new InsurancePolicy
        {
            PropertyId = propertyId,
            HostUserId = hostUserId,
            Provider = provider.ProviderName,
            PlanCode = plan.Code,
            Market = plan.Market,
            MonthlyAmount = plan.MonthlyAmount,
            Currency = plan.Currency,
            PropertyDamageCoverage = plan.PropertyDamageCoverage,
            AccidentalMedicalCoverage = plan.AccidentalMedicalCoverage,
            Status = InsurancePolicyStatuses.PlanSelected,
            LastIdempotencyKey = request.IdempotencyKey.Trim()
        };
        db.InsurancePolicies.Add(policy);
        db.InsurancePolicyEvents.Add(new InsurancePolicyEvent { PolicyId = policy.Id, PropertyId = propertyId, FromStatus = "NO_COVERAGE", ToStatus = InsurancePolicyStatuses.PlanSelected, Reason = "Host selected an InsuraGuest contract plan.", IdempotencyKey = request.IdempotencyKey.Trim() });
        await db.SaveChangesAsync(cancellationToken);

        policy.Status = InsurancePolicyStatuses.Pending;
        db.InsurancePolicyEvents.Add(new InsurancePolicyEvent { PolicyId = policy.Id, PropertyId = propertyId, FromStatus = InsurancePolicyStatuses.PlanSelected, ToStatus = InsurancePolicyStatuses.Pending, Reason = "Activation request submitted to the configured provider seam.", IdempotencyKey = request.IdempotencyKey.Trim() });
        await db.SaveChangesAsync(cancellationToken);

        var result = await provider.ActivateAsync(propertyId, plan, request.IdempotencyKey.Trim(), cancellationToken);
        var previousStatus = policy.Status;
        ApplyProviderResult(policy, result);
        db.InsurancePolicyEvents.Add(new InsurancePolicyEvent { PolicyId = policy.Id, PropertyId = propertyId, FromStatus = previousStatus, ToStatus = policy.Status, Reason = policy.FailureReason ?? "Provider activation result persisted.", IdempotencyKey = request.IdempotencyKey.Trim() });
        property.InsuraGuestEnabled = policy.Status == InsurancePolicyStatuses.Active;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(policy));
    }

    [Authorize(Roles = "Host")]
    [HttpPost("properties/{propertyId:guid}/policy/cancel")]
    public async Task<ActionResult<InsurancePolicyDto>> Cancel(Guid propertyId, InsuranceLifecycleRequest request, CancellationToken cancellationToken)
    {
        var hostUserId = authorization.RequireHost();
        var property = await db.MilestoneProperties.SingleOrDefaultAsync(item => item.Id == propertyId && item.HostUserId == hostUserId && !item.IsDeleted, cancellationToken)
            ?? throw new ForbiddenAccessException("The host does not own this property.");
        var policy = await LatestPolicyAsync(propertyId, cancellationToken);
        if (policy is null) return NotFound("No InsuraGuest policy exists for this property.");
        if (policy.Status is InsurancePolicyStatuses.Cancelled or InsurancePolicyStatuses.Failed) return Ok(ToDto(policy));
        var result = await provider.CancelAsync(policy.ProviderReference ?? $"local_{policy.Id:N}", request.IdempotencyKey, cancellationToken);
        var previousStatus = policy.Status;
        ApplyProviderResult(policy, result);
        db.InsurancePolicyEvents.Add(new InsurancePolicyEvent { PolicyId = policy.Id, PropertyId = propertyId, FromStatus = previousStatus, ToStatus = policy.Status, Reason = "Host cancelled InsuraGuest coverage.", IdempotencyKey = request.IdempotencyKey });
        property.InsuraGuestEnabled = false;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(policy));
    }

    [Authorize(Roles = "Host")]
    [HttpPost("properties/{propertyId:guid}/policy/renewal-due")]
    public async Task<ActionResult<InsurancePolicyDto>> MarkRenewalDue(Guid propertyId, CancellationToken cancellationToken)
    {
        var hostUserId = authorization.RequireHost();
        var property = await db.MilestoneProperties.SingleOrDefaultAsync(item => item.Id == propertyId && item.HostUserId == hostUserId && !item.IsDeleted, cancellationToken)
            ?? throw new ForbiddenAccessException("The host does not own this property.");
        var policy = await LatestPolicyAsync(propertyId, cancellationToken);
        if (policy is null) return NotFound("No InsuraGuest policy exists for this property.");
        if (policy.Status != InsurancePolicyStatuses.Active) return Conflict("Only an active policy can become renewal due.");
        var previousStatus = policy.Status;
        policy.Status = InsurancePolicyStatuses.RenewalDue;
        property.InsuraGuestEnabled = false;
        db.InsurancePolicyEvents.Add(new InsurancePolicyEvent { PolicyId = policy.Id, PropertyId = propertyId, FromStatus = previousStatus, ToStatus = policy.Status, Reason = "Policy renewal window opened.", IdempotencyKey = null });
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(policy));
    }

    [Authorize(Roles = "Host")]
    [HttpPost("properties/{propertyId:guid}/policy/renew")]
    public async Task<ActionResult<InsurancePolicyDto>> Renew(Guid propertyId, InsuranceLifecycleRequest request, CancellationToken cancellationToken)
    {
        var hostUserId = authorization.RequireHost();
        var property = await db.MilestoneProperties.SingleOrDefaultAsync(item => item.Id == propertyId && item.HostUserId == hostUserId && !item.IsDeleted, cancellationToken)
            ?? throw new ForbiddenAccessException("The host does not own this property.");
        var policy = await LatestPolicyAsync(propertyId, cancellationToken);
        if (policy is null) return NotFound("No InsuraGuest policy exists for this property.");
        if (policy.Status is not (InsurancePolicyStatuses.Active or InsurancePolicyStatuses.RenewalDue)) return Conflict("Only an active or renewal-due policy can be renewed.");
        var plan = (await provider.GetAvailablePlansAsync(cancellationToken)).Single(item => item.Code == policy.PlanCode);
        var previousStatus = policy.Status;
        var result = await provider.RenewAsync(policy.ProviderReference ?? $"local_{policy.Id:N}", plan, request.IdempotencyKey, cancellationToken);
        ApplyProviderResult(policy, result);
        db.InsurancePolicyEvents.Add(new InsurancePolicyEvent { PolicyId = policy.Id, PropertyId = propertyId, FromStatus = previousStatus, ToStatus = policy.Status, Reason = "Policy renewal result persisted.", IdempotencyKey = request.IdempotencyKey });
        property.InsuraGuestEnabled = policy.Status == InsurancePolicyStatuses.Active;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(policy));
    }

    [Authorize(Roles = "Host")]
    [HttpPost("properties/{propertyId:guid}/claims")]
    public async Task<ActionResult<InsuranceClaimDto>> SubmitClaim(Guid propertyId, InsuranceClaimRequest request, CancellationToken cancellationToken)
    {
        var hostUserId = authorization.RequireHost();
        var property = await db.MilestoneProperties.SingleOrDefaultAsync(item => item.Id == propertyId && item.HostUserId == hostUserId && !item.IsDeleted, cancellationToken)
            ?? throw new ForbiddenAccessException("The host does not own this property.");
        var policy = await LatestPolicyAsync(propertyId, cancellationToken);
        if (policy is null || policy.Status != InsurancePolicyStatuses.Active) return Conflict("An active InsuraGuest policy is required before submitting a claim.");
        if (request.IncidentAt > DateTimeOffset.UtcNow || DateTimeOffset.UtcNow - request.IncidentAt > TimeSpan.FromHours(72)) return BadRequest("Claims must be submitted within 72 hours of the incident.");
        if (string.IsNullOrWhiteSpace(request.Description)) return BadRequest("A claim description is required.");

        if (request.BookingId is { } bookingId && !await db.MilestoneBookings.AnyAsync(item => item.Id == bookingId && item.PropertyId == propertyId && !item.IsDeleted, cancellationToken))
            return BadRequest("The booking does not belong to this property.");

        var claim = new InsuranceClaim
        {
            PolicyId = policy.Id,
            PropertyId = propertyId,
            HostUserId = hostUserId,
            BookingId = request.BookingId,
            IncidentAt = request.IncidentAt,
            Description = request.Description.Trim(),
            EvidenceJson = string.IsNullOrWhiteSpace(request.EvidenceJson) ? "[]" : request.EvidenceJson,
            Status = InsuranceClaimStatuses.Submitted,
            ProviderReference = $"ig_claim_{Guid.NewGuid():N}",
            SubmittedAt = DateTimeOffset.UtcNow
        };
        db.InsuranceClaims.Add(claim);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(claim));
    }

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpGet("managed")]
    public async Task<ActionResult<IReadOnlyList<ManagedInsuranceDto>>> GetManaged(CancellationToken cancellationToken)
    {
        var actor = authorization.RequireSignedInUser();
        var isAdmin = authorization.IsInRole(UserRole.Admin);
        var mappings = await db.MilestoneManagerProperties.AsNoTracking()
            .Where(item => !item.IsDeleted && (isAdmin || item.ManagerUserId == actor))
            .ToListAsync(cancellationToken);
        var listingIds = mappings.Where(item => item.RentalListingId.HasValue).Select(item => item.RentalListingId!.Value).ToArray();
        var properties = await db.MilestoneProperties.AsNoTracking()
            .Where(item => !item.IsDeleted && (isAdmin || item.HostUserId == actor || listingIds.Contains(item.Id)))
            .ToListAsync(cancellationToken);
        var policies = await db.InsurancePolicies.AsNoTracking().Where(item => !item.IsDeleted && properties.Select(property => property.Id).Contains(item.PropertyId)).ToListAsync(cancellationToken);
        return Ok(properties.Select(property =>
        {
            var policy = policies.Where(item => item.PropertyId == property.Id).OrderByDescending(item => item.CreatedAt).FirstOrDefault();
            return new ManagedInsuranceDto(property.Id, property.Title, property.HostUserId, policy is null ? "NO_COVERAGE" : policy.Status, policy?.PlanCode, policy?.Provider, policy?.ProviderReference, policy?.RenewsAt, policy?.FailureReason);
        }).ToList());
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("policies")]
    public async Task<ActionResult<IReadOnlyList<InsurancePolicyDto>>> GetPolicies(CancellationToken cancellationToken) =>
        Ok((await db.InsurancePolicies.AsNoTracking().Where(item => !item.IsDeleted).OrderByDescending(item => item.CreatedAt).ToListAsync(cancellationToken)).Select(ToDto).ToList());

    private async Task<MilestoneProperty> RequireReadablePropertyAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        var property = await db.MilestoneProperties.AsNoTracking().SingleOrDefaultAsync(item => item.Id == propertyId && !item.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("Property was not found.");
        var actor = authorization.TryGetSignedInUser();
        if (actor == property.HostUserId || authorization.IsInRole(UserRole.Admin)) return property;
        if (actor is { } userId && (await db.MilestoneManagerProperties.AnyAsync(item => !item.IsDeleted && item.RentalListingId == propertyId && (item.ManagerUserId == userId || item.OwnerUserId == userId), cancellationToken))) return property;
        throw new ForbiddenAccessException("You do not have access to this property insurance record.");
    }

    private async Task<InsurancePolicy?> LatestPolicyAsync(Guid propertyId, CancellationToken cancellationToken) =>
        await db.InsurancePolicies.Where(item => item.PropertyId == propertyId && !item.IsDeleted).OrderByDescending(item => item.CreatedAt).FirstOrDefaultAsync(cancellationToken);

    private static void ApplyProviderResult(InsurancePolicy policy, InsuranceProviderResult result)
    {
        policy.Provider = result.ProviderName;
        policy.Status = result.Status.ToUpperInvariant();
        policy.ProviderReference = result.ProviderReference ?? policy.ProviderReference;
        policy.FailureReason = result.FailureReason;
        policy.EffectiveAt = result.EffectiveAt ?? policy.EffectiveAt;
        policy.RenewsAt = result.RenewsAt ?? policy.RenewsAt;
        policy.CancelledAt = policy.Status == InsurancePolicyStatuses.Cancelled ? DateTimeOffset.UtcNow : policy.CancelledAt;
        policy.LastProviderEventAt = DateTimeOffset.UtcNow;
    }

    private static InsurancePropertyDto ToPropertyDto(MilestoneProperty property, InsurancePolicy? policy) => new(property.Id, property.Title, property.InsuraGuestEnabled, policy is null ? "NO_COVERAGE" : policy.Status, policy is null ? null : ToDto(policy));
    private static InsurancePolicyDto ToDto(InsurancePolicy policy) => new(policy.Id, policy.PropertyId, policy.HostUserId, policy.Provider, policy.PlanCode, policy.Market, policy.MonthlyAmount, policy.Currency, policy.PropertyDamageCoverage, policy.AccidentalMedicalCoverage, policy.Status, policy.ProviderReference, policy.EffectiveAt, policy.RenewsAt, policy.CancelledAt, policy.FailureReason);
    private static InsuranceClaimDto ToDto(InsuranceClaim claim) => new(claim.Id, claim.PolicyId, claim.PropertyId, claim.BookingId, claim.IncidentAt, claim.Description, claim.EvidenceJson, claim.Status, claim.ProviderReference, claim.SubmittedAt);
    private static InsurancePolicyEventDto ToDto(InsurancePolicyEvent item) => new(item.Id, item.PolicyId, item.PropertyId, item.FromStatus, item.ToStatus, item.Reason, item.IdempotencyKey, item.CreatedAt);
}

public sealed record InsuranceActivationRequest(string PlanCode, string IdempotencyKey);
public sealed record InsuranceLifecycleRequest(string IdempotencyKey);
public sealed record InsuranceClaimRequest(DateTimeOffset IncidentAt, string Description, Guid? BookingId = null, string EvidenceJson = "[]");
public sealed record InsurancePropertyDto(Guid PropertyId, string PropertyTitle, bool CoverageFlag, string Status, InsurancePolicyDto? Policy);
public sealed record InsurancePolicyDto(Guid Id, Guid PropertyId, Guid HostUserId, string Provider, string PlanCode, string Market, decimal MonthlyAmount, string Currency, decimal PropertyDamageCoverage, decimal AccidentalMedicalCoverage, string Status, string? ProviderReference, DateTimeOffset? EffectiveAt, DateTimeOffset? RenewsAt, DateTimeOffset? CancelledAt, string? FailureReason);
public sealed record InsuranceClaimDto(Guid Id, Guid PolicyId, Guid PropertyId, Guid? BookingId, DateTimeOffset IncidentAt, string Description, string EvidenceJson, string Status, string? ProviderReference, DateTimeOffset SubmittedAt);
public sealed record InsurancePolicyEventDto(Guid Id, Guid PolicyId, Guid PropertyId, string FromStatus, string ToStatus, string Reason, string? IdempotencyKey, DateTimeOffset CreatedAt);
public sealed record ManagedInsuranceDto(Guid PropertyId, string PropertyTitle, Guid HostUserId, string Status, string? PlanCode, string? Provider, string? ProviderReference, DateTimeOffset? RenewsAt, string? FailureReason);
