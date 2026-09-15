# NestyStay M1–M5 final gap audit

Audit date: 2026-09-15  
Audit basis: current release-candidate worktrees and the clean local runtime evidence already produced in this task. This document supersedes older percentage-based summaries; it does not claim that protected `main` contains the candidate until the open PRs are approved and merged.

## How to read this audit

The forensic classifications below are intentionally exact:

- **FULLY IMPLEMENTED** — current production code has the API, UI, persistence, authorization and local test path.
- **PARTIALLY IMPLEMENTED** — the local application path exists, but a required surface or verification boundary remains.
- **BROKEN** — an expected current path fails in a reproducible way.
- **IMPLEMENTED BUT NOT VERIFIED** — code is present but the required evidence has not been completed.
- **EXTERNAL PROVIDER BLOCKED** — completion requires credentials, an external account, deployment infrastructure or a real provider callback.
- **MANUAL HUMAN VERIFICATION REQUIRED** — automation is insufficient for the remaining acceptance claim.
- **OUTSIDE SIGNED SCOPE** — explicitly excluded optional work is not a release defect.

Evidence used: clean local PostgreSQL database with current migrations and deterministic seed, local backend health (`/api/health/live` and `/api/health/ready` returned 200), backend 167/167 tests, frontend 48/48 unit tests, passing typecheck/build, lint with 0 errors, and the final configured Playwright matrix with 182 passed, 0 failed and 9 intentional skips. The earlier tablet timeout and mobile visual drift were isolated and the complete rerun passed.

## M1 — Core booking system

### Forensic matrix

| Requirement group | Classification | Backend/API | Frontend | Database | Browser | Staging | Evidence / exact gap / fix |
|---|---|---|---|---|---|---|---|
| Registration, login, logout, cookie session, role authorization, 2FA, recovery, password reset, passwordless login, Google OAuth | FULLY IMPLEMENTED | Auth/session controllers, TOTP/recovery and role guards are present and tested | Login, 2FA, recovery, reset and role routes are connected | Session, auth-attempt, recovery and identity records persist | Auth/role journeys passed in the configured matrix | Client reported Google OAuth E2E working on staging | No active local code gap. Production OAuth client/domain and staging recheck remain deployment work. |
| Explore search, parish/title/location, dates, guests, filters, sorting, pagination, favorites, list/map, loading/empty/error/retry | FULLY IMPLEMENTED | Property/search/availability/favorite APIs are real and scoped | `PublicSearchMap` and property routes use API data and persistent save actions | Property, availability and favorite data are persisted | Desktop/tablet/mobile discovery checks passed | `/api/properties` reported 200 by client | Live map/geocoding tiles remain provider-configurable; local manual fallback is intentional. |
| Property details, gallery, amenities, beds/sleeping arrangements, house rules, coordinates, reviews, host badge, verification, cancellation and pricing | FULLY IMPLEMENTED | Property detail and quote data are server-backed | Detail page, gallery and booking CTA are connected | Listing/photos/amenity/review/price fields persist | Property and listing lifecycle journeys passed | Public API health only; full staging route recheck remains | No current code gap found. |
| Quote, date/guest validation, hold/expiry, overlap/concurrency protection, pending booking, host approval/rejection, stored reason, guest message, cancellation | FULLY IMPLEMENTED | `BookingsController` and persistence stores enforce server rules and idempotency | Booking modal/status/trips screens reflect backend states | Booking history, holds, rejection reason and payment references persist | M1 booking and rejection flows passed on clean local DB | Staging end-to-end booking not independently verified in this environment | No current reproducible code defect. |
| Payment authorization/capture/refund, webhooks, duplicate/replay protection, invalid signature, receipts/invoices | FULLY IMPLEMENTED | Stripe adapter plus local deterministic fallback, webhook/idempotency paths and refund handling are tested | PaymentElement/status/success/failure/refund states are connected | Payment attempts, references and ledger state persist | Local/test payment flows and API tests passed | Client reported Stripe test webhook 202; live mode remains unverified | Live Stripe keys, Connect onboarding, webhook signature and real provider events remain external. |
| Stripe Identity session, return flow, processing/verified/failure states, webhook correlation and secure handling | FULLY IMPLEMENTED | `StripeIdentityProvider` is the only active provider; unsupported providers throw | Booking identity flow and status handling are connected | Verification transaction/provider/status metadata persist | Deterministic local Stripe Identity path passed where covered | Live Stripe Identity session not verified | Add client live credentials/return URL/webhook events, then execute real staging session. Alibaba is not active code. |
| Notifications, unread count, mark read/all, booking/payment/rejection/identity deep links | FULLY IMPLEMENTED | Persisted notification APIs and role scoping exist | Inbox/deep-link behavior is connected | Notification/read state persists | Notification checks passed in the configured matrix | Provider delivery not independently verified | Brevo/SMS/push delivery and production worker operations remain external. |
| Desktop/tablet/mobile and automated accessibility | FULLY IMPLEMENTED LOCALLY | Responsive backend-independent | Responsive layouts, focus/live-region/reduced-motion/forced-color paths exist | N/A | Final matrix 182/191 passed with 0 failures; nine documented intentional skips | Staging device certification not done | Human screen-reader, manual keyboard, forced-color and reduced-motion certification remain required. |

