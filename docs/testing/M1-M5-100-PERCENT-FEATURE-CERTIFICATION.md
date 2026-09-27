# NestyStay M1–M5 feature certification

**Date:** 2026-09-26  
**Scope:** signed web milestones 1–5 only, evaluated from the isolated release-certification worktrees.
**Contract source:** [`NestyStay-Signed-Agreement-April-2026.pdf`](../contracts/NestyStay-Signed-Agreement-April-2026.pdf)
**Row-level source of truth:** [`M1-M5-MASTER-REQUIREMENTS-MATRIX.md`](M1-M5-MASTER-REQUIREMENTS-MATRIX.md)

## Executive decision

The current code has **no remaining `MISSING` rows and no code-caused `PARTIAL` rows** in the signed M1–M5 matrix. The remaining non-PASS statuses are explicit deployment/provider blockers or a contract change-control item. This is a local implementation-closure result, not a claim that staging or production is certified.

The signed agreement names Alibaba eKYC, while the later approved product decision selects Stripe Identity. The runtime uses Stripe Identity and has no active Alibaba provider wiring. This is recorded as `CONTRACT_CHANGE_REQUIRED`; historical migration/document references were preserved and were not edited.

The signed agreement does not require Airbnb, Booking.com, VRBO, external iCal/channel synchronization, or backup/retention operations within M1–M5. Those traceability rows are `NOT_APPLICABLE`, not missing features. Native mobile and the dedicated native Gate Guard interface remain Phase 6 / `OUT_OF_SIGNED_SCOPE`.

## Scope-adjusted scorecard

There are 123 traceability rows. Seven are `NOT_APPLICABLE` and two are `OUT_OF_SIGNED_SCOPE`, leaving **114 signed in-scope rows**.

| Milestone | In-scope rows | PASS | CONFIG_BLOCKED | PROVIDER_BLOCKED | CONTRACT_CHANGE_REQUIRED | Strict PASS |
|---|---:|---:|---:|---:|---:|---:|
| M1 Core Booking | 41 | 30 | 1 | 8 | 2 | 73.2% |
| M2 Badge System | 17 | 13 | 1 | 3 | 0 | 76.5% |
| M3 Wellness | 17 | 12 | 2 | 3 | 0 | 70.6% |
| M4 Directories/Trust/QR | 16 | 13 | 1 | 2 | 0 | 81.3% |
| M5 Property Manager | 23 | 14 | 2 | 7 | 0 | 60.9% |
| **Total signed in scope** | **114** | **82** | **7** | **23** | **2** | **71.9%** |

The strict percentage intentionally counts only locally verified contract rows. It is not the same as local implementation completeness: the blocked rows already have application paths and require staging/provider evidence.

## Closure changes completed in this pass

| Row | Before | After | Evidence |
|---|---|---|---|
| M1-05, M1-35, M1-39 | PARTIAL | PROVIDER_BLOCKED | Local email/notification/verification pricing paths exist; only external provider delivery/charge evidence remains. |
| M1-36, M1-37 | PARTIAL/PROVIDER_BLOCKED | CONTRACT_CHANGE_REQUIRED | Stripe Identity is the approved active runtime; the signed PDF still names Alibaba. |
| M1-41, M1-42, M1-43, M1-44 | MISSING/PARTIAL | NOT_APPLICABLE | Direct channels and external iCal are not signed M1–M5 requirements. |
| M2-09 | PARTIAL | PASS | The signed requirement is the Trusted search-boost/referral-program entitlement; the catalog and entitlement surfaces pass locally. No unsupported referral marketplace was invented. |
| M2-12 | PARTIAL | PROVIDER_BLOCKED | Local renewal/expiry worker and lifecycle tests pass; external Stripe billing/deployed worker evidence remains. |
| M2-15 | MISSING | PASS | Added persisted `MilestoneBadgeReview`, admin run/history endpoints, maintenance-worker scheduling, idempotent fingerprints, audit event, admin UI, API authorization test and frontend UI test. |
| M3-10, M3-13 | PARTIAL | CONFIG_BLOCKED/PROVIDER_BLOCKED | Local report/ledger paths pass; production storage and payout rail are external. |
| M4-11, M4-15 | PARTIAL | PROVIDER_BLOCKED | Local upsell/message persistence paths pass; Stripe/Brevo/SMS delivery remains external. |
| M5-03, M5-11, M5-18, M5-19, M5-21 | PARTIAL | PROVIDER_BLOCKED | Local invitation, billing-worker, QR/gate, staff-scope and verification paths pass; provider/device delivery remains external. |
| M5-24, M5-25 | PARTIAL/CONFIG_BLOCKED | NOT_APPLICABLE | External channel sync and operational backup/retention are not signed M1–M5 application requirements. |

## Remaining signed-scope statuses

