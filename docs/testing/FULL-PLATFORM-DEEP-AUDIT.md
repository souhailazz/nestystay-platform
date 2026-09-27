# NestyStay Full Platform Deep Audit

## Current certification refresh — 2026-09-27

The latest complete local browser run is recorded in [`LOCAL-PLAYWRIGHT-2026-09-27.md`](../../testing-evidence/final-hardening/07-browser/LOCAL-PLAYWRIGHT-2026-09-27.md): **224 discovered, 224 started, 213 passed, 0 failed, 11 explicit skips, 0 did-not-run**. The current Playwright inventory is 224 tests in 36 files; the earlier planning baseline of 227 is not the current repository inventory. The run used a real server-issued disposable Admin fixture and the isolated PostgreSQL-backed backend. The admin-token-gated subset passed **12/12**. The explicit skips are provider/credential or viewport-scope guards only; they do not represent unstarted tests.

The current source heads used were root evidence `979d67f` (the next documentation update will supersede this line), backend `28c7814` and frontend `352f2f8c8e131c8f1fd2800a697cb6b4c5a364a0`; the root evidence branch also contains the current report commits. The frontend unit suite is **146/146** across 34 files with fresh V8 coverage of **61.16% lines, 57.12% statements, 48.76% branches, and 53.49% functions**; typecheck and production build pass. The complete configured backend solution passes **213/213**, including the real PostgreSQL concurrency test; the database-backed API test is isolated in a non-parallel xUnit collection. A direct `dotnet-coverage` collection of that same complete suite generated a Cobertura report; after excluding only EF-generated migration designer and snapshot source it measures **17,228/33,561 authored lines (51.33%)**. The pinned collector and secret-free runner are now in backend commit `28c7814`; the runner rejects missing disposable-service variables instead of silently skipping integration tests. Its follow-up full execution stalled in MSBuild before a test host began, so that retry is not counted as a pass. The latest backend Sonar analysis was uploaded at the prior code-equivalent head `e195e8b`: 0 bugs, 0 vulnerabilities, 0 hotspots, 805 code smells, 15.1% Sonar line coverage, 48.7% branch coverage, 6.9% duplication, and 0 new violations under the isolated new-code-only gate. Two local attempts to upload the new full coverage report stalled in the analyzer-backed MSBuild phase before a server upload; the backend coverage and maintainability requirements therefore remain open. Frontend `352f2f8` has a fresh Sonar Quality Gate `OK` under the configured new-code condition with 0 bugs/vulnerabilities/hotspots, 972 code smells, 56.6% imported line coverage, and 1.4% duplication. Root orchestration scope has zero current issues. Production certification remains blocked by maintainability/full-coverage acceptance, real staging Brevo/MinIO proof, and deployed SHA parity (`/api/health/version` is 404 and `/version.json` is SPA HTML on the current staging probe). See [`CERTIFICATION-SUMMARY-2026-09-27.md`](../../testing-evidence/sonarqube/CERTIFICATION-SUMMARY-2026-09-27.md) and the post-refactor evidence.

### Coverage-runner update — 2026-09-27

The stale local Roslyn compiler lock that affected the first runner attempt was cleared without touching source or generated audit evidence. Backend commit `0e58c1f8b490a4ac3f9a905d4d8a14d44de845eb` now pins `dotnet-coverage`, builds before instrumentation, requires runtime-only disposable PostgreSQL/MinIO variables, and retains a separate TRX result for each test project. The final run produced a valid Cobertura report with **213 passed, 0 failed, 0 skipped**: Domain 6, Application 24, Infrastructure 32, API 151. After excluding only EF-generated migration designer/snapshot files, the artifact measures **17,228/33,561 authored lines (51.33%)**. This proves the collector path; it does not satisfy the 80% backend coverage target, and fresh Sonar ingestion remains separately blocked by the local analyzer-build issue.

Date: 2026-09-23
Scope: isolated release-certification branches only. The dirty original workspace and production were not changed.

## Code under test

| Repository | Branch | SHA |
|---|---|---|
| Frontend | `codex/final-release-certification` | `8947fafc21d321186b1fd31eece198d1285f269a` |
| Backend | `codex/final-release-certification` | `55af964` |
| Root/orchestration | `codex/final-release-certification` | `c53834b76a2a84e3f5c226ecafaf121b9c562e12` |

## Automated results

- Backend: **202 passed, 0 failed, 0 skipped**. This includes the real local MinIO integration suite and authorization matrix tests.
- Frontend unit tests: **70 passed**.
- Frontend typecheck: **PASS**.
- Frontend production build: **PASS**.
- Frontend lint: **0 errors, 85 warnings**.
- `npm audit --audit-level=moderate`: **0 vulnerabilities**.
- Playwright: **224 discovered, 224 started, 213 passed, 0 failed, 11 explicit skips, 0 not-run** across the configured Chromium desktop/tablet/mobile matrix.

The 11 skips are explicit conditional guards: privileged/provider checks requiring an injected admin token or explicit MinIO browser mode, non-local smoke credentials, and project-specific responsive-matrix guards. They are recorded as skips by the test suite; they are not counted as passes.

## Local integration

- Disposable private MinIO was started locally and actual upload, overwrite/hash, signed download, invalid credentials, traversal and size-limit checks passed.
- Backend release metadata endpoint and frontend `/version.json` generation were implemented and covered by automated checks.
- Brevo real delivery was not run because no staging API key/mailbox was supplied to this local run. Local outbox behavior is not equivalent to Brevo delivery.
- Live Stripe/Stripe Identity was not called. Staging remains the controlled test-mode environment.

## Release blockers

1. Frontend all-source coverage is 25.13% locally (Sonar reports 25.8% line / 21.4% combined), below the requested 60%; new-code coverage is 18.2% combined / 25.0% line, below the requested 80% gate.
2. Backend OpenCover coverage is now imported correctly: 51.2% overall (50.9% line, 52.1% branch). The backend Quality Gate remains failed because new-code coverage is 45.6% and one new violation remains.
3. Frontend Sonar Quality Gate is failed: 0 bugs, 0 vulnerabilities, 970 code smells and 1 new violation. The certification pass removed the previously reported frontend security and bug findings; the gate still fails on coverage/new-code violation.
4. Backend Sonar Quality Gate is failed: 9 bugs, 31 vulnerabilities, 1009 code smells and 1 new violation.
5. Staging has not deployed the certification branches: `/api/health/version` currently returns 404 and `/version.json` currently falls through to HTML, so deployed SHA parity is **UNKNOWN**.
6. Real staging MinIO I/O and Brevo mailbox delivery remain external configuration checks.

This report is evidence for review; it is not a production-release approval.
