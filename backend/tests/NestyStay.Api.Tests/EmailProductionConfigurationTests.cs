using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NestyStay.Api.Configuration;

namespace NestyStay.Api.Tests;

public sealed class EmailProductionConfigurationTests
{
    [Fact]
    public void BrevoModeRequiresSenderCredentials()
    {
        var configuration = BaseConfiguration(new Dictionary<string, string?>
        {
            ["Email:Provider"] = "brevo"
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionIntegrationValidator.Validate(configuration, new TestHostEnvironment(Environments.Production)));

        Assert.Contains("Brevo", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FileModeSupportsSelfHostedLocalDelivery()
    {
        var configuration = BaseConfiguration(new Dictionary<string, string?>
        {
            ["Email:Provider"] = "file"
        });

        ProductionIntegrationValidator.Validate(configuration, new TestHostEnvironment(Environments.Production));
    }

    [Fact]
    public void BrevoCanBeDisabledForControlledLocalCapture()
    {
        var configuration = BaseConfiguration(new Dictionary<string, string?>
        {
            ["Email:Provider"] = "brevo",
            ["Email:Brevo:Enabled"] = "false"
        });

        ProductionIntegrationValidator.Validate(configuration, new TestHostEnvironment(Environments.Production));
    }

    private static IConfiguration BaseConfiguration(Dictionary<string, string?> overrides)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = "Host=prod-db;Database=nesty_prod;Username=nesty_app;Password=strong-production-db-password",
            ["Security:SessionTokenSecret"] = "strong-production-session-token-secret-32-plus",
            ["Security:TotpSecretProtectionKey"] = "strong-production-totp-protection-key-32-plus",
            ["Webhooks:SharedSecret"] = "production-webhook-shared-secret",
            ["Webhooks:StripeSigningSecret"] = "production-stripe-webhook-secret",
            ["Integrations:StripeSecretKey"] = "production-stripe-secret-key",
            ["Integrations:StripePublishableKey"] = "production-stripe-publishable-key",
            ["Integrations:AlibabaCloudAccessKeyId"] = "ram-access-key-id",
            ["Integrations:AlibabaCloudAccessKeySecret"] = "ram-access-key-secret",
            ["Integrations:AlibabaEkycRegion"] = "ap-southeast-1",
            ["Integrations:AlibabaEkycEndpoint"] = "cloudauth-intl.ap-southeast-1.aliyuncs.com",
            ["Integrations:AlibabaEkycProductCode"] = "eKYC_PRO",
            ["Integrations:AlibabaEkycSceneCode"] = "NESTYWEB",
            ["Integrations:AlibabaEkycCallbackUrl"] = "https://api.example.test/api/webhooks/alibaba-ekyc/callback",
            ["Integrations:AlibabaEkycReturnUrl"] = "https://app.example.test/ekyc/complete",
            ["Integrations:AlibabaEkycCallbackToken"] = "callback-token",
            ["Integrations:InsuraGuestApiBaseUrl"] = "https://insurance.provider.invalid"
        };
        foreach (var (key, value) in overrides) values[key] = value;
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "NestyStay.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(AppContext.BaseDirectory);
    }
}
