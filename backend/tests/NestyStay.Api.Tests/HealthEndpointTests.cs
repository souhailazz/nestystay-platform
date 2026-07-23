using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using NestyStay.Domain;

namespace NestyStay.Api.Tests;

public sealed class HealthEndpointTests : IClassFixture<NestyStayApiFactory>
{
    private readonly NestyStayApiFactory _factory;

    public HealthEndpointTests(NestyStayApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthEndpointReturnsOk()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task BackendSchemaEndpointReturnsRules()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/backend-schema/rules");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Payment capture", body);
        Assert.Contains("Managers are hard-blocked", body);
    }

    [Fact]
    public async Task BackendSchemaSeedPricebookEndpointReturnsSeedPrices()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/backend-schema/seed/pricebook");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("host-listing", body);
        Assert.Contains("verified-host-standard-annual", body);
    }

    [Fact]
    public async Task BackendJobsEndpointReturnsPlannedJobs()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/backend-jobs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("officer-id-reset", body);
        Assert.Contains("wellness-escrow-auto-release", body);
    }

    [Fact]
    public async Task PhaseOneEndpointsSupportRegistrationListingAndPendingBooking()
    {
        using var client = _factory.CreateClient();
        var email = $"phase1-{Guid.NewGuid():N}@test.local";
        var checkIn = new DateOnly(2026, 6, 10).AddDays(Random.Shared.Next(0, 1000));
        var checkOut = checkIn.AddDays(3);

        var unauthenticatedBookings = await client.GetAsync("/api/bookings");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticatedBookings.StatusCode);

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Password123!",
            displayName = "Phase One Guest",
            phone = "254-248-2435",
            confirmPassword = "Password123!",
            acceptedTerms = true,
            acceptedPrivacy = true,
            role = "Guest"
        });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        Assert.DoesNotContain("twoFactorCode", await register.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        var registered = await register.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(registered);
        Assert.True(registered.RequiresTwoFactor);

        var duplicateRegister = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Password123!",
            displayName = "Phase One Guest",
            phone = "254-248-2435",
            confirmPassword = "Password123!",
            acceptedTerms = true,
            acceptedPrivacy = true,
            role = "Guest"
        });
        Assert.Equal(HttpStatusCode.BadRequest, duplicateRegister.StatusCode);

        var weakRegister = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"weak-{Guid.NewGuid():N}@test.local",
            password = "short",
            displayName = "Weak Guest",
            confirmPassword = "short",
            acceptedTerms = true,
            acceptedPrivacy = true,
            role = "Guest"
        });
        Assert.Equal(HttpStatusCode.BadRequest, weakRegister.StatusCode);

        var wrongLogin = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "wrong-password"
        });
        Assert.Equal(HttpStatusCode.BadRequest, wrongLogin.StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Password123!"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.DoesNotContain("twoFactorCode", await login.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        var challenge = await login.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(challenge);
        var developmentCode = await client.GetFromJsonAsync<DevelopmentAuthCodeResponse>(
            $"/api/auth/development/challenges/{challenge.ChallengeId}");
        Assert.NotNull(developmentCode);

        var badTwoFactor = await client.PostAsJsonAsync("/api/auth/2fa/verify", new
        {
            challengeId = challenge.ChallengeId,
            code = "000000"
        });
        Assert.Equal(HttpStatusCode.BadRequest, badTwoFactor.StatusCode);

        var twoFactor = await client.PostAsJsonAsync("/api/auth/2fa/verify", new
        {
            challengeId = challenge.ChallengeId,
            code = developmentCode.Code
        });
        Assert.Equal(HttpStatusCode.OK, twoFactor.StatusCode);
        var session = await twoFactor.Content.ReadFromJsonAsync<TwoFactorResponse>();
        Assert.NotNull(session);
        Assert.Equal(registered.UserId, session.UserId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);

        var google = await client.PostAsJsonAsync("/api/auth/google", new
        {
            email = $"google-{Guid.NewGuid():N}@gmail.com",
            displayName = "Google Guest",
            googleSubject = $"google-{Guid.NewGuid():N}",
            pictureUrl = "https://lh3.googleusercontent.com/a/default-user",
            credential = "local-google-credential"
        });
        Assert.Equal(HttpStatusCode.OK, google.StatusCode);
        var googleSession = await google.Content.ReadFromJsonAsync<GoogleSignInResponse>();
        Assert.NotNull(googleSession);
        Assert.Equal("Google Guest", googleSession.DisplayName);
        Assert.Equal("Google", googleSession.Provider);
        Assert.NotEmpty(googleSession.AccessToken);

        var properties = await client.GetFromJsonAsync<List<PropertyResponse>>("/api/properties");
        Assert.NotNull(properties);
        var property = properties.First(item => item.GuestVerificationEnabled);

        client.DefaultRequestHeaders.Authorization = null;
        var unauthenticatedPropertyCreate = await client.PostAsJsonAsync("/api/properties", new
        {
            hostUserId = Guid.NewGuid(),
            hostName = "No Auth Host",
            hostEmail = "no-auth-host@test.local",
            title = "No Auth Villa",
            location = "Runaway Bay, St. Ann",
            country = "Jamaica",
            nightlyRate = 175,
            currency = "USD"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticatedPropertyCreate.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        var guestPropertyCreate = await client.PostAsJsonAsync("/api/properties", new
        {
            hostUserId = Guid.NewGuid(),
            hostName = "Guest Host",
            hostEmail = "guest-host@test.local",
            title = "Guest Attempt Villa",
            location = "Runaway Bay, St. Ann",
            country = "Jamaica",
            nightlyRate = 175,
            currency = "USD"
        });
        Assert.Equal(HttpStatusCode.Forbidden, guestPropertyCreate.StatusCode);

        var hostUserId = Guid.NewGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            NestyStayApiFactory.UserToken(hostUserId, UserRole.Host));
        var createdPropertyResponse = await client.PostAsJsonAsync("/api/properties", new
        {
            hostUserId = Guid.NewGuid(),
            hostName = "API Verified Host",
            hostEmail = "api-host@test.local",
            title = $"API Created Villa {Guid.NewGuid():N}",
            location = "Runaway Bay, St. Ann",
            country = "Jamaica",
            nightlyRate = 175,
            currency = "USD",
            badgeLevel = "Verified",
            guestVerificationEnabled = true,
            insuraGuestEnabled = true,
            cancellationPolicy = "Moderate",
            highlights = new[] { "API created", "Alibaba eKYC" }
        });
        Assert.Equal(HttpStatusCode.OK, createdPropertyResponse.StatusCode);
        var createdProperty = await createdPropertyResponse.Content.ReadFromJsonAsync<PropertyResponse>();
        Assert.NotNull(createdProperty);
        Assert.Equal(hostUserId, createdProperty.HostUserId);

        var freeUpsellResponse = await client.PostAsJsonAsync("/api/properties", new
        {
            hostUserId = Guid.NewGuid(),
            hostName = "Free Host",
            hostEmail = "free-host@test.local",
            title = "Free Host Upsell Attempt",
            location = "Kingston",
            country = "Jamaica",
            nightlyRate = 90,
            currency = "USD",
            badgeLevel = "Free",
            guestVerificationEnabled = true,
            cancellationPolicy = "Flexible"
        });
        Assert.Equal(HttpStatusCode.BadRequest, freeUpsellResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);

        var quoteResponse = await client.PostAsJsonAsync("/api/bookings/quote", new
        {
            propertyId = property.Id,
            checkIn = checkIn.ToString("yyyy-MM-dd"),
            checkOut = checkOut.ToString("yyyy-MM-dd")
        });
        Assert.Equal(HttpStatusCode.OK, quoteResponse.StatusCode);
        var quote = await quoteResponse.Content.ReadFromJsonAsync<QuoteResponse>();
        Assert.NotNull(quote);
        Assert.True(quote.DatesAvailable);
        Assert.Equal(3, quote.Nights);

        var bookingResponse = await client.PostAsJsonAsync("/api/bookings", new
        {
            propertyId = property.Id,
            guestUserId = Guid.NewGuid(),
            checkIn = checkIn.ToString("yyyy-MM-dd"),
            checkOut = checkOut.ToString("yyyy-MM-dd")
        });

        Assert.Equal(HttpStatusCode.OK, bookingResponse.StatusCode);
        var booking = await bookingResponse.Content.ReadFromJsonAsync<BookingResponse>();
        Assert.NotNull(booking);
        Assert.Equal("PENDING", booking.Status);
        Assert.Equal(registered.UserId, booking.GuestUserId);
        Assert.True(booking.DatesHeld);
        Assert.Equal("Alibaba Cloud eKYC", booking.EkycProvider);
        Assert.NotNull(booking.EkycTransactionId);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            NestyStayApiFactory.UserToken(Guid.NewGuid()));
        var crossUserBooking = await client.GetAsync($"/api/bookings/{booking.Id}");
        Assert.Equal(HttpStatusCode.NotFound, crossUserBooking.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-valid-token");
        var invalidTokenBooking = await client.GetAsync($"/api/bookings/{booking.Id}");
        Assert.Equal(HttpStatusCode.Unauthorized, invalidTokenBooking.StatusCode);

        client.DefaultRequestHeaders.Authorization = null;
        var missingTokenBooking = await client.GetAsync($"/api/bookings/{booking.Id}");
        Assert.Equal(HttpStatusCode.Unauthorized, missingTokenBooking.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);

        var overlappingQuote = await client.PostAsJsonAsync("/api/bookings/quote", new
        {
            propertyId = property.Id,
            checkIn = checkIn.AddDays(1).ToString("yyyy-MM-dd"),
            checkOut = checkOut.AddDays(1).ToString("yyyy-MM-dd")
        });
        Assert.Equal(HttpStatusCode.BadRequest, overlappingQuote.StatusCode);

        var pendingCapture = await client.PostAsync($"/api/bookings/{booking.Id}/capture-payment", null);
        Assert.Equal(HttpStatusCode.Forbidden, pendingCapture.StatusCode);

        var travelerVerificationWrite = await client.PostAsJsonAsync($"/api/bookings/{booking.Id}/verification-result", new
        {
            passed = true,
            transactionId = booking.EkycTransactionId,
            notes = "Traveler attempted to self-approve."
        });
        Assert.Equal(HttpStatusCode.Forbidden, travelerVerificationWrite.StatusCode);

        var approvedResponse = await client.PostAsJsonAsync("/api/webhooks/alibaba-ekyc", new
        {
            bookingId = booking.Id,
            transactionId = booking.EkycTransactionId,
            passed = true
        });
        Assert.Equal(HttpStatusCode.Accepted, approvedResponse.StatusCode);
        var approved = await approvedResponse.Content.ReadFromJsonAsync<BookingResponse>();
        Assert.NotNull(approved);
        Assert.Equal("APPROVED", approved.Status);
        Assert.Equal("AUTHORIZED", approved.PaymentStatus);
        Assert.Contains(approved.Notifications, item => item.RecipientType == "guest");
        Assert.Contains(approved.Notifications, item => item.RecipientType == "host");

        var duplicateWebhookResponse = await client.PostAsJsonAsync("/api/webhooks/alibaba-ekyc", new
        {
            bookingId = booking.Id,
            transactionId = booking.EkycTransactionId,
            passed = true
        });
        Assert.Equal(HttpStatusCode.Accepted, duplicateWebhookResponse.StatusCode);
        var duplicateWebhook = await duplicateWebhookResponse.Content.ReadFromJsonAsync<BookingResponse>();
        Assert.NotNull(duplicateWebhook);
        Assert.Equal(approved.Notifications.Count, duplicateWebhook.Notifications.Count);

        var conflictingWebhookResponse = await client.PostAsJsonAsync("/api/webhooks/alibaba-ekyc", new
        {
            bookingId = booking.Id,
            transactionId = booking.EkycTransactionId,
            passed = false
        });
        Assert.Equal(HttpStatusCode.BadRequest, conflictingWebhookResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            NestyStayApiFactory.UserToken(property.HostUserId, UserRole.Host));
        var captureResponse = await client.PostAsync($"/api/bookings/{booking.Id}/capture-payment", null);
        Assert.Equal(HttpStatusCode.OK, captureResponse.StatusCode);
        var captured = await captureResponse.Content.ReadFromJsonAsync<BookingResponse>();
        Assert.NotNull(captured);
        Assert.Equal("APPROVED", captured.Status);
        Assert.Equal("CAPTURED", captured.PaymentStatus);

        var duplicateCaptureResponse = await client.PostAsync($"/api/bookings/{booking.Id}/capture-payment", null);
        Assert.Equal(HttpStatusCode.OK, duplicateCaptureResponse.StatusCode);
    }

    private sealed record RegisterResponse(Guid UserId, bool RequiresTwoFactor);

    private sealed record LoginResponse(string ChallengeId);

    private sealed record DevelopmentAuthCodeResponse(string Code);

    private sealed record TwoFactorResponse(Guid UserId, string AccessToken);

    private sealed record GoogleSignInResponse(Guid UserId, string DisplayName, string AccessToken, string Provider);

    private sealed record PropertyResponse(Guid Id, Guid HostUserId, bool GuestVerificationEnabled);

    private sealed record QuoteResponse(bool DatesAvailable, int Nights);

    private sealed record BookingResponse(
        Guid Id,
        Guid GuestUserId,
        string Status,
        string PaymentStatus,
        bool DatesHeld,
        string? EkycProvider,
        string? EkycTransactionId,
        IReadOnlyList<NotificationResponse> Notifications);

    private sealed record NotificationResponse(string RecipientType);
}
