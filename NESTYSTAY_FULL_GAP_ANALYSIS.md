# NestyStay Full Gap Analysis

**Audit date:** 2026-09-28
**Audit type:** forensic requirements-completeness audit only  
**Scope:** current approved NestyStay business rules, current source code, current tests, local runtime evidence, deployment/configuration evidence available in the repositories  
**Implementation policy:** the forensic baseline was audit-only; the subsequent autonomous remediation iteration corrected the confirmed standard-fee drift. No production/staging changes, historical migration rewrites, secrets, or protected-main bypasses were made.

## Executive conclusion

NestyStay is a substantial, working ASP.NET/React platform with broad M1–M5 coverage. The current isolated certification heads have now been exercised with disposable services: backend `d2c8436` passed the complete unfiltered suite **303/303 with 0 failures and 0 skips** (Domain 6, Application 98, Infrastructure 32, API 167), including the current-head MinIO object I/O and PostgreSQL migration/concurrency checks. The frontend tracked-test suite passes **177 tests across 45 files** at `fe01b88`, with **64.41% local V8 line coverage** and **60.0% exact-head Sonar line coverage**; typecheck/build remain clean, and the current-head Playwright matrix started all 224 discovered tests with 213 passes, zero failures, 11 explicit skips, and zero did-not-run across desktop/tablet/mobile Chromium plus configured Firefox/WebKit smoke.

That evidence does not support a production-ready or requirements-complete verdict yet. The standard post-launch guest fee has now been corrected to the approved 10% rule across active backend logic, seeded pricebooks, the forward migration, frontend estimates/copy, regression expectations, and the root monorepo copies of those sources. The legible Gold/Platinum founding values are now implemented in backend `d2c8436`: Gold is `$150` for 18 months with a `$36` guest fee per booking and 2.9% host commission; Platinum is `$150` for 36 months with a `$24` guest fee per booking and 2% host commission. The ambiguous handwritten `150+150` allocation remains intentionally unresolved pending human business clarification. This is now a bounded commercial-clarification gap, not the former lifetime-fee implementation defect.

The other material gaps are external or operational: Brevo delivery was not verified with a real configured transport; production/staging provider configuration and deployed SHA parity were not verified; real Stripe/Stripe Identity/Connect/InsuraGuest delivery was not proven; fresh exact-head Sonar scans at backend `d2c8436` and frontend `fe01b88` report 62.7%/60.0% line coverage and 996/972 open maintainability issues respectively, with zero bugs/vulnerabilities/hotspots. The requested 80% backend target and maintainability target remain open; the frontend 60% overall Sonar target is met locally, while new-code coverage and live release-gate conditions remain unverified. The browser suite has 11 explicit scope/configuration skips; and Gate Guard still requires staging/browser/device qualification even though its local invitation, assignment, authenticated role boundary, revocation lifecycle, and legacy-route guard are implemented.

### Latest frontend coverage expansion — 2026-09-28

Frontend commit `f1f935b` adds behavioral coverage for the landing SearchBar and Property Manager module dispatcher/content. The complete local Vitest suite is **153/153 passed across 36 files**. V8 reports **62.84% lines**, **59.21% statements**, **50.80% branches**, and **55.93% functions**. Typecheck and production build pass; lint remains **0 errors / 107 existing warnings**. This clears the local 60% line target, but no new Sonar analysis is claimed for this test-only commit, and the existing Sonar/imported coverage and maintainability backlog remain separate release gates. Detailed evidence is in [`FRONTEND-COVERAGE-2026-09-28.md`](../../testing-evidence/15-coverage/FRONTEND-COVERAGE-2026-09-28.md).

The follow-up frontend commit `9ccfb75` adds behavioral coverage for the booking identity-verification handoff, including provider messaging, document selection, hosted-session opening, pending navigation, and request failure/loading behavior. The complete local Vitest suite is now **155/155 passed across 37 files** with **63.10% lines, 59.44% statements, 51.04% branches, and 56.14% functions**. Detailed evidence is in [`FRONTEND-COVERAGE-IDENTITY-2026-09-28.md`](../../testing-evidence/15-coverage/FRONTEND-COVERAGE-IDENTITY-2026-09-28.md). No new Sonar analysis is claimed for this test-only commit.

