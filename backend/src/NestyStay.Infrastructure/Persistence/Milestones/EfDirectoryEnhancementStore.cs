using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NestyStay.Application.Directories;

namespace NestyStay.Infrastructure.Persistence.Milestones;

/// <summary>
/// PostgreSQL-backed quote, review and structured local-business workflows.
/// All provider mutations are scoped to the owning provider account (or an
/// explicitly authorized administrator); public reads only expose published
/// reviews and never expose requester identity.
/// </summary>
public sealed class EfDirectoryEnhancementStore(
    NestyStayDbContext db,
    TimeProvider timeProvider) : IDirectoryEnhancementStore
{
    public async Task<DirectoryQuoteDto> CreateQuoteRequestAsync(Guid requesterUserId, string slug, CreateDirectoryQuoteRequest request, CancellationToken cancellationToken)
    {
        if (requesterUserId == Guid.Empty) throw new UnauthorizedAccessException("A signed-in requester is required.");
        var provider = await RequirePublishedProviderAsync(slug, cancellationToken);
        var scope = Require(request.Scope, "Quote scope");
        if (scope.Length > 2_000) throw new InvalidOperationException("Quote scope is too long.");
        if (request.Budget is < 0) throw new InvalidOperationException("Quote budget cannot be negative.");
        var now = timeProvider.GetUtcNow();
        var expiry = request.ExpiresAt ?? now.AddDays(7);
        if (expiry <= now || expiry > now.AddDays(31)) throw new InvalidOperationException("Quote expiry must be within 31 days.");
        var item = new MilestoneDirectoryQuote
        {
            ProviderId = provider.Id,
            RequesterUserId = requesterUserId,
            Scope = scope,
            PreferredAt = request.PreferredAt,
            Budget = request.Budget,
            ExpiresAt = expiry,
            Status = "OPEN"
        };
        db.MilestoneDirectoryQuotes.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(item, provider.Slug);
    }

    public async Task<IReadOnlyList<DirectoryQuoteDto>> ListQuotesAsync(Guid actorUserId, bool isAdmin, CancellationToken cancellationToken)
    {
        var query = db.MilestoneDirectoryQuotes.AsNoTracking().Where(x => !x.IsDeleted);
        if (!isAdmin)
        {
            var owned = db.MilestoneDirectoryProviders.Where(x => x.OwnerUserId == actorUserId && !x.IsDeleted).Select(x => x.Id);
            query = query.Where(x => x.RequesterUserId == actorUserId || owned.Contains(x.ProviderId));
        }
        var rows = await query.Join(db.MilestoneDirectoryProviders, quote => quote.ProviderId, provider => provider.Id, (quote, provider) => new { quote, provider.Slug })
            .OrderByDescending(x => x.quote.CreatedAt).Take(250).ToListAsync(cancellationToken);
        return rows.Select(x => ToDto(x.quote, x.Slug)).ToList();
    }

    public async Task<DirectoryQuoteDto?> RespondToQuoteAsync(Guid providerOwnerUserId, bool isAdmin, Guid quoteId, RespondDirectoryQuoteRequest request, CancellationToken cancellationToken)
    {
        var row = await db.MilestoneDirectoryQuotes.SingleOrDefaultAsync(x => x.Id == quoteId && !x.IsDeleted, cancellationToken);
        if (row is null) return null;
        var provider = await db.MilestoneDirectoryProviders.SingleOrDefaultAsync(x => x.Id == row.ProviderId && !x.IsDeleted, cancellationToken);
        if (provider is null || (!isAdmin && provider.OwnerUserId != providerOwnerUserId)) throw new UnauthorizedAccessException("Quote is outside the provider account.");
        var status = Require(request.Status, "Quote status").Trim().ToUpperInvariant();
        if (status is not ("ACCEPTED" or "DECLINED" or "QUESTIONS")) throw new InvalidOperationException("Quote status must be ACCEPTED, DECLINED, or QUESTIONS.");
        if (request.Amount is < 0) throw new InvalidOperationException("Quote amount cannot be negative.");
        row.Status = status;
        row.ResponseAmount = request.Amount;
        row.Message = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim();
        row.RespondedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(row, provider.Slug);
    }

    public async Task<DirectoryReviewDto> CreateReviewAsync(Guid reviewerUserId, string slug, CreateDirectoryReviewRequest request, CancellationToken cancellationToken)
    {
        if (reviewerUserId == Guid.Empty) throw new UnauthorizedAccessException("A signed-in reviewer is required.");
        var provider = await RequirePublishedProviderAsync(slug, cancellationToken);
        if (request.Rating is < 1 or > 5) throw new InvalidOperationException("Review rating must be between 1 and 5.");
        var body = Require(request.Body, "Review text");
        if (body.Length > 2_000) throw new InvalidOperationException("Review text is too long.");
        var existing = await db.MilestoneDirectoryReviews.SingleOrDefaultAsync(x => x.ProviderId == provider.Id && x.ReviewerUserId == reviewerUserId && !x.IsDeleted, cancellationToken);
        if (existing is not null) throw new InvalidOperationException("You have already reviewed this provider.");
        var item = new MilestoneDirectoryReview { ProviderId = provider.Id, ReviewerUserId = reviewerUserId, Rating = request.Rating, Body = body, Status = "PUBLISHED" };
        db.MilestoneDirectoryReviews.Add(item);
        var published = await db.MilestoneDirectoryReviews.Where(x => x.ProviderId == provider.Id && !x.IsDeleted && x.Status == "PUBLISHED").ToListAsync(cancellationToken);
        provider.ReviewCount = published.Count + 1;
        provider.Rating = decimal.Round((published.Sum(x => x.Rating) + item.Rating) / (decimal)provider.ReviewCount, 2, MidpointRounding.AwayFromZero);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(item, provider.Slug);
    }

    public async Task<IReadOnlyList<DirectoryReviewDto>> ListReviewsAsync(string slug, bool includeUnpublished, CancellationToken cancellationToken)
    {
        var provider = await db.MilestoneDirectoryProviders.AsNoTracking().SingleOrDefaultAsync(x => x.Slug == NormalizeSlug(slug) && !x.IsDeleted, cancellationToken);
        if (provider is null) return [];
        var rows = await db.MilestoneDirectoryReviews.AsNoTracking().Where(x => x.ProviderId == provider.Id && !x.IsDeleted && (includeUnpublished || x.Status == "PUBLISHED")).OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync(cancellationToken);
        return rows.Select(x => ToDto(x, provider.Slug)).ToList();
    }

    public async Task<DirectoryReviewDto?> RespondToReviewAsync(Guid providerOwnerUserId, bool isAdmin, Guid reviewId, RespondDirectoryReviewRequest request, CancellationToken cancellationToken)
    {
        var row = await db.MilestoneDirectoryReviews.SingleOrDefaultAsync(x => x.Id == reviewId && !x.IsDeleted, cancellationToken);
        if (row is null) return null;
        var provider = await db.MilestoneDirectoryProviders.SingleOrDefaultAsync(x => x.Id == row.ProviderId && !x.IsDeleted, cancellationToken);
        if (provider is null || (!isAdmin && provider.OwnerUserId != providerOwnerUserId)) throw new UnauthorizedAccessException("Review is outside the provider account.");
        var response = Require(request.Response, "Provider response");
        if (response.Length > 2_000) throw new InvalidOperationException("Provider response is too long.");
        row.ProviderResponse = response;
        row.RespondedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(row, provider.Slug);
    }

    public async Task<DirectoryProviderRecord?> SaveBusinessDetailsAsync(Guid actorUserId, bool isAdmin, string slug, SaveDirectoryBusinessDetailsRequest request, CancellationToken cancellationToken)
    {
        var provider = await db.MilestoneDirectoryProviders.SingleOrDefaultAsync(x => x.Slug == NormalizeSlug(slug) && !x.IsDeleted, cancellationToken);
        if (provider is null) return null;
        if (!isAdmin && provider.OwnerUserId != actorUserId) throw new UnauthorizedAccessException("Provider profile is outside the current account.");
        provider.WeeklyHoursJson = ValidateJson(request.WeeklyHoursJson, "Weekly hours");
        provider.HolidayClosuresJson = ValidateJson(request.HolidayClosuresJson, "Holiday closures");
        provider.PromotionsJson = ValidateJson(request.PromotionsJson, "Promotions");
        provider.AccessibilityInfo = string.IsNullOrWhiteSpace(request.AccessibilityInfo) ? null : request.AccessibilityInfo.Trim();
        provider.UpdatedAt = timeProvider.GetUtcNow();
        provider.UpdatedByUserId = actorUserId;
        await db.SaveChangesAsync(cancellationToken);
        return new DirectoryProviderRecord(provider.Id, provider.OwnerUserId, provider.Slug, provider.Kind, provider.Category, provider.Name, provider.Parish, provider.BadgeLevel, provider.Description, provider.AvailabilitySummary, provider.ContactMode, provider.Rating, provider.ReviewCount, provider.VerificationStatus, provider.Status, provider.IsActive, provider.IsBrickAndMortar, provider.PoliceBadgeNumber, provider.CreatedAt, provider.UpdatedAt, MilestoneJson.DeserializeList<string>(provider.ServicesJson), provider.OpeningHours, provider.EmergencyAvailable, provider.ServiceRadiusKm, provider.WeeklyHoursJson, provider.HolidayClosuresJson, provider.PromotionsJson, provider.AccessibilityInfo);
    }

    public async Task<DirectoryProviderInsightsDto?> GetProviderInsightsAsync(Guid actorUserId, bool isAdmin, string slug, CancellationToken cancellationToken)
    {
        var provider = await db.MilestoneDirectoryProviders.AsNoTracking().SingleOrDefaultAsync(x => x.Slug == NormalizeSlug(slug) && !x.IsDeleted, cancellationToken);
        if (provider is null) return null;
        if (!isAdmin && provider.OwnerUserId != actorUserId) throw new UnauthorizedAccessException("Provider insights are outside the current account.");
        var quotes = await db.MilestoneDirectoryQuotes.AsNoTracking().Where(x => x.ProviderId == provider.Id && !x.IsDeleted).OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync(cancellationToken);
        var reviews = await db.MilestoneDirectoryReviews.AsNoTracking().Where(x => x.ProviderId == provider.Id && !x.IsDeleted).OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync(cancellationToken);
        return new DirectoryProviderInsightsDto(provider.Slug, quotes.Count, quotes.Count(x => x.Status == "ACCEPTED"), reviews.Count(x => x.Status == "PUBLISHED"), provider.Rating, reviews.Count(x => x.RespondedAt != null), quotes.Select(x => ToDto(x, provider.Slug)).ToList(), reviews.Select(x => ToDto(x, provider.Slug)).ToList());
    }

    private async Task<MilestoneDirectoryProvider> RequirePublishedProviderAsync(string slug, CancellationToken cancellationToken) =>
        await db.MilestoneDirectoryProviders.SingleOrDefaultAsync(x => x.Slug == NormalizeSlug(slug) && !x.IsDeleted && x.IsActive && x.Status == "Published" && x.VerificationStatus == "Verified", cancellationToken)
        ?? throw new KeyNotFoundException("Published provider not found.");

    private static DirectoryQuoteDto ToDto(MilestoneDirectoryQuote item, string slug) => new(item.Id, item.ProviderId, slug, item.RequesterUserId, item.Scope, item.PreferredAt, item.Budget, item.ResponseAmount, item.Status, item.Message, item.CreatedAt, item.ExpiresAt, item.RespondedAt);
    private static DirectoryReviewDto ToDto(MilestoneDirectoryReview item, string slug) => new(item.Id, item.ProviderId, slug, item.ReviewerUserId, item.Rating, item.Body, item.Status, item.ProviderResponse, item.CreatedAt, item.RespondedAt);
    private static string ValidateJson(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException($"{field} JSON is required.");
        try { using var document = JsonDocument.Parse(value); return document.RootElement.GetRawText(); }
        catch (JsonException) { throw new InvalidOperationException($"{field} must be valid JSON."); }
    }
    private static string NormalizeSlug(string value) => string.Join('-', Require(value, "Provider slug").Trim().ToLowerInvariant().Split('-', StringSplitOptions.RemoveEmptyEntries));
    private static string Require(string? value, string field) => string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException($"{field} is required.") : value.Trim();
}
