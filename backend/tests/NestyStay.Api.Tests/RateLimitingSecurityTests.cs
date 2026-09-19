using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace NestyStay.Api.Tests;

public sealed class RateLimitingSecurityTests : IClassFixture<NestyStayApiFactory>
{
    private readonly NestyStayApiFactory _factory;

    public RateLimitingSecurityTests(NestyStayApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AuthenticationPolicyRejectsRequestsAboveConfiguredIpLimit()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration(configuration =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimiting:Authentication:PermitLimit"] = "3"
                })));
        using var client = factory.CreateClient();

        for (var index = 0; index < 3; index++)
        {
            var allowed = await client.PostAsJsonAsync("/api/auth/login", new
            {
                email = $"unknown-{index}@test.local",
                password = "InvalidPassword1"
            });
            Assert.NotEqual(HttpStatusCode.TooManyRequests, allowed.StatusCode);
        }

        var blocked = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "unknown-final@test.local",
            password = "InvalidPassword1"
        });

        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.Equal("application/problem+json", blocked.Content.Headers.ContentType?.MediaType);
        Assert.True(blocked.Headers.Contains("Retry-After"));
        Assert.Contains("RATE_LIMIT_EXCEEDED", await blocked.Content.ReadAsStringAsync());
    }
}
