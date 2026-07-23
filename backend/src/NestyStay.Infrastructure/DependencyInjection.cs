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
using System.Security.Cryptography;
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
        services.AddSingleton<IFileSafetyScanner, MagicByteFileSafetyScanner>();
        services.AddSingleton<INotificationGateway, CompositeNotificationGateway>();
        services.AddSingleton<IEmailSender, LocalDevelopmentEmailSender>();
        services.AddSingleton<ISmsSender, LocalDevelopmentSmsSender>();
        services.AddSingleton<ISecretProtector, AesGcmSecretProtector>();
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

    public async Task<PaymentSetupIntentResult> CreateSetupIntentAsync(PaymentSetupIntentRequest request, CancellationToken cancellationToken)
    {
        var secretKey = ResolveSetting("Integrations:StripeSecretKey", "STRIPE_SECRET_KEY");
        var publishableKey = ResolveSetting("Integrations:StripePublishableKey", "STRIPE_PUBLISHABLE_KEY");
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            var reference = $"stripe_local_seti_{request.UserId:N}_{Guid.NewGuid():N}";
            return new PaymentSetupIntentResult(
                ProviderName,
                reference,
                $"{reference}_secret_local",
                "requires_payment_method",
                DateTimeOffset.UtcNow.AddMinutes(30),
                publishableKey ?? "pk_test_local");
        }

        var payload = new Dictionary<string, string>
        {
            ["usage"] = "off_session",
            ["payment_method_types[]"] = "card",
            ["metadata[user_id]"] = request.UserId.ToString("N"),
            ["metadata[customer_reference]"] = request.CustomerReference
        };

        using var document = await SendStripeFormAsync(
            secretKey,
            HttpMethod.Post,
            "/v1/setup_intents",
            payload,
            request.IdempotencyKey,
            cancellationToken);
        var root = document.RootElement;

        return new PaymentSetupIntentResult(
            ProviderName,
            root.GetProperty("id").GetString() ?? string.Empty,
            root.GetProperty("client_secret").GetString() ?? string.Empty,
            root.GetProperty("status").GetString() ?? string.Empty,
            DateTimeOffset.UtcNow.AddMinutes(30),
            publishableKey);
    }

    public async Task<PaymentMethodTokenizationResult> GetPaymentMethodAsync(PaymentMethodTokenizationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SetupIntentReference))
        {
            throw new InvalidOperationException("Stripe setup intent reference is required.");
        }

        var secretKey = ResolveSetting("Integrations:StripeSecretKey", "STRIPE_SECRET_KEY");
        if (string.IsNullOrWhiteSpace(secretKey) ||
            request.SetupIntentReference.StartsWith("stripe_local_seti_", StringComparison.Ordinal))
        {
            var referenceHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.SetupIntentReference))).ToLowerInvariant()[..16];
            return new PaymentMethodTokenizationResult(
                ProviderName,
                $"stripe_local_pm_{referenceHash}",
                "Visa",
                "4242",
                12,
                DateTimeOffset.UtcNow.Year + 3);
        }

        using var setupIntentDocument = await SendStripeFormAsync(
            secretKey,
            HttpMethod.Get,
            $"/v1/setup_intents/{Uri.EscapeDataString(request.SetupIntentReference)}",
            new Dictionary<string, string>(),
            null,
            cancellationToken);
        var setupIntent = setupIntentDocument.RootElement;
        var status = setupIntent.GetProperty("status").GetString();
        if (!string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Stripe setup intent must be confirmed before saving the payment method.");
        }

        var paymentMethodReference = setupIntent.GetProperty("payment_method").GetString();
        if (string.IsNullOrWhiteSpace(paymentMethodReference))
        {
            throw new InvalidOperationException("Stripe setup intent did not include a payment method.");
        }

        using var paymentMethodDocument = await SendStripeFormAsync(
            secretKey,
            HttpMethod.Get,
            $"/v1/payment_methods/{Uri.EscapeDataString(paymentMethodReference)}",
            new Dictionary<string, string>(),
            null,
            cancellationToken);
        var paymentMethod = paymentMethodDocument.RootElement;
        var card = paymentMethod.GetProperty("card");

        return new PaymentMethodTokenizationResult(
            ProviderName,
            paymentMethodReference,
            NormalizeCardBrand(card.GetProperty("brand").GetString()),
            card.GetProperty("last4").GetString() ?? string.Empty,
            card.GetProperty("exp_month").GetInt32(),
            card.GetProperty("exp_year").GetInt32());
    }

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

    public async Task<PaymentRefundResult> RefundAsync(PaymentRefundRequest request, CancellationToken cancellationToken)
    {
        var secretKey = ResolveSetting("Integrations:StripeSecretKey", "STRIPE_SECRET_KEY");
        if (string.IsNullOrWhiteSpace(secretKey) || request.PaymentReference.StartsWith("stripe_local_capture_", StringComparison.Ordinal))
        {
            var refundReference = string.IsNullOrWhiteSpace(request.IdempotencyKey)
                ? $"stripe_local_refund_{Guid.NewGuid():N}"
                : $"stripe_local_refund_{request.IdempotencyKey.Replace(":", "_", StringComparison.Ordinal).Replace("/", "_", StringComparison.Ordinal)}";
            return new PaymentRefundResult(
                ProviderName,
                refundReference,
                PaymentStatus.Refunded,
                request.Amount,
                request.Currency,
                DateTimeOffset.UtcNow);
        }

        var payload = new Dictionary<string, string>
        {
            ["payment_intent"] = request.PaymentReference,
            ["amount"] = ToMinorUnits(request.Amount).ToString(CultureInfo.InvariantCulture),
            ["reason"] = "requested_by_customer",
            ["metadata[reason]"] = request.Reason
        };

        using var document = await SendStripeFormAsync(
            secretKey,
            HttpMethod.Post,
            "/v1/refunds",
            payload,
            request.IdempotencyKey,
            cancellationToken);
        var root = document.RootElement;
        var status = root.GetProperty("status").GetString() ?? string.Empty;

        return new PaymentRefundResult(
            ProviderName,
            root.GetProperty("id").GetString() ?? string.Empty,
            MapStripeRefundStatus(status),
            request.Amount,
            request.Currency,
            DateTimeOffset.UtcNow);
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
            Content = method == HttpMethod.Get ? null : new FormUrlEncodedContent(payload)
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

    private static string NormalizeCardBrand(string? brand) =>
        string.IsNullOrWhiteSpace(brand)
            ? "Card"
            : string.Join(' ', brand.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(word => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(word.ToLowerInvariant())));

    private static PaymentStatus MapStripeStatus(string status) =>
        status switch
        {
            "requires_capture" => PaymentStatus.Authorized,
            "succeeded" => PaymentStatus.Captured,
            "canceled" => PaymentStatus.Cancelled,
            "requires_payment_method" or "requires_confirmation" or "requires_action" or "processing" => PaymentStatus.Pending,
            _ => PaymentStatus.Pending
        };

    private static PaymentStatus MapStripeRefundStatus(string status) =>
        status switch
        {
            "succeeded" => PaymentStatus.Refunded,
            "failed" or "canceled" => PaymentStatus.Failed,
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
    private const int BufferSize = 81920;
    private const int HeaderByteLimit = 512;

    public string ProviderName => "Cloudflare R2";

    public Task<string> CreateUploadUrlAsync(string objectKey, CancellationToken cancellationToken)
    {
        var baseUrl = ResolveSetting("Integrations:CloudflareR2UploadUrlBase", "CLOUDFLARE_R2_UPLOAD_URL_BASE") ??
                      "https://storage.nestystay.local/upload";

        return Task.FromResult($"{baseUrl.TrimEnd('/')}/{Uri.EscapeDataString(objectKey)}");
    }

    public async Task<StorageObjectWriteResult> SaveObjectAsync(StorageObjectWriteRequest request, Stream content, CancellationToken cancellationToken)
    {
        if (request.MaximumBytes <= 0)
        {
            throw new InvalidOperationException("Upload size limit is invalid.");
        }

        var root = ResolveLocalStorageRoot();
        var targetPath = ResolveObjectPath(root, request.ObjectKey);
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        var temporaryPath = $"{targetPath}.{Guid.NewGuid():N}.tmp";
        var success = false;
        long totalBytes = 0;
        var headerBytes = new List<byte>(HeaderByteLimit);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[BufferSize];

        try
        {
            await using (var output = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, true))
            {
                while (true)
                {
                    var bytesRead = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    totalBytes += bytesRead;
                    if (totalBytes > request.MaximumBytes)
                    {
                        throw new InvalidOperationException("Attachment exceeds the allowed size.");
                    }

                    var headerRemaining = HeaderByteLimit - headerBytes.Count;
                    if (headerRemaining > 0)
                    {
                        headerBytes.AddRange(buffer.AsSpan(0, Math.Min(bytesRead, headerRemaining)).ToArray());
                    }

                    hash.AppendData(buffer.AsSpan(0, bytesRead));
                    await output.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                }

                if (totalBytes == 0)
                {
                    throw new InvalidOperationException("Attachment upload cannot be empty.");
                }
            }

            File.Move(temporaryPath, targetPath, true);
            success = true;
        }
        finally
        {
            if (!success && File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }

        return new StorageObjectWriteResult(
            ProviderName,
            request.ObjectKey,
            request.ContentType.Trim().ToLowerInvariant(),
            totalBytes,
            Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant(),
            headerBytes.ToArray());
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

    private string ResolveLocalStorageRoot()
    {
        var configured = ResolveSetting("Integrations:LocalStorageRoot", "NESTYSTAY_STORAGE_LOCAL_ROOT");
        return Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Path.GetTempPath(), "nestystay-platform-storage")
            : configured);
    }

    private static string ResolveObjectPath(string root, string objectKey)
    {
        var segments = objectKey.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            throw new InvalidOperationException("Storage object key is required.");
        }

        var invalidCharacters = Path.GetInvalidFileNameChars();
        var path = root;
        foreach (var segment in segments)
        {
            if (segment is "." or ".." || segment.IndexOfAny(invalidCharacters) >= 0)
            {
                throw new InvalidOperationException("Storage object key is invalid.");
            }

            path = Path.Combine(path, segment);
        }

        var rootWithSeparator = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(path);
        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Storage object key is outside the configured storage root.");
        }

        return fullPath;
    }
}

