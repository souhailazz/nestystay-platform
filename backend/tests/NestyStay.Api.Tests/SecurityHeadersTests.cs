using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace NestyStay.Api.Tests;

public sealed class SecurityHeadersTests : IClassFixture<NestyStayApiFactory>
{
    private readonly NestyStayApiFactory _factory;

    public SecurityHeadersTests(NestyStayApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task NonDevelopmentResponsesContainStrictSecurityHeaders()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var csp = SingleHeader(response, "Content-Security-Policy");
        Assert.Contains("default-src 'self'", csp);
        Assert.Contains("frame-ancestors 'none'", csp);
        Assert.Contains("https://js.stripe.com", csp);
        Assert.Contains("https://api.stripe.com", csp);
        Assert.DoesNotContain("unsafe-eval", csp, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("*", csp, StringComparison.Ordinal);
        Assert.DoesNotContain("https:", csp.Split([' ', ';'], StringSplitOptions.RemoveEmptyEntries));

        Assert.Equal("max-age=31536000; includeSubDomains", SingleHeader(response, "Strict-Transport-Security"));
        Assert.Equal("nosniff", SingleHeader(response, "X-Content-Type-Options"));
        Assert.Equal("strict-origin-when-cross-origin", SingleHeader(response, "Referrer-Policy"));
        Assert.Equal("camera=(), microphone=(), geolocation=(), usb=()", SingleHeader(response, "Permissions-Policy"));
        Assert.Equal("DENY", SingleHeader(response, "X-Frame-Options"));
    }

    [Fact]
    public async Task DevelopmentKeepsOpenApiUsableWithoutProductionHeaders()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Content-Security-Policy"));
        Assert.False(response.Headers.Contains("Strict-Transport-Security"));
    }

    private static string SingleHeader(HttpResponseMessage response, string name)
    {
        Assert.True(response.Headers.TryGetValues(name, out var values), $"Missing {name} header.");
        return Assert.Single(values);
    }
}
