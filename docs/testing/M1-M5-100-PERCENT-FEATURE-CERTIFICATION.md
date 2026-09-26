# NestyStay M1–M5 100% feature certification

**Date:** 2026-09-26  
**Decision:** NOT 100% certified.  
**Scope:** signed web milestones 1–5 only, from isolated release-certification worktrees.  
**Contract source:** `docs/contracts/NestyStay-Signed-Agreement-April-2026.pdf`  
**Complete row-level evidence:** [`M1-M5-MASTER-REQUIREMENTS-MATRIX.md`](M1-M5-MASTER-REQUIREMENTS-MATRIX.md)

## Executive decision

The platform has a substantial locally working implementation. It does **not** meet a defensible 100% certification standard yet. The strict score below counts a requirement only when its complete contract path and evidence are available. A local adapter, a compiled provider, a page that renders, or a green unit test does not turn an unverified external/provider requirement into `PASS`.

The signed PDF specifies Alibaba eKYC. Later approved client direction selects Stripe Identity instead. The current runtime correctly uses Stripe Identity and has no active Alibaba provider wiring, but the contract-level provider substitution is recorded as `PARTIAL`/`PROVIDER_BLOCKED` until the change is documented as an approved contract amendment. Historical migration references were preserved.

## Required scorecard

Strict local completeness is `PASS / in-scope contract rows`. `OUT_OF_SIGNED_SCOPE` and `NOT_APPLICABLE` are excluded from the denominator; `PARTIAL`, `MISSING`, `CONFIG_BLOCKED` and `PROVIDER_BLOCKED` are not counted as verified.

| Milestone | In-scope contract rows | PASS / verified | PARTIAL | MISSING | CONFIG_BLOCKED | PROVIDER_BLOCKED | Strict local completeness |
|---|---:|---:|---:|---:|---:|---:|---:|
| M1 Core Booking | 45 | 30 | 5 | 3 | 1 | 6 | 66.7% |
| M2 Badge System | 17 | 11 | 2 | 1 | 1 | 2 | 64.7% |
| M3 Wellness | 17 | 12 | 2 | 0 | 1 | 2 | 70.6% |
| M4 Directories/Trust/QR | 16 | 13 | 2 | 0 | 1 | 0 | 81.3% |
| M5 Property Manager | 25 | 14 | 6 | 0 | 3 | 2 | 56.0% |
| **Total in scope** | **120** | **80** | **17** | **4** | **7** | **12** | **66.7%** |

There are also two explicitly excluded rows: M4-17 dedicated native Gate Guard interface and M5-26 native iOS/Android applications. The contract places those in Phase 6, outside this M1–M5 web certification. M4-16 map/geocoding is `NOT_APPLICABLE` because it is not a signed M1–M5 requirement in the PDF; the current app retains coordinate/manual fallback behavior.

## Every contract requirement and current status

The following index lists every row in the master matrix. The matrix contains the required role, UI, API, service, database, provider, automated-test, manual-test and evidence columns for each ID.

### M1 — Core Booking System

