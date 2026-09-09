using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using NestyStay.Api.Auth;
using NestyStay.Api.Configuration;
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
    [HttpPost("properties/bulk-assign")]
    public async Task<IActionResult> BulkAssignProperties(BulkAssignPropertiesRequest request, CancellationToken cancellationToken) => Ok(await store.BulkAssignPropertiesAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpGet("properties/assignment-history")]
    public async Task<IActionResult> AssignmentHistory([FromQuery] Guid? propertyId, CancellationToken cancellationToken) => Ok(await store.GetPropertyAssignmentHistoryAsync(Actor(), propertyId, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("invoices")]
    public async Task<IActionResult> CreateInvoice(CreateInvoiceRequest request, CancellationToken cancellationToken) => Ok(await store.CreateInvoiceAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("invoices/bulk-issue")]
    public async Task<IActionResult> BulkIssueInvoices(BulkIssueInvoicesRequest request, CancellationToken cancellationToken) => Ok(await store.BulkIssueInvoicesAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("invoices/mark-overdue")]
    public async Task<IActionResult> MarkOverdueInvoices(CancellationToken cancellationToken) => Ok(await store.MarkOverdueInvoicesAsync(Actor(), cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPut("invoices/{invoiceId:guid}")]
    public async Task<IActionResult> UpdateInvoice(Guid invoiceId, UpdateInvoiceRequest request, CancellationToken cancellationToken) =>
        (await store.UpdateInvoiceAsync(Actor(), invoiceId, request, cancellationToken)) is { } result ? Ok(result) : NotFound();

    [Authorize]
    [HttpGet("invoices/{invoiceId:guid}")]
    public async Task<IActionResult> GetInvoice(Guid invoiceId, CancellationToken cancellationToken) => (await store.GetInvoiceAsync(Actor(), authorization.IsInRole(NestyStay.Domain.UserRole.Admin), invoiceId, cancellationToken)) is { } result ? Ok(result) : NotFound();

    [Authorize]
    [HttpPost("invoices/{invoiceId:guid}/payments")]
    [EnableRateLimiting(RateLimitPolicies.SensitiveAction)]
    public async Task<IActionResult> PayInvoice(Guid invoiceId, PayInvoiceRequest request, CancellationToken cancellationToken) => (await store.PayInvoiceAsync(Actor(), authorization.IsInRole(NestyStay.Domain.UserRole.Admin), invoiceId, request, cancellationToken)) is { } result ? Ok(result) : NotFound();

    [Authorize(Roles = "PropertyManager,Owner,Admin")]
    [HttpGet("payments")]
    public async Task<IActionResult> Payments([FromQuery] Guid? ownerUserId, [FromQuery] string? status, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) => Ok(await store.ListPaymentsAsync(Actor(), authorization.IsInRole(NestyStay.Domain.UserRole.Admin), new PaymentQuery(ownerUserId, status, from, to), cancellationToken));

    [Authorize(Roles = "PropertyManager,Owner,Admin")]
    [HttpGet("payment-methods")]
    public async Task<IActionResult> PaymentMethods([FromQuery] Guid? ownerUserId, CancellationToken cancellationToken) => Ok(await store.ListPaymentMethodsAsync(Actor(), authorization.IsInRole(NestyStay.Domain.UserRole.Admin), ownerUserId, cancellationToken));

    [Authorize(Roles = "PropertyManager,Owner,Admin")]
    [HttpPost("payment-methods")]
    public async Task<IActionResult> SavePaymentMethod(SavePaymentMethodRequest request, CancellationToken cancellationToken) => Ok(await store.SavePaymentMethodAsync(Actor(), authorization.IsInRole(NestyStay.Domain.UserRole.Admin), request, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("payments/{paymentId:guid}/refund")]
    public async Task<IActionResult> RefundPayment(Guid paymentId, RefundPaymentRequest request, CancellationToken cancellationToken) => (await store.RefundPaymentAsync(Actor(), authorization.IsInRole(NestyStay.Domain.UserRole.Admin), paymentId, request, cancellationToken)) is { } result ? Ok(result) : NotFound();

    [Authorize(Roles = "PropertyManager,Owner,Admin")]
    [HttpPost("payments/{paymentId:guid}/retry")]
    public async Task<IActionResult> RetryPayment(Guid paymentId, CancellationToken cancellationToken) => (await store.RetryPaymentAsync(Actor(), authorization.IsInRole(NestyStay.Domain.UserRole.Admin), paymentId, cancellationToken)) is { } result ? Ok(result) : NotFound();

    [Authorize]
    [HttpGet("owners/{ownerUserId:guid}/statement")]
    public async Task<IActionResult> Statement(Guid ownerUserId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    { if (!authorization.IsInRole(NestyStay.Domain.UserRole.Admin) && !authorization.IsInRole(NestyStay.Domain.UserRole.PropertyManager) && Actor() != ownerUserId) return Forbid(); return Ok(await store.GetStatementAsync(Actor(), authorization.IsInRole(NestyStay.Domain.UserRole.Admin), ownerUserId, from, to, cancellationToken)); }

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("utilities")]
    public async Task<IActionResult> Utility(CreateUtilityRequest request, CancellationToken cancellationToken) => Ok(await store.CreateUtilityAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("utilities/readings")]
    public async Task<IActionResult> MeterReading(RecordMeterReadingRequest request, CancellationToken cancellationToken) => Ok(await store.RecordMeterReadingAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpGet("utilities/{propertyId:guid}/readings")]
    public async Task<IActionResult> MeterReadings(Guid propertyId, [FromQuery] string? utilityType, CancellationToken cancellationToken) => Ok(await store.ListMeterReadingsAsync(Actor(), propertyId, utilityType, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("utilities/schedules")]
    public async Task<IActionResult> UtilitySchedule(SaveUtilityScheduleRequest request, CancellationToken cancellationToken) => Ok(await store.SaveUtilityScheduleAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "PropertyManager,Owner,Admin")]
    [HttpGet("utilities/disputes")]
    public async Task<IActionResult> UtilityDisputes(CancellationToken cancellationToken) => Ok(await store.ListUtilityDisputesAsync(Actor(), cancellationToken));

    [Authorize(Roles = "PropertyManager,Owner,Admin")]
    [HttpPost("utilities/disputes")]
    public async Task<IActionResult> CreateUtilityDispute(CreateUtilityDisputeRequest request, CancellationToken cancellationToken) => Ok(await store.CreateUtilityDisputeAsync(Actor(), authorization.IsInRole(NestyStay.Domain.UserRole.Admin), request, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("utilities/disputes/{disputeId:guid}/decide")]
    public async Task<IActionResult> DecideUtilityDispute(Guid disputeId, DecideUtilityDisputeRequest request, CancellationToken cancellationToken) => (await store.DecideUtilityDisputeAsync(Actor(), disputeId, request, cancellationToken)) is { } result ? Ok(result) : NotFound();

    [Authorize]
    [HttpPost("maintenance")]
    public async Task<IActionResult> CreateMaintenance(CreateMaintenanceRequest request, CancellationToken cancellationToken)
    { var isAdmin = authorization.IsInRole(NestyStay.Domain.UserRole.Admin); var isManager = authorization.IsInRole(NestyStay.Domain.UserRole.PropertyManager); if (!isAdmin && !isManager && !authorization.IsInRole(NestyStay.Domain.UserRole.Owner)) return Forbid(); var effective = isManager || isAdmin ? request : request with { OwnerUserId = Actor() }; return Ok(await store.CreateMaintenanceAsync(Actor(), isAdmin, effective, cancellationToken)); }

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPatch("maintenance/{maintenanceId:guid}")]
    public async Task<IActionResult> UpdateMaintenance(Guid maintenanceId, UpdateMaintenanceRequest request, CancellationToken cancellationToken) => (await store.UpdateMaintenanceAsync(Actor(), maintenanceId, request, cancellationToken)) is { } result ? Ok(result) : NotFound();

    [Authorize]
    [HttpGet("maintenance/{maintenanceId:guid}/activity")]
    public async Task<IActionResult> MaintenanceActivity(Guid maintenanceId, CancellationToken cancellationToken) => Ok(await store.ListMaintenanceActivityAsync(Actor(), authorization.IsInRole(NestyStay.Domain.UserRole.Admin), maintenanceId, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("maintenance/attachments")]
    public async Task<IActionResult> MaintenanceAttachment(AddMaintenanceAttachmentRequest request, CancellationToken cancellationToken) => Ok(await store.AddMaintenanceAttachmentAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("vendors")]
    public async Task<IActionResult> Vendor(CreateVendorRequest request, CancellationToken cancellationToken) => Ok(await store.CreateVendorAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPatch("vendors/{vendorId:guid}")]
    public async Task<IActionResult> UpdateVendor(Guid vendorId, UpdateVendorRequest request, CancellationToken cancellationToken) => (await store.UpdateVendorAsync(Actor(), vendorId, request, cancellationToken)) is { } result ? Ok(result) : NotFound();

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("vendors/documents")]
    public async Task<IActionResult> VendorDocument(AddVendorDocumentRequest request, CancellationToken cancellationToken) => Ok(await store.AddVendorDocumentAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpGet("vendors/{vendorId:guid}/documents")]
    public async Task<IActionResult> VendorDocuments(Guid vendorId, CancellationToken cancellationToken) => Ok(await store.ListVendorDocumentsAsync(Actor(), vendorId, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("notices")]
    public async Task<IActionResult> Notice(CreateNoticeRequest request, CancellationToken cancellationToken) => Ok(await store.CreateNoticeAsync(Actor(), request, cancellationToken));

    [Authorize]
    [HttpGet("notices")]
    public async Task<IActionResult> Notices(CancellationToken cancellationToken) => Ok(await store.GetNoticesAsync(Actor(), authorization.IsInRole(NestyStay.Domain.UserRole.Admin), cancellationToken));

    [Authorize]
    [HttpPost("notices/{noticeId:guid}/comments")]
    public async Task<IActionResult> NoticeComment(Guid noticeId, CommentNoticeRequest request, CancellationToken cancellationToken) => Ok(await store.CommentOnNoticeAsync(Actor(), authorization.IsInRole(NestyStay.Domain.UserRole.Admin), request with { NoticeId = noticeId }, cancellationToken));

    [Authorize(Roles = "Owner")]
    [HttpPost("notices/{noticeId:guid}/acknowledge")]
    public async Task<IActionResult> NoticeAcknowledge(Guid noticeId, CancellationToken cancellationToken) => Ok(await store.AcknowledgeNoticeAsync(Actor(), noticeId, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("governance/proposals")]
    public async Task<IActionResult> Proposal(CreateProposalRequest request, CancellationToken cancellationToken) => Ok(await store.CreateProposalAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "Owner,PropertyManager,Admin")]
    [HttpPost("governance/proposals/{proposalId:guid}/discussions")]
    public async Task<IActionResult> ProposalDiscussion(Guid proposalId, AddProposalDiscussionRequest request, CancellationToken cancellationToken) => Ok(await store.AddProposalDiscussionAsync(Actor(), authorization.IsInRole(NestyStay.Domain.UserRole.Admin), request with { ProposalId = proposalId }, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("governance/proposals/{proposalId:guid}/close")]
    public async Task<IActionResult> CloseProposal(Guid proposalId, CancellationToken cancellationToken) => (await store.CloseProposalAsync(Actor(), proposalId, cancellationToken)) is { } result ? Ok(result) : NotFound();

    [Authorize(Roles = "Owner,PropertyManager,Admin")]
    [HttpPost("governance/proposals/{proposalId:guid}/votes")]
    [EnableRateLimiting(RateLimitPolicies.SensitiveAction)]
    public async Task<IActionResult> Vote(Guid proposalId, VoteRequest request, CancellationToken cancellationToken) => Ok(await store.VoteAsync(Actor(), authorization.IsInRole(NestyStay.Domain.UserRole.Admin), proposalId, request, cancellationToken));

    [Authorize(Roles = "Owner")]
    [HttpPost("governance/proxies")]
    public async Task<IActionResult> Proxy(CreateProxyRequest request, CancellationToken cancellationToken) => Ok(await store.CreateProxyAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "Owner")]
    [HttpGet("governance/proxies")]
    public async Task<IActionResult> Proxies(CancellationToken cancellationToken) => Ok(await store.ListProxiesAsync(Actor(), cancellationToken));

    [Authorize(Roles = "Owner")]
    [HttpPost("governance/proxies/{proxyId:guid}/revoke")]
    public async Task<IActionResult> RevokeProxy(Guid proxyId, CancellationToken cancellationToken) => (await store.RevokeProxyAsync(Actor(), proxyId, cancellationToken)) is { } result ? Ok(result) : NotFound();

    [Authorize]
    [HttpGet("documents")]
    public async Task<IActionResult> Documents(CancellationToken cancellationToken) => Ok(await store.GetDocumentsAsync(Actor(), authorization.IsInRole(NestyStay.Domain.UserRole.Admin), cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("documents")]
    [EnableRateLimiting(RateLimitPolicies.Upload)]
    public async Task<IActionResult> Document(AddDocumentRequest request, CancellationToken cancellationToken) => Ok(await store.AddDocumentAsync(Actor(), request, cancellationToken));

    [Authorize]
    [HttpGet("documents/{documentId:guid}/download")]
    public async Task<IActionResult> DownloadDocument(Guid documentId, CancellationToken cancellationToken)
    {
        var result = await store.GetDocumentDownloadAsync(Actor(), authorization.IsInRole(NestyStay.Domain.UserRole.Admin), documentId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Roles = "PropertyManager")]
    [HttpPost("documents/exports")]
    public async Task<IActionResult> CreateDocumentExport(CreateDocumentExportRequest request, CancellationToken cancellationToken) => Ok(await store.CreateDocumentExportAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "PropertyManager")]
    [HttpGet("documents/exports/{exportId:guid}")]
    public async Task<IActionResult> GetDocumentExport(Guid exportId, CancellationToken cancellationToken) => (await store.GetDocumentExportAsync(Actor(), exportId, cancellationToken)) is { } result ? Ok(result) : NotFound();

    [Authorize(Roles = "PropertyManager")]
    [HttpGet("documents/exports/{exportId:guid}/download")]
    public async Task<IActionResult> DownloadDocumentExport(Guid exportId, CancellationToken cancellationToken)
    {
        var result = await store.OpenDocumentExportAsync(Actor(), exportId, cancellationToken);
        return result is null ? NotFound() : File(result.Content, result.ContentType, result.FileName, enableRangeProcessing: false);
    }

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("documents/{documentId:guid}/archive")]
    public async Task<IActionResult> ArchiveDocument(Guid documentId, [FromQuery] bool restore, CancellationToken cancellationToken) => (await store.ArchiveDocumentAsync(Actor(), documentId, restore, cancellationToken)) is { } result ? Ok(result) : NotFound();

    [Authorize]
    [HttpGet("documents/{documentId:guid}/versions")]
    public async Task<IActionResult> DocumentVersions(Guid documentId, CancellationToken cancellationToken) => Ok(await store.ListDocumentVersionsAsync(Actor(), authorization.IsInRole(NestyStay.Domain.UserRole.Admin), documentId, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("documents/versions")]
    [EnableRateLimiting(RateLimitPolicies.Upload)]
    public async Task<IActionResult> DocumentVersion(AddDocumentVersionRequest request, CancellationToken cancellationToken) => Ok(await store.AddDocumentVersionAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("gate/messages")]
    public async Task<IActionResult> GateMessage(CreateGateMessageRequest request, CancellationToken cancellationToken) => Ok(await store.CreateGateMessageAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpGet("gate/messages/{gateMessageId:guid}/delivery")]
    public async Task<IActionResult> GateDelivery(Guid gateMessageId, CancellationToken cancellationToken) => Ok(await store.ListGateDeliveryAttemptsAsync(Actor(), gateMessageId, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("gate/messages/{gateMessageId:guid}/delivery/retry")]
    public async Task<IActionResult> RetryGateDelivery(Guid gateMessageId, CancellationToken cancellationToken) => (await store.RetryGateDeliveryAsync(Actor(), gateMessageId, cancellationToken)) is { } result ? Ok(result) : NotFound();

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("qr")]
    public async Task<IActionResult> IssueQr(IssueQrRequest request, CancellationToken cancellationToken) => Ok(await store.IssueQrAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpGet("qr")]
    public async Task<IActionResult> ListQr(CancellationToken cancellationToken) => Ok(await store.ListQrAsync(Actor(), cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpGet("qr/{qrId:guid}/history")]
    public async Task<IActionResult> QrHistory(Guid qrId, CancellationToken cancellationToken) => Ok(await store.ListQrHistoryAsync(Actor(), qrId, cancellationToken));

    [AllowAnonymous]
    [HttpPost("qr/validate")]
    [EnableRateLimiting(RateLimitPolicies.SensitiveAction)]
    public async Task<IActionResult> ValidateQr(ValidateQrRequest request, CancellationToken cancellationToken) => Ok(await store.ValidateQrAsync(request.Token, request.PropertyId, authorization.TryGetSignedInUser(), cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("qr/{qrId:guid}/revoke")]
    public async Task<IActionResult> RevokeQr(Guid qrId, [FromBody] RevokeQrRequest? request, CancellationToken cancellationToken) => Ok(await store.RevokeQrAsync(Actor(), qrId, request?.Reason, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("subscription/change")]
    public async Task<IActionResult> ChangeSubscription(ChangeSubscriptionRequest request, CancellationToken cancellationToken) => Ok(await store.ChangeSubscriptionAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpGet("subscription/events")]
    public async Task<IActionResult> SubscriptionEvents(CancellationToken cancellationToken) => Ok(await store.ListSubscriptionEventsAsync(Actor(), cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("subscription/payment-retry")]
    public async Task<IActionResult> SubscriptionPaymentRetry(CancellationToken cancellationToken) => Ok(await store.RetrySubscriptionPaymentAsync(Actor(), cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpGet("owners/{ownerUserId:guid}/invitation-events")]
    public async Task<IActionResult> InvitationEvents(Guid ownerUserId, CancellationToken cancellationToken) => Ok(await store.ListInvitationEventsAsync(Actor(), ownerUserId, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpGet("owners/{ownerUserId:guid}/verification-requirements")]
    public async Task<IActionResult> VerificationRequirements(Guid ownerUserId, CancellationToken cancellationToken) => Ok(await store.ListOwnerVerificationAsync(Actor(), ownerUserId, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("owners/verification-requirements")]
    public async Task<IActionResult> DecideVerification(DecideOwnerVerificationRequest request, CancellationToken cancellationToken) => Ok(await store.DecideOwnerVerificationAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpGet("dashboard/preferences")]
    public async Task<IActionResult> DashboardPreference(CancellationToken cancellationToken) => Ok(await store.GetDashboardPreferenceAsync(Actor(), cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPut("dashboard/preferences")]
    public async Task<IActionResult> SaveDashboardPreference(SaveDashboardPreferenceRequest request, CancellationToken cancellationToken) => Ok(await store.SaveDashboardPreferenceAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("agreements")]
    public async Task<IActionResult> Agreement(SaveAgreementRequest request, CancellationToken cancellationToken) => Ok(await store.SaveAgreementAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("fee-rules")]
    public async Task<IActionResult> FeeRule(SaveFeeRuleRequest request, CancellationToken cancellationToken) => Ok(await store.SaveFeeRuleAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpGet("payouts")]
    public async Task<IActionResult> OwnerPayouts([FromQuery] Guid? ownerUserId, CancellationToken cancellationToken) => Ok(await store.ListOwnerPayoutsAsync(Actor(), ownerUserId, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("payouts")]
    public async Task<IActionResult> CreateOwnerPayout(CreateOwnerPayoutRequest request, CancellationToken cancellationToken) => Ok(await store.CreateOwnerPayoutAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("approvals")]
    public async Task<IActionResult> OwnerApproval(CreateOwnerApprovalRequest request, CancellationToken cancellationToken) => Ok(await store.CreateOwnerApprovalAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "Owner")]
    [HttpPost("approvals/{approvalId:guid}/decide")]
    public async Task<IActionResult> DecideOwnerApproval(Guid approvalId, DecideOwnerApprovalRequest request, CancellationToken cancellationToken) => (await store.DecideOwnerApprovalAsync(Actor(), approvalId, request, cancellationToken)) is { } result ? Ok(result) : NotFound();

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("staff")]
    public async Task<IActionResult> Staff(InviteStaffRequest request, CancellationToken cancellationToken) => Ok(await store.InviteStaffAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpGet("staff")]
    public async Task<IActionResult> StaffList(CancellationToken cancellationToken) => Ok(await store.ListStaffAsync(Actor(), cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("calendar/events")]
    public async Task<IActionResult> CalendarEvent(CreateCalendarEventRequest request, CancellationToken cancellationToken) => Ok(await store.CreateCalendarEventAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpGet("calendar/events")]
    public async Task<IActionResult> CalendarEvents([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, [FromQuery] Guid? propertyId, [FromQuery] Guid? ownerUserId, [FromQuery] string? eventType, CancellationToken cancellationToken) => Ok(await store.ListCalendarEventsAsync(Actor(), new CalendarEventQuery(from, to, propertyId, ownerUserId, eventType), cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("work-orders")]
    public async Task<IActionResult> WorkOrder(CreateWorkOrderRequest request, CancellationToken cancellationToken) => Ok(await store.CreateWorkOrderAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPatch("work-orders/{workOrderId:guid}")]
    public async Task<IActionResult> UpdateWorkOrder(Guid workOrderId, UpdateWorkOrderRequest request, CancellationToken cancellationToken) => (await store.UpdateWorkOrderAsync(Actor(), workOrderId, request, cancellationToken)) is { } result ? Ok(result) : NotFound();

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("cleaning")]
    public async Task<IActionResult> Cleaning(CreateCleaningTaskRequest request, CancellationToken cancellationToken) => Ok(await store.CreateCleaningTaskAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("inspections")]
    public async Task<IActionResult> Inspection(CreateInspectionRequest request, CancellationToken cancellationToken) => Ok(await store.CreateInspectionAsync(Actor(), request, cancellationToken));

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpPost("inspections/{inspectionId:guid}/complete")]
    public async Task<IActionResult> CompleteInspection(Guid inspectionId, CompleteInspectionRequest request, CancellationToken cancellationToken) => (await store.CompleteInspectionAsync(Actor(), inspectionId, request, cancellationToken)) is { } result ? Ok(result) : NotFound();

    [Authorize(Roles = "PropertyManager,Admin")]
    [HttpGet("reports")]
    public async Task<IActionResult> Report([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) => Ok(await store.GetPmsReportAsync(Actor(), from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)), to ?? DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken));

    [Authorize(Roles = "Owner")]
    [HttpGet("owner/portal")]
    public async Task<IActionResult> OwnerPortal(CancellationToken cancellationToken) => Ok(await store.GetOwnerPortalAsync(Actor(), cancellationToken));

    private Guid Actor() => authorization.RequireSignedInUser();
}

public sealed record ReviewOwnerRequest(string Status);
public sealed record ValidateQrRequest(string Token, Guid? PropertyId = null);
