using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NestyStay.Domain;

namespace NestyStay.Api.Tests;

public sealed class M3M4EndpointTests : IClassFixture<NestyStayApiFactory>
{
    private readonly NestyStayApiFactory _factory;

    public M3M4EndpointTests(NestyStayApiFactory factory) => _factory = factory;

    [Fact]
    public async Task DirectoryProviderRequiresModerationAndPoliceUsesBadgeOnly()
    {
        using var client = _factory.CreateClient();
        var owner = Guid.NewGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(owner, UserRole.Host));

        var save = await client.PostAsJsonAsync("/api/directories/providers", new
        {
            kind = "LocalBusiness", category = "Tours", name = $"M4 Test Tours {owner:N}", parish = "St. Ann",
            badgeLevel = "Free", description = "A real persisted provider awaiting review.", availabilitySummary = "Daily",
            contactMode = "email@test.invalid", isBrickAndMortar = true, isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, save.StatusCode);
        using var savedJson = JsonDocument.Parse(await save.Content.ReadAsStringAsync());
        var slug = savedJson.RootElement.GetProperty("slug").GetString()!;
        Assert.Equal("PendingReview", savedJson.RootElement.GetProperty("status").GetString());
        Assert.False(savedJson.RootElement.GetProperty("isActive").GetBoolean());
        Assert.Equal("Platform messaging only", savedJson.RootElement.GetProperty("contactMode").GetString());

        client.DefaultRequestHeaders.Authorization = null;
        var hidden = await client.GetAsync($"/api/directories/providers/{slug}");
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.AdminToken);
        var moderate = await client.PostAsJsonAsync($"/api/directories/providers/{slug}/moderate", new { status = "approve", reason = "M4 API test" });
        Assert.Equal(HttpStatusCode.OK, moderate.StatusCode);
        var visible = await client.GetAsync($"/api/directories/providers/{slug}");
        Assert.Equal(HttpStatusCode.OK, visible.StatusCode);

        var policeSave = await client.PostAsJsonAsync("/api/directories/providers", new
        {
            kind = "Police", category = "Police Wellness", name = "Should never publish", parish = "St. Ann",
            badgeLevel = "Wellness", description = "invalid", availabilitySummary = "on call", contactMode = "direct"
        });
        Assert.Equal(HttpStatusCode.BadRequest, policeSave.StatusCode);

