using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NestyStay.Api.Middleware;
using NestyStay.Application.PhaseOne;
using NestyStay.Domain;

namespace NestyStay.Api.Tests;

public sealed class ApiExceptionMiddlewareTests
{
    [Fact]
    public async Task RateLimitExceptionMapsToStableProblemDetails()
    {
        var context = await InvokeAsync(new RateLimitExceededException(
            "Too many booking requests. Try again in 2 minutes.",
            TimeSpan.FromSeconds(93)));

        Assert.Equal(HttpStatusCode.TooManyRequests, (HttpStatusCode)context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.Equal("93", context.Response.Headers["Retry-After"]);

        var problem = ReadProblem(context);
        Assert.Equal("rate_limit_exceeded", problem.RootElement.GetProperty("code").GetString());
        Assert.Equal(429, problem.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task InvalidOperationWithRateLimitLikeMessageStillMapsToBadRequest()
    {
        var context = await InvokeAsync(new InvalidOperationException("Too many booking requests. Try again later."));

        Assert.Equal(HttpStatusCode.BadRequest, (HttpStatusCode)context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.False(context.Response.Headers.ContainsKey("Retry-After"));

        var problem = ReadProblem(context);
        Assert.False(problem.RootElement.TryGetProperty("code", out _));
        Assert.Equal(400, problem.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task BookingStateConflictMapsToConflictProblemDetails()
    {
        var context = await InvokeAsync(new BookingStateConflictException(
            "Payment cannot move from Captured to Failed.",
            "stripe_webhook",
            BookingStatus.PaymentCaptured,
            PaymentStatus.Captured));

        Assert.Equal(HttpStatusCode.Conflict, (HttpStatusCode)context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);

        var problem = ReadProblem(context);
        Assert.Equal("booking_state_conflict", problem.RootElement.GetProperty("code").GetString());
        Assert.Equal("stripe_webhook", problem.RootElement.GetProperty("operation").GetString());
        Assert.Equal("PaymentCaptured", problem.RootElement.GetProperty("currentBookingStatus").GetString());
        Assert.Equal("Captured", problem.RootElement.GetProperty("currentPaymentStatus").GetString());
        Assert.Equal(409, problem.RootElement.GetProperty("status").GetInt32());
    }

    private static async Task<DefaultHttpContext> InvokeAsync(Exception exception)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ApiExceptionMiddleware(
            _ => throw exception,
            NullLogger<ApiExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);
        return context;
    }

    private static JsonDocument ReadProblem(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        return JsonDocument.Parse(context.Response.Body);
    }
}