### M1 exact remaining items

1. Complete one clean 191-test Playwright run with no transient failures. Complexity: **SMALL** test stability/infrastructure.
2. Run a real Stripe Identity session and signed webhook on staging. Complexity: **MEDIUM**, external credentials/configuration.
3. Validate live Stripe payment/refund/Connect behavior and production webhook signature. Complexity: **LARGE**, external account/onboarding.
4. Configure and verify Brevo/SMS/push delivery if those channels are in launch scope. Complexity: **MEDIUM**, external credentials/operations.
5. Obtain human screen-reader, keyboard, reduced-motion and forced-color sign-off. Complexity: **MEDIUM**, manual verification.

No active M1 backend, frontend or database code blocker was found in the tested local paths.

## M2 — Badge system

### Forensic matrix

| Requirement group | Classification | Backend/API | Frontend | Database | Browser | Staging | Evidence / exact gap / fix |
|---|---|---|---|---|---|---|---|
| FREE, VERIFIED, TRUSTED and WELLNESS definitions, labels, colors, benefits and restrictions | FULLY IMPLEMENTED | Typed pricebook, badge catalog and feature gates | Shared badge rendering on cards, detail, profiles and dashboards | Seeded definitions and lifecycle records persist | Four-level API/UI checks passed | Live badge records not independently checked on staging | No active code gap. |
| Eligibility, prerequisites, host ownership, verification/property/subscription dependencies and recalculation | FULLY IMPLEMENTED | Server-authoritative eligibility and authorization | Progress/checklist and locked/unlocked states are connected | Eligibility and audit state persist | Backend authorization/pricing tests and browser badge journeys passed | Staging data/config not independently checked | No active code gap. |
| Assignment, upgrade, expiry, renewal, suspension, reactivation, history and audit | FULLY IMPLEMENTED | Lifecycle endpoints/jobs/history/audit are present | Host/admin lifecycle screens are connected | Status/history/payment records persist | Phase 2 and admin mutation tests passed | Production worker timing not checked | Production scheduler/monitoring remains deployment verification. |
| Badge purchase PaymentIntent, server amount/currency, persistent record, idempotency, processing/failure/cancel/refund and webhook confirmation | FULLY IMPLEMENTED | `EfBadgePaymentStore` and Stripe payment lifecycle are server authoritative | PaymentElement flow has no frontend `PaymentSucceeded` trust flag | Provider reference, amount, currency and status persist | Payment/idempotency/refund tests passed | Live Stripe PaymentIntent/webhook not verified | Client must test live/test account behavior and Connect/payout policy. |
| Admin search/select, grant/suspend/renew/review, ownership and audit | FULLY IMPLEMENTED | Admin authorization and mutation endpoints exist | Admin badge management route uses real API | Audit/history rows persist | Admin mutation and browser checks passed | Staging admin permission check pending | No active code gap. |
| Desktop/tablet/mobile visual verification of all levels | IMPLEMENTED BUT NOT VERIFIED | N/A | Responsive surfaces exist | N/A | Representative browser coverage passed; no complete manual visual certification for every badge state | Not done | Capture/review all four states on all configured devices and complete manual accessibility review. |

