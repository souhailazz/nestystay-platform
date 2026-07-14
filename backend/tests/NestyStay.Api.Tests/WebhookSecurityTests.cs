using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using NestyStay.Api.Controllers;
using NestyStay.Api.Webhooks;
using NestyStay.Application.PhaseOne;

namespace NestyStay.Api.Tests;

public sealed class WebhookSecurityTests
{
    [Fact]
    public void GenericWebhookRejectsMissingSecretInProduction()
    {
        var controller = CreateController();

        var result = controller.Receive("stripe", new WebhookEventRequest("stripe", "payment_intent.succeeded", "{}"));

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void GenericWebhookAcceptsMatchingSecretInProduction()
    {
        var controller = CreateController();
        controller.Request.Headers["X-NestyStay-Webhook-Secret"] = "test-webhook-secret";

        var result = controller.Receive("stripe", new WebhookEventRequest("stripe", "payment_intent.succeeded", "{}"));

        var accepted = Assert.IsType<AcceptedResult>(result);
        Assert.NotNull(accepted.Value);
    }

    private static WebhooksController CreateController()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Webhooks:SharedSecret"] = "test-webhook-secret"
            })
            .Build();

        var controller = new WebhooksController(new StubPhaseOneStore(), configuration, new ProductionEnvironment())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return controller;
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

        public Task<VerifyTwoFactorResponse> VerifyTwoFactorAsync(VerifyTwoFactorRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public IReadOnlyList<PropertyListingDto> GetProperties() =>
            throw new NotSupportedException();

        public PropertyListingDto? GetProperty(Guid id) =>
            throw new NotSupportedException();

        public Task<PropertyListingDto> CreatePropertyAsync(CreatePropertyRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<BookingQuoteDto> QuoteBookingAsync(BookingQuoteRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public IReadOnlyList<BookingDto> GetBookings(Guid? guestUserId = null) =>
            throw new NotSupportedException();

        public BookingDto? GetBooking(Guid id) =>
            throw new NotSupportedException();

        public Task<BookingDto> CreateBookingAsync(CreateBookingRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<BookingDto?> ResolveVerificationAsync(Guid bookingId, ResolveVerificationRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<BookingDto?> CapturePaymentAsync(Guid bookingId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
