using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NestyStay.Domain;

namespace NestyStay.Api.Tests;

public sealed class PropertyManagerProfessionalWorkflowTests(NestyStayApiFactory factory) : IClassFixture<NestyStayApiFactory>
{
    [Fact]
    public async Task OwnerBlockRejectsReservationOverlapAndCanBeCancelled()
    {
        using var client = factory.CreateClient();
        var portfolio = await CreatePortfolio(client);
        var starts = DateTimeOffset.UtcNow.AddDays(10).Date.AddHours(10); var ends = starts.AddDays(2);
        var block = await Ok(client.PostAsJsonAsync("/api/property-manager/professional/owner-blocks", new { ownerUserId = portfolio.Owner, propertyId = portfolio.Property, startsAt = starts, endsAt = ends, timeZone = "America/Jamaica", reason = "Owner stay" }));
        Assert.Equal("ACTIVE", block.GetProperty("status").GetString());
        var duplicate = await client.PostAsJsonAsync("/api/property-manager/professional/owner-blocks", new { ownerUserId = portfolio.Owner, propertyId = portfolio.Property, startsAt = starts.AddHours(1), endsAt = ends.AddHours(1), reason = "Overlap" });
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        var cancelled = await Ok(client.PostAsJsonAsync($"/api/property-manager/professional/owner-blocks/{block.GetProperty("id").GetGuid()}/cancel", new { reason = "Owner changed plans", rowVersion = block.GetProperty("rowVersion").GetInt64() }));
        Assert.Equal("CANCELLED", cancelled.GetProperty("status").GetString());

        using var ownerClient = factory.CreateClient(); SignIn(ownerClient, portfolio.Owner, UserRole.Owner);
        var ownerBlock = await Ok(ownerClient.PostAsJsonAsync("/api/property-manager/owner/owner-blocks", new { propertyId = portfolio.Property, startsAt = starts.AddDays(5), endsAt = ends.AddDays(5), reason = "Owner weekend", category = "PERSONAL", notes = "Owner-only operational request" }));
        Assert.Equal("PERSONAL", ownerBlock.GetProperty("category").GetString());
        Assert.NotEmpty((await ownerClient.GetFromJsonAsync<JsonElement[]>("/api/property-manager/owner/owner-blocks")) ?? []);
    }