The latest frontend commit `e74c701` added behavioral coverage for booking invoice and receipt workflows, including authenticated downloads, print behavior, and receipt failure handling. The complete local Vitest suite passed **157/157 across 37 files** with **63.41% lines, 59.68% statements, 51.20% branches, and 56.27% functions**. The follow-up `20f199f` corrected only test typing; the full suite passed **157/157** again, with typecheck and production build passing. Detailed evidence is in [`FRONTEND-COVERAGE-BOOKING-2026-09-28.md`](../../testing-evidence/15-coverage/FRONTEND-COVERAGE-BOOKING-2026-09-28.md). No new Sonar analysis is claimed for these test-only commits.

### Coverage-runner follow-up

Backend commit `0e58c1f8b490a4ac3f9a905d4d8a14d44de845eb` introduced the reproducible disposable-service coverage workflow. The current head `d2c8436` adds booking/payment state-machine behavior and founding-term coverage and passed the complete configured disposable-service suite—Domain 6, Application 98, Infrastructure 32, API 167; **303 passed, 0 failed, 0 skipped**. The real MinIO object I/O check passed **1/1**, and PostgreSQL two-instance concurrency passed **1/1**. Fresh exact-head Sonar processing reports **62.7% line coverage**, 996 code smells, 20.3% duplication, and zero bugs/vulnerabilities/hotspots; the 80% coverage and maintainability targets remain unmet. Evidence: `testing-evidence/sonarqube/BACKEND-CURRENT-2026-09-28.json` and `testing-evidence/sonarqube/CURRENT-LOCAL-SCAN-2026-09-28.md`.

## Evidence and exact revisions

The audit used isolated clean worktrees so the user's dirty working workspace was not altered. The latest remediation also rejects the historical unscoped Gate Guard staff route rather than allowing it to create misleading legacy records, and aligns the root monorepo with the split backend's scoped Gate Guard lifecycle.

### Authorization/IDOR follow-up

The focused local matrix was rerun at backend `393edd1e20269350ed565fff8dda53b997792e39` and passed **13/13** with zero failures or skips. It exercises cross-resource message/attachment, wellness/officer-document, provider-private-data, owner/manager/staff portfolio, Gate Guard invite/scope/revocation, admin-only endpoint, Property Manager policy, badge ownership, and logout/session boundaries. This is local API evidence; staging role/IDOR verification remains required and is not inferred from it. Evidence: `testing-evidence/final-hardening/04-authorization/LOCAL-AUTHORIZATION-MATRIX-2026-09-27.md`.

| Repository | Audited branch | Audited SHA | Remote | Main reference |
|---|---|---|---|---|
| Root/orchestration | `codex/final-release-certification` | current branch tip reported in final status | `https://github.com/souhailazz/nestystay-platform.git` | `76dfee9a4f25f3b83a8bdb2db3a0177ccdb73301` |
| Backend | `codex/final-release-certification` | `d2c8436` | `https://github.com/NestyStayJamaica/NESTY-STAY_Backend.git` | `9d13748a6fe60934ffcf51c02522b1a52794e2e8` |
| Frontend | `codex/final-release-certification` | `fe01b88` | `https://github.com/NestyStayJamaica/NESTY-STAY_Frontend.git` | `510512375b613eb0ce5bf9ae39bf5d3f7042e008` |

The signed agreement was read from `docs/contracts/NestyStay-Signed-Agreement-April-2026.pdf`. Its recorded SHA is `0C4AAD0B1A2D015433C0875107DD171C93C4191281D7CCCBBE748B37DB4FD28`. The later handwritten business amendment takes precedence over conflicting typed values for this audit. The audit intentionally ignores developer/client milestone payment terms. Root audit artifacts were initially committed at `a44e664`; remediation state/history is committed separately after the backend/frontend pricing commits.

Certification branch revisions at this refresh: root `db5fc83`, backend `d2c8436`, frontend `fe01b88`. The backend configured Docker-backed suite is 303/303; the frontend tracked-test suite is 177/177 across 45 files. Exact-head Sonar is now recorded for these revisions: backend 62.7% line coverage / 996 smells and frontend 60.0% / 972 smells. The backend 80% target, maintainability acceptance, new-code conditions, and external/staging gates remain open.

### Root monorepo parity follow-up

