using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NestyStay.Domain;
using NestyStay.Infrastructure.Persistence;
using NestyStay.Infrastructure.Persistence.Milestones;

namespace NestyStay.Api.Tests;

public sealed class PropertyManagerFoundationRegressionTests(NestyStayApiFactory factory) : IClassFixture<NestyStayApiFactory>
{
    private const string Api = "/api/property-manager";

    [Fact]
    public async Task DashboardPreferencesSurviveANewRequest()
    {
        using var client = factory.CreateClient();
        SignIn(client, Guid.NewGuid(), UserRole.PropertyManager);
        await Ok(client.PutAsJsonAsync($"{Api}/dashboard/preferences", new { kpiOrder = new[] { "owners", "properties" }, visibleKpis = new[] { "owners" }, savedFiltersJson = "{\"status\":\"ACTIVE\"}", savedViews = new[] { "My portfolio" } }));
        var saved = await client.GetFromJsonAsync<JsonElement>($"{Api}/dashboard/preferences");
        Assert.Equal("owners", saved.GetProperty("kpiOrder")[0].GetString());
        Assert.Equal("My portfolio", saved.GetProperty("savedViews")[0].GetString());
    }

    [Fact]
    public async Task UtilitySchedulePersistsAndRejectsForeignProperties()
    {
        using var client = factory.CreateClient();
        var data = await Portfolio(client);
        await Ok(client.PostAsJsonAsync($"{Api}/utilities/schedules", new { ownerUserId = data.Owner, propertyId = data.Property, utilityType = "WATER", rate = 3m, dayOfMonth = 5 }));
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NestyStayDbContext>();
            var schedule = await db.MilestoneManagerUtilitySchedules.AsNoTracking().SingleAsync(x => x.ManagerUserId == data.Manager);
            Assert.Equal(3m, schedule.Rate);
            Assert.Equal(data.Property, schedule.PropertyId);
        }
        var rejected = await client.PostAsJsonAsync($"{Api}/utilities/schedules", new { ownerUserId = data.Owner, propertyId = Guid.NewGuid(), utilityType = "WATER", rate = 3m, dayOfMonth = 5 });
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
    }

    [Fact]
    public async Task VerificationDecisionsAreAppendOnlyAndUpdateAccountStatus()
    {
        using var client = factory.CreateClient();
        var data = await Portfolio(client);
        foreach (var status in new[] { "CHANGES_REQUESTED", "APPROVED", "REJECTED" })
            await Ok(client.PostAsJsonAsync($"{Api}/owners/verification-requirements", new { ownerUserId = data.Owner, requirement = "ACCOUNT", status, reason = $"Reviewed: {status}" }));
        var history = await client.GetFromJsonAsync<JsonElement[]>($"{Api}/owners/{data.Owner}/verification-requirements");
        Assert.Equal(3, history!.Length);
        Assert.Equal(3, history.Select(x => x.GetProperty("id").GetGuid()).Distinct().Count());
        var dashboard = await client.GetFromJsonAsync<JsonElement>($"{Api}/dashboard");
        Assert.Equal("REJECTED", dashboard.GetProperty("owners")[0].GetProperty("verificationStatus").GetString());
    }

    [Fact]
    public async Task AgreementDraftVersionsPersistWithoutActivatingOrOverwriting()
    {
        using var client = factory.CreateClient();
        var data = await Portfolio(client);
        for (var version = 1; version <= 2; version++)
        {
            var result = await Ok(client.PostAsJsonAsync($"{Api}/agreements", new { ownerUserId = data.Owner, propertyId = data.Property, effectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow), feeRuleJson = "{}", maintenanceApprovalLimit = 25m * version, expenseApprovalLimit = 50m }));
            Assert.Equal("DRAFT", result.GetProperty("status").GetString());
            Assert.Equal(version, result.GetProperty("version").GetInt32());
        }
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NestyStayDbContext>();
        var versions = await db.MilestoneManagementAgreements.AsNoTracking().Where(x => x.ManagerUserId == data.Manager).OrderBy(x => x.Version).ToListAsync();
        Assert.Equal(2, versions.Count);
        Assert.Equal(25m, versions[0].MaintenanceApprovalLimit);
        Assert.All(versions, x => Assert.Equal("DRAFT", x.Status));
    }

    [Fact]
    public async Task InvoiceEditPreservesPostedChargeAndAddsAdjustment()
    {
        using var client = factory.CreateClient();
        var data = await Portfolio(client);
        var invoiceId = await Invoice(client, data, 100m);
        await Ok(client.PutAsJsonAsync($"{Api}/invoices/{invoiceId}", new { dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), tax = 0, lines = new[] { new { description = "Revised service", quantity = 1, unitAmount = 125 } } }));
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NestyStayDbContext>();
        var entries = await db.MilestoneManagerLedgerEntries.AsNoTracking().Where(x => x.InvoiceId == invoiceId).ToListAsync();
        Assert.Equal(100m, Assert.Single(entries, x => x.EntryType == "CHARGE").Amount);
        Assert.Equal(25m, Assert.Single(entries, x => x.EntryType == "ADJUSTMENT").Amount);
        Assert.Equal(125m, entries.Sum(x => x.Amount));
    }

    [Fact]
    public async Task OwnerCannotRefundOwnPaymentAndReportDoesNotInventFeeRevenue()
    {
        using var client = factory.CreateClient();
        var data = await Portfolio(client);
        var invoiceId = await Invoice(client, data, 100m);
        await Ok(client.PostAsJsonAsync($"{Api}/invoices/{invoiceId}/payments", new { amount = 100m, idempotencyKey = Guid.NewGuid().ToString("N") }));
        var payments = await client.GetFromJsonAsync<JsonElement[]>($"{Api}/payments");
        var paymentId = Assert.Single(payments!).GetProperty("id").GetGuid();
        SignIn(client, data.Owner, UserRole.Owner);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync($"{Api}/payments/{paymentId}/refund", new { amount = 20, reason = "Forged refund", idempotencyKey = Guid.NewGuid().ToString("N") })).StatusCode);
        SignIn(client, Guid.NewGuid(), UserRole.PropertyManager);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"{Api}/payments/{paymentId}/refund", new { amount = 20, reason = "Other manager", idempotencyKey = Guid.NewGuid().ToString("N") })).StatusCode);
        SignIn(client, data.Manager, UserRole.PropertyManager);
        var key = Guid.NewGuid().ToString("N");
        for (var attempt = 0; attempt < 2; attempt++)
            await Ok(client.PostAsJsonAsync($"{Api}/payments/{paymentId}/refund", new { amount = 20, reason = "Service adjustment", idempotencyKey = key }));
        var invoice = await client.GetFromJsonAsync<JsonElement>($"{Api}/invoices/{invoiceId}");
        Assert.Equal(20m, invoice.GetProperty("balance").GetDecimal());
        var report = await client.GetFromJsonAsync<JsonElement>($"{Api}/reports");
        Assert.Equal(80m, report.GetProperty("paymentRevenue").GetDecimal());
        Assert.Equal(0m, report.GetProperty("pmFeeRevenue").GetDecimal());
    }

    [Fact]
    public async Task ApprovalCannotBeWaivedOrDecidedByManagerOrOtherOwner()
    {
        using var client = factory.CreateClient();
        var data = await Portfolio(client);
        var approval = await Ok(client.PostAsJsonAsync($"{Api}/approvals", new { ownerUserId = data.Owner, propertyId = data.Property, approvalType = "EXPENSE", description = "Repair", amount = 500, limit = 999999 }));
        Assert.Equal("REQUIRED", approval.GetProperty("status").GetString());
        var id = approval.GetProperty("id").GetGuid();
        var endpoint = $"{Api}/approvals/{id}/decide";
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(endpoint, new { status = "APPROVED", reason = "Manager impersonation" })).StatusCode);
        SignIn(client, Guid.NewGuid(), UserRole.Owner);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(endpoint, new { status = "APPROVED", reason = "Another owner" })).StatusCode);
        SignIn(client, data.Owner, UserRole.Owner);
        await Ok(client.PostAsJsonAsync(endpoint, new { status = "CHANGES_REQUESTED", reason = "Please attach a second quote" }));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(endpoint, new { status = "APPROVED", reason = "Overwrite" })).StatusCode);
    }

    [Fact]
    public async Task NoticeInteractionsEnforceMembershipAudienceAndPublicationWindow()
    {
        using var client = factory.CreateClient();
        var data = await Portfolio(client);
        var notice = await Ok(client.PostAsJsonAsync($"{Api}/notices", new { title = "Community update", body = "Water maintenance", isPinned = false }));
        var id = notice.GetProperty("id").GetGuid();
        SignIn(client, Guid.NewGuid(), UserRole.Owner);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync($"{Api}/notices/{id}/acknowledge", null)).StatusCode);
        SignIn(client, data.Owner, UserRole.Owner);
        await Ok(client.PostAsJsonAsync($"{Api}/notices/{id}/comments", new { body = "Thank you" }));
        await Ok(client.PostAsync($"{Api}/notices/{id}/acknowledge", null));
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NestyStayDbContext>();
            var row = await db.MilestoneManagerNotices.SingleAsync(x => x.Id == id);
            row.PublishAt = DateTimeOffset.UtcNow.AddDays(1);
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync($"{Api}/notices/{id}/acknowledge", null)).StatusCode);
        var visible = await client.GetFromJsonAsync<JsonElement[]>($"{Api}/notices");
        Assert.Empty(visible!);
    }

    [Fact]
    public async Task PostedLedgerRejectsMutationAndDeletion()
    {
        using var client = factory.CreateClient();
        var data = await Portfolio(client);
        var invoiceId = await Invoice(client, data, 100m);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NestyStayDbContext>();
        var entry = await db.MilestoneManagerLedgerEntries.SingleAsync(x => x.InvoiceId == invoiceId);
        entry.Amount = 999m;
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        db.Entry(entry).Reload();
        db.MilestoneManagerLedgerEntries.Remove(entry);
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task ConcurrentPaymentsCannotOverpayAndChangedIdempotencyPayloadIsRejected()
    {
        using var client = factory.CreateClient();
        var data = await Portfolio(client);
        var invoiceId = await Invoice(client, data, 100m);
        var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => client.PostAsJsonAsync($"{Api}/invoices/{invoiceId}/payments", new { amount = 60m, idempotencyKey = Guid.NewGuid().ToString("N") })));
        Assert.Single(responses, x => x.IsSuccessStatusCode);
        Assert.Equal(4, responses.Count(x => x.StatusCode == HttpStatusCode.BadRequest));
        var key = Guid.NewGuid().ToString("N");
        var duplicates = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => client.PostAsJsonAsync($"{Api}/invoices/{invoiceId}/payments", new { amount = 40m, idempotencyKey = key })));
        Assert.All(duplicates, x => Assert.Equal(HttpStatusCode.OK, x.StatusCode));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"{Api}/invoices/{invoiceId}/payments", new { amount = 39m, idempotencyKey = key })).StatusCode);
        var invoice = await client.GetFromJsonAsync<JsonElement>($"{Api}/invoices/{invoiceId}");
        Assert.Equal(100m, invoice.GetProperty("amountPaid").GetDecimal());
        Assert.Equal(0m, invoice.GetProperty("balance").GetDecimal());
    }

    private async Task<(Guid Manager, Guid Owner, Guid Property)> Portfolio(HttpClient client)
    {
        var email = $"foundation-{Guid.NewGuid():N}@nestystay.local";
        var registered = await Ok(client.PostAsJsonAsync("/api/auth/register", new { email, password = "NestyStay1", confirmPassword = "NestyStay1", displayName = "Foundation owner", phone = "+15550104003", acceptedTerms = true, acceptedPrivacy = true, role = "Owner" }));
        var owner = registered.GetProperty("userId").GetGuid();
        var manager = Guid.NewGuid();
        SignIn(client, manager, UserRole.PropertyManager);
        await Ok(client.PostAsJsonAsync($"{Api}/owners", new { email, displayName = "Foundation owner" }));
        var property = await Ok(client.PostAsJsonAsync($"{Api}/properties", new { ownerUserId = owner, title = "Foundation unit", unitNumber = "F-1", address = "Kingston" }));
        return (manager, owner, property.GetProperty("id").GetGuid());
    }

    private static async Task<Guid> Invoice(HttpClient client, (Guid Manager, Guid Owner, Guid Property) data, decimal amount)
    {
        var result = await Ok(client.PostAsJsonAsync($"{Api}/invoices", new { ownerUserId = data.Owner, propertyId = data.Property, dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), tax = 0, lines = new[] { new { description = "Service", quantity = 1, unitAmount = amount } } }));
        return result.GetProperty("id").GetGuid();
    }

    private static void SignIn(HttpClient client, Guid userId, UserRole role) => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(userId, role));

    private static async Task<JsonElement> Ok(Task<HttpResponseMessage> action)
    {
        using var response = await action;
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {body}");
        return JsonDocument.Parse(body).RootElement.Clone();
    }
}
