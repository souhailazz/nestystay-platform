namespace NestyStay.Application.PropertyManager;

public sealed record P0Actor(Guid UserId, bool IsAdmin, bool IsPropertyManager);

public interface IPropertyManagerP0Store
{
    Task<P0OwnerProfileDto> UpsertOwnerProfileAsync(P0Actor actor, Guid ownerUserId, UpsertP0OwnerProfileRequest request, CancellationToken cancellationToken);
    Task<P0OwnerProfileDto?> GetOwnerProfileAsync(P0Actor actor, Guid ownerUserId, CancellationToken cancellationToken);
    Task<P0OwnerProfileDto?> ChangeOwnerStatusAsync(P0Actor actor, Guid ownerUserId, P0StatusChangeRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<P0OwnerLifecycleEventDto>> ListOwnerLifecycleAsync(P0Actor actor, Guid ownerUserId, CancellationToken cancellationToken);
    Task<P0PortfolioDto> GetPortfolioAsync(P0Actor actor, P0PortfolioQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<PropertyAssignmentHistoryDto>> GetAssignmentHistoryAsync(P0Actor actor, Guid? propertyId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PropertyDto>> AssignPropertiesAsync(P0Actor actor, P0AssignPropertiesRequest request, CancellationToken cancellationToken);

    Task<P0AgreementDto> CreateAgreementAsync(P0Actor actor, P0CreateAgreementRequest request, CancellationToken cancellationToken);
    Task<P0AgreementDto?> GetAgreementAsync(P0Actor actor, Guid agreementId, CancellationToken cancellationToken);
    Task<IReadOnlyList<P0AgreementDto>> ListAgreementsAsync(P0Actor actor, P0AgreementQuery query, CancellationToken cancellationToken);
    Task<P0AgreementDto?> UpdateAgreementDraftAsync(P0Actor actor, Guid agreementId, P0UpdateAgreementRequest request, CancellationToken cancellationToken);
    Task<P0AgreementDto?> ActivateAgreementAsync(P0Actor actor, Guid agreementId, CancellationToken cancellationToken);
    Task<P0AgreementDto?> RenewAgreementAsync(P0Actor actor, Guid agreementId, P0RenewAgreementRequest request, CancellationToken cancellationToken);
    Task<P0AgreementDto?> TerminateAgreementAsync(P0Actor actor, Guid agreementId, P0TerminateAgreementRequest request, CancellationToken cancellationToken);

    Task<P0FeeRuleDto> CreateFeeRuleAsync(P0Actor actor, P0CreateFeeRuleRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<P0FeeRuleDto>> ListFeeRulesAsync(P0Actor actor, P0FeeRuleQuery query, CancellationToken cancellationToken);
    Task<P0FeeCalculationDto> CalculateFeeAsync(P0Actor actor, P0CalculateFeeRequest request, CancellationToken cancellationToken);
    Task<P0FeeCalculationDto> PostFeeAsync(P0Actor actor, P0PostFeeRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<P0AccountDto>> ListAccountsAsync(P0Actor actor, string? currency, CancellationToken cancellationToken);
    Task<P0JournalDto> PostJournalAsync(P0Actor actor, P0PostJournalRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<P0JournalDto>> ListJournalsAsync(P0Actor actor, P0JournalQuery query, CancellationToken cancellationToken);
    Task<P0JournalDto?> ReverseJournalAsync(P0Actor actor, Guid journalId, P0ReverseJournalRequest request, CancellationToken cancellationToken);
    Task<P0ReconciliationDto> ReconcileJournalAsync(P0Actor actor, P0ReconcileJournalRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<P0ReconciliationDto>> ListReconciliationsAsync(P0Actor actor, P0JournalQuery query, CancellationToken cancellationToken);

    Task<P0StatementDto> BuildStatementAsync(P0Actor actor, P0StatementQuery query, CancellationToken cancellationToken);
    Task<P0StatementDto> FinalizeStatementAsync(P0Actor actor, P0FinalizeStatementRequest request, CancellationToken cancellationToken);
    Task<P0StatementExportDto> ExportStatementAsync(P0Actor actor, Guid snapshotId, string format, CancellationToken cancellationToken);
    Task<P0ProfitabilityDto> GetProfitabilityAsync(P0Actor actor, P0ProfitabilityQuery query, CancellationToken cancellationToken);

    Task<P0ApprovalDto> CreateApprovalAsync(P0Actor actor, P0CreateApprovalRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<P0ApprovalDto>> ListApprovalsAsync(P0Actor actor, P0ApprovalQuery query, CancellationToken cancellationToken);
    Task<P0ApprovalDto?> DecideApprovalAsync(P0Actor actor, Guid approvalId, P0ApprovalDecisionRequest request, CancellationToken cancellationToken);

    Task<P0StaffMembershipDto> InviteStaffAsync(P0Actor actor, P0InviteStaffRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<P0StaffMembershipDto>> ListStaffAsync(P0Actor actor, CancellationToken cancellationToken);
    Task<IReadOnlyList<P0StaffEventDto>> ListStaffHistoryAsync(P0Actor actor, Guid membershipId, CancellationToken cancellationToken);
    Task<P0StaffMembershipDto?> AcceptStaffAsync(P0Actor actor, Guid membershipId, CancellationToken cancellationToken);
    Task<P0StaffMembershipDto?> UpdateStaffAsync(P0Actor actor, Guid membershipId, P0UpdateStaffRequest request, CancellationToken cancellationToken);
    Task<P0StaffMembershipDto?> RevokeStaffAsync(P0Actor actor, Guid membershipId, P0StatusChangeRequest request, CancellationToken cancellationToken);

    Task<P0PayoutAvailabilityDto> GetPayoutAvailabilityAsync(P0Actor actor, Guid ownerUserId, string currency, DateOnly from, DateOnly to, CancellationToken cancellationToken);
    Task<P0PayoutBatchDto> CreatePayoutBatchAsync(P0Actor actor, P0CreatePayoutBatchRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<P0PayoutBatchDto>> ListPayoutBatchesAsync(P0Actor actor, P0PayoutQuery query, CancellationToken cancellationToken);
    Task<P0PayoutBatchDto?> ApprovePayoutBatchAsync(P0Actor actor, Guid batchId, P0PayoutDecisionRequest request, CancellationToken cancellationToken);
    Task<P0PayoutBatchDto?> ProcessPayoutBatchAsync(P0Actor actor, Guid batchId, P0PayoutProcessRequest request, CancellationToken cancellationToken);
    Task<P0PayoutBatchDto?> CancelPayoutBatchAsync(P0Actor actor, Guid batchId, P0PayoutDecisionRequest request, CancellationToken cancellationToken);
    Task<P0PayoutBatchDto?> RetryPayoutBatchAsync(P0Actor actor, Guid batchId, CancellationToken cancellationToken);

    Task<P0OwnerPortalDto> GetOwnerPortalAsync(P0Actor actor, Guid? managerUserId, CancellationToken cancellationToken);
}

public sealed record UpsertP0OwnerProfileRequest(string LegalName, string ContactEmail, string ContactPhone, string BillingAddress, string PreferredCurrency, string TimeZone, string OperationalMetadataJson, string Notes, string? BillingMetadataJson = null, string? PaymentProviderCustomerReference = null);
public sealed record P0StatusChangeRequest(string Status, string Reason);
public sealed record P0PortfolioQuery(Guid? OwnerUserId = null, Guid? PropertyId = null, string? Search = null, string? Status = null, int Page = 1, int PageSize = 50);
public sealed record P0AssignPropertiesRequest(IReadOnlyList<Guid> PropertyIds, Guid OwnerUserId, string Reason, Guid? ExpectedBatchId = null);
public sealed record P0CreateAgreementRequest(Guid OwnerUserId, Guid? PropertyId, DateOnly EffectiveFrom, DateOnly? EffectiveTo, string Currency, string TermsJson, string FeeRuleJson, decimal MaintenanceApprovalLimit, decimal ExpenseApprovalLimit, Guid? DocumentId = null, string? DocumentKey = null);
public sealed record P0AgreementQuery(Guid? OwnerUserId = null, Guid? PropertyId = null, string? Status = null, int Page = 1, int PageSize = 50);
public sealed record P0UpdateAgreementRequest(DateOnly EffectiveFrom, DateOnly? EffectiveTo, string Currency, string TermsJson, string FeeRuleJson, decimal MaintenanceApprovalLimit, decimal ExpenseApprovalLimit, Guid? DocumentId = null, string? DocumentKey = null, long RowVersion = 1);
public sealed record P0RenewAgreementRequest(DateOnly EffectiveFrom, DateOnly? EffectiveTo, string? Reason = null);
public sealed record P0TerminateAgreementRequest(string Reason);
public sealed record P0CreateFeeRuleRequest(Guid OwnerUserId, Guid? PropertyId, string Category, string RuleType, string CalculationBasis, string Currency, decimal Percentage, decimal FixedAmount, decimal MinimumAmount, decimal CleaningMarkup, decimal MaintenanceMarkup, DateOnly EffectiveFrom, DateOnly? EffectiveTo = null);
public sealed record P0FeeRuleQuery(Guid? OwnerUserId = null, Guid? PropertyId = null, string? Currency = null, string? Category = null, DateOnly? On = null);
public sealed record P0CalculateFeeRequest(Guid OwnerUserId, Guid? PropertyId, string Category, string Currency, decimal BaseAmount, DateOnly On, Guid? SourceId = null);
public sealed record P0PostFeeRequest(Guid OwnerUserId, Guid? PropertyId, string Category, string Currency, decimal BaseAmount, DateOnly On, Guid? SourceId, string IdempotencyKey);
public sealed record P0JournalLineRequest(string AccountCode, decimal Debit, decimal Credit, Guid? OwnerUserId = null, Guid? PropertyId = null, string? Description = null);
public sealed record P0PostJournalRequest(string SourceType, Guid? SourceId, string? IdempotencyKey, string Currency, DateOnly AccountingDate, string Memo, IReadOnlyList<P0JournalLineRequest> Lines, bool Reconcile = false, string? ReconciliationReference = null, Guid? ApprovalId = null);
public sealed record P0JournalQuery(Guid? OwnerUserId = null, Guid? PropertyId = null, string? Currency = null, DateOnly? From = null, DateOnly? To = null, string? Status = null, int Page = 1, int PageSize = 100);
public sealed record P0ReverseJournalRequest(string Reason, string IdempotencyKey);
public sealed record P0ReconcileJournalRequest(Guid JournalId, string ExternalReference, decimal Amount, string Currency, string Reason);
public sealed record P0StatementQuery(Guid OwnerUserId, Guid? PropertyId, string Currency, DateOnly From, DateOnly To);
public sealed record P0FinalizeStatementRequest(Guid OwnerUserId, Guid? PropertyId, string Currency, DateOnly From, DateOnly To, string IdempotencyKey);
public sealed record P0ProfitabilityQuery(string Currency, DateOnly From, DateOnly To, Guid? OwnerUserId = null, Guid? PropertyId = null);
public sealed record P0CreateApprovalRequest(Guid OwnerUserId, Guid? PropertyId, Guid? AgreementId, Guid? FeeRuleId, string ApprovalType, string Description, decimal Amount, string Currency, IReadOnlyList<Guid>? EvidenceDocumentIds = null, string? SourceType = null, Guid? SourceId = null, DateTimeOffset? ExpiresAt = null, string? IdempotencyKey = null);
public sealed record P0ApprovalQuery(Guid? OwnerUserId = null, Guid? PropertyId = null, string? Status = null, int Page = 1, int PageSize = 100);
public sealed record P0ApprovalDecisionRequest(string Status, string Reason, long RowVersion = 1, string? IdempotencyKey = null);
public sealed record P0InviteStaffRequest(Guid StaffUserId, string Role, IReadOnlyList<Guid>? PropertyIds = null, IReadOnlyList<Guid>? OwnerIds = null, bool CanManageFinance = false, bool CanApprovePayouts = false, decimal ApprovalLimit = 0);
public sealed record P0UpdateStaffRequest(string Role, IReadOnlyList<Guid>? PropertyIds, IReadOnlyList<Guid>? OwnerIds, bool CanManageFinance, bool CanApprovePayouts, decimal ApprovalLimit, string Status, long RowVersion = 1);
public sealed record P0PayoutQuery(Guid? OwnerUserId = null, string? Currency = null, string? Status = null, int Page = 1, int PageSize = 100);
public sealed record P0CreatePayoutBatchRequest(Guid OwnerUserId, string Currency, DateOnly PeriodFrom, DateOnly PeriodTo, string IdempotencyKey, Guid? StatementSnapshotId = null);
public sealed record P0PayoutDecisionRequest(string Reason, long RowVersion = 1);
public sealed record P0PayoutProcessRequest(string? ProviderReference = null, bool SimulateFailure = false, string? FailureReason = null);

public sealed record P0OwnerProfileDto(Guid Id, Guid ManagerUserId, Guid OwnerUserId, string Status, string LegalName, string ContactEmail, string ContactPhone, string BillingAddress, string PreferredCurrency, string TimeZone, string OperationalMetadataJson, string Notes, long Version, DateTimeOffset? ActivatedAt, DateTimeOffset? SuspendedAt, DateTimeOffset? ArchivedAt, string BillingMetadataJson = "{}", string? PaymentProviderCustomerReference = null);
public sealed record P0OwnerLifecycleEventDto(Guid Id, Guid OwnerUserId, string EventType, string FromStatus, string ToStatus, string Reason, Guid ActorUserId, DateTimeOffset CreatedAt);
public sealed record P0PortfolioDto(IReadOnlyList<P0PortfolioRowDto> Items, int Total, int Page, int PageSize);
public sealed record P0PortfolioRowDto(Guid PropertyId, Guid OwnerUserId, string OwnerName, string OwnerEmail, string OwnerStatus, string PropertyTitle, string UnitNumber, string Address, string PropertyStatus, DateTimeOffset? OwnershipChangedAt);
public sealed record P0AgreementDto(Guid Id, Guid ManagerUserId, Guid OwnerUserId, Guid? PropertyId, int Version, Guid? SupersedesAgreementId, string Status, DateOnly EffectiveFrom, DateOnly? EffectiveTo, string Currency, string TermsJson, string FeeRuleJson, decimal MaintenanceApprovalLimit, decimal ExpenseApprovalLimit, Guid? DocumentId, string? DocumentKey, DateTimeOffset? ActivatedAt, DateTimeOffset? TerminatedAt, string? TerminationReason, long RowVersion);
public sealed record P0FeeRuleDto(Guid Id, Guid ManagerUserId, Guid OwnerUserId, Guid? PropertyId, string Category, string RuleType, string CalculationBasis, string Currency, decimal Percentage, decimal FixedAmount, decimal MinimumAmount, decimal CleaningMarkup, decimal MaintenanceMarkup, DateOnly EffectiveFrom, DateOnly? EffectiveTo, bool IsActive, long RowVersion);
public sealed record P0FeeCalculationDto(Guid? RuleId, string Category, string Currency, decimal BaseAmount, decimal CalculatedAmount, decimal PercentageAmount, decimal FixedAmount, decimal MinimumTopUp, decimal MarkupAmount, DateOnly On, Guid? SourceId, bool Posted, Guid? JournalId);
public sealed record P0AccountDto(Guid Id, string Code, string Name, string AccountType, string Currency, Guid? OwnerUserId, Guid? PropertyId, bool IsClientMoney, bool IsPmMoney, bool IsThirdParty, string Status);
public sealed record P0JournalLineDto(Guid Id, Guid AccountId, string AccountCode, Guid? OwnerUserId, Guid? PropertyId, decimal Debit, decimal Credit, string Description, string SourceReference);
public sealed record P0JournalDto(Guid Id, Guid ManagerUserId, string JournalNumber, string SourceType, Guid? SourceId, string? IdempotencyKey, string Currency, DateOnly AccountingDate, string Memo, string Status, string ReconciliationStatus, decimal TotalDebit, decimal TotalCredit, Guid? ReversalOfJournalId, DateTimeOffset? PostedAt, IReadOnlyList<P0JournalLineDto> Lines);
public sealed record P0ReconciliationDto(Guid Id, Guid JournalId, string ExternalReference, string Status, decimal Amount, string Currency, string Reason, Guid ActorUserId, DateTimeOffset ReconciledAt);
public sealed record P0StatementEntryDto(DateOnly Date, string SourceType, Guid? SourceId, string Description, string Currency, decimal Income, decimal Expenses, decimal ManagementFees, decimal Payouts, decimal Net, Guid JournalId);
public sealed record P0StatementDto(Guid? SnapshotId, Guid ManagerUserId, Guid OwnerUserId, Guid? PropertyId, string Currency, DateOnly From, DateOnly To, string Status, decimal OpeningBalance, decimal Income, decimal Expenses, decimal ManagementFees, decimal Payouts, decimal ClosingBalance, bool HasUnresolvedSuspense, IReadOnlyList<P0StatementEntryDto> Entries, string? ContentHash);
public sealed record P0StatementExportDto(string Format, string FileName, string ContentType, string ContentBase64, Guid SnapshotId);
public sealed record P0ProfitabilityDto(string Currency, DateOnly From, DateOnly To, decimal Income, decimal Expenses, decimal ManagementFees, decimal Payouts, decimal OwnerNet, decimal PmMargin, decimal CashCollected, decimal UnreconciledCash, IReadOnlyList<P0ProfitabilityRowDto> Rows);
public sealed record P0ProfitabilityRowDto(Guid OwnerUserId, Guid? PropertyId, decimal Income, decimal Expenses, decimal ManagementFees, decimal OwnerNet, decimal PmMargin);
public sealed record P0ApprovalDto(Guid Id, Guid ManagerUserId, Guid OwnerUserId, Guid? PropertyId, Guid? AgreementId, Guid? FeeRuleId, string ApprovalType, string Description, decimal Amount, decimal Threshold, string Currency, string Status, string EvidenceJson, string? DecisionReason, Guid? DecidedByUserId, DateTimeOffset? DecidedAt, long RowVersion, IReadOnlyList<P0ApprovalEventDto> History, IReadOnlyList<P0ApprovalEvidenceDto>? Evidence = null, string? SourceType = null, Guid? SourceId = null, DateTimeOffset? ExpiresAt = null, string? RequestIdempotencyKey = null);
public sealed record P0ApprovalEventDto(Guid Id, Guid ActorUserId, string EventType, string FromStatus, string ToStatus, string Reason, DateTimeOffset CreatedAt, string? IdempotencyKey = null);
public sealed record P0ApprovalEvidenceDto(Guid Id, string Title, string FileName, string ContentType, long SizeBytes, string Status);
public sealed record P0StaffMembershipDto(Guid Id, Guid ManagerUserId, Guid StaffUserId, string Role, IReadOnlyList<Guid> PropertyIds, IReadOnlyList<Guid> OwnerIds, bool CanManageFinance, bool CanApprovePayouts, decimal ApprovalLimit, string Status, DateTimeOffset? AcceptedAt, DateTimeOffset? SuspendedAt, DateTimeOffset? RevokedAt, long RowVersion);
public sealed record P0StaffEventDto(Guid Id, Guid MembershipId, Guid ActorUserId, string EventType, string FromStatus, string ToStatus, string Reason, DateTimeOffset CreatedAt);
public sealed record P0PayoutAvailabilityDto(Guid ManagerUserId, Guid OwnerUserId, string Currency, DateOnly From, DateOnly To, decimal Income, decimal Expenses, decimal ManagementFees, decimal ExistingPayouts, decimal AvailableReconciledCash, decimal ReservedInDraftOrProcessing, decimal PayableAmount, bool HasUnresolvedSuspense);
public sealed record P0PayoutBatchDto(Guid Id, Guid ManagerUserId, Guid OwnerUserId, string Currency, DateOnly PeriodFrom, DateOnly PeriodTo, decimal Amount, decimal ReservedAmount, string Status, string? IdempotencyKey, string? ProviderReference, string? FailureReason, Guid? ApprovedByUserId, DateTimeOffset? ApprovedAt, DateTimeOffset? ProcessedAt, DateTimeOffset? CancelledAt, Guid? StatementSnapshotId, long RowVersion, IReadOnlyList<P0PayoutItemDto> Items, IReadOnlyList<P0PayoutEventDto> History);
public sealed record P0PayoutItemDto(Guid Id, Guid OwnerUserId, Guid? PropertyId, decimal Amount, Guid? StatementSnapshotId, string SourceJson);
public sealed record P0PayoutEventDto(Guid Id, Guid ActorUserId, string EventType, string FromStatus, string ToStatus, string Reason, string? ProviderReference, DateTimeOffset CreatedAt);
public sealed record P0OwnerPortalDto(Guid ManagerUserId, P0OwnerProfileDto? Profile, IReadOnlyList<PropertyDto> Properties, IReadOnlyList<P0AgreementDto> Agreements, IReadOnlyList<P0ApprovalDto> Approvals, P0StatementDto Statement, IReadOnlyList<P0JournalDto> Transactions, IReadOnlyList<P0PayoutBatchDto> Payouts, IReadOnlyList<P0ProfitabilityRowDto> PropertyFinancials);
