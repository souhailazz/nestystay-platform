using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestyStay.Api.Auth;
using NestyStay.Application.PropertyManager;
using NestyStay.Domain;

namespace NestyStay.Api.Controllers;

/// <summary>Money, authority and owner-portal endpoints introduced by P0.</summary>
[ApiController]
[Route("api/property-manager/p0")]
[Authorize]
public sealed class PropertyManagerP0Controller(IPropertyManagerP0Store store, IResourceAuthorizationService authorization) : ControllerBase
{
    private P0Actor Actor() => new(authorization.RequireSignedInUser(), authorization.IsInRole(UserRole.Admin), authorization.IsInRole(UserRole.PropertyManager));

    [HttpGet("owners/{ownerUserId:guid}/profile")] public async Task<IActionResult> OwnerProfile(Guid ownerUserId, CancellationToken ct) => (await store.GetOwnerProfileAsync(Actor(), ownerUserId, ct)) is { } value ? Ok(value) : NotFound();
    [HttpPut("owners/{ownerUserId:guid}/profile")] public async Task<IActionResult> OwnerProfile(Guid ownerUserId, UpsertP0OwnerProfileRequest request, CancellationToken ct) => Ok(await store.UpsertOwnerProfileAsync(Actor(), ownerUserId, request, ct));
    [HttpPost("owners/{ownerUserId:guid}/status")] public async Task<IActionResult> OwnerStatus(Guid ownerUserId, P0StatusChangeRequest request, CancellationToken ct) => (await store.ChangeOwnerStatusAsync(Actor(), ownerUserId, request, ct)) is { } value ? Ok(value) : NotFound();
    [HttpGet("owners/{ownerUserId:guid}/lifecycle")] public async Task<IActionResult> OwnerLifecycle(Guid ownerUserId, CancellationToken ct) => Ok(await store.ListOwnerLifecycleAsync(Actor(), ownerUserId, ct));
    [HttpGet("portfolio")] public async Task<IActionResult> Portfolio([FromQuery] P0PortfolioQuery query, CancellationToken ct) => Ok(await store.GetPortfolioAsync(Actor(), query, ct));
    [HttpGet("assignments/history")] public async Task<IActionResult> AssignmentHistory([FromQuery] Guid? propertyId, CancellationToken ct) => Ok(await store.GetAssignmentHistoryAsync(Actor(), propertyId, ct));
    [HttpPost("assignments")] public async Task<IActionResult> Assignments(P0AssignPropertiesRequest request, CancellationToken ct) => Ok(await store.AssignPropertiesAsync(Actor(), request, ct));

    [HttpPost("agreements")] public async Task<IActionResult> CreateAgreement(P0CreateAgreementRequest request, CancellationToken ct) => Ok(await store.CreateAgreementAsync(Actor(), request, ct));
    [HttpGet("agreements")] public async Task<IActionResult> Agreements([FromQuery] P0AgreementQuery query, CancellationToken ct) => Ok(await store.ListAgreementsAsync(Actor(), query, ct));
    [HttpGet("agreements/{agreementId:guid}")] public async Task<IActionResult> Agreement(Guid agreementId, CancellationToken ct) => (await store.GetAgreementAsync(Actor(), agreementId, ct)) is { } value ? Ok(value) : NotFound();
    [HttpPut("agreements/{agreementId:guid}/draft")] public async Task<IActionResult> UpdateAgreement(Guid agreementId, P0UpdateAgreementRequest request, CancellationToken ct) => (await store.UpdateAgreementDraftAsync(Actor(), agreementId, request, ct)) is { } value ? Ok(value) : NotFound();
    [HttpPost("agreements/{agreementId:guid}/activate")] public async Task<IActionResult> ActivateAgreement(Guid agreementId, CancellationToken ct) => (await store.ActivateAgreementAsync(Actor(), agreementId, ct)) is { } value ? Ok(value) : NotFound();
    [HttpPost("agreements/{agreementId:guid}/renew")] public async Task<IActionResult> RenewAgreement(Guid agreementId, P0RenewAgreementRequest request, CancellationToken ct) => (await store.RenewAgreementAsync(Actor(), agreementId, request, ct)) is { } value ? Ok(value) : NotFound();
    [HttpPost("agreements/{agreementId:guid}/terminate")] public async Task<IActionResult> TerminateAgreement(Guid agreementId, P0TerminateAgreementRequest request, CancellationToken ct) => (await store.TerminateAgreementAsync(Actor(), agreementId, request, ct)) is { } value ? Ok(value) : NotFound();

