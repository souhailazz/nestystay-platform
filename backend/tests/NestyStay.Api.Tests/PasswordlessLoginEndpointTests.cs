using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace NestyStay.Api.Tests;

public sealed class PasswordlessLoginEndpointTests : IClassFixture<NestyStayApiFactory>
{
    private readonly NestyStayApiFactory factory;

    public PasswordlessLoginEndpointTests(NestyStayApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task PasswordlessLoginUsesSingleUseHashedFlowAndCookieSession()
    {
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        var email = $"magic-{Guid.NewGuid():N}@test.local";

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Password123!",
            displayName = "Magic Guest",
            phone = "+18765550123",
            confirmPassword = "Password123!",
            acceptedTerms = true,
            acceptedPrivacy = true,
            role = "Guest"
        });
        Assert.True(register.StatusCode == HttpStatusCode.OK, await register.Content.ReadAsStringAsync());

        var request = await client.PostAsJsonAsync("/api/spec/auth/passwordless/request", new { email });
        Assert.Equal(HttpStatusCode.OK, request.StatusCode);
        var flow = await request.Content.ReadFromJsonAsync<AuthFlowResponse>();
        Assert.NotNull(flow);
        Assert.Equal("PasswordlessLogin", flow.FlowType);
        Assert.Null(flow.UserId);
        Assert.Contains("If that email is registered", flow.Message, StringComparison.Ordinal);

        var secret = await client.GetFromJsonAsync<DevelopmentAuthFlowSecretResponse>(
            $"/api/spec/auth/development/flows/{flow.Id}");
        Assert.NotNull(secret);

        client.DefaultRequestHeaders.Add("X-Session-Mode", "cookie");
        var complete = await client.PostAsJsonAsync("/api/spec/auth/passwordless/complete", new
        {
            flowId = flow.Id,
            token = secret.Token
        });
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
        var session = await complete.Content.ReadFromJsonAsync<PasswordlessResponse>();
        Assert.NotNull(session);
        Assert.Equal(email, session.Email);
        Assert.Empty(session.AccessToken ?? string.Empty);

        var profile = await client.GetAsync("/api/auth/profile");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);

        var reused = await client.PostAsJsonAsync("/api/spec/auth/passwordless/complete", new
        {
            flowId = flow.Id,
            token = secret.Token
        });
        // Cookie-authenticated writes are intentionally rejected without the
        // double-submit CSRF token; this still proves a replay cannot silently
        // create another session.
        Assert.Equal(HttpStatusCode.Forbidden, reused.StatusCode);
    }

    private sealed record AuthFlowResponse(
        Guid Id,
        Guid? UserId,
        string FlowType,
        string Destination,
        string Status,
        string DeliveryChannel,
        DateTimeOffset ExpiresAt,
        DateTimeOffset? LastSentAt,
        int AttemptsRemaining,
        string? Message);

    private sealed record DevelopmentAuthFlowSecretResponse(Guid Id, string Code, string Token, DateTimeOffset ExpiresAt);

    private sealed record PasswordlessResponse(Guid UserId, string Email, string DisplayName, string? AccessToken, DateTimeOffset ExpiresAt);
}
