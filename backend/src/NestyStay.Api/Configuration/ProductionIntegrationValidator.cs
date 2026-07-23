namespace NestyStay.Api.Configuration;

public static class ProductionIntegrationValidator
{
    private const int MinimumSessionTokenSecretBytes = 32;

    private static readonly RequiredSetting[] RequiredSettings =
    [
        new("Security:AdminTokenSha256", "NESTYSTAY_ADMIN_TOKEN_SHA256", "admin token hash"),
        new("Security:SessionTokenSecret", "NESTYSTAY_SESSION_TOKEN_SECRET", "session token signing secret"),
        new("Webhooks:SharedSecret", "NESTYSTAY_WEBHOOK_SHARED_SECRET", "webhook shared secret"),
        new("Integrations:StripeSecretKey", "STRIPE_SECRET_KEY", "Stripe secret key"),
        new("Integrations:AlibabaEkycTransactionUrlBase", "ALIBABA_EKYC_TRANSACTION_URL_BASE", "Alibaba eKYC URL base"),
        new("Integrations:CloudflareR2UploadUrlBase", "CLOUDFLARE_R2_UPLOAD_URL_BASE", "Cloudflare R2 upload URL base"),
        new("Integrations:InsuraGuestApiBaseUrl", "INSURAGUEST_API_BASE_URL", "InsuraGuest API base URL")
    ];

    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsProduction())
        {
            return;
        }

        var missing = RequiredSettings
            .Where(setting => string.IsNullOrWhiteSpace(Resolve(configuration, setting)))
            .Select(setting => $"{setting.Description} ({setting.ConfigurationKey} or {setting.EnvironmentKey})")
            .ToArray();

        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                "Production integration configuration is incomplete. Missing: " + string.Join("; ", missing));
        }

        var sessionSecret = Resolve(configuration, RequiredSettings.Single(setting => setting.ConfigurationKey == "Security:SessionTokenSecret"));
        if (sessionSecret is not null && System.Text.Encoding.UTF8.GetByteCount(sessionSecret) < MinimumSessionTokenSecretBytes)
        {
            throw new InvalidOperationException("Production session token signing secret must be at least 32 bytes.");
        }
    }

    private static string? Resolve(IConfiguration configuration, RequiredSetting setting)
    {
        var configured = configuration[setting.ConfigurationKey];
        return string.IsNullOrWhiteSpace(configured)
            ? Environment.GetEnvironmentVariable(setting.EnvironmentKey)
            : configured;
    }

    private sealed record RequiredSetting(string ConfigurationKey, string EnvironmentKey, string Description);
}
