using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestyStay.Api.Auth;
using NestyStay.Application.PropertyManager;

namespace NestyStay.Api.Controllers;

/// <summary>Owner-side operational controls.  Only the authenticated owner can see or change their own blocks.</summary>
[ApiController]
[Authorize(Roles = "Owner")]
[Route("api/property-manager/owner")]
public sealed class PropertyManagerOwnerOperationsController(IPropertyManagerProfessionalStore store, IResourceAuthorizationService authorization) : ControllerBase
{
    [HttpGet("owner-blocks")]
    public async Task<IActionResult> OwnerBlocks([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken ct) => Ok(await store.ListOwnerBlocksForOwnerAsync(Actor(), from, to, ct));

    [HttpPost("owner-blocks")]
    public async Task<IActionResult> CreateOwnerBlock(CreatePmOwnerBlockRequest request, CancellationToken ct) => Ok(await store.CreateOwnerBlockForOwnerAsync(Actor(), request, ct));

    [HttpPost("owner-blocks/{id:guid}/cancel")]
    public async Task<IActionResult> CancelOwnerBlock(Guid id, CancelPmOwnerBlockRequest request, CancellationToken ct) => (await store.CancelOwnerBlockForOwnerAsync(Actor(), id, request.Reason, request.RowVersion, ct)) is { } row ? Ok(row) : NotFound();

    private Guid Actor() => authorization.RequireSignedInUser();
}
