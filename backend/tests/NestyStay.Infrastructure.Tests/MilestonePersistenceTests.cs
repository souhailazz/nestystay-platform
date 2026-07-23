using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Text;
using NestyStay.Application.Abstractions;
using NestyStay.Application.PhaseOne;
using NestyStay.Application.PhaseTwo;
using NestyStay.Application.Services;
using NestyStay.Domain;
using NestyStay.Infrastructure.Persistence;
using NestyStay.Infrastructure.Persistence.Milestones;

namespace NestyStay.Infrastructure.Tests;

public sealed class MilestonePersistenceTests
{
    [Fact]
    public async Task PhaseOneStorePersistsUsersTwoFactorPropertiesBookingsAndVerificationFlow()
    {
        var databaseName = $"phase-one-persistence-{Guid.NewGuid():N}";
        var root = new InMemoryDatabaseRoot();
        var providers = new ProviderHarness();
        Guid userId;
        Guid bookingId;
        string? transactionId;

        await using (var db = CreateContext(databaseName, root))
        {
            var store = CreatePhaseOneStore(db, providers);
            var registered = await store.RegisterAsync(
                new RegisterUserRequest("persisted@test.local", "Password123!", "Persisted Guest", null, "Password123!", true, true),
                CancellationToken.None);
            var storedUser = await db.MilestoneUsers.SingleAsync(user => user.Id == registered.UserId);
            Assert.True(providers.SecretProtector.IsProtected(storedUser.TwoFactorSecret));
            Assert.NotEqual(20, storedUser.TwoFactorSecret.Length);

            var property = store.GetProperties().First(item => item.GuestVerificationEnabled);
            var booking = await store.CreateBookingAsync(
                new CreateBookingRequest(property.Id, registered.UserId, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 4)),
                CancellationToken.None);

            userId = registered.UserId;
            bookingId = booking.Id;
            transactionId = booking.EkycTransactionId;
        }

        await using (var db = CreateContext(databaseName, root))
        {
            var store = CreatePhaseOneStore(db, providers);
            var login = await store.LoginAsync(new LoginRequest("persisted@test.local", "Password123!"), CancellationToken.None);
            Assert.NotNull(login.ChallengeId);
            var code = await store.GetDevelopmentTwoFactorCodeAsync(login.ChallengeId, CancellationToken.None);
            Assert.NotNull(code);
            var session = await store.VerifyTwoFactorAsync(new VerifyTwoFactorRequest(login.ChallengeId, code.Code), CancellationToken.None);
            var booking = store.GetBooking(bookingId);

            Assert.Equal(userId, session.UserId);
            Assert.NotNull(booking);
            Assert.Equal("PENDING", booking.Status);
            Assert.True(booking.DatesHeld);

            var approved = await store.ResolveVerificationAsync(
                bookingId,
                new ResolveVerificationRequest(true, transactionId),
                CancellationToken.None);

            Assert.NotNull(approved);
            Assert.Equal("APPROVED", approved.Status);
            Assert.Equal("AUTHORIZED", approved.PaymentStatus);

            var authorizationAttempt = await db.MilestonePaymentAttempts.SingleAsync(item => item.BookingId == bookingId && item.Operation == "Authorize");
            Assert.Equal(PaymentStatus.Authorized, authorizationAttempt.Status);
            Assert.StartsWith($"booking:{bookingId:N}:authorize", authorizationAttempt.IdempotencyKey, StringComparison.Ordinal);

            var captured = await store.CapturePaymentAsync(bookingId, CancellationToken.None);
            Assert.NotNull(captured);
            Assert.Equal("CAPTURED", captured.PaymentStatus);

            var captureAttempt = await db.MilestonePaymentAttempts.SingleAsync(item => item.BookingId == bookingId && item.Operation == "Capture");
            Assert.Equal(PaymentStatus.Captured, captureAttempt.Status);
            Assert.StartsWith($"booking:{bookingId:N}:capture", captureAttempt.IdempotencyKey, StringComparison.Ordinal);

            var refunded = await store.RefundPaymentAsync(
                bookingId,
                new RefundBookingRequest(Reason: "Persistence refund test", IdempotencyKey: $"refund-{bookingId:N}"),
                CancellationToken.None);
            Assert.NotNull(refunded);
            Assert.Equal("REFUNDED", refunded.PaymentStatus);
            Assert.Equal(refunded.TotalAmount, refunded.RefundedAmount);
            Assert.NotNull(refunded.PaymentRefundReference);

            var refundAttempt = await db.MilestonePaymentAttempts.SingleAsync(item => item.BookingId == bookingId && item.Operation == "Refund");
            Assert.Equal(PaymentStatus.Refunded, refundAttempt.Status);
            Assert.Equal(refunded.TotalAmount, refundAttempt.Amount);
            Assert.Equal($"refund-{bookingId:N}", refundAttempt.IdempotencyKey);
        }

