using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestyStay.Api.Auth;
using NestyStay.Application.Access;
using NestyStay.Domain;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/access")]
public sealed class AccessController(
    IQrAccessStore qrAccessStore,
    IResourceAuthorizationService authorization) : ControllerBase
{
    [Authorize]
    [HttpPost("qr/bookings/{bookingId:guid}")]
    public async Task<ActionResult<QrIssueResult>> Issue(Guid bookingId, CancellationToken cancellationToken)
    {
        var actor = authorization.RequireSignedInUser();
        return Ok(await qrAccessStore.IssueForBookingAsync(bookingId, actor, cancellationToken));
    }

    [Authorize]
    [HttpGet("qr/{qrId:guid}")]
    public async Task<ActionResult<QrAccessDto>> Get(Guid qrId, CancellationToken cancellationToken)
    {
        var actor = authorization.RequireSignedInUser();
        return await qrAccessStore.GetAsync(qrId, actor, cancellationToken) is { } access ? Ok(access) : NotFound();
    }

    [Authorize]
    [HttpPost("qr/{qrId:guid}/revoke")]
    public async Task<ActionResult<QrAccessDto>> Revoke(Guid qrId, CancellationToken cancellationToken)
    {
        var actor = authorization.RequireSignedInUser();
        return await qrAccessStore.RevokeAsync(qrId, actor, cancellationToken) is { } access ? Ok(access) : NotFound();
    }

    // Gate-facing validation deliberately returns no guest name, email, or private contact data.
    [AllowAnonymous]
    [HttpPost("qr/validate")]
    public async Task<ActionResult<QrValidationResult>> Validate(ValidateQrRequest request, CancellationToken cancellationToken) =>
        Ok(await qrAccessStore.ValidateAsync(request.Token, request.PropertyId, request.DeviceMetadata, cancellationToken));

    public sealed record ValidateQrRequest(string Token, Guid PropertyId, string? DeviceMetadata = null);
}
