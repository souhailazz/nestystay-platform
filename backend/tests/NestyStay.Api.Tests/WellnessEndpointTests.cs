using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace NestyStay.Api.Tests;

public sealed class WellnessEndpointTests : IClassFixture<NestyStayApiFactory>
{
    private readonly NestyStayApiFactory _factory;

    public WellnessEndpointTests(NestyStayApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task OfficerOnboardingAndAdminVerificationEnforceMilestoneRules()
    {
        using var client = _factory.CreateClient();

        var retiredResponse = await client.PostAsJsonAsync("/api/wellness/officers", new
        {
            badgeNumber = $"JCF-R-{Guid.NewGuid():N}"[..16],
            parish = "St. Ann",
            coverageArea = "Ocho Rios",
            isActiveOffDuty = false,
            isRetired = true
        });
        Assert.Equal(HttpStatusCode.OK, retiredResponse.StatusCode);
        var retired = await retiredResponse.Content.ReadFromJsonAsync<OfficerResponse>();
        Assert.NotNull(retired);
        Assert.Equal("Rejected", retired.VerificationStatus);

        var badgeNumber = $"JCF-{Guid.NewGuid():N}"[..14];
        var officerResponse = await client.PostAsJsonAsync("/api/wellness/officers", new
        {
            badgeNumber,
            parish = "St. Ann",
            coverageArea = "Ocho Rios",
            isActiveOffDuty = true,
            isRetired = false
        });
        Assert.Equal(HttpStatusCode.OK, officerResponse.StatusCode);
        var officer = await officerResponse.Content.ReadFromJsonAsync<OfficerResponse>();
        Assert.NotNull(officer);
        Assert.Equal("Pending", officer.VerificationStatus);
        Assert.DoesNotContain(officer.BadgeNumber, " ");

        var duplicateResponse = await client.PostAsJsonAsync("/api/wellness/officers", new
        {
            badgeNumber,
            parish = "St. Ann",
            coverageArea = "Ocho Rios",
            isActiveOffDuty = true,
            isRetired = false
        });
        Assert.Equal(HttpStatusCode.BadRequest, duplicateResponse.StatusCode);

        var queueWithoutToken = await client.GetAsync("/api/wellness/officers");
        Assert.Equal(HttpStatusCode.Unauthorized, queueWithoutToken.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.OperatorToken);
        var queueAsOperator = await client.GetAsync("/api/wellness/officers");
        Assert.Equal(HttpStatusCode.Forbidden, queueAsOperator.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.AdminToken);
        var queueAsAdmin = await client.GetAsync("/api/wellness/officers");
        Assert.Equal(HttpStatusCode.OK, queueAsAdmin.StatusCode);

        var approvedResponse = await client.PostAsJsonAsync($"/api/wellness/officers/{officer.Id}/approve", new
        {
            reason = "JCF active off-duty status confirmed.",
            reviewedBy = "QA admin"
        });
        Assert.Equal(HttpStatusCode.OK, approvedResponse.StatusCode);
        var approved = await approvedResponse.Content.ReadFromJsonAsync<OfficerResponse>();
        Assert.NotNull(approved);
        Assert.Equal("Verified", approved.VerificationStatus);
        Assert.Equal("Available", approved.AvailabilityStatus);
        Assert.Contains("Verified", approved.FreeBadges);
        Assert.Contains("Trusted", approved.FreeBadges);

        var suspendedResponse = await client.PostAsJsonAsync($"/api/wellness/officers/{officer.Id}/suspend", new
        {
            reason = "Temporary review"
        });
        Assert.Equal(HttpStatusCode.OK, suspendedResponse.StatusCode);
        var suspended = await suspendedResponse.Content.ReadFromJsonAsync<OfficerResponse>();
        Assert.NotNull(suspended);
        Assert.Equal("Suspended", suspended.VerificationStatus);

        var reactivatedResponse = await client.PostAsJsonAsync($"/api/wellness/officers/{officer.Id}/reactivate", new
        {
            reason = "Cleared"
        });
        Assert.Equal(HttpStatusCode.OK, reactivatedResponse.StatusCode);
        var reactivated = await reactivatedResponse.Content.ReadFromJsonAsync<OfficerResponse>();
        Assert.NotNull(reactivated);
        Assert.Equal("Verified", reactivated.VerificationStatus);
    }

    [Fact]
    public async Task WellnessBookingReportAndPayoutFlowEnforceEligibilityAndPaymentState()
    {
        using var client = _factory.CreateClient();
        var hostUserId = Guid.NewGuid();
        var wellnessProperty = await CreatePropertyAsync(client, hostUserId, "Wellness");
        var freeProperty = await CreatePropertyAsync(client, hostUserId, "Free");

        var freeQuote = await client.PostAsJsonAsync("/api/wellness/quote", new
        {
            hostUserId,
            propertyId = freeProperty.Id,
            visitType = "DriveByPatrol",
            scheduledAt = DateTimeOffset.UtcNow.AddHours(2),
            parish = "St. Ann",
            area = "Ocho Rios"
        });
        Assert.Equal(HttpStatusCode.OK, freeQuote.StatusCode);
        var locked = await freeQuote.Content.ReadFromJsonAsync<QuoteResponse>();
        Assert.NotNull(locked);
        Assert.False(locked.Eligible);

        var blockedCreate = await client.PostAsJsonAsync("/api/wellness/visits", new
        {
            hostUserId,
            propertyId = freeProperty.Id,
            visitType = "DriveByPatrol",
            scheduledAt = DateTimeOffset.UtcNow.AddHours(2),
            parish = "St. Ann",
            area = "Ocho Rios"
        });
        Assert.Equal(HttpStatusCode.BadRequest, blockedCreate.StatusCode);

        var officer = await OnboardOfficerAsync(client, "St. Ann");
        var unverified = await OnboardOfficerAsync(client, "St. Ann");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.AdminToken);
        var approvedResponse = await client.PostAsJsonAsync($"/api/wellness/officers/{officer.Id}/approve", new { reason = "Approved" });
        Assert.Equal(HttpStatusCode.OK, approvedResponse.StatusCode);

        var scheduledAt = DateTimeOffset.UtcNow.AddSeconds(1.5);
        var quoteResponse = await client.PostAsJsonAsync("/api/wellness/quote", new
        {
            hostUserId,
            propertyId = wellnessProperty.Id,
            visitType = "StandardWellnessCheck",
            scheduledAt,
            parish = "St. Ann",
            area = "Ocho Rios"
        });
        Assert.Equal(HttpStatusCode.OK, quoteResponse.StatusCode);
        var quote = await quoteResponse.Content.ReadFromJsonAsync<QuoteResponse>();
        Assert.NotNull(quote);
        Assert.True(quote.Eligible);
        Assert.Equal("119", quote.EmergencyNumber);

        var createResponse = await client.PostAsJsonAsync("/api/wellness/visits", new
        {
            hostUserId,
            propertyId = wellnessProperty.Id,
            visitType = "StandardWellnessCheck",
            scheduledAt,
            parish = "St. Ann",
            area = "Ocho Rios"
        });
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        var visit = await createResponse.Content.ReadFromJsonAsync<VisitResponse>();
        Assert.NotNull(visit);
        Assert.Equal("Requested", visit.VisitStatus);
        Assert.Equal("Authorized", visit.PaymentStatus);

        var unverifiedAssign = await client.PostAsJsonAsync($"/api/wellness/visits/{visit.Id}/assign", new
        {
            officerId = unverified.Id
        });
        Assert.Equal(HttpStatusCode.BadRequest, unverifiedAssign.StatusCode);

        var assignResponse = await client.PostAsJsonAsync($"/api/wellness/visits/{visit.Id}/assign", new
        {
            officerId = officer.Id
        });
        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);
        var scheduled = await assignResponse.Content.ReadFromJsonAsync<VisitResponse>();
        Assert.NotNull(scheduled);
        Assert.Equal("Scheduled", scheduled.VisitStatus);
        Assert.Equal(officer.BadgeNumber, scheduled.OfficerBadgeNumber);

