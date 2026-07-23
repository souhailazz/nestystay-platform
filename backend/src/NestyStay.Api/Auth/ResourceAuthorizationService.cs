using NestyStay.Application.PhaseOne;
using NestyStay.Domain;

namespace NestyStay.Api.Auth;

public sealed class ResourceAuthorizationService(
    CurrentUserContext currentUser,
    IHttpContextAccessor httpContextAccessor,
    IPhaseOneStore phaseOneStore) : IResourceAuthorizationService
{
    public Guid RequireSignedInUser(string? message = null) =>
        currentUser.UserId ?? throw new UnauthorizedAccessException(message ?? "A signed session bearer token is required.");

    public Guid RequireResourceOwner(Guid expectedUserId)
    {
        var actual = RequireSignedInUser();
        if (actual != expectedUserId)
        {
            throw new UnauthorizedAccessException("The bearer token does not match this resource owner.");
        }

        return actual;
    }

    public Guid RequireHost()
    {
        var userId = RequireSignedInUser("Authenticated host id is required.");
        if (!IsInRole(UserRole.Host))
        {
            throw new ForbiddenAccessException("Host role is required.");
        }

        return userId;
    }

    public Guid RequireHostOwner(Guid expectedHostUserId)
    {
        var userId = RequireResourceOwner(expectedHostUserId);
        if (!IsInRole(UserRole.Host))
        {
            throw new ForbiddenAccessException("Host role is required.");
        }

        return userId;
    }

    public bool HostOwnsProperty(Guid hostUserId, Guid propertyId) =>
        phaseOneStore.GetProperty(propertyId) is { } property && property.HostUserId == hostUserId;

    public bool CanAccessBooking(BookingDto booking)
    {
        if (IsInRole(UserRole.Admin))
        {
            return true;
        }

        var userId = RequireSignedInUser("Authenticated user id is required.");
        return booking.GuestUserId == userId ||
            (IsInRole(UserRole.Host) && booking.HostUserId == userId);
    }

    public bool CanCaptureBooking(BookingDto booking)
    {
        if (IsInRole(UserRole.Admin))
        {
            return true;
        }

        var userId = TryGetSignedInUser();
        return userId == booking.HostUserId && IsInRole(UserRole.Host);
    }

    public bool IsInRole(UserRole role) =>
        httpContextAccessor.HttpContext?.User.IsInRole(role.ToString()) == true;

    public Guid? TryGetSignedInUser() => currentUser.UserId;
}
