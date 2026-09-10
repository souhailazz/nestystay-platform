using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using NestyStay.Domain;

namespace NestyStay.Api.Tests;

public sealed class PropertyManagerP0EndpointTests(NestyStayApiFactory factory) : IClassFixture<NestyStayApiFactory>
{
    private const string Api = "/api/property-manager/p0";

    [Fact]
    public async Task OwnerAgreementFeeJournalAndStatementRoundTripThroughPostgresModel()
    {
        using var client = factory.CreateClient();
        var data = await Portfolio(client);
        var profile = await Ok(client.PutAsJsonAsync($"{Api}/owners/{data.Owner}/profile", new { legalName = "Owner Legal", contactEmail = data.Email, contactPhone = "+18765550100", billingAddress = "Kingston", preferredCurrency = "JMD", timeZone = "America/Jamaica", billingMetadataJson = "{\"invoiceDay\":15}", paymentProviderCustomerReference = "cus_local_owner", operationalMetadataJson = "{}", notes = "Primary owner" }));
        Assert.Equal("ACTIVE", profile.GetProperty("status").GetString());
        Assert.Equal("cus_local_owner", profile.GetProperty("paymentProviderCustomerReference").GetString());
        Assert.Equal(15, JsonDocument.Parse(profile.GetProperty("billingMetadataJson").GetString()!).RootElement.GetProperty("invoiceDay").GetInt32());
        var agreement = await Ok(client.PostAsJsonAsync($"{Api}/agreements", new { ownerUserId = data.Owner, propertyId = data.Property, effectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow), currency = "JMD", termsJson = "{}", feeRuleJson = "[]", maintenanceApprovalLimit = 500m, expenseApprovalLimit = 500m }));
        Assert.Equal("DRAFT", agreement.GetProperty("status").GetString());
        await Ok(client.PostAsync($"{Api}/agreements/{agreement.GetProperty("id").GetGuid()}/activate", null));
        await Ok(client.PostAsJsonAsync($"{Api}/fees", new { ownerUserId = data.Owner, propertyId = data.Property, category = "MANAGEMENT", ruleType = "PERCENTAGE", calculationBasis = "COLLECTED_RENT", currency = "JMD", percentage = 10m, fixedAmount = 0m, minimumAmount = 0m, cleaningMarkup = 0m, maintenanceMarkup = 0m, effectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow) }));
        var incomeAccount = $"OWNER_INCOME:{data.Owner}:{data.Property}:JMD";
        var journal = await Ok(client.PostAsJsonAsync($"{Api}/accounting/journals", new { sourceType = "RENT_RECEIPT", currency = "JMD", accountingDate = DateOnly.FromDateTime(DateTime.UtcNow), memo = "Rent collected", idempotencyKey = Guid.NewGuid().ToString("N"), reconcile = true, reconciliationReference = "BANK-001", lines = new object[] { new { accountCode = "CASH_CLEARING:JMD", debit = 100m, credit = 0m, ownerUserId = (Guid?)null, propertyId = (Guid?)null }, new { accountCode = incomeAccount, debit = 0m, credit = 100m, ownerUserId = (Guid?)data.Owner, propertyId = (Guid?)data.Property } } }));
        Assert.Equal(100m, journal.GetProperty("totalDebit").GetDecimal());
        Assert.Equal("RECONCILED", journal.GetProperty("reconciliationStatus").GetString());
        var fee = await Ok(client.PostAsJsonAsync($"{Api}/fees/post", new { ownerUserId = data.Owner, propertyId = data.Property, category = "MANAGEMENT", currency = "JMD", baseAmount = 100m, on = DateOnly.FromDateTime(DateTime.UtcNow), idempotencyKey = Guid.NewGuid().ToString("N") }));
        Assert.Equal(10m, fee.GetProperty("calculatedAmount").GetDecimal());
        var availability = await Ok(client.GetAsync($"{Api}/payouts/availability?ownerUserId={data.Owner}&currency=JMD&from={DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)):yyyy-MM-dd}&to={DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1):yyyy-MM-dd}"));
        Assert.Equal(100m, availability.GetProperty("availableReconciledCash").GetDecimal());
        var statement = await Ok(client.GetAsync($"{Api}/statements?ownerUserId={data.Owner}&propertyId={data.Property}&currency=JMD&from={DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)):yyyy-MM-dd}&to={DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1):yyyy-MM-dd}"));
        Assert.Equal(100m, statement.GetProperty("income").GetDecimal());
        Assert.Equal(10m, statement.GetProperty("managementFees").GetDecimal());
        Assert.False(statement.GetProperty("hasUnresolvedSuspense").GetBoolean());
    }

    [Fact]
    public async Task P0JournalIsImmutableAndCanOnlyBeReversed()
    {
        using var client = factory.CreateClient();
        var data = await Portfolio(client);
        var journal = await Ok(client.PostAsJsonAsync($"{Api}/accounting/journals", new { sourceType = "RENT_RECEIPT", currency = "USD", accountingDate = DateOnly.FromDateTime(DateTime.UtcNow), memo = "USD rent", idempotencyKey = Guid.NewGuid().ToString("N"), lines = new object[] { new { accountCode = "CASH_CLEARING:USD", debit = 50m, credit = 0m, ownerUserId = (Guid?)null, propertyId = (Guid?)null }, new { accountCode = $"OWNER_INCOME:{data.Owner}:{data.Property}:USD", debit = 0m, credit = 50m, ownerUserId = (Guid?)data.Owner, propertyId = (Guid?)data.Property } } }));
        var id = journal.GetProperty("id").GetGuid();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NestyStay.Infrastructure.Persistence.NestyStayDbContext>();
        var row = await db.MilestoneP0Journals.SingleAsync(x => x.Id == id);
        row.Memo = "tampered";
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        db.Entry(row).Reload();
        var reversal = await Ok(client.PostAsJsonAsync($"{Api}/accounting/journals/{id}/reverse", new { reason = "Correcting entry", idempotencyKey = Guid.NewGuid().ToString("N") }));
        Assert.Equal(id, reversal.GetProperty("reversalOfJournalId").GetGuid());
    }

    [Fact]
    public async Task PayoutRequiresDifferentApproverAndPostsPayoutJournal()
    {
        using var client = factory.CreateClient();
        var data = await Portfolio(client);
        await Ok(client.PostAsJsonAsync($"{Api}/accounting/journals", new { sourceType = "RENT_RECEIPT", currency = "JMD", accountingDate = DateOnly.FromDateTime(DateTime.UtcNow), memo = "Rent for payout", idempotencyKey = Guid.NewGuid().ToString("N"), reconcile = true, lines = new object[] { new { accountCode = "CASH_CLEARING:JMD", debit = 80m, credit = 0m, ownerUserId = (Guid?)null, propertyId = (Guid?)null }, new { accountCode = $"OWNER_INCOME:{data.Owner}:{data.Property}:JMD", debit = 0m, credit = 80m, ownerUserId = (Guid?)data.Owner, propertyId = (Guid?)data.Property } } }));
        var from = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)); var to = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        var batch = await Ok(client.PostAsJsonAsync($"{Api}/payouts/batches", new { ownerUserId = data.Owner, currency = "JMD", periodFrom = from, periodTo = to, idempotencyKey = Guid.NewGuid().ToString("N") }));
        var batchId = batch.GetProperty("id").GetGuid();
        Assert.Equal("PENDING_APPROVAL", batch.GetProperty("status").GetString());
        Assert.NotEqual(HttpStatusCode.OK, (await client.PostAsJsonAsync($"{Api}/payouts/batches/{batchId}/approve", new { reason = "Same person", rowVersion = batch.GetProperty("rowVersion").GetInt64() })).StatusCode);
        var approverEmail = $"p0-approver-{Guid.NewGuid():N}@nestystay.local";
        var approver = await Ok(client.PostAsJsonAsync("/api/auth/register", new { email = approverEmail, password = "NestyStay1", confirmPassword = "NestyStay1", displayName = "P0 approver", acceptedTerms = true, acceptedPrivacy = true, role = "PropertyManager" }));
        var approverId = approver.GetProperty("userId").GetGuid();
        SignIn(client, data.Manager, UserRole.PropertyManager);
        await Ok(client.PostAsJsonAsync($"{Api}/members", new { staffUserId = approverId, role = "APPROVER", ownerIds = new[] { data.Owner }, canManageFinance = true, canApprovePayouts = true, approvalLimit = 100m }));
        SignIn(client, approverId, UserRole.PropertyManager);
        var members = await Ok(client.GetAsync($"{Api}/members")); var membershipId = members.EnumerateArray().Single().GetProperty("id").GetGuid();
        await Ok(client.PostAsync($"{Api}/members/{membershipId}/accept", null));
        var staffHistory = await Ok(client.GetAsync($"{Api}/members/{membershipId}/history"));
        Assert.Contains(staffHistory.EnumerateArray(), item => item.GetProperty("eventType").GetString() == "INVITED");
        Assert.Contains(staffHistory.EnumerateArray(), item => item.GetProperty("eventType").GetString() == "ACCEPTED");
        SignIn(client, approverId, UserRole.PropertyManager);
        var refreshed = (await Ok(client.GetAsync($"{Api}/payouts/batches?ownerUserId={data.Owner}"))).EnumerateArray().Single();
        await Ok(client.PostAsJsonAsync($"{Api}/payouts/batches/{batchId}/approve", new { reason = "Second-person approval", rowVersion = refreshed.GetProperty("rowVersion").GetInt64() }));
        SignIn(client, data.Manager, UserRole.PropertyManager);
        var processed = await Ok(client.PostAsJsonAsync($"{Api}/payouts/batches/{batchId}/process", new { }));
        Assert.Equal("PAID", processed.GetProperty("status").GetString());
    }

    [Fact]
    public async Task JournalIdempotencyKeyCannotBeReusedForDifferentPayload()
    {
        using var client = factory.CreateClient();
        var data = await Portfolio(client);
        var key = Guid.NewGuid().ToString("N");
        var first = await Ok(client.PostAsJsonAsync($"{Api}/accounting/journals", new { sourceType = "RENT_RECEIPT", currency = "JMD", accountingDate = DateOnly.FromDateTime(DateTime.UtcNow), memo = "Idempotent rent", idempotencyKey = key, lines = new object[] { new { accountCode = "CASH_CLEARING:JMD", debit = 10m, credit = 0m }, new { accountCode = $"OWNER_INCOME:{data.Owner}:{data.Property}:JMD", debit = 0m, credit = 10m, ownerUserId = (Guid?)data.Owner, propertyId = (Guid?)data.Property } } }));
        Assert.Equal(key, first.GetProperty("idempotencyKey").GetString());
        using var response = await client.PostAsJsonAsync($"{Api}/accounting/journals", new { sourceType = "RENT_RECEIPT", currency = "JMD", accountingDate = DateOnly.FromDateTime(DateTime.UtcNow), memo = "Idempotent rent", idempotencyKey = key, lines = new object[] { new { accountCode = "CASH_CLEARING:JMD", debit = 20m, credit = 0m }, new { accountCode = $"OWNER_INCOME:{data.Owner}:{data.Property}:JMD", debit = 0m, credit = 20m, ownerUserId = (Guid?)data.Owner, propertyId = (Guid?)data.Property } } });
        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task OwnerBillingUsesReceivableAndPmRevenueAccounts()
    {
        using var client = factory.CreateClient();
        var data = await Portfolio(client);
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var billing = await Ok(client.PostAsJsonAsync($"{Api}/accounting/journals", new
        {
            sourceType = "OWNER_BILLING",
            currency = "JMD",
            accountingDate = date,
            memo = "Monthly management invoice",
            idempotencyKey = Guid.NewGuid().ToString("N"),
            lines = new object[]
            {
                new { accountCode = $"OWNER_RECEIVABLE:{data.Owner}:JMD", debit = 25m, credit = 0m, ownerUserId = (Guid?)data.Owner },
                new { accountCode = "PM_FEE_REVENUE:JMD", debit = 0m, credit = 25m }
            }
        }));
        Assert.Contains(billing.GetProperty("lines").EnumerateArray(), line => line.GetProperty("accountCode").GetString() == $"OWNER_RECEIVABLE:{data.Owner}:JMD");
        var statement = await Ok(client.GetAsync($"{Api}/statements?ownerUserId={data.Owner}&currency=JMD&from={date:yyyy-MM-dd}&to={date:yyyy-MM-dd}"));
        Assert.Equal(25m, statement.GetProperty("managementFees").GetDecimal());
        Assert.Equal(0m, statement.GetProperty("income").GetDecimal());
    }

    [Fact]
    public async Task ThirdPartyExpenseKeepsVendorPayableSeparateFromCash()
    {
        using var client = factory.CreateClient();
        var data = await Portfolio(client);
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var agreement = await Ok(client.PostAsJsonAsync($"{Api}/agreements", new { ownerUserId = data.Owner, propertyId = data.Property, effectiveFrom = date, currency = "USD", termsJson = "{}", feeRuleJson = "[]", maintenanceApprovalLimit = 100m, expenseApprovalLimit = 100m }));
        await Ok(client.PostAsync($"{Api}/agreements/{agreement.GetProperty("id").GetGuid()}/activate", null));
        var journal = await Ok(client.PostAsJsonAsync($"{Api}/accounting/journals", new
        {
            sourceType = "THIRD_PARTY_EXPENSE",
            currency = "USD",
            accountingDate = date,
            memo = "Vendor expense accrued",
            idempotencyKey = Guid.NewGuid().ToString("N"),
            lines = new object[]
            {
                new { accountCode = $"OWNER_EXPENSE:{data.Owner}:{data.Property}:USD", debit = 40m, credit = 0m, ownerUserId = (Guid?)data.Owner, propertyId = (Guid?)data.Property },
                new { accountCode = $"THIRD_PARTY_PAYABLE:{data.Owner}:{data.Property}:USD", debit = 0m, credit = 40m, ownerUserId = (Guid?)data.Owner, propertyId = (Guid?)data.Property }
            }
        }));
        Assert.Equal(40m, journal.GetProperty("totalCredit").GetDecimal());
        Assert.Contains(journal.GetProperty("lines").EnumerateArray(), line => line.GetProperty("accountCode").GetString() == $"THIRD_PARTY_PAYABLE:{data.Owner}:{data.Property}:USD");
    }

    [Fact]
    public async Task UnresolvedSuspenseBlocksFinalStatement()
    {
        using var client = factory.CreateClient();
        var data = await Portfolio(client);
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        await Ok(client.PostAsJsonAsync($"{Api}/accounting/journals", new { sourceType = "LEGACY_IMPORT", currency = "JMD", accountingDate = date, memo = "Ambiguous legacy balance", idempotencyKey = Guid.NewGuid().ToString("N"), lines = new object[] { new { accountCode = "CASH_CLEARING:JMD", debit = 20m, credit = 0m }, new { accountCode = "SUSPENSE:JMD", debit = 0m, credit = 20m } } }));
        var from = date.AddDays(-1); var to = date.AddDays(1);
        var preview = await Ok(client.GetAsync($"{Api}/statements?ownerUserId={data.Owner}&propertyId={data.Property}&currency=JMD&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}"));
        Assert.True(preview.GetProperty("hasUnresolvedSuspense").GetBoolean());
        using var response = await client.PostAsJsonAsync($"{Api}/statements/finalize", new { ownerUserId = data.Owner, propertyId = data.Property, currency = "JMD", from, to, idempotencyKey = Guid.NewGuid().ToString("N") });
        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task FinalStatementSnapshotCannotBeEditedThroughDbContext()
    {
        using var client = factory.CreateClient();
        var data = await Portfolio(client);
        var date = DateOnly.FromDateTime(DateTime.UtcNow); var from = date.AddDays(-1); var to = date.AddDays(1);
        await Ok(client.PostAsJsonAsync($"{Api}/accounting/journals", new { sourceType = "RENT_RECEIPT", currency = "USD", accountingDate = date, memo = "Snapshot rent", idempotencyKey = Guid.NewGuid().ToString("N"), reconcile = true, lines = new object[] { new { accountCode = "CASH_CLEARING:USD", debit = 30m, credit = 0m }, new { accountCode = $"OWNER_INCOME:{data.Owner}:{data.Property}:USD", debit = 0m, credit = 30m, ownerUserId = (Guid?)data.Owner, propertyId = (Guid?)data.Property } } }));
        var snapshot = await Ok(client.PostAsJsonAsync($"{Api}/statements/finalize", new { ownerUserId = data.Owner, propertyId = data.Property, currency = "USD", from, to, idempotencyKey = Guid.NewGuid().ToString("N") }));
        var snapshotId = snapshot.GetProperty("snapshotId").GetGuid();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NestyStay.Infrastructure.Persistence.NestyStayDbContext>();
        var row = await db.MilestoneP0StatementSnapshots.SingleAsync(x => x.Id == snapshotId);
        row.ClosingBalance = 999m;
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task ManagerCannotReadAnotherManagersAgreement()
    {
        using var client = factory.CreateClient();
        var data = await Portfolio(client);
        var agreement = await Ok(client.PostAsJsonAsync($"{Api}/agreements", new { ownerUserId = data.Owner, propertyId = data.Property, effectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow), currency = "JMD", termsJson = "{}", feeRuleJson = "[]", maintenanceApprovalLimit = 100m, expenseApprovalLimit = 100m }));
        var otherManager = Guid.NewGuid();
        SignIn(client, otherManager, UserRole.PropertyManager);
        using var response = await client.GetAsync($"{Api}/agreements/{agreement.GetProperty("id").GetGuid()}");
        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task OwnerPortalReturnsOnlyTheOwnerScopedFinancialWorkspace()
    {
        using var client = factory.CreateClient();
        var data = await Portfolio(client);
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        await Ok(client.PutAsJsonAsync($"{Api}/owners/{data.Owner}/profile", new
        {
            legalName = "Portal Owner",
            contactEmail = data.Email,
            contactPhone = "+18765550100",
            billingAddress = "Kingston",
            preferredCurrency = "JMD",
            timeZone = "America/Jamaica",
            operationalMetadataJson = "{}",
            notes = "Portal test"
        }));
        await Ok(client.PostAsJsonAsync($"{Api}/accounting/journals", new
        {
            sourceType = "RENT_RECEIPT",
            currency = "JMD",
            accountingDate = date,
            memo = "Owner portal rent",
            idempotencyKey = Guid.NewGuid().ToString("N"),
            reconcile = true,
            lines = new object[]
            {
                new { accountCode = "CASH_CLEARING:JMD", debit = 45m, credit = 0m },
                new { accountCode = $"OWNER_INCOME:{data.Owner}:{data.Property}:JMD", debit = 0m, credit = 45m, ownerUserId = (Guid?)data.Owner, propertyId = (Guid?)data.Property }
            }
        }));

        SignIn(client, data.Owner, UserRole.Owner);
        var portal = await Ok(client.GetAsync($"{Api}/owner/portal"));
        Assert.Equal(data.Owner, portal.GetProperty("profile").GetProperty("ownerUserId").GetGuid());
        Assert.Contains(portal.GetProperty("properties").EnumerateArray(), item => item.GetProperty("id").GetGuid() == data.Property);
        Assert.Contains(portal.GetProperty("transactions").EnumerateArray(), item => item.GetProperty("sourceType").GetString() == "RENT_RECEIPT");
        Assert.Equal(45m, portal.GetProperty("statement").GetProperty("income").GetDecimal());
    }

    [Fact]
    public async Task PropertyScopedStaffCannotReadAnUnrelatedOwnerWideRecord()
    {
        using var client = factory.CreateClient();
        var data = await Portfolio(client);
        var otherEmail = $"p0-other-{Guid.NewGuid():N}@nestystay.local";
        var other = await Ok(client.PostAsJsonAsync("/api/auth/register", new { email = otherEmail, password = "NestyStay1", confirmPassword = "NestyStay1", displayName = "Other owner", acceptedTerms = true, acceptedPrivacy = true, role = "Owner" }));
        var otherOwner = other.GetProperty("userId").GetGuid();
        SignIn(client, data.Manager, UserRole.PropertyManager);
        await Ok(client.PostAsJsonAsync("/api/property-manager/owners", new { email = otherEmail, displayName = "Other owner" }));
        await Ok(client.PutAsJsonAsync($"{Api}/owners/{otherOwner}/profile", new { legalName = "Other Legal", contactEmail = otherEmail, contactPhone = "+18765550101", billingAddress = "Montego Bay", preferredCurrency = "JMD", timeZone = "America/Jamaica", operationalMetadataJson = "{}", notes = "Other portfolio owner" }));
        var staff = await Ok(client.PostAsJsonAsync("/api/auth/register", new { email = $"p0-scope-{Guid.NewGuid():N}@nestystay.local", password = "NestyStay1", confirmPassword = "NestyStay1", displayName = "Scoped staff", acceptedTerms = true, acceptedPrivacy = true, role = "PropertyManager" }));
        var staffId = staff.GetProperty("userId").GetGuid();
        var invitation = await Ok(client.PostAsJsonAsync($"{Api}/members", new { staffUserId = staffId, role = "OPERATIONS", propertyIds = new[] { data.Property }, ownerIds = Array.Empty<Guid>(), canManageFinance = false, canApprovePayouts = false, approvalLimit = 0m }));
        SignIn(client, staffId, UserRole.PropertyManager);
        await Ok(client.PostAsync($"{Api}/members/{invitation.GetProperty("id").GetGuid()}/accept", null));
        using var response = await client.GetAsync($"{Api}/owners/{otherOwner}/profile");
        Assert.True(response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden);
    }

    private async Task<(Guid Manager, Guid Owner, Guid Property, string Email)> Portfolio(HttpClient client)
    {
        var email = $"p0-{Guid.NewGuid():N}@nestystay.local";
        var registered = await Ok(client.PostAsJsonAsync("/api/auth/register", new { email, password = "NestyStay1", confirmPassword = "NestyStay1", displayName = "P0 owner", phone = "+15550104003", acceptedTerms = true, acceptedPrivacy = true, role = "Owner" }));
        var owner = registered.GetProperty("userId").GetGuid(); var manager = Guid.NewGuid(); SignIn(client, manager, UserRole.PropertyManager);
        await Ok(client.PostAsJsonAsync("/api/property-manager/owners", new { email, displayName = "P0 owner" }));
        var property = await Ok(client.PostAsJsonAsync("/api/property-manager/properties", new { ownerUserId = owner, title = "P0 unit", unitNumber = "P0-1", address = "Kingston" }));
        return (manager, owner, property.GetProperty("id").GetGuid(), email);
    }

    private static void SignIn(HttpClient client, Guid userId, UserRole role) => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(userId, role));

    private static async Task<JsonElement> Ok(Task<HttpResponseMessage> action)
    {
        using var response = await action; var body = await response.Content.ReadAsStringAsync(); Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {body}"); return JsonDocument.Parse(body).RootElement.Clone();
    }
}
