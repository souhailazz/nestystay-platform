using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestyStay.Api.Auth;
using NestyStay.Application.PropertyManager;

namespace NestyStay.Api.Controllers;

/// <summary>Unified local PMS operations for the remaining professional modules.</summary>
[ApiController]
[Authorize]
[Route("api/property-manager/professional-completion")]
public sealed class PropertyManagerProfessionalCompletionController(IPropertyManagerProfessionalCompletionStore store, IResourceAuthorizationService authorization) : ControllerBase
{
    [HttpGet("{area}")]
    public async Task<IActionResult> List(string area, [FromQuery] Guid? propertyId, [FromQuery] Guid? ownerUserId, [FromQuery] string? status, [FromQuery] string? search, CancellationToken cancellationToken) => Ok(await store.ListAsync(Actor(), area, propertyId, ownerUserId, status, search, cancellationToken));

    [HttpPost("{area}")]
    public async Task<IActionResult> Create(string area, ProfessionalRecordRequest request, CancellationToken cancellationToken) => Ok(await store.CreateAsync(Actor(), area, request, cancellationToken));

    [HttpPut("{area}/{id:guid}")]
    public async Task<IActionResult> Update(string area, Guid id, ProfessionalRecordRequest request, CancellationToken cancellationToken) => (await store.UpdateAsync(Actor(), area, id, request, cancellationToken)) is { } result ? Ok(result) : NotFound();

    [HttpGet("records/{id:guid}/history")]
    public async Task<IActionResult> History(Guid id, CancellationToken cancellationToken) => Ok(await store.HistoryAsync(Actor(), id, cancellationToken));

    [HttpGet("reports")]
    public async Task<IActionResult> Report([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] Guid? propertyId, [FromQuery] Guid? ownerUserId, CancellationToken cancellationToken) => Ok(await store.ReportAsync(Actor(), from ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)), to ?? DateOnly.FromDateTime(DateTime.UtcNow), propertyId, ownerUserId, cancellationToken));

    private Guid Actor() => authorization.RequireSignedInUser();
}