### M2 exact remaining items

1. Test Stripe-backed badge PaymentIntents and webhook replay against the configured staging account. Complexity: **MEDIUM**, external credentials.
2. Confirm production renewal/expiry workers and monitoring. Complexity: **MEDIUM**, deployment verification.
3. Complete all-level desktop/tablet/mobile visual and human accessibility review. Complexity: **MEDIUM**, manual verification.
4. Resolve the commercial proration/grace-period decision if the client wants it fixed rather than configurable; it is not specified in the signed agreement. Complexity: **MEDIUM**, client decision.

No active M2 backend, frontend or database blocker was found in the tested local paths.

## M3 — Wellness services

### Forensic matrix

| Requirement group | Classification | Backend/API | Frontend | Database | Browser | Staging | Evidence / exact gap / fix |
|---|---|---|---|---|---|---|---|
| Officer onboarding, documents, eligibility, availability, approval/rejection/suspension/reactivation and privacy | FULLY IMPLEMENTED | Officer lifecycle, document, approval and audit APIs are persisted and scoped | Three-step onboarding, resume, checklist, privacy and status UI are connected | Officer/application/document/audit records persist | M3 lifecycle passed desktop/tablet/mobile | External document/storage delivery not checked | Production storage and provider delivery remain external. |
| Wellness service types, quote, plans/subscription, included visits, property/host/parish association | FULLY IMPLEMENTED | Quote, subscription and eligibility APIs are server-backed | Host service/plan/booking flow is connected | Subscription, visit and quote state persists | M3 lifecycle and enhancement journeys passed | Staging not independently verified | No local code gap found. |
| Scheduling, assignment, conflicts, rescheduling, cancellation and visit states | FULLY IMPLEMENTED | Availability, assignment and contention rules are enforced | Host/officer/admin operational screens are connected | Visit timeline and history persist | Targeted M3 lifecycle passed 3/3 viewports | Staging operational workflow not checked | No local code gap found. |
| Reports, photo upload/compression/retry/offline draft, host/admin visibility and acknowledgement | FULLY IMPLEMENTED | Validated upload/report/status APIs and scoped visibility exist | Officer report and host/admin report screens are connected | Report/photo/history state persists | M3 report journey passed where covered | Production object storage not verified | Production bucket/lifecycle/retention remains external. |
| Commission, payout states, disputes and idempotency | FULLY IMPLEMENTED | Local ledger/payout desk and audit state are present | Payout/status views are connected | Payout/commission records persist | Local API/browser coverage passed | Real bank/Connect payout not verified | Stripe Connect/bank onboarding and dispute rails are external. |
| Notifications/reminders and responsive accessibility | PARTIALLY IMPLEMENTED | Queued notification state exists | Operational notification surfaces exist | Notification state persists | Representative responsive/axe checks passed | Brevo/SMS/push delivery not verified | Production delivery, scheduler and human accessibility certification remain. |

### M3 exact remaining items

1. Verify real Stripe Connect/bank payout and dispute handling. Complexity: **LARGE**, external provider/onboarding.
2. Configure/test Brevo and any SMS/push reminder channels. Complexity: **MEDIUM**, external provider.
3. Verify production object storage for private photos/reports and retention. Complexity: **MEDIUM**, deployment/provider.
4. Complete manual accessibility and full mobile operational certification. Complexity: **MEDIUM**, human verification.

No reproducible local M3 code failure was found in the covered lifecycle.

## M4 — Directories, trust, QR and gate

### Forensic matrix

