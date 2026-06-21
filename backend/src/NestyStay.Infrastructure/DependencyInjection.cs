using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using NestyStay.Application.Abstractions;
using NestyStay.Application.PhaseOne;
using NestyStay.Application.PhaseTwo;
using NestyStay.Domain;
using NestyStay.Infrastructure.Persistence;
using NestyStay.Infrastructure.Persistence.Milestones;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace NestyStay.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, string? postgresConnectionString = null)
    {
        services.AddDbContext<NestyStayDbContext>(options =>
            options.UseNpgsql(postgresConnectionString ??
                              "Host=localhost;Port=5432;Database=nestystay_dev;Username=nestystay;Password=nestystay"));

        services.AddSingleton<IEkycProvider, AlibabaEkycProvider>();
        services.AddSingleton<IPaymentGateway, StripePaymentGateway>();
        services.AddSingleton<IStorageProvider, CloudflareR2StorageProvider>();
        services.AddSingleton<INotificationGateway, CompositeNotificationGateway>();
        services.AddSingleton<IInsuranceProvider, InsuraGuestProvider>();
        services.AddScoped<IPhaseOneStore, EfPhaseOneStore>();
        services.AddScoped<IPhaseTwoStore, EfPhaseTwoStore>();

        return services;
    }
}

internal sealed class AlibabaEkycProvider : IEkycProvider
{
    public string ProviderName => "Alibaba Cloud eKYC";

    public Task<EkycStartResult> StartCheckAsync(EkycStartRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.MerchantBizId))
        {
            throw new InvalidOperationException("Alibaba Cloud eKYC requires a merchant business id.");
        }

        var transactionId = $"aliyun_ekyc_{request.MerchantBizId[..Math.Min(request.MerchantBizId.Length, 24)]}";
        var baseUrl = Environment.GetEnvironmentVariable("ALIBABA_EKYC_TRANSACTION_URL_BASE") ??
                      "https://ekyc.alibaba-cloud.local/start";
        var transactionUrl =
            $"{baseUrl}?transactionId={Uri.EscapeDataString(transactionId)}&merchantBizId={Uri.EscapeDataString(request.MerchantBizId)}";
        var clientPayload = JsonSerializer.Serialize(new
        {
            productCode = "eKYC",
            request.DocumentType,
            request.CallbackUrl,
            request.MetaInfo
        });

        return Task.FromResult(new EkycStartResult(
            ProviderName,
            VerificationStatus.Pending,
            transactionId,
            transactionUrl,
            clientPayload));
    }
}

internal sealed class StripePaymentGateway : IPaymentGateway
{
    private static readonly HttpClient HttpClient = new()
    {
        BaseAddress = new Uri("https://api.stripe.com")
    };

    public string ProviderName => "Stripe";

    public async Task<PaymentAuthorizationResult> AuthorizeAsync(PaymentAuthorizationRequest request, CancellationToken cancellationToken)
    {
        var secretKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            var reference = $"stripe_local_auth_{request.BookingId:N}";
            return new PaymentAuthorizationResult(
                ProviderName,
                reference,
                $"local_client_secret_{request.BookingId:N}",
                PaymentStatus.Authorized,
                DateTimeOffset.UtcNow.AddDays(7));
        }

        var payload = new Dictionary<string, string>
        {
            ["amount"] = ToMinorUnits(request.Amount).ToString(),
            ["currency"] = request.Currency.ToLowerInvariant(),
            ["capture_method"] = "manual",
            ["description"] = request.Description,
            ["metadata[booking_id]"] = request.BookingId.ToString("N")
        };

        using var document = await SendStripeFormAsync(
            secretKey,
            HttpMethod.Post,
            "/v1/payment_intents",
            payload,
            cancellationToken);
        var root = document.RootElement;
        var status = root.GetProperty("status").GetString() ?? string.Empty;

        return new PaymentAuthorizationResult(
            ProviderName,
            root.GetProperty("id").GetString() ?? string.Empty,
            root.TryGetProperty("client_secret", out var clientSecret) ? clientSecret.GetString() : null,
            MapStripeStatus(status),
            null);
    }

    public async Task<PaymentCaptureResult> CaptureAsync(PaymentCaptureRequest request, CancellationToken cancellationToken)
    {
        var secretKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");
        if (string.IsNullOrWhiteSpace(secretKey) || request.AuthorizationReference.StartsWith("stripe_local_auth_", StringComparison.Ordinal))
        {
            return new PaymentCaptureResult(
                ProviderName,
                $"stripe_local_capture_{Guid.NewGuid():N}",
                PaymentStatus.Captured,
                request.Amount,
                request.Currency);
        }

        using var document = await SendStripeFormAsync(
            secretKey,
            HttpMethod.Post,
            $"/v1/payment_intents/{Uri.EscapeDataString(request.AuthorizationReference)}/capture",
            new Dictionary<string, string>(),
            cancellationToken);
        var root = document.RootElement;
        var status = root.GetProperty("status").GetString() ?? string.Empty;

        return new PaymentCaptureResult(
            ProviderName,
            root.GetProperty("id").GetString() ?? request.AuthorizationReference,
            MapStripeStatus(status),
            request.Amount,
            request.Currency);
    }

    private static async Task<JsonDocument> SendStripeFormAsync(
        string secretKey,
        HttpMethod method,
        string path,
        IReadOnlyDictionary<string, string> payload,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path)
        {
            Content = new FormUrlEncodedContent(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.ASCII.GetBytes($"{secretKey}:")));

        using var response = await HttpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Stripe request failed: {responseBody}");
        }

        return JsonDocument.Parse(responseBody);
    }

    private static long ToMinorUnits(decimal amount) =>
        decimal.ToInt64(decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero));

    private static PaymentStatus MapStripeStatus(string status) =>
        status switch
        {
            "requires_capture" => PaymentStatus.Authorized,
            "succeeded" => PaymentStatus.Captured,
            "canceled" => PaymentStatus.Cancelled,
            "requires_payment_method" or "requires_confirmation" or "requires_action" or "processing" => PaymentStatus.Pending,
            _ => PaymentStatus.Pending
        };
}

internal sealed class CloudflareR2StorageProvider : IStorageProvider
{
    public string ProviderName => "Cloudflare R2";

    public Task<string> CreateUploadUrlAsync(string objectKey, CancellationToken cancellationToken) =>
        Task.FromResult($"https://storage.nestystay.local/upload/{Uri.EscapeDataString(objectKey)}");
}

internal sealed class CompositeNotificationGateway : INotificationGateway
{
    public string ProviderName => "SES/Twilio/Firebase";

    public Task QueueAsync(NotificationMessage message, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

internal sealed class InsuraGuestProvider : IInsuranceProvider
{
    public string ProviderName => "InsuraGuest";

    public Task<IReadOnlyList<string>> GetAvailablePlansAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<string>>([
            "$50/month non-US: $10K property damage",
            "$69/month US: $10K property damage + $10K accidental medical",
            "$99/month US: $25K property damage + $25K accidental medical"
        ]);
}
