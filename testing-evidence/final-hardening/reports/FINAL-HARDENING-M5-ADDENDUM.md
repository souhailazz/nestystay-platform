# NestyStay final hardening — M5 and security addendum

Generated 2026-09-02 from the worktree resumed at hardening commit `35794c8e8718d3c2f8a76e124a477d9a76f13427`.

This addendum is the current result for the M1–M5 hardening pass. Historical baseline numbers remain in `FINAL-HARDENING-REPORT.md`; this document records the new M5, session-boundary, database, and regression evidence.

The signed agreement was verified readable at `docs/contracts/NestyStay-Signed-Agreement-April-2026.pdf` (11 pages, 785,845 bytes). Its SHA-256 is `0C4AAD0B1A2D015433C0875107DD171C93C4191281D7CCCBBE748B37DB4FD28D`, matching the supplied Downloads source byte-for-byte.

## Milestone decision

| Milestone | Backend complete | Frontend complete | Backend↔Frontend connected | Real browser verified | Missing UI pieces | Decision |
| --- | --- | --- | --- | --- | --- | --- |
| M1 Core | PASS | PASS | PASS | PASS (existing M1 evidence) | None found in this pass | PASS |
| M2 Badges | PASS | PASS | PASS | PASS (existing M2 evidence) | None found in this pass | PASS |
| M3 Wellness | PASS | PASS | PASS | PASS (existing M3 evidence) | None found in this pass | PASS |
| M4 Directories + QR | PASS | PASS | PASS | PASS (existing M4 evidence; QR revoke rechecked) | None found in this pass | PASS |
| M5 Property Manager | PASS | PASS | PASS | PASS — 7/7 M5 journeys across configured Chromium/Firefox/WebKit targets | None found for the exercised contractual journeys | PASS |

M5 browser evidence exercises persisted property, invoice, maintenance, governance, document, owner-portal, QR issue/revoke, and gate-denied flows through the real API and PostgreSQL-backed service. The workflow also checks console errors, unexpected 5xx responses, axe violations, horizontal overflow, and minimum control sizing.

## Authorization and session boundary

- Authorization matrix: **54/54 (100%)**; dynamic security checks: **7/7 (100%)**.
- Browser sessions now use an HttpOnly `nestyStay.session` cookie when `X-Session-Mode: cookie` is requested. The bearer value is not returned to browser clients or persisted in localStorage; local storage contains only non-secret identity metadata.
- Unsafe cookie-authenticated requests require a matching non-HttpOnly CSRF cookie/header double-submit token. Bearer API clients remain compatible.
- Covered: cookie login, cookie 2FA verification, CSRF rejection/acceptance, logout revocation, and multi-tab invalidation. Existing bearer expiry/revocation, CORS, role, and cross-owner checks remain green.

## PostgreSQL integrity

- 145 tables remain present.
- Foreign keys increased from **0 to 45** through reviewed EF migrations `20260902125555_FinalHardeningM5Relationships` and `20260902125706_FixMaintenanceVendorForeignKey`.
- Enforced M5 orphan rows: **0**. The single `milestone_manager_qr_scan.property_id` mismatch value is intentionally not constrained because it records a guard-supplied wrong-property comparison; it is documented as a reviewed contextual value.
- Migration application and relationship validation: **PASS**. See `../12-database/relationship-inventory.md` and `../12-database/fk-validation.json`.

## Current verification totals

- Backend: **101 passed / 0 failed** (Domain, Application, Infrastructure, and API suites; API includes cookie-session and cookie-2FA tests).
- Frontend unit: **30 passed / 0 failed**; typecheck and production build pass.
- Hardening browser matrix: **70 planned / 22 passed / 0 failed / 48 intentional skips**. M5 itself is 7/7 across the seven configured browser/viewport projects.
- Authorization: **54/54**; dynamic security: **7/7**; financial assertions: **16/16**; concurrency API assertions: **4/4** plus 13 backend concurrency scenarios discovered.
- Accessibility: **12 pages**, 0 critical/serious/moderate/minor axe violations in the representative scan. Responsive evidence: **60 screens**, 0 overflow failures, 1,750 controls measured.
- API/performance evidence remains 210 samples, 0 errors, maximum 43.97 ms; load evidence remains 960 requests, 0 application errors, and 16 expected rate-limit responses.
- Vitest included-source coverage after the new auth tests: statements **44.73%**, branches **44.29%**, functions **29.61%**, lines **44.22%**. The earlier 4.97/4.36/3.81/5.66 values are the broader full-source baseline and are retained as such; the scopes are not interchangeable.
- ESLint: **0 errors, 159 warnings** (143 `no-unused-vars`, 16 `no-explicit-any`). All warnings are classified in `../15-coverage/frontend-lint-classification.md`; no unsafe semantic autofix was applied.

## Differences found and fixed

1. The owner maintenance UI journey exposed an authorization/business-logic mismatch. Owner actors now resolve through their assigned property-manager relationship; the M5 persisted owner maintenance flow returns 200.
2. The Property Manager UI claimed QR revocation but did not expose the action. A visible revoke control, status state, and denied gate result are now wired to the real revoke endpoint.
3. Browser bearer storage was replaced with an HttpOnly cookie session boundary plus CSRF protection and browser-safe response sanitization.
4. M5 parent/child and actor relationships were not represented as database FKs. Forty-five reviewed constraints and an orphan inventory are now applied and green.

## Release status

- Local/contractual M1–M5 release candidate: **YES**.
- Production readiness: **NO**. Live Stripe/Connect, Alibaba/eKYC, email/SMS/push, managed infrastructure, production secrets, distributed rate limiting/locks, backups/restore drills, observability, privacy-retention review, and external penetration testing remain production-only blockers; they do not block local contractual M1–M5 completion.

## Evidence index

- M5 browser journeys: `../07-browser/m5-*.json`
- Authorization: `../04-authorization/authorization-matrix.json`
- Cookie/CSRF API tests: `../../../backend/tests/NestyStay.Api.Tests/CookieSessionSecurityTests.cs`
- Database relationships: `../12-database/relationship-inventory.md`, `../12-database/fk-validation.json`
- Frontend coverage and lint: `../15-coverage/` 
- Existing M1–M4 evidence and contract reconciliation: `FINAL-HARDENING-REPORT.md`, `EVIDENCE-INDEX.md`, and the contract traceability/interpretation reports.
