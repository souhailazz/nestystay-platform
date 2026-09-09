using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NestyStay.Domain;

namespace NestyStay.Api.Tests;

public sealed class PropertyEnhancementEndpointTests : IClassFixture<NestyStayApiFactory>
{
    private readonly NestyStayApiFactory factory;

    public PropertyEnhancementEndpointTests(NestyStayApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task HostCanDuplicatePublishAndReadPropertyRevisions()
    {
        using var client = factory.CreateClient();
        var hostId = Guid.NewGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            NestyStayApiFactory.UserToken(hostId, UserRole.Host));

        var createResponse = await client.PostAsJsonAsync("/api/properties", new
        {
            hostUserId = hostId,
            hostName = "Revision Test Host",
            hostEmail = $"{hostId:N}@test.local",
            title = $"Revision test {hostId:N}",
            location = "Kingston",
            country = "Jamaica",
            nightlyRate = 150,
            currency = "USD",
            badgeLevel = "Free",
            guestVerificationEnabled = false,
            insuraGuestEnabled = false,
            cancellationPolicy = "Flexible",
            highlights = new[] { "Test listing" }
        });
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        using var createdJson = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var sourceId = createdJson.RootElement.GetProperty("id").GetGuid();

        var duplicateResponse = await client.PostAsJsonAsync($"/api/properties/{sourceId}/duplicate", new { title = "Draft copy" });
        Assert.Equal(HttpStatusCode.OK, duplicateResponse.StatusCode);
        using var duplicateJson = JsonDocument.Parse(await duplicateResponse.Content.ReadAsStringAsync());
        var duplicateId = duplicateJson.RootElement.GetProperty("id").GetGuid();
        Assert.True(duplicateJson.RootElement.GetProperty("isDraft").GetBoolean());

        var ownedWithDraft = await client.GetFromJsonAsync<List<PropertyResponse>>("/api/properties/owned");
        Assert.NotNull(ownedWithDraft);
        Assert.Contains(ownedWithDraft, property => property.Id == duplicateId && property.IsDraft);

        var publicDraftResponse = await client.GetAsync($"/api/properties/{duplicateId}");
        Assert.Equal(HttpStatusCode.NotFound, publicDraftResponse.StatusCode);

        var revisions = await client.GetFromJsonAsync<List<PropertyRevisionResponse>>($"/api/properties/{duplicateId}/revisions");
        Assert.NotNull(revisions);
        var firstRevision = Assert.Single(revisions, revision => revision.Version == 1);

        var restoreResponse = await client.PostAsync($"/api/properties/{duplicateId}/revisions/{firstRevision.Id}/restore", null);
        Assert.Equal(HttpStatusCode.OK, restoreResponse.StatusCode);
        using var restoredJson = JsonDocument.Parse(await restoreResponse.Content.ReadAsStringAsync());
        Assert.True(restoredJson.RootElement.GetProperty("isDraft").GetBoolean());

        var publishResponse = await client.PostAsync($"/api/properties/{duplicateId}/publish", null);
        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);
        using var publishedJson = JsonDocument.Parse(await publishResponse.Content.ReadAsStringAsync());
        Assert.False(publishedJson.RootElement.GetProperty("isDraft").GetBoolean());

        var publicPublishedResponse = await client.GetAsync($"/api/properties/{duplicateId}");
        Assert.Equal(HttpStatusCode.OK, publicPublishedResponse.StatusCode);

        var bulkArchiveResponse = await client.PostAsJsonAsync("/api/properties/bulk/archive", new
        {
            propertyIds = new[] { sourceId, duplicateId },
            isArchived = true
        });
        Assert.Equal(HttpStatusCode.OK, bulkArchiveResponse.StatusCode);
        var archived = await bulkArchiveResponse.Content.ReadFromJsonAsync<List<PropertyResponse>>();
        Assert.NotNull(archived);
        Assert.Equal(2, archived.Count);
        Assert.All(archived, property => Assert.True(property.IsArchived));
    }

    private sealed record PropertyResponse(Guid Id, bool IsArchived, bool IsDraft);
    private sealed record PropertyRevisionResponse(Guid Id, Guid PropertyId, int Version, string SnapshotJson, DateTimeOffset CreatedAt, Guid? CreatedByUserId);
}
