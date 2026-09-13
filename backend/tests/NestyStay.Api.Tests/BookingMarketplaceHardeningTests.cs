using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NestyStay.Domain;

namespace NestyStay.Api.Tests;

public sealed class BookingMarketplaceHardeningTests : IClassFixture<NestyStayApiFactory>
{
    private readonly NestyStayApiFactory factory;

    public BookingMarketplaceHardeningTests(NestyStayApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task MarketplaceSearchReturnsRealPropertyDetailsAfterModeration()
    {
        using var client = factory.CreateClient();
        var hostId = Guid.NewGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(hostId, UserRole.Host));
        var create = await client.PostAsJsonAsync("/api/properties", new
        {
            hostName = "Marketplace Fixture Host",
            hostEmail = $"fixture-host-{hostId:N}@test.local",
            title = "Harbour View Search Fixture",
            location = "Kingston",
            parish = "St. Andrew",
            country = "Jamaica",
            nightlyRate = 210,
            currency = "USD",
            badgeLevel = "Verified",
            cancellationPolicy = "Flexible",
            description = "A complete search fixture.",
            bedrooms = 2,
            bathrooms = 2,
            maxGuests = 4,
            amenities = new[] { "Wi-Fi", "Workspace" },
            sleepingArrangements = new[] { "Bedroom: queen bed" },
            houseRules = new[] { "No smoking" },
            cleaningFee = 30,
            serviceFee = 20,
            latitude = 18.0179,
            longitude = -76.8099,
            imageUrl = "https://images.example.test/harbour.jpg",
            galleryUrls = new[] { "https://images.example.test/harbour.jpg" }
        });
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<PropertyBody>();
        Assert.NotNull(created);
        Assert.Equal("Pending", created!.ModerationStatus);
        Assert.Equal(4, created.MaxGuests);
        Assert.Equal(30, created.CleaningFee);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.AdminToken);
        var moderate = await client.PostAsJsonAsync($"/api/properties/{created.Id}/moderate", new { status = "Approved" });
        Assert.Equal(HttpStatusCode.OK, moderate.StatusCode);