The tracked root checkout was re-audited after the split-repository fee correction and still contained active 9% values in `backend/src/NestyStay.Application/Services/PricebookService.cs`, `backend/src/NestyStay.Infrastructure/Persistence/NestyStaySeed.cs`, `backend/tests/NestyStay.Infrastructure.Tests/SchemaModelTests.cs`, `frontend/src/features/public/PublicSearchMap.tsx`, and booking copy/comment files. These were corrected locally to the approved 10% rule. A second parity audit found that the root monorepo also lacked the split backend's scoped Gate Guard lifecycle and authenticated QR endpoint; those source and regression-test gaps are now aligned in root commit `20e6d9e`. The generated forward migration is `20260927072014_ApplyApprovedGuestFee`; historical migrations remain unchanged. Root verification after the combined correction is 186 backend tests passed, 48 focused frontend tests passed, 2 Gate Guard regression tests passed, typecheck/build passed, lint 0 errors / 84 warnings, and npm audit 0 known vulnerabilities.

## Architecture inventory

### Backend

- ASP.NET Core 10 API with Domain, Application, Infrastructure, and API projects.
- PostgreSQL 17 through EF Core/Npgsql; the repository contains a broad migration history and a large `NestyStayDbContext` model.
- Real provider abstractions exist for Stripe payments, Stripe Identity, MinIO/S3-compatible storage, Brevo email, local file email, and local deterministic test adapters.
- `MinioStorageProvider.cs` is a real S3 Signature V4 implementation with upload, download, presigned URL, bucket setup, SHA-256, object-key canonicalisation, and size enforcement.
- Durable email outbox and worker exist; Brevo is selected only when the provider and enable flag are configured.
- The application exposes health/integration status surfaces, but a healthy response is not by itself proof of deployed revision or real-provider delivery.

### Frontend

- React 19 + TypeScript + Vite with a manifest-driven route system.
- Current route tests cover 99 canonical screens and 57 aliases.
- Real public discovery, property, booking, traveler, host, badge, wellness, directory, admin, and Property Manager screens exist.
- `src/pages/SpecScreens.tsx` remains in the repository and is still used by a set of state/error/design routes. It is not automatically a defect, but it is a source of historical/fixture-style screens that must be distinguished from product workflows.
- No separate native iOS/Android application source was found in the audited repositories. “Mobile” evidence is responsive web/device coverage, not a native application.

## Classification rules

Each row in `NESTYSTAY_GAP_MATRIX.json` has exactly one status:

- `COMPLETE`: implemented and verified at the evidence level stated in the row.
- `PARTIAL`: a meaningful implementation exists but an important part of the requirement is missing or unverified.
- `FRONTEND_ONLY`: visible client surface exists without a complete backend capability.
- `BACKEND_ONLY`: backend capability exists without an adequate user-facing surface.
- `MOCK`: local, deterministic, fixture, file, or placeholder behavior rather than the required real integration.
- `MISSING`: no current implementation was found.
- `BROKEN`: the current implementation fails a reproducible local test or runtime path.
- `WRONG_LOGIC`: current behavior is implemented but conflicts with the current approved business rule.
- `NEEDS_EXACT_VALUE`: the implementation cannot be classified correctly until an unclear handwritten value/text is confirmed.

The matrix contains **87 audited requirement rows**: **52 COMPLETE**, **25 PARTIAL**, **0 FRONTEND_ONLY**, **0 BACKEND_ONLY**, **1 MOCK**, **6 MISSING**, **0 BROKEN**, **1 WRONG_LOGIC**, and **2 NEEDS_EXACT_VALUE**. These are requirement-row counts, not milestone percentages; a row marked PARTIAL may contain substantial working code with one important unverified or incomplete boundary.

## Current business-rule audit

### Booking economics

| Rule | Current code | Current approved rule | Finding |
|---|---|---|---|
| Host standard commission | 3% in `PricebookService.cs` and seed | 3% | COMPLETE |
| Standard guest platform fee | 10% in active `BusinessRules.cs`, `PricebookService.cs`, seed, forward migration, frontend estimate, and tests | 10% in the later handwritten amendment | COMPLETE |
| Standard booking hold | 60 minutes in `BusinessRules.cs` | The signed text gives 30 minutes as an example; the exact current operational value is not unambiguous | NEEDS_EXACT_VALUE |
| Trusted host fee | $49 one-time with annual-renewal capability in the current pricebook | Current source material supports a $49 Trusted commercial item | COMPLETE at current source value |
| Wellness subscription | $19 monthly in the current pricebook and seed | Current source material supports $19/month | COMPLETE at current source value |
| Stripe Identity vendor cost | $0.14 seed item | Current source material contains a $0.14 reference | COMPLETE at current source value |

