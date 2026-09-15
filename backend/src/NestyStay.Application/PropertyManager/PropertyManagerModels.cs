namespace NestyStay.Application.PropertyManager;

public interface IPropertyManagerStore
{
    Task<PropertyManagerDashboardDto> GetDashboardAsync(Guid actorUserId, bool isAdmin, CancellationToken cancellationToken);
    Task<OwnerDto> InviteOwnerAsync(Guid managerUserId, InviteOwnerRequest request, CancellationToken cancellationToken);
    Task<OwnerDto?> ReviewOwnerAsync(Guid managerUserId, Guid ownerUserId, string status, CancellationToken cancellationToken);
    Task<ManagerProfileDto> RenewSubscriptionAsync(Guid managerUserId, CancellationToken cancellationToken);
    Task<PropertyDto> AddPropertyAsync(Guid managerUserId, AddPropertyRequest request, CancellationToken cancellationToken);
    Task<PropertyDto?> LinkRentalListingAsync(Guid managerUserId, Guid propertyId, LinkRentalListingRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<PropertyDto>> BulkAssignPropertiesAsync(Guid managerUserId, BulkAssignPropertiesRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<PropertyAssignmentHistoryDto>> GetPropertyAssignmentHistoryAsync(Guid managerUserId, Guid? propertyId, CancellationToken cancellationToken);
    Task<InvoiceDto> CreateInvoiceAsync(Guid managerUserId, CreateInvoiceRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<InvoiceDto>> BulkIssueInvoicesAsync(Guid managerUserId, BulkIssueInvoicesRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<InvoiceDto>> MarkOverdueInvoicesAsync(Guid managerUserId, CancellationToken cancellationToken);
    Task<InvoiceDto?> UpdateInvoiceAsync(Guid managerUserId, Guid invoiceId, UpdateInvoiceRequest request, CancellationToken cancellationToken);
    Task<InvoiceDto?> GetInvoiceAsync(Guid actorUserId, bool isAdmin, Guid invoiceId, CancellationToken cancellationToken);
    Task<InvoiceDto?> PayInvoiceAsync(Guid actorUserId, bool isAdmin, Guid invoiceId, PayInvoiceRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<PaymentOperationDto>> ListPaymentsAsync(Guid actorUserId, bool isAdmin, PaymentQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<PaymentMethodDto>> ListPaymentMethodsAsync(Guid actorUserId, bool isAdmin, Guid? ownerUserId, CancellationToken cancellationToken);
    Task<PaymentMethodDto> SavePaymentMethodAsync(Guid actorUserId, bool isAdmin, SavePaymentMethodRequest request, CancellationToken cancellationToken);
    Task<PaymentOperationDto?> RefundPaymentAsync(Guid actorUserId, bool isAdmin, Guid paymentId, RefundPaymentRequest request, CancellationToken cancellationToken);
    Task<PaymentOperationDto?> RetryPaymentAsync(Guid actorUserId, bool isAdmin, Guid paymentId, CancellationToken cancellationToken);
    Task<StatementDto> GetStatementAsync(Guid actorUserId, bool isAdmin, Guid ownerUserId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken);
    Task<UtilityChargeDto> CreateUtilityAsync(Guid managerUserId, CreateUtilityRequest request, CancellationToken cancellationToken);
    Task<MeterReadingDto> RecordMeterReadingAsync(Guid managerUserId, RecordMeterReadingRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<MeterReadingDto>> ListMeterReadingsAsync(Guid managerUserId, Guid propertyId, string? utilityType, CancellationToken cancellationToken);
    Task<UtilityScheduleDto> SaveUtilityScheduleAsync(Guid managerUserId, SaveUtilityScheduleRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<UtilityDisputeDto>> ListUtilityDisputesAsync(Guid managerUserId, CancellationToken cancellationToken);
    Task<UtilityDisputeDto> CreateUtilityDisputeAsync(Guid actorUserId, bool isAdmin, CreateUtilityDisputeRequest request, CancellationToken cancellationToken);
    Task<UtilityDisputeDto?> DecideUtilityDisputeAsync(Guid managerUserId, Guid disputeId, DecideUtilityDisputeRequest request, CancellationToken cancellationToken);
    Task<MaintenanceDto> CreateMaintenanceAsync(Guid actorUserId, bool isAdmin, CreateMaintenanceRequest request, CancellationToken cancellationToken);
    Task<MaintenanceDto?> UpdateMaintenanceAsync(Guid managerUserId, Guid maintenanceId, UpdateMaintenanceRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<MaintenanceActivityDto>> ListMaintenanceActivityAsync(Guid actorUserId, bool isAdmin, Guid maintenanceId, CancellationToken cancellationToken);
    Task<MaintenanceAttachmentDto> AddMaintenanceAttachmentAsync(Guid managerUserId, AddMaintenanceAttachmentRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<MaintenanceAttachmentDto>> ListMaintenanceAttachmentsAsync(Guid managerUserId, Guid maintenanceId, CancellationToken cancellationToken);
    Task<DocumentDownloadDto?> GetMaintenanceAttachmentDownloadAsync(Guid managerUserId, Guid maintenanceId, Guid attachmentId, CancellationToken cancellationToken);
    Task<VendorDto> CreateVendorAsync(Guid managerUserId, CreateVendorRequest request, CancellationToken cancellationToken);
    Task<VendorDto?> UpdateVendorAsync(Guid managerUserId, Guid vendorId, UpdateVendorRequest request, CancellationToken cancellationToken);
    Task<VendorDocumentDto> AddVendorDocumentAsync(Guid managerUserId, AddVendorDocumentRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<VendorDocumentDto>> ListVendorDocumentsAsync(Guid managerUserId, Guid vendorId, CancellationToken cancellationToken);
    Task<NoticeDto> CreateNoticeAsync(Guid managerUserId, CreateNoticeRequest request, CancellationToken cancellationToken);
    Task<NoticeInteractionDto> CommentOnNoticeAsync(Guid actorUserId, bool isAdmin, CommentNoticeRequest request, CancellationToken cancellationToken);
    Task<NoticeInteractionDto> AcknowledgeNoticeAsync(Guid ownerUserId, Guid noticeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<NoticeDto>> GetNoticesAsync(Guid actorUserId, bool isAdmin, CancellationToken cancellationToken);
    Task<ProposalDto> CreateProposalAsync(Guid managerUserId, CreateProposalRequest request, CancellationToken cancellationToken);
    Task<ProposalDiscussionDto> AddProposalDiscussionAsync(Guid actorUserId, bool isAdmin, AddProposalDiscussionRequest request, CancellationToken cancellationToken);
    Task<ProposalDto?> CloseProposalAsync(Guid managerUserId, Guid proposalId, CancellationToken cancellationToken);
    Task<ProposalDto> VoteAsync(Guid actorUserId, bool isAdmin, Guid proposalId, VoteRequest request, CancellationToken cancellationToken);
    Task<ProxyDto> CreateProxyAsync(Guid ownerUserId, CreateProxyRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProxyDto>> ListProxiesAsync(Guid ownerUserId, CancellationToken cancellationToken);
    Task<ProxyDto?> RevokeProxyAsync(Guid ownerUserId, Guid proxyId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DocumentDto>> GetDocumentsAsync(Guid actorUserId, bool isAdmin, CancellationToken cancellationToken);
    Task<DocumentDto> AddDocumentAsync(Guid managerUserId, AddDocumentRequest request, CancellationToken cancellationToken);
    Task<DocumentDownloadDto?> GetDocumentDownloadAsync(Guid actorUserId, bool isAdmin, Guid documentId, CancellationToken cancellationToken);
    Task<DocumentExportDto> CreateDocumentExportAsync(Guid managerUserId, CreateDocumentExportRequest request, CancellationToken cancellationToken);
    Task<DocumentExportDto?> GetDocumentExportAsync(Guid managerUserId, Guid exportId, CancellationToken cancellationToken);
    Task<DocumentExportFile?> OpenDocumentExportAsync(Guid managerUserId, Guid exportId, CancellationToken cancellationToken);
    Task<DocumentDto?> ArchiveDocumentAsync(Guid managerUserId, Guid documentId, bool restore, CancellationToken cancellationToken);
    Task<IReadOnlyList<DocumentVersionDto>> ListDocumentVersionsAsync(Guid actorUserId, bool isAdmin, Guid documentId, CancellationToken cancellationToken);
    Task<DocumentVersionDto> AddDocumentVersionAsync(Guid managerUserId, AddDocumentVersionRequest request, CancellationToken cancellationToken);
    Task<GateMessageDto> CreateGateMessageAsync(Guid managerUserId, CreateGateMessageRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<GateDeliveryAttemptDto>> ListGateDeliveryAttemptsAsync(Guid managerUserId, Guid gateMessageId, CancellationToken cancellationToken);
    Task<GateDeliveryAttemptDto?> RetryGateDeliveryAsync(Guid managerUserId, Guid gateMessageId, CancellationToken cancellationToken);
    Task<QrIssueDto> IssueQrAsync(Guid managerUserId, IssueQrRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<QrAccessRecordDto>> ListQrAsync(Guid managerUserId, CancellationToken cancellationToken);
    Task<IReadOnlyList<QrScanDto>> ListQrHistoryAsync(Guid managerUserId, Guid qrId, CancellationToken cancellationToken);
    Task<QrValidationDto> ValidateQrAsync(string token, Guid? propertyId, Guid? gateGuardUserId, CancellationToken cancellationToken);
    Task<QrValidationDto> RevokeQrAsync(Guid managerUserId, Guid qrId, string? reason, CancellationToken cancellationToken);
    Task<ManagerProfileDto> ChangeSubscriptionAsync(Guid managerUserId, ChangeSubscriptionRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<SubscriptionEventDto>> ListSubscriptionEventsAsync(Guid managerUserId, CancellationToken cancellationToken);
    Task<SubscriptionEventDto> RetrySubscriptionPaymentAsync(Guid managerUserId, CancellationToken cancellationToken);
    Task<int> ApplyDueSubscriptionChangesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<InvitationEventDto>> ListInvitationEventsAsync(Guid managerUserId, Guid ownerUserId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OwnerVerificationDto>> ListOwnerVerificationAsync(Guid managerUserId, Guid ownerUserId, CancellationToken cancellationToken);
    Task<OwnerVerificationDto> DecideOwnerVerificationAsync(Guid managerUserId, DecideOwnerVerificationRequest request, CancellationToken cancellationToken);
    Task<DashboardPreferenceDto> GetDashboardPreferenceAsync(Guid managerUserId, CancellationToken cancellationToken);
    Task<DashboardPreferenceDto> SaveDashboardPreferenceAsync(Guid managerUserId, SaveDashboardPreferenceRequest request, CancellationToken cancellationToken);
    Task<AgreementDto> SaveAgreementAsync(Guid managerUserId, SaveAgreementRequest request, CancellationToken cancellationToken);
    Task<FeeRuleDto> SaveFeeRuleAsync(Guid managerUserId, SaveFeeRuleRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<OwnerPayoutDto>> ListOwnerPayoutsAsync(Guid managerUserId, Guid? ownerUserId, CancellationToken cancellationToken);
    Task<OwnerPayoutDto> CreateOwnerPayoutAsync(Guid managerUserId, CreateOwnerPayoutRequest request, CancellationToken cancellationToken);
    Task<OwnerApprovalDto> CreateOwnerApprovalAsync(Guid managerUserId, CreateOwnerApprovalRequest request, CancellationToken cancellationToken);
    Task<OwnerApprovalDto?> DecideOwnerApprovalAsync(Guid managerUserId, Guid approvalId, DecideOwnerApprovalRequest request, CancellationToken cancellationToken);
    Task<StaffDto> InviteStaffAsync(Guid managerUserId, InviteStaffRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<StaffDto>> ListStaffAsync(Guid managerUserId, CancellationToken cancellationToken);
    Task<CalendarEventDto> CreateCalendarEventAsync(Guid managerUserId, CreateCalendarEventRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<CalendarEventDto>> ListCalendarEventsAsync(Guid managerUserId, CalendarEventQuery query, CancellationToken cancellationToken);
    Task<WorkOrderDto> CreateWorkOrderAsync(Guid managerUserId, CreateWorkOrderRequest request, CancellationToken cancellationToken);
    Task<WorkOrderDto?> UpdateWorkOrderAsync(Guid managerUserId, Guid workOrderId, UpdateWorkOrderRequest request, CancellationToken cancellationToken);
    Task<CleaningTaskDto> CreateCleaningTaskAsync(Guid managerUserId, CreateCleaningTaskRequest request, CancellationToken cancellationToken);
    Task<InspectionDto> CreateInspectionAsync(Guid managerUserId, CreateInspectionRequest request, CancellationToken cancellationToken);
    Task<InspectionDto?> CompleteInspectionAsync(Guid managerUserId, Guid inspectionId, CompleteInspectionRequest request, CancellationToken cancellationToken);
    Task<PmsReportDto> GetPmsReportAsync(Guid managerUserId, DateOnly from, DateOnly to, CancellationToken cancellationToken);
    Task<OwnerPortalDto> GetOwnerPortalAsync(Guid ownerUserId, CancellationToken cancellationToken);
}

public sealed record InviteOwnerRequest(string Email, string DisplayName, Guid? OwnerUserId = null, Guid? CommunityId = null);
public sealed record AddPropertyRequest(Guid OwnerUserId, string Title, string UnitNumber, string Address, Guid? CommunityId = null, Guid? RentalListingId = null);
public sealed record LinkRentalListingRequest(Guid? RentalListingId);
public sealed record BulkAssignPropertiesRequest(IReadOnlyList<Guid> PropertyIds, Guid OwnerUserId, string Reason, Guid? BatchId = null);
public sealed record CreateInvoiceLineRequest(string Description, decimal Quantity, decimal UnitAmount);
public sealed record CreateInvoiceRequest(Guid OwnerUserId, Guid? PropertyId, DateOnly DueDate, decimal Tax, IReadOnlyList<CreateInvoiceLineRequest> Lines);
public sealed record BulkIssueInvoicesRequest(IReadOnlyList<Guid> InvoiceIds);
public sealed record UpdateInvoiceRequest(DateOnly DueDate, decimal Tax, IReadOnlyList<CreateInvoiceLineRequest> Lines);
public sealed record PayInvoiceRequest(decimal Amount, string IdempotencyKey);
public sealed record PaymentQuery(Guid? OwnerUserId = null, string? Status = null, DateOnly? From = null, DateOnly? To = null);
public sealed record SavePaymentMethodRequest(Guid OwnerUserId, string Provider, string ProviderReference, string Brand, string Last4, int ExpMonth, int ExpYear, bool IsDefault = false);
public sealed record RefundPaymentRequest(decimal? Amount, string Reason, string IdempotencyKey);
public sealed record CreateUtilityRequest(Guid OwnerUserId, Guid PropertyId, string UtilityType, string BillingPeriod, decimal Usage, decimal Rate, string Currency = "JMD");
public sealed record RecordMeterReadingRequest(Guid OwnerUserId, Guid PropertyId, string UtilityType, string BillingPeriod, decimal PreviousReading, decimal CurrentReading, decimal? Rate = null, string? PhotoBase64 = null, string? BillBase64 = null, string Currency = "JMD");
public sealed record SaveUtilityScheduleRequest(Guid OwnerUserId, Guid PropertyId, string UtilityType, decimal Rate, int DayOfMonth);
public sealed record CreateUtilityDisputeRequest(Guid UtilityChargeId, string Reason, string? EvidenceBase64 = null);
public sealed record DecideUtilityDisputeRequest(string Status, string Decision, decimal AdjustmentAmount);
public sealed record CreateMaintenanceRequest(Guid OwnerUserId, Guid PropertyId, string Title, string Description, string Category, string Urgency);
public sealed record UpdateMaintenanceRequest(string Status, Guid? VendorId, DateTimeOffset? ScheduledAt, decimal Cost, string Notes);
public sealed record AddMaintenanceAttachmentRequest(Guid MaintenanceId, string FileName, string ContentType, string ContentBase64);
public sealed record CreateVendorRequest(string Name, string Category, string Contact, string Notes, IReadOnlyList<string>? ServiceAreas = null, string? AvailabilityJson = null, decimal? Rate = null);
public sealed record UpdateVendorRequest(string? Contact, string? Notes, IReadOnlyList<string>? ServiceAreas, string? AvailabilityJson, decimal? Rate, decimal? Rating, bool? IsPreferred, bool? IsSuspended, bool? IsActive);
public sealed record AddVendorDocumentRequest(Guid VendorId, string DocumentType, string FileName, string ContentType, string ContentBase64, DateOnly? ExpiresOn = null);
public sealed record CreateNoticeRequest(
    Guid? CommunityId,
    Guid? TargetOwnerUserId,
    string Title,
    string Body,
    DateTimeOffset? ExpiresAt,
    bool IsPinned,
    DateTimeOffset? PublishAt = null,
    string? Category = null,
    IReadOnlyList<string>? AudienceRoles = null,
    IReadOnlyList<Guid>? AudienceOwnerIds = null,
    DateTimeOffset? AcknowledgementDueAt = null);
public sealed record CommentNoticeRequest(Guid NoticeId, string Body);
public sealed record CreateProposalRequest(Guid? CommunityId, string Title, string Description, DateTimeOffset OpensAt, DateTimeOffset ClosesAt, bool IsAnonymous, int? Quorum);
public sealed record AddProposalDiscussionRequest(Guid ProposalId, string Body);
public sealed record VoteRequest(string Choice, Guid? ProxyId = null);
public sealed record CreateProxyRequest(Guid ProposalId, Guid ProxyUserId, DateTimeOffset ValidUntil);
public sealed record AddDocumentRequest(Guid? OwnerUserId, Guid? PropertyId, string Title, string Category, string FileName, string ContentType, long SizeBytes, string? ContentBase64 = null, DateOnly? ExpiresOn = null);
public sealed record CreateDocumentExportRequest(IReadOnlyList<Guid> DocumentIds);
public sealed record AddDocumentVersionRequest(Guid DocumentId, string FileName, string ContentType, long SizeBytes, string ContentBase64);
public sealed record CreateGateMessageRequest(Guid? CommunityId, Guid? PropertyId, string Recipient, string Message, string VisitorType, DateTimeOffset ValidFrom, DateTimeOffset ValidUntil);
public sealed record IssueQrRequest(Guid? OwnerUserId, Guid? PropertyId, string SubjectType, DateTimeOffset ValidFrom, DateTimeOffset ValidUntil);
public sealed record RevokeQrRequest(string? Reason = null);
public sealed record ChangeSubscriptionRequest(string Action, string? TargetTier = null, string? Reason = null, bool? AutoRenew = null);
public sealed record DecideOwnerVerificationRequest(Guid OwnerUserId, string Requirement, string Status, string? Reason = null, string? DocumentKey = null);
public sealed record SaveDashboardPreferenceRequest(IReadOnlyList<string> KpiOrder, IReadOnlyList<string> VisibleKpis, string SavedFiltersJson, IReadOnlyList<string> SavedViews);
public sealed record SaveAgreementRequest(Guid OwnerUserId, Guid? PropertyId, DateOnly EffectiveFrom, DateOnly? EffectiveTo, string FeeRuleJson, decimal MaintenanceApprovalLimit, decimal ExpenseApprovalLimit, string? SignedDocumentKey = null);
public sealed record SaveFeeRuleRequest(Guid? PropertyId, string RuleType, decimal Percentage, decimal FixedAmount, decimal CleaningMarkup, decimal MaintenanceMarkup, DateOnly EffectiveFrom, DateOnly? EffectiveTo = null);
public sealed record CreateOwnerPayoutRequest(Guid OwnerUserId, DateOnly PeriodFrom, DateOnly PeriodTo, decimal Amount);
public sealed record CreateOwnerApprovalRequest(Guid OwnerUserId, Guid? PropertyId, string ApprovalType, string Description, decimal Amount, decimal Limit);
public sealed record DecideOwnerApprovalRequest(string Status, string? Reason = null);
public sealed record InviteStaffRequest(Guid StaffUserId, string Role, IReadOnlyList<Guid>? PropertyIds = null, IReadOnlyList<Guid>? OwnerIds = null, bool CanManageFinance = false, decimal ApprovalLimit = 0);
public sealed record CreateCalendarEventRequest(Guid? PropertyId, Guid? OwnerUserId, string EventType, string Title, DateTimeOffset StartsAt, DateTimeOffset EndsAt, string Status = "CONFIRMED");
public sealed record CalendarEventQuery(DateTimeOffset? From = null, DateTimeOffset? To = null, Guid? PropertyId = null, Guid? OwnerUserId = null, string? EventType = null);
public sealed record CreateWorkOrderRequest(Guid PropertyId, Guid OwnerUserId, string Scope, Guid? VendorId = null, decimal? QuoteAmount = null, DateTimeOffset? SlaDueAt = null);
public sealed record UpdateWorkOrderRequest(string Status, Guid? VendorId = null, decimal? ApprovedAmount = null, decimal LaborAmount = 0, decimal PartsAmount = 0, DateTimeOffset? ScheduledAt = null);
public sealed record CreateCleaningTaskRequest(Guid PropertyId, DateTimeOffset DueAt, Guid? AssignedStaffUserId = null, string? Notes = null);
public sealed record CreateInspectionRequest(Guid PropertyId, string ChecklistJson);
public sealed record CompleteInspectionRequest(string Status, string? IssuesJson = null);

public sealed record PropertyManagerDashboardDto(
    ManagerProfileDto Manager,
    int TotalOwners,
    int TotalProperties,
    decimal OutstandingBalance,
    int InvoicesDue,
    int OpenMaintenance,
    int PendingVerification,
    int GateActivity,
    IReadOnlyList<OwnerDto> Owners,
    IReadOnlyList<PropertyDto> Properties,
    IReadOnlyList<InvoiceDto> Invoices,
    IReadOnlyList<MaintenanceDto> Maintenance,
    IReadOnlyList<UtilityChargeDto> Utilities,
    IReadOnlyList<VendorDto> Vendors,
    IReadOnlyList<NoticeDto> Notices,
    IReadOnlyList<ProposalDto> Proposals,
    IReadOnlyList<DocumentDto> Documents,
    IReadOnlyList<GateMessageDto> GateMessages);
public sealed record ManagerProfileDto(
    Guid ManagerUserId,
    string BusinessName,
    string SubscriptionTier,
    decimal MonthlyAmount,
    string SubscriptionStatus,
    DateTimeOffset NextBillingAt,
    string? PendingSubscriptionTier = null,
    DateTimeOffset? PendingSubscriptionEffectiveAt = null,
    bool AutoRenew = true,
    string BillingProviderStatus = "LOCAL_TEST",
    int? UnitLimit = null,
    int UnitsUsed = 0,
    string? CancellationReason = null);
public sealed record OwnerDto(Guid Id, Guid OwnerUserId, string DisplayName, string Email, string VerificationStatus, string InvitationStatus, Guid? CommunityId);
public sealed record PropertyDto(Guid Id, Guid OwnerUserId, Guid? CommunityId, string Title, string UnitNumber, string Address, string Status, string OccupancyStatus, Guid? RentalListingId = null);
public sealed record PropertyAssignmentHistoryDto(Guid Id, Guid PropertyId, Guid? PreviousOwnerUserId, Guid NewOwnerUserId, Guid ActorUserId, string Reason, Guid BatchId, DateTimeOffset ChangedAt);
public sealed record InvoiceLineDto(Guid Id, string Description, decimal Quantity, decimal UnitAmount, decimal Amount);
public sealed record InvoiceDto(Guid Id, Guid OwnerUserId, Guid? PropertyId, string InvoiceNumber, DateOnly IssueDate, DateOnly DueDate, decimal Subtotal, decimal Tax, decimal Total, decimal AmountPaid, decimal Balance, string Currency, string Status, IReadOnlyList<InvoiceLineDto> Lines);
public sealed record PaymentDto(Guid Id, Guid InvoiceId, decimal Amount, string Provider, string ProviderReference, string Status, DateTimeOffset CreatedAt);
public sealed record PaymentOperationDto(Guid Id, Guid InvoiceId, Guid OwnerUserId, decimal Amount, decimal RefundedAmount, string Provider, string ProviderReference, string Status, string ReconciliationStatus, string? ReconciliationReference, string? RefundReason, DateTimeOffset CreatedAt);
public sealed record PaymentMethodDto(Guid Id, Guid OwnerUserId, string Provider, string Brand, string Last4, int ExpMonth, int ExpYear, bool IsDefault);
public sealed record StatementEntryDto(DateOnly Date, string Type, string Description, decimal Amount, Guid? InvoiceId);
public sealed record StatementDto(Guid OwnerUserId, DateOnly From, DateOnly To, decimal OpeningBalance, IReadOnlyList<StatementEntryDto> Entries, decimal ClosingBalance, IReadOnlyList<InvoiceDto> Invoices, IReadOnlyList<PaymentDto> Payments);
public sealed record UtilityChargeDto(Guid Id, Guid OwnerUserId, Guid PropertyId, string UtilityType, string BillingPeriod, decimal Usage, decimal Rate, decimal Amount, string Currency, Guid? InvoiceId, string Status);
public sealed record MeterReadingDto(Guid Id, Guid OwnerUserId, Guid PropertyId, string UtilityType, string BillingPeriod, decimal PreviousReading, decimal CurrentReading, decimal Usage, bool IsAnomaly, string Status, DateTimeOffset CreatedAt);
public sealed record UtilityScheduleDto(Guid Id, Guid OwnerUserId, Guid PropertyId, string UtilityType, decimal Rate, int DayOfMonth, bool IsActive, DateTimeOffset? LastRunAt);
public sealed record UtilityDisputeDto(Guid Id, Guid UtilityChargeId, Guid OwnerUserId, string Reason, string Status, string? Decision, decimal AdjustmentAmount, DateTimeOffset CreatedAt);
public sealed record MaintenanceDto(Guid Id, Guid OwnerUserId, Guid PropertyId, Guid? VendorId, string Title, string Description, string Category, string Urgency, string Status, DateTimeOffset? ScheduledAt, decimal Cost, string Notes);
public sealed record MaintenanceActivityDto(Guid Id, Guid MaintenanceId, Guid ActorUserId, string Action, string Details, DateTimeOffset CreatedAt);
public sealed record MaintenanceAttachmentDto(Guid Id, Guid MaintenanceId, string FileName, string ContentType, string Status, DateTimeOffset CreatedAt);
public sealed record MaintenanceAttachmentDownloadDto(Guid Id, string FileName, string ContentType, string Url, DateTimeOffset ExpiresAt);
public sealed record VendorDto(Guid Id, string Name, string Category, string Contact, string VerificationStatus, bool IsActive, string Notes, IReadOnlyList<string>? ServiceAreas = null, decimal? Rate = null, decimal Rating = 0, bool IsPreferred = false, bool IsSuspended = false, int CompletedJobCount = 0, decimal SpendTotal = 0);
public sealed record VendorDocumentDto(Guid Id, Guid VendorId, string DocumentType, string FileName, DateOnly? ExpiresOn, string Status, DateTimeOffset CreatedAt);
public sealed record NoticeDto(
    Guid Id,
    Guid? CommunityId,
    Guid? TargetOwnerUserId,
    string Title,
    string Body,
    DateTimeOffset PublishAt,
    DateTimeOffset? ExpiresAt,
    bool IsPinned,
    bool IsArchived,
    string Category = "GENERAL",
    IReadOnlyList<string>? AudienceRoles = null,
    IReadOnlyList<Guid>? AudienceOwnerIds = null,
    DateTimeOffset? AcknowledgementDueAt = null);
public sealed record NoticeInteractionDto(Guid Id, Guid SubjectId, Guid ActorUserId, string Type, string Body, DateTimeOffset CreatedAt);
public sealed record ProposalDto(Guid Id, Guid? CommunityId, string Title, string Description, DateTimeOffset OpensAt, DateTimeOffset ClosesAt, string Status, bool IsAnonymous, int? Quorum, int EligibleVoters, int VotesCast, IReadOnlyDictionary<string, int> Results);
public sealed record ProposalDiscussionDto(Guid Id, Guid ProposalId, Guid AuthorUserId, string Body, DateTimeOffset CreatedAt);
public sealed record ProxyDto(Guid Id, Guid ProposalId, Guid OwnerUserId, Guid ProxyUserId, string Status, DateTimeOffset ValidUntil, DateTimeOffset? AcceptedAt);
public sealed record DocumentDto(Guid Id, Guid? OwnerUserId, Guid? PropertyId, string Title, string Category, string FileName, string ContentType, long SizeBytes, string AccessScope, bool IsArchived, DateOnly? ExpiresOn, DateTimeOffset CreatedAt);
public sealed record DocumentVersionDto(Guid Id, Guid DocumentId, int Version, string FileName, string ContentType, long SizeBytes, Guid CreatedByUserId, DateTimeOffset CreatedAt);
public sealed record DocumentDownloadDto(Guid Id, string FileName, string ContentType, long SizeBytes, string Url, DateTimeOffset ExpiresAt);
public sealed record DocumentExportFile(Stream Content, string FileName, string ContentType, DateTimeOffset ExpiresAt);
public sealed record DocumentExportDto(Guid Id, string Status, int DocumentCount, string? FileName, string? Url, string? Error, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt, DateTimeOffset? ExpiresAt);
public sealed record GateMessageDto(Guid Id, Guid? CommunityId, Guid? PropertyId, string Recipient, string Message, string VisitorType, DateTimeOffset ValidFrom, DateTimeOffset ValidUntil);
public sealed record GateDeliveryAttemptDto(Guid Id, Guid GateMessageId, string Recipient, string Status, string? ProviderReference, int AttemptNumber, string? FailureReason, DateTimeOffset CreatedAt);
public sealed record QrIssueDto(Guid Id, string Token, string SubjectType, Guid? PropertyId, DateTimeOffset ValidFrom, DateTimeOffset ValidUntil);
public sealed record QrAccessRecordDto(Guid Id, string SubjectType, Guid? PropertyId, DateTimeOffset ValidFrom, DateTimeOffset ValidUntil, bool IsRevoked, int ValidationCount, DateTimeOffset? LastValidatedAt, string Status);
public sealed record QrScanDto(Guid Id, Guid QrAccessId, Guid? GateGuardUserId, Guid? PropertyId, string Result, DateTimeOffset ScannedAt);
public sealed record QrValidationDto(string Result, string Status, Guid? PropertyId, string SubjectType, DateTimeOffset? ValidUntil, Guid? QrId, string? Message);
public sealed record SubscriptionEventDto(Guid Id, string EventType, string FromTier, string ToTier, string Status, string? Reason, DateTimeOffset EffectiveAt);
public sealed record InvitationEventDto(Guid Id, Guid OwnerUserId, string EventType, string? ProviderReference, DateTimeOffset CreatedAt);
public sealed record OwnerVerificationDto(Guid Id, Guid OwnerUserId, string Requirement, string Status, string? Reason, string? DocumentKey, DateTimeOffset CreatedAt);
public sealed record DashboardPreferenceDto(Guid ManagerUserId, IReadOnlyList<string> KpiOrder, IReadOnlyList<string> VisibleKpis, string SavedFiltersJson, IReadOnlyList<string> SavedViews);
public sealed record AgreementDto(Guid Id, Guid OwnerUserId, Guid? PropertyId, DateOnly EffectiveFrom, DateOnly? EffectiveTo, string FeeRuleJson, decimal MaintenanceApprovalLimit, decimal ExpenseApprovalLimit, string? SignedDocumentKey, string Status, int Version);
public sealed record FeeRuleDto(Guid Id, Guid? PropertyId, string RuleType, decimal Percentage, decimal FixedAmount, decimal CleaningMarkup, decimal MaintenanceMarkup, DateOnly EffectiveFrom, DateOnly? EffectiveTo);
public sealed record OwnerPayoutDto(Guid Id, Guid OwnerUserId, DateOnly PeriodFrom, DateOnly PeriodTo, decimal Amount, string Status, string? ProviderReference, string? FailureReason);
public sealed record OwnerApprovalDto(Guid Id, Guid OwnerUserId, Guid? PropertyId, string ApprovalType, string Description, decimal Amount, decimal Limit, string Status, string? DecisionReason);
public sealed record StaffDto(Guid Id, Guid StaffUserId, string Role, IReadOnlyList<Guid> PropertyIds, IReadOnlyList<Guid> OwnerIds, bool CanManageFinance, decimal ApprovalLimit, string Status);
public sealed record CalendarEventDto(Guid Id, Guid? PropertyId, Guid? OwnerUserId, string EventType, string Title, DateTimeOffset StartsAt, DateTimeOffset EndsAt, string Status, string SourceType);
public sealed record WorkOrderDto(Guid Id, Guid PropertyId, Guid OwnerUserId, Guid? VendorId, string WorkOrderNumber, string Scope, string Status, decimal? QuoteAmount, decimal? ApprovedAmount, decimal LaborAmount, decimal PartsAmount, DateTimeOffset? SlaDueAt, DateTimeOffset? ScheduledAt);
public sealed record CleaningTaskDto(Guid Id, Guid PropertyId, string Status, DateTimeOffset DueAt, Guid? AssignedStaffUserId, string Notes);
public sealed record InspectionDto(Guid Id, Guid PropertyId, string Status, string ChecklistJson, string? IssuesJson, DateTimeOffset? CompletedAt);
public sealed record PmsReportDto(DateOnly From, DateOnly To, int PropertiesManaged, int Owners, int OpenMaintenance, int OpenWorkOrders, decimal GrossInvoiceRevenue, decimal PaymentRevenue, decimal MaintenanceSpend, decimal UtilityRevenue, decimal OutstandingBalance, decimal PmFeeRevenue, IReadOnlyList<Guid> InvoiceIds, IReadOnlyList<Guid> MaintenanceIds);
public sealed record OwnerPortalDto(Guid OwnerUserId, IReadOnlyList<PropertyDto> Properties, IReadOnlyList<InvoiceDto> Invoices, StatementDto Statement, IReadOnlyList<UtilityChargeDto> Utilities, IReadOnlyList<MaintenanceDto> Maintenance, IReadOnlyList<NoticeDto> Notices, IReadOnlyList<ProposalDto> Proposals, IReadOnlyList<DocumentDto> Documents);
