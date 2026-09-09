using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

        var updateInvoice = await client.PutAsJsonAsync($"/api/property-manager/invoices/{invoiceId}", new
        {
            dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(45)), tax = 10m,
            lines = new[] { new { description = "Updated community fee", quantity = 1m, unitAmount = 120m } }
        });
        Assert.Equal(HttpStatusCode.OK, updateInvoice.StatusCode);
        Assert.Equal(130m, (await updateInvoice.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("total").GetDecimal());

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
        Assert.Equal(90m, (await paymentResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("balance").GetDecimal());

        var statement = await client.GetFromJsonAsync<JsonElement>($"/api/property-manager/owners/{ownerId}/statement");
        Assert.Equal(110m, statement.GetProperty("closingBalance").GetDecimal());
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
        var expiresOn = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6));
        var response = await client.PostAsJsonAsync("/api/property-manager/documents", new
        {
            title = "Annual statement", category = "Finance", fileName = "statement.pdf", contentType = "application/pdf",
            sizeBytes = bytes.Length, contentBase64 = Convert.ToBase64String(bytes), expiresOn = expiresOn.ToString("yyyy-MM-dd")
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var createdDocument = await response.Content.ReadFromJsonAsync<JsonElement>();
        var documentId = createdDocument.GetProperty("id").GetGuid();
        Assert.Equal(expiresOn, DateOnly.Parse(createdDocument.GetProperty("expiresOn").GetString()!));

        var download = await client.GetAsync($"/api/property-manager/documents/{documentId}/download");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        var body = await download.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(documentId, body.GetProperty("id").GetGuid());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("url").GetString()));

        var exportResponse = await client.PostAsJsonAsync("/api/property-manager/documents/exports", new { documentIds = new[] { documentId } });
        Assert.Equal(HttpStatusCode.OK, exportResponse.StatusCode);
        var export = await exportResponse.Content.ReadFromJsonAsync<JsonElement>();
        var exportId = export.GetProperty("id").GetGuid();
        var exportService = factory.Services.GetServices<Microsoft.Extensions.Hosting.IHostedService>().OfType<NestyStay.Api.Services.PropertyManagerDocumentExportService>().Single();
        _ = await exportService.ProcessOneAsync();
        JsonElement exportState = default;
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var stateResponse = await client.GetAsync($"/api/property-manager/documents/exports/{exportId}");
            exportState = await stateResponse.Content.ReadFromJsonAsync<JsonElement>();
            if (exportState.GetProperty("status").GetString() is "COMPLETED" or "FAILED") break;
            await Task.Delay(25);
        }
        Assert.Equal("COMPLETED", exportState.GetProperty("status").GetString());
        var exportDownload = await client.GetAsync($"/api/property-manager/documents/exports/{exportId}/download");
        Assert.Equal(HttpStatusCode.OK, exportDownload.StatusCode);
        Assert.Equal("application/zip", exportDownload.Content.Headers.ContentType?.MediaType);
        var archiveBytes = await exportDownload.Content.ReadAsByteArrayAsync();
        Assert.True(archiveBytes.Length > 4 && archiveBytes[0] == (byte)'P' && archiveBytes[1] == (byte)'K');

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(Guid.NewGuid(), UserRole.Owner));
        var denied = await client.GetAsync($"/api/property-manager/documents/{documentId}/download");
        Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
    }

    [Fact]
    public async Task ManagerDocumentExpiryReminderIsQueuedIdempotently()
    {
        using var client = factory.CreateClient();
        var email = $"expiry-manager-{Guid.NewGuid():N}@nestystay.local";
        var registration = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email, password = "NestyStay1", confirmPassword = "NestyStay1", displayName = "Expiry manager",
            phone = "+15550104111", acceptedTerms = true, acceptedPrivacy = true, role = "PropertyManager"
        });
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);
        var managerId = (await registration.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("userId").GetGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(managerId, UserRole.PropertyManager));
        var bytes = System.Text.Encoding.ASCII.GetBytes("%PDF-1.7\nexpiry\n");
        var expiresOn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        var document = await client.PostAsJsonAsync("/api/property-manager/documents", new
        {
            title = "Insurance certificate", category = "Compliance", fileName = "insurance.pdf", contentType = "application/pdf",
            sizeBytes = bytes.Length, contentBase64 = Convert.ToBase64String(bytes), expiresOn = expiresOn.ToString("yyyy-MM-dd")
        });
        Assert.Equal(HttpStatusCode.OK, document.StatusCode);

        var reminderService = factory.Services.GetServices<Microsoft.Extensions.Hosting.IHostedService>().OfType<NestyStay.Api.Services.PropertyManagerDocumentExpiryService>().Single();
        Assert.Equal(1, await reminderService.RunOnceAsync());
        Assert.Equal(0, await reminderService.RunOnceAsync());
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NestyStay.Infrastructure.Persistence.NestyStayDbContext>();
        Assert.Contains(await db.NotificationQueue.Where(item => item.Channel == "Email" && item.IdempotencyKey != null && item.IdempotencyKey.StartsWith("pm-document-expiry:")).ToListAsync(), item => item.Recipient == email);
    }

    [Fact]
    public async Task ManagerSubscriptionLifecyclePersistsStateEventsAndPlanLimit()
    {
        using var client = factory.CreateClient();
        var managerId = Guid.NewGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(managerId, UserRole.PropertyManager));

        var initial = await client.GetFromJsonAsync<JsonElement>("/api/property-manager/dashboard");
        Assert.Equal("Portfolio", initial.GetProperty("manager").GetProperty("subscriptionTier").GetString());
        Assert.True(initial.GetProperty("manager").GetProperty("autoRenew").GetBoolean());
        Assert.Equal("LOCAL_TEST_READY", initial.GetProperty("manager").GetProperty("billingProviderStatus").GetString());

        var downgrade = await client.PostAsJsonAsync("/api/property-manager/subscription/change", new
        {
            action = "DOWNGRADE", targetTier = "Standard", reason = "Reduce portfolio size"
        });
        Assert.Equal(HttpStatusCode.OK, downgrade.StatusCode);
        var scheduled = await downgrade.Content.ReadFromJsonAsync<JsonElement>();
        var scheduledManager = scheduled;
        Assert.Equal("Portfolio", scheduledManager.GetProperty("subscriptionTier").GetString());
        Assert.Equal("Standard", scheduledManager.GetProperty("pendingSubscriptionTier").GetString());
        Assert.False(string.IsNullOrWhiteSpace(scheduledManager.GetProperty("pendingSubscriptionEffectiveAt").GetString()));
        Assert.Equal(0, scheduledManager.GetProperty("unitsUsed").GetInt32());
        Assert.Equal(JsonValueKind.Null, scheduledManager.GetProperty("unitLimit").ValueKind);

        var disableAutoRenew = await client.PostAsJsonAsync("/api/property-manager/subscription/change", new
        {
            action = "AUTO_RENEW", autoRenew = false, reason = "Pause at term end"
        });
        Assert.Equal(HttpStatusCode.OK, disableAutoRenew.StatusCode);
        Assert.False((await disableAutoRenew.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("autoRenew").GetBoolean());

        var paused = await client.PostAsJsonAsync("/api/property-manager/subscription/change", new { action = "PAUSE", reason = "Seasonal pause" });
        Assert.Equal(HttpStatusCode.OK, paused.StatusCode);
        Assert.Equal("PAUSED", (await paused.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("subscriptionStatus").GetString());

        var resumed = await client.PostAsJsonAsync("/api/property-manager/subscription/change", new { action = "RESUME", reason = "Operations resumed" });
        Assert.Equal(HttpStatusCode.OK, resumed.StatusCode);
        Assert.Equal("ACTIVE", (await resumed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("subscriptionStatus").GetString());

        var cancelled = await client.PostAsJsonAsync("/api/property-manager/subscription/change", new { action = "CANCEL", reason = "Moving to another provider" });
        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);
        var cancelledManager = await cancelled.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("CANCELLED", cancelledManager.GetProperty("subscriptionStatus").GetString());
        Assert.Equal("Moving to another provider", cancelledManager.GetProperty("cancellationReason").GetString());
        Assert.False(cancelledManager.GetProperty("autoRenew").GetBoolean());

        var reactivated = await client.PostAsJsonAsync("/api/property-manager/subscription/change", new { action = "REACTIVATE", reason = "Keep portfolio active" });
        Assert.Equal(HttpStatusCode.OK, reactivated.StatusCode);
        var reactivatedManager = await reactivated.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ACTIVE", reactivatedManager.GetProperty("subscriptionStatus").GetString());
        Assert.True(reactivatedManager.GetProperty("autoRenew").GetBoolean());
        Assert.Equal("LOCAL_TEST_READY", reactivatedManager.GetProperty("billingProviderStatus").GetString());

        var retry = await client.PostAsync("/api/property-manager/subscription/payment-retry", null);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        var retryEvent = await retry.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("PAYMENT_RETRY", retryEvent.GetProperty("eventType").GetString());
        Assert.Equal("RETRY_QUEUED", retryEvent.GetProperty("status").GetString());

        var dashboard = await client.GetFromJsonAsync<JsonElement>("/api/property-manager/dashboard");
        Assert.Equal("RETRY_QUEUED", dashboard.GetProperty("manager").GetProperty("billingProviderStatus").GetString());
        var events = await client.GetFromJsonAsync<JsonElement[]>("/api/property-manager/subscription/events");
        Assert.NotNull(events);
        Assert.Contains(events!, item => item.GetProperty("eventType").GetString() == "DOWNGRADE" && item.GetProperty("status").GetString() == "SCHEDULED");
        Assert.Contains(events!, item => item.GetProperty("eventType").GetString() == "PAYMENT_RETRY");
        Assert.Contains(events!, item => item.GetProperty("eventType").GetString() == "REACTIVATE");

        // Move the scheduled change into the past and exercise the same worker
        // that runs in production, rather than relying only on a dashboard read.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NestyStay.Infrastructure.Persistence.NestyStayDbContext>();
            var row = await db.MilestonePropertyManagers.SingleAsync(item => item.ManagerUserId == managerId);
            row.PendingSubscriptionTier = "Standard";
            row.PendingSubscriptionEffectiveAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            row.AutoRenew = true;
            await db.SaveChangesAsync();
        }
        var subscriptionWorker = factory.Services.GetServices<Microsoft.Extensions.Hosting.IHostedService>().OfType<NestyStay.Api.Services.PropertyManagerSubscriptionMaintenanceService>().Single();
        Assert.Equal(1, await subscriptionWorker.RunOnceAsync());
        var appliedDashboard = await client.GetFromJsonAsync<JsonElement>("/api/property-manager/dashboard");
        Assert.Equal("Standard", appliedDashboard.GetProperty("manager").GetProperty("subscriptionTier").GetString());
        Assert.Equal(JsonValueKind.Null, appliedDashboard.GetProperty("manager").GetProperty("pendingSubscriptionTier").ValueKind);
        var appliedEvents = await client.GetFromJsonAsync<JsonElement[]>("/api/property-manager/subscription/events");
        Assert.Contains(appliedEvents!, item => item.GetProperty("eventType").GetString() == "DOWNGRADE_APPLIED");
    }

    [Fact]
    public async Task ManagerQrHistoryAndProxyRevocationAreScoped()
    {
        using var client = factory.CreateClient();
        var managerId = Guid.NewGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(managerId, UserRole.PropertyManager));
        var issued = await client.PostAsJsonAsync("/api/property-manager/qr", new { subjectType = "VISITOR", validFrom = DateTimeOffset.UtcNow.AddMinutes(-1), validUntil = DateTimeOffset.UtcNow.AddHours(1) });
        Assert.Equal(HttpStatusCode.OK, issued.StatusCode);
        var qr = await issued.Content.ReadFromJsonAsync<JsonElement>();
        var qrId = qr.GetProperty("id").GetGuid();
        var token = qr.GetProperty("token").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/property-manager/qr/validate", new { token })).StatusCode);
        var history = await client.GetFromJsonAsync<JsonElement[]>($"/api/property-manager/qr/{qrId}/history");
        var historyItems = history ?? Array.Empty<JsonElement>();
        Assert.Single(historyItems);
        Assert.Equal("VALID", historyItems[0].GetProperty("result").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/property-manager/qr/{qrId}/revoke", null)).StatusCode);
        var denied = await client.GetAsync($"/api/property-manager/qr/{qrId}/history");
        Assert.Equal(HttpStatusCode.OK, denied.StatusCode);
    }

    [Fact]
    public async Task CommunityNoticeSchedulingAndAudienceArePersistedAndOwnerScoped()
    {
        using var client = factory.CreateClient();
        var managerId = Guid.NewGuid();
        var ownerEmail = $"notice-owner-{Guid.NewGuid():N}@nestystay.local";
        var ownerRegistration = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = ownerEmail, password = "NestyStay1", confirmPassword = "NestyStay1", displayName = "Notice Owner",
            phone = "+15550104030", acceptedTerms = true, acceptedPrivacy = true, role = "Owner"
        });
        Assert.Equal(HttpStatusCode.OK, ownerRegistration.StatusCode);
        var ownerId = (await ownerRegistration.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("userId").GetGuid();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(managerId, UserRole.PropertyManager));
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/property-manager/owners", new { email = ownerEmail, displayName = "Notice Owner" })).StatusCode);
        var publishAt = DateTimeOffset.UtcNow.AddHours(2);
        var acknowledgementDueAt = publishAt.AddDays(1);
        var createdResponse = await client.PostAsJsonAsync("/api/property-manager/notices", new
        {
            title = "Planned maintenance", body = "Water will be unavailable during the scheduled window.", category = "MAINTENANCE",
            publishAt, expiresAt = publishAt.AddDays(3), acknowledgementDueAt, audienceRoles = new[] { "Owner" }, audienceOwnerIds = new[] { ownerId }, isPinned = true
        });
        Assert.Equal(HttpStatusCode.OK, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("MAINTENANCE", created.GetProperty("category").GetString());
        Assert.Equal(publishAt, created.GetProperty("publishAt").GetDateTimeOffset(), precision: TimeSpan.FromSeconds(1));
        Assert.Equal(ownerId, created.GetProperty("audienceOwnerIds")[0].GetGuid());
        Assert.Equal(acknowledgementDueAt, created.GetProperty("acknowledgementDueAt").GetDateTimeOffset(), precision: TimeSpan.FromSeconds(1));

        var managerNotices = await client.GetFromJsonAsync<JsonElement[]>("/api/property-manager/notices");
        Assert.Contains(managerNotices ?? Array.Empty<JsonElement>(), notice => notice.GetProperty("id").GetGuid() == created.GetProperty("id").GetGuid());

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(ownerId, UserRole.Owner));
        var ownerNotices = await client.GetFromJsonAsync<JsonElement[]>("/api/property-manager/notices");
        Assert.DoesNotContain(ownerNotices ?? Array.Empty<JsonElement>(), notice => notice.GetProperty("id").GetGuid() == created.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task PropertyManagerOperationsExposeAssignmentPaymentsUtilitiesAndWorkOrders()
    {
        using var client = factory.CreateClient();
        var managerId = Guid.NewGuid();
        var ownerEmail = $"ops-owner-{Guid.NewGuid():N}@nestystay.local";
        var registration = await client.PostAsJsonAsync("/api/auth/register", new { email = ownerEmail, password = "NestyStay1", confirmPassword = "NestyStay1", displayName = "Operations Owner", phone = "+15550104011", acceptedTerms = true, acceptedPrivacy = true, role = "Owner" });
        var ownerId = (await registration.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("userId").GetGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(managerId, UserRole.PropertyManager));
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/property-manager/owners", new { email = ownerEmail, displayName = "Operations Owner" })).StatusCode);
        var property = await client.PostAsJsonAsync("/api/property-manager/properties", new { ownerUserId = ownerId, title = "Operations unit", unitNumber = "OP-1", address = "Kingston" });
        var propertyId = (await property.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var bulk = await client.PostAsJsonAsync("/api/property-manager/properties/bulk-assign", new { propertyIds = new[] { propertyId }, ownerUserId = ownerId, reason = "Initial assignment" });
        Assert.Equal(HttpStatusCode.OK, bulk.StatusCode);
        var bulkResults = await bulk.Content.ReadFromJsonAsync<JsonElement[]>() ?? Array.Empty<JsonElement>();
        Assert.Single(bulkResults);
        var assignmentHistory = await client.GetFromJsonAsync<JsonElement[]>("/api/property-manager/properties/assignment-history") ?? Array.Empty<JsonElement>();
        Assert.NotEmpty(assignmentHistory);

        var invoice = await client.PostAsJsonAsync("/api/property-manager/invoices", new { ownerUserId = ownerId, propertyId, dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)), tax = 0m, lines = new[] { new { description = "Ops", quantity = 1m, unitAmount = 80m } } });
        var invoiceId = (await invoice.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(ownerId, UserRole.Owner));
        var payment = await client.PostAsJsonAsync($"/api/property-manager/invoices/{invoiceId}/payments", new { amount = 80m, idempotencyKey = Guid.NewGuid().ToString("N") });
        Assert.Equal(HttpStatusCode.OK, payment.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(managerId, UserRole.PropertyManager));
        var payments = await client.GetFromJsonAsync<JsonElement[]>("/api/property-manager/payments");
        Assert.NotEmpty(payments!);
        var paymentId = payments![0].GetProperty("id").GetGuid();
        var refund = await client.PostAsJsonAsync($"/api/property-manager/payments/{paymentId}/refund", new { amount = 20m, reason = "Adjustment", idempotencyKey = Guid.NewGuid().ToString("N") });
        Assert.Equal(HttpStatusCode.OK, refund.StatusCode);

        var reading = await client.PostAsJsonAsync("/api/property-manager/utilities/readings", new { ownerUserId = ownerId, propertyId, utilityType = "WATER", billingPeriod = "2026-09", previousReading = 0m, currentReading = 10m, rate = 2m });
        Assert.Equal(HttpStatusCode.OK, reading.StatusCode);
        var workOrder = await client.PostAsJsonAsync("/api/property-manager/work-orders", new { propertyId, ownerUserId = ownerId, scope = "Replace leaking tap" });
        Assert.Equal(HttpStatusCode.OK, workOrder.StatusCode);
        var report = await client.GetAsync("/api/property-manager/reports");
        Assert.Equal(HttpStatusCode.OK, report.StatusCode);
    }

    [Fact]
    public async Task PropertyManagerLifecycleEndpointsPersistBulkInvoicesVersionsGateDeliveryQrReasonsAndBillingRetries()
    {
        using var client = factory.CreateClient();
        var managerId = Guid.NewGuid();
        var ownerEmail = $"lifecycle-owner-{Guid.NewGuid():N}@nestystay.local";
        var registration = await client.PostAsJsonAsync("/api/auth/register", new { email = ownerEmail, password = "NestyStay1", confirmPassword = "NestyStay1", displayName = "Lifecycle Owner", phone = "+15550104022", acceptedTerms = true, acceptedPrivacy = true, role = "Owner" });
        var ownerId = (await registration.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("userId").GetGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(managerId, UserRole.PropertyManager));
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/property-manager/owners", new { email = ownerEmail, displayName = "Lifecycle Owner" })).StatusCode);
        var property = await client.PostAsJsonAsync("/api/property-manager/properties", new { ownerUserId = ownerId, title = "Lifecycle Unit", unitNumber = "LC-1", address = "Kingston" });
        var propertyId = (await property.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var invoice = await client.PostAsJsonAsync("/api/property-manager/invoices", new { ownerUserId = ownerId, propertyId, dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)), tax = 0m, lines = new[] { new { description = "Lifecycle fee", quantity = 1m, unitAmount = 50m } } });
        var invoiceId = (await invoice.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var issued = await client.PostAsJsonAsync("/api/property-manager/invoices/bulk-issue", new { invoiceIds = new[] { invoiceId } });
        Assert.Equal(HttpStatusCode.OK, issued.StatusCode);
        var overdue = await client.PostAsync("/api/property-manager/invoices/mark-overdue", null);
        Assert.Equal(HttpStatusCode.OK, overdue.StatusCode);
        Assert.Contains("OVERDUE", await overdue.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

        var bytes = System.Text.Encoding.ASCII.GetBytes("%PDF-1.7\nlifecycle\n");
        var document = await client.PostAsJsonAsync("/api/property-manager/documents", new { title = "Lifecycle document", category = "Finance", fileName = "lifecycle.pdf", contentType = "application/pdf", sizeBytes = bytes.Length, contentBase64 = Convert.ToBase64String(bytes) });
        var documentId = (await document.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var versionBytes = System.Text.Encoding.ASCII.GetBytes("%PDF-1.7\nversion 2\n");
        var version = await client.PostAsJsonAsync("/api/property-manager/documents/versions", new { documentId, fileName = "lifecycle-v2.pdf", contentType = "application/pdf", sizeBytes = versionBytes.Length, contentBase64 = Convert.ToBase64String(versionBytes) });
        Assert.Equal(HttpStatusCode.OK, version.StatusCode);
        Assert.Equal(2, (await version.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("version").GetInt32());

        var gate = await client.PostAsJsonAsync("/api/property-manager/gate/messages", new { propertyId, recipient = "gate@example.invalid", message = "Visitor expected", visitorType = "VISITOR", validFrom = DateTimeOffset.UtcNow, validUntil = DateTimeOffset.UtcNow.AddHours(2) });
        var gateId = (await gate.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var delivery = await client.GetFromJsonAsync<JsonElement[]>($"/api/property-manager/gate/messages/{gateId}/delivery");
        Assert.Single(delivery!);
        var retry = await client.PostAsJsonAsync($"/api/property-manager/gate/messages/{gateId}/delivery/retry", new { });
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal(2, (await retry.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("attemptNumber").GetInt32());

        var qr = await client.PostAsJsonAsync("/api/property-manager/qr", new { ownerUserId = ownerId, propertyId, subjectType = "VISITOR", validFrom = DateTimeOffset.UtcNow.AddMinutes(-1), validUntil = DateTimeOffset.UtcNow.AddHours(1) });
        var qrId = (await qr.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var revoked = await client.PostAsJsonAsync($"/api/property-manager/qr/{qrId}/revoke", new { reason = "Visit cancelled" });
        Assert.Equal(HttpStatusCode.OK, revoked.StatusCode);
        var billingRetry = await client.PostAsync("/api/property-manager/subscription/payment-retry", null);
        Assert.Equal(HttpStatusCode.OK, billingRetry.StatusCode);
        Assert.Contains("RETRY", await billingRetry.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }
}