        client.DefaultRequestHeaders.Authorization = null;
        var policePublic = await client.GetAsync("/api/directories/providers?kind=Police");
        Assert.Equal(HttpStatusCode.Unauthorized, policePublic.StatusCode);
    }

    [Fact]
    public async Task PoliceDirectoryRequiresWellnessHostAccessAndOfficerRegistrationBindsIdentity()
    {
        using var client = _factory.CreateClient();
        var hostId = Guid.NewGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(hostId, UserRole.Host));
        var denied = await client.GetAsync("/api/directories/providers?kind=Police");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        var officerId = Guid.NewGuid();
        client.DefaultRequestHeaders.Authorization = null;
        var registration = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"officer-{officerId:N}@nestystay.local", password = "NestyStay1", confirmPassword = "NestyStay1",
            displayName = "Officer Applicant", phone = "+15550102031", acceptedTerms = true, acceptedPrivacy = true, role = "Officer"
        });
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);
        officerId = (await registration.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("userId").GetGuid();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(officerId, UserRole.Officer));
        var onboarding = await client.PostAsJsonAsync("/api/wellness/officers", new
        {
            userId = officerId, badgeNumber = $"JCF-{officerId:N}"[..16], parish = "St. Ann", coverageArea = "Ocho Rios",
            isActiveOffDuty = true, isRetired = false, verificationMetadata = "Applicant submitted active off-duty JCF evidence."
        });
        Assert.Equal(HttpStatusCode.OK, onboarding.StatusCode);
        var body = await onboarding.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Pending", body.GetProperty("verificationStatus").GetString());
    }

    [Fact]
    public async Task HostDirectoryAccessRequiresContractBadgeLevels()
    {
        using var client = _factory.CreateClient();
        var hostId = Guid.NewGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(hostId, UserRole.Host));
        foreach (var kind in new[] { "Custodian", "Trades", "LocalBusiness" })
        {
            var response = await client.GetAsync($"/api/directories/providers?kind={kind}");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        client.DefaultRequestHeaders.Authorization = null;
        var publicResponse = await client.GetAsync("/api/directories/providers?kind=LocalBusiness");
        Assert.Equal(HttpStatusCode.OK, publicResponse.StatusCode);
    }

    [Fact]
    public async Task ProviderSelfServiceRegistrationCreatesAReviewableDirectoryProfile()
    {
        using var client = _factory.CreateClient();
        var email = $"provider-{Guid.NewGuid():N}@nestystay.local";
        var registration = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email, password = "NestyStay1", confirmPassword = "NestyStay1", displayName = "Provider Account",
            phone = "+15550102034", acceptedTerms = true, acceptedPrivacy = true, role = "ServiceProvider"
        });
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "NestyStay1" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var accessToken = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var provider = await client.PostAsJsonAsync("/api/directories/providers", new
        {
            kind = "Trades", category = "Electrician", name = "Provider Account Services", parish = "St. Ann", badgeLevel = "Free",
            description = "Reviewable provider profile", availabilitySummary = "Weekdays", contactMode = "direct", isBrickAndMortar = false
        });
        Assert.True(provider.IsSuccessStatusCode, await provider.Content.ReadAsStringAsync());
        Assert.Equal("PendingReview", (await provider.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());
    }

    [Fact]
    public async Task DirectoryModerationQueueIncludesPendingAndSupportsRequestChanges()
    {
        using var client = _factory.CreateClient();
        var owner = Guid.NewGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(owner, UserRole.Host));
        var save = await client.PostAsJsonAsync("/api/directories/providers", new
        {
            kind = "Custodian", category = "Cleaning", name = $"Queue Provider {owner:N}", parish = "Kingston",
            badgeLevel = "Free", description = "Pending moderation queue record", availabilitySummary = "Daily", contactMode = "direct", isBrickAndMortar = false
        });
        Assert.Equal(HttpStatusCode.OK, save.StatusCode);
        var slug = (await save.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("slug").GetString()!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.AdminToken);
        var queue = await client.GetAsync("/api/directories/providers/moderation?status=PendingReview&query=Queue%20Provider");
        Assert.Equal(HttpStatusCode.OK, queue.StatusCode);
        var records = await queue.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.Contains(records!, item => item.GetProperty("slug").GetString() == slug);

        var review = await client.PostAsJsonAsync($"/api/directories/providers/{slug}/moderate", new { status = "request-changes", reason = "Upload a current business document." });
        Assert.Equal(HttpStatusCode.OK, review.StatusCode);
        Assert.Equal("ChangesRequested", (await review.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString());
    }

    [Fact]
    public async Task WellnessSubscriptionUsesContractPriceAndRenewalIsIdempotentWhileActive()
    {
        using var client = _factory.CreateClient();
        var hostId = Guid.NewGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(hostId, UserRole.Host));

        var started = await client.PostAsync("/api/wellness/subscriptions", null);
        Assert.Equal(HttpStatusCode.OK, started.StatusCode);
        var startedBody = await started.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(19m, startedBody.GetProperty("monthlyAmount").GetDecimal());
        Assert.Equal("Active", startedBody.GetProperty("status").GetString());
        Assert.Equal(1, startedBody.GetProperty("includedVisits").GetInt32());

        var renewed = await client.PostAsync("/api/wellness/subscriptions/renew", null);
        Assert.Equal(HttpStatusCode.OK, renewed.StatusCode);
        var renewedBody = await renewed.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(startedBody.GetProperty("id").GetGuid(), renewedBody.GetProperty("id").GetGuid());
        Assert.Equal("Active", renewedBody.GetProperty("status").GetString());
    }

    [Fact]
    public async Task QrValidationRejectsForgedAndMalformedTokensWithoutLeakingIdentity()
    {
        using var client = _factory.CreateClient();
        var spoofedOfficer = await client.PostAsJsonAsync("/api/wellness/officers", new
        {
            userId = Guid.NewGuid(), badgeNumber = $"SPOOF-{Guid.NewGuid():N}"[..16], parish = "St. Ann", coverageArea = "Ocho Rios", isActiveOffDuty = true, isRetired = false
        });
        Assert.Equal(HttpStatusCode.Forbidden, spoofedOfficer.StatusCode);
        var malformed = await client.PostAsJsonAsync("/api/access/qr/validate", new { token = "guessable", propertyId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.OK, malformed.StatusCode);
        var result = await malformed.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(result.GetProperty("valid").GetBoolean());

        var unauthorizedIssue = await client.PostAsync($"/api/access/qr/bookings/{Guid.NewGuid()}", null);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorizedIssue.StatusCode);
        var body = await malformed.Content.ReadAsStringAsync();
        Assert.DoesNotContain("guest", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("email", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task QrLifecycleBindsToApprovedBookingAndRejectsWrongPropertyAndRevocation()
    {
        using var client = _factory.CreateClient();
        var hostId = Guid.NewGuid();
        var guestEmail = $"qr-{Guid.NewGuid():N}@nestystay.local";
        var registration = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = guestEmail, password = "NestyStay1", confirmPassword = "NestyStay1", displayName = "QR Guest",
            phone = "+15550102030", acceptedTerms = true, acceptedPrivacy = true, role = "Guest"
        });
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);
        var registrationBody = await registration.Content.ReadFromJsonAsync<JsonElement>();
        var guestId = registrationBody.GetProperty("userId").GetGuid();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(hostId, UserRole.Host));
        var propertyResponse = await client.PostAsJsonAsync("/api/properties", new
        {
            hostUserId = hostId, hostName = "QR Host", hostEmail = "qr-host@nestystay.local", title = "QR Test Villa",
            location = "Ocho Rios", country = "Jamaica", nightlyRate = 120, currency = "USD", badgeLevel = "Free",
            guestVerificationEnabled = false, insuraGuestEnabled = false, cancellationPolicy = "Flexible"
        });
        Assert.Equal(HttpStatusCode.OK, propertyResponse.StatusCode);
        var propertyId = (await propertyResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(guestId, UserRole.Guest));
        var checkIn = DateOnly.FromDateTime(DateTime.UtcNow);
        var checkOut = checkIn.AddDays(1);
        var bookingResponse = await client.PostAsJsonAsync("/api/bookings", new { propertyId, guestUserId = Guid.NewGuid(), checkIn = checkIn.ToString("yyyy-MM-dd"), checkOut = checkOut.ToString("yyyy-MM-dd") });
        Assert.Equal(HttpStatusCode.OK, bookingResponse.StatusCode);
        var booking = await bookingResponse.Content.ReadFromJsonAsync<JsonElement>();
        var bookingId = booking.GetProperty("id").GetGuid();
        Assert.Equal("APPROVED", booking.GetProperty("status").GetString());

        var issue = await client.PostAsync($"/api/access/qr/bookings/{bookingId}", null);
        Assert.Equal(HttpStatusCode.OK, issue.StatusCode);
        var issued = await issue.Content.ReadFromJsonAsync<JsonElement>();
        var qrId = issued.GetProperty("id").GetGuid();
        var token = issued.GetProperty("token").GetString()!;
        Assert.True(token.Length >= 40);

        var wrongProperty = await client.PostAsJsonAsync("/api/access/qr/validate", new { token, propertyId = Guid.NewGuid() });
        Assert.Equal("WrongProperty", (await wrongProperty.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("result").GetString());
        var valid = await client.PostAsJsonAsync("/api/access/qr/validate", new { token, propertyId });
        var validBody = await valid.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(validBody.GetProperty("valid").GetBoolean());
        Assert.Equal(bookingId, validBody.GetProperty("bookingId").GetGuid());

        var revoke = await client.PostAsync($"/api/access/qr/{qrId}/revoke", null);
        Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);
        var rejected = await client.PostAsJsonAsync("/api/access/qr/validate", new { token, propertyId });
        Assert.Equal("Revoked", (await rejected.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("result").GetString());
    }
}
