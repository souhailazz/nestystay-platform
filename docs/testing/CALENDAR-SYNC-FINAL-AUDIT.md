# NestyStay Calendar Sync — Final Audit

Date: 2026-09-16

This audit covers the current candidate backend/frontend branches and the
calendar operations added for host and property-manager workflows. It does not
claim that external marketplace credentials or production infrastructure were
verified locally.

## Result summary

| Area | Status | Evidence |
| --- | --- | --- |
| Inbound ICS connection | Implemented | `POST /api/properties/{propertyId}/calendar/feeds` |
| Manual sync and scheduled sync | Implemented | Calendar controller plus `CalendarSyncMaintenanceService` |
| Feed status, timestamps, errors, retry | Implemented | Feed fields, sync-event history, 15-minute success / 5-minute retry scheduling |
| Idempotent event updates | Implemented | UID-based upsert keeps the imported block identity stable |
| Duplicate UIDs | Implemented | Duplicate events are reduced deterministically to the last event in the feed |
| Cancelled/deleted inbound events | Implemented | `STATUS:CANCELLED` and absent events soft-delete the prior imported block |
| Date-only and date-time ICS | Implemented for booking dates | UTC and `TZID` date-times are normalized to the date-level model used by nightly availability |
| Inbound source labelling | Implemented | Airbnb, Booking, VRBO, or Custom inferred from the feed host and returned as `channel` |
| Outbound ICS download | Implemented | Authenticated `/api/properties/{propertyId}/calendar/export.ics` |
| Private externally subscribable ICS | Implemented | Rotatable token at `/api/properties/{propertyId}/calendar/export-token`; anonymous token URL at `/api/calendar/export/{token}.ics` |
| Outbound contents | Implemented | Confirmed bookings, manual holds, and imported blocks; no guest PII |
| Outbound lifecycle | Implemented | Stable UIDs, `DTSTAMP`, `LAST-MODIFIED`, all-day `DTSTART/DTEND`, cancelled bookings omitted, token revoke/rotate |
| Manual host calendar blocks | Implemented | Create, edit, release, conflict checking, API-backed UI |
| PM owner blocks | Implemented | Existing PM professional/owner block routes with ownership scope, overlap checking, cancellation and history |
| Pricing overrides | Implemented | Host-owned persisted rules are applied by the backend quote builder |
| Promotions/discounts | Implemented | Persisted, property-scoped, server-selected and reflected in quote breakdown |
| SSRF controls | Preserved | URL validation, no credentials, public DNS validation, socket-level DNS check, no redirects, 30-second timeout, 5 MB response limit |
| Background overlap guard | Implemented | In-process per-feed guard prevents overlapping maintenance pulls |

## Remaining limitations

1. The calendar domain stores booking availability as `DateOnly`. Time-of-day
   events are normalized to the affected calendar dates; this is intentional for
   nightly accommodation inventory, but it is not an hourly scheduling model.
2. External Airbnb, Booking.com, and VRBO accounts are not needed to exercise
   the local flow, but a real provider account must be used in staging to prove
   the provider's export URL, update cadence, and feed-specific quirks.
3. A platform-level commercial entitlement matrix is not invented here. Badge
   feature access remains backend-authoritative, and PM subscription lifecycle
   remains backend-authoritative; plan-to-feature policy still needs the
   business-approved mapping before it can safely gate commercial features.

## Files changed

- `backend/src/NestyStay.Api/Controllers/CalendarController.cs`
- `backend/src/NestyStay.Api/Controllers/CalendarExportController.cs`
- `backend/src/NestyStay.Api/Services/CalendarIcsBuilder.cs`
- `backend/src/NestyStay.Api/Services/CalendarSyncMaintenanceService.cs`
- `backend/src/NestyStay.Api/Controllers/PropertiesController.cs`
- `backend/src/NestyStay.Infrastructure/Persistence/Milestones/EfPhaseOneStore.cs`
- `backend/src/NestyStay.Infrastructure/Persistence/Milestones/EfSpecCompletionStore.cs`
- `backend/src/NestyStay.Infrastructure/Persistence/Milestones/MilestoneEntities.cs`
- `backend/src/NestyStay.Infrastructure/Persistence/NestyStayDbContext.cs`
- `frontend/src/pages/ProductPages.tsx`
- `frontend/src/features/host/HostPricingPromotions.tsx`
- `frontend/src/lib/api.ts`

## Local regression evidence

- `CalendarEndpointTests`: inbound connection, sync, export, UID update,
  cancellation removal, manual blocks, outbound token, revocation — passing.
- `SpecCompletionEndpointTests`: pricing rule/promotion create, quote
  application, update, delete, and cross-host rejection — passing.
- `PropertyManagerFoundationRegressionTests`: dashboard without a rental
  listing or booking — added as a local acceptance regression.
