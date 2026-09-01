using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NestyStay.Domain;

namespace NestyStay.Api.Tests;

public sealed class PropertyManagerEndpointTests : IClassFixture<NestyStayApiFactory>
{
    private readonly NestyStayApiFactory factory;

    public PropertyManagerEndpointTests(NestyStayApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task ManagerOwnerInvoiceUtilityPaymentAndStatementRoundTripThroughApi()
    {
        using var client = factory.CreateClient();
        var managerId = Guid.NewGuid();
        var ownerEmail = $"pm-owner-{Guid.NewGuid():N}@nestystay.local";
        var registration = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = ownerEmail, password = "NestyStay1", confirmPassword = "NestyStay1",
            displayName = "Phase 5 Owner", phone = "+15550104001", acceptedTerms = true,
            acceptedPrivacy = true, role = "Owner"
        });
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);
        var ownerId = (await registration.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("userId").GetGuid();
        var managerToken = NestyStayApiFactory.UserToken(managerId, UserRole.PropertyManager);
        var ownerToken = NestyStayApiFactory.UserToken(ownerId, UserRole.Owner);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var invited = await client.PostAsJsonAsync("/api/property-manager/owners", new { email = ownerEmail, displayName = "Phase 5 Owner" });
        Assert.Equal(HttpStatusCode.OK, invited.StatusCode);
        var owner = await invited.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("PENDING", owner.GetProperty("verificationStatus").GetString());

        var propertyResponse = await client.PostAsJsonAsync("/api/property-manager/properties", new
        {
            ownerUserId = ownerId, title = "Managed Apartment", unitNumber = "M-5", address = "Kingston"
        });
        Assert.Equal(HttpStatusCode.OK, propertyResponse.StatusCode);
        var propertyId = (await propertyResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var invoiceResponse = await client.PostAsJsonAsync("/api/property-manager/invoices", new
        {
            ownerUserId = ownerId, propertyId, dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), tax = 5m,
            lines = new[] { new { description = "Monthly management", quantity = 1m, unitAmount = 100m } }
        });
        Assert.Equal(HttpStatusCode.OK, invoiceResponse.StatusCode);
        var invoice = await invoiceResponse.Content.ReadFromJsonAsync<JsonElement>();
        var invoiceId = invoice.GetProperty("id").GetGuid();
        Assert.Equal(105m, invoice.GetProperty("total").GetDecimal());

        var utilityResponse = await client.PostAsJsonAsync("/api/property-manager/utilities", new
        {
            ownerUserId = ownerId, propertyId, utilityType = "Water", billingPeriod = "2026-08", usage = 10m, rate = 2m
        });
        Assert.Equal(HttpStatusCode.OK, utilityResponse.StatusCode);
        var utility = await utilityResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(Guid.Empty, utility.GetProperty("invoiceId").GetGuid());

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var paymentResponse = await client.PostAsJsonAsync($"/api/property-manager/invoices/{invoiceId}/payments", new { amount = 40m, idempotencyKey = Guid.NewGuid().ToString("N") });
        Assert.Equal(HttpStatusCode.OK, paymentResponse.StatusCode);
        Assert.Equal(65m, (await paymentResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("balance").GetDecimal());

        var statement = await client.GetFromJsonAsync<JsonElement>($"/api/property-manager/owners/{ownerId}/statement");
        Assert.Equal(85m, statement.GetProperty("closingBalance").GetDecimal());
        var portal = await client.GetFromJsonAsync<JsonElement>("/api/property-manager/owner/portal");
        Assert.Equal(2, portal.GetProperty("invoices").GetArrayLength());
    }

    [Fact]
    public async Task GovernanceBallotIsAnonymousAndCannotBeSubmittedTwice()
    {
        using var client = factory.CreateClient();
        var managerId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var ownerEmail = $"vote-owner-{Guid.NewGuid():N}@nestystay.local";
        var registration = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = ownerEmail, password = "NestyStay1", confirmPassword = "NestyStay1", displayName = "Ballot Owner",
            phone = "+15550104002", acceptedTerms = true, acceptedPrivacy = true, role = "Owner"
        });
        ownerId = (await registration.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("userId").GetGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(managerId, UserRole.PropertyManager));
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/property-manager/owners", new { email = ownerEmail, displayName = "Ballot Owner" })).StatusCode);
        var proposalResponse = await client.PostAsJsonAsync("/api/property-manager/governance/proposals", new
        {
            title = "Anonymous budget vote", description = "Approve the budget", opensAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            closesAt = DateTimeOffset.UtcNow.AddDays(1), isAnonymous = true, quorum = 1
        });
        Assert.Equal(HttpStatusCode.OK, proposalResponse.StatusCode);
        var proposal = await proposalResponse.Content.ReadFromJsonAsync<JsonElement>();
        var proposalId = proposal.GetProperty("id").GetGuid();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(ownerId, UserRole.Owner));
        var vote = await client.PostAsJsonAsync($"/api/property-manager/governance/proposals/{proposalId}/votes", new { choice = "YES" });
        Assert.Equal(HttpStatusCode.OK, vote.StatusCode);
        var result = await vote.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, result.GetProperty("votesCast").GetInt32());
        Assert.Equal(1, result.GetProperty("results").GetProperty("YES").GetInt32());
        Assert.DoesNotContain("ownerUserId", result.GetRawText(), StringComparison.OrdinalIgnoreCase);

        var duplicate = await client.PostAsJsonAsync($"/api/property-manager/governance/proposals/{proposalId}/votes", new { choice = "NO" });
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
    }

    [Fact]
    public async Task OwnerCannotReadAnotherOwnersInvoiceOrPortal()
    {
        using var client = factory.CreateClient();
        var managerId = Guid.NewGuid();
        var ownerOneId = Guid.NewGuid();
        var ownerTwoId = Guid.NewGuid();
        var oneEmail = $"scope-one-{Guid.NewGuid():N}@nestystay.local";
        var twoEmail = $"scope-two-{Guid.NewGuid():N}@nestystay.local";
        foreach (var tuple in new[] { (oneEmail, "Scope One"), (twoEmail, "Scope Two") })
        {
            var response = await client.PostAsJsonAsync("/api/auth/register", new
            {
                email = tuple.Item1, password = "NestyStay1", confirmPassword = "NestyStay1", displayName = tuple.Item2,
                phone = "+15550104003", acceptedTerms = true, acceptedPrivacy = true, role = "Owner"
            });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            if (tuple.Item1 == oneEmail) ownerOneId = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("userId").GetGuid();
            else ownerTwoId = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("userId").GetGuid();
        }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(managerId, UserRole.PropertyManager));
        foreach (var owner in new[] { (ownerOneId, oneEmail), (ownerTwoId, twoEmail) })
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/property-manager/owners", new { email = owner.Item2, displayName = "Scope" })).StatusCode);
        var property = await client.PostAsJsonAsync("/api/property-manager/properties", new { ownerUserId = ownerOneId, title = "Scoped", unitNumber = "S-1", address = "Kingston" });
        var propertyId = (await property.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var invoice = await client.PostAsJsonAsync("/api/property-manager/invoices", new
        {
            ownerUserId = ownerOneId, propertyId, dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), tax = 0m,
            lines = new[] { new { description = "Scoped", quantity = 1m, unitAmount = 50m } }
        });
        var invoiceId = (await invoice.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(ownerTwoId, UserRole.Owner));
        var hiddenInvoice = await client.GetAsync($"/api/property-manager/invoices/{invoiceId}");
        Assert.Equal(HttpStatusCode.NotFound, hiddenInvoice.StatusCode);
        var portal = await client.GetAsync("/api/property-manager/owner/portal");
        Assert.Equal(HttpStatusCode.OK, portal.StatusCode);
        Assert.DoesNotContain(invoiceId.ToString(), await portal.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ManagerDocumentDownloadIsScopedAndUsesStorageProvider()
    {
        using var client = factory.CreateClient();
        var managerId = Guid.NewGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(managerId, UserRole.PropertyManager));
        var bytes = System.Text.Encoding.ASCII.GetBytes("%PDF-1.7\nstatement\n");
        var response = await client.PostAsJsonAsync("/api/property-manager/documents", new
        {
            title = "Annual statement", category = "Finance", fileName = "statement.pdf", contentType = "application/pdf",
            sizeBytes = bytes.Length, contentBase64 = Convert.ToBase64String(bytes)
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var documentId = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var download = await client.GetAsync($"/api/property-manager/documents/{documentId}/download");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        var body = await download.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(documentId, body.GetProperty("id").GetGuid());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("url").GetString()));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(Guid.NewGuid(), UserRole.Owner));
        var denied = await client.GetAsync($"/api/property-manager/documents/{documentId}/download");
        Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
    }
}
