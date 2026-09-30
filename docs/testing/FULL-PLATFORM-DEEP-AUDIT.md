# NestyStay Full-Platform Deep Audit

Audit date: 2026-09-22  
Result: **INCOMPLETE — not a release certification**

This audit used the current GitHub `main` commits where possible, local tests,
source inspection, current public staging HTTP checks, and the existing test
suite. It did not attempt live financial transactions, change staging, reset the
local database, alter migrations, or expose credentials. A 100-area adversarial
audit cannot be honestly marked complete from the evidence available here;
unverified areas are explicitly left open below.

## Repository and deployment baseline

| Repository | Local checkout | GitHub `main` | Worktree | Deployed version evidence |
|---|---|---|---|---|
| Root/orchestration | `codex/frontend-hardening-release-candidate` at `37f0b750ab7e7c81a948b4fe3d660acf9faed091` | `76dfee9a4f25f3b83a8bdb2db3a0177ccdb73301` | 260 existing changes at audit start; 267 status entries at final check, including local audit artifacts and branch-mirrored files. No merge base with `origin/main`; preserved without cleanup. | Unknown. Latest root deploy workflow failed during SSH setup. |
| Backend | `codex/fix-minio-test-skip` at `a082b84645b93681a68389252854a8775e4976bd` (test-harness fix) | `e362234adf652e09d884f94172f9e4ae4f64173c` | Feature branch clean and pushed; [PR #7](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/pull/7) open. | Backend main deployment to the staging host and its public readiness probe succeeded; a separate systemd-host copy job failed. |
| Frontend | `codex/fix-pm-owner-invitation-feedback` at `788a57ca5d1e097d52b63708a71a1277727a560e` | `d52c73a96d1e12435af80b3ae8cdf092daa43cae` | Feature branch clean and pushed; [PR #4](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/4) open | Unknown. The post-merge frontend deployment failed while copying the release archive. |

PR state checked on GitHub: backend PR #5 and frontend PR #3 are merged; root
PR #9, backend PRs #6 and #7, and frontend PR #4 remain open. PR #4's required
typecheck/test/build check passed; its deployment job was skipped as expected
for a feature PR. PR #7 fixes only the MinIO test's false-green behavior; its
remote restore/build/test checks passed; the staging/systemd deploy jobs did
not run for the feature PR. Neither audit PR deploys until reviewed/merged
through the protected workflow. No branch protection was bypassed.

## Build and test results

| Check | Result |
|---|---|
| Backend restore/build/full solution tests on backend main `e362234` | **194 passed, 0 failed, 0 skipped** across Domain (5), Application (23), Infrastructure (32), and API (134). `NestyStay.MigrationCheck` restored/built as part of the main solution; it is not a test project. |
| Backend full suite on test-harness PR #7 | **193 passed, 0 failed, 1 skipped**; the skipped case is the MinIO round-trip because `MINIO_TEST_ENDPOINT` is not configured. Targeted run confirms xUnit reports a skip, not a pass. |
| Backend compiler warnings | Obsolete test helpers still call the legacy `PurchaseBadge(..., bool)` method; production code is directed to the provider-backed payment workflow. |
| Frontend unit suite on current feature branch | **56 passed** in 12 test files, including the new owner-invitation regression. |
| Frontend typecheck | **PASS** |
| Frontend production build | **PASS**; Vite transformed 2,154 modules. |
| Frontend lint | **0 errors, 84 warnings**, mostly unused imports/state and `any` in tests. Warnings remain cleanup/debt, not a lint failure. |
| `npm audit` | **0 known vulnerabilities** across 377 reported dependencies. |
| NuGet vulnerable-package audit on backend main | **No vulnerable packages reported** across the nine solution projects. |
| Playwright discovery | **221 tests in 35 files discovered.** |
| Playwright execution | **PARTIAL.** The local run was stopped after the first canonical-route inventory test failed and the suite began writing directly into pre-existing `testing-evidence` paths. Twelve tests had started; one failure is confirmed. The interrupted run did not produce trustworthy aggregate passed/failed/skipped totals; 209 tests had not started. Do not count the other started cases as passes. |

Playwright failure observed: `canonical-route-inventory.spec.ts` expected a
workspace shell at `/admin/ops/directory`, but the page correctly showed
“Sign in required.” The fixture creates an Admin object only in browser
`localStorage` with an empty access token and no authenticated server session.
This is a **test-fixture/authentication mismatch**, not proof that the product
should allow local-storage role forgery. Repair the fixture to provision a real
development Admin session (or report the route as not authenticated) before
using this matrix as acceptance evidence.

The local stack was started using the repository’s supported dev script. Local
`/api/health/live` and `/api/health/ready` returned 200, and startup reported
the development database already up to date. The full Playwright suite was not
completed. Several specs write evidence directly under `testing-evidence`
instead of using the configured Playwright output directory; the repository
already had 260 dirty entries, so no attempt was made to restore or clean any
files.

## Findings

| ID | Severity | Area | Evidence / actual behavior | Required action | Status |
|---|---|---|---|---|---|
| AUD-001 | MEDIUM | Owner invitations / PM UI | The previous “Resend invitation” control only announced “Invitation resend queued”; no resend API exists. The UI now removes that false-success control and states that no duplicate invitation was sent. Regression test passes. | Actual resend remains unimplemented; add only as a separately scoped, manager-authorized and audited backend/UI lifecycle with safe prior-token invalidation. | FALSE-SUCCESS UI FIXED LOCALLY in [frontend PR #4](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/4) (`788a57ca5d1e097d52b63708a71a1277727a560e`); resend lifecycle still missing. |
| AUD-002 | MEDIUM | MinIO test validity | The MinIO integration test previously returned normally when `MINIO_TEST_ENDPOINT` was unset, falsely counting as passed. It now uses an xUnit skip attribute and was observed as skipped; its environment-missing guard also fails explicitly if reached. | Merge backend PR #7; then run upload/download/auth tests against a disposable private MinIO fixture. | FALSE-GREEN TEST FIXED LOCALLY in [backend PR #7](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/pull/7) (`a082b84645b93681a68389252854a8775e4976bd`); MinIO I/O remains unverified. |
| AUD-003 | MEDIUM | Alibaba configuration drift | Backend runtime source and frontend `src` have no Alibaba provider references, but root `main` still maps Alibaba environment names into both service blocks in `docker-compose.production.yml:40-49,109-118`; `.env.production.example:40-50` also contains legacy names. | Deployment owner should decide whether to remove these non-runtime mappings and correct stale instructions. Existing migrations were not modified. See `ALIBABA-REFERENCE-AUDIT.md`. | OPEN — not active provider wiring, but configuration/documentation drift remains. |
| AUD-004 | HIGH | Browser role-route test | The route inventory fails for the synthetic Admin fixture because it has no valid server session. | Use a real isolated development Admin fixture; rerun route/authorization matrix. Never weaken application auth to satisfy a forged fixture. | OPEN — full role matrix not established. |
| AUD-005 | HIGH | Release/deployment parity | Backend staging host deploy and readiness check passed, but a separate backend host job failed on SSH file copy. Frontend release copy failed; root deployment failed during SSH setup. Public staging home and `/api/health/ready` returned 200, which does not prove all latest artifacts are deployed. | Repair the correct GitHub Actions host/key/path configuration; rerun main deployments; expose/record deployed commit SHAs; verify frontend/backend parity. | BLOCKING RELEASE CERTIFICATION. |
| AUD-006 | HIGH | Root repository state | Local root branch has 260 dirty entries and no merge base with root `origin/main`; it is not safe to consolidate or clean in place. | Preserve the worktree. Reconcile from a clean main-based worktree with an explicit file/commit inventory before opening or updating a root PR. | OPEN. |
| AUD-007 | HIGH | Production object storage | MinIO provider, DI selection, production validation, and deployment service exist on backend main. Local acceptance used the deterministic local storage provider; the Docker daemon/MinIO service was unavailable, and no real upload/download/restart/authorization cycle was proven against MinIO. | Configure private MinIO in the target environment and run an isolated upload → authorized download → restart → download → unauthorized denial test. | BLOCKED on provider/runtime configuration and E2E. |
| AUD-008 | MEDIUM | Static UI completeness | Lint reports 84 warnings. A code-path review found unused state/import debt. The unsupported PM resend control has been removed; no resend lifecycle exists yet (AUD-001). | Clean warnings where behavior is understood; scope a real resend lifecycle separately if required. | OPEN — cleanup and actual resend lifecycle remain. |
| AUD-009 | MEDIUM | Security certification | Gitleaks reported redacted candidates in example/development/test configuration files: 18 in the root scan, 7 in backend, 0 in frontend. No candidate value was printed. These are not declared safe solely because of their paths. | Confirm each is a non-production placeholder/test value; rotate any value that was ever live. Keep actual `.env` files out of Git. | REVIEW REQUIRED; no confirmed production secret reported. |

## Reproduction and remediation details

**AUD-001 — PM owner-invitation false success (MEDIUM)**

- Role/route: Property Manager, `/pm/dashboard`, “Invite owner” panel.
- Preconditions: dashboard loads and the typed email already belongs to a
  portfolio owner.
- Reproduction before the fix: type that email, press “Resend invitation,”
  observe “Invitation resend queued.” No resend endpoint was called and no
  invitation was queued. The duplicate-owner message also pointed users to
  resend/cancel controls that were not present.
- Expected: a real authorized resend operation, or an honest explanation that
  no duplicate invitation was sent. A success message must not precede a
  persisted/queued backend operation.
- Current local behavior: unsupported resend button removed; duplicate owner
  is explicitly told that no duplicate invitation was sent. The actual resend
  feature remains absent. UI regression is in
  `frontend/src/pages/PropertyManagerDashboardPage.test.tsx` and passes.

**AUD-002 — MinIO integration false pass (MEDIUM)**

- Test: `MinioStorageProviderTests.LocalMinioRoundTripAndAuthorization`.
- Preconditions: `MINIO_TEST_ENDPOINT` unset.
- Reproduction before the fix: run the test; its early `return` made the test
  runner report a pass without any storage request.
- Expected: report skipped when no disposable MinIO endpoint is configured,
  and execute upload/download/authorization assertions when it is configured.
- Current local behavior: discovery-time `MinioIntegrationFact` skip is
  reported by xUnit as **Skipped**. A configured MinIO endpoint was unavailable,
  so actual MinIO I/O was not exercised.

## Product and security verification boundary

Backend tests give useful automated evidence for authentication, bookings,
payments, webhooks, badges, Wellness, directories/QR, and Property Manager
stores. They do not prove full browser/API/database/refresh/role lifecycle for
every milestone. Current audit classifications:

| Area | Evidence-backed status |
|---|---|
| M1 booking/auth/payment | Backend tests pass; browser full lifecycle and real Stripe session not certified. **PARTIAL.** |
| M2 badges | Backend tests pass; browser/admin/real provider lifecycle not certified. **PARTIAL.** |
| M3 Wellness | Backend coverage exists; complete role-separated browser/payment/upload lifecycle not certified. **PARTIAL.** |
| M4 directories/trust/QR | Backend coverage exists; full browser role/privacy/QR matrix not certified. **PARTIAL.** |
| M5 Property Manager | Backend coverage exists; owner/staff/mobile/UI lifecycle not certified. The false-success resend affordance is removed locally; the actual resend lifecycle remains absent. **PARTIAL.** |
| Authentication/session/role isolation | Backend tests and frontend unit/build checks pass; full real-role route and cross-tab matrix did not complete. **PARTIAL.** |
| Payments/webhooks | Automated backend coverage exists; live/test-mode browser payment, duplicate/out-of-order provider behavior in deployed config not re-certified here. **PARTIAL.** |
| Email/Brevo | Local outbox/template tests are in the backend suite; real Brevo send/retry/delivery is not verified in this audit. **BLOCKED EXTERNALLY.** |
| Accessibility/performance/mobile | Only partial browser execution; axe matrix, all requested viewport widths, Lighthouse, and human screen-reader certification did not complete. **UNVERIFIED/PARTIAL.** |

No broad assertion of PASS is made for IDOR, XSS, SQL injection, SSRF, webhook
replay, rate limiting, uploads, or all route/API × role combinations. The full
security matrix and independent penetration/accessibility testing remain open.

## Exact next actions

1. Keep the dirty root worktree untouched. Build a clean root audit worktree
   from the intended PR base before reconciling its 260 changes.
2. Review/merge frontend PR #4 for the false-success UI correction. Implement a
   real resend lifecycle only if separately approved and scoped; the current UI
   no longer claims that a resend occurred.
3. Repair the canonical-route test fixture to authenticate a local Admin without
   trusting browser `localStorage` role claims.
4. Review backend PR #7 after its CI completes; merge it so an unavailable
   MinIO endpoint is not a false pass. Then run the MinIO suite against a
   private disposable test bucket and verify persistence, access isolation,
   and failure handling.
5. Configure and rerun protected deployment workflows; verify live commit IDs
   for both frontend and backend.
6. Rerun all 221 Playwright tests from a clean, isolated test checkout with every
   test evidence path redirected, then report actual executed/pass/fail/skip
   totals. Complete the manual accessibility and external provider checks.

No production/staging data was changed and no existing migration file was
edited. The PM UI correction is committed/pushed on frontend feature branch
`codex/fix-pm-owner-invitation-feedback` (PR #4); the MinIO test-reporting fix
is committed/pushed on backend feature branch `codex/fix-minio-test-skip` (PR
#7). These root audit reports are local evidence in the pre-existing dirty
root worktree and have not been committed or published.
