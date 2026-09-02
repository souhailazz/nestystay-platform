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

    public static void Issue(HttpResponse response, string accessToken, DateTimeOffset expiresAt, bool secure)
    {
        var shared = new CookieOptions
        {
            Secure = secure,
            SameSite = SameSiteMode.Lax,
            HttpOnly = true,
            IsEssential = true,
            Path = "/",
            Expires = expiresAt
        };
        response.Cookies.Append(SessionCookieName, accessToken, shared);

        response.Cookies.Append(CsrfCookieName, CreateToken(), new CookieOptions
        {
            Secure = secure,
            SameSite = SameSiteMode.Strict,
            HttpOnly = false,
            IsEssential = true,
            Path = "/",
            Expires = expiresAt
        });
    }

    public static void Clear(HttpResponse response)
    {
        response.Cookies.Delete(SessionCookieName, new CookieOptions { Path = "/" });
        response.Cookies.Delete(CsrfCookieName, new CookieOptions { Path = "/" });
    }

    public static bool IsCookieMode(HttpRequest request) =>
        string.Equals(request.Headers[SessionModeHeader].ToString(), CookieSessionMode, StringComparison.OrdinalIgnoreCase);

    private static string CreateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