### Founding Gold/Platinum logic

The current code now models the legible signed values: Gold `$150`, 18 months, `$36` guest fee per booking, 2.9% host commission; Platinum `$150`, 36 months, `$24` guest fee per booking, 2% host commission. The forward migration `20260928174138_ApplyFoundingMembershipTerms` adds the membership fields and pricebook rows without editing historical migrations. The handwritten `150+150` property-and-guest allocation is visibly ambiguous/scratched, so it remains `NEEDS_EXACT_VALUE`; no additional payment meaning was invented. The former lifetime-fee mismatch is fixed locally and covered by focused workflow, persistence, and API tests.

### Identity provider

Stripe Identity is the active provider in current configuration defaults and provider wiring. Alibaba references remain in historical migration/model history and prior audit documents, but no active customer-facing Alibaba adapter was found in the current non-migration source scan. The current approved provider rule is therefore represented by Stripe Identity; the historical references must not be deleted casually or treated as an active integration.

## Milestone 1 — Core Booking System

### Complete or locally verified

- Registration, password login, logout/session handling, 2FA enrollment/challenge, recovery, password reset, passwordless flows, Google login wiring, and protected route handling exist.
- Explore/search supports listing discovery, location/parish/title filtering, dates, guests, cards, badge filters, and sorting. Property detail includes gallery/media references, amenities, sleeping arrangements, rules, coordinates, availability, pricing, host information, and booking entry points.
- Quote and booking creation persist real booking records against local PostgreSQL. Verification-enabled bookings enter `PendingVerification` and hold dates; approval/rejection and guest-visible rejection reasons are implemented.
- Payment authorization/capture/refund/webhook/idempotency code exists and is covered by automated tests using local/deterministic provider paths. This is not proof of a real live Stripe account flow.
- Stripe Identity provider wiring and verification webhook mapping exist. Current local evidence does not prove a real external Stripe Identity session.
- Trips, booking detail, invoices/receipts, pending/identity/rejected/cancelled/success/failure states, and in-app booking notifications exist.
- Favorites/wishlist API and UI exist; map/list discovery uses stored coordinates and OpenStreetMap embed surfaces.

### Gaps and qualifications

- The standard guest fee correction is implemented and locally tested; historical migrations and historical audit documents still contain the old typed 9% value by design and were not rewritten.
- Real staging/live Stripe, Stripe Identity, Brevo, object-storage, payout, and InsuraGuest delivery were not verified in this audit.
- The full browser run had an explicit MinIO UI skip and a production authenticated-smoke skip; upload UI and live provider flows therefore remain externally/configuration-qualified.
- There is no geocoding service. Coordinates can be entered/stored and rendered on a map, but addresses are not geocoded by the application.
- SMS and push paths use local/development behavior in the audited configuration; external delivery was not proven.
- Hold duration is 60 minutes in code while the signed document mentions 30 minutes as an example. The required production value needs exact confirmation before it can be called correct.

**M1 status:** PARTIAL — locally implemented and strongly automated-test verified; the approved 10% guest fee is corrected, while external/runtime gates remain unverified.

## Milestone 2 — Badge and membership system

### Complete or locally verified

- FREE, VERIFIED, TRUSTED, and WELLNESS levels are represented in domain, seed, APIs, public profiles/cards, host surfaces, eligibility rules, benefits, admin management, expiration/suspension/renewal records, and authorization checks.
- Trusted eligibility is implemented as verified plus at least three qualifying bookings/reviews in `BusinessRules.cs`.
- Wellness eligibility requires verification, property address, and a completed wellness visit in the current code.
- Server-authoritative badge payment entities, PaymentIntent references/state, webhook/idempotency behavior, and frontend payment UI are present from the current M2 hardening work. Local automated tests cover core persistence and authorization behavior.
- The new automated badge-review engine and migration are present on the audited backend branch and its focused tests passed in the preceding implementation pass.

### Gaps and qualifications

- The ambiguous handwritten `150+150` Gold/Platinum allocation still needs explicit business confirmation before any additional payment semantics are added. The legible membership values and term-based expiry model are implemented and locally tested.
- External Stripe payment, renewal, refund-to-suspension, and real webhook delivery were not tested against a real configured account in this audit.
- All four badge states were seeded and exercised in local browser tests, but staging/live seeded records and admin access were not independently verified here.
- Historical/spec badge screens remain in the frontend repository and require consolidation before they can be considered removed technical debt.

