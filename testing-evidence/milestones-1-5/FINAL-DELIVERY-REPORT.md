# NestyStay M1–M5 final delivery report

Updated 2026-09-01 from the current repository state. The signed agreement was read in full and copied to `docs/contracts/NestyStay-Signed-Agreement-April-2026.pdf`; source and copy are byte-identical (SHA-256 `0C4AAD0B1A2D015433C0875107DD171C93C4191281D7CCCBBE748B37DB4FD28D`).

## Contractual decision

- **M1 contractual functional: PASS locally.**
- **M2 contractual functional: PASS locally.**
- **M3 contractual functional: PASS locally.**
- **M4 contractual functional: PASS locally.**
- **M5 contractual functional: PASS locally.** The signed Phase 5 web-first property-manager suite and client governance/proxy additions are represented in API, PostgreSQL and UI flows.

“Local” is explicit: live provider validation and production operations are separate statuses below.

## Verification totals

- Backend: **96 passed / 0 failed** (5 Domain, 23 Application, 14 Infrastructure, 54 API; includes provider-document storage/scanner/download and PM document-download scope tests).
- Frontend: **26 passed / 0 failed**; TypeScript/Vite build passed.
- Real browser: **88 passed / 0 failed / 2 intentional skips** in the full 90-test desktop/tablet/mobile matrix; this includes **18/18 enhancement checks** for M4/M5, **13 usability checks plus 2 intentional mobile skips**, and the existing M1–M4 route inventory (**19/19 routes passed** on each viewport).
- API + security: **PASS locally** (scope isolation, validation, idempotency, anonymous ballot and QR denial paths).
- Concurrency: **PASS locally** (two same-key payments returned 200/200, one persisted payment row, correct balance).
- PostgreSQL: Phase 5 EF migration applied to `nestystay_dev`; live smoke persisted owners, properties, invoices, utility-linked invoice, payment, statement and owner portal data.
- Lint: **0 errors / 162 existing warnings**; warnings are non-blocking legacy unused imports/parameters.

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
- Provider onboarding previously retained only selected document names: fixed with a persisted provider-document vault, validated binary upload, scanner metadata, scoped download and provider UI status/download controls.
- Manager documents previously had no download action: fixed with a storage-backed, manager-scoped download endpoint and UI action.
- Gate camera guidance previously required manual entry: fixed with browser QR decoding where supported and an explicit manual/offline fallback.

## Remaining blockers

Contractual local functionality has no known failing test. Production blockers are provider credentials, managed infrastructure/secret configuration, a fresh-database operator privilege, independent security review and operational runbooks.

## Evidence index

- `browser/m5-property-manager.spec.ts` and `frontend/e2e/m4-m5-enhancements.spec.ts` (source) with Playwright result artifacts.
- `frontend/e2e/usability-upgrades.spec.ts` and `docs/testing/ACCESSIBILITY-REPORT.md`.
- `browser/route-inventory.json` (19 routes, no 5xx/console failures).
- `backend/` API test output and `security/`/`concurrency/` records.
- `docs/testing/M1-M5-TRACEABILITY.md` and `docs/testing/M5-PROPERTY-MANAGER-ACCEPTANCE.md`.
