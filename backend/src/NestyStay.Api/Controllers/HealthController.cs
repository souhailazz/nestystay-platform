using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestyStay.Api.Auth;
using NestyStay.Application.Abstractions;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController(
    IConfiguration configuration,
    IPaymentGateway paymentGateway,
    IEkycProvider ekycProvider,
    IStorageProvider storageProvider) : ControllerBase
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

    [Authorize(Policy = AdminAuthorizationPolicies.SystemConfiguration)]
    [HttpGet("integrations")]
    public IActionResult GetIntegrations()
    {
        var emailProvider = configuration["Email:Provider"] ?? Environment.GetEnvironmentVariable("NESTYSTAY_EMAIL_PROVIDER") ?? "file";
        var brevoEnabled = configuration["Email:Brevo:Enabled"] ?? Environment.GetEnvironmentVariable("BREVO_ENABLED");
        var brevoConfigured = !string.IsNullOrWhiteSpace(configuration["Email:Brevo:ApiKey"] ?? Environment.GetEnvironmentVariable("BREVO_API_KEY")) &&
                              !string.IsNullOrWhiteSpace(configuration["Email:Brevo:SenderEmail"] ?? Environment.GetEnvironmentVariable("BREVO_SENDER_EMAIL"));

        return Ok(new
        {
            generatedAt = DateTimeOffset.UtcNow,
            services = new[]
            {
                Service("payments", paymentGateway.ProviderName, HasSetting("Integrations:StripeSecretKey", "STRIPE_SECRET_KEY") ? "CONFIGURED" : "BLOCKED_CREDENTIAL", "Stripe payment adapter"),
                Service("identity", ekycProvider.ProviderName, HasSetting("Integrations:AlibabaEkycTransactionUrlBase", "ALIBABA_EKYC_TRANSACTION_URL_BASE") ? "CONFIGURED" : "BLOCKED_CREDENTIAL", "Alibaba eKYC adapter"),
                Service("email", emailProvider.Equals("brevo", StringComparison.OrdinalIgnoreCase) && !string.Equals(brevoEnabled, "false", StringComparison.OrdinalIgnoreCase) ? "Brevo transactional email" : "Local file capture", emailProvider.Equals("brevo", StringComparison.OrdinalIgnoreCase) && !string.Equals(brevoEnabled, "false", StringComparison.OrdinalIgnoreCase) ? (brevoConfigured ? "CONFIGURED" : "BLOCKED_CREDENTIAL") : "LOCAL_CAPTURE", "Transactional email transport"),
                Service("storage", storageProvider.ProviderName, storageProvider.ProviderName.Contains("Local", StringComparison.OrdinalIgnoreCase) ? "SELF_HOSTED" : "CONFIGURED", "Private API-authorized object storage"),
                Service("postgres", "PostgreSQL", HasSetting("ConnectionStrings:Postgres", "ConnectionStrings__Postgres") ? "CONFIGURED" : "BLOCKED_INFRASTRUCTURE", "Application database connection"),
                Service("redis", "Self-hosted Redis", HasSetting("ConnectionStrings:Redis", "ConnectionStrings__Redis") ? "CONFIGURED" : "OPTIONAL_NOT_CONNECTED", "Reserved for distributed coordination before scale-out")
            }
        });
    }

    private object Service(string key, string provider, string status, string detail) => new { key, provider, status, detail };

    private bool HasSetting(string configurationKey, string environmentKey) =>
        !string.IsNullOrWhiteSpace(configuration[configurationKey] ?? Environment.GetEnvironmentVariable(environmentKey));
}
