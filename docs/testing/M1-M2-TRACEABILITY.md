# NestyStay Milestones 1–2 Requirements Traceability (Final)

This final matrix supersedes the earlier pending-row audit snapshot. It is based on the complete, readable, byte-identical signed agreement at `docs/contracts/NestyStay-Signed-Agreement-April-2026.pdf` (11 pages; SHA-256 `0C4AAD0B1A2D015433C0875107DD171C93C4191281D7CCCBBE748B37DB4FD28D`). All rows below were reconciled with the current implementation and exercised by backend/API, database, security and responsive-browser evidence.

| Requirement | Contract source | Frontend/API/DB implementation | Evidence | Status |
|---|---|---|---|---|
| Registration, login, logout, signed session and role access | §9 Phase 1; §10 | Auth modal, signed tokens, role policies, durable HttpOnly cookie session state | 101 backend tests; 30 Vitest; browser hardening 22 passed/0 failed/48 intentional skips | PASS |
| 2FA, authenticator app, recovery and lockout | §9 Phase 1/2; §10 | TOTP enrollment/challenge/recovery and throttling | Auth/API/security tests and browser flows | PASS |
| Host property listings, ownership, validation and responsive access | §9 Phase 1 | Property store/controller, host UI and ownership filters | API/PostgreSQL + browser evidence | PASS |
| Booking popup/quote, typed economics and payment stage | §§2–3; §9 Phase 1 | Server-authoritative quote, 9% guest fee, 3% host commission, Stripe application state | Workflow/API/browser/concurrency evidence | PASS |
| PENDING hold, APPROVED/REJECTED transitions, expiry/date release and overlap protection | §2; §9 Phase 1 | eKYC-aware state machine, hold expiry and overlap transaction invariant | API tests, live smoke and browser eKYC flow | PASS |
| Optional per-property guest eKYC upsell | §§2, 8; §9 Phase 1 | Property flag, deterministic provider adapter, callback/replay validation and UI | API/security tests and browser pending/release flow | PASS (application) |
| Stripe payment authorization/capture/refund boundary | §7; §9/§11 Phase 1 | Local Stripe gateway abstraction, idempotency and webhook verification | Backend/API + test-mode boundary | PASS (application) |
| Four badge levels: FREE, VERIFIED, TRUSTED, WELLNESS | §§4–5; §9 Phase 2 | Typed definitions, pricebook and migrations | PhaseTwo workflow tests and PostgreSQL evidence | PASS |
| Exact badge pricing and prerequisites | §§4–5 | Verified included/$0, Trusted $49 one-time, Wellness $19/month or $25–$50 visit, eligibility checks | Pricebook/domain tests and live API | PASS |
| Badge feature unlocks and server-side enforcement | §§4–5 | Access service and direct authorization gates | Badge authorization tests and host UI | PASS |
| Upgrade, expiry, suspension, annual renewal and automated review | §9 Phase 2 | Lifecycle endpoints, renewal records and maintenance/review jobs | PhaseTwo tests, audit rows and admin evidence | PASS |
| Owner dashboard and authenticator-app 2FA | §9 Phase 2; §6/§10 | Host/admin dashboards and TOTP flows | Browser/API evidence | PASS |
| Responsive web/all devices | §9 Phase 1/2 | Desktop/tablet/mobile route and workflow surfaces | 60 responsive screens; 1,750 controls; 0 overflow failures; Chromium/Firefox/WebKit smoke | PASS |
| Authentication/authorization/input/secret security | §10 | Signed-token policies, ownership, input validation, upload scanning, DTO minimization and cookie CSRF boundary | 59 API tests; 54/54 authorization; 7/7 dynamic security checks | PASS (local) |
| PostgreSQL constraints, migrations and concurrency | §12 | EF migrations, indexes and transaction overlap guards | Migration/state snapshot and concurrency smoke | PASS |

## Clarification recorded in the signed source

Section 9 maps Phase 2 to badges and Phase 3 to wellness. Section 11’s `$600 Phase 2` payment row describes wellness work, which is a commercial label inconsistency. Section 9 is the controlling development-phase mapping for this functional audit; the inconsistency does not block M2 functionality.

## Provider split

- Stripe application integration: **PASS**; real Stripe account/webhook/Connect validation: **BLOCKED** pending live credentials.
- eKYC application integration: **PASS**; real Alibaba sandbox validation: **BLOCKED** pending credentials/signature material.
- Local security validation: **PASS**; production readiness: **NO** pending external providers and deployment controls.
