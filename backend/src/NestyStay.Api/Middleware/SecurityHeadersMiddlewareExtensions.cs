using Microsoft.Extensions.Primitives;

namespace NestyStay.Api.Middleware;

public static class SecurityHeadersMiddlewareExtensions
{
    private const string ContentSecurityPolicy = "default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'none'; form-action 'self'; script-src 'self' https://js.stripe.com; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; font-src 'self' data:; connect-src 'self' https://api.stripe.com https://r.stripe.com; frame-src https://js.stripe.com https://hooks.stripe.com; upgrade-insecure-requests";

    public static IApplicationBuilder UseProductionSecurityHeaders(this IApplicationBuilder app, IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            return app;
        }

        return app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers["Content-Security-Policy"] = new StringValues(ContentSecurityPolicy);
                headers["Strict-Transport-Security"] = new StringValues("max-age=31536000; includeSubDomains");
                headers["X-Content-Type-Options"] = new StringValues("nosniff");
                headers["Referrer-Policy"] = new StringValues("strict-origin-when-cross-origin");
                headers["Permissions-Policy"] = new StringValues("camera=(), microphone=(), geolocation=(), usb=()");
                headers["X-Frame-Options"] = new StringValues("DENY");
                return Task.CompletedTask;
            });

            await next();
        });
    }
}