        var overlapVisitResponse = await client.PostAsJsonAsync("/api/wellness/visits", new
        {
            hostUserId,
            propertyId = wellnessProperty.Id,
            visitType = "DriveByPatrol",
            scheduledAt = scheduledAt.AddMinutes(10),
            parish = "St. Ann",
            area = "Ocho Rios"
        });
        Assert.Equal(HttpStatusCode.OK, overlapVisitResponse.StatusCode);
        var overlapVisit = await overlapVisitResponse.Content.ReadFromJsonAsync<VisitResponse>();
        Assert.NotNull(overlapVisit);
        var overlapAssign = await client.PostAsJsonAsync($"/api/wellness/visits/{overlapVisit.Id}/assign", new
        {
            officerId = officer.Id
        });
        Assert.Equal(HttpStatusCode.BadRequest, overlapAssign.StatusCode);

        var payoutBeforeReport = await client.PostAsJsonAsync($"/api/wellness/visits/{visit.Id}/payout", new
        {
            providerReference = "too-early"
        });
        Assert.Equal(HttpStatusCode.BadRequest, payoutBeforeReport.StatusCode);

        var wrongOfficerReport = await client.PostAsJsonAsync($"/api/wellness/visits/{visit.Id}/report", new
        {
            officerBadgeNumber = "JCF-WRONG",
            notes = "Wrong officer",
            photos = new[] { "local://wrong.jpg" }
        });
        Assert.Equal(HttpStatusCode.BadRequest, wrongOfficerReport.StatusCode);