| ID | Requirement | Status |
|---|---|---|
| M1-01 | Registration | PASS |
| M1-02 | Login and logout | PASS |
| M1-03 | Session persistence and role-aware routing | PASS |
| M1-04 | Two-factor authentication | PASS |
| M1-05 | Password reset and passwordless login | PARTIAL |
| M1-06 | Google login | PROVIDER_BLOCKED |
| M1-07 | Public property listings | PASS |
| M1-08 | Property creation and editing | PASS |
| M1-09 | Property gallery and photo management | CONFIG_BLOCKED |
| M1-10 | Property name, location and parish | PASS |
| M1-11 | Amenities and sleeping arrangements | PASS |
| M1-12 | House rules and cancellation policy | PASS |
| M1-13 | Coordinates and approximate location | PASS |
| M1-14 | Availability calendar | PASS |
| M1-15 | Blocked dates and manual holds | PASS |
| M1-16 | Minimum stay, date buffers and overlap protection | PASS |
| M1-17 | Search by location, parish, title, dates and guests | PASS |
| M1-18 | Filters, sorting and pagination | PASS |
| M1-19 | Favorites/wishlist persistence | PASS |
| M1-20 | Loading, empty, error and retry states | PASS |
| M1-21 | Ratings and guest reviews | PASS |
| M1-22 | Booking popup | PASS |
| M1-23 | Price breakdown, cleaning/service fees and total | PASS |
| M1-24 | Booking creation and PENDING state | PASS |
| M1-25 | Date hold window and release behavior | PASS |
| M1-26 | Host approval workflow | PASS |
| M1-27 | Host rejection with stored reason | PASS |
| M1-28 | Guest rejection message and released dates | PASS |
| M1-29 | Booking cancellation | PASS |
| M1-30 | Payment authorization and capture | PROVIDER_BLOCKED |
| M1-31 | Payment failure and recovery | PROVIDER_BLOCKED |
| M1-32 | Refund and idempotent refund handling | PROVIDER_BLOCKED |
| M1-33 | Invoice, receipt and payment history | PASS |
| M1-34 | Guest trips/dashboard | PASS |
| M1-35 | Notifications, unread count, mark-read and deep links | PARTIAL |
| M1-36 | eKYC required/optional branch | PARTIAL |
| M1-37 | Contract-specified Alibaba ID/liveness verification | PROVIDER_BLOCKED |
| M1-38 | Identity processing, approval, failure and webhook correlation | PROVIDER_BLOCKED |
| M1-39 | Host-paid guest verification upsell | PARTIAL |
| M1-40 | Responsive booking experience | PASS |
| M1-41 | Airbnb direct channel integration | MISSING |
| M1-42 | Booking.com direct channel integration | MISSING |
| M1-43 | VRBO direct channel integration | MISSING |
| M1-44 | Custom iCal import/export and sync logs | PARTIAL |
| M1-45 | Web app across desktop/tablet/mobile | PASS |

**M1 verified:** clean local seeded PostgreSQL booking/auth/property paths; host approve/reject with stored reason; quote/fees; trips; deterministic payment/identity states; responsive browser coverage; server-side negative authorization. Backend: 209 passed, 0 failed, 0 skipped. Frontend: 127 passed, typecheck/build passed, lint 0 errors. Playwright: 224 started, 216 passed, 0 failed, 8 explicit skips, 0 did-not-run.

**M1 not complete:** real provider payment/Identity evidence, the contract provider substitution record, Brevo delivery, staging SHA parity, production storage/restart evidence, and direct Airbnb/Booking.com/VRBO integrations. The direct channel rows are not inferred from the calendar page.

### M2 — Badge System

| ID | Requirement | Status |
|---|---|---|
| M2-01 | FREE definition, price and benefits | PASS |
| M2-02 | VERIFIED definition and requirements | PASS |
| M2-03 | TRUSTED definition and prerequisites | PASS |
| M2-04 | WELLNESS definition and prerequisites | PASS |
| M2-05 | Eligibility recalculation and ownership enforcement | PASS |
| M2-06 | Assignment and activation | PASS |
| M2-07 | Badge visibility on cards/details/profiles/filters | PASS |
| M2-08 | Benefit restrictions and badge-gated directories | PASS |
| M2-09 | Trusted search boost and referrals | PARTIAL |
| M2-10 | Badge purchase and server-authoritative amount/currency | PROVIDER_BLOCKED |
| M2-11 | Badge payment processing/failure/cancel/webhook | PROVIDER_BLOCKED |
| M2-12 | Badge renewal and annual lifecycle | PARTIAL |
| M2-13 | Expiration/suspension/revocation/reactivation | PASS |
| M2-14 | Admin search/review/grant/audit | CONFIG_BLOCKED |
| M2-15 | Automated review/eligibility engine | MISSING |
| M2-16 | Authenticator-app 2FA | PASS |
| M2-17 | Badge pricing/campaign/founding configuration | PASS |

**M2 verified:** four badge definitions, entitlement restrictions, eligibility, assignment, lifecycle, ownership protection, local server-authoritative PaymentIntent state and admin mutation tests. Real external Stripe badge payments/webhooks and staging Admin access are not verified. The distinct automated review engine named in the agreement is not proven by the current scheduled maintenance job.

### M3 — Wellness Services

