using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NestyStay.Api.Auth;
using NestyStay.Application.PhaseOne;

namespace NestyStay.Api.Middleware;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (RateLimitExceededException exception)
        {
            logger.LogWarning(exception, "API rate limit exceeded");
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "application/problem+json";
            if (exception.RetryAfterSeconds is { } retryAfterSeconds)
            {
                context.Response.Headers.RetryAfter = retryAfterSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                title = exception.Message,
                status = context.Response.StatusCode,
                code = exception.Code,
                traceId = context.TraceIdentifier
            }));
        }
        catch (BookingStateConflictException exception)
        {
            logger.LogWarning(exception, "API booking state conflict");
            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                title = exception.Message,
                status = context.Response.StatusCode,
                code = exception.Code,
                operation = exception.Operation,
                currentBookingStatus = exception.CurrentBookingStatus?.ToString(),
                currentPaymentStatus = exception.CurrentPaymentStatus?.ToString(),
                traceId = context.TraceIdentifier
            }));
        }
        catch (DbUpdateConcurrencyException exception)
        {
            logger.LogWarning(exception, "API concurrency conflict");
            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                title = "The record changed while you were working. Reload and try again.",
                status = context.Response.StatusCode,
                code = "CONCURRENCY_CONFLICT",
                traceId = context.TraceIdentifier
            }));
        }
        catch (DbUpdateException exception)
        {
            // Unique indexes intentionally enforce idempotency and one-time
            // state transitions.  Do not leak provider SQL or constraint
            // names; clients receive a retryable conflict instead.
            logger.LogWarning(exception, "API persistence conflict");
            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                title = "The operation conflicts with an existing record. Retry with the original request or reload.",
                status = context.Response.StatusCode,
                code = "PERSISTENCE_CONFLICT",
                traceId = context.TraceIdentifier
            }));
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "API validation error");
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                title = exception.Message,
                status = context.Response.StatusCode,
                traceId = context.TraceIdentifier
            }));
        }
        catch (UnauthorizedAccessException exception)
        {
            logger.LogWarning(exception, "API authorization error");
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                title = exception.Message,
                status = context.Response.StatusCode,
                traceId = context.TraceIdentifier
            }));
        }
        catch (ForbiddenAccessException exception)
        {
            logger.LogWarning(exception, "API forbidden error");
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                title = exception.Message,
                status = context.Response.StatusCode,
                traceId = context.TraceIdentifier
            }));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled API exception");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                title = "Unexpected backend error",
                status = context.Response.StatusCode,
                traceId = context.TraceIdentifier
            }));
        }
    }
}
