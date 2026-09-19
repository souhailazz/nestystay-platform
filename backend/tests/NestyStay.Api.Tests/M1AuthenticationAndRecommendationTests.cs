using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using NestyStay.Domain;

namespace NestyStay.Api.Tests;

public sealed class M1AuthenticationAndRecommendationTests : IClassFixture<NestyStayApiFactory>
{
    private readonly NestyStayApiFactory factory;

    public M1AuthenticationAndRecommendationTests(NestyStayApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task SessionManagementKeepsCurrentDeviceWhenOtherSessionsAreRevoked()
    {
        using var client = factory.CreateClient();
        var email = $"sessions-{Guid.NewGuid():N}@test.local";
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Password123!",
            displayName = "Session Guest",
            confirmPassword = "Password123!",
            acceptedTerms = true,
            acceptedPrivacy = true,
            role = "Guest"
        });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        var first = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!", deviceName = "Laptop", rememberDevice = true });
        var firstBody = await first.Content.ReadFromJsonAsync<LoginBody>();
        Assert.NotNull(firstBody?.AccessToken);
        var second = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!", deviceName = "Phone", rememberDevice = false });
        var secondBody = await second.Content.ReadFromJsonAsync<LoginBody>();
        Assert.NotNull(secondBody?.AccessToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secondBody!.AccessToken);
        var sessions = await client.GetFromJsonAsync<List<SessionBody>>("/api/auth/sessions");
        Assert.NotNull(sessions);
        Assert.True(sessions.Count >= 2);
        Assert.Contains(sessions, item => item.IsCurrent && !item.IsRevoked);

        var revoke = await client.PostAsJsonAsync("/api/auth/sessions/revoke-others", new { });
        Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);
        var remaining = await client.GetFromJsonAsync<List<SessionBody>>("/api/auth/sessions");
        Assert.NotNull(remaining);
        Assert.Single(remaining!, item => !item.IsRevoked);
        Assert.Contains(remaining, item => item.IsCurrent && !item.IsRevoked);
    }

    [Fact]
    public async Task AvailabilityAndRecommendationsUsePersistedApiState()
    {
        using var client = factory.CreateClient();
        var hostId = Guid.NewGuid();
        var guestId = Guid.NewGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(hostId, UserRole.Host));
        var propertyResponse = await client.PostAsJsonAsync("/api/properties", new
        {
            hostUserId = hostId,
            hostName = "Availability Host",
            hostEmail = $"availability-{hostId:N}@test.local",
            title = "Persisted Availability Stay",
            location = "Kingston",
            country = "Jamaica",
            nightlyRate = 155,
            currency = "USD",
            badgeLevel = "Verified",
            cancellationPolicy = "Flexible"
        });
        Assert.Equal(HttpStatusCode.OK, propertyResponse.StatusCode);
        var propertyId = (await propertyResponse.Content.ReadFromJsonAsync<IdBody>())!.Id;

        client.DefaultRequestHeaders.Authorization = null;
        var availability = await client.GetFromJsonAsync<AvailabilityBody>($"/api/properties/{propertyId}/availability?from=2026-09-10&to=2026-09-13");
        Assert.NotNull(availability);
        Assert.Equal(3, availability!.Days.Count);
        Assert.All(availability.Days, day => Assert.False(string.IsNullOrWhiteSpace(day.Status)));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.UserToken(guestId, UserRole.Guest));
        var recommendations = await client.GetAsync($"/api/spec/traveler/{guestId}/recommendations");
        Assert.Equal(HttpStatusCode.OK, recommendations.StatusCode);
        var initial = await recommendations.Content.ReadFromJsonAsync<List<RecommendationBody>>();
        Assert.NotNull(initial);
        Assert.NotEmpty(initial!);
        var target = initial[0].PropertyId;

        var dismiss = await client.PostAsJsonAsync($"/api/spec/traveler/{guestId}/recommendations/{target}/dismiss", new { });
        Assert.Equal(HttpStatusCode.OK, dismiss.StatusCode);
        var afterDismiss = await client.GetFromJsonAsync<List<RecommendationBody>>($"/api/spec/traveler/{guestId}/recommendations");
        Assert.DoesNotContain(afterDismiss!, item => item.PropertyId == target);
    }

    private sealed record LoginBody(Guid UserId, string Email, bool RequiresTwoFactor, string? AccessToken);
    private sealed record SessionBody(Guid Id, string DeviceName, bool IsCurrent, bool IsTrusted, bool IsRevoked);
    private sealed record IdBody(Guid Id);
    private sealed record AvailabilityBody(Guid PropertyId, string From, string To, List<AvailabilityDayBody> Days);
    private sealed record AvailabilityDayBody(string Date, string Status, string Source, string? Label);
    private sealed record RecommendationBody(Guid PropertyId, string PropertyTitle, int Score, bool IsDismissed);
}
