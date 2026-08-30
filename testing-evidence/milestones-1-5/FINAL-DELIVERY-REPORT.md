# NestyStay M1–M5 final delivery report

Generated 2026-08-31 from the current repository state. The signed agreement was read in full and copied to `docs/contracts/NestyStay-Signed-Agreement-April-2026.pdf`; source and copy are byte-identical (SHA-256 `0C4AAD0B1A2D015433C0875107DD171C93C4191281D7CCCBBE748B37DB4FD28D`).

## Contractual decision

- **M1 contractual functional: PASS locally.**
- **M2 contractual functional: PASS locally.**
- **M3 contractual functional: PASS locally.**
- **M4 contractual functional: PASS locally.**
- **M5 contractual functional: PASS locally.** The signed Phase 5 web-first property-manager suite and client governance/proxy additions are represented in API, PostgreSQL and UI flows.

“Local” is explicit: live provider validation and production operations are separate statuses below.

## Verification totals

- Backend: **93 passed / 0 failed** (5 Domain, 23 Application, 14 Infrastructure, 51 API; includes 3 PM endpoint tests).
- Frontend: **25 passed / 0 failed**; TypeScript/Vite build passed.
- Real browser: **6 passed / 0 failed** for PM/owner/gate across desktop, tablet and mobile; existing M1–M4 route inventory: **19/19 routes passed** on desktop.
- API + security: **PASS locally** (scope isolation, validation, idempotency, anonymous ballot and QR denial paths).
- Concurrency: **PASS locally** (two same-key payments returned 200/200, one persisted payment row, correct balance).
- PostgreSQL: Phase 5 EF migration applied to `nestystay_dev`; live smoke persisted owners, properties, invoices, utility-linked invoice, payment, statement and owner portal data.

## Required separation

| Area | Status |
|---|---|
| Stripe application integration | PASS |
| Real Stripe provider validation | BLOCKED — live credentials/webhook/Connect configuration |
| eKYC application integration | PASS |
| Real Alibaba provider validation | BLOCKED — credentials and signed callback material |
| Local security validation | PASS |
| Full production readiness | NO |
| Fresh disposable DB migration | BLOCKED — local role lacks `CREATEDB` |

## Differences found and fixed

- PM functionality had no complete manager/owner UI/API surface: added scoped entities, migration, store, controller, API client, routes and responsive pages.
- Utility allocations were not linked to invoices: fixed by creating an invoice, line and ledger reference atomically.
- Several scope edges lacked property/owner validation: fixed for invoices, documents, gate messages and QR issuance.
- Voting allowed lifecycle/race edge cases: fixed open-window enforcement, result persistence and serialized duplicate protection.
- Added owner verification controls, maintenance state controls, governance proposal creation, document upload and owner proxy UI.

## Remaining blockers

Contractual local functionality has no known failing test. Production blockers are provider credentials, managed infrastructure/secret configuration, a fresh-database operator privilege, independent security review and operational runbooks.

## Evidence index

- `browser/m5-property-manager.spec.ts` (source) and Playwright result artifacts.
- `browser/route-inventory.json` (19 routes, no 5xx/console failures).
- `backend/` API test output and `security/`/`concurrency/` records.
- `docs/testing/M1-M5-TRACEABILITY.md` and `docs/testing/M5-PROPERTY-MANAGER-ACCEPTANCE.md`.
