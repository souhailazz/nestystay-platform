using System.Reflection;
using NestyStay.Application.Abstractions;
using NestyStay.Application.PhaseOne;
using NestyStay.Domain;

namespace NestyStay.Application.Tests;

public sealed class PhaseOneWorkflowTests
{
    [Fact]
    public async Task RegistrationRejectsInvalidDuplicateAndWeakInputsAndStoresPasswordHash()
    {
        var harness = CreateHarness();
        var registered = await harness.Store.RegisterAsync(
            Registration("secure@test.local", "Secure Guest", "254-248-2435"),
            CancellationToken.None);

        Assert.True(registered.RequiresTwoFactor);
        Assert.Equal("secure@test.local", registered.Email);
        Assert.Equal("Secure Guest", registered.DisplayName);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.RegisterAsync(Registration("secure@test.local", "Secure Guest"), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.RegisterAsync(Registration("not-an-email", "Bad Email"), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.RegisterAsync(Registration("", "No Email"), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.RegisterAsync(Registration("weak@test.local", "Weak Password", password: "short"), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.RegisterAsync(new RegisterUserRequest("mismatch@test.local", "Password123!", "Mismatch", null, "Password124!", true, true), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.RegisterAsync(new RegisterUserRequest("terms@test.local", "Password123!", "Terms", null, "Password123!", false, true), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.RegisterAsync(new RegisterUserRequest("admin@test.local", "Password123!", "Admin", null, "Password123!", true, true, UserRole.Admin), CancellationToken.None));

        var passwordHash = ExtractStoredPasswordHash(harness.Store, registered.UserId);
        Assert.NotEqual("Password123!", passwordHash);
        Assert.StartsWith("PBKDF2-SHA256$", passwordHash);
    }

    [Fact]
    public async Task LoginAndTwoFactorRejectWrongUnknownExpiredInvalidAndReusedChallenges()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 6, 21, 12, 0, 0, TimeSpan.Zero));
        var harness = CreateHarness(clock);
        var registered = await harness.Store.RegisterAsync(
            Registration("secure@test.local", "Secure Guest"),
            CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.LoginAsync(new LoginRequest("secure@test.local", "wrong-password"), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.LoginAsync(new LoginRequest("unknown@test.local", "Password123!"), CancellationToken.None));

        var challenge = await harness.Store.LoginAsync(new LoginRequest("secure@test.local", "Password123!"), CancellationToken.None);
        Assert.NotNull(challenge.ChallengeId);
        var challengeCode = await GetDevelopmentCodeAsync(harness.Store, challenge.ChallengeId);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.VerifyTwoFactorAsync(new VerifyTwoFactorRequest("missing", challengeCode), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.VerifyTwoFactorAsync(new VerifyTwoFactorRequest(challenge.ChallengeId, "000000"), CancellationToken.None));

        var session = await harness.Store.VerifyTwoFactorAsync(
            new VerifyTwoFactorRequest(challenge.ChallengeId, challengeCode),
            CancellationToken.None);

        Assert.Equal(registered.UserId, session.UserId);
        Assert.NotEmpty(session.AccessToken);
        Assert.False(session.AccessToken.StartsWith("local-phase1-token-", StringComparison.Ordinal));
        Assert.False(session.AccessToken.StartsWith("local-google-token-", StringComparison.Ordinal));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.VerifyTwoFactorAsync(new VerifyTwoFactorRequest(challenge.ChallengeId, challengeCode), CancellationToken.None));

        var expiringChallenge = await harness.Store.LoginAsync(new LoginRequest("secure@test.local", "Password123!"), CancellationToken.None);
        Assert.NotNull(expiringChallenge.ChallengeId);
        var expiringCode = await GetDevelopmentCodeAsync(harness.Store, expiringChallenge.ChallengeId);
        clock.Advance(TimeSpan.FromMinutes(11));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.VerifyTwoFactorAsync(new VerifyTwoFactorRequest(expiringChallenge.ChallengeId, expiringCode), CancellationToken.None));
    }

    [Fact]
    public async Task LoginLocksAccountAfterRepeatedInvalidPasswordAttemptsAndRecoversAfterWindow()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 6, 21, 12, 0, 0, TimeSpan.Zero));
        var harness = CreateHarness(clock);
        await harness.Store.RegisterAsync(Registration("lockout@test.local", "Lockout Guest"), CancellationToken.None);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                harness.Store.LoginAsync(new LoginRequest("lockout@test.local", "wrong-password"), CancellationToken.None));
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.LoginAsync(new LoginRequest("lockout@test.local", "Password123!"), CancellationToken.None));

        clock.Advance(TimeSpan.FromMinutes(16));
        var challenge = await harness.Store.LoginAsync(new LoginRequest("lockout@test.local", "Password123!"), CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(challenge.ChallengeId));
    }

    [Fact]
    public async Task TwoFactorChallengeIsInvalidatedAfterRepeatedBadCodes()
    {
        var harness = CreateHarness();
        await harness.Store.RegisterAsync(Registration("attempts@test.local", "Attempts Guest"), CancellationToken.None);
        var challenge = await harness.Store.LoginAsync(new LoginRequest("attempts@test.local", "Password123!"), CancellationToken.None);
        Assert.NotNull(challenge.ChallengeId);
        var code = await GetDevelopmentCodeAsync(harness.Store, challenge.ChallengeId);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                harness.Store.VerifyTwoFactorAsync(new VerifyTwoFactorRequest(challenge.ChallengeId, "000000"), CancellationToken.None));
        }

        Assert.Null(await harness.Store.GetDevelopmentTwoFactorCodeAsync(challenge.ChallengeId, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.VerifyTwoFactorAsync(new VerifyTwoFactorRequest(challenge.ChallengeId, code), CancellationToken.None));
    }

    [Fact]
    public async Task TwoFactorRejectsReplayedTotpCounterAcrossChallenges()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 6, 21, 12, 0, 0, TimeSpan.Zero));
        var harness = CreateHarness(clock);
        await harness.Store.RegisterAsync(Registration("replay@test.local", "Replay Guest"), CancellationToken.None);

        var firstChallenge = await harness.Store.LoginAsync(new LoginRequest("replay@test.local", "Password123!"), CancellationToken.None);
        Assert.NotNull(firstChallenge.ChallengeId);
        var firstCode = await GetDevelopmentCodeAsync(harness.Store, firstChallenge.ChallengeId);
        var firstSession = await harness.Store.VerifyTwoFactorAsync(
            new VerifyTwoFactorRequest(firstChallenge.ChallengeId, firstCode),
            CancellationToken.None);
        Assert.NotEmpty(firstSession.AccessToken);

        var replayChallenge = await harness.Store.LoginAsync(new LoginRequest("replay@test.local", "Password123!"), CancellationToken.None);
        Assert.NotNull(replayChallenge.ChallengeId);
        var replayCode = await GetDevelopmentCodeAsync(harness.Store, replayChallenge.ChallengeId);
        Assert.Equal(firstCode, replayCode);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.VerifyTwoFactorAsync(new VerifyTwoFactorRequest(replayChallenge.ChallengeId, replayCode), CancellationToken.None));

        clock.Advance(TimeSpan.FromSeconds(30));
        var nextChallenge = await harness.Store.LoginAsync(new LoginRequest("replay@test.local", "Password123!"), CancellationToken.None);
        Assert.NotNull(nextChallenge.ChallengeId);
        var nextCode = await GetDevelopmentCodeAsync(harness.Store, nextChallenge.ChallengeId);
        var nextSession = await harness.Store.VerifyTwoFactorAsync(
            new VerifyTwoFactorRequest(nextChallenge.ChallengeId, nextCode),
            CancellationToken.None);
        Assert.NotEmpty(nextSession.AccessToken);
    }

    [Fact]
    public async Task TwoFactorCanBeDisabledWithValidCurrentCode()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 6, 21, 12, 0, 0, TimeSpan.Zero));
        var harness = CreateHarness(clock);
        var registered = await harness.Store.RegisterAsync(Registration("disable@test.local", "Disable Guest"), CancellationToken.None);

        var firstChallenge = await harness.Store.LoginAsync(new LoginRequest("disable@test.local", "Password123!"), CancellationToken.None);
        Assert.True(firstChallenge.RequiresTwoFactor);
        Assert.NotNull(firstChallenge.ChallengeId);
        var firstCode = await GetDevelopmentCodeAsync(harness.Store, firstChallenge.ChallengeId);
        var firstSession = await harness.Store.VerifyTwoFactorAsync(
            new VerifyTwoFactorRequest(firstChallenge.ChallengeId, firstCode),
            CancellationToken.None);
        Assert.Equal(registered.UserId, firstSession.UserId);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.DisableTwoFactorAsync(registered.UserId, new DisableTwoFactorRequest("000000"), CancellationToken.None));

        clock.Advance(TimeSpan.FromSeconds(30));
        var disableChallenge = await harness.Store.LoginAsync(new LoginRequest("disable@test.local", "Password123!"), CancellationToken.None);
        Assert.NotNull(disableChallenge.ChallengeId);
        var disableCode = await GetDevelopmentCodeAsync(harness.Store, disableChallenge.ChallengeId);
        var disabled = await harness.Store.DisableTwoFactorAsync(
            registered.UserId,
            new DisableTwoFactorRequest(disableCode),
            CancellationToken.None);

        Assert.True(disabled.Disabled);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.VerifyTwoFactorAsync(new VerifyTwoFactorRequest(disableChallenge.ChallengeId, disableCode), CancellationToken.None));

        var directLogin = await harness.Store.LoginAsync(new LoginRequest("disable@test.local", "Password123!"), CancellationToken.None);
        Assert.False(directLogin.RequiresTwoFactor);
        Assert.Null(directLogin.ChallengeId);
        Assert.False(string.IsNullOrWhiteSpace(directLogin.AccessToken));
        Assert.Contains(UserRole.Guest, directLogin.Roles ?? []);
    }

    [Fact]
    public async Task PropertyCreationListingAndValidationRespectGuestVerificationUpsellRules()
    {
        var harness = CreateHarness();

        var created = await harness.Store.CreatePropertyAsync(new CreatePropertyRequest(
            Guid.NewGuid(),
            "Verified Host",
            "host@test.local",
            "Negril Beach Studio",
            "Negril, Westmoreland",
            "Jamaica",
            155m,
            "usd",
            BadgeLevel.Verified,
            GuestVerificationEnabled: true,
            InsuraGuestEnabled: true,
            CancellationPolicy: "Moderate",
            Highlights: ["Alibaba eKYC", "Guest verification upsell"]),
            CancellationToken.None);

        Assert.Equal("USD", created.Currency);
        Assert.True(created.GuestVerificationEnabled);
        Assert.Equal(created.Id, harness.Store.GetProperty(created.Id)?.Id);
        Assert.Contains(harness.Store.GetProperties(), item => item.Id == created.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.CreatePropertyAsync(new CreatePropertyRequest(Guid.NewGuid(), "", "host@test.local", "Missing", "Kingston", "Jamaica", 100m, "USD"), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.CreatePropertyAsync(new CreatePropertyRequest(Guid.NewGuid(), "Free Host", "host@test.local", "Free Upsell", "Kingston", "Jamaica", 100m, "USD", BadgeLevel.Free, GuestVerificationEnabled: true), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.CreatePropertyAsync(new CreatePropertyRequest(Guid.NewGuid(), "Bad Rate", "host@test.local", "Bad Rate", "Kingston", "Jamaica", 0m, "USD"), CancellationToken.None));
    }

    [Fact]
    public async Task BookingWithGuestVerificationMovesThroughPendingApprovedAndIdempotentPaymentCapture()
    {
        var harness = CreateHarness();
        var user = await harness.Store.RegisterAsync(Registration("phase1@test.local", "Phase Guest"), CancellationToken.None);
        var property = harness.Store.GetProperties().First(item => item.GuestVerificationEnabled);

        var quote = await harness.Store.QuoteBookingAsync(new BookingQuoteRequest(property.Id, new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 13)), CancellationToken.None);

        Assert.True(quote.DatesAvailable);
        Assert.Equal(3, quote.Nights);
        Assert.Equal(555m, quote.StaySubtotal);
        Assert.Equal(55.5m, quote.GuestPlatformFee);
        Assert.Equal(610.5m, quote.TotalAmount);
        Assert.Contains(quote.PriceBreakdown, line => line.Code == "guest-verification" && line.Amount == 0m);

        var booking = await harness.Store.CreateBookingAsync(new CreateBookingRequest(property.Id, user.UserId, new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 13)), CancellationToken.None);

        Assert.Equal("PENDING", booking.Status);
        Assert.True(booking.DatesHeld);
        Assert.Equal("Alibaba Cloud eKYC", booking.EkycProvider);
        Assert.NotNull(booking.EkycTransactionId);
        Assert.Equal(1, harness.EkycProvider.StartCount);

        await Assert.ThrowsAsync<InvalidOperationException>(() => harness.Store.CapturePaymentAsync(booking.Id, CancellationToken.None));

        var approved = await harness.Store.ResolveVerificationAsync(booking.Id, new ResolveVerificationRequest(true, booking.EkycTransactionId), CancellationToken.None);
        Assert.NotNull(approved);
        Assert.Equal("APPROVED", approved.Status);
        Assert.Equal("AUTHORIZED", approved.PaymentStatus);
        Assert.Equal(2, approved.Notifications.Count);

        var duplicateWebhook = await harness.Store.ResolveVerificationAsync(booking.Id, new ResolveVerificationRequest(true, booking.EkycTransactionId), CancellationToken.None);
        Assert.NotNull(duplicateWebhook);
        Assert.Equal(2, duplicateWebhook.Notifications.Count);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.ResolveVerificationAsync(booking.Id, new ResolveVerificationRequest(false, booking.EkycTransactionId), CancellationToken.None));

        var captured = await harness.Store.CapturePaymentAsync(booking.Id, CancellationToken.None);
        var duplicateCapture = await harness.Store.CapturePaymentAsync(booking.Id, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.NotNull(duplicateCapture);
        Assert.Equal("APPROVED", captured.Status);
        Assert.Equal("CAPTURED", captured.PaymentStatus);
        Assert.Equal(captured.PaymentCaptureReference, duplicateCapture.PaymentCaptureReference);
        Assert.Equal(1, harness.PaymentGateway.CaptureCount);
    }

    [Fact]
    public async Task RejectionFlowReleasesDatesAndPreventsPaymentOrConflictingWebhook()
    {
        var harness = CreateHarness();
        var user = await harness.Store.RegisterAsync(Registration("reject@test.local", "Reject Guest"), CancellationToken.None);
        var property = harness.Store.GetProperties().First(item => item.GuestVerificationEnabled);
        var booking = await harness.Store.CreateBookingAsync(new CreateBookingRequest(property.Id, user.UserId, new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 4)), CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.ResolveVerificationAsync(booking.Id, new ResolveVerificationRequest(false, "unknown-transaction"), CancellationToken.None));

        var rejected = await harness.Store.ResolveVerificationAsync(booking.Id, new ResolveVerificationRequest(false, booking.EkycTransactionId), CancellationToken.None);
        var duplicateRejection = await harness.Store.ResolveVerificationAsync(booking.Id, new ResolveVerificationRequest(false, booking.EkycTransactionId), CancellationToken.None);

        Assert.NotNull(rejected);
        Assert.NotNull(duplicateRejection);
        Assert.Equal("REJECTED", rejected.Status);
        Assert.Equal("FAILED", rejected.VerificationStatus);
        Assert.Equal("CANCELLED", rejected.PaymentStatus);
        Assert.False(rejected.DatesHeld);
        Assert.Equal(2, rejected.Notifications.Count);
        Assert.Equal(2, duplicateRejection.Notifications.Count);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.ResolveVerificationAsync(booking.Id, new ResolveVerificationRequest(true, booking.EkycTransactionId), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => harness.Store.CapturePaymentAsync(booking.Id, CancellationToken.None));

        var replacement = await harness.Store.CreateBookingAsync(new CreateBookingRequest(property.Id, user.UserId, new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 4)), CancellationToken.None);
        Assert.Equal("PENDING", replacement.Status);
    }

    [Fact]
    public async Task BookingQuotesAndCreationRejectInvalidUnavailableAndUnknownInputs()
    {
        var harness = CreateHarness();
        var user = await harness.Store.RegisterAsync(Registration("held@test.local", "Held Guest"), CancellationToken.None);
        var property = harness.Store.GetProperties().First(item => item.GuestVerificationEnabled);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.QuoteBookingAsync(new BookingQuoteRequest(property.Id, new DateOnly(2026, 8, 4), new DateOnly(2026, 8, 4)), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.QuoteBookingAsync(new BookingQuoteRequest(Guid.NewGuid(), new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 4)), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.CreateBookingAsync(new CreateBookingRequest(property.Id, Guid.NewGuid(), new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 4)), CancellationToken.None));

        var booking = await harness.Store.CreateBookingAsync(new CreateBookingRequest(property.Id, user.UserId, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 4)), CancellationToken.None);
        Assert.Equal("PENDING", booking.Status);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.QuoteBookingAsync(new BookingQuoteRequest(property.Id, new DateOnly(2026, 8, 2), new DateOnly(2026, 8, 5)), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Store.CreateBookingAsync(new CreateBookingRequest(property.Id, user.UserId, new DateOnly(2026, 8, 2), new DateOnly(2026, 8, 5)), CancellationToken.None));
    }

    [Fact]
    public async Task NonVerificationPropertyApprovesImmediatelyAndDoesNotStartEkyc()
    {
        var harness = CreateHarness();
        var user = await harness.Store.RegisterAsync(Registration("approved@test.local", "Approved Guest"), CancellationToken.None);
        var property = await harness.Store.CreatePropertyAsync(new CreatePropertyRequest(
            Guid.NewGuid(),
            "Free Host",
            "free-host@test.local",
            "No eKYC Cottage",
            "Port Antonio",
            "Jamaica",
            120m,
            "USD"),
            CancellationToken.None);

        var booking = await harness.Store.CreateBookingAsync(new CreateBookingRequest(property.Id, user.UserId, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 3)), CancellationToken.None);

        Assert.Equal("APPROVED", booking.Status);
        Assert.Equal("PASSED", booking.VerificationStatus);
        Assert.Equal("AUTHORIZED", booking.PaymentStatus);
        Assert.False(booking.RequiresGuestVerification);
        Assert.Equal(0, harness.EkycProvider.StartCount);
        Assert.DoesNotContain(booking.PriceBreakdown, line => line.Code == "guest-verification");
    }

    private static PhaseOneHarness CreateHarness(TimeProvider? timeProvider = null)
    {
        var ekycProvider = new TestEkycProvider();
        var paymentGateway = new TestPaymentGateway();
        var notificationGateway = new TestNotificationGateway();
        var store = new PhaseOneStore(ekycProvider, paymentGateway, notificationGateway, timeProvider ?? TimeProvider.System);
        return new PhaseOneHarness(store, ekycProvider, paymentGateway, notificationGateway);
    }

    private static RegisterUserRequest Registration(
        string email,
        string displayName,
        string? phone = null,
        string password = "Password123!",
        UserRole role = UserRole.Guest) =>
        new(email, password, displayName, phone, password, true, true, role);

    private static string ExtractStoredPasswordHash(PhaseOneStore store, Guid userId)
    {
        var usersField = typeof(PhaseOneStore).GetField("_users", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("PhaseOneStore user field not found.");
        var users = (System.Collections.IEnumerable)usersField.GetValue(store)!;
        foreach (var user in users)
        {
            var id = (Guid)user.GetType().GetProperty("Id")!.GetValue(user)!;
            if (id == userId)
            {
                return (string)user.GetType().GetProperty("PasswordHash")!.GetValue(user)!;
            }
        }

        throw new InvalidOperationException("Registered user not found.");
    }

    private static async Task<string> GetDevelopmentCodeAsync(PhaseOneStore store, string challengeId)
    {
        var code = await store.GetDevelopmentTwoFactorCodeAsync(challengeId, CancellationToken.None);
        Assert.NotNull(code);
        return code.Code;
    }

    private sealed record PhaseOneHarness(
        PhaseOneStore Store,
        TestEkycProvider EkycProvider,
        TestPaymentGateway PaymentGateway,
        TestNotificationGateway NotificationGateway);

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan by) => _utcNow = _utcNow.Add(by);
    }

    private sealed class TestEkycProvider : IEkycProvider
    {
        public string ProviderName => "Alibaba Cloud eKYC";
        public int StartCount { get; private set; }

        public Task<EkycStartResult> StartCheckAsync(EkycStartRequest request, CancellationToken cancellationToken)
        {
            StartCount++;
            return Task.FromResult(new EkycStartResult(
                ProviderName,
                VerificationStatus.Pending,
                $"test-ekyc-{request.MerchantBizId}",
                "https://ekyc.test/start",
                "{}"));
        }
    }

    private sealed class TestPaymentGateway : IPaymentGateway
    {
        public string ProviderName => "Stripe";
        public int AuthorizationCount { get; private set; }
        public int CaptureCount { get; private set; }
        public int RefundCount { get; private set; }

        public Task<PaymentAuthorizationResult> AuthorizeAsync(PaymentAuthorizationRequest request, CancellationToken cancellationToken)
        {
            AuthorizationCount++;
            return Task.FromResult(new PaymentAuthorizationResult(
                ProviderName,
                $"auth_{request.Currency}_{request.Amount:0.00}",
                "client_secret_test",
                PaymentStatus.Authorized,
                DateTimeOffset.UtcNow.AddDays(7)));
        }

        public Task<PaymentCaptureResult> CaptureAsync(PaymentCaptureRequest request, CancellationToken cancellationToken)
        {
            CaptureCount++;
            return Task.FromResult(new PaymentCaptureResult(
                ProviderName,
                $"capture_{request.AuthorizationReference}",
                PaymentStatus.Captured,
                request.Amount,
                request.Currency));
        }

        public Task<PaymentRefundResult> RefundAsync(PaymentRefundRequest request, CancellationToken cancellationToken)
        {
            RefundCount++;
            return Task.FromResult(new PaymentRefundResult(
                ProviderName,
                $"refund_{request.PaymentReference}",
                PaymentStatus.Refunded,
                request.Amount,
                request.Currency,
                DateTimeOffset.UtcNow));
        }
    }

    private sealed class TestNotificationGateway : INotificationGateway
    {
        public string ProviderName => "Test notifications";
        public List<NotificationMessage> Messages { get; } = [];

        public Task QueueAsync(NotificationMessage message, CancellationToken cancellationToken)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }
}
