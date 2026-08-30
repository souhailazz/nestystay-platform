using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace NestyStay.Api.Tests;

public sealed class BadgeAuthorizationTests : IClassFixture<NestyStayApiFactory>
{
    private readonly NestyStayApiFactory _factory;

    public BadgeAuthorizationTests(NestyStayApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task BadgeMutationsRejectAnonymousAndHostControlledEligibilityFacts()
    {
        using var client = _factory.CreateClient();
        var host = await RegisterAndLoginHostAsync(client);
        var request = new
        {
            subjectType = "Host",
            subjectId = host.UserId,
            level = "Wellness",
            hostVerificationPassed = true,
            completedApprovedBookings = int.MaxValue,
            hasPropertyAddress = true,
            hasWellnessSubscription = true,
            paymentSucceeded = true
        };

        client.DefaultRequestHeaders.Authorization = null;
        var anonymousEligibility = await client.PostAsJsonAsync("/api/badges-pricing/badges/eligibility", request);
        var anonymousPurchase = await client.PostAsJsonAsync("/api/badges-pricing/badges/purchase", request);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousEligibility.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousPurchase.StatusCode);

        client.DefaultRequestHeaders.Authorization = Bearer(host.AccessToken);
        var hostEligibility = await client.PostAsJsonAsync("/api/badges-pricing/badges/eligibility", request);
        var hostPurchase = await client.PostAsJsonAsync("/api/badges-pricing/badges/purchase", request);
        Assert.Equal(HttpStatusCode.Forbidden, hostEligibility.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, hostPurchase.StatusCode);
    }

    [Fact]
    public async Task HostCanReadOwnBadgeAccessButNotAnotherHostsDataOrUnfilteredAssignments()
    {
        using var client = _factory.CreateClient();
        var host = await RegisterAndLoginHostAsync(client);
        client.DefaultRequestHeaders.Authorization = Bearer(host.AccessToken);

        var ownFeatures = await client.GetAsync($"/api/badges-pricing/badges/features/Host/{host.UserId}");
        var ownAssignments = await client.GetAsync($"/api/badges-pricing/badges/assignments?subjectType=Host&subjectId={host.UserId}");
        var anotherHostsFeatures = await client.GetAsync($"/api/badges-pricing/badges/features/Host/{Guid.NewGuid()}");
        var unfilteredAssignments = await client.GetAsync("/api/badges-pricing/badges/assignments");

        Assert.Equal(HttpStatusCode.OK, ownFeatures.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ownAssignments.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, anotherHostsFeatures.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, unfilteredAssignments.StatusCode);
    }

    [Fact]
    public async Task LogoutInvalidatesTheServerSessionToken()
    {
        using var client = _factory.CreateClient();
        var host = await RegisterAndLoginHostAsync(client);
        client.DefaultRequestHeaders.Authorization = Bearer(host.AccessToken);

        var beforeLogout = await client.GetAsync("/api/auth/profile");
        var logout = await client.PostAsync("/api/auth/logout", null);
        var afterLogout = await client.GetAsync("/api/auth/profile");

        Assert.Equal(HttpStatusCode.OK, beforeLogout.StatusCode);
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact]
    public async Task PropertyCreationUsesAuthenticatedHostIdentityAndActiveBadge()
    {
        using var client = _factory.CreateClient();
        var host = await RegisterAndLoginHostAsync(client);
        client.DefaultRequestHeaders.Authorization = Bearer(host.AccessToken);

        var spoofedVerification = await client.PostAsJsonAsync("/api/properties", new
        {
            hostUserId = Guid.NewGuid(),
            hostName = "Spoofed Host",
            hostEmail = "spoofed@example.com",
            title = "Verification gated listing",
            location = "Kingston",
            country = "Jamaica",
            nightlyRate = 200m,
            currency = "USD",
            badgeLevel = "Wellness",
            guestVerificationEnabled = true,
            insuraGuestEnabled = false,
            cancellationPolicy = "Flexible"
        });

        var freeListing = await client.PostAsJsonAsync("/api/properties", new
        {
            hostUserId = Guid.NewGuid(),
            hostName = "Spoofed Host",
            hostEmail = "spoofed@example.com",
            title = "Free listing",
            location = "Kingston",
            country = "Jamaica",
            nightlyRate = 200m,
            currency = "USD",
            badgeLevel = "Wellness",
            guestVerificationEnabled = false,
            insuraGuestEnabled = false,
            cancellationPolicy = "Flexible"
        });

        Assert.Equal(HttpStatusCode.BadRequest, spoofedVerification.StatusCode);
        Assert.Equal(HttpStatusCode.OK, freeListing.StatusCode);
        var listing = await freeListing.Content.ReadFromJsonAsync<PropertyResponse>();
        Assert.NotNull(listing);
        Assert.Equal(host.UserId, listing.HostUserId);
        Assert.Equal("Badge Security Host", listing.HostName);
        Assert.Equal("Free", listing.BadgeLevel);

        var updated = await client.PutAsJsonAsync($"/api/properties/{listing.Id}", new
        {
            hostName = "Another Spoof",
            hostEmail = "another-spoof@example.com",
            title = "Free listing edited",
            location = "Montego Bay",
            country = "Jamaica",
            nightlyRate = 210m,
            currency = "USD",
            badgeLevel = "Wellness",
            guestVerificationEnabled = false,
            insuraGuestEnabled = false,
            cancellationPolicy = "Flexible"
        });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var updatedListing = await updated.Content.ReadFromJsonAsync<PropertyResponse>();
        Assert.NotNull(updatedListing);
        Assert.Equal(host.UserId, updatedListing.HostUserId);
        Assert.Equal("Badge Security Host", updatedListing.HostName);
        Assert.Equal("Free", updatedListing.BadgeLevel);

        var owned = await client.GetAsync("/api/properties/owned");
        Assert.Equal(HttpStatusCode.OK, owned.StatusCode);
        var ownedListings = await owned.Content.ReadFromJsonAsync<List<PropertyResponse>>();
        Assert.NotNull(ownedListings);
        Assert.Contains(ownedListings, item => item.Id == listing.Id);

        var otherHost = await RegisterAndLoginHostAsync(client);
        client.DefaultRequestHeaders.Authorization = Bearer(otherHost.AccessToken);
        var otherOwned = await client.GetAsync("/api/properties/owned");
        Assert.Equal(HttpStatusCode.OK, otherOwned.StatusCode);
        var otherListings = await otherOwned.Content.ReadFromJsonAsync<List<PropertyResponse>>();
        Assert.NotNull(otherListings);
        Assert.DoesNotContain(otherListings, item => item.Id == listing.Id);
    }

    private static async Task<HostSession> RegisterAndLoginHostAsync(HttpClient client)
    {
        var email = $"badge-host-{Guid.NewGuid():N}@test.local";
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Password123!",
            confirmPassword = "Password123!",
            displayName = "Badge Security Host",
            phone = "+1 555 0100",
            acceptedTerms = true,
            acceptedPrivacy = true,
            role = "Host"
        });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var session = await login.Content.ReadFromJsonAsync<HostSession>();
        Assert.NotNull(session);
        Assert.False(string.IsNullOrWhiteSpace(session.AccessToken));
        return session;
    }

    private static AuthenticationHeaderValue Bearer(string token) => new("Bearer", token);

    private sealed record HostSession(Guid UserId, string AccessToken, bool RequiresTwoFactor);

    private sealed record PropertyResponse(Guid Id, Guid HostUserId, string HostName, string BadgeLevel);
}
