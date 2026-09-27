# NestyStay Full Gap Analysis

**Audit date:** 2026-09-27  
**Audit type:** forensic requirements-completeness audit only  
**Scope:** current approved NestyStay business rules, current source code, current tests, local runtime evidence, deployment/configuration evidence available in the repositories  
**Implementation policy:** the forensic baseline was audit-only; the subsequent autonomous remediation iteration corrected the confirmed standard-fee drift. No production/staging changes, historical migration rewrites, secrets, or protected-main bypasses were made.

## Executive conclusion

NestyStay is a substantial, working ASP.NET/React platform with broad M1–M5 coverage. The clean local verification evidence is strong: the backend suite passed 210/210 with local MinIO enabled, the frontend unit suite passed 128 tests, the frontend typecheck and production build passed, lint had zero errors, npm audit reported zero known vulnerabilities, and the complete local Playwright inventory started all 224 discovered tests with 213 passes, zero failures, and 11 explicit skips.

That evidence does not support a production-ready or requirements-complete verdict yet. The standard post-launch guest fee has now been corrected to the approved 10% rule across active backend logic, seeded pricebooks, the forward migration, frontend estimates/copy, and regression expectations. The remaining commercial-rule gap is the founding Gold/Platinum model, which still behaves as a lifetime flat-fee feature while the current amendment describes time-limited founding memberships, different host percentages, and additional commercial values that are not represented in the current pricebook. This is a wrong/outdated business-logic finding, not a missing-test finding.

The other material gaps are external or operational: Brevo delivery was not verified with a real configured transport; production/staging provider configuration and deployed SHA parity were not verified; real Stripe/Stripe Identity/Connect/InsuraGuest delivery was not proven; current Sonar results are historical rather than a fresh scan at these audit SHAs; the browser suite has 11 explicit scope/configuration skips; and Gate Guard has an enum and QR UI but no complete invitation, assignment, authenticated role boundary, or revocation lifecycle.

## Evidence and exact revisions

The audit used isolated clean worktrees so the user's dirty working workspace was not altered.

| Repository | Audited branch | Audited SHA | Remote | Main reference |
|---|---|---|---|---|
| Root/orchestration | `codex/final-release-certification` | `3526b9508e7a09d6a32c369903a87b9c71573518` | `https://github.com/souhailazz/nestystay-platform.git` | `76dfee9a4f25f3b83a8bdb2db3a0177ccdb73301` |
| Backend | `codex/final-release-certification` | `dfec3e9` (`fix(pricing): apply approved ten percent guest fee`) | `https://github.com/NestyStayJamaica/NESTY-STAY_Backend.git` | `9d13748a6fe60934ffcf51c02522b1a52794e2e8` |
| Frontend | `codex/final-release-certification` | `db2fbbc` (`fix(booking): align guest fee display with approved pricing`) | `https://github.com/NestyStayJamaica/NESTY-STAY_Frontend.git` | `510512375b613eb0ce5bf9ae39bf5d3f7042e008` |

The signed agreement was read from `docs/contracts/NestyStay-Signed-Agreement-April-2026.pdf`. Its recorded SHA is `0C4AAD0B1A2D015433C0875107DD171C93C4191281D7CCCBBE748B37DB4FD28`. The later handwritten business amendment takes precedence over conflicting typed values for this audit. The audit intentionally ignores developer/client milestone payment terms. Root audit artifacts were initially committed at `a44e664`; remediation state/history is committed separately after the backend/frontend pricing commits.

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

The matrix contains **87 audited requirement rows**: **50 COMPLETE**, **23 PARTIAL**, **1 FRONTEND_ONLY**, **0 BACKEND_ONLY**, **1 MOCK**, **8 MISSING**, **1 BROKEN**, **1 WRONG_LOGIC**, and **2 NEEDS_EXACT_VALUE**. These are requirement-row counts, not milestone percentages; a row marked PARTIAL may contain substantial working code with one important unverified or incomplete boundary.

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

The current code treats Gold and Platinum as `FoundingTier` values with a lifetime guest flat fee. `ResolveFoundingGuestFlatFee` returns Platinum `$29`, Gold `$36`, and Silver `$45`; `EfPhaseTwoStore` marks all non-Standard tiers as lifetime and transferable. The current amendment describes approximately `$150` founding membership values, approximately 18 months for Gold and 36 months for Platinum, approximately 2.9% and 2% host percentages, and booking references of `$36` and `$24`. The exact handwritten text is not represented as a machine-readable source in the repository, so any unclear sub-value remains `NEEDS_EXACT_VALUE`. The broad model mismatch is definitely `WRONG_LOGIC`: the current implementation does not model the stated term, membership price, tier-specific host commission, or expiry lifecycle.

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

- Gold/Platinum founding behavior is materially outdated versus the current amendment: lifetime flat guest fees replace the stated term-based membership model; current `$29/$36/$45` values are not the current amended commercial model; tier-specific host percentages and term expiry are not represented.
- External Stripe payment, renewal, refund-to-suspension, and real webhook delivery were not tested against a real configured account in this audit.
- All four badge states were seeded and exercised in local browser tests, but staging/live seeded records and admin access were not independently verified here.
- Historical/spec badge screens remain in the frontend repository and require consolidation before they can be considered removed technical debt.

**M2 status:** PARTIAL — core levels and local workflow are present; founding commercial logic is wrong/outdated and external financial lifecycle remains unverified.

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

