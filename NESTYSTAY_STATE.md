# Current State

## Last Completed Task

AUTO-0001 — Correct approved post-launch guest fee across the active pricing path.

## Validation Result

- Backend full solution with local MinIO enabled: 210 passed, 0 failed, 0 skipped.
- Frontend unit suite: 128 passed across 31 files.
- Frontend typecheck: PASS.
- Frontend production build: PASS.
- Frontend lint: 0 errors, 112 warnings.
- Frontend npm audit at high severity: 0 known vulnerabilities.
- Browser baseline: 224 started, 213 passed, 0 failed, 11 explicit skips, 0 did-not-run.

## Confirmed Complete

- Active standard guest platform fee is 10% in backend business rules, pricebook service, seed, frontend estimate/copy, and regression expectations.
- A forward EF migration updates existing pricebook rows to 10% without editing historical migrations.
- Local PostgreSQL and disposable MinIO-backed backend regression is green.

## Partial

- M1: external Stripe/Identity/Brevo/storage/payout/insurance and staging SHA parity are not proven.
- M2: Gold/Platinum commercial membership model remains outdated; real Stripe lifecycle is not proven.
- M3: external provider, storage, payout and operational certification remain.
- M4: Gate Guard authenticated lifecycle and geocoding remain incomplete.
- M5: production storage/email/billing and complete manual responsive certification remain.
- Fresh Sonar analysis and full staging role/IDOR certification remain.

## Missing

- Geocoding provider/workflow.
- Complete Gate Guard invitation/provisioning/assignment path.
- Native mobile application source, which is future scope unless explicitly promoted.
- Fresh current-SHA Sonar export.
- Exact deployed staging frontend/backend SHA evidence.

## Broken

- Anonymous QR validation is not a complete authenticated Gate Guard authorization boundary; it accepts an optional actor instead of requiring a scoped guard role.

## Wrong Business Logic

- Gold/Platinum still use lifetime flat guest fees and shared host commission instead of the current amended term-based commercial model.

## P0

- Resolve the Gold/Platinum commercial model and any unreadable mandatory values.
- Confirm production/staging provider configuration and deployed SHA parity before release.

## P1

- Verify private MinIO configuration, backup/retention and upload workflows.
- Verify Brevo acceptance, receipt, retry and duplicate behavior.
- Complete current staging role/IDOR matrix.
- Decide and, if required, implement the Gate Guard lifecycle/security boundary.
- Run fresh Sonar and review findings.

## P2

- Decide whether geocoding is current contractual scope.
- Consolidate/document historical SpecScreens and aliases.
- Complete human accessibility and operational mobile certification.

## P3

- Reduce existing frontend lint warnings and maintainability debt after release blockers are handled.

## External Blockers

- Safe staging/production credentials and access for MinIO, Brevo, Stripe, Stripe Identity, Stripe Connect, InsuraGuest, and deployed build metadata.
- Exact handwritten Gold/Platinum values where the source cannot be read with certainty.
- Human accessibility/legal/privacy certification.

## Newly Discovered Problems

- Frontend `PublicSearchMap` contained a second hardcoded 9% estimate independent of the backend quote.
- The persistent pricebook migration `20260830144204_AlignSignedContractM1M2PricingV2` writes the old 9% value, requiring a forward correction migration rather than history rewriting.

## Decisions

- Historical migrations and historical audit documents are preserved.
- Standard guest fee correction is complete; Gold/Platinum is not guessed.
- Local deterministic providers are never presented as live external verification.
- Original dirty workspace remains untouched; remediation is isolated to certification branches.

## Next Best Action

Implement and test the next highest-priority unblocked slice: Gold/Platinum model only after exact mandatory values are confirmed; otherwise proceed with Gate Guard authorization or local security/IDOR hardening while external providers remain blocked.

## Loop Status

CONTINUE
