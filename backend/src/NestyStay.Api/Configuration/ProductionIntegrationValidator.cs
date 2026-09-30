using Microsoft.Extensions.Configuration;
using NestyStay.Application.Configuration;

namespace NestyStay.Api.Configuration;

public static class ProductionIntegrationValidator
{
    private const int MinimumSessionTokenSecretBytes = 32;
    private const int MinimumTotpProtectionKeyBytes = 32;

    private static readonly RequiredSetting[] RequiredSettings =
    [
        new("ConnectionStrings:Postgres", "ConnectionStrings__Postgres", "PostgreSQL connection string"),
        new("Security:SessionTokenSecret", "NESTYSTAY_SESSION_TOKEN_SECRET", "session token signing secret"),
        new("Security:TotpSecretProtectionKey", "NESTYSTAY_TOTP_SECRET_PROTECTION_KEY", "TOTP secret protection key"),
        new("Webhooks:SharedSecret", "NESTYSTAY_WEBHOOK_SHARED_SECRET", "webhook shared secret"),
        new("Webhooks:StripeSigningSecret", "STRIPE_WEBHOOK_SECRET", "Stripe webhook signing secret"),
        new("Integrations:StripeSecretKey", "STRIPE_SECRET_KEY", "Stripe secret key"),
        new("Integrations:StripePublishableKey", "STRIPE_PUBLISHABLE_KEY", "Stripe publishable key"),
        new("Integrations:InsuraGuestApiBaseUrl", "INSURAGUEST_API_BASE_URL", "InsuraGuest API base URL")
    ];

    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsProduction())
        {
            return;
        }

        var providerFlags = ProviderFeatureFlags.From(configuration);
        var requiredSettings = RequiredSettings.Concat(new[]
        {
            new RequiredSetting("Integrations:StripeIdentityReturnUrl", "STRIPE_IDENTITY_RETURN_URL", "Stripe Identity return URL")
        }).ToArray();

        ValidateIdentityProvider(providerFlags);
        ValidateRequiredSettings(configuration, requiredSettings);
        ValidateLegacyAdmin(configuration);
        ValidateSecretsAndDatabase(configuration);
        ValidateStripe(configuration);
        ValidateBrevo(configuration, providerFlags);
        ValidateStorage(configuration, environment, providerFlags);
        ValidateAdminBootstrap(configuration);
    }

    private static void ValidateIdentityProvider(ProviderFeatureFlags providerFlags)
    {
        if (providerFlags.EkycProvider is not ("stripe_identity" or "stripe-identity" or "stripeidentity"))
        {
            throw new InvalidOperationException(
                $"Unsupported identity provider '{providerFlags.EkycProvider}'. NestyStay supports stripe_identity only.");
        }
    }

    private static void ValidateRequiredSettings(IConfiguration configuration, RequiredSetting[] requiredSettings)
    {
        var missing = requiredSettings
            .Where(setting => string.IsNullOrWhiteSpace(Resolve(configuration, setting)))
            .Select(setting => $"{setting.Description} ({setting.ConfigurationKey} or {setting.EnvironmentKey})")
            .ToArray();

        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                "Production integration configuration is incomplete. Missing: " + string.Join("; ", missing));
        }

        foreach (var setting in requiredSettings)
        {
            RejectPlaceholderValue(configuration, setting);
        }
    }

    private static void ValidateLegacyAdmin(IConfiguration configuration)
    {
        if (configuration.GetValue<bool>("Security:AllowLegacyAdminTokens") &&
            string.IsNullOrWhiteSpace(Resolve(configuration, new RequiredSetting("Security:AdminTokenSha256", "NESTYSTAY_ADMIN_TOKEN_SHA256", "admin token hash"))))
        {
            throw new InvalidOperationException(
                "Production legacy administrator token support is enabled but admin token hash is not configured.");
        }

        if (configuration.GetValue<bool>("Security:AllowLegacyAdminTokens"))
        {
            RequireSha256Hex(configuration, new RequiredSetting("Security:AdminTokenSha256", "NESTYSTAY_ADMIN_TOKEN_SHA256", "admin token hash"));
            RejectPlaceholderValue(configuration, new RequiredSetting("Security:AdminTokenSha256", "NESTYSTAY_ADMIN_TOKEN_SHA256", "admin token hash"));
        }
    }

    private static void ValidateSecretsAndDatabase(IConfiguration configuration)
    {
        var sessionSecret = Resolve(configuration, RequiredSettings.Single(setting => setting.ConfigurationKey == "Security:SessionTokenSecret"));
        if (sessionSecret is not null && System.Text.Encoding.UTF8.GetByteCount(sessionSecret) < MinimumSessionTokenSecretBytes)
        {
            throw new InvalidOperationException("Production session token signing secret must be at least 32 bytes.");
        }

        var totpProtectionKey = Resolve(configuration, RequiredSettings.Single(setting => setting.ConfigurationKey == "Security:TotpSecretProtectionKey"));
        if (totpProtectionKey is not null && System.Text.Encoding.UTF8.GetByteCount(totpProtectionKey) < MinimumTotpProtectionKeyBytes)
        {
            throw new InvalidOperationException("Production TOTP secret protection key must be at least 32 bytes.");
        }

        var connectionString = Resolve(configuration, RequiredSettings.Single(setting => setting.ConfigurationKey == "ConnectionStrings:Postgres"));
        if (Contains(connectionString, "Database=nestystay_dev") || Contains(connectionString, "Password=nestystay")) // NOSONAR: this deliberately detects a known development credential marker.
        {
            throw new InvalidOperationException("Production PostgreSQL connection string uses a development database name or password.");
        }
    }

    private static void ValidateStripe(IConfiguration configuration)
    {
        var stripeSecretKey = Resolve(configuration, RequiredSettings.Single(setting => setting.ConfigurationKey == "Integrations:StripeSecretKey"));
        if (stripeSecretKey?.StartsWith("sk_test_", StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new InvalidOperationException("Production Stripe secret key must be a live key.");
        }

        var stripePublishableKey = Resolve(configuration, RequiredSettings.Single(setting => setting.ConfigurationKey == "Integrations:StripePublishableKey"));
        if (stripePublishableKey?.StartsWith("pk_test_", StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new InvalidOperationException("Production Stripe publishable key must be a live key.");
        }

        var stripeWebhookSecret = Resolve(configuration, RequiredSettings.Single(setting => setting.ConfigurationKey == "Webhooks:StripeSigningSecret"));
        if (stripeWebhookSecret?.StartsWith("whsec_test", StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new InvalidOperationException("Production Stripe webhook signing secret must be a live secret.");
        }

        var returnUrl = Resolve(configuration, new RequiredSetting("Integrations:StripeIdentityReturnUrl", "STRIPE_IDENTITY_RETURN_URL", "Stripe Identity return URL"));
        if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri) || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Production Stripe Identity return URL must be an absolute HTTPS URL.");
        }
    }

    private static void ValidateBrevo(IConfiguration configuration, ProviderFeatureFlags providerFlags)
    {
        // Keep production validation on the same selector resolution as DI and
        // the integration-health endpoint. This honors EMAIL_PROVIDER and its
        // backwards-compatible NESTYSTAY_EMAIL_PROVIDER alias instead of
        // validating only the legacy environment variable.
        var emailProvider = providerFlags.EmailProvider;
        var brevoEnabled = configuration["Email:Brevo:Enabled"] ?? Environment.GetEnvironmentVariable("BREVO_ENABLED");
        if (emailProvider.Equals("brevo", StringComparison.OrdinalIgnoreCase) && !string.Equals(brevoEnabled, "false", StringComparison.OrdinalIgnoreCase))
        {
            var brevoKey = configuration["Email:Brevo:ApiKey"] ?? Environment.GetEnvironmentVariable("BREVO_API_KEY");
            var senderEmail = configuration["Email:Brevo:SenderEmail"] ?? Environment.GetEnvironmentVariable("BREVO_SENDER_EMAIL");
            if (string.IsNullOrWhiteSpace(brevoKey) || string.IsNullOrWhiteSpace(senderEmail))
            {
                throw new InvalidOperationException("Production Brevo email is selected but BREVO_API_KEY or BREVO_SENDER_EMAIL is missing.");
            }
            if (Contains(brevoKey, "replace-with") || Contains(senderEmail, "example."))
            {
                throw new InvalidOperationException("Production Brevo email configuration contains a placeholder value.");
            }
        }
    }

    private static void ValidateStorage(IConfiguration configuration, IHostEnvironment environment, ProviderFeatureFlags providerFlags)
    {
        var storageProvider = providerFlags.ObjectStorageProvider.Trim().ToLowerInvariant();
        if (storageProvider is "minio" or "s3")
        {
            ValidateMinioStorage(configuration);
            return;
        }

        if (storageProvider is "local" or "filesystem" or "server" or "server_local")
        {
            ValidateLocalStorage(configuration, environment);
            return;
        }

        throw new InvalidOperationException(
            $"Unsupported object storage provider '{providerFlags.ObjectStorageProvider}'. Select MinIO/S3 or private server-local storage.");
    }

    private static void ValidateMinioStorage(IConfiguration configuration)
    {
        var minioEndpoint = configuration["Integrations:MinioEndpoint"] ?? Environment.GetEnvironmentVariable("MINIO_ENDPOINT");
        var minioBucket = configuration["Integrations:MinioBucket"] ?? Environment.GetEnvironmentVariable("MINIO_BUCKET");
        var minioAccessKey = configuration["Integrations:MinioAccessKey"] ?? Environment.GetEnvironmentVariable("MINIO_ACCESS_KEY") ?? configuration["Integrations:MinioRootUser"] ?? Environment.GetEnvironmentVariable("MINIO_ROOT_USER");
        var minioSecretKey = configuration["Integrations:MinioSecretKey"] ?? Environment.GetEnvironmentVariable("MINIO_SECRET_KEY") ?? configuration["Integrations:MinioRootPassword"] ?? Environment.GetEnvironmentVariable("MINIO_ROOT_PASSWORD");
        var missingMinio = new[]
        {
            (Value: minioEndpoint, Description: "MinIO endpoint"),
            (Value: minioBucket, Description: "MinIO bucket"),
            (Value: minioAccessKey, Description: "MinIO access key"),
            (Value: minioSecretKey, Description: "MinIO secret key")
        }.Where(setting => string.IsNullOrWhiteSpace(setting.Value)).Select(setting => setting.Description).ToArray();
        if (missingMinio.Length > 0)
        {
            throw new InvalidOperationException("Production MinIO storage configuration is incomplete. Missing: " + string.Join(", ", missingMinio));
        }

        RejectPlaceholderValue(minioEndpoint, "MinIO endpoint");
        RejectPlaceholderValue(minioBucket, "MinIO bucket");
        RejectPlaceholderValue(minioAccessKey, "MinIO access key");
        RejectPlaceholderValue(minioSecretKey, "MinIO secret key");
        if (!Uri.TryCreate(minioEndpoint, UriKind.Absolute, out var minioUri) ||
            (!string.Equals(minioUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(minioUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Production MinIO endpoint must be an absolute HTTP or HTTPS URL.");
        }

        if (System.Text.Encoding.UTF8.GetByteCount(minioSecretKey!) < 16)
        {
            throw new InvalidOperationException("Production MinIO secret key must be at least 16 bytes.");
        }
    }

    private static void ValidateLocalStorage(IConfiguration configuration, IHostEnvironment environment)
    {
        var localStorageSettings = new[]
        {
            new RequiredSetting("Integrations:LocalStorageRoot", "NESTYSTAY_STORAGE_LOCAL_ROOT", "private server storage root"),
            new RequiredSetting("Integrations:LocalStorageSigningSecret", "NESTYSTAY_STORAGE_SIGNING_SECRET", "private storage URL signing secret")
        };
        var missingLocalStorage = localStorageSettings
            .Where(setting => string.IsNullOrWhiteSpace(Resolve(configuration, setting)))
            .Select(setting => $"{setting.Description} ({setting.ConfigurationKey} or {setting.EnvironmentKey})")
            .ToArray();
        if (missingLocalStorage.Length > 0)
        {
            throw new InvalidOperationException("Production server-local storage configuration is incomplete. Missing: " + string.Join("; ", missingLocalStorage));
        }

        foreach (var setting in localStorageSettings) RejectPlaceholderValue(configuration, setting);

        var storageRoot = Resolve(configuration, localStorageSettings[0]);
        var storageSigningSecret = Resolve(configuration, localStorageSettings[1]);
        if (storageSigningSecret is null || System.Text.Encoding.UTF8.GetByteCount(storageSigningSecret) < 32)
        {
            throw new InvalidOperationException("Production private storage signing secret must be at least 32 bytes.");
        }

        if (!Path.IsPathRooted(storageRoot))
        {
            throw new InvalidOperationException("Production private server storage root must be an absolute path outside the application directory.");
        }

        var normalizedStorageRoot = Path.GetFullPath(storageRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var contentRoot = Path.GetFullPath(environment.ContentRootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var webRoot = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "wwwroot"))
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (IsSameOrChildPath(normalizedStorageRoot, contentRoot) || IsSameOrChildPath(normalizedStorageRoot, webRoot))
        {
            throw new InvalidOperationException("Production private server storage root must not be inside the application or web root.");
        }
    }

    private static void ValidateAdminBootstrap(IConfiguration configuration)
    {
        if (ResolveBoolean(configuration, "Security:AdminBootstrap:Enabled", "NESTYSTAY_ADMIN_BOOTSTRAP_ENABLED"))
        {
            var bootstrapEmail = new RequiredSetting("Security:AdminBootstrap:Email", "NESTYSTAY_ADMIN_BOOTSTRAP_EMAIL", "administrator bootstrap email");
            var bootstrapPassword = new RequiredSetting("Security:AdminBootstrap:Password", "NESTYSTAY_ADMIN_BOOTSTRAP_PASSWORD", "administrator bootstrap password");
            if (string.IsNullOrWhiteSpace(Resolve(configuration, bootstrapEmail)) || string.IsNullOrWhiteSpace(Resolve(configuration, bootstrapPassword)))
            {
                throw new InvalidOperationException("Production administrator bootstrap is enabled but bootstrap email or password is not configured.");
            }

            RejectPlaceholderValue(configuration, bootstrapEmail);
            RejectPlaceholderValue(configuration, bootstrapPassword);
        }
    }

    private static string? Resolve(IConfiguration configuration, RequiredSetting setting)
    {
        var configured = configuration[setting.ConfigurationKey];
        return string.IsNullOrWhiteSpace(configured)
            ? Environment.GetEnvironmentVariable(setting.EnvironmentKey)
            : configured;
    }

    private static bool ResolveBoolean(IConfiguration configuration, string configurationKey, string environmentKey)
    {
        if (bool.TryParse(configuration[configurationKey], out var configured))
        {
            return configured;
        }

        return bool.TryParse(Environment.GetEnvironmentVariable(environmentKey), out var environmentValue) && environmentValue;
    }

    private static void RejectPlaceholderValue(IConfiguration configuration, RequiredSetting setting)
    {
        var value = Resolve(configuration, setting);
        RejectPlaceholderValue(value, setting.Description);
    }

    private static void RejectPlaceholderValue(string? value, string description)
    {
        if (Contains(value, "replace-with") ||
            Contains(value, "<") ||
            Contains(value, "development-only") ||
            Contains(value, "dev-webhook-secret"))
        {
            throw new InvalidOperationException($"Production {description} contains a placeholder or development value.");
        }
    }

    private static void RequireSha256Hex(IConfiguration configuration, RequiredSetting setting)
    {
        var value = Resolve(configuration, setting)?.Trim();
        if (value is null ||
            value.Length != 64 ||
            value.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new InvalidOperationException($"Production {setting.Description} must be a SHA-256 hex digest.");
        }
    }

    private static bool Contains(string? value, string expected) =>
        value?.Contains(expected, StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsSameOrChildPath(string path, string parent)
    {
        var normalizedParent = parent.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return path.Equals(parent, StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith(normalizedParent, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record RequiredSetting(string ConfigurationKey, string EnvironmentKey, string Description);
}