internal sealed class MagicByteFileSafetyScanner : IFileSafetyScanner
{
    public string ProviderName => "Local magic-byte scanner";

    public Task<FileSafetyScanResult> ScanAsync(FileSafetyScanRequest request, CancellationToken cancellationToken)
    {
        if (request.SizeBytes <= 0)
        {
            return Task.FromResult(new FileSafetyScanResult("Rejected", "Attachment upload cannot be empty."));
        }

        if (!IsSha256Hash(request.Sha256Hash))
        {
            return Task.FromResult(new FileSafetyScanResult("Rejected", "Attachment checksum is invalid."));
        }

        var allowed = request.ContentType.Trim().ToLowerInvariant() switch
        {
            "image/jpeg" => StartsWith(request.HeaderBytes, [0xFF, 0xD8, 0xFF]),
            "image/png" => StartsWith(request.HeaderBytes, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
            "image/webp" => HasWebpHeader(request.HeaderBytes),
            "application/pdf" => StartsWith(request.HeaderBytes, Encoding.ASCII.GetBytes("%PDF-")),
            _ => false
        };

        var result = allowed
            ? new FileSafetyScanResult("Clean", null)
            : new FileSafetyScanResult("Rejected", "Attachment bytes do not match the declared content type.");
        return Task.FromResult(result);
    }

    private static bool StartsWith(byte[] value, byte[] prefix)
    {
        if (value.Length < prefix.Length)
        {
            return false;
        }

        for (var index = 0; index < prefix.Length; index++)
        {
            if (value[index] != prefix[index])
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasWebpHeader(byte[] value) =>
        value.Length >= 12 &&
        StartsWith(value, Encoding.ASCII.GetBytes("RIFF")) &&
        value[8] == (byte)'W' &&
        value[9] == (byte)'E' &&
        value[10] == (byte)'B' &&
        value[11] == (byte)'P';

    private static bool IsSha256Hash(string value) =>
        value.Length == 64 && value.All(character =>
            (character >= '0' && character <= '9') ||
            (character >= 'a' && character <= 'f') ||
            (character >= 'A' && character <= 'F'));
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

public sealed class AesGcmSecretProtector(IConfiguration configuration) : ISecretProtector
{
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;
    private static readonly byte[] Prefix = Encoding.ASCII.GetBytes("NSTY.AESGCM.V1.");
    private readonly byte[] _key = SHA256.HashData(Encoding.UTF8.GetBytes(ResolveKey(configuration)));

    public byte[] Protect(string purpose, byte[] secret)
    {
        ArgumentNullException.ThrowIfNull(secret);
        if (secret.Length == 0)
        {
            throw new InvalidOperationException("Cannot protect an empty secret.");
        }

        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var ciphertext = new byte[secret.Length];
        var tag = new byte[TagSizeBytes];
        using var aes = new AesGcm(_key, TagSizeBytes);
        aes.Encrypt(nonce, secret, ciphertext, tag, Encoding.UTF8.GetBytes(purpose));

        var protectedSecret = new byte[Prefix.Length + nonce.Length + tag.Length + ciphertext.Length];
        Prefix.CopyTo(protectedSecret, 0);
        nonce.CopyTo(protectedSecret, Prefix.Length);
        tag.CopyTo(protectedSecret, Prefix.Length + nonce.Length);
        ciphertext.CopyTo(protectedSecret, Prefix.Length + nonce.Length + tag.Length);
        return protectedSecret;
    }

    public byte[] Unprotect(string purpose, byte[] protectedSecret)
    {
        ArgumentNullException.ThrowIfNull(protectedSecret);
        if (!IsProtected(protectedSecret))
        {
            return protectedSecret.ToArray();
        }

        var minimumLength = Prefix.Length + NonceSizeBytes + TagSizeBytes + 1;
        if (protectedSecret.Length < minimumLength)
        {
            throw new InvalidOperationException("Protected secret payload is invalid.");
        }

        var nonce = protectedSecret.AsSpan(Prefix.Length, NonceSizeBytes);
        var tag = protectedSecret.AsSpan(Prefix.Length + NonceSizeBytes, TagSizeBytes);
        var ciphertext = protectedSecret.AsSpan(Prefix.Length + NonceSizeBytes + TagSizeBytes);
        var secret = new byte[ciphertext.Length];
        using var aes = new AesGcm(_key, TagSizeBytes);
        aes.Decrypt(nonce, ciphertext, tag, secret, Encoding.UTF8.GetBytes(purpose));
        return secret;
    }

    public bool IsProtected(byte[] protectedSecret) =>
        protectedSecret.Length > Prefix.Length &&
        protectedSecret.AsSpan(0, Prefix.Length).SequenceEqual(Prefix);

    private static string ResolveKey(IConfiguration configuration)
    {
        var configured = configuration["Security:TotpSecretProtectionKey"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        var environmentKey = Environment.GetEnvironmentVariable("NESTYSTAY_TOTP_SECRET_PROTECTION_KEY");
        if (!string.IsNullOrWhiteSpace(environmentKey))
        {
            return environmentKey;
        }

        var sessionSecret = configuration["Security:SessionTokenSecret"] ??
            Environment.GetEnvironmentVariable("NESTYSTAY_SESSION_TOKEN_SECRET");
        return string.IsNullOrWhiteSpace(sessionSecret)
            ? "development-only-nestystay-totp-secret-protection-key"
            : sessionSecret;
    }
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
