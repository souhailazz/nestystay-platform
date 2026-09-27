# Autopilot History

## AUTO-0001 — Apply approved 10 percent guest platform fee

Priority: P0 pricing integrity  
Date: 2026-09-27

### Problem

Active NestyStay logic still calculated a 9% standard guest platform fee even though the current approved handwritten amendment sets the post-launch guest fee to 10%. The stale value appeared in backend rules, pricebooks, the persistent migration path, frontend estimates/copy, and regression expectations.

### Changes

- Changed active backend standard guest fee resolution and default pricebook entries to 10%.
- Added a forward-only EF migration to update existing `milestone_pricebook_entry` and `pricebook_entry` rows without rewriting migration history.
- Changed the frontend search estimate and booking copy to 10%.
- Updated affected tests and booking totals.
- Preserved historical migration/audit references to the former typed value.

### Files

- Backend `BusinessRules.cs`, `PricebookService.cs`, `NestyStaySeed.cs`.
- Backend migration `20260927100000_ApplyPostLaunchGuestFee.cs`.
- Backend application/infrastructure tests.
- Frontend `PublicSearchMap.tsx`, `BookingShell.tsx`, `BookingReviewPage.tsx`, `BookingModal.test.tsx`, and coming-soon pricing copy.
- Updated `NESTYSTAY_FULL_GAP_ANALYSIS.md`, `NESTYSTAY_GAP_MATRIX.json`, and `NESTYSTAY_STATE.md`.

### Tests

- Backend full solution with local MinIO: 210 passed, 0 failed, 0 skipped.
- Frontend unit: 128 passed.
- Frontend typecheck/build: PASS.
- Frontend lint: 0 errors, 112 warnings.

### Result

The active standard guest fee is now aligned with the approved 10% rule and existing database rows have a safe forward migration path.

### Discovered Follow-ups

- Gold/Platinum founding economics still need the exact amended model.
- External provider and staging SHA verification remain outstanding.
- Gate Guard provisioning/authorization remains incomplete.