**M2 status:** PARTIAL — core levels and local workflow are present; the legible founding terms are implemented and locally tested, while the ambiguous allocation and external financial lifecycle remain unverified.

## Milestone 3 — Wellness and officer services

### Complete or locally verified

- Officer onboarding/application fields, approval/rejection/suspension/reactivation, active/off-duty declarations, privacy-oriented badge identifiers, availability, officer search, quote/plans/subscriptions, visit booking, scheduling/assignment/conflict handling, rescheduling/cancellation, report records, photos/documents metadata, host/admin visibility, commissions, escrow/payout/dispute records, and role checks are represented in the backend and frontend surfaces.
- The signed-rule labels distinguish guest-facing “Private security / body guard” from owner-facing “Off-duty police officer”; the current code exposes those separate labels.

### Gaps and qualifications

- Upload-dependent officer documents/report photos require configured private object storage. Local MinIO I/O passed, but deployed storage configuration and application upload journeys were not verified end-to-end.
- Stripe Connect/payouts, Brevo notifications, insurance/InsuraGuest, and real external officer identity evidence were not tested against production-capable providers.
- Browser evidence is responsive web evidence; the native mobile/officer application described for a later phase is not in these repositories.
- Human privacy/accessibility certification and physical officer operational verification remain outside automated local evidence.

**M3 status:** PARTIAL — broad backend/UI implementation exists and local tests cover it, but provider-backed execution and operational certification are outstanding.

## Milestone 4 — Directories, trust, police/privacy, QR and gate

### Complete or locally verified

- Custodian, Trades, Local Business, and Police/Officer directory models, search/filter/category/parish fields, provider/profile lifecycle, reviews/ratings/moderation structures, and role-scoped screens exist.
- QR issue, validation, expiry, revoke, wrong-property rejection, scan history, and manual validator flows are implemented and tested locally.
- Gate communication and delivery-attempt records exist within the Property Manager API.
- Police-facing privacy rules are represented in officer data/UI patterns with badge identifiers and restricted document access; this still needs human/legal review before production certification.

### Gaps and qualifications

- `UserRole.GateGuard`, the existing staff membership tables, and `/gate` validator UI now form a locally tested invitation/acceptance/revocation path. Authenticated QR validation requires an active `GATE_GUARD` membership with the requested property in scope; Gate Guard users are denied PM finance/professional workspaces; the historical unscoped staff route rejects `GATE_GUARD`. The anonymous validator remains intentionally available for guest/pass compatibility. Staging/browser/device qualification is still outstanding, so this is locally implemented but not fully released.
- Guest/provider camera scanning, physical gate hardware, external delivery, and a full guard account lifecycle were not proven.
- Map rendering uses stored latitude/longitude and an OpenStreetMap embed; no geocoding provider or address-to-coordinate workflow was found.
- No separate native QR/gate app was found.

**M4 status:** PARTIAL — directory, QR mechanics, and the scoped Gate Guard lifecycle are locally implemented; staging/browser/device qualification and geocoding remain outstanding.

## Milestone 5 — Property Manager platform

### Complete or locally verified

- Manager dashboard and portfolio scoping, owner assignments/invitations/verification, property/owner boundaries, invoices/lines/bulk issue/overdue/payment/refund/retry/state records, statements/ledger, utility readings/charges/schedules/disputes, maintenance requests/activity/attachments/work orders, vendors/documents, community notices/comments/acknowledgements, governance proposals/discussion/voting/anonymous/proxy/revocation, documents/versions/archive/export, gate messages/QR lifecycle, subscriptions/events/retry, staff invitations, calendar, cleaning, inspections, reporting, and owner portal surfaces are represented in backend and frontend code.
- Backend tests cover a large part of Property Manager persistence, authorization, lifecycle, and export behavior. The Playwright matrix also covers role/device routes locally.

### Gaps and qualifications

- Production object storage is a hard dependency for real document/media/attachment upload and download. The local MinIO provider passed focused real-I/O tests, but staging/live MinIO configuration and application-level upload journeys were not verified here.
- Brevo transport, real billing, external payouts, external document delivery, and production backups/retention were not externally verified.
- Some screens still expose “staged”/demo wording and deterministic local workflows; those are not equivalent to a configured production provider.
- Responsive browser coverage exists, but human tablet/mobile usability certification for every operational table, dialog, and workflow is not complete.

