using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using NestyStay.Domain;

namespace NestyStay.Api.Tests;

public sealed class SpecCompletionEndpointTests : IClassFixture<NestyStayApiFactory>
{
    private readonly NestyStayApiFactory _factory;

    public SpecCompletionEndpointTests(NestyStayApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PublicContentExperiencesJournalAndAuthFlowsArePersistedAndValidated()
    {
        using var client = _factory.CreateClient();
        var userId = Guid.NewGuid();

        var seedResponse = await client.PostAsync("/api/spec/seed", null);
        Assert.Equal(HttpStatusCode.OK, seedResponse.StatusCode);
        var seed = await seedResponse.Content.ReadFromJsonAsync<SpecSeedResponse>();
        Assert.NotNull(seed);
        Assert.True(seed.PublicPages >= 10);
        Assert.True(seed.Experiences >= 3);
        Assert.True(seed.JournalArticles >= 3);
        Assert.True(seed.HostProfiles >= 1);
        Assert.True(seed.DirectoryProviders >= 5);

        var helpArticle = await client.GetFromJsonAsync<PublicPageResponse>("/api/spec/public/pages/help/booking");
        Assert.NotNull(helpArticle);
        Assert.Equal("help/booking", helpArticle.Slug);
        Assert.Contains("Payment capture", helpArticle.Body);

        var invalidContact = await client.PostAsJsonAsync("/api/spec/public/contact", new
        {
            name = "",
            email = "guest@test.local",
            subject = "Question",
            message = "Need help."
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidContact.StatusCode);

        var contact = await client.PostAsJsonAsync("/api/spec/public/contact", new
        {
            name = "Guest Traveler",
            email = "GUEST@TEST.LOCAL",
            subject = "Booking support",
            message = "Need help with a pending booking."
        });
        Assert.Equal(HttpStatusCode.OK, contact.StatusCode);
        var contactBody = await contact.Content.ReadFromJsonAsync<ContactResponse>();
        Assert.NotNull(contactBody);
        Assert.Equal("guest@test.local", contactBody.Email);
        Assert.Equal("Open", contactBody.Status);

        var experiences = await client.GetFromJsonAsync<List<ExperienceResponse>>("/api/spec/experiences?category=Wellness&query=lagoon");
        Assert.NotNull(experiences);
        var wellness = Assert.Single(experiences);
        Assert.Equal("blue-lagoon-wellness-swim", wellness.Slug);
        Assert.True(wellness.Price > 0);
        Assert.NotEmpty(wellness.Images);

        var journal = await client.GetFromJsonAsync<List<JournalArticleResponse>>("/api/spec/journal?query=badges");
        Assert.NotNull(journal);
        Assert.Contains(journal, item => item.Slug == "host-badges-explained");

        var authFlow = await client.PostAsJsonAsync("/api/spec/auth/flows", new
        {
            userId,
            flowType = "EmailVerification",
            destination = "guest@test.local"
        });
        Assert.Equal(HttpStatusCode.OK, authFlow.StatusCode);
        var startedBody = await authFlow.Content.ReadAsStringAsync();
        Assert.DoesNotContain("code", startedBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", startedBody, StringComparison.OrdinalIgnoreCase);
        var started = await authFlow.Content.ReadFromJsonAsync<AuthFlowResponse>();
        Assert.NotNull(started);
        Assert.Equal("Pending", started.Status);

        var invalidCompletion = await client.PostAsJsonAsync("/api/spec/auth/flows/complete", new
        {
            flowId = started.Id,
            code = "000000"
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidCompletion.StatusCode);

        var restarted = await client.PostAsJsonAsync("/api/spec/auth/flows", new
        {
            userId,
            flowType = "PhoneVerification",
            destination = "+17542482435"
        });
        Assert.Equal(HttpStatusCode.OK, restarted.StatusCode);
        var phone = await restarted.Content.ReadFromJsonAsync<AuthFlowResponse>();
        Assert.NotNull(phone);
        var phoneSecret = await client.GetFromJsonAsync<DevelopmentAuthFlowSecretResponse>(
            $"/api/spec/auth/development/flows/{phone.Id}");
        Assert.NotNull(phoneSecret);

        var completedResponse = await client.PostAsJsonAsync("/api/spec/auth/flows/complete", new
        {
            flowId = phone.Id,
            code = phoneSecret.Code
        });
        Assert.Equal(HttpStatusCode.OK, completedResponse.StatusCode);
        var completed = await completedResponse.Content.ReadFromJsonAsync<AuthFlowResponse>();
        Assert.NotNull(completed);
        Assert.Equal("Completed", completed.Status);

        var reusedCompletion = await client.PostAsJsonAsync("/api/spec/auth/flows/complete", new
        {
            flowId = phone.Id,
            code = phoneSecret.Code
        });
        Assert.Equal(HttpStatusCode.BadRequest, reusedCompletion.StatusCode);

        var recoveryWithoutToken = await client.PostAsync($"/api/spec/auth/{userId}/recovery-codes", null);
        Assert.Equal(HttpStatusCode.Unauthorized, recoveryWithoutToken.StatusCode);

        client.DefaultRequestHeaders.Authorization = LocalUser(userId);
        var recoveryResponse = await client.PostAsync($"/api/spec/auth/{userId}/recovery-codes", null);
        Assert.Equal(HttpStatusCode.OK, recoveryResponse.StatusCode);
        var recoveryCodes = await recoveryResponse.Content.ReadFromJsonAsync<List<RecoveryCodeResponse>>();
        Assert.NotNull(recoveryCodes);
        Assert.Equal(8, recoveryCodes.Count);
        Assert.All(recoveryCodes, code => Assert.False(code.Used));

        client.DefaultRequestHeaders.Authorization = null;
        var socialConfig = await client.GetFromJsonAsync<SocialConfigResponse>("/api/spec/auth/social-config");
        Assert.NotNull(socialConfig);
        Assert.True(socialConfig.GoogleEnabled);
        Assert.False(socialConfig.AppleEnabled);
        Assert.False(socialConfig.FacebookEnabled);
        Assert.Contains("GOOGLE_AUTH_CLIENT_ID", socialConfig.RequiredEnvironmentVariables);
        Assert.DoesNotContain("APPLE_AUTH_CLIENT_ID", socialConfig.RequiredEnvironmentVariables);
        Assert.DoesNotContain("FACEBOOK_AUTH_APP_ID", socialConfig.RequiredEnvironmentVariables);
    }

    [Fact]
    public async Task TravelerMessagingDirectoryHostAndAdminMilestoneApisPersistAndEnforceAuthorization()
    {
        using var client = _factory.CreateClient();
        var travelerId = Guid.NewGuid();
        var otherTravelerId = Guid.NewGuid();
        var hostId = Guid.NewGuid();
        var propertyId = await CreateHostPropertyAsync(client, hostId);
        client.DefaultRequestHeaders.Authorization = null;

        var travelerNoToken = await client.GetAsync($"/api/spec/traveler/{travelerId}");
        Assert.Equal(HttpStatusCode.Unauthorized, travelerNoToken.StatusCode);

        client.DefaultRequestHeaders.Authorization = LocalUser(otherTravelerId);
        var travelerWrongToken = await client.GetAsync($"/api/spec/traveler/{travelerId}");
        Assert.Equal(HttpStatusCode.Unauthorized, travelerWrongToken.StatusCode);

        client.DefaultRequestHeaders.Authorization = LocalUser(travelerId);
        var workspace = await client.GetFromJsonAsync<TravelerWorkspaceResponse>($"/api/spec/traveler/{travelerId}");
        Assert.NotNull(workspace);
        Assert.NotEmpty(workspace.WishlistCollections);
        Assert.NotEmpty(workspace.Notifications);

        var collectionResponse = await client.PostAsJsonAsync($"/api/spec/traveler/{travelerId}/wishlist/collections", new
        {
            name = "Birthday trip",
            sortOrder = 2
        });
        Assert.Equal(HttpStatusCode.OK, collectionResponse.StatusCode);
        var collection = await collectionResponse.Content.ReadFromJsonAsync<WishlistCollectionResponse>();
        Assert.NotNull(collection);

        var renamedResponse = await client.PutAsJsonAsync($"/api/spec/traveler/{travelerId}/wishlist/collections/{collection.Id}", new
        {
            name = "Birthday trip shortlist",
            sortOrder = 1
        });
        Assert.Equal(HttpStatusCode.OK, renamedResponse.StatusCode);
        var renamed = await renamedResponse.Content.ReadFromJsonAsync<WishlistCollectionResponse>();
        Assert.NotNull(renamed);
        Assert.Equal("Birthday trip shortlist", renamed.Name);

        var wishlistItemResponse = await client.PostAsJsonAsync($"/api/spec/traveler/{travelerId}/wishlist/collections/{collection.Id}/items", new
        {
            propertyId,
            propertyTitle = "Ocho Rios Verified Villa",
            status = "Available",
            sortOrder = 1
        });
        Assert.Equal(HttpStatusCode.OK, wishlistItemResponse.StatusCode);
        var wishlistItem = await wishlistItemResponse.Content.ReadFromJsonAsync<WishlistItemResponse>();
        Assert.NotNull(wishlistItem);
        Assert.Equal(propertyId, wishlistItem.PropertyId);

        client.DefaultRequestHeaders.Authorization = LocalUser(otherTravelerId);
        var crossTravelerCollectionRename = await client.PutAsJsonAsync($"/api/spec/traveler/{otherTravelerId}/wishlist/collections/{collection.Id}", new
        {
            name = "Takeover list",
            sortOrder = 1
        });
        Assert.Equal(HttpStatusCode.Unauthorized, crossTravelerCollectionRename.StatusCode);

        var crossTravelerCollectionDelete = await client.DeleteAsync($"/api/spec/traveler/{otherTravelerId}/wishlist/collections/{collection.Id}");
        Assert.Equal(HttpStatusCode.Unauthorized, crossTravelerCollectionDelete.StatusCode);

        var crossTravelerWishlistItemAdd = await client.PostAsJsonAsync($"/api/spec/traveler/{otherTravelerId}/wishlist/collections/{collection.Id}/items", new
        {
            propertyId,
            propertyTitle = "Cross traveler property",
            status = "Available",
            sortOrder = 1
        });
        Assert.Equal(HttpStatusCode.Unauthorized, crossTravelerWishlistItemAdd.StatusCode);

        var crossTravelerWishlistItemRemove = await client.DeleteAsync($"/api/spec/traveler/{otherTravelerId}/wishlist/items/{wishlistItem.Id}");
        Assert.Equal(HttpStatusCode.Unauthorized, crossTravelerWishlistItemRemove.StatusCode);

        client.DefaultRequestHeaders.Authorization = LocalUser(travelerId);
        var invalidPayment = await client.PostAsJsonAsync($"/api/spec/traveler/{travelerId}/payment-methods", new
        {
            setupIntentReference = "",
            isDefault = true
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidPayment.StatusCode);

        var setupIntentResponse = await client.PostAsync($"/api/spec/traveler/{travelerId}/payment-methods/setup-intents", null);
        Assert.Equal(HttpStatusCode.OK, setupIntentResponse.StatusCode);
        var setupIntent = await setupIntentResponse.Content.ReadFromJsonAsync<PaymentSetupIntentResponse>();
        Assert.NotNull(setupIntent);
        Assert.StartsWith("stripe_local_seti_", setupIntent.SetupIntentReference, StringComparison.Ordinal);
        Assert.NotEmpty(setupIntent.ClientSecret);

        var paymentResponse = await client.PostAsJsonAsync($"/api/spec/traveler/{travelerId}/payment-methods", new
        {
            setupIntentReference = setupIntent.SetupIntentReference,
            isDefault = true
        });
        Assert.Equal(HttpStatusCode.OK, paymentResponse.StatusCode);
        var payment = await paymentResponse.Content.ReadFromJsonAsync<PaymentMethodResponse>();
        Assert.NotNull(payment);
        Assert.True(payment.IsDefault);
        Assert.StartsWith("stripe_local_pm_", payment.ProviderPaymentMethodReference, StringComparison.Ordinal);

        client.DefaultRequestHeaders.Authorization = LocalUser(otherTravelerId);
        var crossTravelerDefaultPayment = await client.PostAsync($"/api/spec/traveler/{otherTravelerId}/payment-methods/{payment.Id}/default", null);
        Assert.Equal(HttpStatusCode.Unauthorized, crossTravelerDefaultPayment.StatusCode);

        var crossTravelerRemovePayment = await client.DeleteAsync($"/api/spec/traveler/{otherTravelerId}/payment-methods/{payment.Id}");
        Assert.Equal(HttpStatusCode.Unauthorized, crossTravelerRemovePayment.StatusCode);

        var crossTravelerIdentityPrepare = await client.PostAsJsonAsync($"/api/spec/traveler/{travelerId}/identity-documents/uploads", new
        {
            documentType = "Passport",
            fileName = "passport.pdf",
            contentType = "application/pdf",
            sizeBytes = 12
        });
        Assert.Equal(HttpStatusCode.Unauthorized, crossTravelerIdentityPrepare.StatusCode);

        client.DefaultRequestHeaders.Authorization = LocalUser(travelerId);
        var invalidIdentityPrepare = await client.PostAsJsonAsync($"/api/spec/traveler/{travelerId}/identity-documents/uploads", new
        {
            documentType = "Passport",
            fileName = "passport.gif",
            contentType = "image/gif",
            sizeBytes = 12
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidIdentityPrepare.StatusCode);

        var spoofedIdentityBytes = Encoding.ASCII.GetBytes("not a pdf");
        var spoofedIdentityPrepare = await client.PostAsJsonAsync($"/api/spec/traveler/{travelerId}/identity-documents/uploads", new
        {
            documentType = "Passport",
            fileName = "spoofed.pdf",
            contentType = "application/pdf",
            sizeBytes = spoofedIdentityBytes.Length
        });
        Assert.Equal(HttpStatusCode.OK, spoofedIdentityPrepare.StatusCode);
        var spoofedIdentity = await spoofedIdentityPrepare.Content.ReadFromJsonAsync<IdentityDocumentUploadResponse>();
        Assert.NotNull(spoofedIdentity);
        using var spoofedIdentityContent = new ByteArrayContent(spoofedIdentityBytes);
        spoofedIdentityContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        var rejectedIdentityUpload = await client.PutAsync(
            $"/api/spec/traveler/{travelerId}/identity-documents/uploads/{spoofedIdentity.Id}/content",
            spoofedIdentityContent);
        Assert.Equal(HttpStatusCode.BadRequest, rejectedIdentityUpload.StatusCode);

        var identityPdfBytes = Encoding.ASCII.GetBytes("%PDF-1.7\nidentity document");
        var identityPrepare = await client.PostAsJsonAsync($"/api/spec/traveler/{travelerId}/identity-documents/uploads", new
        {
            documentType = "Passport",
            fileName = "../Passport.pdf",
            contentType = "application/pdf",
            sizeBytes = identityPdfBytes.Length,
            issuingCountry = "jm"
        });
        Assert.Equal(HttpStatusCode.OK, identityPrepare.StatusCode);
        var preparedIdentity = await identityPrepare.Content.ReadFromJsonAsync<IdentityDocumentUploadResponse>();
        Assert.NotNull(preparedIdentity);
        Assert.Equal("passport.pdf", preparedIdentity.FileName);
        Assert.Equal("PendingUpload", preparedIdentity.Status);
        Assert.Equal("PendingScan", preparedIdentity.ScanStatus);
        using var identityContent = new ByteArrayContent(identityPdfBytes);
        identityContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        var uploadedIdentityResponse = await client.PutAsync(
            $"/api/spec/traveler/{travelerId}/identity-documents/uploads/{preparedIdentity.Id}/content",
            identityContent);
        Assert.Equal(HttpStatusCode.OK, uploadedIdentityResponse.StatusCode);
        var uploadedIdentity = await uploadedIdentityResponse.Content.ReadFromJsonAsync<IdentityDocumentUploadResponse>();
        Assert.NotNull(uploadedIdentity);
        Assert.Equal("Uploaded", uploadedIdentity.Status);
        Assert.Equal("Clean", uploadedIdentity.ScanStatus);
        Assert.NotEmpty(uploadedIdentity.Sha256Hash ?? string.Empty);
        Assert.NotNull(uploadedIdentity.IdentityDocumentId);

        var identityWorkspace = await client.GetFromJsonAsync<TravelerWorkspaceResponse>($"/api/spec/traveler/{travelerId}");
        Assert.NotNull(identityWorkspace);
        var identityDocument = Assert.Single(identityWorkspace.IdentityDocuments);
        Assert.Equal(uploadedIdentity.IdentityDocumentId, identityDocument.Id);
        Assert.Equal("Passport", identityDocument.DocumentType);
        Assert.Equal("passport.pdf", identityDocument.FileName);

        var reviewResponse = await client.PostAsJsonAsync($"/api/spec/traveler/{travelerId}/reviews", new
        {
            propertyId,
            bookingId = (Guid?)null,
            subjectTitle = "Ocho Rios Verified Villa",
            rating = 5,
            text = "Clean, verified, and easy to access."
        });
        Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode);
        var review = await reviewResponse.Content.ReadFromJsonAsync<ReviewResponse>();
        Assert.NotNull(review);

        client.DefaultRequestHeaders.Authorization = LocalUser(hostId);
        var hostRouteAsGuest = await client.GetAsync($"/api/spec/host/{hostId}/operations");
        Assert.Equal(HttpStatusCode.Forbidden, hostRouteAsGuest.StatusCode);

        var otherHostId = Guid.NewGuid();
        client.DefaultRequestHeaders.Authorization = LocalUser(otherHostId, UserRole.Host);
        var crossHostReplyResponse = await client.PostAsJsonAsync($"/api/spec/host/{otherHostId}/reviews/{review.Id}/reply", new
        {
            reply = "This host should not be able to reply."
        });
        Assert.Equal(HttpStatusCode.Unauthorized, crossHostReplyResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = LocalUser(hostId, UserRole.Host);
        var replyResponse = await client.PostAsJsonAsync($"/api/spec/host/{hostId}/reviews/{review.Id}/reply", new
        {
            reply = "Big thanks for staying with us."
        });
        Assert.Equal(HttpStatusCode.OK, replyResponse.StatusCode);
        var replied = await replyResponse.Content.ReadFromJsonAsync<ReviewResponse>();
        Assert.NotNull(replied);
        Assert.Equal("Big thanks for staying with us.", replied.HostReply);

        client.DefaultRequestHeaders.Authorization = LocalUser(travelerId);
        var readAll = await client.PostAsync($"/api/spec/traveler/{travelerId}/notifications/read-all", null);
        Assert.Equal(HttpStatusCode.NoContent, readAll.StatusCode);
        var refreshedWorkspace = await client.GetFromJsonAsync<TravelerWorkspaceResponse>($"/api/spec/traveler/{travelerId}");
        Assert.NotNull(refreshedWorkspace);
        Assert.All(refreshedWorkspace.Notifications, item => Assert.True(item.IsRead));

        var otherHostPropertyId = await CreateHostPropertyAsync(client, otherHostId);
        client.DefaultRequestHeaders.Authorization = LocalUser(hostId, UserRole.Host);
        var hostProfileResponse = await client.PutAsJsonAsync("/api/spec/host-profiles/profile-ownership-test", new
        {
            hostUserId = hostId,
            displayName = "Profile Ownership Host",
            parish = "Kingston",
            bio = "A host profile with owned listing references.",
            responseTime = "Replies in 15 minutes",
            badges = new[] { "Verified" },
            listingIds = new[] { propertyId },
            isPublic = true,
            highlights = new[] { "Owned listing only" }
        });
        Assert.Equal(HttpStatusCode.OK, hostProfileResponse.StatusCode);

        var foreignListingProfileResponse = await client.PutAsJsonAsync("/api/spec/host-profiles/profile-ownership-test", new
        {
            hostUserId = hostId,
            displayName = "Foreign Listing Host",
            parish = "Kingston",
            bio = "This profile attempts to claim another host property.",
            responseTime = "Replies in 5 minutes",
            badges = new[] { "Verified" },
            listingIds = new[] { otherHostPropertyId },
            isPublic = true,
            highlights = new[] { "Foreign listing" }
        });
        Assert.Equal(HttpStatusCode.Unauthorized, foreignListingProfileResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = LocalUser(otherHostId, UserRole.Host);
        var crossHostProfileResponse = await client.PutAsJsonAsync("/api/spec/host-profiles/profile-ownership-test", new
        {
            hostUserId = otherHostId,
            displayName = "Takeover Host",
            parish = "St. Ann",
            bio = "This host should not overwrite an existing profile slug.",
            responseTime = "Replies instantly",
            badges = new[] { "Trusted" },
            listingIds = new[] { otherHostPropertyId },
            isPublic = true,
            highlights = new[] { "Slug takeover" }
        });
        Assert.Equal(HttpStatusCode.Unauthorized, crossHostProfileResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = null;
        var directoryCreateNoToken = await client.PostAsJsonAsync("/api/spec/directories/providers", new
        {
            kind = "Custodian",
            category = "Cleaning",
            name = "No Token Team",
            parish = "Kingston",
            badgeLevel = "Verified",
            description = "Should be rejected.",
            availabilitySummary = "Weekdays",
            contactMode = "Platform messaging only"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, directoryCreateNoToken.StatusCode);

        client.DefaultRequestHeaders.Authorization = LocalUser(hostId, UserRole.Host);
        var seedProviderEditResponse = await client.PostAsJsonAsync("/api/spec/directories/providers", new
        {
            slug = "spark-cleaning-team",
            kind = "Custodian",
            category = "Cleaning",
            name = "Seed Provider Takeover",
            parish = "St. James",
            badgeLevel = "Trusted",
            description = "Seed providers should not be mutable through onboarding.",
            availabilitySummary = "Always",
            contactMode = "Direct",
            isActive = true
        });
        Assert.Equal(HttpStatusCode.Unauthorized, seedProviderEditResponse.StatusCode);

        var directoryProviderResponse = await client.PostAsJsonAsync("/api/spec/directories/providers", new
        {
            slug = "kingston-turnover-team",
            kind = "Custodian",
            category = "Cleaning",
            name = "Kingston Turnover Team",
            parish = "Kingston",
            badgeLevel = "Verified",
            description = "Turnover and inspection support for hosts.",
            availabilitySummary = "Mon-Fri 8 AM-5 PM",
            contactMode = "Platform messaging only",
            isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, directoryProviderResponse.StatusCode);
        var provider = await directoryProviderResponse.Content.ReadFromJsonAsync<DirectoryProviderResponse>();
        Assert.NotNull(provider);
        Assert.Equal(hostId, provider.OwnerUserId);
        Assert.Equal("kingston-turnover-team", provider.Slug);

        var providerLookup = await client.GetFromJsonAsync<DirectoryProviderResponse>("/api/spec/directories/providers/kingston-turnover-team");
        Assert.NotNull(providerLookup);
        Assert.Equal("Kingston Turnover Team", providerLookup.Name);

        client.DefaultRequestHeaders.Authorization = LocalUser(otherHostId, UserRole.Host);
        var providerTakeoverResponse = await client.PostAsJsonAsync("/api/spec/directories/providers", new
        {
            slug = "kingston-turnover-team",
            kind = "Custodian",
            category = "Cleaning",
            name = "Taken Over Provider",
            parish = "Kingston",
            badgeLevel = "Trusted",
            description = "This should not overwrite another provider owner.",
            availabilitySummary = "Always",
            contactMode = "Direct",
            isActive = true
        });
        Assert.Equal(HttpStatusCode.Unauthorized, providerTakeoverResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = LocalUser(travelerId);
        var createConversation = await client.PostAsJsonAsync($"/api/spec/messages/conversations?userId={travelerId}", new
        {
            subject = "Booking question",
            bookingId = (Guid?)null,
            isSupportThread = true,
            participants = new[]
            {
                new { userId = travelerId, displayName = "Guest Traveler", role = "Guest" },
                new { userId = hostId, displayName = "Island Host", role = "Host" }
            },
            initialMessage = "Can I check in after sunset?"
        });
        Assert.Equal(HttpStatusCode.OK, createConversation.StatusCode);
        var conversation = await createConversation.Content.ReadFromJsonAsync<ConversationResponse>();
        Assert.NotNull(conversation);
        Assert.Equal(2, conversation.Participants.Count);
        Assert.Single(conversation.Messages);

        var unpreparedAttachmentMessage = await client.PostAsJsonAsync($"/api/spec/messages/conversations/{conversation.Id}/messages?userId={travelerId}", new
        {
            body = "This arbitrary URL should not be trusted.",
            attachments = new[]
            {
                new
                {
                    fileName = "arrival-note.pdf",
                    contentType = "application/pdf",
                    sizeBytes = 2048,
                    url = "https://example.invalid/arrival-note.pdf",
                    status = "Uploaded"
                }
            }
        });
        Assert.Equal(HttpStatusCode.BadRequest, unpreparedAttachmentMessage.StatusCode);

        var invalidAttachmentUpload = await client.PostAsJsonAsync($"/api/spec/messages/conversations/{conversation.Id}/attachments/uploads?userId={travelerId}", new
        {
            fileName = "arrival-note.exe",
            contentType = "application/octet-stream",
            sizeBytes = 2048
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidAttachmentUpload.StatusCode);

        var rejectedPdfBytes = Encoding.ASCII.GetBytes("not a pdf");
        var rejectedUploadResponse = await client.PostAsJsonAsync($"/api/spec/messages/conversations/{conversation.Id}/attachments/uploads?userId={travelerId}", new
        {
            fileName = "spoofed.pdf",
            contentType = "application/pdf",
            sizeBytes = rejectedPdfBytes.Length
        });
        Assert.Equal(HttpStatusCode.OK, rejectedUploadResponse.StatusCode);
        var rejectedUpload = await rejectedUploadResponse.Content.ReadFromJsonAsync<AttachmentUploadResponse>();
        Assert.NotNull(rejectedUpload);
        using var rejectedContent = new ByteArrayContent(rejectedPdfBytes);
        rejectedContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        var rejectedUploadComplete = await client.PutAsync($"/api/spec/messages/conversations/{conversation.Id}/attachments/{rejectedUpload.Id}/content?userId={travelerId}", rejectedContent);
        Assert.Equal(HttpStatusCode.BadRequest, rejectedUploadComplete.StatusCode);

        var pdfBytes = Encoding.ASCII.GetBytes("%PDF-1.7\n1 0 obj\n<<>>\nendobj\n");
        var uploadResponse = await client.PostAsJsonAsync($"/api/spec/messages/conversations/{conversation.Id}/attachments/uploads?userId={travelerId}", new
        {
            fileName = "../Arrival Note.pdf",
            contentType = "application/pdf",
            sizeBytes = pdfBytes.Length
        });
        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);
        var upload = await uploadResponse.Content.ReadFromJsonAsync<AttachmentUploadResponse>();
        Assert.NotNull(upload);
        Assert.Equal("arrival-note.pdf", upload.FileName);
        Assert.Equal("PendingUpload", upload.Status);
        Assert.Equal("PendingScan", upload.ScanStatus);

        using var pdfContent = new ByteArrayContent(pdfBytes);
        pdfContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        var completedUploadResponse = await client.PutAsync($"/api/spec/messages/conversations/{conversation.Id}/attachments/{upload.Id}/content?userId={travelerId}", pdfContent);
        Assert.Equal(HttpStatusCode.OK, completedUploadResponse.StatusCode);
        var completedUpload = await completedUploadResponse.Content.ReadFromJsonAsync<AttachmentUploadResponse>();
        Assert.NotNull(completedUpload);
        Assert.Equal("Uploaded", completedUpload.Status);
        Assert.Equal("Clean", completedUpload.ScanStatus);

        var sendMessage = await client.PostAsJsonAsync($"/api/spec/messages/conversations/{conversation.Id}/messages?userId={travelerId}", new
        {
            body = "Attached is my arrival note.",
            attachments = new[]
            {
                new
                {
                    attachmentId = completedUpload.Id,
                    fileName = "ignored.svg",
                    contentType = "image/svg+xml",
                    sizeBytes = 1,
                    url = "https://example.invalid/ignored.svg",
                    status = "Uploaded"
                }
            }
        });
        Assert.Equal(HttpStatusCode.OK, sendMessage.StatusCode);
        var message = await sendMessage.Content.ReadFromJsonAsync<MessageResponse>();
        Assert.NotNull(message);
        Assert.Single(message.Attachments);
        var attachment = message.Attachments.Single();
        Assert.Equal(completedUpload.Id, attachment.AttachmentId);
        Assert.Equal("arrival-note.pdf", attachment.FileName);

        client.DefaultRequestHeaders.Authorization = LocalUser(otherTravelerId);
        var crossUserDownload = await client.GetAsync($"/api/spec/messages/conversations/{conversation.Id}/attachments/{attachment.AttachmentId}/download?userId={otherTravelerId}");
        Assert.Equal(HttpStatusCode.Unauthorized, crossUserDownload.StatusCode);

        client.DefaultRequestHeaders.Authorization = LocalUser(hostId, UserRole.Host);
        var attachmentDownload = await client.GetFromJsonAsync<AttachmentDownloadResponse>($"/api/spec/messages/conversations/{conversation.Id}/attachments/{attachment.AttachmentId}/download?userId={hostId}");
        Assert.NotNull(attachmentDownload);
        Assert.Equal("arrival-note.pdf", attachmentDownload.FileName);
        Assert.Contains("expires=", attachmentDownload.Url);

        var hostThread = await client.GetFromJsonAsync<ConversationResponse>($"/api/spec/messages/conversations/{conversation.Id}?userId={hostId}");
        Assert.NotNull(hostThread);
        Assert.Equal(2, hostThread.Messages.Count);
        var readThread = await client.PostAsync($"/api/spec/messages/conversations/{conversation.Id}/read?userId={hostId}", null);
        Assert.Equal(HttpStatusCode.NoContent, readThread.StatusCode);

        client.DefaultRequestHeaders.Authorization = null;
        var hostOpsNoToken = await client.GetAsync($"/api/spec/host/{hostId}/operations");
        Assert.Equal(HttpStatusCode.Unauthorized, hostOpsNoToken.StatusCode);

        client.DefaultRequestHeaders.Authorization = LocalUser(hostId, UserRole.Host);
        var hostOps = await client.GetFromJsonAsync<HostOperationsResponse>($"/api/spec/host/{hostId}/operations");
        Assert.NotNull(hostOps);
        Assert.True(hostOps.Analytics.Revenue > 0);
        Assert.NotEmpty(hostOps.PricingRules);

        var pricingRule = await client.PostAsJsonAsync($"/api/spec/host/{hostId}/pricing-rules", new
        {
            propertyId,
            name = "Summer weekend",
            startsOn = "2026-08-01",
            endsOn = "2026-08-31",
            nightlyRate = 225,
            minimumStay = 2,
            isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, pricingRule.StatusCode);

        var crossHostPricingRule = await client.PostAsJsonAsync($"/api/spec/host/{hostId}/pricing-rules", new
        {
            propertyId = Guid.NewGuid(),
            name = "Cross host attempt",
            startsOn = "2026-09-01",
            endsOn = "2026-09-05",
            nightlyRate = 225,
            minimumStay = 2,
            isActive = true
        });
        Assert.Equal(HttpStatusCode.NotFound, crossHostPricingRule.StatusCode);

        var invalidPromotion = await client.PostAsJsonAsync($"/api/spec/host/{hostId}/promotions", new
        {
            propertyId,
            name = "Impossible discount",
            discountPercent = 90,
            startsOn = "2026-08-01",
            endsOn = "2026-08-31",
            minimumNights = 2,
            badgeLevel = "Trusted",
            isActive = true
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidPromotion.StatusCode);

        var promotion = await client.PostAsJsonAsync($"/api/spec/host/{hostId}/promotions", new
        {
            propertyId,
            name = "Trusted long stay",
            discountPercent = 15,
            startsOn = "2026-08-01",
            endsOn = "2026-08-31",
            minimumNights = 4,
            badgeLevel = "Trusted",
            isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, promotion.StatusCode);

        client.DefaultRequestHeaders.Authorization = null;
        var adminNoToken = await client.GetAsync("/api/spec/admin/operations");
        Assert.Equal(HttpStatusCode.Unauthorized, adminNoToken.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.OperatorToken);
        var adminAsOperator = await client.GetAsync("/api/spec/admin/operations");
        Assert.Equal(HttpStatusCode.Forbidden, adminAsOperator.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.AdminToken);
        var adminOps = await client.GetFromJsonAsync<AdminOperationsResponse>("/api/spec/admin/operations");
        Assert.NotNull(adminOps);
        Assert.NotEmpty(adminOps.Cases);

        var adminCaseResponse = await client.PostAsJsonAsync("/api/spec/admin/cases", new
        {
            caseType = "Refund management",
            subjectType = "Booking",
            subjectId = propertyId,
            priority = "High",
            reason = "Refund requested after verification failure.",
            assignedTo = "Support lead"
        });
        Assert.Equal(HttpStatusCode.OK, adminCaseResponse.StatusCode);
        var adminCase = await adminCaseResponse.Content.ReadFromJsonAsync<AdminCaseResponse>();
        Assert.NotNull(adminCase);
        Assert.Equal("Open", adminCase.Status);
        Assert.Empty(adminCase.Evidence);

        client.DefaultRequestHeaders.Authorization = null;
        var evidenceWithoutToken = await client.PostAsJsonAsync($"/api/spec/admin/cases/{adminCase.Id}/evidence/uploads", new
        {
            fileName = "refund-evidence.pdf",
            contentType = "application/pdf",
            sizeBytes = 32
        });
        Assert.Equal(HttpStatusCode.Unauthorized, evidenceWithoutToken.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.OperatorToken);
        var evidenceAsOperator = await client.PostAsJsonAsync($"/api/spec/admin/cases/{adminCase.Id}/evidence/uploads", new
        {
            fileName = "refund-evidence.pdf",
            contentType = "application/pdf",
            sizeBytes = 32
        });
        Assert.Equal(HttpStatusCode.Forbidden, evidenceAsOperator.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.AdminToken);
        var invalidEvidence = await client.PostAsJsonAsync($"/api/spec/admin/cases/{adminCase.Id}/evidence/uploads", new
        {
            fileName = "refund-evidence.gif",
            contentType = "image/gif",
            sizeBytes = 32
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidEvidence.StatusCode);

        var spoofedEvidenceBytes = Encoding.ASCII.GetBytes("not a pdf");
        var spoofedEvidenceResponse = await client.PostAsJsonAsync($"/api/spec/admin/cases/{adminCase.Id}/evidence/uploads", new
        {
            fileName = "spoofed.pdf",
            contentType = "application/pdf",
            sizeBytes = spoofedEvidenceBytes.Length
        });
        Assert.Equal(HttpStatusCode.OK, spoofedEvidenceResponse.StatusCode);
        var spoofedEvidence = await spoofedEvidenceResponse.Content.ReadFromJsonAsync<AdminCaseEvidenceUploadResponse>();
        Assert.NotNull(spoofedEvidence);
        using var spoofedEvidenceContent = new ByteArrayContent(spoofedEvidenceBytes);
        spoofedEvidenceContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        var rejectedEvidenceUpload = await client.PutAsync($"/api/spec/admin/cases/{adminCase.Id}/evidence/{spoofedEvidence.Id}/content", spoofedEvidenceContent);
        Assert.Equal(HttpStatusCode.BadRequest, rejectedEvidenceUpload.StatusCode);

        var evidencePdfBytes = Encoding.ASCII.GetBytes("%PDF-1.7\nrefund evidence");
        var evidencePrepareResponse = await client.PostAsJsonAsync($"/api/spec/admin/cases/{adminCase.Id}/evidence/uploads", new
        {
            fileName = "refund evidence FINAL.pdf",
            contentType = "application/pdf",
            sizeBytes = evidencePdfBytes.Length
        });
        Assert.Equal(HttpStatusCode.OK, evidencePrepareResponse.StatusCode);
        var preparedEvidence = await evidencePrepareResponse.Content.ReadFromJsonAsync<AdminCaseEvidenceUploadResponse>();
        Assert.NotNull(preparedEvidence);
        Assert.Equal("refund-evidence-final.pdf", preparedEvidence.FileName);
        Assert.Equal("PendingUpload", preparedEvidence.Status);
        Assert.Equal("PendingScan", preparedEvidence.ScanStatus);

        using var evidenceContent = new ByteArrayContent(evidencePdfBytes);
        evidenceContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        var uploadedEvidenceResponse = await client.PutAsync($"/api/spec/admin/cases/{adminCase.Id}/evidence/{preparedEvidence.Id}/content", evidenceContent);
        Assert.Equal(HttpStatusCode.OK, uploadedEvidenceResponse.StatusCode);
        var uploadedEvidence = await uploadedEvidenceResponse.Content.ReadFromJsonAsync<AdminCaseEvidenceUploadResponse>();
        Assert.NotNull(uploadedEvidence);
        Assert.Equal("Uploaded", uploadedEvidence.Status);
        Assert.Equal("Clean", uploadedEvidence.ScanStatus);
        Assert.NotEmpty(uploadedEvidence.Sha256Hash ?? string.Empty);

        var adminOpsWithEvidence = await client.GetFromJsonAsync<AdminOperationsResponse>("/api/spec/admin/operations");
        Assert.NotNull(adminOpsWithEvidence);
        var refreshedCase = Assert.Single(adminOpsWithEvidence.Cases, item => item.Id == adminCase.Id);
        var evidence = Assert.Single(refreshedCase.Evidence);
        Assert.Equal(uploadedEvidence.Id, evidence.Id);
        Assert.Equal("refund-evidence-final.pdf", evidence.FileName);

        var evidenceDownloadResponse = await client.GetAsync($"/api/spec/admin/cases/{adminCase.Id}/evidence/{uploadedEvidence.Id}/download");
        Assert.Equal(HttpStatusCode.OK, evidenceDownloadResponse.StatusCode);
        var evidenceDownload = await evidenceDownloadResponse.Content.ReadFromJsonAsync<AdminCaseEvidenceDownloadResponse>();
        Assert.NotNull(evidenceDownload);
        Assert.Equal("refund-evidence-final.pdf", evidenceDownload.FileName);
        Assert.NotEmpty(evidenceDownload.Url);

        var resolvedResponse = await client.PostAsJsonAsync($"/api/spec/admin/cases/{adminCase.Id}/resolve", new
        {
            resolutionNotes = "Refund evidence reviewed and status updated.",
            status = "Resolved"
        });
        Assert.Equal(HttpStatusCode.OK, resolvedResponse.StatusCode);
        var resolved = await resolvedResponse.Content.ReadFromJsonAsync<AdminCaseResponse>();
        Assert.NotNull(resolved);
        Assert.Equal("Resolved", resolved.Status);

        var audit = await client.GetFromJsonAsync<List<AuditEventResponse>>("/api/spec/admin/audit-log");
        Assert.NotNull(audit);
        Assert.Contains(audit, item => item.Action == "AdminCaseEvidenceUploaded");
        Assert.Contains(audit, item => item.Action == "AdminCaseResolved");
    }

    private static AuthenticationHeaderValue LocalUser(Guid userId, params UserRole[] roles) =>
        new("Bearer", NestyStayApiFactory.UserToken(userId, roles));

    private static async Task<Guid> CreateHostPropertyAsync(HttpClient client, Guid hostId)
    {
        client.DefaultRequestHeaders.Authorization = LocalUser(hostId, UserRole.Host);
        var response = await client.PostAsJsonAsync("/api/properties", new
        {
            hostUserId = Guid.NewGuid(),
            hostName = "Spec Host",
            hostEmail = $"spec-host-{Guid.NewGuid():N}@test.local",
            title = $"Spec Host Property {Guid.NewGuid():N}",
            location = "Kingston",
            country = "Jamaica",
            nightlyRate = 180,
            currency = "USD",
            badgeLevel = "Trusted",
            guestVerificationEnabled = true,
            insuraGuestEnabled = true,
            cancellationPolicy = "Flexible",
            highlights = new[] { "Spec owned property" }
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var property = await response.Content.ReadFromJsonAsync<PropertyResponse>();
        Assert.NotNull(property);
        Assert.Equal(hostId, property.HostUserId);
        return property.Id;
    }

    private sealed record SpecSeedResponse(bool Seeded, int PublicPages, int Experiences, int JournalArticles, int HostProfiles, int DirectoryProviders);

    private sealed record PublicPageResponse(string Slug, string Body);

    private sealed record ContactResponse(string Email, string Status);

    private sealed record ExperienceResponse(string Slug, decimal Price, IReadOnlyList<string> Images);

    private sealed record JournalArticleResponse(string Slug);

    private sealed record AuthFlowResponse(Guid Id, string Status, int AttemptsRemaining);

    private sealed record DevelopmentAuthFlowSecretResponse(Guid Id, string Code, string Token, DateTimeOffset ExpiresAt);

    private sealed record RecoveryCodeResponse(string Code, bool Used);

    private sealed record SocialConfigResponse(
        bool GoogleEnabled,
        bool AppleEnabled,
        bool FacebookEnabled,
        IReadOnlyList<string> RequiredEnvironmentVariables);

    private sealed record PropertyResponse(Guid Id, Guid HostUserId);

    private sealed record TravelerWorkspaceResponse(
        IReadOnlyList<WishlistCollectionResponse> WishlistCollections,
        IReadOnlyList<IdentityDocumentResponse> IdentityDocuments,
        IReadOnlyList<TravelerNotificationResponse> Notifications);

    private sealed record WishlistCollectionResponse(Guid Id, string Name);

    private sealed record WishlistItemResponse(Guid Id, Guid PropertyId);

    private sealed record PaymentSetupIntentResponse(string SetupIntentReference, string ClientSecret);

    private sealed record PaymentMethodResponse(Guid Id, string ProviderPaymentMethodReference, bool IsDefault);

    private sealed record IdentityDocumentUploadResponse(Guid Id, string FileName, string Status, string ScanStatus, string? Sha256Hash, Guid? IdentityDocumentId);

    private sealed record IdentityDocumentResponse(Guid Id, string DocumentType, string FileName);

    private sealed record ReviewResponse(Guid Id, string? HostReply);

    private sealed record TravelerNotificationResponse(bool IsRead);

    private sealed record DirectoryProviderResponse(Guid? OwnerUserId, string Slug, string Name);

    private sealed record ConversationResponse(
        Guid Id,
        IReadOnlyList<ConversationParticipantResponse> Participants,
        IReadOnlyList<MessageResponse> Messages);

    private sealed record ConversationParticipantResponse(Guid UserId);

    private sealed record MessageResponse(IReadOnlyList<MessageAttachmentResponse> Attachments);

    private sealed record AttachmentUploadResponse(Guid Id, string FileName, string Status, string ScanStatus);

    private sealed record AttachmentDownloadResponse(string FileName, string Url);

    private sealed record MessageAttachmentResponse(Guid? AttachmentId, string FileName);

    private sealed record HostOperationsResponse(HostAnalyticsResponse Analytics, IReadOnlyList<HostPricingRuleResponse> PricingRules);

    private sealed record HostAnalyticsResponse(decimal Revenue);

    private sealed record HostPricingRuleResponse(Guid Id);

    private sealed record AdminOperationsResponse(IReadOnlyList<AdminCaseResponse> Cases);

    private sealed record AdminCaseResponse(Guid Id, string Status, IReadOnlyList<AdminCaseEvidenceResponse> Evidence);

    private sealed record AdminCaseEvidenceUploadResponse(Guid Id, string FileName, string Status, string ScanStatus, string? Sha256Hash);

    private sealed record AdminCaseEvidenceResponse(Guid Id, string FileName);

    private sealed record AdminCaseEvidenceDownloadResponse(string FileName, string Url);

    private sealed record AuditEventResponse(string Action);
}