    [HttpPost("fees")] public async Task<IActionResult> CreateFeeRule(P0CreateFeeRuleRequest request, CancellationToken ct) => Ok(await store.CreateFeeRuleAsync(Actor(), request, ct));
    [HttpGet("fees")] public async Task<IActionResult> FeeRules([FromQuery] P0FeeRuleQuery query, CancellationToken ct) => Ok(await store.ListFeeRulesAsync(Actor(), query, ct));
    [HttpPost("fees/calculate")] public async Task<IActionResult> CalculateFee(P0CalculateFeeRequest request, CancellationToken ct) => Ok(await store.CalculateFeeAsync(Actor(), request, ct));
    [HttpPost("fees/post")] public async Task<IActionResult> PostFee(P0PostFeeRequest request, CancellationToken ct) => Ok(await store.PostFeeAsync(Actor(), request, ct));

    [HttpGet("accounting/accounts")] public async Task<IActionResult> Accounts([FromQuery] string? currency, CancellationToken ct) => Ok(await store.ListAccountsAsync(Actor(), currency, ct));
    [HttpPost("accounting/journals")] public async Task<IActionResult> Journal(P0PostJournalRequest request, CancellationToken ct) => Ok(await store.PostJournalAsync(Actor(), request, ct));
    [HttpGet("accounting/journals")] public async Task<IActionResult> Journals([FromQuery] P0JournalQuery query, CancellationToken ct) => Ok(await store.ListJournalsAsync(Actor(), query, ct));
    [HttpPost("accounting/journals/{journalId:guid}/reverse")] public async Task<IActionResult> Reverse(Guid journalId, P0ReverseJournalRequest request, CancellationToken ct) => (await store.ReverseJournalAsync(Actor(), journalId, request, ct)) is { } value ? Ok(value) : NotFound();
    [HttpPost("accounting/reconcile")] public async Task<IActionResult> Reconcile(P0ReconcileJournalRequest request, CancellationToken ct) => Ok(await store.ReconcileJournalAsync(Actor(), request, ct));
    [HttpGet("accounting/reconcile")] public async Task<IActionResult> Reconciliations([FromQuery] P0JournalQuery query, CancellationToken ct) => Ok(await store.ListReconciliationsAsync(Actor(), query, ct));

    [HttpGet("statements")] public async Task<IActionResult> Statement([FromQuery] P0StatementQuery query, CancellationToken ct) => Ok(await store.BuildStatementAsync(Actor(), query, ct));
    [HttpPost("statements/finalize")] public async Task<IActionResult> FinalizeStatement(P0FinalizeStatementRequest request, CancellationToken ct) => Ok(await store.FinalizeStatementAsync(Actor(), request, ct));
    [HttpGet("statements/{snapshotId:guid}/export")] public async Task<IActionResult> ExportStatement(Guid snapshotId, [FromQuery] string format = "csv", CancellationToken ct = default) => Ok(await store.ExportStatementAsync(Actor(), snapshotId, format, ct));
    [HttpGet("profitability")] public async Task<IActionResult> Profitability([FromQuery] P0ProfitabilityQuery query, CancellationToken ct) => Ok(await store.GetProfitabilityAsync(Actor(), query, ct));

    [HttpPost("approvals")] public async Task<IActionResult> CreateApproval(P0CreateApprovalRequest request, CancellationToken ct) => Ok(await store.CreateApprovalAsync(Actor(), request, ct));
    [HttpGet("approvals")] public async Task<IActionResult> Approvals([FromQuery] P0ApprovalQuery query, CancellationToken ct) => Ok(await store.ListApprovalsAsync(Actor(), query, ct));
    [HttpPost("approvals/{approvalId:guid}/decision")] public async Task<IActionResult> DecideApproval(Guid approvalId, P0ApprovalDecisionRequest request, CancellationToken ct) => (await store.DecideApprovalAsync(Actor(), approvalId, request, ct)) is { } value ? Ok(value) : NotFound();

