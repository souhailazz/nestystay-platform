using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using NestyStay.Api.Controllers;
using NestyStay.Api.Webhooks;
using NestyStay.Application.PhaseOne;
using NestyStay.Domain;
using System.Security.Cryptography;
using System.Text;

namespace NestyStay.Api.Tests;

public sealed class WebhookSecurityTests
{
    [Fact]
    public async Task GenericWebhookRejectsMissingSecretInProduction()
    {
        var controller = CreateController();

        var result = await controller.Receive("provider", new WebhookEventRequest("provider", "event.received", "{}"), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GenericWebhookAcceptsMatchingSecretInProduction()
    {
        var controller = CreateController();
        controller.Request.Headers["X-NestyStay-Webhook-Secret"] = "test-webhook-secret";

        var result = await controller.Receive("provider", new WebhookEventRequest("provider", "event.received", "{}", Guid.NewGuid().ToString("N")), CancellationToken.None);

        var accepted = Assert.IsType<AcceptedResult>(result);
        Assert.NotNull(accepted.Value);
    }

    [Fact]
    public async Task StripeWebhookRejectsInvalidSignatureInProduction()
    {
        var controller = CreateController();
        controller.Request.Headers["Stripe-Signature"] = "t=1,v1=bad";

        var result = await controller.Receive("stripe", new WebhookEventRequest("stripe", "payment_intent.succeeded", "{\"id\":\"evt_bad\"}", "evt_bad"), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task StripeWebhookAcceptsValidSignatureAndRejectsReplayInProduction()
    {
        var eventId = $"evt_{Guid.NewGuid():N}";
        var payload = $"{{\"id\":\"{eventId}\"}}";
        var controller = CreateController();
        controller.Request.Headers["Stripe-Signature"] = CreateStripeSignature(payload);

        var acceptedResult = await controller.Receive("stripe", new WebhookEventRequest("stripe", "payment_intent.succeeded", payload, eventId), CancellationToken.None);

        Assert.IsType<AcceptedResult>(acceptedResult);

        var replayController = CreateController();
        replayController.Request.Headers["Stripe-Signature"] = CreateStripeSignature(payload);
        var replayResult = await replayController.Receive("stripe", new WebhookEventRequest("stripe", "payment_intent.succeeded", payload, eventId), CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(replayResult);
    }

    [Fact]
    public async Task StripeWebhookAppliesPaymentIntentUpdatesToBookingStore()
    {
        var eventId = $"evt_{Guid.NewGuid():N}";
        var payload = $$"""
            {
              "id": "{{eventId}}",
              "type": "payment_intent.succeeded",
              "data": {
                "object": {
                  "id": "pi_test_capture",
                  "amount_received": 61050,
                  "currency": "usd",
                  "latest_charge": "ch_test_capture",
                  "created": 1784768400
                }
              }
            }
            """;
        var store = new StubPhaseOneStore();
        var controller = CreateController(store);
        controller.Request.Headers["Stripe-Signature"] = CreateStripeSignature(payload);

        var result = await controller.Receive("stripe", new WebhookEventRequest("stripe", "payment_intent.succeeded", payload, eventId), CancellationToken.None);

        Assert.IsType<AcceptedResult>(result);
        var update = Assert.Single(store.PaymentWebhookUpdates);
        Assert.Equal("Stripe", update.ProviderName);
        Assert.Equal(eventId, update.ProviderEventId);
        Assert.Equal("payment_intent.succeeded", update.EventType);
        Assert.Equal("pi_test_capture", update.PaymentIntentReference);
        Assert.Equal("ch_test_capture", update.ProviderReference);
        Assert.Equal("USD", update.Currency);
        Assert.Equal(610.50m, update.Amount);
        Assert.Equal(PaymentStatus.Captured, update.Status);
    }

    private static WebhooksController CreateController(StubPhaseOneStore? store = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Webhooks:SharedSecret"] = "test-webhook-secret",
                ["Webhooks:StripeSigningSecret"] = "whsec_test"
            })
            .Build();

        var controller = new WebhooksController(store ?? new StubPhaseOneStore(), configuration, new ProductionEnvironment())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return controller;
    }

    private static string CreateStripeSignature(string payload)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("whsec_test"));
        var signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}.{payload}"))).ToLowerInvariant();
        return $"t={timestamp},v1={signature}";
    }

