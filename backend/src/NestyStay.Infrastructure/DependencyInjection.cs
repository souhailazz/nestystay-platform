using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using NestyStay.Application.Abstractions;
using NestyStay.Application.PhaseOne;
using NestyStay.Application.PhaseTwo;
using NestyStay.Application.SpecCompletion;
using NestyStay.Domain;
using NestyStay.Application.Wellness;
using NestyStay.Infrastructure.Persistence;
using NestyStay.Infrastructure.Persistence.Milestones;
using System.Globalization;
using System.Net.Http.Headers;
using System.Collections.Concurrent;
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
        services.AddSingleton<IEmailSender, LocalDevelopmentEmailSender>();
        services.AddSingleton<ISmsSender, LocalDevelopmentSmsSender>();
        services.AddSingleton<IDevelopmentAuthSecretStore, InMemoryDevelopmentAuthSecretStore>();
        services.AddSingleton<IGoogleIdentityValidator, GoogleTokenInfoValidator>();
        services.AddSingleton<IInsuranceProvider, InsuraGuestProvider>();
        services.AddScoped<IPhaseOneStore, EfPhaseOneStore>();
        services.AddScoped<IPhaseTwoStore, EfPhaseTwoStore>();
        services.AddScoped<IWellnessStore, EfWellnessStore>();
        services.AddScoped<ISpecCompletionStore, EfSpecCompletionStore>();

        return services;
    }
}

internal sealed class AlibabaEkycProvider(IConfiguration configuration) : IEkycProvider
{
    public string ProviderName => "Alibaba Cloud eKYC";

    public Task<EkycStartResult> StartCheckAsync(EkycStartRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.MerchantBizId))
        {
            throw new InvalidOperationException("Alibaba Cloud eKYC requires a merchant business id.");
        }

        var transactionId = $"aliyun_ekyc_{request.MerchantBizId[..Math.Min(request.MerchantBizId.Length, 24)]}";
        var baseUrl = ResolveSetting("Integrations:AlibabaEkycTransactionUrlBase", "ALIBABA_EKYC_TRANSACTION_URL_BASE") ??
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

    private string? ResolveSetting(string configurationKey, string environmentKey)
    {
        var configured = configuration[configurationKey];
        return string.IsNullOrWhiteSpace(configured)
            ? Environment.GetEnvironmentVariable(environmentKey)
            : configured;
    }
}

internal sealed class StripePaymentGateway(IConfiguration configuration) : IPaymentGateway
{
    private static readonly HttpClient HttpClient = new()
    {
        BaseAddress = new Uri("https://api.stripe.com")
    };

    public string ProviderName => "Stripe";

    public async Task<PaymentAuthorizationResult> AuthorizeAsync(PaymentAuthorizationRequest request, CancellationToken cancellationToken)
    {
        var secretKey = ResolveSetting("Integrations:StripeSecretKey", "STRIPE_SECRET_KEY");
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
            request.IdempotencyKey,
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
        var secretKey = ResolveSetting("Integrations:StripeSecretKey", "STRIPE_SECRET_KEY");
        if (string.IsNullOrWhiteSpace(secretKey) || request.AuthorizationReference.StartsWith("stripe_local_auth_", StringComparison.Ordinal))
        {
            var captureReference = string.IsNullOrWhiteSpace(request.IdempotencyKey)
                ? $"stripe_local_capture_{Guid.NewGuid():N}"
                : $"stripe_local_capture_{request.IdempotencyKey.Replace(":", "_", StringComparison.Ordinal).Replace("/", "_", StringComparison.Ordinal)}";
            return new PaymentCaptureResult(
                ProviderName,
                captureReference,
                PaymentStatus.Captured,
                request.Amount,
                request.Currency);
        }

        using var document = await SendStripeFormAsync(
            secretKey,
            HttpMethod.Post,
            $"/v1/payment_intents/{Uri.EscapeDataString(request.AuthorizationReference)}/capture",
            new Dictionary<string, string>(),
            request.IdempotencyKey,
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
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path)
        {
            Content = new FormUrlEncodedContent(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.ASCII.GetBytes($"{secretKey}:")));
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        }

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

    private string? ResolveSetting(string configurationKey, string environmentKey)
    {
        var configured = configuration[configurationKey];
        return string.IsNullOrWhiteSpace(configured)
            ? Environment.GetEnvironmentVariable(environmentKey)
            : configured;
    }
}

internal sealed class CloudflareR2StorageProvider(IConfiguration configuration) : IStorageProvider
{
    public string ProviderName => "Cloudflare R2";

    public Task<string> CreateUploadUrlAsync(string objectKey, CancellationToken cancellationToken)
    {
        var baseUrl = ResolveSetting("Integrations:CloudflareR2UploadUrlBase", "CLOUDFLARE_R2_UPLOAD_URL_BASE") ??
                      "https://storage.nestystay.local/upload";

        return Task.FromResult($"{baseUrl.TrimEnd('/')}/{Uri.EscapeDataString(objectKey)}");
    }

