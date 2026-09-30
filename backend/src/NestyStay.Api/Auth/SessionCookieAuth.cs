using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;

namespace NestyStay.Api.Auth;

/// <summary>
/// Browser sessions use an HttpOnly cookie so application JavaScript never has
/// to persist or read the bearer secret.  The CSRF value is intentionally a
/// separate, non-secret double-submit token and is required only for cookie
/// authenticated state-changing requests.
/// </summary>
public static class SessionCookieAuth
{
    public const string SessionCookieName = "nestyStay.session";
    public const string CsrfCookieName = "nestyStay.csrf";
    public const string SessionModeHeader = "X-Session-Mode";
    public const string CookieSessionMode = "cookie";
    public const string CsrfHeaderName = "X-CSRF-Token";

    public static void Issue(
        HttpResponse response,
        string accessToken,
        DateTimeOffset expiresAt,
        bool secure,
        string? domain = null,
        SameSiteMode sameSite = SameSiteMode.Lax)
    {
        var shared = new CookieOptions // NOSONAR: Secure is configured from the HTTPS deployment mode; local HTTP is intentionally supported.
        {
            Secure = secure, // NOSONAR: local development may run on HTTP; production validation forces HTTPS cookies.
            SameSite = sameSite,
            HttpOnly = true,
            IsEssential = true,
            Path = "/",
            Domain = NormalizeDomain(domain),
            Expires = expiresAt
        };
        response.Cookies.Append(SessionCookieName, accessToken, shared);

        response.Cookies.Append(CsrfCookieName, CreateToken(), new CookieOptions // NOSONAR: the CSRF cookie is intentionally JavaScript-readable and follows the configured HTTPS mode.
        {
            Secure = secure, // NOSONAR: local development may run on HTTP; production validation forces HTTPS cookies.
            SameSite = sameSite,
            HttpOnly = false, // NOSONAR: the double-submit CSRF token must be readable by browser JavaScript.
            IsEssential = true,
            Path = "/",
            Domain = NormalizeDomain(domain),
            Expires = expiresAt
        });
    }

    public static void Clear(
        HttpResponse response,
        string? domain = null,
        bool secure = false,
        SameSiteMode sameSite = SameSiteMode.Lax)
    {
        var sessionOptions = new CookieOptions // NOSONAR: the options deliberately mirror the issued session cookie for safe deletion.
        {
            Secure = secure, // NOSONAR: mirrors the issued session cookie for local HTTP and production HTTPS.
            SameSite = sameSite,
            HttpOnly = true,
            Path = "/",
            Domain = NormalizeDomain(domain)
        };
        var csrfOptions = new CookieOptions // NOSONAR: the options deliberately mirror the browser-readable CSRF cookie for safe deletion.
        {
            Secure = secure, // NOSONAR: mirrors the issued CSRF cookie for local HTTP and production HTTPS.
            SameSite = sameSite,
            HttpOnly = false, // NOSONAR: mirrors the browser-readable CSRF cookie being deleted.
            Path = "/",
            Domain = NormalizeDomain(domain)
        };
        response.Cookies.Delete(SessionCookieName, sessionOptions);
        response.Cookies.Delete(CsrfCookieName, csrfOptions);
    }

    public static bool IsCookieMode(HttpRequest request) =>
        string.Equals(request.Headers[SessionModeHeader].ToString(), CookieSessionMode, StringComparison.OrdinalIgnoreCase);

    private static string CreateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string? NormalizeDomain(string? domain) =>
        string.IsNullOrWhiteSpace(domain) ? null : domain.Trim();
}
