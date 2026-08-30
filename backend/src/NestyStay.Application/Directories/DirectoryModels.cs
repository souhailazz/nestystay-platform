namespace NestyStay.Application.Directories;

public interface IDirectoryModerationStore
{
    Task<IReadOnlyList<DirectoryProviderRecord>> GetProvidersAsync(string? kind, string? category, string? parish, string? query, bool includeUnpublished, CancellationToken cancellationToken);
    Task<DirectoryProviderRecord?> GetProviderAsync(string slug, bool includeUnpublished, CancellationToken cancellationToken);
    Task<DirectoryProviderRecord> SaveProviderAsync(SaveDirectoryProviderRequest request, Guid actorUserId, bool adminOverride, CancellationToken cancellationToken);
    Task<DirectoryProviderRecord?> ModerateAsync(string slug, string status, Guid actorUserId, string? reason, CancellationToken cancellationToken);
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
    DateTimeOffset UpdatedAt);

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
    bool IsActive = false);