        client.DefaultRequestHeaders.Authorization = null;
        var search = await client.GetFromJsonAsync<List<PropertyBody>>($"/api/properties?search=St.%20Andrew&checkIn=2099-01-10&checkOut=2099-01-13&adults=2");
        var result = Assert.Single(search!, item => item.Id == created.Id);
        Assert.Equal("St. Andrew", result.Parish);
        Assert.Equal(2, result.Bedrooms);
        Assert.Contains("Wi-Fi", result.Amenities!);
        Assert.Equal(18.0179m, result.Latitude);
    }

    [Fact]
    public async Task PropertyModerationRejectionPersistsReasonAndHidesListingFromPublicSearch()
    {
        using var client = factory.CreateClient();
        var hostId = Guid.NewGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(hostId, UserRole.Host));
        var create = await client.PostAsJsonAsync("/api/properties", new
        {
            hostName = "Moderation Fixture Host",
            hostEmail = $"moderation-host-{hostId:N}@test.local",
            title = "Hidden Moderation Fixture",
            location = "Negril",
            parish = "Westmoreland",
            country = "Jamaica",
            nightlyRate = 180,
            currency = "USD",
            cancellationPolicy = "Strict",
            maxGuests = 2
        });
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var property = await create.Content.ReadFromJsonAsync<PropertyBody>();
        Assert.NotNull(property);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.AdminToken);
        var moderate = await client.PostAsJsonAsync($"/api/properties/{property!.Id}/moderate", new
        {
            status = "Rejected",
            reason = "Please add a complete gallery before publishing."
        });
        Assert.Equal(HttpStatusCode.OK, moderate.StatusCode);
        var moderated = await moderate.Content.ReadFromJsonAsync<PropertyBody>();
        Assert.Equal("Rejected", moderated!.ModerationStatus);
        Assert.Equal("Please add a complete gallery before publishing.", moderated.ModerationReason);

        client.DefaultRequestHeaders.Authorization = null;
        var publicSearch = await client.GetFromJsonAsync<List<PropertyBody>>("/api/properties?search=Hidden%20Moderation%20Fixture");
        Assert.DoesNotContain(publicSearch!, item => item.Id == property.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/properties/{property.Id}")).StatusCode);
    }

    [Fact]
    public async Task HostVerificationAndHostBookingRejectionPersistTheirRealReasons()
    {
        using var client = factory.CreateClient();
        var seededHostId = await RegisterHostAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(seededHostId, UserRole.Host));
        var verification = await client.GetFromJsonAsync<HostVerificationBody>("/api/host-verification");
        Assert.NotNull(verification);
        var submitted = await client.PostAsJsonAsync("/api/host-verification", new { documentType = "Passport", notes = "Renewal fixture" });
        Assert.Equal(HttpStatusCode.OK, submitted.StatusCode);
        var submittedBody = await submitted.Content.ReadFromJsonAsync<HostVerificationBody>();
        Assert.Equal("Pending", submittedBody!.Status);
        Assert.Equal("Passport", submittedBody.DocumentType);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.AdminToken);
        var queue = await client.GetFromJsonAsync<List<HostVerificationQueueBody>>("/api/host-verification/queue");
        var queuedHost = Assert.Single(queue!, item => item.UserId == seededHostId);
        Assert.Equal("Pending", queuedHost.Status);
        var review = await client.PostAsJsonAsync($"/api/host-verification/{seededHostId}/decision", new { status = "Approved", reason = "Identity document review fixture approved." });
        Assert.Equal(HttpStatusCode.OK, review.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(seededHostId, UserRole.Host));
        var reviewed = await client.GetFromJsonAsync<HostVerificationBody>("/api/host-verification");
        Assert.Equal("Approved", reviewed!.Status);

        var hostId = Guid.NewGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(hostId, UserRole.Host));
        var propertyResponse = await client.PostAsJsonAsync("/api/properties", new
        {
            hostName = "Host Reject Fixture Host",
            hostEmail = $"fixture-reject-host-{hostId:N}@test.local",
            title = "Host Reject Fixture",
            location = "Ocho Rios",
            country = "Jamaica",
            nightlyRate = 125,
            currency = "USD",
            badgeLevel = "Verified",
            cancellationPolicy = "Flexible",
            guestVerificationEnabled = false,
            maxGuests = 2
        });
        var property = await propertyResponse.Content.ReadFromJsonAsync<PropertyBody>();
        Assert.NotNull(property);

        var guestId = await RegisterGuestAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(guestId, UserRole.Guest));
        var bookingResponse = await client.PostAsJsonAsync("/api/bookings", new { propertyId = property!.Id, checkIn = "2099-02-01", checkOut = "2099-02-04", adults = 2, termsAccepted = true });
        Assert.Equal(HttpStatusCode.OK, bookingResponse.StatusCode);
        var booking = await bookingResponse.Content.ReadFromJsonAsync<BookingBody>();
        Assert.NotNull(booking);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(hostId, UserRole.Host));
        var rejectedResponse = await client.PostAsJsonAsync($"/api/bookings/{booking!.Id}/reject", new { reason = "The home is unavailable for these dates." });
        Assert.Equal(HttpStatusCode.OK, rejectedResponse.StatusCode);
        var rejected = await rejectedResponse.Content.ReadFromJsonAsync<BookingBody>();
        Assert.Equal("REJECTED", rejected!.Status);
        Assert.Equal("Host", rejected.RejectionSource);
        Assert.Equal("The home is unavailable for these dates.", rejected.RejectionReason);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(guestId, UserRole.Guest));
        var guestView = await client.GetFromJsonAsync<BookingBody>($"/api/bookings/{booking.Id}");
        Assert.Equal("Host", guestView!.RejectionSource);
        Assert.Equal("The home is unavailable for these dates.", guestView.RejectionReason);
    }

    private static async Task<Guid> RegisterGuestAsync(HttpClient client)
    {
        var email = $"fixture-{Guid.NewGuid():N}@test.local";
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "FixturePassword123!",
            confirmPassword = "FixturePassword123!",
            displayName = "Marketplace Fixture Guest",
            acceptedTerms = true,
            acceptedPrivacy = true,
            role = "Guest"
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<RegisterBody>())!.UserId;
    }

    private static async Task<Guid> RegisterHostAsync(HttpClient client)
    {
        var email = $"host-fixture-{Guid.NewGuid():N}@test.local";
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "FixturePassword123!",
            confirmPassword = "FixturePassword123!",
            displayName = "Marketplace Fixture Host",
            acceptedTerms = true,
            acceptedPrivacy = true,
            role = "Host"
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<RegisterBody>())!.UserId;
    }

    private sealed record RegisterBody(Guid UserId);
    private sealed record PropertyBody(Guid Id, string ModerationStatus, string Parish, string Description, int Bedrooms, int Bathrooms, int MaxGuests, decimal CleaningFee, decimal ServiceFee, decimal? Latitude, decimal? Longitude, string[]? Amenities, string[]? SleepingArrangements, string[]? HouseRules, string? ModerationReason = null);
    private sealed record BookingBody(Guid Id, string Status, string? RejectionReason, string? RejectionSource);
    private sealed record HostVerificationBody(string Status, string? DocumentType);
    private sealed record HostVerificationQueueBody(Guid UserId, string Email, string DisplayName, string Status, string? DocumentType);
}
