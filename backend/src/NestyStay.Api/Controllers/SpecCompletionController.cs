using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestyStay.Api.Auth;
using NestyStay.Application.PhaseOne;
using NestyStay.Application.SpecCompletion;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/spec")]
public sealed class SpecCompletionController(
    ISpecCompletionStore store,
    IPhaseOneStore phaseOneStore,
    CurrentUserContext currentUser) : ControllerBase
{
    [HttpPost("seed")]
    public async Task<ActionResult<SpecSeedStatusDto>> Seed(CancellationToken cancellationToken) =>
        Ok(await store.EnsureSeededAsync(cancellationToken));

    [HttpGet("public/pages")]
    public async Task<ActionResult<IReadOnlyList<PublicContentPageDto>>> GetPublicPages(CancellationToken cancellationToken) =>
        Ok(await store.GetPublicPagesAsync(cancellationToken));

    [HttpGet("public/pages/{*slug}")]
    public async Task<ActionResult<PublicContentPageDto>> GetPublicPage(string slug, CancellationToken cancellationToken) =>
        await store.GetPublicPageAsync(slug, cancellationToken) is { } page ? Ok(page) : NotFound();

    [HttpPost("public/contact")]
    public async Task<ActionResult<ContactRequestDto>> CreateContact(CreateContactRequest request, CancellationToken cancellationToken) =>
        Ok(await store.CreateContactRequestAsync(request, cancellationToken));

    [HttpGet("experiences")]
    public async Task<ActionResult<IReadOnlyList<ExperienceDto>>> GetExperiences(
        [FromQuery] string? category,
        [FromQuery] string? parish,
        [FromQuery] string? query,
        CancellationToken cancellationToken) =>
        Ok(await store.GetExperiencesAsync(category, parish, query, cancellationToken));

    [HttpGet("experiences/{slug}")]
    public async Task<ActionResult<ExperienceDto>> GetExperience(string slug, CancellationToken cancellationToken) =>
        await store.GetExperienceAsync(slug, cancellationToken) is { } experience ? Ok(experience) : NotFound();

    [HttpGet("journal")]
    public async Task<ActionResult<IReadOnlyList<JournalArticleDto>>> GetJournal(
        [FromQuery] string? category,
        [FromQuery] string? query,
        CancellationToken cancellationToken) =>
        Ok(await store.GetJournalAsync(category, query, cancellationToken));

    [HttpGet("journal/{slug}")]
    public async Task<ActionResult<JournalArticleDto>> GetJournalArticle(string slug, CancellationToken cancellationToken) =>
        await store.GetJournalArticleAsync(slug, cancellationToken) is { } article ? Ok(article) : NotFound();

    [HttpGet("host-profiles")]
    public async Task<ActionResult<IReadOnlyList<HostProfileDto>>> GetHostProfiles(CancellationToken cancellationToken) =>
        Ok(await store.GetHostProfilesAsync(cancellationToken));

    [HttpGet("host-profiles/{slug}")]
    public async Task<ActionResult<HostProfileDto>> GetHostProfile(string slug, CancellationToken cancellationToken) =>
        await store.GetHostProfileAsync(slug, cancellationToken) is { } profile ? Ok(profile) : NotFound();

    [HttpPut("host-profiles/{slug}")]
    public async Task<ActionResult<HostProfileDto>> UpsertHostProfile(
        string slug,
        UpsertHostProfileRequest request,
        CancellationToken cancellationToken)
    {
        var actor = RequireUser(request.HostUserId);
        return Ok(await store.UpsertHostProfileAsync(slug, request, actor, cancellationToken));
    }

    [HttpGet("traveler/{userId:guid}")]
    public async Task<ActionResult<TravelerWorkspaceDto>> GetTraveler(Guid userId, CancellationToken cancellationToken)
    {
        RequireUser(userId);
        return Ok(await store.GetTravelerWorkspaceAsync(userId, cancellationToken));
    }

    [HttpPost("traveler/{userId:guid}/wishlist/collections")]
    public async Task<ActionResult<WishlistCollectionDto>> CreateWishlistCollection(Guid userId, SaveWishlistCollectionRequest request, CancellationToken cancellationToken)
    {
        RequireUser(userId);
        return Ok(await store.CreateWishlistCollectionAsync(userId, request, cancellationToken));
    }

    [HttpPut("traveler/{userId:guid}/wishlist/collections/{collectionId:guid}")]
    public async Task<ActionResult<WishlistCollectionDto>> RenameWishlistCollection(Guid userId, Guid collectionId, SaveWishlistCollectionRequest request, CancellationToken cancellationToken)
    {
        RequireUser(userId);
        return Ok(await store.RenameWishlistCollectionAsync(userId, collectionId, request, cancellationToken));
    }

    [HttpDelete("traveler/{userId:guid}/wishlist/collections/{collectionId:guid}")]
    public async Task<IActionResult> DeleteWishlistCollection(Guid userId, Guid collectionId, CancellationToken cancellationToken)
    {
        RequireUser(userId);
        await store.DeleteWishlistCollectionAsync(userId, collectionId, cancellationToken);
        return NoContent();
    }

    [HttpPost("traveler/{userId:guid}/wishlist/collections/{collectionId:guid}/items")]
    public async Task<ActionResult<WishlistItemDto>> AddWishlistItem(Guid userId, Guid collectionId, SaveWishlistItemRequest request, CancellationToken cancellationToken)
    {
        RequireUser(userId);
        return Ok(await store.AddWishlistItemAsync(userId, collectionId, request, cancellationToken));
    }

    [HttpDelete("traveler/{userId:guid}/wishlist/items/{itemId:guid}")]
    public async Task<IActionResult> RemoveWishlistItem(Guid userId, Guid itemId, CancellationToken cancellationToken)
    {
        RequireUser(userId);
        await store.RemoveWishlistItemAsync(userId, itemId, cancellationToken);
        return NoContent();
    }

    [HttpPost("traveler/{userId:guid}/payment-methods")]
    public async Task<ActionResult<PaymentMethodDto>> AddPaymentMethod(Guid userId, SavePaymentMethodRequest request, CancellationToken cancellationToken)
    {
        RequireUser(userId);
        return Ok(await store.AddPaymentMethodAsync(userId, request, cancellationToken));
    }

    [HttpPost("traveler/{userId:guid}/payment-methods/{paymentMethodId:guid}/default")]
    public async Task<IActionResult> SetDefaultPaymentMethod(Guid userId, Guid paymentMethodId, CancellationToken cancellationToken)
    {
        RequireUser(userId);
        await store.SetDefaultPaymentMethodAsync(userId, paymentMethodId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("traveler/{userId:guid}/payment-methods/{paymentMethodId:guid}")]
    public async Task<IActionResult> RemovePaymentMethod(Guid userId, Guid paymentMethodId, CancellationToken cancellationToken)
    {
        RequireUser(userId);
        await store.RemovePaymentMethodAsync(userId, paymentMethodId, cancellationToken);
        return NoContent();
    }

    [HttpPost("traveler/{userId:guid}/reviews")]
    public async Task<ActionResult<ReviewDto>> SubmitReview(Guid userId, SaveReviewRequest request, CancellationToken cancellationToken)
    {
        RequireUser(userId);
        return Ok(await store.SubmitReviewAsync(userId, request, cancellationToken));
    }

    [HttpPost("host/{hostUserId:guid}/reviews/{reviewId:guid}/reply")]
    public async Task<ActionResult<ReviewDto>> ReplyToReview(Guid hostUserId, Guid reviewId, SaveReviewReplyRequest request, CancellationToken cancellationToken)
    {
        RequireUser(hostUserId);
        return Ok(await store.ReplyToReviewAsync(hostUserId, reviewId, request, cancellationToken));
    }

    [HttpPost("traveler/{userId:guid}/notifications/{notificationId:guid}/read")]
    public async Task<IActionResult> MarkNotificationRead(Guid userId, Guid notificationId, CancellationToken cancellationToken)
    {
        RequireUser(userId);
        await store.MarkNotificationReadAsync(userId, notificationId, cancellationToken);
        return NoContent();
    }

    [HttpPost("traveler/{userId:guid}/notifications/read-all")]
    public async Task<IActionResult> MarkAllNotificationsRead(Guid userId, CancellationToken cancellationToken)
    {
        RequireUser(userId);
        await store.MarkAllNotificationsReadAsync(userId, cancellationToken);
        return NoContent();
    }

    [HttpGet("directories/providers")]
    public async Task<ActionResult<IReadOnlyList<DirectoryProviderDto>>> GetDirectoryProviders(
        [FromQuery] string? kind,
        [FromQuery] string? category,
        [FromQuery] string? parish,
        [FromQuery] string? query,
        CancellationToken cancellationToken) =>
        Ok(await store.GetDirectoryProvidersAsync(kind, category, parish, query, cancellationToken));

    [HttpGet("directories/providers/{slug}")]
    public async Task<ActionResult<DirectoryProviderDto>> GetDirectoryProvider(string slug, CancellationToken cancellationToken) =>
        await store.GetDirectoryProviderAsync(slug, cancellationToken) is { } provider ? Ok(provider) : NotFound();

    [HttpPost("directories/providers")]
    public async Task<ActionResult<DirectoryProviderDto>> UpsertDirectoryProvider(UpsertDirectoryProviderRequest request, CancellationToken cancellationToken)
    {
        var actor = RequireAnyUser();
        return Ok(await store.UpsertDirectoryProviderAsync(request, actor, cancellationToken));
    }

    [HttpGet("messages/inbox")]
    public async Task<ActionResult<MessagingInboxDto>> GetInbox([FromQuery] Guid userId, CancellationToken cancellationToken)
    {
        RequireUser(userId);
        return Ok(await store.GetInboxAsync(userId, cancellationToken));
    }

    [HttpGet("messages/conversations/{conversationId:guid}")]
    public async Task<ActionResult<ConversationDto>> GetConversation(Guid conversationId, [FromQuery] Guid userId, CancellationToken cancellationToken)
    {
        RequireUser(userId);
        return await store.GetConversationAsync(userId, conversationId, cancellationToken) is { } conversation ? Ok(conversation) : NotFound();
    }

    [HttpPost("messages/conversations")]
    public async Task<ActionResult<ConversationDto>> CreateConversation([FromQuery] Guid userId, CreateConversationRequest request, CancellationToken cancellationToken)
    {
        RequireUser(userId);
        return Ok(await store.CreateConversationAsync(userId, request, cancellationToken));
    }

    [HttpPost("messages/conversations/{conversationId:guid}/messages")]
    public async Task<ActionResult<MessageDto>> SendMessage(Guid conversationId, [FromQuery] Guid userId, SendMessageRequest request, CancellationToken cancellationToken)
    {
        RequireUser(userId);
        return Ok(await store.SendMessageAsync(userId, conversationId, request, cancellationToken));
    }

    [HttpPost("messages/conversations/{conversationId:guid}/read")]
    public async Task<IActionResult> MarkConversationRead(Guid conversationId, [FromQuery] Guid userId, CancellationToken cancellationToken)
    {
        RequireUser(userId);
        await store.MarkConversationReadAsync(userId, conversationId, cancellationToken);
        return NoContent();
    }

    [HttpGet("host/{hostUserId:guid}/operations")]
    public async Task<ActionResult<HostOperationsDto>> GetHostOperations(Guid hostUserId, CancellationToken cancellationToken)
    {
        RequireUser(hostUserId);
        return Ok(await store.GetHostOperationsAsync(hostUserId, cancellationToken));
    }

    [HttpPost("host/{hostUserId:guid}/pricing-rules")]
    public async Task<ActionResult<HostPricingRuleDto>> SavePricingRule(Guid hostUserId, SaveHostPricingRuleRequest request, CancellationToken cancellationToken)
    {
        RequireUser(hostUserId);
        if (!HostOwnsProperty(hostUserId, request.PropertyId))
        {
            return NotFound();
        }

        return Ok(await store.SaveHostPricingRuleAsync(hostUserId, request, cancellationToken));
    }

    [HttpPost("host/{hostUserId:guid}/promotions")]
    public async Task<ActionResult<HostPromotionDto>> SavePromotion(Guid hostUserId, SaveHostPromotionRequest request, CancellationToken cancellationToken)
    {
        RequireUser(hostUserId);
        if (!HostOwnsProperty(hostUserId, request.PropertyId))
        {
            return NotFound();
        }

        return Ok(await store.SaveHostPromotionAsync(hostUserId, request, cancellationToken));
    }

    [Authorize(Policy = AdminTokenAuthenticationHandler.AdminPolicyName)]
    [HttpGet("admin/operations")]
    public async Task<ActionResult<AdminOperationsDto>> GetAdminOperations(CancellationToken cancellationToken) =>
        Ok(await store.GetAdminOperationsAsync(cancellationToken));

    [Authorize(Policy = AdminTokenAuthenticationHandler.AdminPolicyName)]
    [HttpPost("admin/cases")]
    public async Task<ActionResult<AdminCaseDto>> CreateAdminCase(CreateAdminCaseRequest request, CancellationToken cancellationToken) =>
        Ok(await store.CreateAdminCaseAsync(request, TryGetUserFromBearer(), cancellationToken));

    [Authorize(Policy = AdminTokenAuthenticationHandler.AdminPolicyName)]
    [HttpPost("admin/cases/{caseId:guid}/resolve")]
    public async Task<ActionResult<AdminCaseDto>> ResolveAdminCase(Guid caseId, ResolveAdminCaseRequest request, CancellationToken cancellationToken) =>
        Ok(await store.ResolveAdminCaseAsync(caseId, request, TryGetUserFromBearer(), cancellationToken));

    [Authorize(Policy = AdminTokenAuthenticationHandler.AdminPolicyName)]
    [HttpGet("admin/audit-log")]
    public async Task<ActionResult<IReadOnlyList<AuditEventDto>>> GetAuditLog(CancellationToken cancellationToken) =>
        Ok(await store.GetAuditEventsAsync(cancellationToken));

    [HttpPost("auth/flows")]
    public async Task<ActionResult<AuthFlowResultDto>> StartAuthFlow(StartAuthFlowRequest request, CancellationToken cancellationToken) =>
        Ok(await store.StartAuthFlowAsync(request, cancellationToken));

    [HttpPost("auth/flows/complete")]
    public async Task<ActionResult<AuthFlowResultDto>> CompleteAuthFlow(CompleteAuthFlowRequest request, CancellationToken cancellationToken) =>
        Ok(await store.CompleteAuthFlowAsync(request, cancellationToken));

    [HttpPost("auth/{userId:guid}/recovery-codes")]
    public async Task<ActionResult<IReadOnlyList<RecoveryCodeDto>>> GenerateRecoveryCodes(Guid userId, CancellationToken cancellationToken)
    {
        RequireUser(userId);
        return Ok(await store.GenerateRecoveryCodesAsync(userId, cancellationToken));
    }

    [HttpGet("auth/social-config")]
    public async Task<ActionResult<SocialAuthConfigDto>> SocialConfig(CancellationToken cancellationToken) =>
        Ok(await store.GetSocialAuthConfigAsync(cancellationToken));

    private Guid RequireAnyUser() => currentUser.UserId ?? throw new UnauthorizedAccessException("A signed session bearer token is required.");

    private bool HostOwnsProperty(Guid hostUserId, Guid propertyId) =>
        phaseOneStore.GetProperty(propertyId) is { } property && property.HostUserId == hostUserId;

    private Guid RequireUser(Guid expectedUserId)
    {
        var actual = RequireAnyUser();
        if (actual != expectedUserId)
        {
            throw new UnauthorizedAccessException("The bearer token does not match this resource owner.");
        }

        return actual;
    }

    private Guid? TryGetUserFromBearer() => currentUser.UserId;
}
