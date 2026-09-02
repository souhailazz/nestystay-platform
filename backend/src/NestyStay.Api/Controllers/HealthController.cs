using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using NestyStay.Api.Auth;
using NestyStay.Application.Abstractions;
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
        var emailProvider = configuration["Email:Provider"] ?? Environment.GetEnvironmentVariable("NESTYSTAY_EMAIL_PROVIDER") ?? "file";
        var brevoEnabled = configuration["Email:Brevo:Enabled"] ?? Environment.GetEnvironmentVariable("BREVO_ENABLED");
        var brevoConfigured = !string.IsNullOrWhiteSpace(configuration["Email:Brevo:ApiKey"] ?? Environment.GetEnvironmentVariable("BREVO_API_KEY")) &&
                              !string.IsNullOrWhiteSpace(configuration["Email:Brevo:SenderEmail"] ?? Environment.GetEnvironmentVariable("BREVO_SENDER_EMAIL"));
        var webPushEnabled = ResolveBoolean("WebPush:Enabled", "WEB_PUSH_ENABLED");
        var webPushConfigured = HasSetting("WebPush:PublicKey", "VAPID_PUBLIC_KEY") &&
                                HasSetting("WebPush:PrivateKey", "VAPID_PRIVATE_KEY") &&
                                HasSetting("WebPush:Subject", "VAPID_SUBJECT");
        var pendingEmailCount = 0;
        var failedEmailCount = 0;
        try
        {
            pendingEmailCount = await db.NotificationQueue.CountAsync(item =>
                item.Channel == "Email" && (item.DeliveryStatus == "PENDING" || item.DeliveryStatus == "PROCESSING" || item.DeliveryStatus == "RETRYING"), cancellationToken);
            failedEmailCount = await db.NotificationQueue.CountAsync(item =>
                item.Channel == "Email" && (item.DeliveryStatus == "FAILED" || item.DeliveryStatus == "DEAD_LETTER"), cancellationToken);
        }
        catch (Exception)
        {
            // Integration status must remain useful even while PostgreSQL is being recovered.
            pendingEmailCount = -1;
            failedEmailCount = -1;
        }

        var emailStatus = emailProvider.Equals("brevo", StringComparison.OrdinalIgnoreCase) && !string.Equals(brevoEnabled, "false", StringComparison.OrdinalIgnoreCase)
            ? (brevoConfigured ? "CONFIGURED" : "BLOCKED_CREDENTIAL")
            : "LOCAL_CAPTURE";
        var emailDetail = $"Transactional email transport; queue pending={pendingEmailCount}, failed={failedEmailCount}";

        return Ok(new
        {
            generatedAt = DateTimeOffset.UtcNow,
            services = new[]
            {
                Service("payments", paymentGateway.ProviderName, HasSetting("Integrations:StripeSecretKey", "STRIPE_SECRET_KEY") ? "CONFIGURED" : "BLOCKED_CREDENTIAL", "Stripe payment adapter"),
                Service("identity", ekycProvider.ProviderName, HasSetting("Integrations:AlibabaEkycTransactionUrlBase", "ALIBABA_EKYC_TRANSACTION_URL_BASE") ? "CONFIGURED" : "BLOCKED_CREDENTIAL", "Alibaba eKYC adapter"),
                Service("email", emailProvider.Equals("brevo", StringComparison.OrdinalIgnoreCase) && !string.Equals(brevoEnabled, "false", StringComparison.OrdinalIgnoreCase) ? "Brevo transactional email" : "Local file capture", emailStatus, emailDetail),
                Service("storage", storageProvider.ProviderName, storageProvider.ProviderName.Contains("Local", StringComparison.OrdinalIgnoreCase) ? "SELF_HOSTED" : "CONFIGURED", "Private API-authorized object storage"),
                Service("postgres", "PostgreSQL", HasSetting("ConnectionStrings:Postgres", "ConnectionStrings__Postgres") ? "CONFIGURED" : "BLOCKED_INFRASTRUCTURE", "Application database connection"),
                Service("redis", "Self-hosted Redis", HasSetting("ConnectionStrings:Redis", "ConnectionStrings__Redis") ? "CONFIGURED" : "OPTIONAL_NOT_CONNECTED", "Reserved for distributed coordination before scale-out"),
                Service("webPush", "Provider-neutral Web Push", webPushEnabled ? (webPushConfigured ? "CONFIGURED" : "BLOCKED_CREDENTIAL") : "OPTIONAL_DISABLED", webPushEnabled ? "VAPID configuration is present; browser subscriptions remain opt-in" : "Optional channel disabled; in-app and email remain available"),
                Service("worker", "Self-hosted background worker", ResolveBoolean("Worker:SidecarEnabled", "WORKER_SIDECAR_ENABLED") || ResolveBoolean("Worker:Enabled", "WORKER_ENABLED") || ResolveBoolean("BackgroundJobs:Enabled", "BACKGROUND_JOBS_ENABLED") ? "CONFIGURED" : "OPTIONAL_NOT_CONNECTED", "Worker sidecar is deployed separately; heartbeat is not exposed through the API"),
                Service("backups", "pg_dump + object archive + optional restic", HasSetting("Backup:Repository", "RESTIC_REPOSITORY") ? "CONFIGURED" : "BLOCKED_OPERATIONAL", "Off-server encrypted destination is required before production")
            }
        });
    }

    private object Service(string key, string provider, string status, string detail) => new { key, provider, status, detail };

    private bool HasSetting(string configurationKey, string environmentKey) =>
        !string.IsNullOrWhiteSpace(configuration[configurationKey] ?? Environment.GetEnvironmentVariable(environmentKey));

    private bool ResolveBoolean(string configurationKey, string environmentKey) =>
        bool.TryParse(configuration[configurationKey] ?? Environment.GetEnvironmentVariable(environmentKey), out var value) && value;
}