    [Fact]
    public async Task MaintenanceLifecycleAndAssetIncidentInspectionPersist()
    {
        using var client = factory.CreateClient(); var portfolio = await CreatePortfolio(client);
        var maintenance = await Ok(client.PostAsJsonAsync("/api/property-manager/professional/maintenance", new { ownerUserId = portfolio.Owner, propertyId = portfolio.Property, title = "Leaking tap", description = "Kitchen tap", priority = "HIGH" }));
        var id = maintenance.GetProperty("id").GetGuid();
        var next = await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/maintenance/{id}", new { status = "TRIAGED", rowVersion = maintenance.GetProperty("rowVersion").GetInt64(), details = "Reviewed by manager" }));
        Assert.Equal("TRIAGED", next.GetProperty("status").GetString());
        var vendor = await Ok(client.PostAsJsonAsync("/api/property-manager/vendors", new { name = "Trusted Plumbing", category = "PLUMBING", contact = "ops@trusted.example", notes = "Preferred vendor" }));
        var quoting = await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/maintenance/{id}", new { status = "QUOTING", rowVersion = next.GetProperty("rowVersion").GetInt64(), details = "Solicit comparable bids" }));
        var quote = await Ok(client.PostAsJsonAsync($"/api/property-manager/professional/maintenance/{id}/quotes", new { vendorId = vendor.GetProperty("id").GetGuid(), amount = 180.00m, scope = "Replace tap", currency = "JMD" }));
        var quotes = await client.GetFromJsonAsync<JsonElement[]>($"/api/property-manager/professional/maintenance/{id}/quotes");
        Assert.NotNull(quotes); Assert.Contains(quotes!, item => item.GetProperty("id").GetGuid() == quote.GetProperty("id").GetGuid());
        var assigned = await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/maintenance/{id}", new { status = "ASSIGNED", vendorId = vendor.GetProperty("id").GetGuid(), approvedAmount = 180.00m, rowVersion = quoting.GetProperty("rowVersion").GetInt64(), details = "Selected lowest valid quote" }));
        Assert.Equal("ASSIGNED", assigned.GetProperty("status").GetString());
        var asset = await Ok(client.PostAsJsonAsync("/api/property-manager/professional/assets", new { propertyId = portfolio.Property, assetTag = "K-001", name = "Kitchen tap", description = "Chrome replacement tap", serialReference = "TAP-001", purchaseDate = "2026-01-10", purchaseCost = 125.50m, warrantyExpiry = "2027-01-10", condition = "GOOD", category = "FIXTURE", quantity = 1 }));
        Assert.Equal("TAP-001", asset.GetProperty("serialReference").GetString()); Assert.Equal(125.50m, asset.GetProperty("purchaseCost").GetDecimal());
        var assetUpdate = await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/assets/{asset.GetProperty("id").GetGuid()}", new { status = "DAMAGED", quantity = 1, location = "Kitchen", metadataJson = "{}", photosJson = "[]", rowVersion = asset.GetProperty("rowVersion").GetInt64() }));
        Assert.Equal("DAMAGED", assetUpdate.GetProperty("status").GetString());
        using (var staleAsset = await client.PatchAsJsonAsync($"/api/property-manager/professional/assets/{asset.GetProperty("id").GetGuid()}", new { status = "RETIRED", quantity = 1, location = "Kitchen", metadataJson = "{}", photosJson = "[]", rowVersion = asset.GetProperty("rowVersion").GetInt64() })) Assert.Equal(HttpStatusCode.Conflict, staleAsset.StatusCode);
        var incident = await Ok(client.PostAsJsonAsync("/api/property-manager/professional/incidents", new { propertyId = portfolio.Property, incidentType = "DAMAGE", severity = "HIGH", occurredAt = DateTimeOffset.UtcNow, description = "Water leak" }));
        var inspection = await Ok(client.PostAsJsonAsync("/api/property-manager/professional/inspections", new { propertyId = portfolio.Property, inspectionType = "MOVE_OUT", scheduledAt = DateTimeOffset.UtcNow.AddDays(1), checklistJson = "[]" }));
        await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/incidents/{incident.GetProperty("id").GetGuid()}", new { status = "RESOLVED", actionTaken = "Valve closed", followUp = "Replace tap", rowVersion = incident.GetProperty("rowVersion").GetInt64() }));
        _ = await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/inspections/{inspection.GetProperty("id").GetGuid()}", new { status = "SIGNED_OFF", evidenceJson = "[]", findingsJson = "[{\"severity\":\"HIGH\",\"finding\":\"Tap leak\"}]", rowVersion = inspection.GetProperty("rowVersion").GetInt64() }));
        var correctiveWorkOrder = await Ok(client.PostAsJsonAsync($"/api/property-manager/professional/inspections/{inspection.GetProperty("id").GetGuid()}/work-order", new { scope = "Replace leaking tap" }));
        Assert.Equal(portfolio.Property, correctiveWorkOrder.GetProperty("propertyId").GetGuid());
        var refreshedInspection = (await client.GetFromJsonAsync<JsonElement[]>("/api/property-manager/professional/inspections"))!.Single(item => item.GetProperty("id").GetGuid() == inspection.GetProperty("id").GetGuid());
        Assert.Equal(correctiveWorkOrder.GetProperty("id").GetGuid(), refreshedInspection.GetProperty("correctiveWorkOrderId").GetGuid());
        Assert.NotEmpty((await client.GetFromJsonAsync<JsonElement[]>("/api/property-manager/professional/assets"))!);
        Assert.NotEmpty((await client.GetFromJsonAsync<JsonElement[]>("/api/property-manager/professional/incidents"))!);
        Assert.NotEmpty((await client.GetFromJsonAsync<JsonElement[]>("/api/property-manager/professional/inspections"))!);
    }

    private async Task<(Guid Manager, Guid Owner, Guid Property)> CreatePortfolio(HttpClient client)
    {
        var email = $"professional-{Guid.NewGuid():N}@nestystay.local";
        var registered = await Ok(client.PostAsJsonAsync("/api/auth/register", new { email, password = "NestyStay1", confirmPassword = "NestyStay1", displayName = "Professional owner", phone = "+15550104003", acceptedTerms = true, acceptedPrivacy = true, role = "Owner" }));
        var owner = registered.GetProperty("userId").GetGuid(); var manager = Guid.NewGuid(); SignIn(client, manager, UserRole.PropertyManager);
        await Ok(client.PostAsJsonAsync("/api/property-manager/owners", new { email, displayName = "Professional owner" }));
        var property = await Ok(client.PostAsJsonAsync("/api/property-manager/properties", new { ownerUserId = owner, title = "Professional unit", unitNumber = "P-1", address = "Kingston" }));
        return (manager, owner, property.GetProperty("id").GetGuid());
    }
    private static void SignIn(HttpClient client, Guid userId, UserRole role) => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(userId, role));
    private static async Task<JsonElement> Ok(Task<HttpResponseMessage> action) { using var response = await action; var body = await response.Content.ReadAsStringAsync(); Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {body}"); return JsonDocument.Parse(body).RootElement.Clone(); }
}