| Requirement group | Classification | Backend/API | Frontend | Database | Browser | Staging | Evidence / exact gap / fix |
|---|---|---|---|---|---|---|---|
| Custodian, Trades, Local Business and Police directories, search/filter/sort/pagination/profile/rating/contact | FULLY IMPLEMENTED | Live directory/provider APIs, privacy gates and filters exist | Directory routes and provider profiles are API-backed | Provider/profile/review/history data persists | M4 enhancement and route journeys passed | Staging directory data not independently checked | No active local code gap. |
| Provider onboarding, document vault, moderation approve/reject/request-changes, reason and audit | FULLY IMPLEMENTED | Moderation mutations, scoped documents and audit history persist | Admin queue reflects API state | Provider status/documents/audit persist | Provider moderation browser/API tests passed | Staging moderation not checked | No active local code gap. |
| Police privacy, badge ID, in-platform boundary and 119 messaging | FULLY IMPLEMENTED | Role/privacy rules are tested | Police UI exposes badge-safe data and emergency action | Provider privacy state persists | Police route and privacy checks passed | Staging policy review still required | No local code gap found. |
| QR issue/validate/expire/revoke, wrong-property rejection, history, manual fallback and gate communication | FULLY IMPLEMENTED | Hashed token lifecycle and scan history are persisted | Camera/manual guard and status states are connected | QR/scan/revoke/history persist | QR lifecycle and guard journeys passed across configured viewports | Physical scanner/device deployment not checked | Browser/device camera certification remains. |
| Map/list, selected marker, geocoding, loading/failure and mobile usability | PARTIALLY IMPLEMENTED | Coordinates and map/list API state exist | Map board/manual fallback exists | Coordinates persist | List/map behavior tested locally | Real tile/geocoder network not verified | Configure the chosen map/geocoder provider and test rate limits, tiles and marker behavior. Complexity: **MEDIUM** external. |

### M4 exact remaining items

1. Configure and verify the selected production map/geocoder provider. Complexity: **MEDIUM**, external provider.
2. Complete physical/mobile camera and accessibility certification. Complexity: **MEDIUM**, manual/device verification.
3. Verify production gate message delivery if external SMS/email is required. Complexity: **MEDIUM**, external provider.

No reproducible local M4 moderation, directory or QR code failure was found.

## M5 — Property Manager platform

### Forensic matrix

| Requirement group | Classification | Backend/API | Frontend | Database | Browser | Staging | Evidence / exact gap / fix |
|---|---|---|---|---|---|---|---|
| Portfolio dashboard, owner/property scoping, KPIs, filters and owner portal | FULLY IMPLEMENTED | Role-scoped dashboard and portfolio APIs | `/pm/*` and owner routes use cookie sessions and live data | Manager/owner/property relationships persist | Dedicated M5 workflows and responsive checks passed | Staging PM scope not independently verified | No active local code gap. |
| Owners, invitations, verification, assignment, history and owner views | FULLY IMPLEMENTED | Invite/review/assignment endpoints enforce scope | Manager/owner controls are connected | Owner assignments/history persist | Owner journey and authorization tests passed | Staging email/invite delivery pending | Brevo/SMTP delivery remains external. |
| Invoices, lines, bulk issue, overdue, partial/full payments, refunds, retries, receipts and statements | FULLY IMPLEMENTED locally | Server totals, idempotency and local payment ledger exist | Finance/statement controls are connected | Invoice/payment/refund/journal state persists | M5 finance and isolation checks passed | Live billing/payment not verified | Stripe live payment/Connect/reconciliation remains external. |
| Utilities readings, schedules, charges, anomalies, disputes and currency | FULLY IMPLEMENTED | Durable recurring worker, ISO currency and history are implemented | Utility workspace is connected | Readings/charges/history persist | Professional completion API/browser evidence passed | Production worker operations not checked | Deployment monitoring and evidence workflow remain. |
| Maintenance, vendors, quotes, approvals, work orders, evidence, readiness and history | FULLY IMPLEMENTED locally | Transition guards, quote/cost lines, attachments, corrective work and journal idempotency exist | Kanban/list/detail controls are connected | Maintenance/vendor/work-order/history records persist | M5 lifecycle and professional completion journey passed desktop/tablet/mobile | Production storage/delivery not checked | No local code gap found; full manual operations review remains. |
| Community notices, governance, anonymous/proxy voting and audit | FULLY IMPLEMENTED locally | Eligibility, quorum, privacy, proxy conflict/revoke and audit rules exist | Community/governance controls use live APIs | Ballots/proxies/results/history persist | Governance and owner portal checks passed | Staging communication not checked | External delivery and human policy sign-off remain. |
| Documents upload/version/archive/download/export/expiry | FULLY IMPLEMENTED locally | MIME/magic-byte/size validation, scoped download and async export exist | Document workspace and recovery states are connected | Metadata, versions and export state persist | Document workflow passed locally | Production object storage not verified | Configure production S3/MinIO/R2 bucket, keys, lifecycle and retention. |
| Gate/QR, staff/team/RBAC, calendar, cleaning/inspection, reporting and subscription lifecycle | PARTIALLY IMPLEMENTED | Current API/UI paths and local persistence exist; external calendar feeds are separately gated | Responsive operational surfaces exist | Operational records/history persist | Representative M5 browser checks passed | Production integrations not checked | Complete route-by-route manual certification and external calendar/billing/provider verification. Do not call external iCal/channel sync complete merely because the calendar page exists. |

