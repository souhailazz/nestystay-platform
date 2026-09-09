using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using NestyStay.Api.Auth;
using NestyStay.Application.Abstractions;
using NestyStay.Application.Configuration;
using NestyStay.Infrastructure.Persistence;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController(
    IConfiguration configuration,
    IPaymentGateway paymentGateway,
    IEkycProvider ekycProvider,
    IStorageProvider storageProvider,
    NestyStayDbContext db) : ControllerBase
{
    [HttpGet]
    [HttpHead]
    public IActionResult Get() => Ok(new
    {
        service = "NestyStay API",
        status = "ok",
        architecture = "Vite React frontend + ASP.NET Core backend",
        database = "PostgreSQL",
        openApi = "/openapi/v1.json"
    });

    [HttpGet("live")]
    [HttpHead("live")]
    public IActionResult GetLive() => Ok(new { status = "ok", check = "live" });

    [HttpGet("ready")]
    public async Task<IActionResult> GetReady(CancellationToken cancellationToken)
    {
        try
        {
            var databaseReady = await db.Database.CanConnectAsync(cancellationToken);
            return databaseReady
                ? Ok(new { status = "ok", database = "ready", storage = "configured" })
                : StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "degraded", database = "unavailable" });
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "degraded", database = "unavailable" });
        }
    }

    [Authorize(Policy = AdminAuthorizationPolicies.SystemConfiguration)]
    [HttpGet("integrations")]
    public async Task<IActionResult> GetIntegrations(CancellationToken cancellationToken)
    {
        var flags = ProviderFeatureFlags.From(configuration);
        var emailProvider = flags.EmailProvider;
        var brevoEnabled = configuration["Email:Brevo:Enabled"] ?? Environment.GetEnvironmentVariable("BREVO_ENABLED");
        var brevoConfigured = !string.IsNullOrWhiteSpace(configuration["Email:Brevo:ApiKey"] ?? Environment.GetEnvironmentVariable("BREVO_API_KEY")) &&
                              !string.IsNullOrWhiteSpace(configuration["Email:Brevo:SenderEmail"] ?? Environment.GetEnvironmentVariable("BREVO_SENDER_EMAIL"));
        var webPushEnabled = flags.WebPushEnabled;
        var webPushConfigured = HasSetting("WebPush:PublicKey", "VAPID_PUBLIC_KEY") &&
                                HasSetting("WebPush:PrivateKey", "VAPID_PRIVATE_KEY") &&
                                HasSetting("WebPush:Subject", "VAPID_SUBJECT");
        var pendingEmailCount = 0;
        var failedEmailCount = 0;
        var recentEmailFailureCount = 0;
        DateTimeOffset? lastSuccessfulEmailAt = null;
        try
        {
            pendingEmailCount = await db.NotificationQueue.CountAsync(item =>
                item.Channel == "Email" && (item.DeliveryStatus == "PENDING" || item.DeliveryStatus == "PROCESSING" || item.DeliveryStatus == "RETRYING"), cancellationToken);
            failedEmailCount = await db.NotificationQueue.CountAsync(item =>
                item.Channel == "Email" && (item.DeliveryStatus == "FAILED" || item.DeliveryStatus == "DEAD_LETTER"), cancellationToken);
            var recentFailureSince = DateTimeOffset.UtcNow.AddHours(-24);
            recentEmailFailureCount = await db.NotificationQueue.CountAsync(item =>
                item.Channel == "Email" && item.UpdatedAt >= recentFailureSince && (item.DeliveryStatus == "FAILED" || item.DeliveryStatus == "DEAD_LETTER"), cancellationToken);
            lastSuccessfulEmailAt = await db.NotificationQueue
                .Where(item => item.Channel == "Email" && item.DeliveryStatus == "SENT")
                .OrderByDescending(item => item.UpdatedAt)
                .Select(item => (DateTimeOffset?)item.UpdatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception)
        {
            // Integration status must remain useful even while PostgreSQL is being recovered.
            pendingEmailCount = -1;
            failedEmailCount = -1;
        }

        var emailStatus = emailProvider.Equals("brevo", StringComparison.OrdinalIgnoreCase) && !string.Equals(brevoEnabled, "false", StringComparison.OrdinalIgnoreCase)
            ? (brevoConfigured ? "CONFIGURED" : "BLOCKED_CREDENTIAL")
            : "CONFIGURED";
        var emailDetail = $"Transactional email transport; queue pending={pendingEmailCount}, failed={failedEmailCount}, recentFailures24h={recentEmailFailureCount}, lastSuccess={lastSuccessfulEmailAt?.ToString("O") ?? "never"}";
        var alibabaEkycConfigured = HasSetting("Integrations:AlibabaCloudAccessKeyId", "ALIBABA_CLOUD_ACCESS_KEY_ID") &&
                                    HasSetting("Integrations:AlibabaCloudAccessKeySecret", "ALIBABA_CLOUD_ACCESS_KEY_SECRET") &&
                                    HasSetting("Integrations:AlibabaEkycCallbackUrl", "ALIBABA_EKYC_CALLBACK_URL") &&
                                    HasSetting("Integrations:AlibabaEkycReturnUrl", "ALIBABA_EKYC_RETURN_URL") &&
                                    HasSetting("Integrations:AlibabaEkycCallbackToken", "ALIBABA_EKYC_CALLBACK_TOKEN");

        return Ok(new
        {
            generatedAt = DateTimeOffset.UtcNow,
            services = new[]
            {
                Service("payments", paymentGateway.ProviderName, HasSetting("Integrations:StripeSecretKey", "STRIPE_SECRET_KEY") ? "CONFIGURED" : "BLOCKED_EXTERNAL", "Stripe payment adapter"),
                Service("identity", ekycProvider.ProviderName, alibabaEkycConfigured ? "CONFIGURED" : "BLOCKED_EXTERNAL", "Alibaba eKYC adapter"),
                Service("email", emailProvider.Equals("brevo", StringComparison.OrdinalIgnoreCase) && !string.Equals(brevoEnabled, "false", StringComparison.OrdinalIgnoreCase) ? "Brevo transactional email" : "Local file capture", emailStatus.Replace("BLOCKED_CREDENTIAL", "BLOCKED_EXTERNAL", StringComparison.Ordinal), emailDetail),
                Service("storage", storageProvider.ProviderName, storageProvider.ProviderName.Contains("MinIO", StringComparison.OrdinalIgnoreCase) ? "CONFIGURED" : "DEGRADED", "Private API-authorized object storage"),
                Service("minio", storageProvider.ProviderName, storageProvider.ProviderName.Contains("MinIO", StringComparison.OrdinalIgnoreCase) ? "CONFIGURED" : "NOT_CONFIGURED", "Private bucket; admin console is internal-only"),
                Service("postgres", "PostgreSQL", HasSetting("ConnectionStrings:Postgres", "ConnectionStrings__Postgres") ? "CONFIGURED" : "NOT_CONFIGURED", "Application database connection"),
                Service("redis", "Self-hosted Redis", HasSetting("ConnectionStrings:Redis", "ConnectionStrings__Redis") ? "CONFIGURED" : "NOT_CONFIGURED", "Distributed coordination and rate-limit backing service"),
                Service("webPush", "Provider-neutral Web Push", webPushEnabled ? (webPushConfigured ? "CONFIGURED" : "BLOCKED_EXTERNAL") : "OPTIONAL_DISABLED", webPushEnabled ? "VAPID configuration is present; browser subscriptions remain opt-in" : "Optional channel disabled; in-app and email remain available"),
                Service("worker", "Self-hosted background worker", ResolveBoolean("Worker:SidecarEnabled", "WORKER_SIDECAR_ENABLED") || ResolveBoolean("Worker:Enabled", "WORKER_ENABLED") || ResolveBoolean("BackgroundJobs:Enabled", "BACKGROUND_JOBS_ENABLED") ? "CONFIGURED" : "NOT_CONFIGURED", "Worker sidecar is deployed separately; heartbeat is not exposed through the API"),
                Service("backups", "pg_dump + object archive + optional restic", HasSetting("Backup:Repository", "RESTIC_REPOSITORY") ? "CONFIGURED" : "BLOCKED_EXTERNAL", "Off-server encrypted destination is required before production"),
                Service("monitoring", "Prometheus, Grafana, Loki, Uptime Kuma", HasSetting("Monitoring:Enabled", "MONITORING_ENABLED") ? "CONFIGURED" : "NOT_CONFIGURED", "Compose profile with starter alert rules and dashboards")
            }
        });
    }

    private object Service(string key, string provider, string status, string detail) => new { key, provider, status, detail };

    private bool HasSetting(string configurationKey, string environmentKey) =>
        !string.IsNullOrWhiteSpace(configuration[configurationKey] ?? Environment.GetEnvironmentVariable(environmentKey));

    private bool ResolveBoolean(string configurationKey, string environmentKey) =>
        bool.TryParse(configuration[configurationKey] ?? Environment.GetEnvironmentVariable(environmentKey), out var value) && value;
}