        var earlyReport = await client.PostAsJsonAsync($"/api/wellness/visits/{visit.Id}/report", new
        {
            officerBadgeNumber = officer.BadgeNumber,
            notes = "Too early",
            photos = new[] { "local://early.jpg" }
        });
        Assert.Equal(HttpStatusCode.BadRequest, earlyReport.StatusCode);

        await Task.Delay(TimeSpan.FromSeconds(2));

        var reportResponse = await client.PostAsJsonAsync($"/api/wellness/visits/{visit.Id}/report", new
        {
            officerBadgeNumber = officer.BadgeNumber,
            notes = "Photo report submitted after the wellness check.",
            photos = new[] { "local://wellness-report.jpg" },
            locationMetadata = "{\"source\":\"api-test\"}"
        });
        Assert.Equal(HttpStatusCode.OK, reportResponse.StatusCode);
        var completed = await reportResponse.Content.ReadFromJsonAsync<VisitResponse>();
        Assert.NotNull(completed);
        Assert.Equal("Completed", completed.VisitStatus);
        Assert.Equal("Submitted", completed.ReportStatus);
        Assert.Equal("PayoutPending", completed.PaymentStatus);

        var cancelCompleted = await client.PostAsJsonAsync($"/api/wellness/visits/{visit.Id}/cancel", new
        {
            reason = "Should not cancel"
        });
        Assert.Equal(HttpStatusCode.BadRequest, cancelCompleted.StatusCode);

        var payoutResponse = await client.PostAsJsonAsync($"/api/wellness/visits/{visit.Id}/payout", new
        {
            providerReference = "local-paid",
            notes = "Local milestone payout"
        });
        Assert.Equal(HttpStatusCode.OK, payoutResponse.StatusCode);
        var payout = await payoutResponse.Content.ReadFromJsonAsync<PayoutResponse>();
        Assert.NotNull(payout);
        Assert.Equal("Paid", payout.Status);

        var duplicatePayout = await client.PostAsJsonAsync($"/api/wellness/visits/{visit.Id}/payout", new
        {
            providerReference = "local-paid-again"
        });
        Assert.Equal(HttpStatusCode.OK, duplicatePayout.StatusCode);
        var duplicate = await duplicatePayout.Content.ReadFromJsonAsync<PayoutResponse>();
        Assert.NotNull(duplicate);
        Assert.Equal(payout.Id, duplicate.Id);
        Assert.Equal("Paid", duplicate.Status);
    }

    private static async Task<PropertyResponse> CreatePropertyAsync(HttpClient client, Guid hostUserId, string badgeLevel)
    {
        var response = await client.PostAsJsonAsync("/api/properties", new
        {
            hostUserId,
            hostName = "Wellness Host",
            hostEmail = $"wellness-{Guid.NewGuid():N}@nestystay.local",
            title = $"{badgeLevel} Wellness Villa",
            location = "Ocho Rios, St. Ann",
            country = "Jamaica",
            nightlyRate = 150,
            currency = "USD",
            badgeLevel,
            guestVerificationEnabled = badgeLevel != "Free",
            insuraGuestEnabled = badgeLevel != "Free",
            cancellationPolicy = "Flexible",
            highlights = new[] { "Emergency 119 displayed", "Wellness-ready host" }
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PropertyResponse>())!;
    }

    private static async Task<OfficerResponse> OnboardOfficerAsync(HttpClient client, string parish)
    {
        client.DefaultRequestHeaders.Authorization = null;
        var response = await client.PostAsJsonAsync("/api/wellness/officers", new
        {
            badgeNumber = $"JCF-{Guid.NewGuid():N}"[..14],
            parish,
            coverageArea = "Ocho Rios",
            isActiveOffDuty = true,
            isRetired = false
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<OfficerResponse>())!;
    }

    private sealed record OfficerResponse(
        Guid Id,
        string BadgeNumber,
        string VerificationStatus,
        string AvailabilityStatus,
        IReadOnlyList<string> FreeBadges);

    private sealed record PropertyResponse(Guid Id);

    private sealed record QuoteResponse(bool Eligible, string EmergencyNumber);

    private sealed record VisitResponse(
        Guid Id,
        string VisitStatus,
        string PaymentStatus,
        string ReportStatus,
        string? OfficerBadgeNumber);

    private sealed record PayoutResponse(Guid Id, string Status);
}
