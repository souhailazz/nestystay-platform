# NestyStay local M1–M5 completion report

Date: 2026-09-15
Scope: repository-owned local development environment only. This report does not claim staging or production certification.

## Executive result

The complete locally executable platform matrix is green on the current release-candidate worktrees:

| Area | Result |
|---|---|
| PostgreSQL | PASS — repository-owned PostgreSQL 18 cluster on `127.0.0.1:55432` |
| API liveness/readiness | PASS — both endpoints HTTP 200 |
| Deterministic seed | PASS — 4 properties, 5 host profiles, 14 directory-provider seed records, 10 public pages, 3 experiences and 3 journal articles |
| Backend | PASS — 173/173 tests, 0 failed, 0 skipped |
| Frontend unit | PASS — 48/48 tests |
| Frontend typecheck/build | PASS |
| Frontend lint | PASS — 0 errors, 92 existing warnings |
| Browser matrix | PASS — 182 passed, 0 failed, 9 intentional skips |
| Browser projects | Desktop Chromium, Firefox, WebKit, tablet Chromium and mobile Chromium |
| Human accessibility certification | NOT COMPLETE — automation is not a human screen-reader/device sign-off |
| External provider certification | NOT COMPLETE — credentials and deployed callbacks are outside this local run |

## Reproduce locally

From the repository root:

```powershell
.\scripts\dev-up.ps1
.\scripts\dev-seed.ps1
.\scripts\dev-smoke.ps1
```

The local app uses the isolated database `nestystay_dev` on port `55432`. It never touches staging or production data. The local admin bearer token is `test-admin-token` and is accepted only by the Development configuration.

Full verification:

```powershell
.\scripts\dev-test.ps1
```

The browser command used for the final matrix was:

```powershell
$env:PLAYWRIGHT_API_URL='http://127.0.0.1:5019/api/health'
$env:PLAYWRIGHT_BASE_URL='http://127.0.0.1:5173'
$env:NESTYSTAY_E2E_ADMIN_TOKEN='test-admin-token'
npx playwright test --workers=1
```

## Intentional skips

The nine skips are not hidden failures:

1. `final-hardening-minio-ui.spec.ts` — three Chromium viewport cases. The test requires `NESTYSTAY_MINIO_E2E=true` and an explicitly configured MinIO fixture; local file storage was used instead.
2. `production-smoke.spec.ts` authenticated login — three Chromium viewport cases. `SMOKE_EMAIL` and `SMOKE_PASSWORD` were not supplied, so no deployed account was touched.
3. `usability-upgrades.spec.ts` mobile workspace navigation — two desktop/tablet cases. This assertion is intentionally mobile-only.
4. `canonical-route-inventory.spec.ts` role authorization — one tablet case. The existing contract certifies this check on desktop and mobile Chromium.

The Firefox and WebKit projects intentionally collect only the cross-browser critical smoke test; they do not create skipped results for the Chromium-only inventory assertions.

## Milestone scorecard

Percentages below are local verification coverage estimates based on the declared acceptance groups in the existing M1–M5 matrices. They are not production-readiness percentages.

### M1 — Core booking: 95% local / externally blocked for release

Implemented and verified locally: registration/login/session/2FA, role guards, Explore search and filters, dates and guests, listing details, availability, pricing and fees, persistent favorites, map/list behavior, booking quote/create/hold/pending, host approval and rejection with stored reason, guest status messaging, cancellation/status surfaces, receipts/invoices, deterministic Stripe-compatible payment authorization/capture/refund/idempotency/webhooks, Stripe Identity application boundary, notifications/unread/read/deep-link routes, desktop/tablet/mobile workflows and automated axe/keyboard checks.

Remaining: real Stripe Identity session and signed events on staging; live Stripe PaymentIntent/capture/refund/Connect behavior; deployed notification delivery; human accessibility certification; three intentionally external/deployed browser checks described above.

### M2 — Badge system: 95% local / externally blocked for release

Implemented and verified locally: FREE, VERIFIED, TRUSTED and WELLNESS catalog; benefits and restrictions; eligibility; ownership authorization; assignment; upgrade/purchase lifecycle; expiration, renewal, suspension and reactivation; audit/history; admin search/mutation; server-authoritative local PaymentIntent lifecycle, idempotency and refund-to-suspension path; responsive host/admin UI; all-level browser journeys.

Remaining: live Stripe badge PaymentIntent/webhook/refund certification, production expiry/renewal worker operations and human visual/accessibility sign-off.

### M3 — Wellness services: 95% local / externally blocked for release

Implemented and verified locally: officer onboarding/resume, privacy-aware profile and document state, approval/rejection/suspension/reactivation, availability, plans/subscriptions, quote, visit scheduling/assignment/conflict handling/rescheduling/cancellation, report creation and scoped visibility, photos/upload abstraction, commission/payout states, admin operations and responsive journeys.

Remaining: real Connect/bank payout and dispute rails, production private object storage, Brevo/SMS/push delivery and human operational/device certification.

### M4 — Directories, trust, QR and gate: 95% local / external map/device gates remain

Implemented and verified locally: four directory categories, search/filter/parish/profile/rating/contact, provider onboarding, moderation approve/reject/request-changes with persisted reason and audit, police privacy boundary, badge-gated contact, QR issue/validate/expire/revoke, wrong-property rejection, scan history, manual guard fallback and gate-facing statuses.

Remaining: production map/geocoder/tile configuration, physical camera/scanner certification, external gate-message delivery and human accessibility/device certification.

### M5 — Property Manager: 90% local / external operations remain

Implemented and verified locally: manager/owner scoping, dashboard/KPIs, owner portal, invitations and assignment, invoices/lines/bulk issue/overdue/payment/refund/retry/statement paths, utilities, maintenance/work orders/vendors, community notices, governance/proxy/anonymous voting, documents/version/download/archive/export state, gate/QR, staff/workspace controls, cleaning/inspection/readiness, reporting and subscription lifecycle surfaces. Desktop/tablet/mobile representative M5 journeys passed.

Remaining: production storage and export retention, live billing/Connect reconciliation, external calendar/channel sync if required by contract, email/SMS/push delivery, scheduled-worker/monitoring/recovery drills and human operational certification.

## Honest provider boundary

| Capability | Local mode | Not proven by this report |
|---|---|---|
| Stripe payments | Deterministic Stripe-compatible adapter and local lifecycle | Real account, live keys, live webhook signature and production money movement |
| Stripe Identity | Deterministic local result plus Stripe Identity application boundary | Real Identity session, document review and signed staging events |
| Stripe Connect | Local persisted account/transfer state machine with paid/pending/failed/disputed scenarios | Connected-account onboarding, bank verification, real transfer and dispute rails |
| Email | File outbox with real queued application templates | Brevo/SMTP delivery, sender/domain reputation and retry operations |
| Storage | Local private file-storage abstraction | MinIO/S3/R2 production bucket, keys, retention and disaster recovery |
| Maps | Coordinates and application list/map/manual fallback | Chosen tile/geocoder provider, quotas and network behavior |
| SMS/Web Push | Disabled/queued application state | Provider credentials, device delivery and retry/dead-letter operations |
| InsuraGuest | No live provider claim | External account/configuration if contractually required |

## Release decision

Local development is operationally runnable and the locally executable M1–M5 matrix is green. The platform is **not yet release-certified** until protected-main consolidation, staging/provider verification, external storage/notification setup where required, and manual accessibility/device sign-off are completed.

The exact source SHAs used for the final consolidated result must be recorded after the protected-branch workflow completes; this document is intentionally not a claim that the current candidate is already the remote `main` branch.