    [HttpPost("members")] public async Task<IActionResult> InviteStaff(P0InviteStaffRequest request, CancellationToken ct) => Ok(await store.InviteStaffAsync(Actor(), request, ct));
    [HttpGet("members")] public async Task<IActionResult> Staff(CancellationToken ct) => Ok(await store.ListStaffAsync(Actor(), ct));
    [HttpGet("members/{membershipId:guid}/history")] public async Task<IActionResult> StaffHistory(Guid membershipId, CancellationToken ct) => Ok(await store.ListStaffHistoryAsync(Actor(), membershipId, ct));
    [HttpPost("members/{membershipId:guid}/accept")] public async Task<IActionResult> AcceptStaff(Guid membershipId, CancellationToken ct) => (await store.AcceptStaffAsync(Actor(), membershipId, ct)) is { } value ? Ok(value) : NotFound();
    [HttpPatch("members/{membershipId:guid}")] public async Task<IActionResult> UpdateStaff(Guid membershipId, P0UpdateStaffRequest request, CancellationToken ct) => (await store.UpdateStaffAsync(Actor(), membershipId, request, ct)) is { } value ? Ok(value) : NotFound();
    [HttpPost("members/{membershipId:guid}/revoke")] public async Task<IActionResult> RevokeStaff(Guid membershipId, P0StatusChangeRequest request, CancellationToken ct) => (await store.RevokeStaffAsync(Actor(), membershipId, request, ct)) is { } value ? Ok(value) : NotFound();

    [HttpGet("payouts/availability")] public async Task<IActionResult> PayoutAvailability([FromQuery] Guid ownerUserId, [FromQuery] string currency, [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct) => Ok(await store.GetPayoutAvailabilityAsync(Actor(), ownerUserId, currency, from, to, ct));
    [HttpPost("payouts/batches")] public async Task<IActionResult> CreatePayout(P0CreatePayoutBatchRequest request, CancellationToken ct) => Ok(await store.CreatePayoutBatchAsync(Actor(), request, ct));
    [HttpGet("payouts/batches")] public async Task<IActionResult> Payouts([FromQuery] P0PayoutQuery query, CancellationToken ct) => Ok(await store.ListPayoutBatchesAsync(Actor(), query, ct));
    [HttpPost("payouts/batches/{batchId:guid}/approve")] public async Task<IActionResult> ApprovePayout(Guid batchId, P0PayoutDecisionRequest request, CancellationToken ct) => (await store.ApprovePayoutBatchAsync(Actor(), batchId, request, ct)) is { } value ? Ok(value) : NotFound();
    [HttpPost("payouts/batches/{batchId:guid}/process")] public async Task<IActionResult> ProcessPayout(Guid batchId, P0PayoutProcessRequest request, CancellationToken ct) => (await store.ProcessPayoutBatchAsync(Actor(), batchId, request, ct)) is { } value ? Ok(value) : NotFound();
    [HttpPost("payouts/batches/{batchId:guid}/cancel")] public async Task<IActionResult> CancelPayout(Guid batchId, P0PayoutDecisionRequest request, CancellationToken ct) => (await store.CancelPayoutBatchAsync(Actor(), batchId, request, ct)) is { } value ? Ok(value) : NotFound();
    [HttpPost("payouts/batches/{batchId:guid}/retry")] public async Task<IActionResult> RetryPayout(Guid batchId, CancellationToken ct) => (await store.RetryPayoutBatchAsync(Actor(), batchId, ct)) is { } value ? Ok(value) : NotFound();

    [Authorize(Roles = "Owner")]
    [HttpGet("owner/portal")]
    public async Task<IActionResult> OwnerPortal([FromQuery] Guid? managerUserId, CancellationToken ct) => Ok(await store.GetOwnerPortalAsync(Actor(), managerUserId, ct));
}
