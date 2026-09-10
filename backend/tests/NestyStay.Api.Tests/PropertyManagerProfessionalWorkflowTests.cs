using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NestyStay.Domain;

namespace NestyStay.Api.Tests;

public sealed class PropertyManagerProfessionalWorkflowTests(NestyStayApiFactory factory) : IClassFixture<NestyStayApiFactory>
{
    [Fact]
    public async Task ManagedPropertyCanLinkOnlyItsOwnersRentalListing()
    {
        using var client = factory.CreateClient();
        var portfolio = await CreatePortfolio(client);
        using var ownerClient = factory.CreateClient();
        SignIn(ownerClient, portfolio.Owner, UserRole.Host);
        var listing = await Ok(ownerClient.PostAsJsonAsync("/api/properties", new { hostUserId = portfolio.Owner, hostName = "Owner", hostEmail = portfolio.OwnerEmail, title = "Rental listing", location = "Kingston", country = "Jamaica", nightlyRate = 100, currency = "JMD", badgeLevel = "Free", cancellationPolicy = "Flexible" }));
        var listingId = listing.GetProperty("id").GetGuid();
        var linked = await Ok(client.PatchAsJsonAsync($"/api/property-manager/properties/{portfolio.Property}/rental-listing", new { rentalListingId = listingId }));
        Assert.Equal(listingId, linked.GetProperty("rentalListingId").GetGuid());
        var second = await Ok(client.PostAsJsonAsync("/api/property-manager/properties", new { ownerUserId = portfolio.Owner, title = "Second managed property", unitNumber = "P-2", address = "Kingston" }));
        var rejected = await client.PatchAsJsonAsync($"/api/property-manager/properties/{second.GetProperty("id").GetGuid()}/rental-listing", new { rentalListingId = listingId });
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);