| ID | Requirement | Status |
|---|---|---|
| M3-01 | Officer registration/onboarding | PASS |
| M3-02 | Officer eligibility, active/off-duty state and availability | PASS |
| M3-03 | Approval/rejection/suspension/reactivation | PASS |
| M3-04 | Officer privacy and badge-ID rules | PASS |
| M3-05 | Wellness service types and pricing | PASS |
| M3-06 | Subscription, included visits and eligibility | PASS |
| M3-07 | Visit booking request and quote | PASS |
| M3-08 | Scheduling, conflicts and assignment | PASS |
| M3-09 | Rescheduling, cancellation and visit states | PASS |
| M3-10 | Photo reports and host/admin visibility | PARTIAL |
| M3-11 | Report photos, drafts, retry and upload validation | CONFIG_BLOCKED |
| M3-12 | Host acknowledgement and report completion | PASS |
| M3-13 | Host fees, officer commission and ledger | PARTIAL |
| M3-14 | Escrow, payout, dispute and failure states | PROVIDER_BLOCKED |
| M3-15 | Wellness notifications/reminders | PROVIDER_BLOCKED |
| M3-16 | Wellness role restrictions and privacy | PASS |
| M3-17 | Responsive officer/host/admin workflows | PASS |

**M3 verified:** officer lifecycle, availability, plan/quote, visit assignment/conflict handling, reports, privacy and local ledger states. Real private storage, Brevo/SMS/push, Stripe Connect/bank payout and dispute rails remain provider/deployment blockers.

### M4 — Directories, Trust, QR and Gate

| ID | Requirement | Status |
|---|---|---|
| M4-01 | Custodian directory | PASS |
| M4-02 | Trades directory | PASS |
| M4-03 | Local Business directory | PASS |
| M4-04 | Police/off-duty officer directory | PASS |
| M4-05 | Directory search/parish/category/sort/ratings | PASS |
| M4-06 | Provider onboarding and recoverable draft | PASS |
| M4-07 | Provider documents and private download | CONFIG_BLOCKED |
| M4-08 | Moderation approve/reject/request changes with reason/audit | PASS |
| M4-09 | Badge-gated directory access and upgrade explanation | PASS |
| M4-10 | Police privacy/platform communication/119 | PASS |
| M4-11 | Guest verification upsell | PARTIAL |
| M4-12 | QR issue and secure persistence | PASS |
| M4-13 | QR validation/expiry/revoke/wrong-property denial | PASS |
| M4-14 | QR scan history and manual fallback | PASS |
| M4-15 | Gate/property communication | PARTIAL |
| M4-16 | Map/geocoding provider | NOT_APPLICABLE |
| M4-17 | Dedicated native Gate Guard interface | OUT_OF_SIGNED_SCOPE |
| M4-18 | Responsive directory/provider/QR workflows | PASS |

**M4 verified:** four directory categories, search/filter, provider onboarding/moderation/audit, police privacy, QR lifecycle, wrong-property denial and manual fallback. Storage-backed provider documents, external gate delivery and physical camera/device certification are not complete. Dedicated Gate Guard provisioning is not silently treated as an M4 defect because the signed PDF places that native interface in Phase 6.

### M5 — Property Manager Platform

| ID | Requirement | Status |
|---|---|---|
| M5-01 | Multi-owner/multi-property portfolio dashboard | PASS |
| M5-02 | Owner portal and scoping | PASS |
| M5-03 | Owner invitation and verification | PARTIAL |
| M5-04 | Owner/property/unit assignment | PASS |
| M5-05 | Reservations/availability/calendar conflicts | PASS |
| M5-06 | Manual blocks and property readiness | PASS |
| M5-07 | Maintenance requests and work orders | PASS |
| M5-08 | Vendors, quotes, approvals and cost lines | PASS |
| M5-09 | Utilities readings/schedules/charges/disputes | PASS |
| M5-10 | Invoices and invoice lines | PASS |
| M5-11 | Bulk issue/overdue/recurring reminders | PARTIAL |
| M5-12 | Payments/refunds/retries/receipts/statements | PROVIDER_BLOCKED |
| M5-13 | Accounting balances and owner reporting | PASS |
| M5-14 | Documents metadata/versions/archive/download/expiry | CONFIG_BLOCKED |
| M5-15 | Document upload and secure object storage | CONFIG_BLOCKED |
| M5-16 | Community notices/comments/acknowledgements | PASS |
| M5-17 | Governance proposals/discussion/voting | PASS |
| M5-18 | Gate communication and QR lifecycle | PARTIAL |
| M5-19 | Staff/team invitations and permission scope | PARTIAL |
| M5-20 | Subscription plan lifecycle | PROVIDER_BLOCKED |
| M5-21 | Tenant/owner verification and balances | PARTIAL |
| M5-22 | Search/filters/reporting/bulk operations | PASS |
| M5-23 | Responsive PM/owner/operations workflows | PASS |
| M5-24 | External iCal/channel synchronization | PARTIAL |
| M5-25 | Backup/retention/recovery of PM documents | CONFIG_BLOCKED |
| M5-26 | Native iOS/Android applications | OUT_OF_SIGNED_SCOPE |

