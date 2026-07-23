using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NestyStay.Application.Abstractions;
using NestyStay.Application.SpecCompletion;
using NestyStay.Domain;

namespace NestyStay.Infrastructure.Persistence.Milestones;

public sealed class EfSpecCompletionStore(
    NestyStayDbContext db,
    TimeProvider timeProvider,
    IEmailSender emailSender,
    ISmsSender smsSender,
    IDevelopmentAuthSecretStore developmentAuthSecrets,
    IGoogleIdentityValidator googleIdentityValidator,
    IStorageProvider storageProvider) : ISpecCompletionStore
{
    private const int MaximumAuthFlowAttempts = 5;
    private const int MaximumAccountAuthFlowsPerWindow = 5;
    private const int MaximumIpAuthFlowsPerWindow = 20;
    private const string AuthStatusPending = "Pending";
    private const string AuthStatusCompleted = "Completed";
    private const string AuthStatusExpired = "Expired";
    private const string AuthStatusFailed = "Failed";
    private const string AuthStatusInvalidated = "Invalidated";
    private const long MaximumAttachmentBytes = 10 * 1024 * 1024;
    private static readonly TimeSpan AuthFlowResendCooldown = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan AuthFlowRateLimitWindow = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan AttachmentUploadLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan AttachmentDownloadLifetime = TimeSpan.FromHours(24);
    private static readonly IReadOnlyDictionary<string, string[]> AllowedAttachmentExtensions = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = [".jpg", ".jpeg"],
        ["image/png"] = [".png"],
        ["image/webp"] = [".webp"],
        ["application/pdf"] = [".pdf"]
    };
    private static readonly TimeSpan VerificationCodeLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan PasswordResetLifetime = TimeSpan.FromMinutes(30);
    private static readonly Guid SeedHostUserId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
    private static readonly Guid SeedGuestUserId = Guid.Parse("99999999-9999-4999-8999-999999999999");
    private static readonly Guid SeedPropertyId = Guid.Parse("11111111-1111-4111-8111-111111111111");

    public async Task<SpecSeedStatusDto> EnsureSeededAsync(CancellationToken cancellationToken)
    {
        await SeedAsync(cancellationToken);
        return new SpecSeedStatusDto(
            true,
            await db.MilestonePublicContentPages.CountAsync(cancellationToken),
            await db.MilestoneExperiences.CountAsync(cancellationToken),
            await db.MilestoneJournalArticles.CountAsync(cancellationToken),
            await db.MilestoneHostProfiles.CountAsync(cancellationToken),
            await db.MilestoneDirectoryProviders.CountAsync(cancellationToken));
    }

    public async Task<IReadOnlyList<PublicContentPageDto>> GetPublicPagesAsync(CancellationToken cancellationToken)
    {
        await SeedAsync(cancellationToken);
        return await db.MilestonePublicContentPages.AsNoTracking()
            .Where(page => page.IsPublished && !page.IsDeleted)
            .OrderBy(page => page.Kind)
            .ThenBy(page => page.Title)
            .Select(page => ToDto(page))
            .ToListAsync(cancellationToken);
    }

    public async Task<PublicContentPageDto?> GetPublicPageAsync(string slug, CancellationToken cancellationToken)
    {
        await SeedAsync(cancellationToken);
        var normalized = NormalizeContentSlug(slug);
        return await db.MilestonePublicContentPages.AsNoTracking()
            .Where(page => page.Slug == normalized && page.IsPublished && !page.IsDeleted)
            .Select(page => ToDto(page))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ContactRequestDto> CreateContactRequestAsync(CreateContactRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Subject) ||
            string.IsNullOrWhiteSpace(request.Message))
        {
            throw new InvalidOperationException("Name, email, subject, and message are required.");
        }

        var entity = new MilestoneContactRequest
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Subject = request.Subject.Trim(),
            Message = request.Message.Trim(),
            Status = "Open",
            CreatedAt = timeProvider.GetUtcNow(),
            UpdatedAt = timeProvider.GetUtcNow()
        };
        db.MilestoneContactRequests.Add(entity);
        await AddAuditAsync("ContactRequestCreated", "ContactRequest", entity.Id, "Public contact form submitted.", null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<IReadOnlyList<ExperienceDto>> GetExperiencesAsync(string? category, string? parish, string? query, CancellationToken cancellationToken)
    {
        await SeedAsync(cancellationToken);
        var normalizedCategory = category?.Trim();
        var normalizedParish = parish?.Trim();
        var normalizedQuery = query?.Trim().ToLowerInvariant();
        var experiences = await db.MilestoneExperiences.AsNoTracking()
            .Where(item => item.IsPublished && !item.IsDeleted)
            .Where(item => string.IsNullOrWhiteSpace(normalizedCategory) || item.Category == normalizedCategory)
            .Where(item => string.IsNullOrWhiteSpace(normalizedParish) || item.Parish == normalizedParish)
            .OrderByDescending(item => item.Rating)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);
        return experiences
            .Where(item => string.IsNullOrWhiteSpace(normalizedQuery) ||
                           item.Name.ToLowerInvariant().Contains(normalizedQuery) ||
                           item.ProviderName.ToLowerInvariant().Contains(normalizedQuery) ||
                           item.Summary.ToLowerInvariant().Contains(normalizedQuery))
            .Select(ToDto)
            .ToList();
    }

    public async Task<ExperienceDto?> GetExperienceAsync(string slug, CancellationToken cancellationToken)
    {
        await SeedAsync(cancellationToken);
        var normalized = NormalizeSlug(slug);
        return await db.MilestoneExperiences.AsNoTracking()
            .Where(item => item.Slug == normalized && item.IsPublished && !item.IsDeleted)
            .Select(item => ToDto(item))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<JournalArticleDto>> GetJournalAsync(string? category, string? query, CancellationToken cancellationToken)
    {
        await SeedAsync(cancellationToken);
        var normalizedCategory = category?.Trim();
        var normalizedQuery = query?.Trim().ToLowerInvariant();
        var articles = await db.MilestoneJournalArticles.AsNoTracking()
            .Where(item => item.IsPublished && !item.IsDeleted)
            .Where(item => string.IsNullOrWhiteSpace(normalizedCategory) || item.Category == normalizedCategory)
            .OrderByDescending(item => item.PublishedAt)
            .ToListAsync(cancellationToken);
        return articles
            .Where(item => string.IsNullOrWhiteSpace(normalizedQuery) ||
                           item.Title.ToLowerInvariant().Contains(normalizedQuery) ||
                           item.Summary.ToLowerInvariant().Contains(normalizedQuery))
            .Select(ToDto)
            .ToList();
    }

    public async Task<JournalArticleDto?> GetJournalArticleAsync(string slug, CancellationToken cancellationToken)
    {
        await SeedAsync(cancellationToken);
        var normalized = NormalizeSlug(slug);
        return await db.MilestoneJournalArticles.AsNoTracking()
            .Where(item => item.Slug == normalized && item.IsPublished && !item.IsDeleted)
            .Select(item => ToDto(item))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HostProfileDto>> GetHostProfilesAsync(CancellationToken cancellationToken)
    {
        await SeedAsync(cancellationToken);
        return await db.MilestoneHostProfiles.AsNoTracking()
            .Where(profile => profile.IsPublic && !profile.IsDeleted)
            .OrderByDescending(profile => profile.Rating)
            .Select(profile => ToDto(profile))
            .ToListAsync(cancellationToken);
    }

    public async Task<HostProfileDto?> GetHostProfileAsync(string slug, CancellationToken cancellationToken)
    {
        await SeedAsync(cancellationToken);
        var normalized = NormalizeSlug(slug);
        return await db.MilestoneHostProfiles.AsNoTracking()
            .Where(profile => profile.Slug == normalized && profile.IsPublic && !profile.IsDeleted)
            .Select(profile => ToDto(profile))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<HostProfileDto> UpsertHostProfileAsync(string slug, UpsertHostProfileRequest request, Guid actorUserId, CancellationToken cancellationToken)
    {
        if (actorUserId != request.HostUserId)
        {
            throw new UnauthorizedAccessException("Only the host can update this profile.");
        }

        var normalized = NormalizeSlug(slug);
        var profile = await db.MilestoneHostProfiles.SingleOrDefaultAsync(item => item.Slug == normalized, cancellationToken);
        if (profile is null)
        {
            profile = new MilestoneHostProfile { Slug = normalized, HostUserId = request.HostUserId, CreatedByUserId = actorUserId };
            db.MilestoneHostProfiles.Add(profile);
        }
        else if (profile.HostUserId != actorUserId)
        {
            throw new UnauthorizedAccessException("This host profile slug is already owned by another host.");
        }

        await EnsureHostListingIdsBelongToHostAsync(actorUserId, request.ListingIds, cancellationToken);

        profile.DisplayName = RequireText(request.DisplayName, "Display name");
        profile.Parish = RequireText(request.Parish, "Parish");
        profile.Bio = RequireText(request.Bio, "Biography");
        profile.ResponseTime = RequireText(request.ResponseTime, "Response time");
        profile.BadgesJson = MilestoneJson.Serialize(request.Badges ?? [BadgeLevel.Verified]);
        profile.ListingIdsJson = MilestoneJson.Serialize(request.ListingIds ?? []);
        profile.IsPublic = request.IsPublic;
        profile.HighlightsJson = MilestoneJson.Serialize(request.Highlights ?? []);
        profile.Rating = profile.Rating == 0 ? 4.9m : profile.Rating;
        profile.ReviewCount = profile.ReviewCount == 0 ? 24 : profile.ReviewCount;
        profile.UpdatedAt = timeProvider.GetUtcNow();
        profile.UpdatedByUserId = actorUserId;
        await AddAuditAsync("HostProfileUpdated", "HostProfile", profile.Id, "Host profile changed.", actorUserId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(profile);
    }

    public async Task<TravelerWorkspaceDto> GetTravelerWorkspaceAsync(Guid userId, CancellationToken cancellationToken)
    {
        await SeedTravelerAsync(userId, cancellationToken);
        return new TravelerWorkspaceDto(
            userId,
            await GetWishlistCollectionsNoSeedAsync(userId, cancellationToken),
            await db.MilestoneTravelerPaymentMethods.AsNoTracking().Where(item => item.UserId == userId && !item.IsDeleted).OrderByDescending(item => item.IsDefault).Select(item => ToDto(item)).ToListAsync(cancellationToken),
            await db.MilestoneReviews.AsNoTracking().Where(item => item.UserId == userId && !item.IsDeleted).OrderByDescending(item => item.CreatedAt).Select(item => ToDto(item)).ToListAsync(cancellationToken),
            await db.MilestoneTravelerNotifications.AsNoTracking().Where(item => item.UserId == userId && !item.IsDeleted).OrderByDescending(item => item.CreatedAt).Select(item => ToDto(item)).ToListAsync(cancellationToken));
    }

    public async Task<WishlistCollectionDto> CreateWishlistCollectionAsync(Guid userId, SaveWishlistCollectionRequest request, CancellationToken cancellationToken)
    {
        var entity = new MilestoneWishlistCollection
        {
            UserId = userId,
            Name = RequireText(request.Name, "Collection name"),
            SortOrder = request.SortOrder,
            CreatedByUserId = userId,
            UpdatedByUserId = userId
        };
        db.MilestoneWishlistCollections.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return new WishlistCollectionDto(entity.Id, entity.UserId, entity.Name, entity.SortOrder, []);
    }

    public async Task<WishlistCollectionDto> RenameWishlistCollectionAsync(Guid userId, Guid collectionId, SaveWishlistCollectionRequest request, CancellationToken cancellationToken)
    {
        var entity = await FindCollectionAsync(userId, collectionId, cancellationToken);
        entity.Name = RequireText(request.Name, "Collection name");
        entity.SortOrder = request.SortOrder;
        entity.UpdatedAt = timeProvider.GetUtcNow();
        entity.UpdatedByUserId = userId;
        await db.SaveChangesAsync(cancellationToken);
        return (await GetWishlistCollectionsNoSeedAsync(userId, cancellationToken)).Single(item => item.Id == collectionId);
    }

    public async Task DeleteWishlistCollectionAsync(Guid userId, Guid collectionId, CancellationToken cancellationToken)
    {
        var entity = await FindCollectionAsync(userId, collectionId, cancellationToken);
        entity.IsDeleted = true;
        foreach (var item in await db.MilestoneWishlistItems.Where(item => item.CollectionId == collectionId).ToListAsync(cancellationToken))
        {
            item.IsDeleted = true;
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<WishlistItemDto> AddWishlistItemAsync(Guid userId, Guid collectionId, SaveWishlistItemRequest request, CancellationToken cancellationToken)
    {
        _ = await FindCollectionAsync(userId, collectionId, cancellationToken);
        var existing = await db.MilestoneWishlistItems.SingleOrDefaultAsync(
            item => item.UserId == userId && item.CollectionId == collectionId && item.PropertyId == request.PropertyId && !item.IsDeleted,
            cancellationToken);
        if (existing is not null)
        {
            return ToDto(existing);
        }

        var entity = new MilestoneWishlistItem
        {
            UserId = userId,
            CollectionId = collectionId,
            PropertyId = request.PropertyId,
            PropertyTitle = RequireText(request.PropertyTitle, "Property title"),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Available" : request.Status.Trim(),
            SortOrder = request.SortOrder,
            CreatedByUserId = userId,
            UpdatedByUserId = userId
        };
        db.MilestoneWishlistItems.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task RemoveWishlistItemAsync(Guid userId, Guid itemId, CancellationToken cancellationToken)
    {
        var item = await db.MilestoneWishlistItems.SingleOrDefaultAsync(entity => entity.Id == itemId && entity.UserId == userId && !entity.IsDeleted, cancellationToken)
            ?? throw new UnauthorizedAccessException("Wishlist item was not found for this traveler.");
        item.IsDeleted = true;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PaymentMethodDto> AddPaymentMethodAsync(Guid userId, SavePaymentMethodRequest request, CancellationToken cancellationToken)
    {
        if (request.Last4.Trim().Length != 4 || request.ExpMonth is < 1 or > 12 || request.ExpYear < 2026)
        {
            throw new InvalidOperationException("A valid tokenized payment method is required.");
        }

        if (request.IsDefault || !await db.MilestoneTravelerPaymentMethods.AnyAsync(item => item.UserId == userId && !item.IsDeleted, cancellationToken))
        {
            await ClearDefaultPaymentMethodsAsync(userId, cancellationToken);
        }

        var entity = new MilestoneTravelerPaymentMethod
        {
            UserId = userId,
            Brand = RequireText(request.Brand, "Card brand"),
            Last4 = request.Last4.Trim(),
            ExpMonth = request.ExpMonth,
            ExpYear = request.ExpYear,
            IsDefault = request.IsDefault,
            CreatedByUserId = userId,
            UpdatedByUserId = userId
        };
        db.MilestoneTravelerPaymentMethods.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task SetDefaultPaymentMethodAsync(Guid userId, Guid paymentMethodId, CancellationToken cancellationToken)
    {
        var method = await FindPaymentMethodAsync(userId, paymentMethodId, cancellationToken);
        await ClearDefaultPaymentMethodsAsync(userId, cancellationToken);
        method.IsDefault = true;
        method.UpdatedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemovePaymentMethodAsync(Guid userId, Guid paymentMethodId, CancellationToken cancellationToken)
    {
        var method = await FindPaymentMethodAsync(userId, paymentMethodId, cancellationToken);
        method.IsDeleted = true;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ReviewDto> SubmitReviewAsync(Guid userId, SaveReviewRequest request, CancellationToken cancellationToken)
    {
        if (request.Rating is < 1 or > 5)
        {
            throw new InvalidOperationException("Review rating must be between 1 and 5.");
        }

        var entity = new MilestoneReview
        {
            UserId = userId,
            PropertyId = request.PropertyId,
            BookingId = request.BookingId,
            SubjectTitle = RequireText(request.SubjectTitle, "Review subject"),
            Rating = request.Rating,
            Text = RequireText(request.Text, "Review text"),
            Status = "Published",
            EditableUntil = timeProvider.GetUtcNow().AddHours(48),
            CreatedByUserId = userId,
            UpdatedByUserId = userId
        };
        db.MilestoneReviews.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<ReviewDto> ReplyToReviewAsync(Guid hostUserId, Guid reviewId, SaveReviewReplyRequest request, CancellationToken cancellationToken)
    {
        var review = await db.MilestoneReviews.SingleOrDefaultAsync(item => item.Id == reviewId && !item.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Review not found.");

        await EnsureReviewBelongsToHostAsync(hostUserId, review, cancellationToken);

        review.HostReply = RequireText(request.Reply, "Reply");
        review.UpdatedAt = timeProvider.GetUtcNow();
        review.UpdatedByUserId = hostUserId;
        await AddAuditAsync("ReviewReplyUpdated", "Review", review.Id, "Host replied to a verified review.", hostUserId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(review);
    }

    public async Task MarkNotificationReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken)
    {
        var notification = await db.MilestoneTravelerNotifications.SingleOrDefaultAsync(item => item.Id == notificationId && item.UserId == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Notification was not found for this traveler.");
        notification.IsRead = true;
        notification.ReadAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllNotificationsReadAsync(Guid userId, CancellationToken cancellationToken)
    {
        var notifications = await db.MilestoneTravelerNotifications.Where(item => item.UserId == userId && !item.IsRead).ToListAsync(cancellationToken);
        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = timeProvider.GetUtcNow();
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DirectoryProviderDto>> GetDirectoryProvidersAsync(string? kind, string? category, string? parish, string? query, CancellationToken cancellationToken)
    {
        await SeedAsync(cancellationToken);
        var normalizedKind = kind?.Trim();
        var normalizedCategory = category?.Trim();
        var normalizedParish = parish?.Trim();
        var normalizedQuery = query?.Trim().ToLowerInvariant();
        var providers = await db.MilestoneDirectoryProviders.AsNoTracking()
            .Where(item => item.IsActive && !item.IsDeleted)
            .Where(item => string.IsNullOrWhiteSpace(normalizedKind) || item.Kind == normalizedKind)
            .Where(item => string.IsNullOrWhiteSpace(normalizedCategory) || item.Category == normalizedCategory)
            .Where(item => string.IsNullOrWhiteSpace(normalizedParish) || item.Parish == normalizedParish)
            .OrderByDescending(item => item.Rating)
            .ToListAsync(cancellationToken);
        return providers
            .Where(item => string.IsNullOrWhiteSpace(normalizedQuery) ||
                           item.Name.ToLowerInvariant().Contains(normalizedQuery) ||
                           item.Description.ToLowerInvariant().Contains(normalizedQuery))
            .Select(ToDto)
            .ToList();
    }

    public async Task<DirectoryProviderDto?> GetDirectoryProviderAsync(string slug, CancellationToken cancellationToken)
    {
        await SeedAsync(cancellationToken);
        var normalized = NormalizeSlug(slug);
        return await db.MilestoneDirectoryProviders.AsNoTracking()
            .Where(item => item.Slug == normalized && item.IsActive && !item.IsDeleted)
            .Select(item => ToDto(item))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<DirectoryProviderDto> UpsertDirectoryProviderAsync(UpsertDirectoryProviderRequest request, Guid actorUserId, CancellationToken cancellationToken)
    {
        var slug = NormalizeSlug(request.Slug ?? request.Name);
        var entity = await db.MilestoneDirectoryProviders.SingleOrDefaultAsync(item => item.Slug == slug, cancellationToken);
        if (entity is null)
        {
            entity = new MilestoneDirectoryProvider { Slug = slug, OwnerUserId = actorUserId, CreatedByUserId = actorUserId };
            db.MilestoneDirectoryProviders.Add(entity);
        }
        else
        {
            var ownerUserId = entity.OwnerUserId ?? entity.CreatedByUserId;
            if (ownerUserId is null)
            {
                throw new UnauthorizedAccessException("System provider profiles cannot be changed through provider onboarding.");
            }

            if (ownerUserId != actorUserId)
            {
                throw new UnauthorizedAccessException("This provider slug is already owned by another user.");
            }

            entity.OwnerUserId ??= actorUserId;
        }

        entity.Kind = RequireText(request.Kind, "Provider kind");
        entity.Category = RequireText(request.Category, "Category");
        entity.Name = RequireText(request.Name, "Provider name");
        entity.Parish = RequireText(request.Parish, "Parish");
        entity.BadgeLevel = RequireText(request.BadgeLevel, "Badge level");
        entity.Description = RequireText(request.Description, "Description");
        entity.AvailabilitySummary = RequireText(request.AvailabilitySummary, "Availability");
        entity.ContactMode = RequireText(request.ContactMode, "Contact mode");
        entity.IsActive = request.IsActive;
        entity.Rating = entity.Rating == 0 ? 4.8m : entity.Rating;
        entity.ReviewCount = entity.ReviewCount == 0 ? 12 : entity.ReviewCount;
        entity.UpdatedAt = timeProvider.GetUtcNow();
        entity.UpdatedByUserId = actorUserId;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<MessagingInboxDto> GetInboxAsync(Guid userId, CancellationToken cancellationToken)
    {
        await SeedMessagingAsync(userId, cancellationToken);
        var participants = await db.MilestoneConversationParticipants.AsNoTracking()
            .Where(item => item.UserId == userId && !item.IsDeleted)
            .ToListAsync(cancellationToken);
        var conversationIds = participants.Select(item => item.ConversationId).ToList();
        var conversations = await db.MilestoneConversations.AsNoTracking()
            .Where(item => conversationIds.Contains(item.Id) && !item.IsDeleted)
            .OrderByDescending(item => item.UpdatedAt)
            .ToListAsync(cancellationToken);
        var messages = await db.MilestoneMessages.AsNoTracking()
            .Where(item => conversationIds.Contains(item.ConversationId) && !item.IsDeleted)
            .OrderByDescending(item => item.SentAt)
            .ToListAsync(cancellationToken);
        var summaries = conversations.Select(conversation =>
        {
            var participant = participants.Single(item => item.ConversationId == conversation.Id);
            var lastMessage = messages.FirstOrDefault(item => item.ConversationId == conversation.Id);
            var unread = messages.Count(item => item.ConversationId == conversation.Id && item.SenderUserId != userId && (participant.LastReadAt is null || item.SentAt > participant.LastReadAt));
            var other = db.MilestoneConversationParticipants.AsNoTracking().FirstOrDefault(item => item.ConversationId == conversation.Id && item.UserId != userId);
            return new ConversationSummaryDto(
                conversation.Id,
                conversation.Subject,
                other?.DisplayName ?? "NestyStay Support",
                lastMessage?.Body ?? "No messages yet.",
                conversation.UpdatedAt,
                unread,
                conversation.IsSupportThread,
                other?.OnlineStatus ?? "Offline");
        }).ToList();
        return new MessagingInboxDto(userId, summaries);
    }

    public async Task<ConversationDto?> GetConversationAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken)
    {
        await RequireParticipantAsync(userId, conversationId, cancellationToken);
        var conversation = await db.MilestoneConversations.AsNoTracking().SingleOrDefaultAsync(item => item.Id == conversationId && !item.IsDeleted, cancellationToken);
        if (conversation is null) return null;
        var participants = await db.MilestoneConversationParticipants.AsNoTracking().Where(item => item.ConversationId == conversationId && !item.IsDeleted).Select(item => ToDto(item)).ToListAsync(cancellationToken);
        var messages = await db.MilestoneMessages.AsNoTracking().Where(item => item.ConversationId == conversationId && !item.IsDeleted).OrderBy(item => item.SentAt).Select(item => ToDto(item)).ToListAsync(cancellationToken);
        return new ConversationDto(conversation.Id, conversation.Subject, conversation.BookingId, conversation.IsSupportThread, participants, messages);
    }

    public async Task<ConversationDto> CreateConversationAsync(Guid userId, CreateConversationRequest request, CancellationToken cancellationToken)
    {
        if (!request.Participants.Any(item => item.UserId == userId))
        {
            throw new UnauthorizedAccessException("Conversation creator must be a participant.");
        }

        var conversation = new MilestoneConversation
        {
            Subject = RequireText(request.Subject, "Subject"),
            BookingId = request.BookingId,
            IsSupportThread = request.IsSupportThread,
            CreatedByUserId = userId,
            UpdatedByUserId = userId
        };
        db.MilestoneConversations.Add(conversation);
        foreach (var participant in request.Participants)
        {
            db.MilestoneConversationParticipants.Add(new MilestoneConversationParticipant
            {
                ConversationId = conversation.Id,
                UserId = participant.UserId,
                DisplayName = RequireText(participant.DisplayName, "Participant name"),
                Role = RequireText(participant.Role, "Participant role"),
                OnlineStatus = participant.UserId == userId ? "Mi Deh Yah" : "Offline",
                CreatedByUserId = userId,
                UpdatedByUserId = userId
            });
        }
        db.MilestoneMessages.Add(new MilestoneMessage
        {
            ConversationId = conversation.Id,
            SenderUserId = userId,
            Body = RequireText(request.InitialMessage, "Initial message"),
            SentAt = timeProvider.GetUtcNow(),
            Status = "Delivered",
            AttachmentsJson = "[]",
            CreatedByUserId = userId,
            UpdatedByUserId = userId
        });
        await db.SaveChangesAsync(cancellationToken);
        return (await GetConversationAsync(userId, conversation.Id, cancellationToken))!;
    }

    public async Task<AttachmentUploadDto> PrepareMessageAttachmentUploadAsync(Guid userId, Guid conversationId, PrepareMessageAttachmentUploadRequest request, CancellationToken cancellationToken)
    {
        await RequireParticipantAsync(userId, conversationId, cancellationToken);
        var safeFileName = ValidateAttachment(request.FileName, request.ContentType, request.SizeBytes);
        var extension = Path.GetExtension(safeFileName).ToLowerInvariant();
        var objectKey = $"messages/{conversationId:N}/{userId:N}/{Guid.NewGuid():N}{extension}";
        var uploadUrl = await storageProvider.CreateUploadUrlAsync(objectKey, cancellationToken);
        var now = timeProvider.GetUtcNow();

        var attachment = new MilestoneMessageAttachment
        {
            ConversationId = conversationId,
            OwnerUserId = userId,
            OriginalFileName = Path.GetFileName(request.FileName.Trim()),
            SafeFileName = safeFileName,
            ContentType = request.ContentType.Trim().ToLowerInvariant(),
            SizeBytes = request.SizeBytes,
            ObjectKey = objectKey,
            UploadUrl = uploadUrl,
            Status = "PendingUpload",
            UploadExpiresAt = now.Add(AttachmentUploadLifetime),
            CreatedByUserId = userId,
            UpdatedByUserId = userId
        };
        db.MilestoneMessageAttachments.Add(attachment);
        await db.SaveChangesAsync(cancellationToken);
        return ToUploadDto(attachment);
    }

    public async Task<AttachmentUploadDto> CompleteMessageAttachmentUploadAsync(Guid userId, Guid conversationId, Guid attachmentId, CancellationToken cancellationToken)
    {
        await RequireParticipantAsync(userId, conversationId, cancellationToken);
        var attachment = await RequireOwnedAttachmentAsync(userId, conversationId, attachmentId, cancellationToken);
        if (attachment.Status != "PendingUpload")
        {
            throw new InvalidOperationException("Attachment upload is not pending.");
        }

        var now = timeProvider.GetUtcNow();
        if (attachment.UploadExpiresAt <= now)
        {
            attachment.Status = "Expired";
            await db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Attachment upload URL has expired.");
        }

        attachment.Status = "Uploaded";
        attachment.UploadedAt = now;
        attachment.UpdatedAt = now;
        attachment.UpdatedByUserId = userId;
        await db.SaveChangesAsync(cancellationToken);
        return ToUploadDto(attachment);
    }

    public async Task<AttachmentDownloadDto> GetMessageAttachmentDownloadAsync(Guid userId, Guid conversationId, Guid attachmentId, CancellationToken cancellationToken)
    {
        await RequireParticipantAsync(userId, conversationId, cancellationToken);
        var attachment = await db.MilestoneMessageAttachments
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == attachmentId && item.ConversationId == conversationId && item.MessageId != null && !item.IsDeleted, cancellationToken)
            ?? throw new UnauthorizedAccessException("Attachment is not available to this conversation participant.");

        var expiresAt = timeProvider.GetUtcNow().Add(AttachmentDownloadLifetime);
        var url = await storageProvider.CreateDownloadUrlAsync(attachment.ObjectKey, expiresAt, cancellationToken);
        return new AttachmentDownloadDto(attachment.Id, attachment.SafeFileName, attachment.ContentType, attachment.SizeBytes, url, expiresAt);
    }

    public async Task<MessageDto> SendMessageAsync(Guid userId, Guid conversationId, SendMessageRequest request, CancellationToken cancellationToken)
    {
        await RequireParticipantAsync(userId, conversationId, cancellationToken);
        var message = new MilestoneMessage
        {
            ConversationId = conversationId,
            SenderUserId = userId,
            Body = RequireText(request.Body, "Message"),
            SentAt = timeProvider.GetUtcNow(),
            Status = "Delivered",
            CreatedByUserId = userId,
            UpdatedByUserId = userId
        };
        message.AttachmentsJson = MilestoneJson.Serialize(await AttachUploadedFilesAsync(userId, conversationId, message.Id, message.SentAt, request.Attachments, cancellationToken));
        db.MilestoneMessages.Add(message);
        var conversation = await db.MilestoneConversations.SingleAsync(item => item.Id == conversationId, cancellationToken);
        conversation.UpdatedAt = message.SentAt;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(message);
    }

    public async Task MarkConversationReadAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken)
    {
        var participant = await RequireParticipantAsync(userId, conversationId, cancellationToken);
        participant.LastReadAt = timeProvider.GetUtcNow();
        foreach (var message in await db.MilestoneMessages.Where(item => item.ConversationId == conversationId && item.SenderUserId != userId && item.ReadAt == null).ToListAsync(cancellationToken))
        {
            message.ReadAt = participant.LastReadAt;
            message.Status = "Read";
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<HostOperationsDto> GetHostOperationsAsync(Guid hostUserId, CancellationToken cancellationToken)
    {
        await SeedHostOperationsAsync(hostUserId, cancellationToken);
        var pricing = await db.MilestoneHostPricingRules.AsNoTracking().Where(item => item.HostUserId == hostUserId && !item.IsDeleted).OrderBy(item => item.StartsOn).Select(item => ToDto(item)).ToListAsync(cancellationToken);
        var promotions = await db.MilestoneHostPromotions.AsNoTracking().Where(item => item.HostUserId == hostUserId && !item.IsDeleted).OrderByDescending(item => item.CreatedAt).Select(item => ToDto(item)).ToListAsync(cancellationToken);
        var reviews = await db.MilestoneReviews.AsNoTracking().Where(item => item.PropertyId == SeedPropertyId && !item.IsDeleted).OrderByDescending(item => item.CreatedAt).Select(item => ToDto(item)).ToListAsync(cancellationToken);
        return new HostOperationsDto(hostUserId, BuildAnalytics(hostUserId), pricing, promotions, reviews);
    }

    public async Task<HostPricingRuleDto> SaveHostPricingRuleAsync(Guid hostUserId, SaveHostPricingRuleRequest request, CancellationToken cancellationToken)
    {
        ValidateDateRange(request.StartsOn, request.EndsOn);
        if (request.NightlyRate <= 0 || request.MinimumStay <= 0)
        {
            throw new InvalidOperationException("Nightly rate and minimum stay must be positive.");
        }

        var entity = new MilestoneHostPricingRule
        {
            HostUserId = hostUserId,
            PropertyId = request.PropertyId,
            Name = RequireText(request.Name, "Rule name"),
            StartsOn = request.StartsOn,
            EndsOn = request.EndsOn,
            NightlyRate = request.NightlyRate,
            MinimumStay = request.MinimumStay,
            IsActive = request.IsActive,
            CreatedByUserId = hostUserId,
            UpdatedByUserId = hostUserId
        };
        db.MilestoneHostPricingRules.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<HostPromotionDto> SaveHostPromotionAsync(Guid hostUserId, SaveHostPromotionRequest request, CancellationToken cancellationToken)
    {
        ValidateDateRange(request.StartsOn, request.EndsOn);
        if (request.DiscountPercent <= 0 || request.DiscountPercent > 80 || request.MinimumNights <= 0)
        {
            throw new InvalidOperationException("Promotion discount and minimum nights must be valid.");
        }

        var entity = new MilestoneHostPromotion
        {
            HostUserId = hostUserId,
            PropertyId = request.PropertyId,
            Name = RequireText(request.Name, "Promotion name"),
            DiscountPercent = request.DiscountPercent,
            StartsOn = request.StartsOn,
            EndsOn = request.EndsOn,
            MinimumNights = request.MinimumNights,
            BadgeLevel = RequireText(request.BadgeLevel, "Badge level"),
            IsActive = request.IsActive,
            CreatedByUserId = hostUserId,
            UpdatedByUserId = hostUserId
        };
        db.MilestoneHostPromotions.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<AdminOperationsDto> GetAdminOperationsAsync(CancellationToken cancellationToken)
    {
        await SeedAdminAsync(cancellationToken);
        var cases = await db.MilestoneAdminCases.AsNoTracking().Where(item => !item.IsDeleted).OrderByDescending(item => item.UpdatedAt).Select(item => ToDto(item)).ToListAsync(cancellationToken);
        var audits = await GetAuditEventsAsync(cancellationToken);
        var metrics = new List<AdminMetricDto>
        {
            new("Open cases", cases.Count(item => item.Status != "Resolved").ToString()),
            new("Fraud reviews", cases.Count(item => item.CaseType == "Fraud").ToString()),
            new("Property moderation", cases.Count(item => item.CaseType == "Property moderation").ToString()),
            new("Audit records", audits.Count.ToString())
        };
        return new AdminOperationsDto(cases, audits, metrics);
    }

    public async Task<AdminCaseDto> CreateAdminCaseAsync(CreateAdminCaseRequest request, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var entity = new MilestoneAdminCase
        {
            CaseType = RequireText(request.CaseType, "Case type"),
            SubjectType = RequireText(request.SubjectType, "Subject type"),
            SubjectId = request.SubjectId,
            Priority = string.IsNullOrWhiteSpace(request.Priority) ? "Normal" : request.Priority.Trim(),
            Reason = RequireText(request.Reason, "Reason"),
            AssignedTo = request.AssignedTo.Trim(),
            Status = "Open",
            CreatedByUserId = actorUserId,
            UpdatedByUserId = actorUserId
        };
        db.MilestoneAdminCases.Add(entity);
        await AddAuditAsync("AdminCaseCreated", entity.SubjectType, entity.SubjectId, entity.Reason, actorUserId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<AdminCaseDto> ResolveAdminCaseAsync(Guid caseId, ResolveAdminCaseRequest request, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var entity = await db.MilestoneAdminCases.SingleOrDefaultAsync(item => item.Id == caseId && !item.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Admin case not found.");
        entity.Status = string.IsNullOrWhiteSpace(request.Status) ? "Resolved" : request.Status.Trim();
        entity.ResolutionNotes = RequireText(request.ResolutionNotes, "Resolution notes");
        entity.ResolvedAt = timeProvider.GetUtcNow();
        entity.UpdatedAt = timeProvider.GetUtcNow();
        entity.UpdatedByUserId = actorUserId;
        await AddAuditAsync("AdminCaseResolved", entity.SubjectType, entity.SubjectId, entity.ResolutionNotes, actorUserId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<IReadOnlyList<AuditEventDto>> GetAuditEventsAsync(CancellationToken cancellationToken)
    {
        await SeedAdminAsync(cancellationToken);
        return await db.MilestoneAuditEvents.AsNoTracking()
            .Where(item => !item.IsDeleted)
            .OrderByDescending(item => item.CreatedAt)
            .Take(100)
            .Select(item => ToDto(item))
            .ToListAsync(cancellationToken);
    }

    public async Task<AuthFlowResultDto> StartAuthFlowAsync(StartAuthFlowRequest request, CancellationToken cancellationToken)
    {
        var flowType = NormalizeFlowType(request.FlowType);
        var destination = RequireText(request.Destination, "Destination");
        var deliveryChannel = ResolveDeliveryChannel(flowType, destination);
        var normalizedDestination = NormalizeDestination(destination, deliveryChannel);
        var requestIpHash = HashOpaque(string.IsNullOrWhiteSpace(request.RequestIp) ? "unknown" : request.RequestIp.Trim());
        var now = timeProvider.GetUtcNow();
        var cooldownThreshold = now.Subtract(AuthFlowResendCooldown);

        await EnforceAuthFlowRateLimitsAsync(request.UserId, normalizedDestination, requestIpHash, now, cancellationToken);

        var recentFlow = await db.MilestoneAuthFlows
            .Where(item =>
                !item.IsDeleted &&
                item.UserId == request.UserId &&
                item.FlowType == flowType &&
                item.NormalizedDestination == normalizedDestination &&
                item.Status == AuthStatusPending)
            .OrderByDescending(item => item.LastSentAt ?? item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (recentFlow is not null && (recentFlow.LastSentAt ?? recentFlow.CreatedAt) > cooldownThreshold)
        {
            await AddAuditAsync(
                "AuthFlowResendCooldown",
                "AuthFlow",
                recentFlow.Id,
                $"{flowType} request was throttled by resend cooldown.",
                request.UserId,
                cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Please wait before requesting another verification code.");
        }

        var pendingFlows = await db.MilestoneAuthFlows
            .Where(item =>
                !item.IsDeleted &&
                item.UserId == request.UserId &&
                item.FlowType == flowType &&
                item.NormalizedDestination == normalizedDestination &&
                item.Status == AuthStatusPending)
            .ToListAsync(cancellationToken);
        foreach (var pendingFlow in pendingFlows)
        {
            pendingFlow.Status = AuthStatusInvalidated;
            pendingFlow.InvalidatedAt = now;
            pendingFlow.UpdatedAt = now;
        }

        var code = GenerateSixDigitCode();
        var token = GenerateSecureToken();
        var salt = RandomNumberGenerator.GetBytes(16);
        var flow = new MilestoneAuthFlow
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            FlowType = flowType,
            Destination = NormalizeDisplayDestination(destination, deliveryChannel),
            NormalizedDestination = normalizedDestination,
            DestinationHash = HashOpaque(normalizedDestination),
            CodeHash = HashBoundSecret(flowType, request.UserId, normalizedDestination, code, salt),
            TokenHash = HashBoundSecret(flowType, request.UserId, normalizedDestination, token, salt),
            SecretSalt = Convert.ToBase64String(salt),
            Status = AuthStatusPending,
            DeliveryChannel = deliveryChannel,
            RequestIpHash = requestIpHash,
            ExpiresAt = now.Add(flowType.Equals("PasswordReset", StringComparison.OrdinalIgnoreCase)
                ? PasswordResetLifetime
                : VerificationCodeLifetime),
            LastSentAt = now
        };

        db.MilestoneAuthFlows.Add(flow);
        developmentAuthSecrets.Store(new DevelopmentAuthSecret(
            flow.Id,
            flow.Destination,
            flow.DeliveryChannel,
            code,
            token,
            flow.ExpiresAt,
            now));
        await SendAuthFlowAsync(flow, code, token, cancellationToken);
        await AddAuditAsync("AuthFlowStarted", "AuthFlow", flow.Id, $"{flowType} code delivered by {deliveryChannel}.", request.UserId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(flow);
    }

    public async Task<AuthFlowResultDto> CompleteAuthFlowAsync(CompleteAuthFlowRequest request, CancellationToken cancellationToken)
    {
        var flow = await db.MilestoneAuthFlows.SingleOrDefaultAsync(item => item.Id == request.FlowId && !item.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Verification flow not found.");
        var now = timeProvider.GetUtcNow();

        if (flow.Status != AuthStatusPending)
        {
            throw new InvalidOperationException(flow.Status switch
            {
                AuthStatusCompleted => "Verification code was already used.",
                AuthStatusExpired => "Verification link or code has expired.",
                AuthStatusInvalidated => "Verification code was replaced by a newer request.",
                AuthStatusFailed => "Verification flow is no longer valid.",
                _ => "Verification flow is no longer valid."
            });
        }

        if (flow.ExpiresAt < now)
        {
            flow.Status = AuthStatusExpired;
            flow.UpdatedAt = now;
            developmentAuthSecrets.Remove(flow.Id);
            await AddAuditAsync("AuthFlowExpired", "AuthFlow", flow.Id, $"{flow.FlowType} code expired before completion.", flow.UserId, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Verification link or code has expired.");
        }

        if (!VerifyBoundSecret(flow, request.Code))
        {
            flow.FailedAttempts++;
            flow.UpdatedAt = now;
            if (flow.FailedAttempts >= MaximumAuthFlowAttempts)
            {
                flow.Status = AuthStatusFailed;
                developmentAuthSecrets.Remove(flow.Id);
            }

            await AddAuditAsync("AuthFlowFailed", "AuthFlow", flow.Id, $"{flow.FlowType} code verification failed.", flow.UserId, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Verification code is invalid.");
        }

        flow.Status = AuthStatusCompleted;
        flow.CompletedAt = now;
        flow.UpdatedAt = now;
        developmentAuthSecrets.Remove(flow.Id);
        await AddAuditAsync("AuthFlowCompleted", "AuthFlow", flow.Id, $"{flow.FlowType} code completed.", flow.UserId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(flow);
    }

    public async Task<DevelopmentAuthFlowSecretDto?> GetDevelopmentAuthFlowSecretAsync(Guid flowId, CancellationToken cancellationToken)
    {
        var flow = await db.MilestoneAuthFlows.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == flowId && !item.IsDeleted, cancellationToken);
        if (flow is null || flow.Status != AuthStatusPending || flow.ExpiresAt < timeProvider.GetUtcNow())
        {
            return null;
        }

        var secret = developmentAuthSecrets.Get(flowId);
        if (secret is null || secret.ExpiresAt < timeProvider.GetUtcNow())
        {
            return null;
        }

        return new DevelopmentAuthFlowSecretDto(flow.Id, secret.Code, secret.Token, flow.ExpiresAt);
    }

    public async Task<IReadOnlyList<RecoveryCodeDto>> GenerateRecoveryCodesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var existing = await db.MilestoneRecoveryCodes.Where(item => item.UserId == userId).ToListAsync(cancellationToken);
        db.MilestoneRecoveryCodes.RemoveRange(existing);
        var codes = Enumerable.Range(0, 8).Select(_ => GenerateRecoveryCode()).ToList();
        foreach (var code in codes)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            db.MilestoneRecoveryCodes.Add(new MilestoneRecoveryCode
            {
                UserId = userId,
                CodeHash = HashBoundSecret("RecoveryCode", userId, userId.ToString("N"), code, salt),
                SecretSalt = Convert.ToBase64String(salt),
                CreatedByUserId = userId
            });
        }
        await AddAuditAsync("RecoveryCodesRegenerated", "RecoveryCode", null, "Recovery codes regenerated and previous codes invalidated.", userId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return codes.Select(code => new RecoveryCodeDto(code, false)).ToList();
    }

    public Task<SocialAuthConfigDto> GetSocialAuthConfigAsync(CancellationToken cancellationToken)
    {
        var google = googleIdentityValidator.IsConfigured;
        return Task.FromResult(new SocialAuthConfigDto(google, false, false, [
            "GOOGLE_AUTH_CLIENT_ID"
        ]));
    }

    private async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (!await db.MilestonePublicContentPages.AnyAsync(cancellationToken))
        {
            db.MilestonePublicContentPages.AddRange(
                Page("about", "About NestyStay", "Public", "Jamaica's trusted stays platform.", "NestyStay brings verified stays, secure payments, identity checks, and local accountability into one island-first marketplace.", ["Local teams", "Accountability", "Verification", "Island community"], ["/trust", "/contact"]),
                Page("trust", "Trust and Safety", "Public", "Verified hosts, secure payments, and clear support.", "Trust flows through eKYC, host badges, payment authorization, 119 emergency guidance, and platform-mediated messaging.", ["Verified Hosts", "Identity Verification", "Secure Payments", "Trusted Community"], ["/terms", "/privacy"]),
                Page("help", "Help Center", "Public", "Searchable help for guests, hosts, payments, and verification.", "Welcome back, Bredda / Sistren. NestyStay support targets responses within one business hour.", ["Booking help", "Hosting help", "Payments", "Verification"], ["/help/booking", "/help/hosting", "/help/payments"]),
                Page("help/booking", "Booking Help", "Help", "Booking requests, pending verification, approval, and date holds.", "Bookings may be approved instantly or held while guest verification runs. Payment capture is blocked until approval.", ["Pending bookings", "Date holds", "Payment capture"], ["/explore"]),
                Page("help/hosting", "Hosting Help", "Help", "Property setup, badges, pricing, and reviews.", "Hosts manage listings, pricing, availability, verification toggles, badges, promotions, and guest communication.", ["Listing setup", "Badges", "Reviews"], ["/host-dashboard"]),
                Page("help/payments", "Payment Help", "Help", "Stripe authorization, receipts, refunds, and invoices.", "Stripe-style manual capture protects guests and hosts while approval and verification complete.", ["Authorization", "Capture", "Refunds"], ["/traveler/invoices"]),
                Page("contact", "Contact NestyStay", "Public", "Reach the team by form or WhatsApp.", "Use the contact form for support, partnerships, property onboarding, or urgent account questions. WhatsApp: 754-248-2435.", ["Support", "Partnerships", "Host onboarding"], ["https://wa.me/17542482435"]),
                Page("terms", "Terms of Service", "Legal", "State of Florida governing law with Jamaica addendum.", "The terms cover scope, eligibility, host and guest obligations, officer privacy, liability, privacy, governing law, and the Jamaica addendum.", ["Scope", "Eligibility", "Officer terms", "Governing law"], ["/privacy"]),
                Page("privacy", "Privacy Policy", "Legal", "GDPR, CCPA, retention, and zero-sale data posture.", "Booking records are retained for seven years. Officer ID history follows zero-linkage annual reset rules. NestyStay does not sell personal data.", ["Data retention", "Data rights", "Officer privacy"], ["/terms"]),
                Page("maintenance", "Maintenance", "Error", "We are preparing the next welcome.", "NestyStay is temporarily under maintenance. Bookings are preserved and support remains available by WhatsApp.", ["Bookings preserved", "Support available"], ["/contact"]));
        }

        if (!await db.MilestoneExperiences.AnyAsync(cancellationToken))
        {
            db.MilestoneExperiences.AddRange(
                Experience("blue-lagoon-wellness-swim", "Blue Lagoon Wellness Swim", "Wellness", "Portland", "Blue Lagoon Guides", 85m, 150, "Di Riddim Right - a guided mineral-water swim with local wellness context."),
                Experience("kingston-food-culture-walk", "Kingston Food & Culture Walk", "Food", "Kingston", "Marcia's Kitchen", 72m, 180, "Taste patties, ital plates, fresh juice, and sound-system history with a verified provider."),
                Experience("negril-sunset-water-tour", "Negril Sunset Water Tour", "Water", "Negril", "Island Ride Co.", 120m, 210, "A late-afternoon reef and sunset ride with clear cancellation rules and platform messaging."));
        }

        if (!await db.MilestoneJournalArticles.AnyAsync(cancellationToken))
        {
            db.MilestoneJournalArticles.AddRange(
                Article("why-jamaica-needs-trusted-stays", "Why Jamaica Needs Trusted Stays", "Platform", "NestyStay Team", "How identity, local accountability, and fair host payouts create better island travel."),
                Article("host-badges-explained", "Host Badges Explained", "Hosting", "NestyStay Trust", "Free, Verified, Trusted, and Wellness tiers with clear unlock paths and pricing."),
                Article("booking-with-confidence", "Booking With Confidence", "Traveler", "NestyStay Support", "What guests see from quote to approval, payment capture, invoice, and receipt."));
        }

        if (!await db.MilestoneHostProfiles.AnyAsync(cancellationToken))
        {
            db.MilestoneHostProfiles.Add(new MilestoneHostProfile
            {
                HostUserId = SeedHostUserId,
                Slug = "island-villa-hosting",
                DisplayName = "Island Villa Hosting",
                Parish = "St. Ann",
                Bio = "Verified Jamaican host team managing guest-ready stays near Ocho Rios with platform messaging only.",
                ResponseTime = "Replies in 10 minutes",
                BadgesJson = MilestoneJson.Serialize<IReadOnlyList<BadgeLevel>>([BadgeLevel.Verified, BadgeLevel.Trusted]),
                ListingIdsJson = MilestoneJson.Serialize<IReadOnlyList<Guid>>([SeedPropertyId]),
                Rating = 4.96m,
                ReviewCount = 38,
                HighlightsJson = MilestoneJson.Serialize<IReadOnlyList<string>>(["Verified host", "Trusted badge", "119 listing guidance", "97% host payout"])
            });
        }

        if (!await db.MilestoneDirectoryProviders.AnyAsync(cancellationToken))
        {
            db.MilestoneDirectoryProviders.AddRange(
                Provider("spark-cleaning-team", "Custodian", "Cleaning", "Spark Cleaning Team", "St. James", "Verified", "Turnover cleaning, laundry, and restock support.", "Mon-Sat 8 AM-6 PM"),
                Provider("island-spark-electric", "Trades", "Electrician", "Island Spark Electric", "Kingston", "Trusted", "Licensed electrical service with EITA cross-reference.", "Emergency calls through platform"),
                Provider("blue-lagoon-tours", "LocalBusiness", "Tours", "Blue Lagoon Tours", "Portland", "Trusted", "Water and wellness tours with guest-facing offers.", "Daily 9 AM-5 PM"),
                Provider("marcias-kitchen", "LocalBusiness", "Restaurant", "Marcia's Kitchen", "Kingston", "Trusted", "Local food partner for verified guests and hosts.", "Daily 8 AM-9 PM"),
                Provider("guest-verification-upsell", "Verification", "Guest verification", "Guest Verification Upsell", "All Jamaica", "Verified", "NEVER automatic. Host pays $0.14 per booking, guest pays nothing.", "Available per property"));
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedTravelerAsync(Guid userId, CancellationToken cancellationToken)
    {
        await SeedAsync(cancellationToken);
        if (!await db.MilestoneWishlistCollections.AnyAsync(item => item.UserId == userId && !item.IsDeleted, cancellationToken))
        {
            var collection = new MilestoneWishlistCollection { UserId = userId, Name = "Honeymoon Ideas", SortOrder = 1, CreatedByUserId = userId };
            db.MilestoneWishlistCollections.Add(collection);
            db.MilestoneWishlistItems.Add(new MilestoneWishlistItem { UserId = userId, CollectionId = collection.Id, PropertyId = SeedPropertyId, PropertyTitle = "Ocho Rios Verified Villa", Status = "Available", SortOrder = 1, CreatedByUserId = userId });
        }
        if (!await db.MilestoneTravelerNotifications.AnyAsync(item => item.UserId == userId, cancellationToken))
        {
            db.MilestoneTravelerNotifications.AddRange(
                Notification(userId, "Booking", "Bless Up - booking approved", "Your booking approval notification is ready.", "/traveler/reservations"),
                Notification(userId, "Payments", "Receipt available", "Payment receipt can be downloaded from traveler payments.", "/traveler/payments"),
                Notification(userId, "Messages", "Host replied", "Island Villa Hosting sent a platform message.", "/messages"));
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedMessagingAsync(Guid userId, CancellationToken cancellationToken)
    {
        await SeedTravelerAsync(userId, cancellationToken);
        if (await db.MilestoneConversationParticipants.AnyAsync(item => item.UserId == userId, cancellationToken))
        {
            return;
        }

        var conversation = new MilestoneConversation { Subject = "NestyStay support thread", IsSupportThread = true, CreatedByUserId = userId, UpdatedByUserId = userId };
        db.MilestoneConversations.Add(conversation);
        db.MilestoneConversationParticipants.AddRange(
            new MilestoneConversationParticipant { ConversationId = conversation.Id, UserId = userId, DisplayName = "Nesty Guest", Role = "Guest", OnlineStatus = "Mi Deh Yah", CreatedByUserId = userId },
            new MilestoneConversationParticipant { ConversationId = conversation.Id, UserId = SeedHostUserId, DisplayName = "NestyStay Support", Role = "Support", OnlineStatus = "Online" });
        db.MilestoneMessages.Add(new MilestoneMessage
        {
            ConversationId = conversation.Id,
            SenderUserId = SeedHostUserId,
            Body = "Welcome to NestyStay support. Verification confirmations, booking status, and payment notices stay here.",
            SentAt = timeProvider.GetUtcNow(),
            Status = "Delivered",
            AttachmentsJson = "[]"
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedHostOperationsAsync(Guid hostUserId, CancellationToken cancellationToken)
    {
        await SeedAsync(cancellationToken);
        if (!await db.MilestoneHostPricingRules.AnyAsync(item => item.HostUserId == hostUserId, cancellationToken))
        {
            db.MilestoneHostPricingRules.AddRange(
                new MilestoneHostPricingRule { HostUserId = hostUserId, PropertyId = SeedPropertyId, Name = "Independence Week", StartsOn = new DateOnly(2026, 8, 1), EndsOn = new DateOnly(2026, 8, 7), NightlyRate = 240m, MinimumStay = 3, IsActive = true, CreatedByUserId = hostUserId },
                new MilestoneHostPricingRule { HostUserId = hostUserId, PropertyId = SeedPropertyId, Name = "Christmas and New Year", StartsOn = new DateOnly(2026, 12, 20), EndsOn = new DateOnly(2027, 1, 5), NightlyRate = 295m, MinimumStay = 5, IsActive = true, CreatedByUserId = hostUserId });
        }
        if (!await db.MilestoneHostPromotions.AnyAsync(item => item.HostUserId == hostUserId, cancellationToken))
        {
            db.MilestoneHostPromotions.Add(new MilestoneHostPromotion { HostUserId = hostUserId, PropertyId = SeedPropertyId, Name = "Trusted host last-minute", DiscountPercent = 12m, StartsOn = new DateOnly(2026, 7, 22), EndsOn = new DateOnly(2026, 8, 31), MinimumNights = 2, BadgeLevel = "Trusted", IsActive = true, CreatedByUserId = hostUserId });
        }
        if (!await db.MilestoneReviews.AnyAsync(item => item.PropertyId == SeedPropertyId, cancellationToken))
        {
            db.MilestoneReviews.Add(new MilestoneReview { UserId = SeedGuestUserId, PropertyId = SeedPropertyId, SubjectTitle = "Ocho Rios Verified Villa", Rating = 5, Text = "Verified stay, clear check-in, and fast host replies.", Status = "Published", EditableUntil = timeProvider.GetUtcNow().AddHours(-1) });
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAdminAsync(CancellationToken cancellationToken)
    {
        await SeedAsync(cancellationToken);
        if (!await db.MilestoneAdminCases.AnyAsync(cancellationToken))
        {
            db.MilestoneAdminCases.AddRange(
                Case("User management", "User", SeedGuestUserId, "Normal", "Identity verification status review required.", "Trust team"),
                Case("Property moderation", "Property", SeedPropertyId, "High", "Flagged listing photo needs moderation.", "Ops"),
                Case("Dispute", "Booking", null, "High", "Guest refund evidence uploaded.", "Support"),
                Case("Fraud", "User", SeedGuestUserId, "Urgent", "Risk score exceeded manual review threshold.", "Fraud desk"));
            await AddAuditAsync("AdminSeedCreated", "AdminOperations", null, "Seeded admin operation queues.", null, cancellationToken);
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private static MilestonePublicContentPage Page(string slug, string title, string kind, string summary, string body, IReadOnlyList<string> sections, IReadOnlyList<string> links) => new()
    {
        Slug = slug,
        Title = title,
        Kind = kind,
        Summary = summary,
        Body = body,
        SectionsJson = MilestoneJson.Serialize(sections),
        LinksJson = MilestoneJson.Serialize(links)
    };

    private static MilestoneExperience Experience(string slug, string name, string category, string parish, string provider, decimal price, int minutes, string summary) => new()
    {
        Slug = slug,
        Name = name,
        Category = category,
        Parish = parish,
        ProviderName = provider,
        Price = price,
        DurationMinutes = minutes,
        Rating = 4.9m,
        Summary = summary,
        Description = $"{summary} Availability, pricing, guest selection, reviews, rules, and cancellation terms are all managed inside NestyStay.",
        ImagesJson = MilestoneJson.Serialize<IReadOnlyList<string>>(["/assets/stays/jamaica-seaview-villa.png", "/assets/stays/jamaica-beach-cottage.png"]),
        IncludedJson = MilestoneJson.Serialize<IReadOnlyList<string>>(["Verified provider", "Platform messaging", "Cancellation terms", "Guest support"]),
        RulesJson = MilestoneJson.Serialize<IReadOnlyList<string>>(["Arrive 10 minutes early", "Provider messages stay in platform", "Weather reschedule allowed"]),
        AvailabilityJson = MilestoneJson.Serialize<IReadOnlyList<string>>(["Mon", "Wed", "Fri", "Sat"])
    };

    private static MilestoneJournalArticle Article(string slug, string title, string category, string author, string summary) => new()
    {
        Slug = slug,
        Title = title,
        Category = category,
        Author = author,
        PublishedAt = DateTimeOffset.Parse("2026-06-15T12:00:00+00:00"),
        Summary = summary,
        Body = $"{summary}\n\nThis article is stored in PostgreSQL-backed milestone content and appears through the journal routes.",
        TagsJson = MilestoneJson.Serialize<IReadOnlyList<string>>([category, "Jamaica", "NestyStay"]),
        RelatedSlugsJson = MilestoneJson.Serialize<IReadOnlyList<string>>(["host-badges-explained", "booking-with-confidence"])
    };

    private static MilestoneDirectoryProvider Provider(string slug, string kind, string category, string name, string parish, string badgeLevel, string description, string availability) => new()
    {
        Slug = slug,
        Kind = kind,
        Category = category,
        Name = name,
        Parish = parish,
        BadgeLevel = badgeLevel,
        Description = description,
        AvailabilitySummary = availability,
        ContactMode = "Platform messaging only",
        Rating = 4.8m,
        ReviewCount = 18
    };

    private MilestoneTravelerNotification Notification(Guid userId, string type, string title, string body, string link) => new()
    {
        UserId = userId,
        Type = type,
        Title = title,
        Body = body,
        DeepLink = link,
        CreatedAt = timeProvider.GetUtcNow()
    };

    private MilestoneAdminCase Case(string type, string subject, Guid? subjectId, string priority, string reason, string assignedTo) => new()
    {
        CaseType = type,
        SubjectType = subject,
        SubjectId = subjectId,
        Priority = priority,
        Reason = reason,
        AssignedTo = assignedTo,
        Status = "Open"
    };

    private async Task<IReadOnlyList<WishlistCollectionDto>> GetWishlistCollectionsNoSeedAsync(Guid userId, CancellationToken cancellationToken)
    {
        var collections = await db.MilestoneWishlistCollections.AsNoTracking().Where(item => item.UserId == userId && !item.IsDeleted).OrderBy(item => item.SortOrder).ToListAsync(cancellationToken);
        var collectionIds = collections.Select(item => item.Id).ToList();
        var items = await db.MilestoneWishlistItems.AsNoTracking().Where(item => collectionIds.Contains(item.CollectionId) && !item.IsDeleted).OrderBy(item => item.SortOrder).Select(item => ToDto(item)).ToListAsync(cancellationToken);
        return collections.Select(collection => new WishlistCollectionDto(collection.Id, collection.UserId, collection.Name, collection.SortOrder, items.Where(item => item.CollectionId == collection.Id).ToList())).ToList();
    }

    private async Task<MilestoneWishlistCollection> FindCollectionAsync(Guid userId, Guid collectionId, CancellationToken cancellationToken) =>
        await db.MilestoneWishlistCollections.SingleOrDefaultAsync(item => item.Id == collectionId && item.UserId == userId && !item.IsDeleted, cancellationToken)
        ?? throw new UnauthorizedAccessException("Wishlist collection was not found for this traveler.");

    private async Task<MilestoneTravelerPaymentMethod> FindPaymentMethodAsync(Guid userId, Guid id, CancellationToken cancellationToken) =>
        await db.MilestoneTravelerPaymentMethods.SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId && !item.IsDeleted, cancellationToken)
        ?? throw new UnauthorizedAccessException("Payment method was not found for this traveler.");

    private async Task EnsureHostListingIdsBelongToHostAsync(Guid hostUserId, IReadOnlyList<Guid>? listingIds, CancellationToken cancellationToken)
    {
        if (listingIds is null || listingIds.Count == 0)
        {
            return;
        }

        var distinctIds = listingIds.Distinct().ToArray();
        var ownedCount = await db.MilestoneProperties.CountAsync(item => distinctIds.Contains(item.Id) && item.HostUserId == hostUserId && !item.IsDeleted, cancellationToken);
        if (ownedCount != distinctIds.Length)
        {
            throw new UnauthorizedAccessException("Host profile listings must belong to the signed-in host.");
        }
    }

    private async Task ClearDefaultPaymentMethodsAsync(Guid userId, CancellationToken cancellationToken)
    {
        foreach (var method in await db.MilestoneTravelerPaymentMethods.Where(item => item.UserId == userId && item.IsDefault).ToListAsync(cancellationToken))
        {
            method.IsDefault = false;
        }
    }

    private async Task<MilestoneConversationParticipant> RequireParticipantAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken) =>
        await db.MilestoneConversationParticipants.SingleOrDefaultAsync(item => item.ConversationId == conversationId && item.UserId == userId && !item.IsDeleted, cancellationToken)
        ?? throw new UnauthorizedAccessException("Conversation is not available to this user.");

    private async Task<MilestoneMessageAttachment> RequireOwnedAttachmentAsync(Guid userId, Guid conversationId, Guid attachmentId, CancellationToken cancellationToken) =>
        await db.MilestoneMessageAttachments.SingleOrDefaultAsync(item => item.Id == attachmentId && item.ConversationId == conversationId && item.OwnerUserId == userId && !item.IsDeleted, cancellationToken)
        ?? throw new UnauthorizedAccessException("Attachment is not available to this user.");

    private async Task<IReadOnlyList<MessageAttachmentDto>> AttachUploadedFilesAsync(
        Guid userId,
        Guid conversationId,
        Guid messageId,
        DateTimeOffset attachedAt,
        IReadOnlyList<MessageAttachmentDto>? attachments,
        CancellationToken cancellationToken)
    {
        if (attachments is null || attachments.Count == 0)
        {
            return [];
        }

        if (attachments.Any(item => item.AttachmentId is null))
        {
            throw new InvalidOperationException("Message attachments must be prepared and uploaded before sending.");
        }

        var ids = attachments.Select(item => item.AttachmentId!.Value).Distinct().ToArray();
        var persisted = await db.MilestoneMessageAttachments
            .Where(item => ids.Contains(item.Id) && item.ConversationId == conversationId && item.OwnerUserId == userId && !item.IsDeleted)
            .ToListAsync(cancellationToken);
        if (persisted.Count != ids.Length)
        {
            throw new UnauthorizedAccessException("One or more attachments are not available to this user.");
        }

        foreach (var attachment in persisted)
        {
            if (attachment.Status != "Uploaded")
            {
                throw new InvalidOperationException("Message attachments must finish uploading before sending.");
            }

            attachment.MessageId = messageId;
            attachment.Status = "Attached";
            attachment.AttachedAt = attachedAt;
            attachment.UpdatedAt = attachedAt;
            attachment.UpdatedByUserId = userId;
        }

        var byId = persisted.ToDictionary(item => item.Id);
        return ids.Select(id =>
        {
            var attachment = byId[id];
            return new MessageAttachmentDto(
                attachment.Id,
                attachment.SafeFileName,
                attachment.ContentType,
                attachment.SizeBytes,
                $"/api/spec/messages/conversations/{conversationId}/attachments/{attachment.Id}/download",
                "Attached",
                attachment.ObjectKey,
                attachedAt.Add(AttachmentDownloadLifetime));
        }).ToList();
    }

    private static string ValidateAttachment(string fileName, string contentType, long sizeBytes)
    {
        if (sizeBytes <= 0 || sizeBytes > MaximumAttachmentBytes)
        {
            throw new InvalidOperationException("Attachments must be 10 MB or smaller.");
        }

        var normalizedContentType = contentType.Trim().ToLowerInvariant();
        if (!AllowedAttachmentExtensions.TryGetValue(normalizedContentType, out var allowedExtensions))
        {
            throw new InvalidOperationException("Attachment type is not allowed.");
        }

        var originalFileName = Path.GetFileName(fileName.Trim());
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Attachment extension does not match the content type.");
        }

        var stem = Path.GetFileNameWithoutExtension(originalFileName).Trim().ToLowerInvariant();
        var safeStem = new string(stem.Select(character => IsSafeFileNameCharacter(character) ? character : '-').ToArray());
        safeStem = string.Join("-", safeStem.Split('-', StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(safeStem))
        {
            safeStem = "attachment";
        }

        if (safeStem.Length > 80)
        {
            safeStem = safeStem[..80];
        }

        return $"{safeStem}{extension}";
    }

    private static bool IsSafeFileNameCharacter(char character) =>
        (character >= 'a' && character <= 'z') || (character >= '0' && character <= '9') || character == '-';

    private async Task EnsureReviewBelongsToHostAsync(Guid hostUserId, MilestoneReview review, CancellationToken cancellationToken)
    {
        if (review.PropertyId is { } propertyId &&
            await db.MilestoneProperties.AnyAsync(item => item.Id == propertyId && item.HostUserId == hostUserId && !item.IsDeleted, cancellationToken))
        {
            return;
        }

        if (review.BookingId is { } bookingId &&
            await db.MilestoneBookings.AnyAsync(item => item.Id == bookingId && item.HostUserId == hostUserId && !item.IsDeleted, cancellationToken))
        {
            return;
        }

        throw new UnauthorizedAccessException("Review is not available to this host.");
    }

    private HostAnalyticsDto BuildAnalytics(Guid hostUserId) => new(
        24850m,
        82m,
        185m,
        db.MilestoneBookings.Count(item => item.HostUserId == hostUserId),
        11.4m,
        [new("30d", 6200m), new("60d", 8100m), new("90d", 10550m)],
        [new("30d", 74m), new("60d", 81m), new("90d", 82m)]);

    private async Task AddAuditAsync(string action, string subjectType, Guid? subjectId, string reason, Guid? actorUserId, CancellationToken cancellationToken)
    {
        db.MilestoneAuditEvents.Add(new MilestoneAuditEvent
        {
            ActorUserId = actorUserId,
            ActorRole = actorUserId is null ? "System" : "User",
            Action = action,
            SubjectType = subjectType,
            SubjectId = subjectId,
            Reason = reason,
            CreatedAt = timeProvider.GetUtcNow()
        });
        await Task.CompletedTask;
    }

    private async Task EnforceAuthFlowRateLimitsAsync(
        Guid? userId,
        string normalizedDestination,
        string requestIpHash,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var windowStart = now.Subtract(AuthFlowRateLimitWindow);
        var accountFlowCount = await db.MilestoneAuthFlows.CountAsync(
            item =>
                !item.IsDeleted &&
                item.CreatedAt >= windowStart &&
                (item.NormalizedDestination == normalizedDestination || (userId != null && item.UserId == userId)),
            cancellationToken);
        if (accountFlowCount >= MaximumAccountAuthFlowsPerWindow)
        {
            await AddAuditAsync(
                "AuthFlowAccountRateLimited",
                "AuthFlow",
                null,
                "Verification request was blocked by account or destination rate limit.",
                userId,
                cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Too many verification requests. Try again later.");
        }

        var ipFlowCount = await db.MilestoneAuthFlows.CountAsync(
            item =>
                !item.IsDeleted &&
                item.CreatedAt >= windowStart &&
                item.RequestIpHash == requestIpHash,
            cancellationToken);
        if (ipFlowCount >= MaximumIpAuthFlowsPerWindow)
        {
            await AddAuditAsync(
                "AuthFlowIpRateLimited",
                "AuthFlow",
                null,
                "Verification request was blocked by IP rate limit.",
                userId,
                cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Too many verification requests from this network. Try again later.");
        }
    }

    private async Task SendAuthFlowAsync(MilestoneAuthFlow flow, string code, string token, CancellationToken cancellationToken)
    {
        var purpose = flow.FlowType switch
        {
            "PasswordReset" => "password reset",
            "PhoneVerification" => "phone verification",
            "OneTimePasscode" => "one-time passcode",
            "TwoFactorSetup" => "authenticator setup",
            _ => "email verification"
        };

        if (flow.DeliveryChannel == "Sms")
        {
            await smsSender.SendAsync(
                new SmsMessage(
                    flow.Destination,
                    $"Your NestyStay {purpose} code is {code}. It expires at {flow.ExpiresAt:O}.",
                    flow.Id),
                cancellationToken);
            return;
        }

        var body = flow.FlowType == "PasswordReset"
            ? $"Use this NestyStay reset code: {code}. Reset token: {token}. It expires at {flow.ExpiresAt:O}."
            : $"Use this NestyStay {purpose} code: {code}. It expires at {flow.ExpiresAt:O}.";
        await emailSender.SendAsync(
            new EmailMessage(flow.Destination, $"NestyStay {purpose} code", body, flow.Id),
            cancellationToken);
    }

    private static string NormalizeFlowType(string flowType)
    {
        var normalized = RequireText(flowType, "Flow type")
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal);
        return normalized.ToLowerInvariant() switch
        {
            "email" or "emailverification" => "EmailVerification",
            "phone" or "phoneverification" => "PhoneVerification",
            "otp" or "onetimepasscode" => "OneTimePasscode",
            "forgot" or "reset" or "passwordreset" or "resetpassword" => "PasswordReset",
            "twofa" or "totp" or "twofactorsetup" => "TwoFactorSetup",
            _ => throw new InvalidOperationException("Unsupported authentication flow type.")
        };
    }

    private static string ResolveDeliveryChannel(string flowType, string destination)
    {
        if (flowType == "PhoneVerification" ||
            (flowType == "OneTimePasscode" && !destination.Contains('@', StringComparison.Ordinal)))
        {
            return "Sms";
        }

        return "Email";
    }

    private static string NormalizeDestination(string destination, string deliveryChannel)
    {
        if (deliveryChannel == "Sms")
        {
            var trimmed = RequireText(destination, "Destination");
            var normalized = new string(trimmed.Where(character => char.IsDigit(character) || character == '+').ToArray());
            if (normalized.Length < 8)
            {
                throw new InvalidOperationException("A valid phone number is required.");
            }

            return normalized;
        }

        try
        {
            var address = new MailAddress(RequireText(destination, "Destination"));
            return address.Address.ToLowerInvariant();
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("A valid email address is required.");
        }
    }

    private static string NormalizeDisplayDestination(string destination, string deliveryChannel) =>
        deliveryChannel == "Sms"
            ? NormalizeDestination(destination, deliveryChannel)
            : new MailAddress(RequireText(destination, "Destination")).Address.ToLowerInvariant();

    private static bool VerifyBoundSecret(MilestoneAuthFlow flow, string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        var salt = Convert.FromBase64String(flow.SecretSalt);
        var actualHash = Convert.FromBase64String(HashBoundSecret(flow.FlowType, flow.UserId, flow.NormalizedDestination, secret.Trim(), salt));
        var expectedHash = Convert.FromBase64String(flow.CodeHash);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    private static string HashBoundSecret(string purpose, Guid? userId, string destination, string secret, byte[] salt)
    {
        var binding = $"{purpose}|{userId?.ToString("N") ?? "anonymous"}|{destination}|{secret}";
        using var hmac = new HMACSHA256(salt);
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(binding)));
    }

    private static string HashOpaque(string value) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim().ToLowerInvariant())));

    private static string GenerateSixDigitCode() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    private static string GenerateSecureToken() =>
        Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    private static string GenerateRecoveryCode()
    {
        var bytes = RandomNumberGenerator.GetBytes(8);
        return $"{Convert.ToHexString(bytes[..4])}-{Convert.ToHexString(bytes[4..])}";
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string NormalizeSlug(string value) =>
        string.Join("-", value.Trim().ToLowerInvariant().Split(Path.GetInvalidFileNameChars().Concat([' ', '_', '/', '\\']).ToArray(), StringSplitOptions.RemoveEmptyEntries));

    private static string NormalizeContentSlug(string value) =>
        string.Join("/", value
            .Trim()
            .ToLowerInvariant()
            .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries)
            .Select(NormalizeSlug));

    private static string RequireText(string value, string label) =>
        string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException($"{label} is required.") : value.Trim();

    private static void ValidateDateRange(DateOnly starts, DateOnly ends)
    {
        if (ends < starts)
        {
            throw new InvalidOperationException("End date must be after start date.");
        }
    }

    private static PublicContentPageDto ToDto(MilestonePublicContentPage page) => new(page.Slug, page.Title, page.Kind, page.Summary, page.Body, MilestoneJson.DeserializeList<string>(page.SectionsJson), MilestoneJson.DeserializeList<string>(page.LinksJson));
    private static ContactRequestDto ToDto(MilestoneContactRequest request) => new(request.Id, request.Name, request.Email, request.Subject, request.Message, request.Status, request.CreatedAt);
    private static ExperienceDto ToDto(MilestoneExperience item) => new(item.Id, item.Slug, item.Name, item.Category, item.Parish, item.ProviderName, item.Price, item.Currency, item.DurationMinutes, item.Rating, item.Summary, item.Description, MilestoneJson.DeserializeList<string>(item.ImagesJson), MilestoneJson.DeserializeList<string>(item.IncludedJson), MilestoneJson.DeserializeList<string>(item.RulesJson), MilestoneJson.DeserializeList<string>(item.AvailabilityJson));
    private static JournalArticleDto ToDto(MilestoneJournalArticle item) => new(item.Id, item.Slug, item.Title, item.Category, item.Author, item.PublishedAt, item.Summary, item.Body, MilestoneJson.DeserializeList<string>(item.TagsJson), MilestoneJson.DeserializeList<string>(item.RelatedSlugsJson));
    private static HostProfileDto ToDto(MilestoneHostProfile item) => new(item.Id, item.HostUserId, item.Slug, item.DisplayName, item.Parish, item.Bio, item.ResponseTime, MilestoneJson.DeserializeList<BadgeLevel>(item.BadgesJson), MilestoneJson.DeserializeList<Guid>(item.ListingIdsJson), item.Rating, item.ReviewCount, item.IsPublic, MilestoneJson.DeserializeList<string>(item.HighlightsJson));
    private static WishlistItemDto ToDto(MilestoneWishlistItem item) => new(item.Id, item.CollectionId, item.UserId, item.PropertyId, item.PropertyTitle, item.Status, item.SortOrder, item.CreatedAt);
    private static PaymentMethodDto ToDto(MilestoneTravelerPaymentMethod item) => new(item.Id, item.UserId, item.Brand, item.Last4, item.ExpMonth, item.ExpYear, item.IsDefault, item.CreatedAt);
    private static ReviewDto ToDto(MilestoneReview item) => new(item.Id, item.UserId, item.PropertyId, item.BookingId, item.SubjectTitle, item.Rating, item.Text, item.Status, item.HostReply, item.CreatedAt, item.EditableUntil);
    private static TravelerNotificationDto ToDto(MilestoneTravelerNotification item) => new(item.Id, item.UserId, item.Type, item.Title, item.Body, item.DeepLink, item.IsRead, item.CreatedAt, item.ReadAt);
    private static DirectoryProviderDto ToDto(MilestoneDirectoryProvider item) => new(item.Id, item.OwnerUserId, item.Slug, item.Kind, item.Category, item.Name, item.Parish, item.BadgeLevel, item.Description, item.AvailabilitySummary, item.ContactMode, item.Rating, item.ReviewCount, item.IsActive);
    private static ConversationParticipantDto ToDto(MilestoneConversationParticipant item) => new(item.UserId, item.DisplayName, item.Role, item.LastReadAt, item.OnlineStatus);
    private static AttachmentUploadDto ToUploadDto(MilestoneMessageAttachment item) => new(item.Id, item.ConversationId, item.OwnerUserId, item.SafeFileName, item.ContentType, item.SizeBytes, item.ObjectKey, item.UploadUrl, item.Status, item.UploadExpiresAt);
    private static MessageDto ToDto(MilestoneMessage item) => new(item.Id, item.ConversationId, item.SenderUserId, item.Body, item.Status, item.SentAt, item.ReadAt, MilestoneJson.DeserializeList<MessageAttachmentDto>(item.AttachmentsJson));
    private static HostPricingRuleDto ToDto(MilestoneHostPricingRule item) => new(item.Id, item.HostUserId, item.PropertyId, item.Name, item.StartsOn, item.EndsOn, item.NightlyRate, item.MinimumStay, item.IsActive);
    private static HostPromotionDto ToDto(MilestoneHostPromotion item) => new(item.Id, item.HostUserId, item.PropertyId, item.Name, item.DiscountPercent, item.StartsOn, item.EndsOn, item.MinimumNights, item.BadgeLevel, item.IsActive);
    private static AdminCaseDto ToDto(MilestoneAdminCase item) => new(item.Id, item.CaseType, item.SubjectType, item.SubjectId, item.Status, item.Priority, item.Reason, item.AssignedTo, item.ResolutionNotes, item.CreatedAt, item.UpdatedAt, item.ResolvedAt);
    private static AuditEventDto ToDto(MilestoneAuditEvent item) => new(item.Id, item.ActorUserId, item.ActorRole, item.Action, item.SubjectType, item.SubjectId, item.Reason, item.CreatedAt);
    private static AuthFlowResultDto ToDto(MilestoneAuthFlow item) => new(
        item.Id,
        item.UserId,
        item.FlowType,
        item.Destination,
        item.Status,
        item.DeliveryChannel,
        item.ExpiresAt,
        item.LastSentAt,
        Math.Max(0, MaximumAuthFlowAttempts - item.FailedAttempts));
}
