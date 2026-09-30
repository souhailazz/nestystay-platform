using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NestyStay.Domain;
using NestyStay.Infrastructure.Persistence;
using NestyStay.Infrastructure.Persistence.Milestones;

namespace NestyStay.Api.Tests;

/// <summary>
/// Focused cross-resource authorization coverage for the locally testable
/// message, Wellness, provider, and administrator boundaries.
/// </summary>
public sealed class CrossResourceAuthorizationMatrixTests(NestyStayApiFactory factory) : IClassFixture<NestyStayApiFactory>
{
    [Fact]
    public async Task ConversationsAndAttachmentsAreRestrictedToParticipants()
    {
        var userA = await RegisterAsync("Message A");
        var userB = await RegisterAsync("Message B");
        var userC = await RegisterAsync("Message C");

        using var clientA = SignedClient(userA.UserId, UserRole.Guest);
        var conversation = await ReadObjectAsync(await clientA.PostAsJsonAsync("/api/spec/messages/conversations?userId=" + userA.UserId, new
        {
            subject = "Private conversation",
            bookingId = (Guid?)null,
            isSupportThread = false,
            participants = new[]
            {
                new { userId = userA.UserId, displayName = userA.DisplayName, role = "Guest" },
                new { userId = userB.UserId, displayName = userB.DisplayName, role = "Guest" }
            },
            initialMessage = "A private message"
        }));
        var conversationId = conversation.GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.OK, (await clientA.GetAsync($"/api/spec/messages/conversations/{conversationId}?userId={userA.UserId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await clientA.GetAsync($"/api/spec/messages/inbox?userId={userA.UserId}")).StatusCode);

        using var clientB = SignedClient(userB.UserId, UserRole.Guest);
        Assert.Equal(HttpStatusCode.OK, (await clientB.GetAsync($"/api/spec/messages/conversations/{conversationId}?userId={userB.UserId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await clientB.PostAsJsonAsync($"/api/spec/messages/conversations/{conversationId}/messages?userId={userB.UserId}", new { body = "B can reply", attachments = Array.Empty<object>() })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await clientB.PostAsync($"/api/spec/messages/conversations/{conversationId}/read?userId={userB.UserId}", null)).StatusCode);

        var bytes = Encoding.ASCII.GetBytes("%PDF-1.7\nNestyStay private attachment\n");
        var prepared = await ReadObjectAsync(await clientA.PostAsJsonAsync($"/api/spec/messages/conversations/{conversationId}/attachments/uploads?userId={userA.UserId}", new
        {
            fileName = "private-evidence.pdf",
            contentType = "application/pdf",
            sizeBytes = bytes.Length
        }));
        var attachmentId = prepared.GetProperty("id").GetGuid();
        using (var content = new ByteArrayContent(bytes))
        {
            content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            Assert.Equal(HttpStatusCode.OK, (await clientA.PutAsync($"/api/spec/messages/conversations/{conversationId}/attachments/{attachmentId}/content?userId={userA.UserId}", content)).StatusCode);
        }

        var sent = await ReadObjectAsync(await clientA.PostAsJsonAsync($"/api/spec/messages/conversations/{conversationId}/messages?userId={userA.UserId}", new
        {
            body = "Attachment message",
            attachments = new[] { new { attachmentId, fileName = "private-evidence.pdf", contentType = "application/pdf", sizeBytes = bytes.Length, url = (string?)null, status = "Uploaded" } }
        }));
        Assert.Contains(sent.GetProperty("attachments").EnumerateArray(), item => item.GetProperty("attachmentId").GetGuid() == attachmentId);

        Assert.Equal(HttpStatusCode.OK, (await clientB.GetAsync($"/api/spec/messages/conversations/{conversationId}/attachments/{attachmentId}/download?userId={userB.UserId}")).StatusCode);

        using var clientC = SignedClient(userC.UserId, UserRole.Guest);
        using (var cInbox = await clientC.GetAsync($"/api/spec/messages/inbox?userId={userC.UserId}"))
        {
            Assert.Equal(HttpStatusCode.OK, cInbox.StatusCode);
            Assert.DoesNotContain(conversationId.ToString(), await cInbox.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        }
        AssertDenied(await clientC.GetAsync($"/api/spec/messages/conversations/{conversationId}?userId={userC.UserId}"), "C conversation read");
        AssertDenied(await clientC.PostAsJsonAsync($"/api/spec/messages/conversations/{conversationId}/messages?userId={userC.UserId}", new { body = "spoof", attachments = Array.Empty<object>() }), "C send message");
        AssertDenied(await clientC.PostAsync($"/api/spec/messages/conversations/{conversationId}/read?userId={userC.UserId}", null), "C mark read");
        AssertDenied(await clientC.GetAsync($"/api/spec/messages/conversations/{conversationId}/attachments/{attachmentId}/download?userId={userC.UserId}"), "C attachment download");
    }

    [Fact]
    public async Task WellnessVisitsReportsPhotosAndOfficerDocumentsAreScoped()
    {
        var hostA = Guid.NewGuid();
        var hostB = Guid.NewGuid();
        var property = await CreateWellnessPropertyAsync(hostA);
        var officerA = await CreateOfficerAsync("Officer A");
        var officerB = await CreateOfficerAsync("Officer B");

        using var admin = factory.CreateClient();
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.AdminToken);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"/api/wellness/officers/{officerA.OfficerId}/approve", new { reason = "QA approval" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"/api/wellness/officers/{officerB.OfficerId}/approve", new { reason = "QA approval" })).StatusCode);

        using var hostAClient = SignedClient(hostA, UserRole.Host);
        var visitA = await ReadObjectAsync(await hostAClient.PostAsJsonAsync("/api/wellness/visits", new
        {
            hostUserId = hostA,
            propertyId = property,
            visitType = "StandardWellnessCheck",
            scheduledAt = DateTimeOffset.UtcNow.AddDays(2),
            parish = "St. Ann",
            area = "Ocho Rios"
        }));
        var visitAId = visitA.GetProperty("id").GetGuid();

        var visitB = await ReadObjectAsync(await hostAClient.PostAsJsonAsync("/api/wellness/visits", new
        {
            hostUserId = hostA,
            propertyId = property,
            visitType = "StandardWellnessCheck",
            scheduledAt = DateTimeOffset.UtcNow.AddDays(4),
            parish = "St. Ann",
            area = "Ocho Rios"
        }));
        var visitBId = visitB.GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"/api/wellness/visits/{visitAId}/assign", new { officerId = officerA.OfficerId })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"/api/wellness/visits/{visitBId}/assign", new { officerId = officerB.OfficerId })).StatusCode);

