using System.Security.Claims;

namespace NestyStay.Api.Auth;

public sealed class CurrentUserContext(IHttpContextAccessor httpContextAccessor)
{
    public Guid? UserId =>
        Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : null;

    public string? SessionTokenId =>
        httpContextAccessor.HttpContext?.User.FindFirst("nesty_session_id")?.Value;
}