### M5 exact remaining items

1. Complete route-by-route manual desktop/tablet/mobile review of every operational control, especially finance, maintenance, documents, governance and staff scope. Complexity: **LARGE**, manual verification.
2. Verify production object storage, private download URLs, retention and export recovery. Complexity: **MEDIUM**, external deployment.
3. Verify live subscription billing, invoices/payments and Connect/bank payout if required. Complexity: **LARGE**, external provider.
4. Verify real email/SMS/push delivery, retry/dead-letter operations and scheduled workers. Complexity: **MEDIUM**, external provider/operations.
5. Verify any contractually required iCal/channel synchronization, conflict handling and sync logs. Complexity: **LARGE**, only if the signed scope requires external synchronization.

### M5 outside signed scope

Smart-meter hardware integration, bank reconciliation, native mobile applications and Jamaican statutory trust-accounting certification are not release blockers unless separately contracted.

## Cross-cutting dead/fake implementation audit

- Active M1–M5 production routes use real API-backed screens in the current candidate.
- Frontend source contains no `PaymentSucceeded` control flag.
- Active backend source/config has no Alibaba provider wiring; the only accepted identity provider is Stripe Identity. Old `AlibabaCloud` values in immutable migration designer snapshots are historical schema snapshots, not runtime adapters.
- The remaining `mock`/`sample`/`legacy` matches are concentrated in tests, fixtures, historical/spec artifacts or copy; they were not used as evidence of production workflows.
- Native browser `alert`/`confirm`, broad historical duplicate screen implementations and some older operational warnings still merit cleanup if they are on a launch route, but no reproducible failure was found in the tested canonical routes. This is a code-quality follow-up, not silently marked complete.

## Provider matrix

| Provider | Feature | Local mode | Staging mode | Configured/tested | Missing credential/code/webhook | Production-ready? |
|---|---|---|---|---|---|---|
| Stripe Payments | Booking, badge, PM billing, refunds | Deterministic local adapter plus Stripe-compatible lifecycle | Client reported Stripe test webhook 202 | Local API/tests passed; staging webhook reported working | Live publishable/secret keys, Connect onboarding and final webhook signature test | NO |
| Stripe Identity | Guest/host identity | Deterministic local result path and Stripe Identity boundary | Not independently verified in this task | Local path covered; real session missing | Live secret, return URL, Identity events/signature validation | NO |
| Stripe Connect | Host/officer/PM payouts | Provider-independent persisted payout states | Not independently verified | Local payout state tested | Connected-account onboarding, bank verification and payout events | NO |
| Brevo/SMTP | Email/outbox | File provider and queued outbox | Client still listed Brevo as outstanding | Local queue/templates tested; delivery not | Brevo API key, sender/domain and worker config | NO |
| MinIO/S3/R2 | Private uploads/downloads | Local storage abstraction | Client listed MinIO as outstanding | Local upload/scoping tested | Bucket, keys, lifecycle and retention | NO |
| InsuraGuest | Insurance | No active production credential verified | Client listed outstanding | No live certification | Provider account/API/webhook if required by contract | NO / external |
| Google OAuth | Login | Configurable OAuth path | Client reported real staging login working | Staging reported working; local client setup not independently repeated | Authorized origins/redirects per environment | STAGING REPORTED, LIVE RELEASE RECHECK REQUIRED |
| SMS/Web Push | Reminders/notifications | Safe local adapter/queued state | Not independently verified | Local state tested | Provider credentials, VAPID/sender configuration and operations | NO |
| Map/geocoder | Discovery/maps | Coordinates/manual fallback | Not independently verified | Local list/map UI tested | Selected provider key/tiles/geocoding/rate limits | NO |

