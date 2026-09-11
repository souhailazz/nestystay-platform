using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NestyStay.Api.Auth;
using NestyStay.Application.Abstractions;
using NestyStay.Domain;
using NestyStay.Infrastructure.Persistence;

namespace NestyStay.Api.Tests;

public sealed class PropertyManagerTwoInstancePostgresTests
{
    [Fact]
    [Trait("Database", "PostgreSQL")]
    public async Task IndependentApiInstancesSerializeAvailabilityApprovalAndLedgerReversal()
    {
        var connectionString = Environment.GetEnvironmentVariable("NESTYSTAY_POSTGRES_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        var options = new DbContextOptionsBuilder<NestyStayDbContext>().UseNpgsql(connectionString).Options;
        await using (var migrationDb = new NestyStayDbContext(options)) await migrationDb.Database.MigrateAsync();

        await using var firstFactory = new PostgreSqlApiFactory(connectionString);
        await using var secondFactory = new PostgreSqlApiFactory(connectionString);
        using var first = firstFactory.CreateClient();
        using var second = secondFactory.CreateClient();

        var password = "NestyStay1";
        var ownerEmail = $"two-instance-owner-{Guid.NewGuid():N}@nestystay.local";
        var registered = await Ok(first.PostAsJsonAsync("/api/auth/register", new { email = ownerEmail, password, confirmPassword = password, displayName = "Concurrent owner", phone = "+15550104004", acceptedTerms = true, acceptedPrivacy = true, role = "Owner" }));
        var ownerId = registered.GetProperty("userId").GetGuid();
        var managerId = Guid.NewGuid();
        SignIn(first, managerId, UserRole.PropertyManager); SignIn(second, managerId, UserRole.PropertyManager);
        await Ok(first.PostAsJsonAsync("/api/property-manager/owners", new { email = ownerEmail, displayName = "Concurrent owner" }));
        var property = await Ok(first.PostAsJsonAsync("/api/property-manager/properties", new { ownerUserId = ownerId, title = "Concurrency villa", unitNumber = $"C-{Guid.NewGuid():N}", address = "Kingston" }));
        var propertyId = property.GetProperty("id").GetGuid();

        using var ownerHost = firstFactory.CreateClient(); SignIn(ownerHost, ownerId, UserRole.Host);
        var listing = await Ok(ownerHost.PostAsJsonAsync("/api/properties", new { hostUserId = ownerId, hostName = "Concurrent owner", hostEmail = ownerEmail, title = $"Concurrency listing {Guid.NewGuid():N}", location = "Kingston", country = "Jamaica", nightlyRate = 90, currency = "JMD", badgeLevel = "Free", cancellationPolicy = "Flexible" }));
        var listingId = listing.GetProperty("id").GetGuid();
        await Ok(first.PatchAsJsonAsync($"/api/property-manager/properties/{propertyId}/rental-listing", new { rentalListingId = listingId }));
        var guestEmail = $"two-instance-guest-{Guid.NewGuid():N}@nestystay.local";
        var guestRegistration = await Ok(ownerHost.PostAsJsonAsync("/api/auth/register", new { email = guestEmail, password, confirmPassword = password, displayName = "Concurrent guest", acceptedTerms = true, acceptedPrivacy = true, role = "Guest" }));
        var guestId = guestRegistration.GetProperty("userId").GetGuid(); using var guest = firstFactory.CreateClient(); SignIn(guest, guestId, UserRole.Guest);
        var booking = await Ok(guest.PostAsJsonAsync("/api/bookings", new { propertyId = listingId, guestUserId = guestId, checkIn = "2046-04-10", checkOut = "2046-04-12" })); var bookingId = booking.GetProperty("id").GetGuid();
        var reservation = (await first.GetFromJsonAsync<JsonElement[]>("/api/property-manager/professional/reservations"))!.Single(item => item.GetProperty("bookingId").GetGuid() == bookingId);
        var amendment = new { checkIn = "2046-04-15", checkOut = "2046-04-17", reason = "Two-instance amendment", idempotencyKey = $"pg-amend-{bookingId:N}", expectedUpdatedAt = reservation.GetProperty("updatedAt").GetDateTimeOffset() };
        var amendmentResponses = await Task.WhenAll(first.PostAsJsonAsync($"/api/property-manager/professional/reservations/{bookingId}/amend", amendment), second.PostAsJsonAsync($"/api/property-manager/professional/reservations/{bookingId}/amend", amendment));
        var amendments = await Task.WhenAll(amendmentResponses.Select(response => Ok(Task.FromResult(response)))); Assert.All(amendments, item => Assert.Equal("2046-04-15", item.GetProperty("checkIn").GetDateTimeOffset().ToString("yyyy-MM-dd")));

        var startsAt = new DateTimeOffset(2044, 8, 12, 15, 0, 0, TimeSpan.Zero).AddDays(Random.Shared.Next(0, 300));
        var block = new { ownerUserId = ownerId, propertyId, startsAt, endsAt = startsAt.AddDays(2), timeZone = "America/Jamaica", reason = "Owner visit", category = "OWNER_STAY" };
        var blockResponses = await Task.WhenAll(first.PostAsJsonAsync("/api/property-manager/professional/owner-blocks", block), second.PostAsJsonAsync("/api/property-manager/professional/owner-blocks", block));
        Assert.Equal(1, blockResponses.Count(response => response.IsSuccessStatusCode));
        Assert.Equal(1, blockResponses.Count(response => response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict));
        var rejectedBlockBody = await blockResponses.Single(response => !response.IsSuccessStatusCode).Content.ReadAsStringAsync();
        Assert.Contains("conflict", rejectedBlockBody, StringComparison.OrdinalIgnoreCase);

        var journal = await Ok(first.PostAsJsonAsync("/api/property-manager/p0/accounting/journals", new
        {
            sourceType = "RENT_RECEIPT", currency = "JMD", accountingDate = DateOnly.FromDateTime(DateTime.UtcNow), memo = "Two-instance reversal proof", idempotencyKey = $"pg-journal-{Guid.NewGuid():N}",
            lines = new object[]
            {
                new { accountCode = "CASH_CLEARING:JMD", debit = 90m, credit = 0m },
                new { accountCode = $"OWNER_INCOME:{ownerId}:{propertyId}:JMD", debit = 0m, credit = 90m, ownerUserId = (Guid?)ownerId, propertyId = (Guid?)propertyId }
            }
        }));
        var journalId = journal.GetProperty("id").GetGuid(); var reversalKey = $"pg-reversal-{journalId:N}";
        var reversalResponses = await Task.WhenAll(first.PostAsJsonAsync($"/api/property-manager/p0/accounting/journals/{journalId}/reverse", new { reason = "Duplicate provider correction", idempotencyKey = reversalKey }), second.PostAsJsonAsync($"/api/property-manager/p0/accounting/journals/{journalId}/reverse", new { reason = "Duplicate provider correction", idempotencyKey = reversalKey }));
        var reversals = await Task.WhenAll(reversalResponses.Select(response => Ok(Task.FromResult(response))));
        Assert.Equal(reversals[0].GetProperty("id").GetGuid(), reversals[1].GetProperty("id").GetGuid());

        var approval = await Ok(first.PostAsJsonAsync("/api/property-manager/p0/approvals", new { ownerUserId = ownerId, propertyId, approvalType = "MAINTENANCE", description = "Concurrent approval proof", amount = 75m, currency = "JMD", expiresAt = DateTimeOffset.UtcNow.AddDays(2), idempotencyKey = $"pg-approval-{Guid.NewGuid():N}" }));
        var approvalId = approval.GetProperty("id").GetGuid(); var approvalVersion = approval.GetProperty("rowVersion").GetInt64(); var decisionKey = $"pg-decision-{approvalId:N}";
        using var firstOwner = firstFactory.CreateClient(); using var secondOwner = secondFactory.CreateClient();
        SignIn(firstOwner, ownerId, UserRole.Owner); SignIn(secondOwner, ownerId, UserRole.Owner);
        var decisions = await Task.WhenAll(firstOwner.PostAsJsonAsync($"/api/property-manager/p0/approvals/{approvalId}/decision", new { status = "APPROVED", reason = "Approved after evidence review", rowVersion = approvalVersion, idempotencyKey = decisionKey }), secondOwner.PostAsJsonAsync($"/api/property-manager/p0/approvals/{approvalId}/decision", new { status = "APPROVED", reason = "Approved after evidence review", rowVersion = approvalVersion, idempotencyKey = decisionKey }));
        var decisionBodies = await Task.WhenAll(decisions.Select(response => Ok(Task.FromResult(response))));
        Assert.All(decisionBodies, body => Assert.Equal("APPROVED", body.GetProperty("status").GetString()));
        Assert.Equal(decisionBodies[0].GetProperty("id").GetGuid(), decisionBodies[1].GetProperty("id").GetGuid());

        var maintenance = await Ok(first.PostAsJsonAsync("/api/property-manager/professional/maintenance", new { ownerUserId = ownerId, propertyId, title = "Concurrent ledger posting", description = "Prove exactly-once financial close" }));
        var maintenanceId = maintenance.GetProperty("id").GetGuid();
        await Ok(first.PostAsJsonAsync($"/api/property-manager/professional/maintenance/{maintenanceId}/cost-lines", new { lineType = "LABOR", responsibility = "OWNER", description = "Concurrent repair", amount = 45m, currency = "JMD", idempotencyKey = $"pg-cost-{maintenanceId:N}" }));
        maintenance = (await first.GetFromJsonAsync<JsonElement[]>("/api/property-manager/professional/maintenance"))!.Single(item => item.GetProperty("id").GetGuid() == maintenanceId);
        foreach (var status in new[] { "TRIAGED", "OWNER_APPROVAL" })
            maintenance = await Ok(first.PatchAsJsonAsync($"/api/property-manager/professional/maintenance/{maintenanceId}", new { status, rowVersion = maintenance.GetProperty("rowVersion").GetInt64(), details = $"Advance to {status}" }));
        var maintenanceApproval = await Ok(first.PostAsJsonAsync("/api/property-manager/p0/approvals", new { ownerUserId = ownerId, propertyId, approvalType = "MAINTENANCE", description = "Approve concurrent repair", amount = 45m, currency = "JMD", sourceType = "MAINTENANCE", sourceId = maintenanceId, expiresAt = DateTimeOffset.UtcNow.AddDays(2), idempotencyKey = $"pg-maintenance-approval-{maintenanceId:N}" }));
        var maintenanceApprovalId = maintenanceApproval.GetProperty("id").GetGuid();
        await Ok(firstOwner.PostAsJsonAsync($"/api/property-manager/p0/approvals/{maintenanceApprovalId}/decision", new { status = "APPROVED", reason = "Approved concurrent repair", rowVersion = maintenanceApproval.GetProperty("rowVersion").GetInt64(), idempotencyKey = $"pg-maintenance-decision-{maintenanceApprovalId:N}" }));
        foreach (var status in new[] { "ASSIGNED", "SCHEDULED", "IN_PROGRESS" })
            maintenance = await Ok(first.PatchAsJsonAsync($"/api/property-manager/professional/maintenance/{maintenanceId}", new { status, rowVersion = maintenance.GetProperty("rowVersion").GetInt64(), ownerApprovalId = maintenanceApprovalId, details = $"Advance to {status}" }));
        var completion = new { status = "COMPLETED", rowVersion = maintenance.GetProperty("rowVersion").GetInt64(), ownerApprovalId = maintenanceApprovalId, details = "Complete once across both instances" };
        var completionResponses = await Task.WhenAll(first.PatchAsJsonAsync($"/api/property-manager/professional/maintenance/{maintenanceId}", completion), second.PatchAsJsonAsync($"/api/property-manager/professional/maintenance/{maintenanceId}", completion));
        Assert.Equal(1, completionResponses.Count(response => response.IsSuccessStatusCode));
        Assert.Equal(1, completionResponses.Count(response => response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict));
        await using (var verificationDb = new NestyStayDbContext(options))
            Assert.Equal(1, await verificationDb.MilestoneP0Journals.CountAsync(x => x.SourceType == "MAINTENANCE_EXPENSE" && x.SourceId == maintenanceId && !x.IsDeleted));

        var template = await Ok(first.PostAsJsonAsync("/api/property-manager/professional/checklist-templates", new { name = "Concurrent safety", workflowType = "INSPECTION", itemsJson = "[{\"id\":\"gate\",\"label\":\"Gate secured\",\"required\":true,\"completed\":false}]" }));
        await Ok(first.PutAsJsonAsync($"/api/property-manager/professional/properties/{propertyId}/checklist-template", new { templateId = template.GetProperty("id").GetGuid() }));
        var inspection = await Ok(first.PostAsJsonAsync("/api/property-manager/professional/inspections", new { propertyId, inspectionType = "SAFETY", scheduledAt = DateTimeOffset.UtcNow.AddDays(1) }));
        var inspectionId = inspection.GetProperty("id").GetGuid();
        await Ok(first.PatchAsJsonAsync($"/api/property-manager/professional/inspections/{inspectionId}", new { status = "FAILED", evidenceJson = "[]", findingsJson = "[{\"checklistItemId\":\"gate\",\"finding\":\"Gate latch failed\",\"severity\":\"HIGH\",\"correctiveRequired\":true}]", checklistJson = "[{\"id\":\"gate\",\"label\":\"Gate secured\",\"required\":true,\"completed\":true}]", rowVersion = inspection.GetProperty("rowVersion").GetInt64() }));
        var correctiveRequest = new { scope = "Replace failed gate latch" };
        var correctiveResponses = await Task.WhenAll(first.PostAsJsonAsync($"/api/property-manager/professional/inspections/{inspectionId}/work-order", correctiveRequest), second.PostAsJsonAsync($"/api/property-manager/professional/inspections/{inspectionId}/work-order", correctiveRequest));
        var correctiveBodies = await Task.WhenAll(correctiveResponses.Select(response => Ok(Task.FromResult(response))));
        Assert.Equal(correctiveBodies[0].GetProperty("id").GetGuid(), correctiveBodies[1].GetProperty("id").GetGuid());
        await using (var verificationDb = new NestyStayDbContext(options))
            Assert.Equal(1, await verificationDb.MilestoneWorkOrders.CountAsync(x => x.SourceInspectionId == inspectionId && !x.IsDeleted));
    }

    private static void SignIn(HttpClient client, Guid userId, UserRole role) => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(userId, role));

