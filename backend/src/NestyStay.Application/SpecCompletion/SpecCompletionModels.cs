using NestyStay.Domain;

namespace NestyStay.Application.SpecCompletion;

public interface ISpecCompletionStore
{
    Task<SpecSeedStatusDto> EnsureSeededAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PublicContentPageDto>> GetPublicPagesAsync(CancellationToken cancellationToken);
    Task<PublicContentPageDto?> GetPublicPageAsync(string slug, CancellationToken cancellationToken);
    Task<ContactRequestDto> CreateContactRequestAsync(CreateContactRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<ExperienceDto>> GetExperiencesAsync(string? category, string? parish, string? query, CancellationToken cancellationToken);
    Task<ExperienceDto?> GetExperienceAsync(string slug, CancellationToken cancellationToken);
    Task<IReadOnlyList<JournalArticleDto>> GetJournalAsync(string? category, string? query, CancellationToken cancellationToken);
    Task<JournalArticleDto?> GetJournalArticleAsync(string slug, CancellationToken cancellationToken);
    Task<IReadOnlyList<HostProfileDto>> GetHostProfilesAsync(CancellationToken cancellationToken);
    Task<HostProfileDto?> GetHostProfileAsync(string slug, CancellationToken cancellationToken);
    Task<HostProfileDto> UpsertHostProfileAsync(string slug, UpsertHostProfileRequest request, Guid actorUserId, CancellationToken cancellationToken);
    Task<TravelerWorkspaceDto> GetTravelerWorkspaceAsync(Guid userId, CancellationToken cancellationToken);
    Task<WishlistCollectionDto> CreateWishlistCollectionAsync(Guid userId, SaveWishlistCollectionRequest request, CancellationToken cancellationToken);
    Task<WishlistCollectionDto> RenameWishlistCollectionAsync(Guid userId, Guid collectionId, SaveWishlistCollectionRequest request, CancellationToken cancellationToken);
    Task DeleteWishlistCollectionAsync(Guid userId, Guid collectionId, CancellationToken cancellationToken);
    Task<WishlistItemDto> AddWishlistItemAsync(Guid userId, Guid collectionId, SaveWishlistItemRequest request, CancellationToken cancellationToken);
    Task RemoveWishlistItemAsync(Guid userId, Guid itemId, CancellationToken cancellationToken);
    Task<PaymentMethodSetupIntentDto> CreatePaymentMethodSetupIntentAsync(Guid userId, CancellationToken cancellationToken);
    Task<PaymentMethodDto> AddPaymentMethodAsync(Guid userId, SavePaymentMethodRequest request, CancellationToken cancellationToken);
    Task SetDefaultPaymentMethodAsync(Guid userId, Guid paymentMethodId, CancellationToken cancellationToken);
    Task RemovePaymentMethodAsync(Guid userId, Guid paymentMethodId, CancellationToken cancellationToken);
    Task<IdentityDocumentUploadDto> PrepareIdentityDocumentUploadAsync(Guid userId, PrepareIdentityDocumentUploadRequest request, CancellationToken cancellationToken);
    Task<IdentityDocumentUploadDto> UploadIdentityDocumentContentAsync(Guid userId, Guid uploadId, string contentType, long sizeBytes, Stream content, CancellationToken cancellationToken);
    Task<ReviewDto> SubmitReviewAsync(Guid userId, SaveReviewRequest request, CancellationToken cancellationToken);
    Task<ReviewDto> ReplyToReviewAsync(Guid hostUserId, Guid reviewId, SaveReviewReplyRequest request, CancellationToken cancellationToken);
    Task MarkNotificationReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken);
    Task MarkAllNotificationsReadAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DirectoryProviderDto>> GetDirectoryProvidersAsync(string? kind, string? category, string? parish, string? query, CancellationToken cancellationToken);
    Task<DirectoryProviderDto?> GetDirectoryProviderAsync(string slug, CancellationToken cancellationToken);
    Task<DirectoryProviderDto> UpsertDirectoryProviderAsync(UpsertDirectoryProviderRequest request, Guid actorUserId, CancellationToken cancellationToken);
    Task<MessagingInboxDto> GetInboxAsync(Guid userId, CancellationToken cancellationToken);
    Task<ConversationDto?> GetConversationAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken);
    Task<ConversationDto> CreateConversationAsync(Guid userId, CreateConversationRequest request, CancellationToken cancellationToken);
    Task<AttachmentUploadDto> PrepareMessageAttachmentUploadAsync(Guid userId, Guid conversationId, PrepareMessageAttachmentUploadRequest request, CancellationToken cancellationToken);
    Task<AttachmentUploadDto> UploadMessageAttachmentContentAsync(Guid userId, Guid conversationId, Guid attachmentId, string contentType, long sizeBytes, Stream content, CancellationToken cancellationToken);
    Task<AttachmentUploadDto> CompleteMessageAttachmentUploadAsync(Guid userId, Guid conversationId, Guid attachmentId, CompleteMessageAttachmentUploadRequest request, CancellationToken cancellationToken);
    Task<AttachmentDownloadDto> GetMessageAttachmentDownloadAsync(Guid userId, Guid conversationId, Guid attachmentId, CancellationToken cancellationToken);
    Task<MessageDto> SendMessageAsync(Guid userId, Guid conversationId, SendMessageRequest request, CancellationToken cancellationToken);
    Task MarkConversationReadAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken);
    Task<HostOperationsDto> GetHostOperationsAsync(Guid hostUserId, CancellationToken cancellationToken);
    Task<HostPricingRuleDto> SaveHostPricingRuleAsync(Guid hostUserId, SaveHostPricingRuleRequest request, CancellationToken cancellationToken);
    Task<HostPromotionDto> SaveHostPromotionAsync(Guid hostUserId, SaveHostPromotionRequest request, CancellationToken cancellationToken);
    Task<AdminOperationsDto> GetAdminOperationsAsync(CancellationToken cancellationToken);
    Task<AdminCaseDto> CreateAdminCaseAsync(CreateAdminCaseRequest request, Guid? actorUserId, CancellationToken cancellationToken);
    Task<AdminCaseDto> ResolveAdminCaseAsync(Guid caseId, ResolveAdminCaseRequest request, Guid? actorUserId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditEventDto>> GetAuditEventsAsync(CancellationToken cancellationToken);
    Task<AuthFlowResultDto> StartAuthFlowAsync(StartAuthFlowRequest request, CancellationToken cancellationToken);
    Task<AuthFlowResultDto> CompleteAuthFlowAsync(CompleteAuthFlowRequest request, CancellationToken cancellationToken);
    Task<DevelopmentAuthFlowSecretDto?> GetDevelopmentAuthFlowSecretAsync(Guid flowId, CancellationToken cancellationToken);
    Task<IReadOnlyList<RecoveryCodeDto>> GenerateRecoveryCodesAsync(Guid userId, CancellationToken cancellationToken);
    Task<SocialAuthConfigDto> GetSocialAuthConfigAsync(CancellationToken cancellationToken);
}

