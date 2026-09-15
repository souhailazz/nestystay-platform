namespace NestyStay.Application.Directories;

public interface IDirectoryModerationStore
{
    Task<IReadOnlyList<DirectoryProviderRecord>> GetProvidersAsync(string? kind, string? category, string? parish, string? query, bool includeUnpublished, CancellationToken cancellationToken);
    Task<DirectoryProviderRecord?> GetProviderAsync(string slug, bool includeUnpublished, CancellationToken cancellationToken);
    Task<DirectoryProviderRecord> SaveProviderAsync(SaveDirectoryProviderRequest request, Guid actorUserId, bool adminOverride, CancellationToken cancellationToken);
    Task<DirectoryProviderRecord?> ModerateAsync(string slug, string status, Guid actorUserId, string? reason, CancellationToken cancellationToken);
    Task RecordRecentViewAsync(Guid userId, Guid providerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DirectoryProviderRecord>> GetRecentViewsAsync(Guid userId, CancellationToken cancellationToken);
    Task RemoveRecentViewAsync(Guid userId, Guid providerId, CancellationToken cancellationToken);
    Task ClearRecentViewsAsync(Guid userId, CancellationToken cancellationToken);
}

/// <summary>
/// Provider-facing directory workflows.  The moderation store remains focused
/// on publication; this contract owns quotes, reviews, structured business data
/// and provider analytics so each workflow can be authorized independently.
/// </summary>
public interface IDirectoryEnhancementStore
{
    Task<DirectoryQuoteDto> CreateQuoteRequestAsync(Guid requesterUserId, string slug, CreateDirectoryQuoteRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<DirectoryQuoteDto>> ListQuotesAsync(Guid actorUserId, bool isAdmin, CancellationToken cancellationToken);
    Task<DirectoryQuoteDto?> RespondToQuoteAsync(Guid providerOwnerUserId, bool isAdmin, Guid quoteId, RespondDirectoryQuoteRequest request, CancellationToken cancellationToken);
    Task<DirectoryReviewDto> CreateReviewAsync(Guid reviewerUserId, string slug, CreateDirectoryReviewRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<DirectoryReviewDto>> ListReviewsAsync(string slug, bool includeUnpublished, CancellationToken cancellationToken);
    Task<DirectoryReviewDto?> RespondToReviewAsync(Guid providerOwnerUserId, bool isAdmin, Guid reviewId, RespondDirectoryReviewRequest request, CancellationToken cancellationToken);
    Task<DirectoryProviderRecord?> SaveBusinessDetailsAsync(Guid actorUserId, bool isAdmin, string slug, SaveDirectoryBusinessDetailsRequest request, CancellationToken cancellationToken);
    Task<DirectoryProviderInsightsDto?> GetProviderInsightsAsync(Guid actorUserId, bool isAdmin, string slug, CancellationToken cancellationToken);
}

public sealed record DirectoryProviderRecord(
    Guid Id,
    Guid? OwnerUserId,
    string Slug,
    string Kind,
    string Category,
    string Name,
    string Parish,
    string BadgeLevel,
    string Description,
    string AvailabilitySummary,
    string ContactMode,
    decimal Rating,
    int ReviewCount,
    string VerificationStatus,
    string Status,
    bool IsActive,
    bool IsBrickAndMortar,
    string? PoliceBadgeNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<string>? Services = null,
    string? OpeningHours = null,
    bool EmergencyAvailable = false,
    decimal? ServiceRadiusKm = null,
    string? WeeklyHoursJson = null,
    string? HolidayClosuresJson = null,
    string? PromotionsJson = null,
    string? AccessibilityInfo = null);

public sealed record SaveDirectoryProviderRequest(
    string? Slug,
    string Kind,
    string Category,
    string Name,
    string Parish,
    string BadgeLevel,
    string Description,
    string AvailabilitySummary,
    string ContactMode,
    bool IsBrickAndMortar = false,
    string? PoliceBadgeNumber = null,
    bool IsActive = false,
    IReadOnlyList<string>? Services = null,
    string? OpeningHours = null,
    bool EmergencyAvailable = false,
    decimal? ServiceRadiusKm = null);

public sealed record CreateDirectoryQuoteRequest(string Scope, DateTimeOffset? PreferredAt = null, decimal? Budget = null, DateTimeOffset? ExpiresAt = null);
public sealed record RespondDirectoryQuoteRequest(string Status, decimal? Amount = null, string? Message = null);
public sealed record CreateDirectoryReviewRequest(int Rating, string Body);
public sealed record RespondDirectoryReviewRequest(string Response);
public sealed record SaveDirectoryBusinessDetailsRequest(string WeeklyHoursJson, string HolidayClosuresJson = "[]", string PromotionsJson = "[]", string? AccessibilityInfo = null);
public sealed record DirectoryQuoteDto(Guid Id, Guid ProviderId, string ProviderSlug, Guid RequesterUserId, string Scope, DateTimeOffset? PreferredAt, decimal? Budget, decimal? ResponseAmount, string Status, string? Message, DateTimeOffset CreatedAt, DateTimeOffset? ExpiresAt, DateTimeOffset? RespondedAt);
public sealed record DirectoryReviewDto(Guid Id, Guid ProviderId, string ProviderSlug, Guid ReviewerUserId, int Rating, string Body, string Status, string? ProviderResponse, DateTimeOffset CreatedAt, DateTimeOffset? RespondedAt);
public sealed record DirectoryProviderInsightsDto(string ProviderSlug, int QuoteRequests, int AcceptedQuotes, int Reviews, decimal AverageRating, int Responses, IReadOnlyList<DirectoryQuoteDto> Quotes, IReadOnlyList<DirectoryReviewDto> ReviewsList);