### Configuration blockers

- **M1-09:** staging/production private MinIO bucket, restart persistence and operational evidence are not available in this local pass.
- **M2-14:** staging Admin account/configuration is not available for browser replay.
- **M3-10/M3-11:** staging/private storage configuration is not verified.
- **M4-07:** deployed provider-document storage is not verified.
- **M5-14/M5-15:** production MinIO bucket, lifecycle and restart evidence are not verified.

### Provider blockers

- **M1-05/M1-35:** Brevo/email and any external notification channel delivery.
- **M1-06:** Google OAuth origin/redirect configuration for the target environment.
- **M1-30/M1-31/M1-32/M1-38/M1-39:** real Stripe payment/refund/Identity/upsell provider replay.
- **M2-10/M2-11/M2-12:** external Stripe badge PaymentIntent, webhook and renewal billing replay.
- **M3-13/M3-14/M3-15:** Stripe Connect/bank payout, dispute and external notification rails.
- **M4-11/M4-15:** external Stripe Identity/charge and Brevo/SMS gate delivery.
- **M5-03/M5-11/M5-12/M5-18/M5-19/M5-20/M5-21:** external email, Stripe billing, camera/device, identity and notification delivery.

These rows are not application-code gaps. They are not certified until the deployment owner supplies the required runtime configuration/provider evidence.

## M1–M5 implementation status

### M1 — Core Booking

Local implementation covers authentication, session/2FA flows, public discovery, search/filter/date/guest controls, property detail data, favorites, map/list behavior, quote and fee calculation, booking creation/hold, host approval/rejection with stored reason, guest-visible status, cancellation, invoices/receipts, trips and in-app notifications. Payment/identity abstractions, webhooks and idempotency are present and deterministic/local tests pass. Remaining statuses are the external/provider/configuration list above plus the signed Alibaba-to-Stripe change-control record.

### M2 — Badge System

FREE, VERIFIED, TRUSTED and WELLNESS definitions, eligibility rules, benefits, assignments, restrictions, expiration, suspension, reactivation, renewal records, pricing, ownership security and server-authoritative Stripe payment architecture are implemented. The automated review engine is now implemented: it evaluates persisted host facts, records only changed results with a fingerprint, exposes authorized admin history/run controls, emits an audit event, and runs from the maintenance service. External Stripe and staging Admin access remain blocked.

### M3 — Wellness

Officer onboarding, eligibility, active/off-duty state, approval/rejection/suspension/reactivation, privacy, service plans, subscriptions, visit quotes, scheduling/conflicts, assignment, rescheduling/cancellation, reports, host acknowledgement, role restrictions and responsive workflows are implemented and locally tested. Private deployed storage, payout rails and external notification delivery remain blocked.

### M4 — Directories/Trust/QR

Custodian, trades, local-business and police/officer directory paths, search/filter/parish behavior, provider onboarding, moderation decisions/reasons/audit, badge-gated access, police privacy, QR issue/validate/expire/revoke, wrong-property denial, scan history, manual fallback and responsive paths are implemented and locally tested. Deployed document storage, external gate delivery and physical camera/device acceptance remain blocked. Map/geocoding is not a signed M1–M5 requirement.

### M5 — Property Manager

Portfolio/owner scoping, owner portal, invitations, assignments, reservations/conflicts, readiness, maintenance/work orders/vendors, utilities, invoices, ledger/reporting, documents, community, governance, QR/gate state, staff scope, subscriptions and responsive workflows are implemented and locally tested. MinIO deployment configuration, external billing/email/device/identity evidence remain blocked. External channel sync and operational backup/retention are outside the signed M1–M5 feature scope.

## Verification evidence

| Area | Result |
|---|---|
| Backend full solution tests | **209 passed, 0 failed; 1 skipped** (`MinioStorageProviderTests.LocalMinioRoundTripAndAuthorization` is conditional when the disposable MinIO endpoint is not supplied) |
| New M2-15 API test | **1 passed**: persisted facts, four-level evaluation, idempotent second run, history, and non-admin 403s |
| Frontend unit tests | **128 passed across 31 files** |
| New AdminBadges UI test | **1 passed**: loads review history, runs admin review, refreshes history and displays status |
| Frontend line coverage | **60.12% local Vitest v8 line coverage** |
| Frontend typecheck | **PASS** |
| Frontend production build | **PASS**; Vite transformed 2,154 modules |
| Frontend lint | **0 errors, 112 warnings**; warnings are pre-existing maintainability debt and are not hidden |
| Frontend npm audit | **0 known vulnerabilities** |
| Previously completed Playwright matrix | **224 started, 216 passed, 0 failed, 8 explicit skips, 0 did-not-run**; the new AdminBadges UI behavior also has a unit regression test |
| Local MinIO | Existing disposable-container I/O certification remains PASS; the full solution run reports the conditional test as skipped when `NESTYSTAY_MINIO_E2E` is not enabled |
| Sonar | Existing local scans report 0 bugs, 0 vulnerabilities and 0 hotspots; maintainability findings and Sonar’s lower executable-line coverage remain separate release debt |
| Gitleaks | Existing triage found no active production credential; placeholders/historical values remain documented without exposing values |

