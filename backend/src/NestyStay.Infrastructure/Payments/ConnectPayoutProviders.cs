using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NestyStay.Application.Abstractions;

namespace NestyStay.Infrastructure.Payments;

/// <summary>
/// Deterministic local payout state machine. It never contacts Stripe and is
/// only selected in Development/Testing. Set PAYOUT_LOCAL_SCENARIO to
/// paid, pending, or failed to exercise the corresponding persisted state.
/// </summary>
internal sealed class LocalConnectPayoutProvider(IConfiguration configuration, IHostEnvironment environment) : IConnectPayoutProvider
{
    public string ProviderName => "Local deterministic Stripe Connect";

    public Task<ConnectAccountResult> EnsureConnectedAccountAsync(ConnectAccountRequest request, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment() && !environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The local Connect payout adapter is available only in Development or Testing.");
        }

        var status = (configuration["Payout:LocalAccountScenario"] ??
                      Environment.GetEnvironmentVariable("PAYOUT_LOCAL_ACCOUNT_SCENARIO") ??
                      "ready").Trim().ToLowerInvariant() switch
        {
            "ready" or "payout_ready" or "connected" => ("PayoutReady", true, (string?)null),
            "onboarding" or "pending" => ("Onboarding", false, (string?)null),
            "failed" => ("Failed", false, "local deterministic account onboarding failure"),
            _ => throw new InvalidOperationException("PAYOUT_LOCAL_ACCOUNT_SCENARIO must be ready, onboarding, or failed.")
        };
        var reference = $"acct_local_{request.RecipientId:N}";
        return Task.FromResult(new ConnectAccountResult(ProviderName, reference, status.Item1, status.Item2, status.Item3));
    }

    public Task<ConnectTransferResult> CreateTransferAsync(ConnectTransferRequest request, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment() && !environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The local Connect payout adapter is available only in Development or Testing.");
        }

        if (request.Amount <= 0 || string.IsNullOrWhiteSpace(request.Currency))
        {
            throw new InvalidOperationException("A positive payout amount and currency are required.");
        }

        var scenario = (configuration["Payout:LocalScenario"] ??
                        Environment.GetEnvironmentVariable("PAYOUT_LOCAL_SCENARIO") ??
                        "paid").Trim().ToLowerInvariant();
        var status = scenario switch
        {
            "pending" or "processing" => "Pending",
            "failed" or "failure" => "Failed",
            "disputed" or "dispute" => "Disputed",
            "paid" or "succeeded" or "success" => "Paid",
            _ => throw new InvalidOperationException("PAYOUT_LOCAL_SCENARIO must be paid, pending, or failed.")
        };

        var key = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? request.PayoutId.ToString("N")
            : request.IdempotencyKey.Trim();
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)))[..24].ToLowerInvariant();
        var reference = $"stripe_connect_local_{digest}";
        return Task.FromResult(new ConnectTransferResult(
            ProviderName,
            reference,
            status,
            decimal.Round(request.Amount, 2, MidpointRounding.AwayFromZero),
            request.Currency.Trim().ToUpperInvariant(),
            status == "Failed" ? "local deterministic payout failure" : null));
    }
}

/// <summary>
/// Production Stripe Connect transfer adapter. It is selected only when
/// PAYOUT_MODE=stripe_connect and a real Stripe secret is configured.
/// </summary>
internal sealed class StripeConnectPayoutProvider(HttpClient httpClient, IConfiguration configuration) : IConnectPayoutProvider
{
    public string ProviderName => "Stripe Connect";

    public async Task<ConnectAccountResult> EnsureConnectedAccountAsync(ConnectAccountRequest request, CancellationToken cancellationToken)
    {
        var secretKey = configuration["Integrations:StripeSecretKey"] ?? Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException("Stripe Connect requires STRIPE_SECRET_KEY.");
        }

        var payload = new Dictionary<string, string>
        {
            ["type"] = "express",
            ["metadata[recipient_id]"] = request.RecipientId.ToString("N")
        };
        if (!string.IsNullOrWhiteSpace(request.DisplayName)) payload["business_profile[name]"] = request.DisplayName.Trim();
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/accounts") { Content = new FormUrlEncodedContent(payload) };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{secretKey}:")));
        httpRequest.Headers.TryAddWithoutValidation("Idempotency-Key", $"connect-account-{request.RecipientId:N}");
        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Stripe Connect account creation failed with HTTP {(int)response.StatusCode}.");
        using var document = JsonDocument.Parse(body);
        var reference = document.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null;
        if (string.IsNullOrWhiteSpace(reference)) throw new InvalidOperationException("Stripe Connect did not return an account id.");
        var payoutsEnabled = document.RootElement.TryGetProperty("payouts_enabled", out var enabled) && enabled.ValueKind == JsonValueKind.True;
        return new ConnectAccountResult(ProviderName, reference, payoutsEnabled ? "PayoutReady" : "Onboarding", payoutsEnabled);
    }

    public async Task<ConnectTransferResult> CreateTransferAsync(ConnectTransferRequest request, CancellationToken cancellationToken)
    {
        var secretKey = configuration["Integrations:StripeSecretKey"] ?? Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException("Stripe Connect requires STRIPE_SECRET_KEY.");
        }

        if (string.IsNullOrWhiteSpace(request.DestinationAccountId))
        {
            throw new InvalidOperationException("Stripe Connect transfer requires a verified connected-account destination.");
        }

        var payload = new Dictionary<string, string>
        {
            ["amount"] = decimal.ToInt64(decimal.Round(request.Amount * 100m, 0, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture),
            ["currency"] = request.Currency.Trim().ToLowerInvariant(),
            ["destination"] = request.DestinationAccountId,
            ["metadata[payout_id]"] = request.PayoutId.ToString("N"),
            ["metadata[recipient_id]"] = request.RecipientId.ToString("N")
        };
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/transfers")
        {
            Content = new FormUrlEncodedContent(payload)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.ASCII.GetBytes($"{secretKey}:")));
        httpRequest.Headers.TryAddWithoutValidation("Idempotency-Key", request.IdempotencyKey);

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Stripe Connect transfer failed with HTTP {(int)response.StatusCode}.");
        }

        using var document = JsonDocument.Parse(body);
        var reference = document.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null;
        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new InvalidOperationException("Stripe Connect did not return a transfer id.");
        }

        return new ConnectTransferResult(ProviderName, reference, "Paid", request.Amount, request.Currency);
    }
}

/// <summary>
/// Manual production mode preserves the existing finance workflow while
/// keeping the application behind the same provider-neutral seam.
/// </summary>
internal sealed class ManualConnectPayoutProvider(IHostEnvironment environment) : IConnectPayoutProvider
{
    public string ProviderName => "Manual payout settlement";

    public Task<ConnectAccountResult> EnsureConnectedAccountAsync(ConnectAccountRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new ConnectAccountResult(ProviderName, $"manual_account_{request.RecipientId:N}", "PayoutReady", true));

    public Task<ConnectTransferResult> CreateTransferAsync(ConnectTransferRequest request, CancellationToken cancellationToken)
    {
        if (environment.IsDevelopment() || environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new ConnectTransferResult(
                ProviderName,
                $"manual_local_{request.PayoutId:N}",
                "Paid",
                request.Amount,
                request.Currency));
        }

        throw new InvalidOperationException("Manual payout settlement must be completed through the approved finance workflow.");
    }
}