**M5 status:** PARTIAL — broad operational implementation and local tests exist, but production-provider, storage, and manual usability gates remain.

## Cross-cutting audit

### Authentication, authorization, and session behavior

The code has registration/login/session records, token validation, 2FA, lockout/rate limits, admin bootstrap, role-aware route access, ownership checks, and automated authorization tests. The local Playwright admin fixture uses a disposable bootstrap path at runtime rather than a browser bypass. The remaining certification gap is breadth: a complete current role/IDOR matrix and staging role-account proof were not independently executed in this pass. Gate Guard now has a separate authenticated, property-scoped validator and is denied PM finance/professional workspace access; the anonymous validator remains for guest/pass compatibility.

### Storage and uploads

The MinIO provider is real code, not a fake adapter. The disposable local MinIO container was running and the focused real-I/O tests passed upload/download/authentication/persistence coverage; the full backend suite also passed with MinIO enabled. That proves the provider locally, not that production/staging has correct credentials, TLS, bucket policy, backup, lifecycle, or all application upload routes. Stripe Identity documents are expected to remain owned by Stripe; NestyStay should not copy them into its own bucket without an explicit data-governance decision.

### Email

The durable outbox, template catalog, worker, retry limit, idempotency key, file transport, and Brevo transport exist. This audit did not have a safe real Brevo API/mailbox configuration, so actual acceptance, mailbox receipt, provider retry behavior, and duplicate prevention through Brevo are BLOCKED_EXTERNAL. Business persistence is designed to occur before delivery, so a mail outage should not roll back the underlying booking/manager action; that behavior should still be verified on the configured staging provider.

### Maps and geocoding

Stored coordinates, OpenStreetMap embeds, public map/list UI, property detail map, and officer coverage-map links exist. There is no geocoder or map-key-backed address search. Map is therefore PARTIAL and geocoding is MISSING as a capability. The signed M1–M5 agreement does not establish geocoding as a core signed requirement; treat it as a separate scope decision unless the client adds it.

### Legacy/duplicate/fixture surfaces

- `SpecScreens.tsx` contains state/error/design and historical-style screens still imported by `App.tsx`.
- The route manifest intentionally retains 57 aliases, and tests verify their canonical ownership. These are not automatically bugs, but they create duplicate entry points and should not be mistaken for separate product implementations.
- Deterministic local adapters, demo seeds, `LocalDevelopmentSmsSender`, file email capture, local Connect/payout providers, and fallback storage URLs are valid for local testing but are not production provider evidence.
- Existing migrations and historical Alibaba references were not edited or deleted.

## Verification record

### Backend

- Solution test run with disposable local PostgreSQL and MinIO enabled: **213 passed, 0 failed, 0 skipped**.
- Project split from the completed run: Domain 5, Application 24, Infrastructure 32, API 151.
- This is local deterministic/provider-backed evidence. It is not live Stripe, Brevo, InsuraGuest, or staging evidence.
- The integration-health endpoint now reports the selected storage provider's readiness rather than assuming every provider is configured; this is local code evidence, not staging storage configuration evidence.

### Frontend

- Vitest: **128 passed across 31 files**.
- Local Vitest coverage: **60.12% line coverage** (`4548/7564`), 56.15% statements, 47.8% branches, 52.8% functions.
- Typecheck: PASS.
- Production build: PASS; Vite transformed 2,154 modules.
- Lint: PASS with **0 errors and 112 warnings**.
- `npm audit --audit-level=high`: **0 known vulnerabilities**.

### Browser regression

- Discovered: **224**.
- Started: **224**.
- Passed: **213**.
- Failed: **0**.
- Skipped: **11 explicit scope/configuration skips**.
- Did not run: **0**.
- Runtime: local PostgreSQL, local deterministic adapters, local file email behavior, local MinIO test container; not staging/live providers.

The 8 skips are explicit, not silent omissions. They are produced by browser/project scope guards for mobile-only, Chromium-only, responsive Chromium, role-matrix and provider-gated tests on projects where they are not applicable. The production authenticated-smoke path still requires runtime `SMOKE_EMAIL`/`SMOKE_PASSWORD`; no test was silently omitted and `did_not_run` is zero.

### SonarQube

