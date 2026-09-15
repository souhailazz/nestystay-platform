using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace NestyStay.Api.Configuration;

public static class RateLimitPolicies
{
    public const string Authentication = "Authentication";
    public const string PublicWrite = "PublicWrite";
    public const string SensitiveAction = "SensitiveAction";
    public const string Upload = "Upload";

    public static IServiceCollection AddNestyStayRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds))
                            .ToString(CultureInfo.InvariantCulture);
                }

                context.HttpContext.Response.ContentType = "application/problem+json";
                await context.HttpContext.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    title = "Too many requests. Please wait before trying again.",
                    status = StatusCodes.Status429TooManyRequests,
                    code = "RATE_LIMIT_EXCEEDED",
                    traceId = context.HttpContext.TraceIdentifier
                }), cancellationToken);
            };

            // This limiter is IP-partitioned before authentication. Keep enough
            // headroom for shared networks while account lockout handles targeted
            // credential guessing.
            AddFixedWindowPolicy(options, configuration, Authentication, 120, TimeSpan.FromMinutes(1));
            AddFixedWindowPolicy(options, configuration, PublicWrite, 60, TimeSpan.FromMinutes(1));
            AddFixedWindowPolicy(options, configuration, SensitiveAction, 120, TimeSpan.FromMinutes(1));
            AddFixedWindowPolicy(options, configuration, Upload, 30, TimeSpan.FromMinutes(1));
        });

        return services;
    }

    private static void AddFixedWindowPolicy(
        RateLimiterOptions options,
        IConfiguration configuration,
        string policyName,
        int defaultPermitLimit,
        TimeSpan window)
    {
        var permitLimit = Math.Max(
            1,
            configuration.GetValue<int?>("RateLimiting:" + policyName + ":PermitLimit") ?? defaultPermitLimit);

        options.AddPolicy(policyName, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                PartitionKey(context),
                _ => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = permitLimit,
                    QueueLimit = 0,
                    Window = window
                }));
    }

    private static string PartitionKey(HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            return "user:" + userId;
        }

        return "ip:" + (context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
    }
}
