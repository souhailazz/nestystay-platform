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
- Gate Guard lifecycle/authentication is locally implemented; staging and browser evidence remain outstanding.

## AUTO-0002 — Add scoped Gate Guard lifecycle and authenticated QR validation

Priority: P1 trust and access control  \
Date: 2026-09-27

### Problem

The code had a `GateGuard` enum and QR UI, but no complete invitation/acceptance/assignment lifecycle. QR validation did not enforce a property-scoped Gate Guard authorization boundary, and Gate Guard members could enter Property Manager workspace paths.

### Changes

- Allowed the existing Property Manager staff invitation flow to create `GATE_GUARD` memberships only with one or more property scopes and no finance/payout capabilities.
- Synchronized the effective `GateGuard` user role on acceptance and removed it on revocation.
- Added authenticated `POST /api/property-manager/qr/validate-authenticated` with active membership and property-scope enforcement.
- Kept the existing anonymous validator for guest/pass compatibility without attributing scans to an untrusted signed-in actor.
- Denied Gate Guard users from PM finance and professional operations contexts.
- Added frontend role typing, navigation, workspace labeling, and authenticated validator API usage.

### Evidence

- Backend commit: `7352a95`.
- Frontend commit: `e9fde6e`.
- Backend full solution with local MinIO: 211 passed, 0 failed, 0 skipped.
- Backend Gate Guard authorization matrix: 4 passed, 0 failed, 0 skipped.
- Frontend unit suite: 128 passed; typecheck and production build passed; lint 0 errors / 112 warnings.

### Remaining qualification

Staging account provisioning, full browser coverage for the new role, physical gate/device integration, and external deployment verification remain outstanding. No production or staging environment was modified.

## AUTO-0003 — Harden legacy Gate Guard assignment path

Priority: P1 access-control integrity  \\
Date: 2026-09-27

### Problem

The historical `/api/property-manager/staff` endpoint accepted arbitrary role text, including `GATE_GUARD`, but only persisted a legacy row. That path could not create the scoped P0 membership or synchronize the effective `GateGuard` user role.

### Changes

- Reject `GATE_GUARD` on the legacy endpoint with an explicit instruction to use `/api/property-manager/p0/members`.
- Keep the supported P0 invite/accept/revoke lifecycle unchanged and covered.
- Add an API regression test proving the legacy route cannot create an unscoped Gate Guard assignment.

### Evidence

- Backend commit: `d2a3ac3`.
- Full backend solution with local MinIO: 212 passed, 0 failed, 0 skipped.
- Focused Property Manager authorization tests: 5 passed, 0 failed, 0 skipped.

## AUTO-0004 — Make integration health report actual storage readiness

Priority: P1 operational correctness  \\
Date: 2026-09-27

### Changes

- Health integration status now uses `IStorageProvider.CheckReadinessAsync`.
- The response reports the configured provider name, actual readiness status, and a safe provider detail.
- Provider exceptions are converted to an explicit `UNAVAILABLE` result rather than an unconditional success claim.
- Added health regression assertions.

### Evidence

- Backend commit: `094b539`.
- Health tests: 14 passed, 0 failed, 0 skipped.
- Full backend solution with local MinIO: 212 passed, 0 failed, 0 skipped.