        var guestEmail = $"guest-{Guid.NewGuid():N}@nestystay.local";
        var guest = await Ok(ownerClient.PostAsJsonAsync("/api/auth/register", new { email = guestEmail, password = "NestyStay1", confirmPassword = "NestyStay1", displayName = "Guest", acceptedTerms = true, acceptedPrivacy = true, role = "Guest" }));
        var guestId = guest.GetProperty("userId").GetGuid();
        SignIn(ownerClient, guestId, UserRole.Guest);
        var booking = await Ok(ownerClient.PostAsJsonAsync("/api/bookings", new { propertyId = listingId, guestUserId = guestId, checkIn = "2099-03-10", checkOut = "2099-03-12" }));
        var bookingId = booking.GetProperty("id").GetGuid();
        SignIn(client, portfolio.Manager, UserRole.PropertyManager);
        var listed = await client.GetFromJsonAsync<JsonElement[]>("/api/property-manager/professional/reservations");
        Assert.Contains(listed!, item => item.GetProperty("bookingId").GetGuid() == bookingId);
        var preview = await Ok(client.PostAsJsonAsync($"/api/property-manager/professional/reservations/{bookingId}/date-change-preview", new { checkIn = "2099-03-10", checkOut = "2099-03-13" }));
        Assert.False(preview.GetProperty("allowed").GetBoolean());
        Assert.Contains("cancelled and rebooked", preview.GetProperty("blockingReason").GetString(), StringComparison.OrdinalIgnoreCase);
        var directCancel = await client.PatchAsJsonAsync($"/api/property-manager/professional/reservations/{bookingId}", new { status = "CANCELLED" });
        Assert.Equal(HttpStatusCode.BadRequest, directCancel.StatusCode);
        var cancelled = await Ok(client.PostAsJsonAsync($"/api/property-manager/professional/reservations/{bookingId}/cancel", new { reason = "Guest requested cancellation", idempotencyKey = $"cancel-{bookingId:N}" }));
        Assert.Equal("Cancelled", cancelled.GetProperty("status").GetString());
        var repeated = await Ok(client.PostAsJsonAsync($"/api/property-manager/professional/reservations/{bookingId}/cancel", new { reason = "Guest requested cancellation", idempotencyKey = $"cancel-{bookingId:N}" }));
        Assert.Equal(bookingId, repeated.GetProperty("bookingId").GetGuid());
        var history = await client.GetFromJsonAsync<JsonElement[]>($"/api/property-manager/professional/reservations/{bookingId}/history");
        Assert.Contains(history!, item => item.GetProperty("eventType").GetString() == "CANCELLED");
    }

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
        var blockHistory = await client.GetFromJsonAsync<JsonElement[]>($"/api/property-manager/professional/owner-blocks/{block.GetProperty("id").GetGuid()}/history");
        Assert.NotNull(blockHistory); Assert.Contains(blockHistory!, item => item.GetProperty("action").GetString() == "OwnerBlockCancelled");

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
        var approval = await Ok(client.PostAsJsonAsync("/api/property-manager/p0/approvals", new { ownerUserId = portfolio.Owner, propertyId = portfolio.Property, approvalType = "MAINTENANCE", description = "Replace leaking tap", amount = 180.00m, currency = "JMD" }));
        using var ownerClient = factory.CreateClient();
        var ownerLogin = await ownerClient.PostAsJsonAsync("/api/auth/login", new { email = portfolio.OwnerEmail, password = portfolio.Password });
        Assert.True(ownerLogin.IsSuccessStatusCode, await ownerLogin.Content.ReadAsStringAsync());
        var ownerToken = (await ownerLogin.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        ownerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var decided = await Ok(ownerClient.PostAsJsonAsync($"/api/property-manager/p0/approvals/{approval.GetProperty("id").GetGuid()}/decision", new { status = "APPROVED", reason = "Approved for repair", rowVersion = approval.GetProperty("rowVersion").GetInt64() }));
        var assigned = await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/maintenance/{id}", new { status = "ASSIGNED", vendorId = vendor.GetProperty("id").GetGuid(), approvedAmount = 180.00m, ownerApprovalId = decided.GetProperty("id").GetGuid(), rowVersion = quoting.GetProperty("rowVersion").GetInt64(), details = "Selected lowest valid quote" }));
        Assert.Equal("ASSIGNED", assigned.GetProperty("status").GetString());
        Assert.Equal(quote.GetProperty("id").GetGuid(), assigned.GetProperty("selectedQuoteId").GetGuid());
        var scheduled = await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/maintenance/{id}", new { status = "SCHEDULED", rowVersion = assigned.GetProperty("rowVersion").GetInt64(), ownerApprovalId = decided.GetProperty("id").GetGuid(), scheduledAt = DateTimeOffset.UtcNow.AddDays(2) }));
        var inProgress = await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/maintenance/{id}", new { status = "IN_PROGRESS", rowVersion = scheduled.GetProperty("rowVersion").GetInt64(), ownerApprovalId = decided.GetProperty("id").GetGuid() }));
        var completed = await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/maintenance/{id}", new { status = "COMPLETED", rowVersion = inProgress.GetProperty("rowVersion").GetInt64(), ownerApprovalId = decided.GetProperty("id").GetGuid(), expenseAmount = 180.00m, ownerCharge = 180.00m }));
        Assert.True(completed.GetProperty("financiallyPosted").GetBoolean());
        Assert.NotEqual(Guid.Empty, completed.GetProperty("financialJournalId").GetGuid());
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
        var dashboard = await client.GetFromJsonAsync<JsonElement>("/api/property-manager/professional/dashboard");
        Assert.True(dashboard.GetProperty("openMaintenance").GetInt32() >= 1);
        Assert.True(dashboard.GetProperty("openWorkOrders").GetInt32() >= 1);
        Assert.True(dashboard.GetProperty("openIncidents").GetInt32() >= 0);
        Assert.True(dashboard.GetProperty("upcomingInspections").GetInt32() >= 0);
        var timeline = await client.GetFromJsonAsync<JsonElement[]>("/api/property-manager/professional/timeline");
        Assert.NotNull(timeline);
        Assert.Contains(timeline!, item => item.GetProperty("action").GetString() == "MaintenanceCreated");
    }

    [Fact]
    public async Task ReadinessCannotBeMarkedReadyUntilRequiredItemsAreComplete()
    {
        using var client = factory.CreateClient(); var portfolio = await CreatePortfolio(client);
        var created = await Ok(client.PostAsJsonAsync("/api/property-manager/professional/cleaning", new { propertyId = portfolio.Property, dueAt = DateTimeOffset.UtcNow.AddDays(1), checklistJson = "[{\"id\":\"bathroom\",\"label\":\"Bathroom\",\"required\":true,\"completed\":false}]" }));
        var id = created.GetProperty("id").GetGuid();
        var rejected = await client.PatchAsJsonAsync($"/api/property-manager/professional/cleaning/{id}", new { status = "READY", checklistJson = created.GetProperty("checklistJson").GetString(), photosJson = "[]", issues = "", rowVersion = created.GetProperty("rowVersion").GetInt64() });
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        var ready = await Ok(client.PatchAsJsonAsync($"/api/property-manager/professional/cleaning/{id}", new { status = "READY", checklistJson = "[{\"id\":\"bathroom\",\"label\":\"Bathroom\",\"required\":true,\"completed\":true}]", photosJson = "[]", issues = "", rowVersion = created.GetProperty("rowVersion").GetInt64() }));
        Assert.Equal("READY", ready.GetProperty("status").GetString());
    }

    private async Task<(Guid Manager, Guid Owner, Guid Property, string OwnerEmail, string Password)> CreatePortfolio(HttpClient client)
    {
        var email = $"professional-{Guid.NewGuid():N}@nestystay.local";
        var registered = await Ok(client.PostAsJsonAsync("/api/auth/register", new { email, password = "NestyStay1", confirmPassword = "NestyStay1", displayName = "Professional owner", phone = "+15550104003", acceptedTerms = true, acceptedPrivacy = true, role = "Owner" }));
        var owner = registered.GetProperty("userId").GetGuid(); var manager = Guid.NewGuid(); SignIn(client, manager, UserRole.PropertyManager);
        await Ok(client.PostAsJsonAsync("/api/property-manager/owners", new { email, displayName = "Professional owner" }));
        var property = await Ok(client.PostAsJsonAsync("/api/property-manager/properties", new { ownerUserId = owner, title = "Professional unit", unitNumber = "P-1", address = "Kingston" }));
        return (manager, owner, property.GetProperty("id").GetGuid(), email, "NestyStay1");
    }
    private static void SignIn(HttpClient client, Guid userId, UserRole role) => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(userId, role));
    private static async Task<JsonElement> Ok(Task<HttpResponseMessage> action) { using var response = await action; var body = await response.Content.ReadAsStringAsync(); Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {body}"); return JsonDocument.Parse(body).RootElement.Clone(); }
}
