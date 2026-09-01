using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestyStay.Api.Auth;
using NestyStay.Application.PropertyManager;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/property-manager")]
public sealed class PropertyManagerController(IPropertyManagerStore store, IResourceAuthorizationService authorization) : ControllerBase
{
    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken) => Ok(await store.GetDashboardAsync(Actor(), authorization.IsInRole(NestyStay.Domain.UserRole.Admin), cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("owners")]
    public async Task<IActionResult> InviteOwner(InviteOwnerRequest request, CancellationToken cancellationToken) => Ok(await store.InviteOwnerAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("owners/{ownerUserId:guid}/verification")]
    public async Task<IActionResult> ReviewOwner(Guid ownerUserId, ReviewOwnerRequest request, CancellationToken cancellationToken) => (await store.ReviewOwnerAsync(Actor(), ownerUserId, request.Status, cancellationToken)) is { } result ? Ok(result) : NotFound();

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("subscription/renew")]
    public async Task<IActionResult> RenewSubscription(CancellationToken cancellationToken) => Ok(await store.RenewSubscriptionAsync(Actor(), cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("properties")]
    public async Task<IActionResult> AddProperty(AddPropertyRequest request, CancellationToken cancellationToken) => Ok(await store.AddPropertyAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("invoices")]
    public async Task<IActionResult> CreateInvoice(CreateInvoiceRequest request, CancellationToken cancellationToken) => Ok(await store.CreateInvoiceAsync(Actor(), request, cancellationToken));

    [Authorize]
    [HttpGet("invoices/{invoiceId:guid}")]
    public async Task<IActionResult> GetInvoice(Guid invoiceId, CancellationToken cancellationToken) => (await store.GetInvoiceAsync(Actor(), authorization.IsInRole(NestyStay.Domain.UserRole.Admin), invoiceId, cancellationToken)) is { } result ? Ok(result) : NotFound();

    [Authorize]
    [HttpPost("invoices/{invoiceId:guid}/payments")]
    public async Task<IActionResult> PayInvoice(Guid invoiceId, PayInvoiceRequest request, CancellationToken cancellationToken) => (await store.PayInvoiceAsync(Actor(), authorization.IsInRole(NestyStay.Domain.UserRole.Admin), invoiceId, request, cancellationToken)) is { } result ? Ok(result) : NotFound();

    [Authorize]
    [HttpGet("owners/{ownerUserId:guid}/statement")]
    public async Task<IActionResult> Statement(Guid ownerUserId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    { if (!authorization.IsInRole(NestyStay.Domain.UserRole.Admin) && !authorization.IsInRole(NestyStay.Domain.UserRole.PropertyManager) && Actor() != ownerUserId) return Forbid(); return Ok(await store.GetStatementAsync(Actor(), authorization.IsInRole(NestyStay.Domain.UserRole.Admin), ownerUserId, from, to, cancellationToken)); }

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("utilities")]
    public async Task<IActionResult> Utility(CreateUtilityRequest request, CancellationToken cancellationToken) => Ok(await store.CreateUtilityAsync(Actor(), request, cancellationToken));

    [Authorize]
    [HttpPost("maintenance")]
    public async Task<IActionResult> CreateMaintenance(CreateMaintenanceRequest request, CancellationToken cancellationToken)
    { var isAdmin = authorization.IsInRole(NestyStay.Domain.UserRole.Admin); var isManager = authorization.IsInRole(NestyStay.Domain.UserRole.PropertyManager); if (!isAdmin && !isManager && !authorization.IsInRole(NestyStay.Domain.UserRole.Owner)) return Forbid(); var effective = isManager || isAdmin ? request : request with { OwnerUserId = Actor() }; return Ok(await store.CreateMaintenanceAsync(Actor(), isAdmin, effective, cancellationToken)); }

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPatch("maintenance/{maintenanceId:guid}")]
    public async Task<IActionResult> UpdateMaintenance(Guid maintenanceId, UpdateMaintenanceRequest request, CancellationToken cancellationToken) => (await store.UpdateMaintenanceAsync(Actor(), maintenanceId, request, cancellationToken)) is { } result ? Ok(result) : NotFound();

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("vendors")]
    public async Task<IActionResult> Vendor(CreateVendorRequest request, CancellationToken cancellationToken) => Ok(await store.CreateVendorAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("notices")]
    public async Task<IActionResult> Notice(CreateNoticeRequest request, CancellationToken cancellationToken) => Ok(await store.CreateNoticeAsync(Actor(), request, cancellationToken));

    [Authorize]
    [HttpGet("notices")]
    public async Task<IActionResult> Notices(CancellationToken cancellationToken) => Ok(await store.GetNoticesAsync(Actor(), authorization.IsInRole(NestyStay.Domain.UserRole.Admin), cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("governance/proposals")]
    public async Task<IActionResult> Proposal(CreateProposalRequest request, CancellationToken cancellationToken) => Ok(await store.CreateProposalAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "Owner,PropertyManager,Admin")]
    [HttpPost("governance/proposals/{proposalId:guid}/votes")]
    public async Task<IActionResult> Vote(Guid proposalId, VoteRequest request, CancellationToken cancellationToken) => Ok(await store.VoteAsync(Actor(), authorization.IsInRole(NestyStay.Domain.UserRole.Admin), proposalId, request, cancellationToken));

    [Authorize(Roles = "Owner")]
    [HttpPost("governance/proxies")]
    public async Task<IActionResult> Proxy(CreateProxyRequest request, CancellationToken cancellationToken) => Ok(await store.CreateProxyAsync(Actor(), request, cancellationToken));

    [Authorize]
    [HttpGet("documents")]
    public async Task<IActionResult> Documents(CancellationToken cancellationToken) => Ok(await store.GetDocumentsAsync(Actor(), authorization.IsInRole(NestyStay.Domain.UserRole.Admin), cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("documents")]
    public async Task<IActionResult> Document(AddDocumentRequest request, CancellationToken cancellationToken) => Ok(await store.AddDocumentAsync(Actor(), request, cancellationToken));

    [Authorize]
    [HttpGet("documents/{documentId:guid}/download")]
    public async Task<IActionResult> DownloadDocument(Guid documentId, CancellationToken cancellationToken)
    {
        var result = await store.GetDocumentDownloadAsync(Actor(), authorization.IsInRole(NestyStay.Domain.UserRole.Admin), documentId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("gate/messages")]
    public async Task<IActionResult> GateMessage(CreateGateMessageRequest request, CancellationToken cancellationToken) => Ok(await store.CreateGateMessageAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("qr")]
    public async Task<IActionResult> IssueQr(IssueQrRequest request, CancellationToken cancellationToken) => Ok(await store.IssueQrAsync(Actor(), request, cancellationToken));

    [AllowAnonymous]
    [HttpPost("qr/validate")]
    public async Task<IActionResult> ValidateQr(ValidateQrRequest request, CancellationToken cancellationToken) => Ok(await store.ValidateQrAsync(request.Token, request.PropertyId, authorization.TryGetSignedInUser(), cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("qr/{qrId:guid}/revoke")]
    public async Task<IActionResult> RevokeQr(Guid qrId, CancellationToken cancellationToken) => Ok(await store.RevokeQrAsync(Actor(), qrId, cancellationToken));

    [Authorize(Roles = "Owner")]
    [HttpGet("owner/portal")]
    public async Task<IActionResult> OwnerPortal(CancellationToken cancellationToken) => Ok(await store.GetOwnerPortalAsync(Actor(), cancellationToken));

    private Guid Actor() => authorization.RequireSignedInUser();
}

public sealed record ReviewOwnerRequest(string Status);
public sealed record ValidateQrRequest(string Token, Guid? PropertyId = null);
