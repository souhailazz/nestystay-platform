using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
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
    public async Task PasswordResetIsGenericOneTimeAndInvalidatesIssuedSessions()
    {
        using var client = _factory.CreateClient();
        var email = $"reset-{Guid.NewGuid():N}@test.local";

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Password123!",
            displayName = "Reset Guest",
            confirmPassword = "Password123!",
            acceptedTerms = true,
            acceptedPrivacy = true,
            role = "Guest"
        });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Password123!"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var challenge = await login.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(challenge);
        var developmentCode = await client.GetFromJsonAsync<DevelopmentAuthCodeResponse>(
            $"/api/auth/development/challenges/{challenge.ChallengeId}");
        Assert.NotNull(developmentCode);
        var twoFactor = await client.PostAsJsonAsync("/api/auth/2fa/verify", new
        {
            challengeId = challenge.ChallengeId,
            code = developmentCode.Code
        });
        Assert.Equal(HttpStatusCode.OK, twoFactor.StatusCode);
        var oldSession = await twoFactor.Content.ReadFromJsonAsync<TwoFactorResponse>();
        Assert.NotNull(oldSession);

        var unknownReset = await client.PostAsJsonAsync("/api/auth/password-reset/request", new
        {
            email = $"unknown-{Guid.NewGuid():N}@test.local"
        });
        Assert.Equal(HttpStatusCode.OK, unknownReset.StatusCode);
        var unknownResetBody = await unknownReset.Content.ReadFromJsonAsync<PasswordResetResponse>();
        Assert.NotNull(unknownResetBody);

        var resetRequest = await client.PostAsJsonAsync("/api/auth/password-reset/request", new
        {
            email
        });
        Assert.Equal(HttpStatusCode.OK, resetRequest.StatusCode);
        var resetBodyText = await resetRequest.Content.ReadAsStringAsync();
        Assert.DoesNotContain("token", resetBodyText, StringComparison.OrdinalIgnoreCase);
        var reset = await resetRequest.Content.ReadFromJsonAsync<PasswordResetResponse>();
        Assert.NotNull(reset);
        Assert.Equal(unknownResetBody.Message, reset.Message);

        var resetSecret = await client.GetFromJsonAsync<DevelopmentPasswordResetResponse>(
            $"/api/auth/development/password-resets/{reset.RequestId}");
        Assert.NotNull(resetSecret);

        var weakReset = await client.PostAsJsonAsync("/api/auth/password-reset/complete", new
        {
            requestId = reset.RequestId,
            token = resetSecret.Token,
            newPassword = "short",
            confirmPassword = "short"
        });
        Assert.Equal(HttpStatusCode.BadRequest, weakReset.StatusCode);

        var completed = await client.PostAsJsonAsync("/api/auth/password-reset/complete", new
        {
            requestId = reset.RequestId,
            token = resetSecret.Token,
            newPassword = "ResetPass123!",
            confirmPassword = "ResetPass123!"
        });
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);

        var reused = await client.PostAsJsonAsync("/api/auth/password-reset/complete", new
        {
            requestId = reset.RequestId,
            token = resetSecret.Token,
            newPassword = "AnotherPass123!",
            confirmPassword = "AnotherPass123!"
        });
        Assert.Equal(HttpStatusCode.BadRequest, reused.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oldSession.AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/bookings")).StatusCode);

        client.DefaultRequestHeaders.Authorization = null;
        var oldPasswordLogin = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Password123!"
        });
        Assert.Equal(HttpStatusCode.BadRequest, oldPasswordLogin.StatusCode);

        var newPasswordLogin = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "ResetPass123!"
        });
        Assert.Equal(HttpStatusCode.OK, newPasswordLogin.StatusCode);
        var newChallenge = await newPasswordLogin.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(newChallenge);
    }

    [Fact]
    public async Task RecoveryCodeCanCompleteTwoFactorOnce()
    {
        using var client = _factory.CreateClient();
        var email = $"recovery-{Guid.NewGuid():N}@test.local";

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Password123!",
            displayName = "Recovery Guest",
            confirmPassword = "Password123!",
            acceptedTerms = true,
            acceptedPrivacy = true,
            role = "Guest"
        });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        var registered = await register.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(registered);

        var firstLogin = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Password123!"
        });
        Assert.Equal(HttpStatusCode.OK, firstLogin.StatusCode);
        var firstChallenge = await firstLogin.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(firstChallenge);
        var firstDevelopmentCode = await client.GetFromJsonAsync<DevelopmentAuthCodeResponse>(
            $"/api/auth/development/challenges/{firstChallenge.ChallengeId}");
        Assert.NotNull(firstDevelopmentCode);
        var firstTwoFactor = await client.PostAsJsonAsync("/api/auth/2fa/verify", new
        {
            challengeId = firstChallenge.ChallengeId,
            code = firstDevelopmentCode.Code
        });
        Assert.Equal(HttpStatusCode.OK, firstTwoFactor.StatusCode);
        var session = await firstTwoFactor.Content.ReadFromJsonAsync<TwoFactorResponse>();
        Assert.NotNull(session);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        var recoveryResponse = await client.PostAsync($"/api/spec/auth/{registered.UserId}/recovery-codes", null);
        Assert.Equal(HttpStatusCode.OK, recoveryResponse.StatusCode);
        var codes = await recoveryResponse.Content.ReadFromJsonAsync<List<RecoveryCodeResponse>>();
        Assert.NotNull(codes);
        var recoveryCode = Assert.Single(codes.Take(1)).Code;

        client.DefaultRequestHeaders.Authorization = null;
        var recoveryLogin = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Password123!"
        });
        Assert.Equal(HttpStatusCode.OK, recoveryLogin.StatusCode);
        var recoveryChallenge = await recoveryLogin.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(recoveryChallenge);
        var recoverySessionResponse = await client.PostAsJsonAsync("/api/auth/2fa/verify", new
        {
            challengeId = recoveryChallenge.ChallengeId,
            code = recoveryCode
        });
        Assert.Equal(HttpStatusCode.OK, recoverySessionResponse.StatusCode);

        var replayLogin = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Password123!"
        });
        Assert.Equal(HttpStatusCode.OK, replayLogin.StatusCode);
        var replayChallenge = await replayLogin.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(replayChallenge);
        var replay = await client.PostAsJsonAsync("/api/auth/2fa/verify", new
        {
            challengeId = replayChallenge.ChallengeId,
            code = recoveryCode
        });
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
    }

    [Fact]
    public async Task TwoFactorEnrollmentRequiresValidTotpAndReturnsOneTimeRecoveryCodes()
    {
        using var client = _factory.CreateClient();
        var email = $"enroll-{Guid.NewGuid():N}@test.local";

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Password123!",
            displayName = "Enrollment Guest",
            confirmPassword = "Password123!",
            acceptedTerms = true,
            acceptedPrivacy = true,
            role = "Guest"
        });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Password123!"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var challenge = await login.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(challenge);
        var developmentCode = await client.GetFromJsonAsync<DevelopmentAuthCodeResponse>(
            $"/api/auth/development/challenges/{challenge.ChallengeId}");
        Assert.NotNull(developmentCode);
        var twoFactor = await client.PostAsJsonAsync("/api/auth/2fa/verify", new
        {
            challengeId = challenge.ChallengeId,
            code = developmentCode.Code
        });
        Assert.Equal(HttpStatusCode.OK, twoFactor.StatusCode);
        var session = await twoFactor.Content.ReadFromJsonAsync<TwoFactorResponse>();
        Assert.NotNull(session);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        var enrollmentResponse = await client.PostAsync("/api/auth/2fa/enrollments", null);
        Assert.Equal(HttpStatusCode.OK, enrollmentResponse.StatusCode);
        var enrollment = await enrollmentResponse.Content.ReadFromJsonAsync<TwoFactorEnrollmentResponse>();
        Assert.NotNull(enrollment);
        Assert.StartsWith("otpauth://totp/", enrollment.OtpAuthUri);
        Assert.Contains("issuer=NestyStay", enrollment.OtpAuthUri);
        Assert.NotEmpty(enrollment.ManualKey);

        var invalidConfirm = await client.PostAsJsonAsync("/api/auth/2fa/enrollments/confirm", new
        {
            enrollmentId = enrollment.EnrollmentId,
            code = "000000"
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidConfirm.StatusCode);

        var setupCode = GenerateTotpFromManualKey(enrollment.ManualKey);
        var confirm = await client.PostAsJsonAsync("/api/auth/2fa/enrollments/confirm", new
        {
            enrollmentId = enrollment.EnrollmentId,
            code = setupCode
        });
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        var confirmed = await confirm.Content.ReadFromJsonAsync<TwoFactorEnrollmentConfirmResponse>();
        Assert.NotNull(confirmed);
        Assert.True(confirmed.Enabled);
        Assert.Equal(8, confirmed.RecoveryCodes.Count);

        client.DefaultRequestHeaders.Authorization = null;
        var replayLogin = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Password123!"
        });
        Assert.Equal(HttpStatusCode.OK, replayLogin.StatusCode);
        var replayChallenge = await replayLogin.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(replayChallenge);
        var replay = await client.PostAsJsonAsync("/api/auth/2fa/verify", new
        {
            challengeId = replayChallenge.ChallengeId,
            code = setupCode
        });
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
    }

    [Fact]
    public async Task TwoFactorCanBeDisabledWithRecoveryCodeAndReturnsDirectLoginSession()
    {
        using var client = _factory.CreateClient();
        var email = $"disable-{Guid.NewGuid():N}@test.local";

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Password123!",
            displayName = "Disable Guest",
            confirmPassword = "Password123!",
            acceptedTerms = true,
            acceptedPrivacy = true,
            role = "Guest"
        });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Password123!"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var challenge = await login.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(challenge);
        Assert.True(challenge.RequiresTwoFactor);
        Assert.NotNull(challenge.ChallengeId);

        var developmentCode = await client.GetFromJsonAsync<DevelopmentAuthCodeResponse>(
            $"/api/auth/development/challenges/{challenge.ChallengeId}");
        Assert.NotNull(developmentCode);
        var twoFactor = await client.PostAsJsonAsync("/api/auth/2fa/verify", new
        {
            challengeId = challenge.ChallengeId,
            code = developmentCode.Code
        });
        Assert.Equal(HttpStatusCode.OK, twoFactor.StatusCode);
        var session = await twoFactor.Content.ReadFromJsonAsync<TwoFactorResponse>();
        Assert.NotNull(session);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        var recoveryResponse = await client.PostAsync($"/api/spec/auth/{session.UserId}/recovery-codes", null);
        Assert.Equal(HttpStatusCode.OK, recoveryResponse.StatusCode);
        var codes = await recoveryResponse.Content.ReadFromJsonAsync<List<RecoveryCodeResponse>>();
        Assert.NotNull(codes);
        Assert.NotEmpty(codes);

        using var invalidDisableRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/auth/2fa")
        {
            Content = JsonContent.Create(new { code = "000000" })
        };
        var invalidDisable = await client.SendAsync(invalidDisableRequest);
        Assert.Equal(HttpStatusCode.BadRequest, invalidDisable.StatusCode);

        using var disableRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/auth/2fa")
        {
            Content = JsonContent.Create(new { code = codes[0].Code })
        };
        var disabled = await client.SendAsync(disableRequest);
        Assert.Equal(HttpStatusCode.OK, disabled.StatusCode);
        var disabledBody = await disabled.Content.ReadFromJsonAsync<DisableTwoFactorResponse>();
        Assert.NotNull(disabledBody);
        Assert.True(disabledBody.Disabled);

        client.DefaultRequestHeaders.Authorization = null;
        var directLoginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Password123!"
        });
        Assert.Equal(HttpStatusCode.OK, directLoginResponse.StatusCode);
        var directLogin = await directLoginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(directLogin);
        Assert.False(directLogin.RequiresTwoFactor);
        Assert.Null(directLogin.ChallengeId);
        Assert.False(string.IsNullOrWhiteSpace(directLogin.AccessToken));
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
            email = "browser-spoof@test.local",
            displayName = "Browser Spoof",
            googleSubject = "browser-spoof-subject",
            pictureUrl = "https://example.invalid/spoof.png",
            credential = "valid-google-credential",
            role = "Guest"
        });
        Assert.Equal(HttpStatusCode.OK, google.StatusCode);
        var googleSession = await google.Content.ReadFromJsonAsync<GoogleSignInResponse>();
        Assert.NotNull(googleSession);
        Assert.Equal("Validated Google Guest", googleSession.DisplayName);
        Assert.Equal("Google", googleSession.Provider);
        Assert.NotEmpty(googleSession.AccessToken);

        var invalidGoogle = await client.PostAsJsonAsync("/api/auth/google", new
        {
            credential = "local-google-sign-in",
            role = "Guest"
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidGoogle.StatusCode);

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

        var otherHostUserId = Guid.NewGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            NestyStayApiFactory.UserToken(otherHostUserId, UserRole.Host));
        var crossHostUpdateResponse = await client.PutAsJsonAsync($"/api/properties/{createdProperty.Id}", new
        {
            hostName = "Cross Host",
            hostEmail = "cross-host@test.local",
            title = "Cross Host Edit",
            location = "Kingston",
            country = "Jamaica",
            nightlyRate = 210,
            currency = "USD",
            badgeLevel = "Verified",
            guestVerificationEnabled = false,
            insuraGuestEnabled = true,
            cancellationPolicy = "Flexible",
            highlights = new[] { "Should not save" }
        });
        Assert.Equal(HttpStatusCode.Unauthorized, crossHostUpdateResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            NestyStayApiFactory.UserToken(hostUserId, UserRole.Host));
        var updatePropertyResponse = await client.PutAsJsonAsync($"/api/properties/{createdProperty.Id}", new
        {
            hostName = "API Verified Host",
            hostEmail = "api-host@test.local",
            title = "API Updated Villa",
            location = "Runaway Bay, St. Ann",
            country = "Jamaica",
            nightlyRate = 205,
            currency = "USD",
            badgeLevel = "Verified",
            guestVerificationEnabled = true,
            insuraGuestEnabled = true,
            cancellationPolicy = "Moderate",
            highlights = new[] { "Updated", "Owner controlled" }
        });
        Assert.Equal(HttpStatusCode.OK, updatePropertyResponse.StatusCode);
        var updatedProperty = await updatePropertyResponse.Content.ReadFromJsonAsync<PropertyResponse>();
        Assert.NotNull(updatedProperty);
        Assert.Equal("API Updated Villa", updatedProperty.Title);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            NestyStayApiFactory.UserToken(otherHostUserId, UserRole.Host));
        var crossHostArchiveResponse = await client.PostAsync($"/api/properties/{createdProperty.Id}/archive", null);
        Assert.Equal(HttpStatusCode.Unauthorized, crossHostArchiveResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            NestyStayApiFactory.UserToken(hostUserId, UserRole.Host));
        var archivePropertyResponse = await client.PostAsync($"/api/properties/{createdProperty.Id}/archive", null);
        Assert.Equal(HttpStatusCode.OK, archivePropertyResponse.StatusCode);
        var archivedProperty = await archivePropertyResponse.Content.ReadFromJsonAsync<PropertyResponse>();
        Assert.NotNull(archivedProperty);
        Assert.True(archivedProperty.IsArchived);
        var archivedPublicLookup = await client.GetAsync($"/api/properties/{createdProperty.Id}");
        Assert.Equal(HttpStatusCode.NotFound, archivedPublicLookup.StatusCode);

        var restorePropertyResponse = await client.PostAsync($"/api/properties/{createdProperty.Id}/restore", null);
        Assert.Equal(HttpStatusCode.OK, restorePropertyResponse.StatusCode);
        var restoredProperty = await restorePropertyResponse.Content.ReadFromJsonAsync<PropertyResponse>();
        Assert.NotNull(restoredProperty);
        Assert.False(restoredProperty.IsArchived);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            NestyStayApiFactory.UserToken(otherHostUserId, UserRole.Host));
        var crossHostDeleteResponse = await client.DeleteAsync($"/api/properties/{createdProperty.Id}");
        Assert.Equal(HttpStatusCode.Unauthorized, crossHostDeleteResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            NestyStayApiFactory.UserToken(hostUserId, UserRole.Host));
        var deletePropertyResponse = await client.DeleteAsync($"/api/properties/{createdProperty.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deletePropertyResponse.StatusCode);
        var deletedPublicLookup = await client.GetAsync($"/api/properties/{createdProperty.Id}");
        Assert.Equal(HttpStatusCode.NotFound, deletedPublicLookup.StatusCode);

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

        var invoiceBeforeCaptureResponse = await client.GetAsync($"/api/bookings/{booking.Id}/invoice");
        Assert.Equal(HttpStatusCode.OK, invoiceBeforeCaptureResponse.StatusCode);
        Assert.Equal("application/pdf", invoiceBeforeCaptureResponse.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(await invoiceBeforeCaptureResponse.Content.ReadAsByteArrayAsync()));

        var receiptBeforeCaptureResponse = await client.GetAsync($"/api/bookings/{booking.Id}/receipt");
        Assert.Equal(HttpStatusCode.BadRequest, receiptBeforeCaptureResponse.StatusCode);

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

        var hostReceiptResponse = await client.GetAsync($"/api/bookings/{booking.Id}/receipt");
        Assert.Equal(HttpStatusCode.OK, hostReceiptResponse.StatusCode);
        Assert.Equal("application/pdf", hostReceiptResponse.Content.Headers.ContentType?.MediaType);
        Assert.Contains($"nestystay-receipt-{booking.Id:N}.pdf", hostReceiptResponse.Content.Headers.ContentDisposition?.FileName?.Trim('"'));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        var guestReceiptResponse = await client.GetAsync($"/api/bookings/{booking.Id}/receipt");
        Assert.Equal(HttpStatusCode.OK, guestReceiptResponse.StatusCode);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(await guestReceiptResponse.Content.ReadAsByteArrayAsync()));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            NestyStayApiFactory.UserToken(Guid.NewGuid()));
        var crossGuestInvoiceResponse = await client.GetAsync($"/api/bookings/{booking.Id}/invoice");
        Assert.Equal(HttpStatusCode.NotFound, crossGuestInvoiceResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            NestyStayApiFactory.UserToken(Guid.NewGuid(), UserRole.Host));
        var crossHostReceiptResponse = await client.GetAsync($"/api/bookings/{booking.Id}/receipt");
        Assert.Equal(HttpStatusCode.NotFound, crossHostReceiptResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        var guestRefundResponse = await client.PostAsJsonAsync($"/api/bookings/{booking.Id}/refund-payment", new
        {
            reason = "Guest attempted to self-refund."
        });
        Assert.Equal(HttpStatusCode.Forbidden, guestRefundResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.AdminToken);
        var refundResponse = await client.PostAsJsonAsync($"/api/bookings/{booking.Id}/refund-payment", new
        {
            amount = captured.TotalAmount,
            reason = "Admin approved cancellation refund.",
            idempotencyKey = $"test-refund-{booking.Id:N}"
        });
        Assert.Equal(HttpStatusCode.OK, refundResponse.StatusCode);
        var refunded = await refundResponse.Content.ReadFromJsonAsync<BookingResponse>();
        Assert.NotNull(refunded);
        Assert.Equal("REFUNDED", refunded.PaymentStatus);
        Assert.Equal(captured.TotalAmount, refunded.RefundedAmount);
        Assert.NotNull(refunded.PaymentRefundReference);
        Assert.Contains(refunded.Notifications, item => item.RecipientType == "guest");
        Assert.Contains(refunded.Notifications, item => item.RecipientType == "host");

        var duplicateRefundResponse = await client.PostAsJsonAsync($"/api/bookings/{booking.Id}/refund-payment", new
        {
            amount = captured.TotalAmount,
            reason = "Admin approved cancellation refund.",
            idempotencyKey = $"test-refund-{booking.Id:N}"
        });
        Assert.Equal(HttpStatusCode.OK, duplicateRefundResponse.StatusCode);
    }

    private sealed record RegisterResponse(Guid UserId, bool RequiresTwoFactor);

    private sealed record LoginResponse(string? ChallengeId, bool RequiresTwoFactor = true, string? AccessToken = null);

    private sealed record DevelopmentAuthCodeResponse(string Code);

    private sealed record TwoFactorResponse(Guid UserId, string AccessToken);

    private sealed record GoogleSignInResponse(Guid UserId, string DisplayName, string AccessToken, string Provider);

    private sealed record PasswordResetResponse(string RequestId, string Message, DateTimeOffset ExpiresAt);

    private sealed record DevelopmentPasswordResetResponse(string RequestId, string Token, DateTimeOffset ExpiresAt);

    private sealed record RecoveryCodeResponse(string Code, bool Used);

    private sealed record TwoFactorEnrollmentResponse(
        string EnrollmentId,
        string ManualKey,
        string OtpAuthUri,
        DateTimeOffset ExpiresAt);

    private sealed record TwoFactorEnrollmentConfirmResponse(bool Enabled, IReadOnlyList<string> RecoveryCodes);

    private sealed record DisableTwoFactorResponse(bool Disabled);

    private sealed record PropertyResponse(Guid Id, Guid HostUserId, bool GuestVerificationEnabled, string Title = "", bool IsArchived = false);

    private sealed record QuoteResponse(bool DatesAvailable, int Nights);

    private sealed record BookingResponse(
        Guid Id,
        Guid GuestUserId,
        string Status,
        string PaymentStatus,
        bool DatesHeld,
        decimal TotalAmount,
        string? EkycProvider,
        string? EkycTransactionId,
        string? PaymentRefundReference,
        decimal RefundedAmount,
        IReadOnlyList<NotificationResponse> Notifications);

    private sealed record NotificationResponse(string RecipientType);

    private static string GenerateTotpFromManualKey(string manualKey)
    {
        var secret = DecodeBase32(manualKey);
        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(counterBytes);
        }

        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(counterBytes);
        var offset = hash[^1] & 0x0f;
        var binaryCode =
            ((hash[offset] & 0x7f) << 24) |
            ((hash[offset + 1] & 0xff) << 16) |
            ((hash[offset + 2] & 0xff) << 8) |
            (hash[offset + 3] & 0xff);
        return (binaryCode % 1_000_000).ToString("D6");
    }

    private static byte[] DecodeBase32(string value)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bits = 0;
        var bitCount = 0;
        var output = new List<byte>();
        foreach (var character in value.Trim().TrimEnd('=').ToUpperInvariant())
        {
            var index = alphabet.IndexOf(character, StringComparison.Ordinal);
            if (index < 0)
            {
                throw new InvalidOperationException("Invalid base32 character.");
            }

            bits = (bits << 5) | index;
            bitCount += 5;
            if (bitCount >= 8)
            {
                output.Add((byte)((bits >> (bitCount - 8)) & 0xff));
                bitCount -= 8;
            }
        }

        return output.ToArray();
    }
}