## Security and authorization

The review-run endpoints require the existing admin system-configuration policy. The new regression test proves an authenticated Host receives `403` for both run and history endpoints. Existing local authorization evidence covers Guest, Host, Owner, Property Manager, PM Staff, Wellness Officer, Service Provider, Local Business and Admin fixtures; no unexpected sensitive cross-account `200` was observed in the prior matrix. Staging role replay and deployed SHA parity remain external verification tasks.

## Exact ownership of remaining work

### Souhail — application/PR

1. Push these backend, frontend and root certification-branch commits and keep them in protected PR workflow.
2. Obtain written contract change control for Alibaba eKYC → Stripe Identity.
3. Do not report the `NOT_APPLICABLE` channel/backup rows as missing features.
4. Re-run the final matrix from protected-main SHAs after merge.

### Terrence — staging/server/provider

1. Review and merge the protected PRs, deploy them, and expose/verify backend/frontend build metadata.
2. Configure private MinIO/S3 storage, TLS, service permissions, persistence and operational recovery evidence.
3. Enable Brevo with a verified sender/domain and verify controlled delivery, outbox `SENT`, retry and duplicate behavior.
4. Provision controlled staging Admin access and replay all required role journeys without committing credentials.
5. Confirm Stripe test mode, Identity return URL/events/signature handling and safe payment/refund/Connect checks.

### Manual/external

Human screen-reader/keyboard/reduced-motion/forced-color certification, physical camera/device acceptance, external provider deliveries and production recovery evidence are not certified locally.

## Required final answers

MISSING BEFORE: **4**
MISSING AFTER: **0**
CODE-CAUSED PARTIAL BEFORE: **1 confirmed row (M2-15)**; other prior partials were implementation-plus-external/scope statuses.
CODE-CAUSED PARTIAL AFTER: **0**
M1 LOCAL IMPLEMENTATION: **100% of signed application paths implemented; 30/41 strict PASS, 11 externally/config/contract blocked**
M2 LOCAL IMPLEMENTATION: **100% of signed application paths implemented; 13/17 strict PASS, 4 externally/config blocked**
M3 LOCAL IMPLEMENTATION: **100% of signed application paths implemented; 12/17 strict PASS, 5 externally/config blocked**
M4 LOCAL IMPLEMENTATION: **100% of signed application paths implemented; 13/16 strict PASS, 3 externally/config blocked**
M5 LOCAL IMPLEMENTATION: **100% of signed application paths implemented; 14/23 strict PASS, 9 externally/config blocked**
TOTAL LOCAL IMPLEMENTATION COMPLETENESS: **100% for signed M1–M5 application paths; no MISSING and no code-caused PARTIAL rows**
STRICT CONTRACT PASS: **82/114 = 71.9%**
PROVIDER_BLOCKED: **23**
CONFIG_BLOCKED: **7**
CONTRACT_CHANGE_REQUIRED: **2**
UNEXPECTED AUTHORIZATION 200S: **NONE OBSERVED IN LOCAL NEGATIVE MATRIX**
BACKEND FAILURES: **0**
FRONTEND UNIT FAILURES: **0**
PLAYWRIGHT FAILURES: **0 in the completed matrix; 8 explicit skips; 0 did-not-run**
ALL CONTRACT-REQUIRED APPLICATION CODE IMPLEMENTED: **YES**
ALL LOCALLY-TESTABLE REQUIREMENTS VERIFIED: **YES, subject to the conditional MinIO test being enabled for a live I/O rerun**
MISSING FEATURES ZERO: **YES (within signed M1–M5 scope)**
CODE PARTIALS ZERO: **YES**
LOCAL IMPLEMENTATION 100%: **YES, as defined above**
READY FOR FINAL TERRENCE REVIEW/MERGE: **YES**
STAGING 100%: **NO — provider/configuration/SHA evidence remains**
EXTERNAL PROVIDERS 100%: **NO**
STRICT CONTRACT 100%: **NO — 23 provider blockers and 2 contract change-control rows remain**
PRODUCTION READY: **NO**

## Final verdict

The application-code closure target is met for the signed M1–M5 web scope: there are no remaining missing features or code-caused partials. The release is **not** staging- or production-certified until Terrence completes the explicit MinIO, Brevo, Stripe, Admin, deployment-SHA and manual/device gates, and the Alibaba-to-Stripe provider substitution is documented in change control.