- `UserRole.GateGuard` exists, QR scans accept an optional `GateGuardUserId`, and a `/gate` validator UI exists, but there is no complete Gate Guard invitation/provisioning/assignment path. QR validation endpoints are anonymous and rely on an optional signed-in actor rather than an authenticated Gate Guard authorization boundary. This is PARTIAL, not COMPLETE.
- Guest/provider camera scanning, physical gate hardware, external delivery, and a full guard account lifecycle were not proven.
- Map rendering uses stored latitude/longitude and an OpenStreetMap embed; no geocoding provider or address-to-coordinate workflow was found.
- No separate native QR/gate app was found.

**M4 status:** PARTIAL — directory and QR mechanics are present; Gate Guard role lifecycle and geocoding are not complete.

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

The code has registration/login/session records, token validation, 2FA, lockout/rate limits, admin bootstrap, role-aware route access, ownership checks, and automated authorization tests. The local Playwright admin fixture uses a disposable bootstrap path at runtime rather than a browser bypass. The remaining certification gap is breadth: a complete current role/IDOR matrix and staging role-account proof were not independently executed in this pass. Gate validation is a special concern because the QR validator is anonymous and does not enforce a Gate Guard role boundary.

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

- Solution test run with disposable local PostgreSQL and MinIO enabled: **210 passed, 0 failed, 0 skipped**.
- Project split from the completed run: Domain 5, Application 24, Infrastructure 32, API 149.
- This is local deterministic/provider-backed evidence. It is not live Stripe, Brevo, InsuraGuest, or staging evidence.

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
- Skipped: **11**.
- Did not run: **0**.
- Runtime: local PostgreSQL, local deterministic adapters, local file email behavior, local MinIO test container; not staging/live providers.

The 11 skips are explicit, not silent omissions. They consist of: the MinIO UI test guarded by `NESTYSTAY_MINIO_E2E=true`; an authenticated production smoke test guarded by runtime `SMOKE_EMAIL`/`SMOKE_PASSWORD`; and browser/project scope guards for mobile-only, Chromium-only, responsive Chromium, and role-matrix tests on projects where they are not applicable. The two configuration-dependent skips are meaningful release qualifications; the viewport/browser guards are intentional test scoping.

### SonarQube

No fresh Sonar analysis at the three audited SHAs was produced during this audit-only pass. Existing Sonar documents are historical evidence and must not be relabeled as current certification. The historical records report a prior frontend line result around 56%, a prior backend line result around 11.2% in one scan, and unresolved maintainability debt; the local Vitest run now shows 60.12% frontend line coverage, but this is not a Sonar Quality Gate result. A fresh scan, issue export, hotspot review, and current Quality Gate remain required.

### Deployment

The audited local revisions and repository main references are recorded above. The deployed staging frontend/backend SHAs were not exposed or independently matched in this audit. Health 200 responses, if observed, do not prove SHA parity. Current deployment status is therefore UNKNOWN for exact deployed-revision parity.

## Required current gaps, ordered by release impact

### P0 — do not certify production

1. Resolve the Gold/Platinum founding model mismatch and confirm any unreadable handwritten values before implementation; current lifetime flat-fee behavior is not the amended model.
2. Verify staging/live SHA parity for frontend and backend against the intended merged main revisions.
3. Configure and verify private production MinIO with TLS, least-privilege credentials, persistence, backup/retention, and application upload journeys.
4. Enable and verify Brevo with a controlled mailbox, including outbox `SENT`, receipt, retry, and duplicate behavior.

### P1 — release qualification

6. Run a fresh Sonar scan at the release SHAs and review all bugs, vulnerabilities, hotspots, and high/major maintainability findings.
7. Execute the two skipped configuration-dependent browser paths after staging credentials/providers are safely supplied.
8. Complete the staging role/IDOR matrix, especially owner/PM/staff/provider/wellness/document/payment cross-account denial.
9. Decide whether Gate Guard provisioning/authentication is current web scope. If required, the current enum/UI/anonymous validator is not enough.
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
| M2 Badges/Membership | PARTIAL | Four badges, eligibility, management, payments, renewals, and local tests exist; Gold/Platinum commercial logic is outdated and external payment lifecycle is unverified. |
| M3 Wellness | PARTIAL | Broad officer/visit/report/payout implementation and local tests exist; storage, email, external payment/insurance, and operational certification remain. |
| M4 Directories/Trust/QR | PARTIAL | Directory and QR flows exist; Gate Guard account lifecycle/auth boundary and geocoding are incomplete. |
| M5 Property Manager | PARTIAL | Broad PM backend/UI and local persistence tests exist; real storage/email/billing/payout/deployment and full manual responsive certification remain. |

## Release verdict

- **Local source implementation:** broad and materially functional.
- **Local automated verification:** strong; backend and frontend listed above are green, and the full browser inventory had no failures.
- **Current business-rule correctness:** NOT COMPLETE because founding-tier behavior still conflicts with the current amendment. The standard 10% guest fee is corrected.
- **External integration readiness:** NOT COMPLETE; Brevo, staging/live MinIO, real Stripe/Identity/Connect/insurance, and exact deployment SHA parity were not proven.
- **Professional QA readiness:** NO — provider/storage configuration and exact current commercial logic are still blockers.
- **Production readiness:** NO — do not promote based on local tests alone.

This audit did not change product code, migrations, production, staging, secrets, or branch-protection workflows. The next step is a scoped implementation pass against the P0/P1 findings, followed by fresh tests and provider/staging evidence.