    private sealed class ProductionEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "NestyStay.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class StubPhaseOneStore : IPhaseOneStore
    {
        public Task<RegisterUserResponse> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<DevelopmentAuthCodeResponse?> GetDevelopmentTwoFactorCodeAsync(string challengeId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<BeginTwoFactorEnrollmentResponse> BeginTwoFactorEnrollmentAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ConfirmTwoFactorEnrollmentResponse> ConfirmTwoFactorEnrollmentAsync(Guid userId, ConfirmTwoFactorEnrollmentRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<DisableTwoFactorResponse> DisableTwoFactorAsync(Guid userId, DisableTwoFactorRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<GoogleSignInResponse> GoogleSignInAsync(GoogleSignInRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<VerifyTwoFactorResponse> VerifyTwoFactorAsync(VerifyTwoFactorRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PasswordResetRequestResponse> RequestPasswordResetAsync(PasswordResetRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<DevelopmentPasswordResetTokenResponse?> GetDevelopmentPasswordResetTokenAsync(string requestId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CompletePasswordResetResponse> CompletePasswordResetAsync(CompletePasswordResetRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> IsSessionActiveAsync(Guid userId, DateTimeOffset issuedAt, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<UserProfileDto> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ProfilePhotoUploadDto> PrepareProfilePhotoUploadAsync(Guid userId, PrepareProfilePhotoUploadRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ProfilePhotoUploadDto> UploadProfilePhotoContentAsync(Guid userId, Guid photoId, string contentType, long sizeBytes, Stream content, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ProfilePhotoDownloadDto> GetProfilePhotoDownloadAsync(Guid userId, Guid photoId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public IReadOnlyList<PropertyListingDto> GetProperties() =>
            throw new NotSupportedException();

        public PropertyListingDto? GetProperty(Guid id) =>
            throw new NotSupportedException();

        public Task<PropertyListingDto> CreatePropertyAsync(CreatePropertyRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PropertyListingDto> UpdatePropertyAsync(Guid hostUserId, Guid propertyId, UpdatePropertyRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PropertyListingDto> ArchivePropertyAsync(Guid hostUserId, Guid propertyId, bool isArchived, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeletePropertyAsync(Guid hostUserId, Guid propertyId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PropertyPhotoUploadDto> PreparePropertyPhotoUploadAsync(Guid hostUserId, Guid propertyId, PreparePropertyPhotoUploadRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PropertyPhotoUploadDto> UploadPropertyPhotoContentAsync(Guid hostUserId, Guid propertyId, Guid photoId, string contentType, long sizeBytes, Stream content, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<BookingQuoteDto> QuoteBookingAsync(BookingQuoteRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public IReadOnlyList<BookingDto> GetBookings(Guid? guestUserId = null) =>
            throw new NotSupportedException();

        public BookingDto? GetBooking(Guid id) =>
            throw new NotSupportedException();

        public Task<BookingDocumentDto?> GetBookingInvoiceAsync(Guid bookingId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<BookingDocumentDto?> GetBookingReceiptAsync(Guid bookingId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<BookingDto> CreateBookingAsync(CreateBookingRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<BookingDto?> ResolveVerificationAsync(Guid bookingId, ResolveVerificationRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<BookingDto?> CapturePaymentAsync(Guid bookingId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<BookingDto?> RefundPaymentAsync(Guid bookingId, RefundBookingRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public List<PaymentWebhookUpdateRequest> PaymentWebhookUpdates { get; } = [];

        public Task<BookingDto?> ApplyPaymentWebhookAsync(PaymentWebhookUpdateRequest request, CancellationToken cancellationToken)
        {
            PaymentWebhookUpdates.Add(request);
            return Task.FromResult<BookingDto?>(null);
        }
    }
}