**M5 verified:** PM portfolio and owner isolation, owner blocks, reservations, maintenance/work orders/vendors, utilities, invoices/lines, local payment ledger paths, community/governance, QR/gate state, documents validation and responsive workflows. Production MinIO persistence/backup, email invitations, live billing webhooks, worker operations and external channel interoperability remain unverified.

## Cross-cutting acceptance results

### Role and authorization

Local backend and browser evidence covered Guest, Host, Owner, Property Manager, PM Staff, Wellness Officer, Service Provider, Local Business and Admin fixtures where available. Cross-account/property/portfolio checks returned only `401`, `403` or safe `404`; no sensitive cross-account `200` was observed. This is local authorization evidence, not staging-role certification. Staging Admin provisioning and real PM Staff invitation delivery are still deployment dependencies.

### State-machine checks

The local evidence covered booking `PENDING → APPROVED/REJECTED`, hold/release, cancellation, payment success/failure/refund, identity processing/approved/failure, badge activation/expiry/suspension/renewal, wellness assignment/visit/report, moderation decisions, QR valid/expired/revoked/wrong-property, maintenance transitions, invoice/payment/refund and subscription transitions. External webhook/provider delivery and production worker timing remain open wherever the row is blocked above.

### Storage, email and external providers

- Local MinIO: real disposable container I/O passed for upload, overwrite/hash, signed download, wrong-credential denial, traversal/empty/oversize rejection, browser property-photo upload and restart persistence. This does not prove staging/production bucket configuration, backups or retention.
- Brevo: real delivery was not verified. The local file/outbox provider and templates were tested; the actual staging API key, verified sender, mailbox receipt, retry and duplicate-delivery evidence are absent.
- Stripe: local deterministic payment/Identity/Connect paths and signature/idempotency tests passed. A real external Identity session, live/test-account PaymentIntent/capture/refund/Connect replay and signed staging event verification were not performed in this certification.
- Map/geocoding: not a signed M1–M5 requirement and not certified as an external provider.

### Build and test evidence

| Area | Result |
|---|---|
| Backend restore/build | PASS |
| Backend complete unfiltered suite | 209 passed, 0 failed, 0 skipped; Domain 5, Application 24, Infrastructure 32, API 148 |
| Frontend unit suite | 127 passed, 30 files |
| Frontend local line coverage | 60.04% |
| Frontend typecheck/build | PASS; Vite 7.3.6, 2,154 modules |
| Frontend lint | 0 errors, 112 warnings |
| Frontend npm audit | 0 known vulnerabilities |
| Playwright | 224 started, 216 passed, 0 failed, 8 explicit skips, 0 did-not-run |
| Sonar frontend | Gate PASS; 0 bugs, 0 vulnerabilities, 0 hotspots; 52.5% overall / 56.2% line; 978 smells |
| Sonar backend | Gate OK; 0 bugs, 0 vulnerabilities, 0 hotspots; 11.2% overall / 9.6% line; 1,007 smells |
| Human accessibility certification | NOT COMPLETE |

### Deployment parity

Staging liveness and readiness were HTTP 200. Readiness reported database ready and storage configured. Staging SHA parity remains `UNKNOWN`: `/api/health/version` returned 404 and `/version.json` returned SPA HTML instead of JSON. The certification branches contain metadata code, but deployed parity is not inferred from source presence.

