using NestyStay.Application.PhaseOne;
using NestyStay.Domain;

namespace NestyStay.Api.Auth;

public interface IResourceAuthorizationService
{
    Guid RequireSignedInUser(string? message = null);
    Guid RequireResourceOwner(Guid expectedUserId);
    Guid RequireHost();
    Guid RequireHostOwner(Guid expectedHostUserId);
    bool HostOwnsProperty(Guid hostUserId, Guid propertyId);
    bool CanAccessBooking(BookingDto booking);
    bool CanCaptureBooking(BookingDto booking);
    bool IsInRole(UserRole role);
    Guid? TryGetSignedInUser();
}
