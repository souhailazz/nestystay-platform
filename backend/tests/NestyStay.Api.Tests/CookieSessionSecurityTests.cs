using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using NestyStay.Application.PhaseOne;

namespace NestyStay.Api.Tests;

public sealed class CookieSessionSecurityTests : IClassFixture<NestyStayApiFactory>
{
    private readonly NestyStayApiFactory factory;

    public CookieSessionSecurityTests(NestyStayApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task BrowserModeLoginIssuesHttpOnlySessionAndDoesNotReturnBearerSecret()
    {
        using var client = factory.CreateClient();
        var email = await RegisterAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email, password = "Password123!" })
        };
        request.Headers.Add("X-Session-Mode", "cookie");
        var response = await client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.TryGetProperty("accessToken", out var accessToken));
        Assert.True(accessToken.ValueKind == JsonValueKind.Null || (accessToken.ValueKind == JsonValueKind.String && string.IsNullOrEmpty(accessToken.GetString())));
        var setCookies = response.Headers.GetValues("Set-Cookie").ToArray();
        var sessionCookie = Assert.Single(setCookies, cookie => cookie.StartsWith("nestyStay.session=", StringComparison.Ordinal));
        Assert.Contains("httponly", sessionCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(setCookies, cookie => cookie.StartsWith("nestyStay.csrf=", StringComparison.Ordinal) && !cookie.Contains("HttpOnly", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CookieSessionRequiresCsrfForChangesAndAcceptsValidDoubleSubmitToken()
    {
        using var client = factory.CreateClient();
        var email = await RegisterAsync(client);
        var cookies = await BrowserLoginCookiesAsync(client, email);
        client.DefaultRequestHeaders.Add("Cookie", cookies.Header);

        var profile = await client.GetAsync("/api/auth/profile");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);

        var missingCsrf = await client.PatchAsJsonAsync("/api/auth/profile", new { displayName = "Blocked change", phone = (string?)null });
        Assert.Equal(HttpStatusCode.Forbidden, missingCsrf.StatusCode);

        client.DefaultRequestHeaders.Add("X-CSRF-Token", cookies.Csrf);
        var allowed = await client.PatchAsJsonAsync("/api/auth/profile", new { displayName = "Cookie session", phone = (string?)null });
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task CookieLogoutClearsBrowserCookiesAndInvalidatesTheServerSession()
    {
        using var client = factory.CreateClient();
        var email = await RegisterAsync(client);
        var cookies = await BrowserLoginCookiesAsync(client, email);
        client.DefaultRequestHeaders.Add("Cookie", cookies.Header);
        client.DefaultRequestHeaders.Add("X-CSRF-Token", cookies.Csrf);
        using var secondTab = factory.CreateClient();
        secondTab.DefaultRequestHeaders.Add("Cookie", cookies.Header);
        secondTab.DefaultRequestHeaders.Add("X-CSRF-Token", cookies.Csrf);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await secondTab.GetAsync("/api/auth/profile")).StatusCode);
        var logout = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        Assert.Contains(logout.Headers.GetValues("Set-Cookie"), value => value.StartsWith("nestyStay.session=", StringComparison.Ordinal) && value.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await secondTab.GetAsync("/api/auth/profile")).StatusCode);
    }

    [Fact]
    public async Task CookieModeCompletesTwoFactorChallengeWithoutReturningBearerSecret()
    {
        using var client = factory.CreateClient();
        var email = await RegisterAsync(client);

        var directLogin = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });
        Assert.Equal(HttpStatusCode.OK, directLogin.StatusCode);
        var directBody = await directLogin.Content.ReadFromJsonAsync<LoginBody>();
        Assert.NotNull(directBody);
        Assert.False(string.IsNullOrWhiteSpace(directBody.AccessToken));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", directBody.AccessToken);

        var enrollmentResponse = await client.PostAsync("/api/auth/2fa/enrollments", null);
        Assert.Equal(HttpStatusCode.OK, enrollmentResponse.StatusCode);
        var enrollment = await enrollmentResponse.Content.ReadFromJsonAsync<EnrollmentBody>();
        Assert.NotNull(enrollment);
        var confirm = await client.PostAsJsonAsync("/api/auth/2fa/enrollments/confirm", new
        {
            enrollmentId = enrollment.EnrollmentId,
            code = GenerateTotp(enrollment.ManualKey, DateTimeOffset.UtcNow.AddSeconds(-30))
        });
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        client.DefaultRequestHeaders.Authorization = null;

        using var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email, password = "Password123!" })
        };
        loginRequest.Headers.Add("X-Session-Mode", "cookie");
        var challengeResponse = await client.SendAsync(loginRequest);
        var challenge = await challengeResponse.Content.ReadFromJsonAsync<LoginBody>();
        Assert.Equal(HttpStatusCode.OK, challengeResponse.StatusCode);
        Assert.NotNull(challenge);
        Assert.True(challenge.RequiresTwoFactor);
        Assert.False(string.IsNullOrWhiteSpace(challenge.ChallengeId));
        var devCode = await client.GetFromJsonAsync<DevelopmentAuthCodeResponse>($"/api/auth/development/challenges/{challenge.ChallengeId}");
        Assert.NotNull(devCode);

        using var verifyRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/2fa/verify")
        {
            Content = JsonContent.Create(new { challengeId = challenge.ChallengeId, code = devCode.Code })
        };
        verifyRequest.Headers.Add("X-Session-Mode", "cookie");
        var verifyResponse = await client.SendAsync(verifyRequest);
        var verifyPayload = await verifyResponse.Content.ReadAsStringAsync();
        var verifyBody = JsonSerializer.Deserialize<JsonElement>(verifyPayload);
        Assert.True(verifyResponse.StatusCode == HttpStatusCode.OK, verifyPayload);
        var accessToken = verifyBody.GetProperty("accessToken");
        Assert.True(accessToken.ValueKind == JsonValueKind.Null || (accessToken.ValueKind == JsonValueKind.String && string.IsNullOrEmpty(accessToken.GetString())));
        var cookies = verifyResponse.Headers.GetValues("Set-Cookie").Select(value => value.Split(';', 2)[0]).ToArray();
        client.DefaultRequestHeaders.Add("Cookie", string.Join("; ", cookies));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/profile")).StatusCode);
    }

    private static async Task<string> RegisterAsync(HttpClient client)
    {
        var email = $"cookie-{Guid.NewGuid():N}@test.local";
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Password123!",
            confirmPassword = "Password123!",
            displayName = "Cookie Session",
            acceptedTerms = true,
            acceptedPrivacy = true,
            role = "Guest"
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return email;
    }

    private static async Task<(string Header, string Csrf)> BrowserLoginCookiesAsync(HttpClient client, string email)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email, password = "Password123!" })
        };
        request.Headers.Add("X-Session-Mode", "cookie");
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookies = response.Headers.GetValues("Set-Cookie").Select(value => value.Split(';', 2)[0]).ToArray();
        var csrf = cookies.Single(value => value.StartsWith("nestyStay.csrf=", StringComparison.Ordinal))["nestyStay.csrf=".Length..];
        return (string.Join("; ", cookies), csrf);
    }

    private static string GenerateTotp(string manualKey, DateTimeOffset? timestamp = null)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bits = new List<bool>();
        foreach (var character in manualKey.TrimEnd('=').ToUpperInvariant())
        {
            var value = alphabet.IndexOf(character);
            for (var bit = 4; bit >= 0; bit--) bits.Add((value & (1 << bit)) != 0);
        }
        var secret = new byte[bits.Count / 8];
        for (var i = 0; i < secret.Length; i++)
            for (var bit = 0; bit < 8; bit++)
                if (bits[i * 8 + bit]) secret[i] |= (byte)(1 << (7 - bit));

        var counter = BitConverter.GetBytes((timestamp ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds() / 30);
        if (BitConverter.IsLittleEndian) Array.Reverse(counter);
        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(counter);
        var offset = hash[^1] & 0x0f;
        var valueCode = ((hash[offset] & 0x7f) << 24) | ((hash[offset + 1] & 0xff) << 16) | ((hash[offset + 2] & 0xff) << 8) | (hash[offset + 3] & 0xff);
        return (valueCode % 1_000_000).ToString("D6");
    }

    private sealed record LoginBody(Guid UserId, string Email, bool RequiresTwoFactor, string? ChallengeId, string? AccessToken);
    private sealed record EnrollmentBody(string EnrollmentId, string ManualKey, string OtpAuthUri, DateTimeOffset ExpiresAt);
}
