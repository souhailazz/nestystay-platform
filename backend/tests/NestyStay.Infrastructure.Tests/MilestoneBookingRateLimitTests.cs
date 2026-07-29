using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NestyStay.Application.Abstractions;
using NestyStay.Application.PhaseOne;
using NestyStay.Domain;
using NestyStay.Domain.Common;
using NestyStay.Infrastructure.Persistence;
using NestyStay.Infrastructure.Persistence.Milestones;

namespace NestyStay.Infrastructure.Tests;

public sealed class MilestoneBookingRateLimitTests
{
    [Fact]
    public async Task BookingCreationAllowsBelowAndThresholdRequestsThenRejectsAboveLimit()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 7, 28, 12, 0, 0, TimeSpan.Zero));
        var databaseName = $"booking-limit-{Guid.NewGuid():N}";
        var root = new InMemoryDatabaseRoot();
        var providers = new ProviderHarness();

        await using var db = CreateContext(databaseName, root);
        var store = CreateStore(db, providers, clock);
        var guest = await RegisterGuestAsync(store, "limit@test.local");
        var property = store.GetProperties().First(item => item.GuestVerificationEnabled);

        for (var index = 0; index < NestyStayBusinessRules.BookingCreationRateLimitMaximum; index++)
        {
            var booking = await store.CreateBookingAsync(
                BookingRequest(property.Id, guest.UserId, index),
                CancellationToken.None);
            Assert.Equal("PENDING", booking.Status);
        }

        var exception = await Assert.ThrowsAsync<RateLimitExceededException>(() =>
            store.CreateBookingAsync(
                BookingRequest(property.Id, guest.UserId, NestyStayBusinessRules.BookingCreationRateLimitMaximum),
                CancellationToken.None));

        Assert.Equal("rate_limit_exceeded", exception.Code);
        Assert.Equal(600, exception.RetryAfterSeconds);
        var bucket = await db.MilestoneBookingCreationRateLimits.SingleAsync(item => item.GuestUserId == guest.UserId);
        Assert.Equal(NestyStayBusinessRules.BookingCreationRateLimitMaximum, bucket.RequestCount);
    }

    [Fact]
    public async Task BookingCreationKeepsSeparateGuestQuotas()
    {
        var databaseName = $"booking-limit-guests-{Guid.NewGuid():N}";
        var root = new InMemoryDatabaseRoot();
        var providers = new ProviderHarness();

        await using var db = CreateContext(databaseName, root);
        var store = CreateStore(db, providers, TimeProvider.System);
        var firstGuest = await RegisterGuestAsync(store, "first-limit@test.local");
        var secondGuest = await RegisterGuestAsync(store, "second-limit@test.local");
        var property = store.GetProperties().First(item => item.GuestVerificationEnabled);

        for (var index = 0; index < NestyStayBusinessRules.BookingCreationRateLimitMaximum; index++)
        {
            await store.CreateBookingAsync(BookingRequest(property.Id, firstGuest.UserId, index), CancellationToken.None);
        }

        var secondGuestBooking = await store.CreateBookingAsync(
            BookingRequest(property.Id, secondGuest.UserId, 10),
            CancellationToken.None);

        Assert.Equal(secondGuest.UserId, secondGuestBooking.GuestUserId);
        Assert.Equal(2, await db.MilestoneBookingCreationRateLimits.CountAsync());
    }

    [Fact]
    public async Task BookingCreationWindowExpiresAndResetsRetryInformation()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 7, 28, 12, 0, 0, TimeSpan.Zero));
        var databaseName = $"booking-limit-window-{Guid.NewGuid():N}";
        var root = new InMemoryDatabaseRoot();
        var providers = new ProviderHarness();

        await using var db = CreateContext(databaseName, root);
        var store = CreateStore(db, providers, clock);
        var guest = await RegisterGuestAsync(store, "window-limit@test.local");
        var property = store.GetProperties().First(item => item.GuestVerificationEnabled);

        for (var index = 0; index < NestyStayBusinessRules.BookingCreationRateLimitMaximum; index++)
        {
            await store.CreateBookingAsync(BookingRequest(property.Id, guest.UserId, index), CancellationToken.None);
        }

        clock.Advance(TimeSpan.FromMinutes(NestyStayBusinessRules.BookingCreationRateLimitWindowMinutes).Add(TimeSpan.FromSeconds(1)));
        var afterWindow = await store.CreateBookingAsync(
            BookingRequest(property.Id, guest.UserId, 20),
            CancellationToken.None);

        Assert.Equal(guest.UserId, afterWindow.GuestUserId);
        var bucket = await db.MilestoneBookingCreationRateLimits.SingleAsync(item => item.GuestUserId == guest.UserId);
        Assert.Equal(1, bucket.RequestCount);
        Assert.Equal(clock.GetUtcNow().AddMinutes(NestyStayBusinessRules.BookingCreationRateLimitWindowMinutes), bucket.WindowEndsAt);
    }

    [Fact]
    public async Task ConcurrentBookingRequestsCannotBypassLimit()
    {
        var databaseName = $"booking-limit-concurrent-{Guid.NewGuid():N}";
        var root = new InMemoryDatabaseRoot();
        var providers = new ProviderHarness();
        Guid guestId;
        Guid propertyId;

        await using (var db = CreateContext(databaseName, root))
        {
            var store = CreateStore(db, providers, TimeProvider.System);
            var guest = await RegisterGuestAsync(store, "concurrent-limit@test.local");
            guestId = guest.UserId;
            propertyId = store.GetProperties().First(item => item.GuestVerificationEnabled).Id;
        }

        var outcomes = await Task.WhenAll(Enumerable.Range(0, NestyStayBusinessRules.BookingCreationRateLimitMaximum + 5)
            .Select(async index =>
            {
                await using var db = CreateContext(databaseName, root);
                var store = CreateStore(db, providers, TimeProvider.System);
                try
                {
                    await store.CreateBookingAsync(BookingRequest(propertyId, guestId, index), CancellationToken.None);
                    return "success";
                }
                catch (RateLimitExceededException)
                {
                    return "limited";
                }
            }));

        Assert.Equal(NestyStayBusinessRules.BookingCreationRateLimitMaximum, outcomes.Count(item => item == "success"));
        Assert.Equal(5, outcomes.Count(item => item == "limited"));
        await using var verificationDb = CreateContext(databaseName, root);
        Assert.Equal(NestyStayBusinessRules.BookingCreationRateLimitMaximum, await verificationDb.MilestoneBookings.CountAsync());
    }

    [Fact]
    public async Task InvalidGuestDoesNotConsumeBookingQuota()
    {
        var databaseName = $"booking-limit-invalid-{Guid.NewGuid():N}";
        var root = new InMemoryDatabaseRoot();
        var providers = new ProviderHarness();

        await using var db = CreateContext(databaseName, root);
        var store = CreateStore(db, providers, TimeProvider.System);
        var property = store.GetProperties().First(item => item.GuestVerificationEnabled);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.CreateBookingAsync(BookingRequest(property.Id, Guid.NewGuid(), 0), CancellationToken.None));

        Assert.Empty(await db.MilestoneBookingCreationRateLimits.ToListAsync());
    }

    private static CreateBookingRequest BookingRequest(Guid propertyId, Guid guestUserId, int index)
    {
        var checkIn = new DateOnly(2026, 9, 1).AddDays(index * 4);
        return new CreateBookingRequest(propertyId, guestUserId, checkIn, checkIn.AddDays(2));
    }

    private static async Task<RegisterUserResponse> RegisterGuestAsync(EfPhaseOneStore store, string email) =>
        await store.RegisterAsync(
            new RegisterUserRequest(email, "Password123!", "Rate Limit Guest", null, "Password123!", true, true),
            CancellationToken.None);

    private static NestyStayDbContext CreateContext(string databaseName, InMemoryDatabaseRoot root)
    {
        var options = new DbContextOptionsBuilder<NestyStayDbContext>()
            .UseInMemoryDatabase(databaseName, root)
            .Options;

        return new NestyStayDbContext(options);
    }

    private static EfPhaseOneStore CreateStore(NestyStayDbContext db, ProviderHarness providers, TimeProvider clock) =>
        new(
            db,
            providers.EkycProvider,
            providers.PaymentGateway,
            providers.NotificationGateway,
            clock,
            secretProtector: providers.SecretProtector);

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan by) => _utcNow = _utcNow.Add(by);
    }

    private sealed class ProviderHarness
    {
        public TestEkycProvider EkycProvider { get; } = new();
        public TestPaymentGateway PaymentGateway { get; } = new();
        public TestNotificationGateway NotificationGateway { get; } = new();
        public ISecretProtector SecretProtector { get; } = new TestSecretProtector();
    }

    private sealed class TestSecretProtector : ISecretProtector
    {
        private static readonly byte[] Prefix = "protected:"u8.ToArray();

        public byte[] Protect(string purpose, byte[] secret) =>
            [.. Prefix, .. secret.Reverse()];

        public byte[] Unprotect(string purpose, byte[] protectedSecret) =>
            IsProtected(protectedSecret)
                ? protectedSecret[Prefix.Length..].Reverse().ToArray()
                : protectedSecret.ToArray();

        public bool IsProtected(byte[] protectedSecret) =>
            protectedSecret.Length > Prefix.Length &&
            protectedSecret.AsSpan(0, Prefix.Length).SequenceEqual(Prefix);
    }

    private sealed class TestEkycProvider : IEkycProvider
    {
        public string ProviderName => "Alibaba Cloud eKYC";

        public Task<EkycStartResult> StartCheckAsync(EkycStartRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new EkycStartResult(
                ProviderName,
                VerificationStatus.Pending,
                $"test-ekyc-{request.MerchantBizId}",
                "https://ekyc.test/start",
                "{}"));
    }

    private sealed class TestPaymentGateway : IPaymentGateway
    {
        public string ProviderName => "Stripe";

        public Task<PaymentSetupIntentResult> CreateSetupIntentAsync(PaymentSetupIntentRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new PaymentSetupIntentResult(
                ProviderName,
                $"seti_test_{request.UserId:N}",
                "seti_client_secret_test",
                "requires_payment_method",
                DateTimeOffset.UtcNow.AddMinutes(30),
                "pk_test_local"));

        public Task<PaymentMethodTokenizationResult> GetPaymentMethodAsync(PaymentMethodTokenizationRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new PaymentMethodTokenizationResult(
                ProviderName,
                $"pm_{request.SetupIntentReference}",
                "Visa",
                "4242",
                12,
                DateTimeOffset.UtcNow.Year + 3));

        public Task<PaymentAuthorizationResult> AuthorizeAsync(PaymentAuthorizationRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new PaymentAuthorizationResult(
                ProviderName,
                $"auth_{request.Currency}_{request.Amount:0.00}",
                "client_secret_test",
                PaymentStatus.Authorized,
                DateTimeOffset.UtcNow.AddDays(7)));

        public Task<PaymentCaptureResult> CaptureAsync(PaymentCaptureRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new PaymentCaptureResult(
                ProviderName,
                $"capture_{request.AuthorizationReference}",
                PaymentStatus.Captured,
                request.Amount,
                request.Currency));

        public Task<PaymentRefundResult> RefundAsync(PaymentRefundRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new PaymentRefundResult(
                ProviderName,
                $"refund_{request.PaymentReference}",
                PaymentStatus.Refunded,
                request.Amount,
                request.Currency,
                DateTimeOffset.UtcNow));
    }

    private sealed class TestNotificationGateway : INotificationGateway
    {
        public string ProviderName => "Test notifications";

        public Task QueueAsync(NotificationMessage message, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
