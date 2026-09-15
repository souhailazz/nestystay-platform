using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using NestyStay.Application.Abstractions;
using NestyStay.Infrastructure;

namespace NestyStay.Infrastructure.Tests;

public sealed class ConnectPayoutProviderTests
{
    [Theory]
    [InlineData("paid", "Paid", null)]
    [InlineData("pending", "Pending", null)]
    [InlineData("failed", "Failed", "local deterministic payout failure")]
    [InlineData("disputed", "Disputed", null)]
    public async Task LocalProviderPersistsDeterministicTransferState(string scenario, string expectedStatus, string? expectedFailure)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Payout:LocalScenario"] = scenario,
                ["Payout:LocalAccountScenario"] = "ready"
            })
            .Build();
        using var provider = BuildProvider(configuration, "Testing");
        var payout = provider.GetRequiredService<IConnectPayoutProvider>();
        var payoutId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();

        var account = await payout.EnsureConnectedAccountAsync(new ConnectAccountRequest(recipientId, "Local Host"), CancellationToken.None);
        var request = new ConnectTransferRequest(payoutId, recipientId, 125.456m, "usd", "payout-idempotency-key");
        var result = await payout.CreateTransferAsync(request, CancellationToken.None);
        var retry = await payout.CreateTransferAsync(request with { Amount = 125.45m }, CancellationToken.None);

        Assert.Equal("PayoutReady", account.Status);
        Assert.True(account.PayoutsEnabled);
        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(expectedFailure, result.FailureReason);
        Assert.Equal("USD", result.Currency);
        Assert.Equal(125.46m, result.Amount);
        Assert.Equal(result.TransferReference, retry.TransferReference);
    }

    [Theory]
    [InlineData("onboarding", "Onboarding", false)]
    [InlineData("failed", "Failed", false)]
    public async Task LocalProviderExposesAccountReadinessScenarios(string scenario, string expectedStatus, bool payoutsEnabled)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Payout:LocalAccountScenario"] = scenario
            })
            .Build();
        using var provider = BuildProvider(configuration, Environments.Development);
        var payout = provider.GetRequiredService<IConnectPayoutProvider>();

        var result = await payout.EnsureConnectedAccountAsync(new ConnectAccountRequest(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(payoutsEnabled, result.PayoutsEnabled);
    }

    private static ServiceProvider BuildProvider(IConfiguration configuration, string environmentName)
    {
        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment(environmentName));
        services.AddInfrastructureServices(backgroundJobsEnabled: false);
        return services.BuildServiceProvider();
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "NestyStay.Infrastructure.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
