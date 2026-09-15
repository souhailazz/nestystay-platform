using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestyStay.Api.Auth;
using NestyStay.Application.PropertyManager;

namespace NestyStay.Api.Controllers;

[ApiController]
[Authorize(Roles = "PropertyManager,Admin")]
[Route("api/property-manager/professional")]
public sealed class PropertyManagerProfessionalController(IPropertyManagerProfessionalStore store, IResourceAuthorizationService authorization) : ControllerBase
{
    [HttpGet("owner-blocks")]
    public async Task<IActionResult> OwnerBlocks([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, [FromQuery] Guid? propertyId, CancellationToken ct) => Ok(await store.ListOwnerBlocksAsync(Actor(), from, to, propertyId, ct));
    [HttpPost("owner-blocks")]
    public async Task<IActionResult> CreateOwnerBlock(CreatePmOwnerBlockRequest request, CancellationToken ct) => Ok(await store.CreateOwnerBlockAsync(Actor(), request, ct));
    [HttpPost("owner-blocks/{id:guid}/cancel")]
    public async Task<IActionResult> CancelOwnerBlock(Guid id, CancelPmOwnerBlockRequest request, CancellationToken ct) => (await store.CancelOwnerBlockAsync(Actor(), id, request.Reason, request.RowVersion, ct)) is { } row ? Ok(row) : NotFound();
    [HttpGet("owner-blocks/{id:guid}/history")]
    public async Task<IActionResult> OwnerBlockHistory(Guid id, CancellationToken ct) => Ok(await store.ListOwnerBlockHistoryAsync(Actor(), id, ct));

    [HttpGet("reservations")]
    public async Task<IActionResult> Reservations([FromQuery] string? search, [FromQuery] string? status, [FromQuery] Guid? propertyId, [FromQuery] Guid? ownerUserId, CancellationToken ct) => Ok(await store.ListReservationsAsync(Actor(), search, status, propertyId, ownerUserId, ct));
    [HttpPatch("reservations/{bookingId:guid}")]
    public async Task<IActionResult> UpdateReservation(Guid bookingId, UpdatePmReservationRequest request, CancellationToken ct) => (await store.UpdateReservationAsync(Actor(), bookingId, request, ct)) is { } row ? Ok(row) : NotFound();
    [HttpPost("reservations/{bookingId:guid}/date-change-preview")]
    public async Task<IActionResult> PreviewReservationDateChange(Guid bookingId, PreviewPmReservationDateChangeRequest request, CancellationToken ct) => (await store.PreviewReservationDateChangeAsync(Actor(), bookingId, request, ct)) is { } row ? Ok(row) : NotFound();
    [HttpPost("reservations/{bookingId:guid}/amend")]
    public async Task<IActionResult> AmendReservation(Guid bookingId, AmendPmReservationRequest request, CancellationToken ct) => (await store.AmendReservationAsync(Actor(), bookingId, request, ct)) is { } row ? Ok(row) : NotFound();
    [HttpPost("reservations/{bookingId:guid}/cancel")]
    public async Task<IActionResult> CancelReservation(Guid bookingId, CancelPmReservationRequest request, CancellationToken ct) => (await store.CancelReservationAsync(Actor(), bookingId, request, ct)) is { } row ? Ok(row) : NotFound();
    [HttpPost("reservations/{bookingId:guid}/rebook")]
    public async Task<IActionResult> RebookReservation(Guid bookingId, RebookPmReservationRequest request, CancellationToken ct) => (await store.RebookReservationAsync(Actor(), bookingId, request, ct)) is { } row ? Ok(row) : NotFound();
    [HttpGet("reservations/{bookingId:guid}/history")]
    public async Task<IActionResult> ReservationHistory(Guid bookingId, CancellationToken ct) => Ok(await store.ListReservationHistoryAsync(Actor(), bookingId, ct));
    [HttpPost("reservations/{bookingId:guid}/notes")]
    public async Task<IActionResult> ReservationNote(Guid bookingId, AddPmReservationNoteRequest request, CancellationToken ct) => Ok(await store.AddReservationNoteAsync(Actor(), bookingId, request, ct));
    [HttpGet("calendar")]
    public async Task<IActionResult> MasterCalendar([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, [FromQuery] Guid? propertyId, [FromQuery] Guid? ownerUserId, [FromQuery] string? eventType, CancellationToken ct) => Ok(await store.ListMasterCalendarAsync(Actor(), from == default ? DateTimeOffset.UtcNow.Date : from, to == default ? DateTimeOffset.UtcNow.Date.AddDays(45) : to, propertyId, ownerUserId, eventType, ct));
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct) => Ok(await store.GetOperationalDashboardAsync(Actor(), ct));
    [HttpGet("timeline")]
    public async Task<IActionResult> Timeline([FromQuery] Guid? propertyId, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken ct) => Ok(await store.ListOperationalTimelineAsync(Actor(), propertyId, from, to, ct));

    [HttpGet("maintenance")]
    public async Task<IActionResult> Maintenance([FromQuery] string? status, [FromQuery] Guid? propertyId, CancellationToken ct) => Ok(await store.ListMaintenanceAsync(Actor(), status, propertyId, ct));
    [HttpPost("maintenance")]
    public async Task<IActionResult> CreateMaintenance(CreatePmMaintenanceRequest request, CancellationToken ct) => Ok(await store.CreateMaintenanceAsync(Actor(), request, ct));
    [HttpPatch("maintenance/{id:guid}")]
    public async Task<IActionResult> TransitionMaintenance(Guid id, TransitionPmMaintenanceRequest request, CancellationToken ct) => (await store.TransitionMaintenanceAsync(Actor(), id, request, ct)) is { } row ? Ok(row) : NotFound();
    [HttpPost("maintenance/{id:guid}/quotes")]
    public async Task<IActionResult> AddMaintenanceQuote(Guid id, AddPmMaintenanceQuoteRequest request, CancellationToken ct) => Ok(await store.AddMaintenanceQuoteAsync(Actor(), id, request, ct));
    [HttpGet("maintenance/{id:guid}/quotes")]
    public async Task<IActionResult> MaintenanceQuotes(Guid id, CancellationToken ct) => Ok(await store.ListMaintenanceQuotesAsync(Actor(), id, ct));
    [HttpGet("maintenance/{id:guid}/history")]
    public async Task<IActionResult> MaintenanceHistory(Guid id, CancellationToken ct) => Ok(await store.ListMaintenanceHistoryAsync(Actor(), id, ct));
    [HttpPost("maintenance/{id:guid}/cost-lines")]
    public async Task<IActionResult> AddMaintenanceCostLine(Guid id, AddPmCostLineRequest request, CancellationToken ct) => Ok(await store.AddMaintenanceCostLineAsync(Actor(), id, request, ct));
    [HttpGet("maintenance/{id:guid}/cost-lines")]
    public async Task<IActionResult> MaintenanceCostLines(Guid id, CancellationToken ct) => Ok(await store.ListMaintenanceCostLinesAsync(Actor(), id, ct));
    [HttpPost("maintenance/{id:guid}/financial-correction")]
    public async Task<IActionResult> CorrectMaintenanceFinancial(Guid id, CorrectPmMaintenanceFinancialRequest request, CancellationToken ct) => (await store.CorrectMaintenanceFinancialAsync(Actor(), id, request, ct)) is { } row ? Ok(row) : NotFound();

    [HttpGet("work-orders")]
    public async Task<IActionResult> WorkOrders([FromQuery] Guid? propertyId, [FromQuery] string? status, CancellationToken ct) => Ok(await store.ListWorkOrdersAsync(Actor(), propertyId, status, ct));
    [HttpPatch("work-orders/{id:guid}")]
    public async Task<IActionResult> UpdateWorkOrder(Guid id, UpdatePmProfessionalWorkOrderRequest request, CancellationToken ct) => (await store.UpdateWorkOrderAsync(Actor(), id, request, ct)) is { } row ? Ok(row) : NotFound();
    [HttpPost("work-orders/{id:guid}/financial-correction")]
    public async Task<IActionResult> CorrectWorkOrderFinancial(Guid id, CorrectPmWorkOrderFinancialRequest request, CancellationToken ct) => (await store.CorrectWorkOrderFinancialAsync(Actor(), id, request, ct)) is { } row ? Ok(row) : NotFound();
    [HttpPost("work-orders/{id:guid}/cost-lines")]
    public async Task<IActionResult> AddWorkOrderCostLine(Guid id, AddPmCostLineRequest request, CancellationToken ct) => Ok(await store.AddWorkOrderCostLineAsync(Actor(), id, request, ct));
    [HttpGet("work-orders/{id:guid}/cost-lines")]
    public async Task<IActionResult> WorkOrderCostLines(Guid id, CancellationToken ct) => Ok(await store.ListWorkOrderCostLinesAsync(Actor(), id, ct));
    [HttpPost("work-orders/{id:guid}/quotes")]
    public async Task<IActionResult> AddWorkOrderQuote(Guid id, AddPmWorkOrderQuoteRequest request, CancellationToken ct) => Ok(await store.AddWorkOrderQuoteAsync(Actor(), id, request, ct));
    [HttpGet("work-orders/{id:guid}/quotes")]
    public async Task<IActionResult> WorkOrderQuotes(Guid id, CancellationToken ct) => Ok(await store.ListWorkOrderQuotesAsync(Actor(), id, ct));

    [HttpGet("cleaning")]
    public async Task<IActionResult> Cleaning([FromQuery] Guid? propertyId, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken ct) => Ok(await store.ListCleaningAsync(Actor(), propertyId, from, to, ct));
    [HttpPost("cleaning")]
    public async Task<IActionResult> CreateCleaning(CreatePmCleaningRequest request, CancellationToken ct) => Ok(await store.CreateCleaningAsync(Actor(), request, ct));
    [HttpPatch("cleaning/{id:guid}")]
    public async Task<IActionResult> UpdateCleaning(Guid id, UpdatePmCleaningRequest request, CancellationToken ct) => (await store.UpdateCleaningAsync(Actor(), id, request, ct)) is { } row ? Ok(row) : NotFound();

    [HttpGet("assets")]
    public async Task<IActionResult> Assets([FromQuery] Guid? propertyId, [FromQuery] string? status, CancellationToken ct) => Ok(await store.ListAssetsAsync(Actor(), propertyId, status, ct));
    [HttpPost("assets")]
    public async Task<IActionResult> CreateAsset(CreatePmAssetRequest request, CancellationToken ct) => Ok(await store.CreateAssetAsync(Actor(), request, ct));
    [HttpPatch("assets/{id:guid}")]
    public async Task<IActionResult> UpdateAsset(Guid id, UpdatePmAssetRequest request, CancellationToken ct) => (await store.UpdateAssetAsync(Actor(), id, request, ct)) is { } row ? Ok(row) : NotFound();

    [HttpGet("incidents")]
    public async Task<IActionResult> Incidents([FromQuery] Guid? propertyId, [FromQuery] string? status, CancellationToken ct) => Ok(await store.ListIncidentsAsync(Actor(), propertyId, status, ct));
    [HttpPost("incidents")]
    public async Task<IActionResult> CreateIncident(CreatePmIncidentRequest request, CancellationToken ct) => Ok(await store.CreateIncidentAsync(Actor(), request, ct));
    [HttpPatch("incidents/{id:guid}")]
    public async Task<IActionResult> UpdateIncident(Guid id, UpdatePmIncidentRequest request, CancellationToken ct) => (await store.UpdateIncidentAsync(Actor(), id, request, ct)) is { } row ? Ok(row) : NotFound();

    [HttpGet("inspections")]
    public async Task<IActionResult> Inspections([FromQuery] Guid? propertyId, CancellationToken ct) => Ok(await store.ListInspectionsAsync(Actor(), propertyId, ct));
    [HttpPost("inspections")]
    public async Task<IActionResult> CreateInspection(CreatePmInspectionRequest request, CancellationToken ct) => Ok(await store.CreateInspectionAsync(Actor(), request, ct));
    [HttpPatch("inspections/{id:guid}")]
    public async Task<IActionResult> UpdateInspection(Guid id, UpdatePmInspectionRequest request, CancellationToken ct) => (await store.UpdateInspectionAsync(Actor(), id, request, ct)) is { } row ? Ok(row) : NotFound();
    [HttpPost("inspections/{id:guid}/work-order")]
    public async Task<IActionResult> CreateInspectionWorkOrder(Guid id, CreatePmInspectionWorkOrderRequest request, CancellationToken ct) => (await store.CreateInspectionWorkOrderAsync(Actor(), id, request, ct)) is { } row ? Ok(row) : NotFound();
    [HttpPost("checklist-templates")]
    public async Task<IActionResult> CreateChecklistTemplate(CreatePmChecklistTemplateRequest request, CancellationToken ct) => Ok(await store.CreateChecklistTemplateAsync(Actor(), request, ct));
    [HttpGet("checklist-templates")]
    public async Task<IActionResult> ChecklistTemplates([FromQuery] string? workflowType, CancellationToken ct) => Ok(await store.ListChecklistTemplatesAsync(Actor(), workflowType, ct));
    [HttpPut("properties/{propertyId:guid}/checklist-template")]
    public async Task<IActionResult> AssignChecklistTemplate(Guid propertyId, AssignPmChecklistTemplateRequest request, CancellationToken ct) => Ok(await store.AssignChecklistTemplateAsync(Actor(), propertyId, request, ct));
    [HttpGet("corrective-actions")]
    public async Task<IActionResult> CorrectiveActions([FromQuery] Guid? inspectionId, [FromQuery] Guid? propertyId, CancellationToken ct) => Ok(await store.ListCorrectiveActionsAsync(Actor(), inspectionId, propertyId, ct));
    [HttpPatch("corrective-actions/{id:guid}")]
    public async Task<IActionResult> UpdateCorrectiveAction(Guid id, UpdatePmCorrectiveActionRequest request, CancellationToken ct) => (await store.UpdateCorrectiveActionAsync(Actor(), id, request, ct)) is { } row ? Ok(row) : NotFound();
    [HttpPost("corrective-actions/{id:guid}/reinspection")]
    public async Task<IActionResult> CreateReinspection(Guid id, CreatePmReinspectionRequest request, CancellationToken ct) => (await store.CreateReinspectionAsync(Actor(), id, request, ct)) is { } row ? Ok(row) : NotFound();

    private Guid Actor() => authorization.RequireSignedInUser();
}

public sealed record CancelPmOwnerBlockRequest(string Reason, long RowVersion);