    private static async Task<JsonElement> Ok(Task<HttpResponseMessage> pending)
    {
        using var response = await pending; var body = await response.Content.ReadAsStringAsync(); Assert.True(response.IsSuccessStatusCode, $"Expected success but received {(int)response.StatusCode}: {body}"); return JsonSerializer.Deserialize<JsonElement>(body);
    }

    private sealed class PostgreSqlApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = connectionString,
                ["BackgroundJobs:Enabled"] = "false",
                ["Security:AdminTokenSha256"] = AdminTokenAuthenticationHandler.ComputeSha256Hex(NestyStayApiFactory.AdminToken),
                ["Security:OperatorTokenSha256"] = AdminTokenAuthenticationHandler.ComputeSha256Hex(NestyStayApiFactory.OperatorToken)
            }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<NestyStayDbContext>(); services.RemoveAll<DbContextOptions<NestyStayDbContext>>(); services.RemoveAll<IGoogleIdentityValidator>();
                services.AddDbContext<NestyStayDbContext>(options => options.UseNpgsql(connectionString));
                services.AddSingleton<IGoogleIdentityValidator, PostgreSqlTestGoogleIdentityValidator>();
            });
        }
    }

    private sealed class PostgreSqlTestGoogleIdentityValidator : IGoogleIdentityValidator
    {
        public string ProviderName => "PostgreSQL integration test";
        public bool IsConfigured => true;
        public Task<GoogleIdentity> ValidateAsync(string credential, CancellationToken cancellationToken) => throw new InvalidOperationException("Google login is outside this integration test.");
    }
}