    public Task<string> CreateDownloadUrlAsync(string objectKey, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        var baseUrl = ResolveSetting("Integrations:CloudflareR2DownloadUrlBase", "CLOUDFLARE_R2_DOWNLOAD_URL_BASE") ??
                      ResolveSetting("Integrations:CloudflareR2UploadUrlBase", "CLOUDFLARE_R2_UPLOAD_URL_BASE") ??
                      "https://storage.nestystay.local/download";

        var expires = expiresAt.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
        return Task.FromResult($"{baseUrl.TrimEnd('/')}/{Uri.EscapeDataString(objectKey)}?expires={Uri.EscapeDataString(expires)}");
    }

    private string? ResolveSetting(string configurationKey, string environmentKey)
    {
        var configured = configuration[configurationKey];
        return string.IsNullOrWhiteSpace(configured)
            ? Environment.GetEnvironmentVariable(environmentKey)
            : configured;
    }
}

internal sealed class CompositeNotificationGateway : INotificationGateway
{
    public string ProviderName => "SES/Twilio/Firebase";

    public Task QueueAsync(NotificationMessage message, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

internal sealed class LocalDevelopmentEmailSender : IEmailSender
{
    public string ProviderName => "Local development email";

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

internal sealed class LocalDevelopmentSmsSender : ISmsSender
{
    public string ProviderName => "Local development SMS";

    public Task SendAsync(SmsMessage message, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

internal sealed class InMemoryDevelopmentAuthSecretStore(TimeProvider timeProvider) : IDevelopmentAuthSecretStore
{
    private readonly ConcurrentDictionary<Guid, DevelopmentAuthSecret> _secrets = new();

    public void Store(DevelopmentAuthSecret secret)
    {
        PruneExpired();
        _secrets[secret.CorrelationId] = secret;
    }

    public DevelopmentAuthSecret? Get(Guid correlationId)
    {
        PruneExpired();
        return _secrets.TryGetValue(correlationId, out var secret) ? secret : null;
    }

    public void Remove(Guid correlationId) =>
        _secrets.TryRemove(correlationId, out _);

    private void PruneExpired()
    {
        var now = timeProvider.GetUtcNow();
        foreach (var item in _secrets.Where(item => item.Value.ExpiresAt <= now))
        {
            _secrets.TryRemove(item.Key, out _);
        }
    }
}

internal sealed class GoogleTokenInfoValidator(IConfiguration configuration, TimeProvider timeProvider) : IGoogleIdentityValidator
{
    private static readonly HttpClient HttpClient = new()
    {
        BaseAddress = new Uri("https://oauth2.googleapis.com")
    };

    public string ProviderName => "Google Identity Services";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId);

    public async Task<GoogleIdentity> ValidateAsync(string credential, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Google sign-in is unavailable until server-side OAuth validation is configured.");
        }

        if (string.IsNullOrWhiteSpace(credential))
        {
            throw new InvalidOperationException("Google credential is required.");
        }

        using var response = await HttpClient.GetAsync($"/tokeninfo?id_token={Uri.EscapeDataString(credential.Trim())}", cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Google credential validation failed.");
        }

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var audience = RequiredString(root, "aud", "Google credential audience is missing.");
        if (!audience.Equals(ClientId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Google credential audience is invalid.");
        }

        var issuer = RequiredString(root, "iss", "Google credential issuer is missing.");
        if (issuer is not ("accounts.google.com" or "https://accounts.google.com"))
        {
            throw new InvalidOperationException("Google credential issuer is invalid.");
        }

        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(
            RequiredString(root, "exp", "Google credential expiration is missing."),
            CultureInfo.InvariantCulture));
        if (expiresAt <= timeProvider.GetUtcNow())
        {
            throw new InvalidOperationException("Google credential has expired.");
        }

        var emailVerified = root.TryGetProperty("email_verified", out var verifiedElement) &&
                            verifiedElement.ValueKind switch
                            {
                                JsonValueKind.True => true,
                                JsonValueKind.String => verifiedElement.GetString()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true,
                                _ => false
                            };
        if (!emailVerified)
        {
            throw new InvalidOperationException("Google account email must be verified.");
        }

        var email = RequiredString(root, "email", "Google credential email is missing.");
        var subject = RequiredString(root, "sub", "Google credential subject is missing.");
        var displayName = root.TryGetProperty("name", out var nameElement) && !string.IsNullOrWhiteSpace(nameElement.GetString())
            ? nameElement.GetString()!
            : email.Split('@')[0];
        var picture = root.TryGetProperty("picture", out var pictureElement) ? pictureElement.GetString() : null;

        return new GoogleIdentity(subject, email, displayName, true, expiresAt, issuer, audience, picture);
    }

    private string? ClientId => ResolveSetting("Authentication:Google:ClientId", "GOOGLE_AUTH_CLIENT_ID");

    private string? ResolveSetting(string configurationKey, string environmentKey)
    {
        var configured = configuration[configurationKey];
        return string.IsNullOrWhiteSpace(configured)
            ? Environment.GetEnvironmentVariable(environmentKey)
            : configured;
    }

    private static string RequiredString(JsonElement root, string propertyName, string error)
    {
        if (!root.TryGetProperty(propertyName, out var property) || string.IsNullOrWhiteSpace(property.GetString()))
        {
            throw new InvalidOperationException(error);
        }

        return property.GetString()!;
    }
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
