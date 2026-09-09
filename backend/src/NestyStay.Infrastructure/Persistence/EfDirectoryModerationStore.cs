using Microsoft.EntityFrameworkCore;
using NestyStay.Application.Directories;
using NestyStay.Infrastructure.Persistence.Milestones;

namespace NestyStay.Infrastructure.Persistence;

public sealed class EfDirectoryModerationStore(
    NestyStayDbContext db,
    TimeProvider timeProvider) : IDirectoryModerationStore
{
    private static readonly IReadOnlyDictionary<string, string> RequiredBadges = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Custodian"] = "Verified",
        ["Trades"] = "Trusted",
        ["LocalBusiness"] = "Verified",
        ["Police"] = "Wellness"
    };

    public async Task<IReadOnlyList<DirectoryProviderRecord>> GetProvidersAsync(
        string? kind,
        string? category,
        string? parish,
        string? query,
        bool includeUnpublished,
        CancellationToken cancellationToken)
    {
        var normalizedKind = NormalizeOptional(kind);
        var normalizedCategory = NormalizeOptional(category);
        var normalizedParish = NormalizeOptional(parish);
        var normalizedQuery = NormalizeOptional(query);
        var providers = await db.MilestoneDirectoryProviders.AsNoTracking()
            .Where(item => !item.IsDeleted && (includeUnpublished || (item.IsActive && item.Status == "Published" && item.VerificationStatus == "Verified")))
            .Where(item => normalizedKind == null || item.Kind == normalizedKind)
            .Where(item => normalizedCategory == null || item.Category == normalizedCategory)
            .Where(item => normalizedParish == null || item.Parish == normalizedParish)
            .OrderByDescending(item => item.Rating)
            .ToListAsync(cancellationToken);

        var result = providers
            .Where(item => normalizedQuery == null || Contains(item.Name, normalizedQuery) || Contains(item.Description, normalizedQuery) || Contains(item.Category, normalizedQuery))
            .Select(ToRecord)
            .ToList();

        if (normalizedKind == "Police")
        {
            var officers = await db.MilestoneWellnessOfficers.AsNoTracking()
                .Where(item => !item.IsDeleted && item.VerificationStatus == "Verified" && item.OnboardingStatus == "Verified" && item.IsActiveOffDuty && !item.IsRetired)
                .Where(item => normalizedKind == null || normalizedKind == "Police")
                .Where(item => normalizedParish == null || item.Parish == normalizedParish)
                .ToListAsync(cancellationToken);
            result.AddRange(officers
                .Where(item => normalizedQuery == null || Contains(item.BadgeNumber, normalizedQuery) || Contains(item.Parish, normalizedQuery) || Contains(item.CoverageArea, normalizedQuery))
                .Select(ToPoliceRecord));
        }

        return result
            .OrderByDescending(item => item.Rating)
            .ThenBy(item => item.Name)
            .ToList();
    }

    public async Task<DirectoryProviderRecord?> GetProviderAsync(string slug, bool includeUnpublished, CancellationToken cancellationToken)
    {
        var normalized = NormalizeSlug(slug);
        var provider = await db.MilestoneDirectoryProviders.AsNoTracking()
            .Where(item => item.Slug == normalized && !item.IsDeleted && (includeUnpublished || (item.IsActive && item.Status == "Published" && item.VerificationStatus == "Verified")))
            .SingleOrDefaultAsync(cancellationToken);
        if (provider is not null)
        {
            return ToRecord(provider);
        }

        if (normalized.StartsWith("police-", StringComparison.OrdinalIgnoreCase))
        {
            var badge = normalized["police-".Length..].ToUpperInvariant();
            var officer = await db.MilestoneWellnessOfficers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.BadgeNumber == badge && item.VerificationStatus == "Verified" && item.OnboardingStatus == "Verified" && item.IsActiveOffDuty && !item.IsRetired, cancellationToken);
            return officer is null ? null : ToPoliceRecord(officer);
        }

        return null;
    }

    public async Task<DirectoryProviderRecord> SaveProviderAsync(SaveDirectoryProviderRequest request, Guid actorUserId, bool adminOverride, CancellationToken cancellationToken)
    {
        var kind = NormalizeKind(request.Kind);
        if (kind == "Police")
        {
            throw new InvalidOperationException("Police directory entries are created only from verified active off-duty officer accounts.");
        }

        if (kind == "LocalBusiness" && !request.IsBrickAndMortar)
        {
            throw new InvalidOperationException("Local Business providers must be brick-and-mortar businesses.");
        }

        var slug = NormalizeSlug(request.Slug ?? request.Name);
        var entity = await db.MilestoneDirectoryProviders.SingleOrDefaultAsync(item => item.Slug == slug, cancellationToken);
        if (entity is null)
        {
            entity = new MilestoneDirectoryProvider { Id = Guid.NewGuid(), Slug = slug, OwnerUserId = actorUserId, CreatedByUserId = actorUserId, CreatedAt = timeProvider.GetUtcNow() };
            db.MilestoneDirectoryProviders.Add(entity);
        }
        else
        {
            var owner = entity.OwnerUserId ?? entity.CreatedByUserId;
            if (owner is null || owner != actorUserId)
            {
                throw new UnauthorizedAccessException("This provider profile is owned by another account.");
            }
        }

        entity.Kind = kind;
        entity.Category = Require(request.Category, "Category");
        entity.Name = Require(request.Name, "Provider name");
        entity.Parish = Require(request.Parish, "Parish");
        entity.BadgeLevel = RequiredBadges[kind];
        entity.Description = Require(request.Description, "Description");
        entity.AvailabilitySummary = Require(request.AvailabilitySummary, "Availability");
        entity.ContactMode = "Platform messaging only";
        entity.IsBrickAndMortar = request.IsBrickAndMortar;
        if (request.ServiceRadiusKm is <= 0 or > 500) throw new InvalidOperationException("Service radius must be between 0 and 500 km.");
        entity.ServicesJson = MilestoneJson.Serialize((request.Services ?? Array.Empty<string>()).Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(50).ToArray());
        entity.OpeningHours = string.IsNullOrWhiteSpace(request.OpeningHours) ? null : request.OpeningHours.Trim();
        entity.EmergencyAvailable = request.EmergencyAvailable;
        entity.ServiceRadiusKm = request.ServiceRadiusKm;
        entity.PoliceBadgeNumber = null;
        entity.IsActive = adminOverride && request.IsActive;
        entity.VerificationStatus = adminOverride ? "Verified" : "Pending";
        entity.Status = adminOverride ? "Published" : "PendingReview";
        entity.Rating = entity.Rating == 0 ? 0 : entity.Rating;
        entity.ReviewCount = entity.ReviewCount < 0 ? 0 : entity.ReviewCount;
        entity.UpdatedAt = timeProvider.GetUtcNow();
        entity.UpdatedByUserId = actorUserId;
        await db.SaveChangesAsync(cancellationToken);
        return ToRecord(entity);
    }

    public async Task<DirectoryProviderRecord?> ModerateAsync(string slug, string status, Guid actorUserId, string? reason, CancellationToken cancellationToken)
    {
        var entity = await db.MilestoneDirectoryProviders.SingleOrDefaultAsync(item => item.Slug == NormalizeSlug(slug) && !item.IsDeleted, cancellationToken);
        if (entity is null) return null;

        var normalizedStatus = status.Trim().ToLowerInvariant() switch
        {
            "approve" or "approved" or "publish" or "published" => "Published",
            "reject" or "rejected" => "Rejected",
            "request-changes" or "request_changes" or "changes" => "ChangesRequested",
            "suspend" or "suspended" => "Suspended",
            "reactivate" or "active" => "Published",
            _ => throw new InvalidOperationException("Unsupported provider moderation status.")
        };

        entity.Status = normalizedStatus;
        entity.VerificationStatus = normalizedStatus == "Published" ? "Verified" : normalizedStatus;
        entity.IsActive = normalizedStatus == "Published";
        entity.UpdatedAt = timeProvider.GetUtcNow();
        entity.UpdatedByUserId = actorUserId;
        await db.SaveChangesAsync(cancellationToken);
        return ToRecord(entity);
    }

    public async Task RecordRecentViewAsync(Guid userId, Guid providerId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty || providerId == Guid.Empty) throw new InvalidOperationException("A signed-in user and provider are required.");
        var providerExists = await db.MilestoneDirectoryProviders.AnyAsync(item => item.Id == providerId && !item.IsDeleted && item.IsActive && item.Status == "Published" && item.VerificationStatus == "Verified", cancellationToken)
            || await db.MilestoneWellnessOfficers.AnyAsync(item => item.Id == providerId && !item.IsDeleted && item.VerificationStatus == "Verified" && item.OnboardingStatus == "Verified" && item.IsActiveOffDuty && !item.IsRetired, cancellationToken);
        if (!providerExists) throw new KeyNotFoundException("Published provider not found.");
        var view = await db.MilestoneDirectoryRecentViews.SingleOrDefaultAsync(item => item.UserId == userId && item.ProviderId == providerId, cancellationToken);
        if (view is null)
        {
            view = new MilestoneDirectoryRecentView { Id = Guid.NewGuid(), UserId = userId, ProviderId = providerId, ViewedAt = timeProvider.GetUtcNow() };
            db.MilestoneDirectoryRecentViews.Add(view);
        }
        else
        {
            view.ViewedAt = timeProvider.GetUtcNow();
            view.UpdatedAt = view.ViewedAt;
            view.IsDeleted = false;
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DirectoryProviderRecord>> GetRecentViewsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var views = await db.MilestoneDirectoryRecentViews.AsNoTracking()
            .Where(item => item.UserId == userId && !item.IsDeleted)
            .OrderByDescending(item => item.ViewedAt)
            .Take(50)
            .ToListAsync(cancellationToken);
        var providerIds = views.Select(item => item.ProviderId).ToArray();
        var providers = await db.MilestoneDirectoryProviders.AsNoTracking()
            .Where(item => providerIds.Contains(item.Id) && !item.IsDeleted && item.IsActive && item.Status == "Published" && item.VerificationStatus == "Verified")
            .ToListAsync(cancellationToken);
        var officers = await db.MilestoneWellnessOfficers.AsNoTracking()
            .Where(item => providerIds.Contains(item.Id) && !item.IsDeleted && item.VerificationStatus == "Verified" && item.OnboardingStatus == "Verified" && item.IsActiveOffDuty && !item.IsRetired)
            .ToListAsync(cancellationToken);
        var result = new List<DirectoryProviderRecord>(views.Count);
        foreach (var view in views)
        {
            var provider = providers.FirstOrDefault(item => item.Id == view.ProviderId);
            if (provider is not null) { result.Add(ToRecord(provider)); continue; }
            var officer = officers.FirstOrDefault(item => item.Id == view.ProviderId);
            if (officer is not null) result.Add(ToPoliceRecord(officer));
        }
        return result;
    }

    public async Task RemoveRecentViewAsync(Guid userId, Guid providerId, CancellationToken cancellationToken)
    {
        var view = await db.MilestoneDirectoryRecentViews.SingleOrDefaultAsync(item => item.UserId == userId && item.ProviderId == providerId && !item.IsDeleted, cancellationToken);
        if (view is null) return;
        view.IsDeleted = true;
        view.UpdatedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearRecentViewsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var views = await db.MilestoneDirectoryRecentViews.Where(item => item.UserId == userId && !item.IsDeleted).ToListAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        foreach (var view in views) { view.IsDeleted = true; view.UpdatedAt = now; }
        await db.SaveChangesAsync(cancellationToken);
    }

    private static DirectoryProviderRecord ToRecord(MilestoneDirectoryProvider item) =>
        new(item.Id, item.OwnerUserId, item.Slug, item.Kind, item.Category, item.Name, item.Parish, item.BadgeLevel, item.Description, item.AvailabilitySummary, item.ContactMode, item.Rating, item.ReviewCount, item.VerificationStatus, item.Status, item.IsActive, item.IsBrickAndMortar, item.Kind == "Police" ? item.PoliceBadgeNumber : null, item.CreatedAt, item.UpdatedAt, MilestoneJson.DeserializeList<string>(item.ServicesJson), item.OpeningHours, item.EmergencyAvailable, item.ServiceRadiusKm, item.WeeklyHoursJson, item.HolidayClosuresJson, item.PromotionsJson, item.AccessibilityInfo);

    private static DirectoryProviderRecord ToPoliceRecord(MilestoneWellnessOfficer officer) =>
        new(officer.Id, null, $"police-{NormalizeSlug(officer.BadgeNumber)}", "Police", "Police Wellness", officer.BadgeNumber, officer.Parish, "Wellness", "Active off-duty JCF officer", officer.CoverageArea, "Platform messaging only", 0, 0, "Verified", "Published", true, false, officer.BadgeNumber, officer.CreatedAt, officer.UpdatedAt);

    private static string NormalizeKind(string value)
    {
        var normalized = Require(value, "Provider type").Replace(" ", string.Empty, StringComparison.Ordinal);
        return normalized.ToLowerInvariant() switch
        {
            "custodian" => "Custodian",
            "trades" or "trade" => "Trades",
            "localbusiness" or "local" => "LocalBusiness",
            "police" => "Police",
            _ => throw new InvalidOperationException("Unsupported directory provider type.")
        };
    }

    private static string NormalizeSlug(string value)
    {
        var normalized = new string(Require(value, "Provider slug").Trim().ToLowerInvariant().Select(character => char.IsLetterOrDigit(character) ? character : '-').ToArray());
        normalized = string.Join('-', normalized.Split('-', StringSplitOptions.RemoveEmptyEntries));
        if (normalized.Length is < 3 or > 96) throw new InvalidOperationException("Provider slug must be between 3 and 96 characters.");
        return normalized;
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool Contains(string value, string query) => value.Contains(query, StringComparison.OrdinalIgnoreCase);
    private static string Require(string? value, string field) => string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException($"{field} is required.") : value.Trim();
}