        await using (var db = CreateContext(databaseName, root))
        {
            var store = CreatePhaseOneStore(db, providers);
            var booking = store.GetBooking(bookingId);

            Assert.NotNull(booking);
            Assert.Equal("APPROVED", booking.Status);
            Assert.Equal("REFUNDED", booking.PaymentStatus);
            Assert.Equal(booking.TotalAmount, booking.RefundedAmount);
            Assert.Equal("Persistence refund test", booking.RefundReason);
            Assert.Contains(booking.Notifications, item => item.RecipientType == "guest");
            Assert.Contains(booking.Timeline, item => item.Contains("Stripe manual-capture", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(booking.Timeline, item => item.Contains("refund", StringComparison.OrdinalIgnoreCase));
            Assert.Equal(3, await db.MilestonePaymentAttempts.CountAsync(item => item.BookingId == bookingId));
        }
    }

    [Fact]
    public void PhaseTwoStorePersistsPricebookAssignmentsRenewalsCampaignsAndFoundingBenefits()
    {
        var databaseName = $"phase-two-persistence-{Guid.NewGuid():N}";
        var root = new InMemoryDatabaseRoot();
        var hostId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        Guid assignmentId;

        using (var db = CreateContext(databaseName, root))
        {
            var store = CreatePhaseTwoStore(db);
            var updated = store.UpdatePricebookItem(
                "verified-host-standard-annual",
                new UpdatePricebookItemRequest(88.88m, "USD", "Annual"));
            var verified = store.PurchaseBadge(new PurchaseBadgeRequest("Host", hostId, BadgeLevel.Verified, HostVerificationPassed: true));
            var campaign = store.CreateCampaign(new CreateCampaignRequest(
                $"trusted-{Guid.NewGuid():N}",
                "Trusted test campaign",
                "BadgePriceOverride",
                39m,
                "Hosts"));
            var benefit = store.UpsertFoundingBenefit(new FoundingBenefitRequest(propertyId, FoundingTier.Gold));

            assignmentId = verified.Id;
            Assert.Equal(88.88m, updated.Amount);
            Assert.True(campaign.IsActive);
            Assert.Equal(36m, benefit.GuestFlatFee);
        }

        using (var db = CreateContext(databaseName, root))
        {
            var store = CreatePhaseTwoStore(db);
            var pricebookItem = store.GetPricebookItem("verified-host-standard-annual");
            var assignments = store.GetBadgeAssignments("Host", hostId);
            var renewals = store.GetRenewals(assignmentId);
            var featureAccess = store.GetFeatureAccess("Host", hostId);
            var benefit = store.GetFoundingBenefit(propertyId);

            Assert.NotNull(pricebookItem);
            Assert.Equal(88.88m, pricebookItem.Amount);
            Assert.Contains(assignments, assignment => assignment.Id == assignmentId);
            Assert.Contains(renewals, renewal => renewal.PaymentStatus == "PENDING");
            Assert.Equal(BadgeLevel.Verified, featureAccess.ActiveLevel);
            Assert.NotNull(benefit);
            Assert.Equal(FoundingTier.Gold, benefit.Tier);
        }
    }

    private static NestyStayDbContext CreateContext(string databaseName, InMemoryDatabaseRoot root)
    {
        var options = new DbContextOptionsBuilder<NestyStayDbContext>()
            .UseInMemoryDatabase(databaseName, root)
            .Options;

        return new NestyStayDbContext(options);
    }

    private static EfPhaseOneStore CreatePhaseOneStore(NestyStayDbContext db, ProviderHarness providers) =>
        new(
            db,
            providers.EkycProvider,
            providers.PaymentGateway,
            providers.NotificationGateway,
            TimeProvider.System,
            secretProtector: providers.SecretProtector);

    private static EfPhaseTwoStore CreatePhaseTwoStore(NestyStayDbContext db) =>
        new(db, new PricebookService(), TimeProvider.System);

    private sealed class ProviderHarness
    {
        public TestEkycProvider EkycProvider { get; } = new();
        public TestPaymentGateway PaymentGateway { get; } = new();
        public TestNotificationGateway NotificationGateway { get; } = new();
        public ISecretProtector SecretProtector { get; } = new TestSecretProtector();
    }

    private sealed class TestSecretProtector : ISecretProtector
    {
        private static readonly byte[] Prefix = Encoding.ASCII.GetBytes("protected:");

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
        public List<NotificationMessage> Messages { get; } = [];

        public Task QueueAsync(NotificationMessage message, CancellationToken cancellationToken)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }
}