        using var officerAClient = SignedClient(officerA.UserId, UserRole.Officer);
        Assert.Equal(HttpStatusCode.OK, (await officerAClient.GetAsync($"/api/wellness/visits/{visitAId}")).StatusCode);
        AssertDenied(await officerAClient.GetAsync($"/api/wellness/visits/{visitBId}"), "Officer A visit B");
        AssertDenied(await officerAClient.GetAsync($"/api/wellness/visits/{visitBId}/report"), "Officer A report B");
        AssertDenied(await officerAClient.PostAsJsonAsync($"/api/wellness/visits/{visitBId}/report/photos/uploads", new { officerBadgeNumber = officerB.BadgeNumber, fileName = "visit-b.png", contentType = "image/png", sizeBytes = 4 }), "Officer A photo B");

        var reportId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NestyStayDbContext>();
            db.MilestoneWellnessReports.Add(new MilestoneWellnessReport
            {
                Id = reportId,
                VisitId = visitAId,
                OfficerId = officerA.OfficerId,
                SubmittedAt = DateTimeOffset.UtcNow,
                ReportStatus = "Submitted",
                Notes = "Seeded only to exercise report access control.",
                PhotosJson = "[]",
                LocationMetadataJson = "{}"
            });
            var visit = await db.MilestoneWellnessVisits.SingleAsync(item => item.Id == visitAId);
            visit.ReportStatus = "Submitted";
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.OK, (await officerAClient.GetAsync($"/api/wellness/visits/{visitAId}/report")).StatusCode);

        using var officerBClient = SignedClient(officerB.UserId, UserRole.Officer);
        AssertDenied(await officerBClient.GetAsync($"/api/wellness/reports/{reportId}/collaboration"), "Officer B report collaboration A");
        AssertDenied(await officerBClient.GetAsync($"/api/wellness/reports/{reportId}/pdf"), "Officer B report PDF A");
        AssertDenied(await officerBClient.GetAsync($"/api/wellness/officers/{officerA.OfficerId}/documents"), "Officer B officer A documents");

        using var hostBClient = SignedClient(hostB, UserRole.Host);
        AssertDenied(await hostBClient.GetAsync($"/api/wellness/visits/{visitAId}"), "Host B visit A");
        AssertDenied(await hostBClient.GetAsync($"/api/wellness/visits/{visitAId}/report"), "Host B report A");

        using var guest = SignedClient(Guid.NewGuid(), UserRole.Guest);
        AssertDenied(await guest.GetAsync($"/api/wellness/visits/{visitAId}/report"), "Guest report A");
    }

    [Fact]
    public async Task ProviderPrivateInsightsDocumentsAndBusinessFieldsAreScoped()
    {
        var providerA = await CreateProviderAsync("Provider A");
        var providerB = await CreateProviderAsync("Provider B");

        using var clientA = SignedClient(providerA.UserId, UserRole.ServiceProvider);
        var details = await clientA.PutAsJsonAsync($"/api/directories/providers/{providerA.Slug}/business-details", new
        {
            weeklyHoursJson = "{\"monday\":\"08:00-17:00\"}",
            holidayClosuresJson = "[]",
            promotionsJson = "[]",
            accessibilityInfo = "Private provider operational detail"
        });
        Assert.Equal(HttpStatusCode.OK, details.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await clientA.GetAsync($"/api/directories/providers/{providerA.Slug}/insights")).StatusCode);

        var prepared = await ReadObjectAsync(await clientA.PostAsJsonAsync($"/api/spec/directories/providers/{providerA.ProviderId}/documents/uploads", new
        {
            documentType = "BUSINESS_LICENSE",
            fileName = "provider-a.pdf",
            contentType = "application/pdf",
            sizeBytes = 20
        }));
        var documentId = prepared.GetProperty("id").GetGuid();

        using var clientB = SignedClient(providerB.UserId, UserRole.ServiceProvider);
        AssertDenied(await clientB.GetAsync($"/api/directories/providers/{providerA.Slug}/insights"), "Provider B insights A");
        AssertDenied(await clientB.PutAsJsonAsync($"/api/directories/providers/{providerA.Slug}/business-details", new { weeklyHoursJson = "{}", holidayClosuresJson = "[]", promotionsJson = "[]", accessibilityInfo = "spoof" }), "Provider B business details A");
        AssertDenied(await clientB.GetAsync($"/api/spec/directories/providers/{providerA.ProviderId}/documents"), "Provider B document list A");
        AssertDenied(await clientB.GetAsync($"/api/spec/directories/providers/{providerA.ProviderId}/documents/{documentId}/download"), "Provider B document download A");
    }

    [Fact]
    public async Task EveryAdminPolicyEndpointRejectsAllNonAdminRoles()
    {
        var apiExplorer = factory.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>();
        var descriptions = apiExplorer.ApiDescriptionGroups.Items
            .SelectMany(group => group.Items)
            .Where(item => item.RelativePath?.StartsWith("api/", StringComparison.OrdinalIgnoreCase) == true)
            .Where(item => item.ActionDescriptor.EndpointMetadata?.OfType<IAllowAnonymous>().Any() != true)
            .Where(item => item.ActionDescriptor.EndpointMetadata?.OfType<IAuthorizeData>().Any(IsAdminOnly) == true)
            .ToList();

        Assert.True(descriptions.Count > 0);
        var tested = 0;
        foreach (var description in descriptions)
        {
            var path = BuildProbePath(description.RelativePath!);
            foreach (var role in Enum.GetValues<UserRole>().Where(role => role != UserRole.Admin))
            {
                using var client = SignedClient(Guid.NewGuid(), role);
                using var request = new HttpRequestMessage(new HttpMethod(description.HttpMethod ?? "GET"), path);
                if (!string.Equals(description.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(description.HttpMethod, "HEAD", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(description.HttpMethod, "DELETE", StringComparison.OrdinalIgnoreCase))
                {
                    request.Content = JsonContent.Create(new { });
                }

                using var response = await client.SendAsync(request);
                Assert.False(response.IsSuccessStatusCode, $"{role} unexpectedly received {(int)response.StatusCode} from {description.HttpMethod} /{description.RelativePath}");
                tested++;
            }
        }

        Assert.True(tested > 0);
    }

    private static bool IsAdminOnly(IAuthorizeData data) =>
        data.Policy?.StartsWith("Admin.", StringComparison.OrdinalIgnoreCase) == true ||
        (data.Roles?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) is { Length: 1 } roles && roles[0].Equals("Admin", StringComparison.OrdinalIgnoreCase));

    private async Task<RegisteredUser> CreateOfficerAsync(string label)
    {
        var user = await RegisterAsync(label, "Officer");
        using var client = SignedClient(user.UserId, UserRole.Officer);
        var response = await client.PostAsJsonAsync("/api/wellness/officers", new
        {
            userId = user.UserId,
            badgeNumber = $"JCF-{Guid.NewGuid():N}"[..16],
            parish = "St. Ann",
            coverageArea = "Ocho Rios",
            isActiveOffDuty = true,
            isRetired = false,
            verificationMetadata = "Focused authorization test"
        });
        var body = await ReadObjectAsync(response);
        return user with { OfficerId = body.GetProperty("id").GetGuid(), BadgeNumber = body.GetProperty("badgeNumber").GetString()! };
    }

    private async Task<Guid> CreateWellnessPropertyAsync(Guid hostUserId)
    {
        using var client = SignedClient(hostUserId, UserRole.Host);
        var response = await client.PostAsJsonAsync("/api/properties", new
        {
            hostUserId,
            hostName = "Wellness QA Host",
            hostEmail = $"wellness-host-{hostUserId:N}@nestystay.local",
            title = $"Wellness QA property {Guid.NewGuid():N}",
            location = "Ocho Rios",
            country = "Jamaica",
            nightlyRate = 180m,
            currency = "USD",
            badgeLevel = "Wellness",
            parish = "St. Ann",
            guestVerificationEnabled = false,
            insuraGuestEnabled = false,
            cancellationPolicy = "Flexible"
        });
        return (await ReadObjectAsync(response)).GetProperty("id").GetGuid();
    }

    private async Task<ProviderAccount> CreateProviderAsync(string label)
    {
        var user = await RegisterAsync(label, "ServiceProvider");
        using var client = SignedClient(user.UserId, UserRole.ServiceProvider);
        var response = await client.PostAsJsonAsync("/api/directories/providers", new
        {
            kind = "Trades",
            category = "Electrician",
            name = $"{label} private profile {Guid.NewGuid():N}",
            parish = "St. Ann",
            badgeLevel = "Free",
            description = "Private provider authorization fixture",
            availabilitySummary = "Weekdays",
            contactMode = "Platform messaging only",
            isBrickAndMortar = false
        });
        var body = await ReadObjectAsync(response);
        return new ProviderAccount(user.UserId, body.GetProperty("id").GetGuid(), body.GetProperty("slug").GetString()!);
    }

    private async Task<RegisteredUser> RegisterAsync(string displayName, string role = "Guest")
    {
        using var client = factory.CreateClient();
        var email = $"focused-{Guid.NewGuid():N}@nestystay.local";
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "NestyStay1",
            confirmPassword = "NestyStay1",
            displayName,
            phone = $"+1555{Random.Shared.Next(1000000, 9999999)}",
            acceptedTerms = true,
            acceptedPrivacy = true,
            role
        });
        var body = await ReadObjectAsync(response);
        return new RegisteredUser(body.GetProperty("userId").GetGuid(), email, displayName);
    }

    private HttpClient SignedClient(Guid userId, UserRole role)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(userId, role));
        return client;
    }

    private static void AssertDenied(HttpResponseMessage response, string operation)
    {
        using (response)
        {
            Assert.False(response.IsSuccessStatusCode, $"{operation} unexpectedly returned {(int)response.StatusCode}.");
        }
    }

    private static async Task<JsonElement> ReadObjectAsync(HttpResponseMessage response)
    {
        using (response)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {body}");
            return JsonDocument.Parse(body).RootElement.Clone();
        }
    }

    private static string BuildProbePath(string relativePath)
    {
        var path = System.Text.RegularExpressions.Regex.Replace(relativePath, @"\{[^}:]+(?::[^}]+)?\}", _ => Guid.NewGuid().ToString());
        path = path.Replace("{area}", "assets", StringComparison.OrdinalIgnoreCase).Replace("{format}", "json", StringComparison.OrdinalIgnoreCase);
        return "/" + path;
    }

    private sealed record RegisteredUser(Guid UserId, string Email, string DisplayName, Guid OfficerId = default, string BadgeNumber = "");
    private sealed record ProviderAccount(Guid UserId, Guid ProviderId, string Slug);
}
