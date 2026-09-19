using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using NestyStay.Application.Abstractions;
using NestyStay.Domain;
using NestyStay.Infrastructure;

namespace NestyStay.Infrastructure.Tests;

public sealed class StripeIdentityProviderTests
{
    [Fact]
    public async Task SelectedStripeIdentityProviderCreatesAndChecksDeterministicDevelopmentSession()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Integrations:EkycProvider"] = "stripe_identity",
                ["PublicAppUrl"] = "https://staging.example.test",
                ["Integrations:StripeIdentityLocalResult"] = "verified"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment(Environments.Staging));
        services.AddInfrastructureServices(backgroundJobsEnabled: false);

        await using var provider = services.BuildServiceProvider();
        var ekyc = provider.GetRequiredService<IEkycProvider>();
        var resultProvider = provider.GetRequiredService<IEkycResultProvider>();
        var bookingId = Guid.NewGuid().ToString("N");

        var started = await ekyc.StartCheckAsync(
            new EkycStartRequest(Guid.NewGuid().ToString("N"), UserRole.Guest, bookingId, null, "GLB03002", null),
            CancellationToken.None);
        var checkedResult = await resultProvider.CheckResultAsync(
            new EkycCheckRequest(bookingId, started.TransactionId),
            CancellationToken.None);

        Assert.Equal("Stripe Identity", ekyc.ProviderName);
        Assert.Equal("Stripe Identity", started.ProviderName);
        Assert.Equal(VerificationStatus.Pending, started.Status);
        Assert.StartsWith("https://identity.stripe.local/verify?", started.TransactionUrl);
        Assert.Equal(VerificationStatus.Passed, checkedResult.Status);
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "NestyStay.Infrastructure.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