## Local verification summary

- Clean local PostgreSQL database was recreated safely for development, all current migrations applied, deterministic seed loaded, and backend health/readiness returned 200.
- Backend full unfiltered suite: 167 passed, 0 failed, 0 skipped (Domain 5, Application 23, Infrastructure 21, API 118).
- Frontend: 48 unit tests passed; typecheck passed; production build passed; lint 0 errors / 92 warnings; npm audit reported 0 vulnerabilities.
- Browser: 191 scheduled; 182 passed, 0 failed, 9 intentional skips. The nine skips are documented in `LOCAL-M1-M5-COMPLETION.md` and are either external-fixture, deployed-credential or viewport-scoped checks.
- Automated axe representative/full reachable-screen assertion passed. Human screen-reader, keyboard, reduced-motion and forced-color certification is not claimed.
- Security: active Alibaba scan clean, frontend `PaymentSucceeded` scan clean, backend/frontend dependency audits clean, no real secrets committed. Negative auth traces in logs are expected test evidence.

## Intentional Playwright skips

1. MinIO browser certification — 3 Chromium viewport cases — skipped because `NESTYSTAY_MINIO_E2E=true` and external storage fixture were not configured; external dependency.
2. Deployed authenticated smoke login — 3 Chromium viewport cases — skipped because `SMOKE_EMAIL` and `SMOKE_PASSWORD` were not supplied; deployment credential dependency.
3. Mobile-only navigation case — 2 desktop/tablet cases — intentionally viewport-scoped, not a missing feature.
4. Tablet role-authorization case — 1 case — intentionally certified only on desktop/mobile; not an external blocker.

## Git and release state

The latest intended code is committed and pushed on feature/release branches, but protected `main` has not been bypassed:

| Repository | Protected main | Candidate / required branch | PR | State |
|---|---|---|---|---|
| Root/orchestration | `146cde38ebe49802f2405fedc1fa36434f15403c` (empty remote orphan) | `codex/root-main-integration` `cfb73da87393adc38ece031582ba76f301e0ee34` | [#5](https://github.com/souhailazz/nestystay-platform/pull/5) | Open; clean; client approval required |
| Backend | `8a248f2070c4acd4b232192458b2f7ab9487e5a9` | `codex/m1-m2-runtime-hardening` `31a92cf8792958d54928d8a6232ccdc502d837c4` | [#2](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/pull/2) | Open; CI pass; merge blocked by approval |
| Frontend | `6a21adce06d275f3c08b35ee2f9c587cb7b9de9f` | `codex/m1-m2-runtime-hardening` `e3893730ddd2e365b17481b3a25a5145f11aa2ca` | [#1](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/1) | Open; CI pass; merge blocked by approval |

All four active worktrees are clean. Root’s protected remote `main` is an empty orphan, so the root PR is intentionally based on that remote and restores the exact release tree; this is why it must be reviewed rather than “merged” locally by rewriting history.

## Exact route to 100%

1. Terrence reviews and merges root PR #5, backend PR #2 and frontend PR #1 through branch protection.
2. Fetch the resulting main SHAs and rerun the clean migration/seed, backend suite and frontend/browser suite from those actual main checkouts.
3. Configure Stripe live/test Identity credentials, return URL, required Identity webhook events/signature verification, Stripe Connect and payment webhook verification.
4. Configure Brevo, storage, SMS/push, map/geocoder and InsuraGuest where those are launch requirements; verify delivery/failure/retry paths.
5. Run staging role-by-role M1–M5 smoke tests against the deployed SHAs.
6. Complete human accessibility, device/camera and operational usability sign-off.
7. If required by the signed scope, complete external iCal/channel synchronization certification and logs.

There is no currently identified true local code blocker for the covered M1–M5 workflows. The release is not 100% complete because protected-main consolidation, live provider/staging verification, external fixture delivery, and human certification remain outstanding.