public sealed record SpecSeedStatusDto(bool Seeded, int PublicPages, int Experiences, int JournalArticles, int HostProfiles, int DirectoryProviders);
public sealed record PublicContentPageDto(string Slug, string Title, string Kind, string Summary, string Body, IReadOnlyList<string> Sections, IReadOnlyList<string> Links);
public sealed record CreateContactRequest(string Name, string Email, string Subject, string Message);
public sealed record ContactRequestDto(Guid Id, string Name, string Email, string Subject, string Message, string Status, DateTimeOffset CreatedAt);
public sealed record ExperienceDto(Guid Id, string Slug, string Name, string Category, string Parish, string ProviderName, decimal Price, string Currency, int DurationMinutes, decimal Rating, string Summary, string Description, IReadOnlyList<string> Images, IReadOnlyList<string> Included, IReadOnlyList<string> Rules, IReadOnlyList<string> Availability);
public sealed record JournalArticleDto(Guid Id, string Slug, string Title, string Category, string Author, DateTimeOffset PublishedAt, string Summary, string Body, IReadOnlyList<string> Tags, IReadOnlyList<string> RelatedSlugs);
public sealed record HostProfileDto(Guid Id, Guid HostUserId, string Slug, string DisplayName, string Parish, string Bio, string ResponseTime, IReadOnlyList<BadgeLevel> Badges, IReadOnlyList<Guid> ListingIds, decimal Rating, int ReviewCount, bool IsPublic, IReadOnlyList<string> Highlights);
public sealed record UpsertHostProfileRequest(Guid HostUserId, string DisplayName, string Parish, string Bio, string ResponseTime, IReadOnlyList<BadgeLevel>? Badges, IReadOnlyList<Guid>? ListingIds, bool IsPublic, IReadOnlyList<string>? Highlights);
public sealed record TravelerWorkspaceDto(Guid UserId, IReadOnlyList<WishlistCollectionDto> WishlistCollections, IReadOnlyList<PaymentMethodDto> PaymentMethods, IReadOnlyList<IdentityDocumentDto> IdentityDocuments, IReadOnlyList<ReviewDto> Reviews, IReadOnlyList<TravelerNotificationDto> Notifications);
public sealed record WishlistCollectionDto(Guid Id, Guid UserId, string Name, int SortOrder, IReadOnlyList<WishlistItemDto> Items);
public sealed record WishlistItemDto(Guid Id, Guid CollectionId, Guid UserId, Guid PropertyId, string PropertyTitle, string Status, int SortOrder, DateTimeOffset CreatedAt);
public sealed record SaveWishlistCollectionRequest(string Name, int SortOrder = 0);
public sealed record SaveWishlistItemRequest(Guid PropertyId, string PropertyTitle, string Status = "Available", int SortOrder = 0);
public sealed record PaymentMethodSetupIntentDto(string ProviderName, string SetupIntentReference, string ClientSecret, string Status, DateTimeOffset ExpiresAt, string? PublishableKey);
public sealed record PaymentMethodDto(Guid Id, Guid UserId, string ProviderName, string ProviderPaymentMethodReference, string Brand, string Last4, int ExpMonth, int ExpYear, bool IsDefault, DateTimeOffset CreatedAt);
public sealed record SavePaymentMethodRequest(string SetupIntentReference, bool IsDefault = false);
public sealed record PrepareIdentityDocumentUploadRequest(string DocumentType, string FileName, string ContentType, long SizeBytes, string? IssuingCountry = null, DateOnly? ExpiresOn = null);
public sealed record IdentityDocumentUploadDto(Guid Id, Guid UserId, string DocumentType, string FileName, string ContentType, long SizeBytes, string ObjectKey, string UploadUrl, string Status, string ScanStatus, DateTimeOffset ExpiresAt, string? Sha256Hash = null, Guid? IdentityDocumentId = null);
public sealed record IdentityDocumentDto(Guid Id, Guid UserId, string DocumentType, string FileName, string ContentType, long SizeBytes, string Status, string ScanStatus, DateTimeOffset UploadedAt, string? IssuingCountry, DateOnly? ExpiresOn);
public sealed record ReviewDto(Guid Id, Guid UserId, Guid? PropertyId, Guid? BookingId, string SubjectTitle, int Rating, string Text, string Status, string? HostReply, DateTimeOffset CreatedAt, DateTimeOffset EditableUntil);
public sealed record SaveReviewRequest(Guid? PropertyId, Guid? BookingId, string SubjectTitle, int Rating, string Text);
public sealed record SaveReviewReplyRequest(string Reply);
public sealed record TravelerNotificationDto(Guid Id, Guid UserId, string Type, string Title, string Body, string DeepLink, bool IsRead, DateTimeOffset CreatedAt, DateTimeOffset? ReadAt);
public sealed record DirectoryProviderDto(Guid Id, Guid? OwnerUserId, string Slug, string Kind, string Category, string Name, string Parish, string BadgeLevel, string Description, string AvailabilitySummary, string ContactMode, decimal Rating, int ReviewCount, bool IsActive);
public sealed record UpsertDirectoryProviderRequest(string? Slug, string Kind, string Category, string Name, string Parish, string BadgeLevel, string Description, string AvailabilitySummary, string ContactMode, bool IsActive = true);
public sealed record MessagingInboxDto(Guid UserId, IReadOnlyList<ConversationSummaryDto> Conversations);
public sealed record ConversationSummaryDto(Guid Id, string Subject, string ParticipantLabel, string LastMessage, DateTimeOffset UpdatedAt, int UnreadCount, bool IsSupportThread, string OnlineStatus);
public sealed record ConversationDto(Guid Id, string Subject, Guid? BookingId, bool IsSupportThread, IReadOnlyList<ConversationParticipantDto> Participants, IReadOnlyList<MessageDto> Messages);
public sealed record ConversationParticipantDto(Guid UserId, string DisplayName, string Role, DateTimeOffset? LastReadAt, string OnlineStatus);
public sealed record CreateConversationRequest(string Subject, Guid? BookingId, bool IsSupportThread, IReadOnlyList<ConversationParticipantInput> Participants, string InitialMessage);
public sealed record ConversationParticipantInput(Guid UserId, string DisplayName, string Role);
public sealed record PrepareMessageAttachmentUploadRequest(string FileName, string ContentType, long SizeBytes);
public sealed record CompleteMessageAttachmentUploadRequest(string ContentType, long SizeBytes, string HeaderBytesBase64, string Sha256Hash);
public sealed record AttachmentUploadDto(Guid Id, Guid ConversationId, Guid OwnerUserId, string FileName, string ContentType, long SizeBytes, string ObjectKey, string UploadUrl, string Status, DateTimeOffset ExpiresAt, string StorageProviderName = "", string ScanStatus = "PendingScan", string? Sha256Hash = null, string? ThumbnailUrl = null);
public sealed record AttachmentDownloadDto(Guid Id, string FileName, string ContentType, long SizeBytes, string Url, DateTimeOffset ExpiresAt);
public sealed record SendMessageRequest(string Body, IReadOnlyList<MessageAttachmentDto>? Attachments);
public sealed record MessageDto(Guid Id, Guid ConversationId, Guid SenderUserId, string Body, string Status, DateTimeOffset SentAt, DateTimeOffset? ReadAt, IReadOnlyList<MessageAttachmentDto> Attachments);
public sealed record MessageAttachmentDto(Guid? AttachmentId, string FileName, string ContentType, long SizeBytes, string? Url, string Status, string? ObjectKey = null, DateTimeOffset? ExpiresAt = null, string ScanStatus = "Clean", string? ThumbnailUrl = null);
public sealed record HostOperationsDto(Guid HostUserId, HostAnalyticsDto Analytics, IReadOnlyList<HostPricingRuleDto> PricingRules, IReadOnlyList<HostPromotionDto> Promotions, IReadOnlyList<ReviewDto> Reviews);
public sealed record HostAnalyticsDto(decimal Revenue, decimal OccupancyPercent, decimal AverageNightlyRate, int BookingCount, decimal ConversionPercent, IReadOnlyList<ChartPointDto> RevenueSeries, IReadOnlyList<ChartPointDto> OccupancySeries);
public sealed record ChartPointDto(string Label, decimal Value);
public sealed record HostPricingRuleDto(Guid Id, Guid HostUserId, Guid PropertyId, string Name, DateOnly StartsOn, DateOnly EndsOn, decimal NightlyRate, int MinimumStay, bool IsActive);
public sealed record SaveHostPricingRuleRequest(Guid PropertyId, string Name, DateOnly StartsOn, DateOnly EndsOn, decimal NightlyRate, int MinimumStay, bool IsActive);
public sealed record HostPromotionDto(Guid Id, Guid HostUserId, Guid PropertyId, string Name, decimal DiscountPercent, DateOnly StartsOn, DateOnly EndsOn, int MinimumNights, string BadgeLevel, bool IsActive);
public sealed record SaveHostPromotionRequest(Guid PropertyId, string Name, decimal DiscountPercent, DateOnly StartsOn, DateOnly EndsOn, int MinimumNights, string BadgeLevel, bool IsActive);
public sealed record AdminOperationsDto(IReadOnlyList<AdminCaseDto> Cases, IReadOnlyList<AuditEventDto> AuditEvents, IReadOnlyList<AdminMetricDto> Metrics);
public sealed record AdminMetricDto(string Label, string Value);
public sealed record AdminCaseDto(Guid Id, string CaseType, string SubjectType, Guid? SubjectId, string Status, string Priority, string Reason, string AssignedTo, string ResolutionNotes, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? ResolvedAt);
public sealed record CreateAdminCaseRequest(string CaseType, string SubjectType, Guid? SubjectId, string Priority, string Reason, string AssignedTo = "");
public sealed record ResolveAdminCaseRequest(string ResolutionNotes, string Status = "Resolved");
public sealed record AuditEventDto(Guid Id, Guid? ActorUserId, string ActorRole, string Action, string SubjectType, Guid? SubjectId, string Reason, DateTimeOffset CreatedAt);
public sealed record AuthFlowResultDto(
    Guid Id,
    Guid? UserId,
    string FlowType,
    string Destination,
    string Status,
    string DeliveryChannel,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? LastSentAt,
    int AttemptsRemaining);
public sealed record DevelopmentAuthFlowSecretDto(Guid Id, string Code, string Token, DateTimeOffset ExpiresAt);
public sealed record StartAuthFlowRequest(Guid? UserId, string FlowType, string Destination, string? RequestIp = null);
public sealed record CompleteAuthFlowRequest(Guid FlowId, string Code);
public sealed record RecoveryCodeDto(string Code, bool Used);
public sealed record SocialAuthConfigDto(bool GoogleEnabled, bool AppleEnabled, bool FacebookEnabled, IReadOnlyList<string> RequiredEnvironmentVariables);
