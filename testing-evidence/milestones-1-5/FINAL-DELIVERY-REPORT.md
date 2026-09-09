# NestyStay M1–M5 final delivery report

Updated 2026-09-09 from the current repository state. The signed agreement was read in full and copied to `docs/contracts/NestyStay-Signed-Agreement-April-2026.pdf`; source and copy are byte-identical (SHA-256 `0C4AAD0B1A2D015433C0875107DD171C93C4191281D7CCCBBE748B37DB4FD28D`).

## Contractual decision

- **M1 contractual functional: PASS locally.**
- **M2 contractual functional: PASS locally.**
- **M3 contractual functional: PASS locally.**
- **M4 contractual functional: PASS locally.**
- **M5 contractual functional: PASS locally.** The signed Phase 5 web-first property-manager suite and client governance/proxy additions are represented in API, PostgreSQL and UI flows.

“Local” is explicit: live provider validation and production operations are separate statuses below.

## Verification totals

- Backend: **135 passed / 0 failed** (5 Domain, 23 Application, 19 Infrastructure, 88 API; includes M1 session/passkey/calendar/recommendation/payout coverage, M5 document export/expiry and subscription lifecycle paths).
- Backend release build: **0 warnings / 0 errors**; the NuGet vulnerability audit is clean after pinning patched `Microsoft.Bcl.Memory 10.0.12`.
- Frontend: **35 passed / 0 failed**; TypeScript/Vite build and typecheck passed; full npm audit is clean after the Vitest/coverage-v8 4.1.11 update.
- Real browser: complete configured Playwright matrix **91 passed / 0 failed / 52 intentional credential/provider-gated or viewport-scoped skips** across 143 collected tests; desktop Chromium was 30/30, the Property Manager subscription/document/utility journey passed at tablet and mobile (**2 additional passes**), and Firefox/WebKit critical smoke passed (**2 additional passes**). The remaining skipped admin/provider/live-provider paths require client credentials.
- API + security: **PASS locally** (scope isolation, validation, idempotency, anonymous ballot and QR denial paths).
- Concurrency: **PASS locally** (two same-key payments returned 200/200, one persisted payment row, correct balance).
- PostgreSQL: additive EF migrations through `20260909200136_FixPropertyManagerSubscriptionDefaults` applied to `nestystay_dev`; **194 public tables** verified, including session/passkey/calendar/recommendation/payout/document-export/subscription lifecycle fields.
- Lint: **0 errors / 156 existing warnings**; warnings are non-blocking legacy unused imports/parameters.

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
- M1 auth/browser fixtures previously placed bearer tokens in local storage: fixed by using HttpOnly cookie sessions, per-device session records and current-session-safe revocation.
- Calendar synchronization was manual-only: fixed with conditional ETag/Last-Modified sync, bounded hosted worker, atomic availability replacement, retry state and history.
- Guest recommendations and host payout visibility were transient: fixed with PostgreSQL interaction/preferences, explainable scoring, payout summaries/history and audited manual settlement.
- eKYC camera capture now has a live media path with compressed file fallback and safe retry states.
- M5 subscription lifecycle previously exposed only core tier changes: fixed with persisted provider status, unit limits, billing events, retry, pause/resume, auto-renew, scheduled downgrade worker, cancellation reasons and reactivation UI/API.

## Remaining blockers

Contractual local functionality has no known failing test in the verified subset. Production blockers are live provider credentials, managed infrastructure/secret configuration, full multi-role browser certification, formal manual WCAG sign-off, production object-storage read/retention certification, a fresh-database operator privilege, independent security review and operational runbooks. The subscription worker and local/test billing state are complete; live billing webhooks and payment certification remain separate.

## Evidence index

- `browser/m5-property-manager.spec.ts` and `frontend/e2e/m4-m5-enhancements.spec.ts` (source) with Playwright result artifacts.
- `frontend/e2e/usability-upgrades.spec.ts` and `docs/testing/ACCESSIBILITY-REPORT.md`.
- `browser/route-inventory.json` (19 routes, no 5xx/console failures).
- `testing-evidence/final-hardening/20-deployment/full-desktop-browser-run.txt` (47 Chromium tests: 30 passed, 17 intentional skips, 0 failed).
- `testing-evidence/final-hardening/20-deployment/full-playwright-matrix.txt` (143 tests across configured projects: 91 passed, 52 intentional skips, 0 failed).
- `backend/` API test output and `security/`/`concurrency/` records.
- `docs/testing/M1-M5-TRACEABILITY.md` and `docs/testing/M5-PROPERTY-MANAGER-ACCEPTANCE.md`.
