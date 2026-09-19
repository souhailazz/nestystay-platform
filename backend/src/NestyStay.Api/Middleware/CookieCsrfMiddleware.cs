using System.Security.Cryptography;
using System.Text;
using NestyStay.Api.Auth;

namespace NestyStay.Api.Middleware;

/// <summary>
/// Double-submit CSRF protection for the HttpOnly browser session boundary.
/// Bearer API clients remain backwards compatible and are not subject to this
/// browser-cookie check because they explicitly provide an Authorization
/// header.
/// </summary>
public sealed class CookieCsrfMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Get,
        HttpMethods.Head,
        HttpMethods.Options,
        HttpMethods.Trace
    };

    public async Task InvokeAsync(HttpContext context)
    {
        if (!SafeMethods.Contains(context.Request.Method) &&
            context.Request.Cookies.ContainsKey(SessionCookieAuth.SessionCookieName) &&
            string.IsNullOrWhiteSpace(context.Request.Headers.Authorization.ToString()))
        {
            var expected = context.Request.Cookies[SessionCookieAuth.CsrfCookieName];
            var supplied = context.Request.Headers[SessionCookieAuth.CsrfHeaderName].ToString();
            if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(supplied) || !FixedTimeEquals(expected, supplied))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(new
                {
                    type = "https://httpstatuses.com/403",
                    title = "CSRF validation failed",
                    status = StatusCodes.Status403Forbidden,
                    detail = "A valid CSRF token is required for cookie-authenticated changes."
                });
                return;
            }
        }

        await next(context);
    }

    private static bool FixedTimeEquals(string first, string second)
    {
        var firstBytes = Encoding.UTF8.GetBytes(first);
        var secondBytes = Encoding.UTF8.GetBytes(second);
        return firstBytes.Length == secondBytes.Length && CryptographicOperations.FixedTimeEquals(firstBytes, secondBytes);
    }
}
