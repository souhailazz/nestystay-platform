namespace NestyStay.Application.PropertyManager;

public interface IPropertyManagerStore
{
    Task<PropertyManagerDashboardDto> GetDashboardAsync(Guid actorUserId, bool isAdmin, CancellationToken cancellationToken);
    Task<OwnerDto> InviteOwnerAsync(Guid managerUserId, InviteOwnerRequest request, CancellationToken cancellationToken);
    Task<OwnerDto?> ReviewOwnerAsync(Guid managerUserId, Guid ownerUserId, string status, CancellationToken cancellationToken);
    Task<ManagerProfileDto> RenewSubscriptionAsync(Guid managerUserId, CancellationToken cancellationToken);
    Task<PropertyDto> AddPropertyAsync(Guid managerUserId, AddPropertyRequest request, CancellationToken cancellationToken);
    Task<InvoiceDto> CreateInvoiceAsync(Guid managerUserId, CreateInvoiceRequest request, CancellationToken cancellationToken);
    Task<InvoiceDto?> GetInvoiceAsync(Guid actorUserId, bool isAdmin, Guid invoiceId, CancellationToken cancellationToken);
    Task<InvoiceDto?> PayInvoiceAsync(Guid actorUserId, bool isAdmin, Guid invoiceId, PayInvoiceRequest request, CancellationToken cancellationToken);
    Task<StatementDto> GetStatementAsync(Guid actorUserId, bool isAdmin, Guid ownerUserId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken);
    Task<UtilityChargeDto> CreateUtilityAsync(Guid managerUserId, CreateUtilityRequest request, CancellationToken cancellationToken);
    Task<MaintenanceDto> CreateMaintenanceAsync(Guid actorUserId, bool isAdmin, CreateMaintenanceRequest request, CancellationToken cancellationToken);
    Task<MaintenanceDto?> UpdateMaintenanceAsync(Guid managerUserId, Guid maintenanceId, UpdateMaintenanceRequest request, CancellationToken cancellationToken);
    Task<VendorDto> CreateVendorAsync(Guid managerUserId, CreateVendorRequest request, CancellationToken cancellationToken);
    Task<NoticeDto> CreateNoticeAsync(Guid managerUserId, CreateNoticeRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<NoticeDto>> GetNoticesAsync(Guid actorUserId, bool isAdmin, CancellationToken cancellationToken);
    Task<ProposalDto> CreateProposalAsync(Guid managerUserId, CreateProposalRequest request, CancellationToken cancellationToken);
    Task<ProposalDto> VoteAsync(Guid actorUserId, bool isAdmin, Guid proposalId, VoteRequest request, CancellationToken cancellationToken);
    Task<ProxyDto> CreateProxyAsync(Guid ownerUserId, CreateProxyRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<DocumentDto>> GetDocumentsAsync(Guid actorUserId, bool isAdmin, CancellationToken cancellationToken);
    Task<DocumentDto> AddDocumentAsync(Guid managerUserId, AddDocumentRequest request, CancellationToken cancellationToken);
    Task<GateMessageDto> CreateGateMessageAsync(Guid managerUserId, CreateGateMessageRequest request, CancellationToken cancellationToken);
    Task<QrIssueDto> IssueQrAsync(Guid managerUserId, IssueQrRequest request, CancellationToken cancellationToken);
    Task<QrValidationDto> ValidateQrAsync(string token, Guid? propertyId, Guid? gateGuardUserId, CancellationToken cancellationToken);
    Task<QrValidationDto> RevokeQrAsync(Guid managerUserId, Guid qrId, CancellationToken cancellationToken);
    Task<OwnerPortalDto> GetOwnerPortalAsync(Guid ownerUserId, CancellationToken cancellationToken);
}

public sealed record InviteOwnerRequest(string Email, string DisplayName, Guid? OwnerUserId = null, Guid? CommunityId = null);
public sealed record AddPropertyRequest(Guid OwnerUserId, string Title, string UnitNumber, string Address, Guid? CommunityId = null);
public sealed record CreateInvoiceLineRequest(string Description, decimal Quantity, decimal UnitAmount);
public sealed record CreateInvoiceRequest(Guid OwnerUserId, Guid? PropertyId, DateOnly DueDate, decimal Tax, IReadOnlyList<CreateInvoiceLineRequest> Lines);
public sealed record PayInvoiceRequest(decimal Amount, string IdempotencyKey);
public sealed record CreateUtilityRequest(Guid OwnerUserId, Guid PropertyId, string UtilityType, string BillingPeriod, decimal Usage, decimal Rate);
public sealed record CreateMaintenanceRequest(Guid OwnerUserId, Guid PropertyId, string Title, string Description, string Category, string Urgency);
public sealed record UpdateMaintenanceRequest(string Status, Guid? VendorId, DateTimeOffset? ScheduledAt, decimal Cost, string Notes);
public sealed record CreateVendorRequest(string Name, string Category, string Contact, string Notes);
public sealed record CreateNoticeRequest(Guid? CommunityId, Guid? TargetOwnerUserId, string Title, string Body, DateTimeOffset? ExpiresAt, bool IsPinned);
public sealed record CreateProposalRequest(Guid? CommunityId, string Title, string Description, DateTimeOffset OpensAt, DateTimeOffset ClosesAt, bool IsAnonymous, int? Quorum);
public sealed record VoteRequest(string Choice, Guid? ProxyId = null);
public sealed record CreateProxyRequest(Guid ProposalId, Guid ProxyUserId, DateTimeOffset ValidUntil);
public sealed record AddDocumentRequest(Guid? OwnerUserId, Guid? PropertyId, string Title, string Category, string FileName, string ContentType, long SizeBytes, string? ContentBase64 = null);
public sealed record CreateGateMessageRequest(Guid? CommunityId, Guid? PropertyId, string Recipient, string Message, string VisitorType, DateTimeOffset ValidFrom, DateTimeOffset ValidUntil);
public sealed record IssueQrRequest(Guid? OwnerUserId, Guid? PropertyId, string SubjectType, DateTimeOffset ValidFrom, DateTimeOffset ValidUntil);

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
public sealed record ManagerProfileDto(Guid ManagerUserId, string BusinessName, string SubscriptionTier, decimal MonthlyAmount, string SubscriptionStatus, DateTimeOffset NextBillingAt);
public sealed record OwnerDto(Guid Id, Guid OwnerUserId, string DisplayName, string Email, string VerificationStatus, string InvitationStatus, Guid? CommunityId);
public sealed record PropertyDto(Guid Id, Guid OwnerUserId, Guid? CommunityId, string Title, string UnitNumber, string Address, string Status, string OccupancyStatus);
public sealed record InvoiceLineDto(Guid Id, string Description, decimal Quantity, decimal UnitAmount, decimal Amount);
public sealed record InvoiceDto(Guid Id, Guid OwnerUserId, Guid? PropertyId, string InvoiceNumber, DateOnly IssueDate, DateOnly DueDate, decimal Subtotal, decimal Tax, decimal Total, decimal AmountPaid, decimal Balance, string Currency, string Status, IReadOnlyList<InvoiceLineDto> Lines);
public sealed record PaymentDto(Guid Id, Guid InvoiceId, decimal Amount, string Provider, string ProviderReference, string Status, DateTimeOffset CreatedAt);
public sealed record StatementEntryDto(DateOnly Date, string Type, string Description, decimal Amount, Guid? InvoiceId);
public sealed record StatementDto(Guid OwnerUserId, DateOnly From, DateOnly To, decimal OpeningBalance, IReadOnlyList<StatementEntryDto> Entries, decimal ClosingBalance, IReadOnlyList<InvoiceDto> Invoices, IReadOnlyList<PaymentDto> Payments);
public sealed record UtilityChargeDto(Guid Id, Guid OwnerUserId, Guid PropertyId, string UtilityType, string BillingPeriod, decimal Usage, decimal Rate, decimal Amount, Guid? InvoiceId, string Status);
public sealed record MaintenanceDto(Guid Id, Guid OwnerUserId, Guid PropertyId, Guid? VendorId, string Title, string Description, string Category, string Urgency, string Status, DateTimeOffset? ScheduledAt, decimal Cost, string Notes);
public sealed record VendorDto(Guid Id, string Name, string Category, string Contact, string VerificationStatus, bool IsActive, string Notes);
public sealed record NoticeDto(Guid Id, Guid? CommunityId, Guid? TargetOwnerUserId, string Title, string Body, DateTimeOffset PublishAt, DateTimeOffset? ExpiresAt, bool IsPinned, bool IsArchived);
public sealed record ProposalDto(Guid Id, Guid? CommunityId, string Title, string Description, DateTimeOffset OpensAt, DateTimeOffset ClosesAt, string Status, bool IsAnonymous, int? Quorum, int EligibleVoters, int VotesCast, IReadOnlyDictionary<string, int> Results);
public sealed record ProxyDto(Guid Id, Guid ProposalId, Guid OwnerUserId, Guid ProxyUserId, string Status, DateTimeOffset ValidUntil, DateTimeOffset? AcceptedAt);
public sealed record DocumentDto(Guid Id, Guid? OwnerUserId, Guid? PropertyId, string Title, string Category, string FileName, string ContentType, long SizeBytes, string AccessScope, bool IsArchived, DateTimeOffset CreatedAt);
public sealed record GateMessageDto(Guid Id, Guid? CommunityId, Guid? PropertyId, string Recipient, string Message, string VisitorType, DateTimeOffset ValidFrom, DateTimeOffset ValidUntil);
public sealed record QrIssueDto(Guid Id, string Token, string SubjectType, Guid? PropertyId, DateTimeOffset ValidFrom, DateTimeOffset ValidUntil);
public sealed record QrValidationDto(string Result, string Status, Guid? PropertyId, string SubjectType, DateTimeOffset? ValidUntil, Guid? QrId, string? Message);
public sealed record OwnerPortalDto(Guid OwnerUserId, IReadOnlyList<PropertyDto> Properties, IReadOnlyList<InvoiceDto> Invoices, StatementDto Statement, IReadOnlyList<UtilityChargeDto> Utilities, IReadOnlyList<MaintenanceDto> Maintenance, IReadOnlyList<NoticeDto> Notices, IReadOnlyList<ProposalDto> Proposals, IReadOnlyList<DocumentDto> Documents);