### Known quality/debt items

Sonar still reports 22 critical/297 major frontend maintainability smells and 59 critical/401 major backend maintainability smells; no Sonar bugs, vulnerabilities or hotspots were reported. These are not silently certified as “perfect.” The Gitleaks triage found placeholders/historical development values only; no active production credential was identified. The exact triage is in `docs/testing/GITLEAKS-TRIAGE-2026-09-26.md`.

## Exact remaining blockers and ownership

### Souhail — application/code/PR

1. Keep the complete matrix and certification report attached to the root PR; do not claim 100%.
2. Resolve or obtain written scope decisions for the three direct channel adapters and the distinct automated badge review engine.
3. Document the signed contract amendment replacing Alibaba eKYC with Stripe Identity, or obtain a client decision for the contract row.
4. Preserve and review the remaining frontend/backend maintainability backlog; Sonar coverage is not a full production gate yet.
5. After merge, rerun the certification from the actual protected-main SHAs.

### Terrence — staging/server/provider configuration

1. Merge/deploy the reviewed release branches and expose/verify `/api/health/version` and JSON `/version.json`.
2. Configure private MinIO/S3 storage, TLS, service permissions, persistence, backup schedule, retention and restore evidence; run staging upload/download/restart/authorization checks.
3. Enable Brevo with verified sender/domain and controlled mailbox; verify outbox `SENT`, delivery, retry and duplicate behavior.
4. Provision controlled staging Admin/role access and replay role journeys without committing credentials.
5. Confirm Stripe test mode, Identity return URL/events/signature handling and safe payment/refund/Connect checks.

### External/manual certification

Human screen-reader/keyboard/reduced-motion/forced-color certification, physical camera/device testing, external provider deliveries and production operational recovery evidence remain outside what can be truthfully marked locally complete.

## Required final answers

M1 CONTRACT REQUIREMENTS: 45
M1 VERIFIED: 30
M1 LOCAL COMPLETENESS: 66.7% strict PASS (30/45)
M2 CONTRACT REQUIREMENTS: 17
M2 VERIFIED: 11
M2 LOCAL COMPLETENESS: 64.7% strict PASS (11/17)
M3 CONTRACT REQUIREMENTS: 17
M3 VERIFIED: 12
M3 LOCAL COMPLETENESS: 70.6% strict PASS (12/17)
M4 CONTRACT REQUIREMENTS: 16 in scope; 2 excluded/not applicable
M4 VERIFIED: 13
M4 LOCAL COMPLETENESS: 81.3% strict PASS (13/16)
M5 CONTRACT REQUIREMENTS: 25 in scope; 1 excluded
M5 VERIFIED: 14
M5 LOCAL COMPLETENESS: 56.0% strict PASS (14/25)
TOTAL CONTRACT REQUIREMENTS: 123 matrix rows; 120 in-scope rows
TOTAL VERIFIED: 80 strict PASS rows
TOTAL LOCAL COMPLETENESS: 66.7% strict PASS (80/120)
MISSING/PARTIAL/BROKEN: 4 missing, 17 partial, 7 configuration-blocked, 12 provider-blocked, 0 reproducible FAIL rows
UNEXPECTED AUTH 200S: NONE OBSERVED IN LOCAL NEGATIVE MATRIX
BACKEND FAILURES: 0
FRONTEND UNIT FAILURES: 0
PLAYWRIGHT FAILURES: 0; 8 explicit skips; 0 did-not-run
ALL IMPLEMENTED: NO
ALL LOCALLY WORKING END TO END: NO
100% LOCALLY VERIFIED: NO
READY FOR TERRENCE MERGE: YES — the certification package is review-ready; it is not a release approval
STAGING 100%: NO — deployed SHAs, real storage/email/provider and role replay are not fully evidenced
EXTERNAL PROVIDERS 100%: NO
PRODUCTION 100%: NO

## Final verdict

NestyStay is a credible local release candidate with green core automated suites and real local MinIO I/O, but it is not complete against every individually enumerated signed requirement. The exact outstanding work is identified above; no requirement is being hidden behind a generic “implemented scope” label.
