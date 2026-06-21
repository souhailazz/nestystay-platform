using Microsoft.AspNetCore.Mvc;
using NestyStay.Api.Contracts;
using NestyStay.Domain.Common;
using NestyStay.Infrastructure.Persistence;

namespace NestyStay.Api.Controllers;

[ApiController]
[Route("api/backend-schema")]
public sealed class BackendSchemaController : ControllerBase
{
    [HttpGet("tables")]
    public IActionResult GetTables() => Ok(SchemaCatalog.Tables);

    [HttpGet("rules")]
    public IActionResult GetRules() => Ok(new BackendRuleDto[]
    {
        new("Bookings", $"Default booking hold is {NestyStayBusinessRules.DefaultBookingHoldMinutes} minutes; legacy PDF {NestyStayBusinessRules.LegacyPdfBookingHoldMinutes} minutes remains configurable."),
        new("Bookings", "Payment capture is allowed only after booking approval."),
        new("Bookings", "Entry details and QR access are released only after full payment."),
        new("Pricing", "All fees, badge prices, founding tiers, commissions, and conflicting PDF/HTML rates live in the pricebook."),
        new("Badges", "Verification opt-out removes reviews, badges, directories, search boost, police wellness, and guest platform access."),
        new("Directories", "Custodians and trades require sponsorship unless continuous verified rating is at least 4.7."),
        new("Wellness", $"Guest-facing wellness language is '{NestyStayBusinessRules.GuestFacingWellnessLabel()}', never police."),
        new("Wellness", $"Jamaica emergency number {NestyStayBusinessRules.JamaicaEmergencyNumber} must display on Jamaica listings."),
        new("PropertyManager", "Managers are hard-blocked from unit rental income."),
        new("Association", $"Meeting and strata records default to at least {NestyStayBusinessRules.AssociationMinimumRetentionYears} years retention.")
    });

    [HttpGet("seed/pricebook")]
    public IActionResult GetSeedPricebook() => Ok(NestyStaySeed.DefaultPricebook());
}