Fresh current-head local SonarQube analyses were completed server-side for all three audited scopes and exported to `testing-evidence/sonarqube/`. Backend (`e215b895`) reported 0 bugs, 0 vulnerabilities, 0 hotspots, 988 code smells, 73.4% line coverage, and 20.4% duplication. Frontend (`e47ace7`) reported 0 bugs, 0 vulnerabilities, 0 hotspots, 972 code smells, 32.7% Sonar line coverage (32.81% local tracked-test V8 lines), and 1.4% duplication. Root runtime/orchestration (`2334432`) reported 0 current issues. The disposable Sonar instance had no configured acceptance conditions; coverage and maintainability remediation remain required before a production-ready verdict.

### Deployment

The audited local revisions and repository main references are recorded above. The deployed staging frontend/backend SHAs were not exposed or independently matched in this audit. Health 200 responses, if observed, do not prove SHA parity. Current deployment status is therefore UNKNOWN for exact deployed-revision parity.

## Required current gaps, ordered by release impact

### P0 — do not certify production

1. Confirm the ambiguous Gold/Platinum `150+150` allocation and whether it has any additional payment meaning; do not implement it without written business clarification.
2. Verify staging/live SHA parity for frontend and backend against the intended merged main revisions.
3. Configure and verify private production MinIO with TLS, least-privilege credentials, persistence, backup/retention, and application upload journeys.
4. Enable and verify Brevo with a controlled mailbox, including outbox `SENT`, receipt, retry, and duplicate behavior.

### P1 — release qualification

6. Run a fresh Sonar scan at the release SHAs and review all bugs, vulnerabilities, hotspots, and high/major maintainability findings.
7. Execute the two skipped configuration-dependent browser paths after staging credentials/providers are safely supplied.
8. Complete the staging role/IDOR matrix, especially owner/PM/staff/provider/wellness/document/payment cross-account denial.
9. Run the staging/browser Gate Guard journey and confirm the deployment uses the authenticated, property-scoped validator; physical gate/device integration remains separately unverified.
10. Verify Stripe Identity, Stripe payments/Connect, insurance, and refund/webhook paths against safe staging test-mode credentials.

### P2 — product completeness/technical debt

11. Decide whether geocoding is contractual scope; it is not present.
12. Consolidate or explicitly document the remaining SpecScreens/legacy duplicate surfaces and route aliases.
13. Replace local SMS/push and provider adapters before claiming real communications coverage.
14. Complete manual accessibility, reduced-motion, forced-color, screen-reader, and operational mobile certification.

## Final phase scorecard

| Phase | Status | Basis |
|---|---|---|
| M1 Core Booking | PARTIAL | Local implementation and automated/browser evidence are strong; the approved 10% guest fee is now implemented and tested; real provider/staging parity remains unverified. |
| M2 Badges/Membership | PARTIAL | Four badges, eligibility, management, payments, renewals, and local tests exist; legible Gold/Platinum founding terms are implemented, while the ambiguous allocation and external payment lifecycle remain unverified. |
| M3 Wellness | PARTIAL | Broad officer/visit/report/payout implementation and local tests exist; storage, email, external payment/insurance, and operational certification remain. |
| M4 Directories/Trust/QR | PARTIAL | Directory, QR, and scoped Gate Guard lifecycle/auth boundary are locally verified; staging/browser/device qualification and geocoding remain. |
| M5 Property Manager | PARTIAL | Broad PM backend/UI and local persistence tests exist; real storage/email/billing/payout/deployment and full manual responsive certification remain. |

## Release verdict

- **Local source implementation:** broad and materially functional.
- **Local automated verification:** strong; backend and frontend listed above are green, and the full browser inventory had no failures.
- **Current business-rule correctness:** NOT COMPLETE because the ambiguous founding allocation still needs business confirmation. The standard 10% guest fee and legible Gold/Platinum terms are corrected.
- **External integration readiness:** NOT COMPLETE; Brevo, staging/live MinIO, real Stripe/Identity/Connect/insurance, and exact deployment SHA parity were not proven.
- **Professional QA readiness:** NO — provider/storage configuration and exact current commercial logic are still blockers.
- **Production readiness:** NO — do not promote based on local tests alone.

This audit did not change product code, migrations, production, staging, secrets, or branch-protection workflows. The next step is a scoped implementation pass against the P0/P1 findings, followed by fresh tests and provider/staging evidence.
