using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using NestyStay.Domain;

namespace NestyStay.Api.Tests;

public sealed class InsuranceEndpointTests : IClassFixture<NestyStayApiFactory>
{
    private readonly NestyStayApiFactory _factory;

    public InsuranceEndpointTests(NestyStayApiFactory factory) => _factory = factory;

    [Fact]
    public async Task PlansExposeTheThreeSignedContractOptions()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/insurance/plans");
        var plans = await response.Content.ReadFromJsonAsync<List<PlanResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(plans);
        Assert.Collection(plans,
            plan => { Assert.Equal("non-us-50", plan.Code); Assert.Equal(50m, plan.MonthlyAmount); Assert.Equal(10000m, plan.PropertyDamageCoverage); Assert.Equal(0m, plan.AccidentalMedicalCoverage); },
            plan => { Assert.Equal("us-69", plan.Code); Assert.Equal(69m, plan.MonthlyAmount); Assert.Equal(10000m, plan.PropertyDamageCoverage); Assert.Equal(10000m, plan.AccidentalMedicalCoverage); },
            plan => { Assert.Equal("us-99", plan.Code); Assert.Equal(99m, plan.MonthlyAmount); Assert.Equal(25000m, plan.PropertyDamageCoverage); Assert.Equal(25000m, plan.AccidentalMedicalCoverage); });
    }

    [Fact]
    public async Task HostActivationIsIdempotentAndPersistsLifecycleHistory()
    {
        using var client = _factory.CreateClient();
        var host = await RegisterHostAsync(client);
        var property = await CreatePropertyAsync(client, host);
        client.DefaultRequestHeaders.Authorization = Bearer(host.AccessToken);
        var request = new { planCode = "non-us-50", idempotencyKey = $"insurance-{Guid.NewGuid():N}" };

        var first = await client.PostAsJsonAsync($"/api/insurance/properties/{property.Id}/policy", request);
        var replay = await client.PostAsJsonAsync($"/api/insurance/properties/{property.Id}/policy", request);
        var state = await client.GetAsync($"/api/insurance/properties/{property.Id}");
        var events = await client.GetAsync($"/api/insurance/properties/{property.Id}/policy/events");
        var body = await first.Content.ReadFromJsonAsync<PolicyResponse>();
        var replayBody = await replay.Content.ReadFromJsonAsync<PolicyResponse>();
        var propertyBody = await state.Content.ReadFromJsonAsync<PropertyInsuranceResponse>();
        var eventBody = await events.Content.ReadFromJsonAsync<List<PolicyEventResponse>>();

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.NotNull(body);
        Assert.NotNull(replayBody);
        Assert.Equal(body.Id, replayBody.Id);
        Assert.Equal("ACTIVE", body.Status);
        Assert.True(propertyBody!.CoverageFlag);
        Assert.Equal("ACTIVE", propertyBody.Status);
        Assert.NotNull(eventBody);
        Assert.Equal(new[] { "PLAN_SELECTED", "PENDING", "ACTIVE" }, eventBody.Select(item => item.ToStatus).ToArray());
    }

    [Fact]
    public async Task HostCanCancelAndRenewOwnPolicyButAnotherHostCannotReadIt()
    {
        using var client = _factory.CreateClient();
        var host = await RegisterHostAsync(client);
        var property = await CreatePropertyAsync(client, host);
        client.DefaultRequestHeaders.Authorization = Bearer(host.AccessToken);
        var key = $"insurance-{Guid.NewGuid():N}";
        var activated = await client.PostAsJsonAsync($"/api/insurance/properties/{property.Id}/policy", new { planCode = "us-69", idempotencyKey = key });
        var renewalDue = await client.PostAsync($"/api/insurance/properties/{property.Id}/policy/renewal-due", null);
        var renewed = await client.PostAsJsonAsync($"/api/insurance/properties/{property.Id}/policy/renew", new { idempotencyKey = $"renew-{Guid.NewGuid():N}" });
        var cancelled = await client.PostAsJsonAsync($"/api/insurance/properties/{property.Id}/policy/cancel", new { idempotencyKey = $"cancel-{Guid.NewGuid():N}" });
        var otherHost = await RegisterHostAsync(client);
        client.DefaultRequestHeaders.Authorization = Bearer(otherHost.AccessToken);
        var forbidden = await client.GetAsync($"/api/insurance/properties/{property.Id}");
        var forbiddenMutation = await client.PostAsJsonAsync($"/api/insurance/properties/{property.Id}/policy", new { planCode = "non-us-50", idempotencyKey = $"id-or-{Guid.NewGuid():N}" });

        Assert.Equal(HttpStatusCode.OK, activated.StatusCode);
        Assert.Equal(HttpStatusCode.OK, renewalDue.StatusCode);
        Assert.Equal(HttpStatusCode.OK, renewed.StatusCode);
        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenMutation.StatusCode);
    }

    [Fact]
    public async Task ClaimsRequireActivePolicyAndTheSeventyTwoHourWindow()
    {
        using var client = _factory.CreateClient();
        var host = await RegisterHostAsync(client);
        var property = await CreatePropertyAsync(client, host);
        client.DefaultRequestHeaders.Authorization = Bearer(host.AccessToken);
        var activate = await client.PostAsJsonAsync($"/api/insurance/properties/{property.Id}/policy", new { planCode = "non-us-50", idempotencyKey = $"insurance-{Guid.NewGuid():N}" });
        var tooOld = await client.PostAsJsonAsync($"/api/insurance/properties/{property.Id}/claims", new { incidentAt = DateTimeOffset.UtcNow.AddHours(-73), description = "Old incident" });
        var valid = await client.PostAsJsonAsync($"/api/insurance/properties/{property.Id}/claims", new { incidentAt = DateTimeOffset.UtcNow.AddHours(-2), description = "Broken window" });
        var claims = await client.GetAsync($"/api/insurance/properties/{property.Id}/claims");

        Assert.Equal(HttpStatusCode.OK, activate.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tooOld.StatusCode);
        Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
        Assert.Contains("SUBMITTED", await claims.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task LocalProviderPersistsPendingToFailedOutcome()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration(configuration =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Insurance:LocalActivationOutcome"] = "FAILED"
                })));
        using var client = factory.CreateClient();
        var host = await RegisterHostAsync(client);
        var property = await CreatePropertyAsync(client, host);
        client.DefaultRequestHeaders.Authorization = Bearer(host.AccessToken);

        var activation = await client.PostAsJsonAsync(
            $"/api/insurance/properties/{property.Id}/policy",
            new { planCode = "us-99", idempotencyKey = $"insurance-failure-{Guid.NewGuid():N}" });
        var propertyState = await client.GetAsync($"/api/insurance/properties/{property.Id}");
        var events = await client.GetAsync($"/api/insurance/properties/{property.Id}/policy/events");
        var body = await activation.Content.ReadFromJsonAsync<PolicyResponse>();
        var stateBody = await propertyState.Content.ReadFromJsonAsync<PropertyInsuranceResponse>();
        var eventBody = await events.Content.ReadFromJsonAsync<List<PolicyEventResponse>>();

        Assert.Equal(HttpStatusCode.OK, activation.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("FAILED", body.Status);
        Assert.False(stateBody!.CoverageFlag);
        Assert.Equal("FAILED", stateBody.Status);
        Assert.NotNull(eventBody);
        Assert.Equal("FAILED", eventBody[^1].ToStatus);
    }

    private async Task<HostSession> RegisterHostAsync(HttpClient client)
    {
        var email = $"insurance-host-{Guid.NewGuid():N}@test.local";
        var register = await client.PostAsJsonAsync("/api/auth/register", new { email, password = "Password123!", confirmPassword = "Password123!", displayName = "Insurance Host", acceptedTerms = true, acceptedPrivacy = true, role = "Host" });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var session = await login.Content.ReadFromJsonAsync<HostSession>();
        Assert.NotNull(session);
        return session!;
    }

    private static async Task<PropertyResponse> CreatePropertyAsync(HttpClient client, HostSession host)
    {
        client.DefaultRequestHeaders.Authorization = Bearer(host.AccessToken);
        var response = await client.PostAsJsonAsync("/api/properties", new { hostUserId = host.UserId, hostName = "Insurance Host", hostEmail = "insurance-host@test.local", title = "Insurance Test Villa", location = "Kingston, Jamaica", country = "Jamaica", nightlyRate = 150m, currency = "USD", badgeLevel = "Free", guestVerificationEnabled = false, insuraGuestEnabled = false, cancellationPolicy = "Flexible" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PropertyResponse>())!;
    }

    private static AuthenticationHeaderValue Bearer(string token) => new("Bearer", token);

    private sealed record HostSession(Guid UserId, string AccessToken, bool RequiresTwoFactor);
    private sealed record PropertyResponse(Guid Id);
    private sealed record PlanResponse(string Code, decimal MonthlyAmount, decimal PropertyDamageCoverage, decimal AccidentalMedicalCoverage);
    private sealed record PolicyResponse(Guid Id, string Status);
    private sealed record PropertyInsuranceResponse(Guid PropertyId, string PropertyTitle, bool CoverageFlag, string Status, PolicyResponse? Policy);
    private sealed record PolicyEventResponse(Guid Id, Guid PolicyId, Guid PropertyId, string FromStatus, string ToStatus, string Reason, string? IdempotencyKey, DateTimeOffset CreatedAt);
}
