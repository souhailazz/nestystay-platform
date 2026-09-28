# Autopilot History

## AUTO-0030 — Centralize Property Manager staff lifecycle validation

Priority: P0 multi-owner/Gate Guard maintainability  \
Date: 2026-09-28

Triaged the exported backend Sonar inventory and selected the highest-risk non-migration concentration: Property Manager staff invitation/update lifecycle logic. Centralized staff role/status normalization and shared Gate Guard/finance configuration validation while preserving existing business rules and messages.

Evidence:

- Backend commit: `b726d6d5e58d4604b6fe7d85ab793ffd9f40cbcf`, pushed to the existing certification PR branch.
- Focused Property Manager P0, authorization, and passkey regression: **21 passed, 0 failed, 0 skipped**.
- Complete non-container suite: **225 passed, 0 failed, 2 explicit integration skips** (MinIO and two-instance PostgreSQL).
- The preceding container-enabled 225/225 result remains valid for the unchanged test infrastructure; current-head container verification awaits Docker recovery and is not inferred.
- Maintainability classification: `testing-evidence/sonarqube/BACKEND-MAINTAINABILITY-TRIAGE-2026-09-28.md`.

## AUTO-0029 — Cover passkey ownership and revocation lifecycle

Priority: P0 authentication/ownership coverage  \
Date: 2026-09-28

Added behavioral API integration tests for a security-sensitive controller that previously had no direct coverage. The tests exercise authenticated listing, own-credential revocation, inactive lifecycle visibility, repeated-delete handling, and cross-user revocation denial.

Evidence:

- Backend commit: `4ece56f`, pushed to the existing certification PR branch.
- Focused passkey suite: **3 passed, 0 failed, 0 skipped**.
- Full configured backend suite: Domain 6, Application 24, Infrastructure 32, API 163; **225 passed, 0 failed, 0 skipped**.
- A full coverage artifact was generated. The disposable Docker daemon stopped before a new server-side Sonar upload could run, so the last verified Sonar percentage remains the earlier `393edd1` scan and is not overstated.
- No production/staging system, migration history, or secret was changed.

## AUTO-0028 — Add behavioral coverage for storage and platform controllers

Priority: P1 quality evidence  \
Date: 2026-09-27

The current Sonar file report identified the storage download boundary and platform metadata controller as untested. Added focused behavioral tests for malformed/unsigned object URLs, content-type mapping, missing-object handling, and all platform blueprint payloads.

Evidence:

- Backend commit: `393edd1e20269350ed565fff8dda53b997792e39`, pushed to the existing certification PR branch.
- Focused tests: **9 passed, 0 failed, 0 skipped**.
- Full configured backend suite: Domain 6, Application 24, Infrastructure 32, API 160; **222 passed, 0 failed, 0 skipped**.
- Current-head server-side Sonar analysis completed successfully: 0 bugs, 0 vulnerabilities, 0 hotspots, 996 code smells, 20.4% duplication, 62.1% raw line coverage. File measures excluding all EF migration files reach 72.8%, still below the requested 80%.
- No production/staging system or migration history was changed.

## AUTO-0027 — Re-run the local authorization and IDOR regression matrix

Priority: P1 security evidence  \
Date: 2026-09-27

The release plan required a current local authorization pass before relying on the broader full-suite result. Existing tests already covered the relevant role and ownership boundaries, so no application behavior was changed.

Evidence:

- Backend commit: `0e58c1f8b490a4ac3f9a905d4d8a14d44de845eb`.
- Focused API command matched `CrossResourceAuthorizationMatrixTests`, `PropertyManagerAuthorizationMatrixTests`, and `BadgeAuthorizationTests`.
- Result: **13 passed, 0 failed, 0 skipped**.
- Covered cross-resource messages/attachments, wellness/officer documents, provider privacy, owner/manager/staff portfolio isolation, Gate Guard invite/scope/revocation, admin-only endpoint rejection, Property Manager policy rejection, badge ownership, and logout/session invalidation.
- Staging role/IDOR certification remains an external gate and is not inferred from local tests.

## AUTO-0026 — Execute the final reproducible backend integration-coverage run

Priority: P1 quality evidence  \
Date: 2026-09-27

### Problem

The first invocation of the new runner inherited a stale local Roslyn compiler lock from an earlier interrupted analysis. It stalled before test-host startup, so its result could not be counted as coverage evidence.

### Resolution and validation

- Identified the file lock on a generated SourceLink file and stopped only the stale local `VBCSCompiler` process.
- Changed the runner to build outside the coverage collector, then instrument the already-built test hosts.
- Added a per-project TRX prefix so test totals cannot overwrite each other.
- Final disposable PostgreSQL/MinIO coverage run produced a valid Cobertura report and four TRX files:
  - Domain: **6 passed, 0 failed, 0 skipped**
  - Application: **24 passed, 0 failed, 0 skipped**
  - Infrastructure: **32 passed, 0 failed, 0 skipped**
  - API: **151 passed, 0 failed, 0 skipped**
  - Total: **213 passed, 0 failed, 0 skipped**
- The fresh artifact reports **17,228/33,561 authored lines (51.33%)** after excluding only EF-generated migration designer/snapshot files. It is valid coverage evidence, but it does not meet the 80% requested backend target.

### Result

Backend test/coverage collection is repeatable and records both coverage and per-project outcomes without committing credentials. The remaining backend coverage gap is test breadth, not a skipped integration path. No production or staging system was changed.

## AUTO-0025 — Commit a reproducible backend integration-coverage workflow

Priority: P1 quality evidence  \
Date: 2026-09-27

### Problem

The complete backend suite had already passed through `dotnet-coverage` with real disposable PostgreSQL and MinIO, but the command depended on an untracked global tool and ad hoc runtime setup. That made the resulting coverage evidence difficult to reproduce safely.

### Changes

- Added backend `.config/dotnet-tools.json`, pinning `dotnet-coverage` 18.11.2.
- Added `tools/collect-coverage.ps1`, which requires the five runtime-only PostgreSQL/MinIO integration variables and fails fast if any are absent.
- Documented the exact non-secret workflow in the backend README. The ignored default output remains under `TestResults/coverage`.

### Validation

- `dotnet tool restore` through the committed manifest: PASS.
- Runner guard with missing variables: PASS; it did not read repository files or fall back to a credential.
- Prior direct complete collection using the same tool/runtime: **213 passed, 0 failed, 0 skipped**, with a valid Cobertura report.
- The follow-up full script invocation stalled in MSBuild before a test host started; it was stopped after repeated idle-process observations and is deliberately **not** represented as a successful rerun.
- The prior report measured **17,228/33,561 authored lines (51.33%)** after excluding only generated EF migration designer and snapshot source. This remains below the 80% requested backend target.

### Result

Backend coverage collection is now reproducible without source-controlled credentials. Full Sonar ingestion of the new report remains blocked by the independently observed local analyzer-build stall; no production or staging system was changed.

## AUTO-0024 — Complete current-head browser regression

Priority: P1 release evidence  \
Date: 2026-09-27

### Problem

The previous full browser evidence predated the latest frontend reservation-filter refactor. The current head needed a complete, real local regression rather than an inferred result.

### Validation

- Command: `npm run test:e2e -- --reporter=line` with a runtime-only local PostgreSQL connection string.
- Runtime: local PostgreSQL-backed ASP.NET API, a real server-issued disposable Admin bootstrap identity, local deterministic providers, and the configured desktop/tablet/mobile Chromium plus Firefox/WebKit projects.
- Result: **224 discovered, 224 started, 213 passed, 0 failed, 11 explicit skips, 0 did-not-run** in **27.7 minutes**.

### Result

The current frontend head `352f2f8c8e131c8f1fd2800a697cb6b4c5a364a0` passed the complete configured browser matrix. The skips remain explicit provider/credential or project-scope guards; they are not hidden failures. Real staging provider delivery and deployed SHA parity remain external certification gates.

## AUTO-0023 — Refresh frontend maintainability evidence after reservation-filter refactor

Priority: P1 release evidence  \
Date: 2026-09-27

The frontend reservation filtering helper was simplified into a decision table on certification commit `352f2f8c8e131c8f1fd2800a697cb6b4c5a364a0`. This is behavior-preserving: the full frontend unit suite remains green and the production build/typecheck remain successful.

Evidence:

- Frontend Vitest: **146 passed** across 34 files.
- V8 coverage: **61.16% lines**, **57.12% statements**, **48.76% branches**, **53.49% functions**.
- Typecheck: PASS; production build: PASS; lint: **0 errors / 107 warnings**; `npm audit --audit-level=high`: **0 known vulnerabilities**.
- SonarQube local refresh on port 9002: task `7e06d0bd-3027-4f56-a7d1-2c38f131bb84`, analysis `cb921fa4-45b3-482c-802a-e589f2919315`, Quality Gate **OK**, 0 bugs, 0 vulnerabilities, 0 hotspots, 972 open code smells, 56.6% imported line coverage, and 1.4% duplication.

The existing maintainability backlog and externally blocked staging gates remain open; this update records the latest evidence without claiming production readiness.

## AUTO-0022 — Refresh certification evidence on current release heads

Priority: P1 release evidence  \
Date: 2026-09-27

The current isolated certification branches were re-verified without touching the original dirty workspace, staging data, production, or protected `main` branches.

Evidence:

- Backend current head `0631b112195be3d4f3df1064b6c4cee31dc3aae2`: complete solution with disposable PostgreSQL and MinIO enabled passed **213**, failed **0**, skipped **0**. The focused real MinIO provider run passed **2/2** and covered upload/overwrite, signed download, unauthorized access, traversal rejection, and size-limit rejection. A uniquely prefixed object remained readable after restarting the disposable MinIO container, confirming the mounted Docker volume persisted it.
- Frontend current head `3b350a28a06f8e3f8b38d8c2448af60b6145becb`: Vitest passed **146/146** across 34 files; V8 coverage was **61.17% lines**, **57.14% statements**, **48.73% branches**, and **53.52% functions**. Typecheck and production build passed. Lint reported **0 errors / 112 warnings**. `npm audit --audit-level=high` reported **0 known vulnerabilities**.
- Staging read-only probes remain: `/api/health/live` 200; `/api/health/ready` 200 with database `ready` and storage `CONFIGURED`; `/api/health/version` 404; `/version.json` is still the SPA HTML fallback. Exact deployed SHA parity is therefore still **UNKNOWN**, not inferred from health status.
- Gitleaks triage was completed with redacted output only: frontend had no findings; backend had 7 historical/example-file `generic-api-key` matches (duplicate historical commits, placeholders, and development configuration); root had 6,729 raw matches dominated by generated Sonar issue JSON plus one placeholder environment-example match. The findings were classified by redacted source/path, and no active credential requiring rotation was identified in the isolated checkouts.
- A fresh frontend Sonar analysis at `3b350a28a06f8e3f8b38d8c2448af60b6145becb` completed server-side: Quality Gate `OK` for the configured new-code gate, 0 bugs, 0 vulnerabilities, 0 hotspots, 977 open code smells, 56.6% Sonar-imported line coverage, and 1.4% duplication. This refresh confirms scanner execution and current coverage ingestion; it does not close the existing maintainability backlog.

Remaining release gates are unchanged: real Brevo delivery, staging MinIO/upload and backup proof, staging role/IDOR evidence, exact deployment SHA parity, Sonar maintainability/coverage remediation, and the unresolved exact Gold/Platinum commercial values.

## AUTO-0001 — Apply approved 10 percent guest platform fee

Priority: P0 pricing integrity  
Date: 2026-09-27

### Problem

Active NestyStay logic still calculated a 9% standard guest platform fee even though the current approved handwritten amendment sets the post-launch guest fee to 10%. The stale value appeared in backend rules, pricebooks, the persistent migration path, frontend estimates/copy, and regression expectations.

### Changes

- Changed active backend standard guest fee resolution and default pricebook entries to 10%.
- Added a forward-only EF migration to update existing `milestone_pricebook_entry` and `pricebook_entry` rows without rewriting migration history.
- Changed the frontend search estimate and booking copy to 10%.
- Updated affected tests and booking totals.
- Preserved historical migration/audit references to the former typed value.

### Files

- Backend `BusinessRules.cs`, `PricebookService.cs`, `NestyStaySeed.cs`.
- Backend migration `20260927100000_ApplyPostLaunchGuestFee.cs`.
- Backend application/infrastructure tests.
- Frontend `PublicSearchMap.tsx`, `BookingShell.tsx`, `BookingReviewPage.tsx`, `BookingModal.test.tsx`, and coming-soon pricing copy.
- Updated `NESTYSTAY_FULL_GAP_ANALYSIS.md`, `NESTYSTAY_GAP_MATRIX.json`, and `NESTYSTAY_STATE.md`.

### Tests

- Backend full solution with local MinIO: 210 passed, 0 failed, 0 skipped.
- Frontend unit: 128 passed.
- Frontend typecheck/build: PASS.
- Frontend lint: 0 errors, 112 warnings.

### Result

The active standard guest fee is now aligned with the approved 10% rule and existing database rows have a safe forward migration path.

### Discovered Follow-ups

- Gold/Platinum founding economics still need the exact amended model.
- External provider and staging SHA verification remain outstanding.
- Gate Guard lifecycle/authentication is locally implemented; staging and browser evidence remain outstanding.

## AUTO-0002 — Add scoped Gate Guard lifecycle and authenticated QR validation

Priority: P1 trust and access control  \
Date: 2026-09-27

### Problem

The code had a `GateGuard` enum and QR UI, but no complete invitation/acceptance/assignment lifecycle. QR validation did not enforce a property-scoped Gate Guard authorization boundary, and Gate Guard members could enter Property Manager workspace paths.

### Changes

- Allowed the existing Property Manager staff invitation flow to create `GATE_GUARD` memberships only with one or more property scopes and no finance/payout capabilities.
- Synchronized the effective `GateGuard` user role on acceptance and removed it on revocation.
- Added authenticated `POST /api/property-manager/qr/validate-authenticated` with active membership and property-scope enforcement.
- Kept the existing anonymous validator for guest/pass compatibility without attributing scans to an untrusted signed-in actor.
- Denied Gate Guard users from PM finance and professional operations contexts.
- Added frontend role typing, navigation, workspace labeling, and authenticated validator API usage.

### Evidence

- Backend commit: `7352a95`.
- Frontend commit: `e9fde6e`.
- Backend full solution with local MinIO: 211 passed, 0 failed, 0 skipped.
- Backend Gate Guard authorization matrix: 4 passed, 0 failed, 0 skipped.
- Frontend unit suite: 128 passed; typecheck and production build passed; lint 0 errors / 112 warnings.

### Remaining qualification

Staging account provisioning, full browser coverage for the new role, physical gate/device integration, and external deployment verification remain outstanding. No production or staging environment was modified.

## AUTO-0003 — Harden legacy Gate Guard assignment path

Priority: P1 access-control integrity  \\
Date: 2026-09-27

### Problem

The historical `/api/property-manager/staff` endpoint accepted arbitrary role text, including `GATE_GUARD`, but only persisted a legacy row. That path could not create the scoped P0 membership or synchronize the effective `GateGuard` user role.

### Changes

- Reject `GATE_GUARD` on the legacy endpoint with an explicit instruction to use `/api/property-manager/p0/members`.
- Keep the supported P0 invite/accept/revoke lifecycle unchanged and covered.
- Add an API regression test proving the legacy route cannot create an unscoped Gate Guard assignment.

### Evidence

- Backend commit: `d2a3ac3`.
- Full backend solution with local MinIO: 212 passed, 0 failed, 0 skipped.
- Focused Property Manager authorization tests: 5 passed, 0 failed, 0 skipped.

## AUTO-0004 — Make integration health report actual storage readiness

Priority: P1 operational correctness  \\
Date: 2026-09-27

### Changes

- Health integration status now uses `IStorageProvider.CheckReadinessAsync`.
- The response reports the configured provider name, actual readiness status, and a safe provider detail.
- Provider exceptions are converted to an explicit `UNAVAILABLE` result rather than an unconditional success claim.
- Added health regression assertions.

### Evidence

- Backend commit: `094b539`.
- Health tests: 14 passed, 0 failed, 0 skipped.
- Full backend solution with local MinIO: 212 passed, 0 failed, 0 skipped.

## AUTO-0006 — Complete local release-certification refresh

Date: 2026-09-27  \\
Scope: isolated certification worktrees only; no staging/production changes.

- Applied the forward EF migration `20260927055223_ApplyCurrentModelAlignment`; it aligns the three active pricebook seed rows to the approved 10% guest fee without editing historical migrations.
- Confirmed the missing badge-review migration locally and verified Admin badge endpoints return 200 after migrations.
- Backend full solution with local PostgreSQL and disposable MinIO enabled: **212 passed, 0 failed, 0 skipped**.
- MinIO provider focused I/O tests: **2 passed, 0 failed, 0 skipped**.
- Frontend: **128 unit tests passed** across 31 files; Vitest line coverage **60.11%**; typecheck and production build passed; lint **0 errors / 112 warnings**; npm audit **0 known vulnerabilities**.
- Full Playwright matrix: **224 discovered / 224 started / 216 passed / 0 failed / 8 explicit skips / 0 did-not-run**. Skips are explicit scope/provider guards; production authenticated smoke still needs runtime smoke credentials.
- Fresh local Sonar analyses completed and exported. Backend and frontend gates remain failed on coverage/new-code debt; root/platform passed its new-code gate. Exact measures and issue exports are under `testing-evidence/sonarqube/`.

Remaining release blockers: real Brevo delivery, staging MinIO/upload proof, exact deployed frontend/backend SHA parity, complete staging role/IDOR evidence, and Sonar remediation. Local deterministic verification is green but is not equivalent to staging or production certification.

## AUTO-0007 — Align root monorepo pricing source

Priority: P0 pricing integrity  \\
Date: 2026-09-27

### Problem

The split backend/frontend branches had the approved 10% standard guest fee, but the tracked root monorepo copies still contained active 9% logic and copy. A root checkout could therefore calculate or display a stale guest fee even though the split repositories were corrected.

### Changes

- Updated root backend `PricebookService`, seed entries, and schema regression expectation to 10%.
- Updated root frontend search estimate and booking copy/comment to 10%.
- Generated the forward-only EF migration `20260927072014_ApplyApprovedGuestFee`; it changes only three active pricebook rows from 9m to 10m and reverses those rows on Down. Historical migrations were not edited.
- Verified the remaining active-source scan is clean; the only remaining `0.09` match is CSS letter-spacing.

### Evidence

- Root backend release suite: 183 passed, 0 failed, 0 skipped.
- Root frontend focused tests: 48 passed; typecheck PASS; production build PASS; lint 0 errors / 84 warnings; npm audit 0 known vulnerabilities.

### Result

The root monorepo and split source now represent the same approved standard guest fee. The root source commit `7d0b5285639838dbb06603b8e7065a5f1882922e` is pushed to the existing protected certification branch and remains subject to PR review; no main bypass was used.

## AUTO-0011 — Close remaining root pricing constant drift

Priority: P0 pricing integrity  \\
Date: 2026-09-27

### Problem

The root monorepo still declared `ContractGuestPlatformFeePercent = 9m` even though its pricebook/service/frontend copies and both split repositories had been moved to the approved 10% rule. This was discovered by a second exact-value scan after the earlier source-alignment commit.

### Changes

- Changed the root domain constant to 10m.
- Added the same standard-fee regression assertion to root and split backend domain tests.
- Preserved historical migrations and unrelated CSS values.

### Tests

- Root domain pricing tests: 6 passed, 0 failed, 0 skipped.
- Split backend domain pricing tests: 6 passed, 0 failed, 0 skipped.
- Root commit: `eadf8cd61e199f6bc291c7dff9c51aae991430dd`.
- Backend commit: `6277a950d563ce32e01a51223776fac9981529dd`.

### Result

The active standard guest fee is now 10% across the root monorepo, split backend, split frontend, seeds, pricebook services, estimates/copy, and regression expectations. The branches remain protected PR branches; main was not bypassed.

### Discovered Follow-ups

- Gold/Platinum founding economics remain unresolved because exact amended handwritten values are not machine-readable.
- External provider, staging SHA, Sonar remediation, Gitleaks review, and staging role/IDOR evidence remain open.

## AUTO-0008 — Re-run local authorization and MinIO evidence

Priority: P1 release evidence  \\
Date: 2026-09-27

The next local-only certification pass re-ran the existing security boundaries and private MinIO integration with the disposable audit container explicitly configured. No application bypass, staging change, or production change was used.

Evidence:

- Cross-resource authorization, Property Manager authorization, cookie/session security, signed-access-token security, and webhook-security filters: **28 passed, 0 failed, 0 skipped**.
- MinIO provider tests with `MINIO_TEST_ENDPOINT=http://127.0.0.1:19000`: **2 passed, 0 failed, 0 skipped**. Coverage included upload/overwrite, signed download, wrong-credential denial, traversal rejection, and size validation.
- Complete split-backend solution rerun with the private MinIO container configured: **212 passed, 0 failed, 0 skipped**.

Result:

The local authorization and MinIO evidence is stronger and current. It still does not certify staging credentials, production storage persistence/backups, real Brevo delivery, or deployment SHA parity.

## AUTO-0009 — Verify public staging health and build metadata

Priority: P1 deployment verification  \\
Date: 2026-09-27

A read-only probe was run against `https://staging.nestystay.net`.

Evidence:

- `/api/health/live`: HTTP 200, `status=ok`.
- `/api/health/ready`: HTTP 200, database `ready`, storage `CONFIGURED`.
- `/api/health/version`: HTTP 404, so the deployed backend revision is not publicly observable.
- `/version.json`: HTTP 200 but content type/body is the SPA HTML fallback, not JSON. The deployed frontend revision is therefore not publicly observable.

Result:

Staging liveness/readiness is healthy, but exact SHA parity is **BLOCKED_EXTERNAL** until Terrence deploys the current branches and configures nginx/static serving for the version surfaces. Readiness `CONFIGURED` is not treated as proof of real MinIO I/O, backup durability, or upload authorization.

## AUTO-0010 — Make PostgreSQL concurrency evidence honest

Priority: P1 test integrity / booking concurrency  \\
Date: 2026-09-27

### Problem

`PropertyManagerTwoInstancePostgresTests` returned early when `NESTYSTAY_POSTGRES_TEST_CONNECTION` was absent. That made the full suite report a pass without executing the multi-instance PostgreSQL serialization and idempotency path.

### Changes

- Added `PostgresIntegrationFactAttribute`, following the repository's existing MinIO integration-test pattern.
- The test now reports an explicit skip when the disposable PostgreSQL connection is not configured and executes normally when it is present.
- No authentication or application behavior was weakened.

### Tests

- Configured disposable PostgreSQL run: **1 passed, 0 failed, 0 skipped**.
- Unconfigured run: **0 passed, 0 failed, 1 explicit skip**.
- Full backend solution with disposable PostgreSQL and MinIO configured: **212 passed, 0 failed, 0 skipped**.

### Result

The concurrency evidence now distinguishes a real database run from an unavailable integration environment instead of silently treating the latter as success.

## AUTO-0012 — Align root monorepo Gate Guard lifecycle

Priority: P1 access-control integrity  \\
Date: 2026-09-27

### Problem

The split backend contained the current property-scoped Gate Guard lifecycle and authenticated QR authorization, but the tracked root monorepo copy did not. A root checkout could therefore drift from the split runtime and accept an unscoped legacy Gate Guard assignment.

### Changes

- Added `GATE_GUARD` scope validation to the root P0 invitation and update paths.
- Synchronized the effective user role on acceptance and removed it on revocation.
- Added the authenticated, property-scoped QR validation endpoint and denied ordinary PM finance/professional workflows to Gate Guards.
- Made the legacy `/api/property-manager/staff` route reject Gate Guard assignments.
- Added focused root API regression tests and corrected stale root test expectations for the approved 10% guest fee.

### Evidence

- Root commit: `20e6d9e411f112e263f4fbe9ee3c1012cfd82546`.
- Root backend release suite: **186 passed, 0 failed, 0 skipped**.
- Root Gate Guard regression tests: **2 passed, 0 failed, 0 skipped**.
- PR #10 remains open for protected review; no main or staging bypass was used.
## AUTO-0013 — Complete local browser regression

Date: 2026-09-27

The complete isolated Playwright matrix was rerun with a real server-issued disposable Admin fixture and the local PostgreSQL-backed backend. **224 discovered, 224 started, 213 passed, 0 failed, 11 explicit skips, 0 did-not-run.** Desktop Chromium, tablet Chromium, mobile Chromium, and the configured Firefox/WebKit critical smoke entries executed. The separate admin-token-gated checks passed **12/12**. A follow-up MinIO browser run with the private disposable container passed **3/3** across desktop/tablet/mobile Chromium. Skips remain explicit provider/credential or viewport-scope guards for the full run, deployment smoke credentials, and mobile-only checks. No source, staging, production, protected `main`, or original dirty workspace was changed.

## AUTO-0014 — Expand frontend behavioral coverage

Date: 2026-09-27

Added behavioral Vitest coverage for the real host view router, deferred landing-section loading (IntersectionObserver and fallback paths), and Google Identity Services success/error paths. The changes are isolated to the frontend certification branch and do not alter production behavior.

Evidence:

- Frontend commit: `325e585` (`codex/final-release-certification`), pushed to the existing protected PR branch.
- Full frontend unit suite: **146 passed, 0 failed across 34 files**.
- Fresh V8 coverage: **61.14% lines, 57.08% statements, 48.71% branches, 53.38% functions**.
- Typecheck: PASS; production build: PASS; lint: 0 errors / 112 warnings; `npm audit --audit-level=moderate`: 0 known vulnerabilities.

The local coverage target is now above 60% lines. The Sonar frontend gate has not yet been re-run at this new SHA, so its prior server-side metrics remain the authoritative Sonar result until refreshed.

## AUTO-0015 — Attempt fresh frontend Sonar refresh

Date: 2026-09-27

The frontend LCOV was ready at certification SHA `325e585`, but the local SonarScanner could not authenticate to `http://localhost:9000` and returned HTTP 401 before analysis. No server-side metrics were changed or inferred. The prior frontend Sonar result at `96cbac8` remains the authoritative gate result until a valid local Sonar session/token is supplied.

## AUTO-0016 — Refactor admin reporting maintainability hotspots

Date: 2026-09-27

Refactored the Admin Insights and Admin Pricebook/Campaigns containers into smaller render components, preserving their existing API behavior and tested empty/loading/error/report states. Frontend commit `3b350a2` was pushed to the existing protected certification branch.

Evidence:

- Full frontend unit suite: **146 passed, 0 failed across 34 files**.
- Fresh V8 coverage: **61.17% lines, 57.12% statements, 48.75% branches, 53.47% functions**.
- Typecheck: PASS; production build: PASS; lint: 0 errors / 112 warnings.

The server-side Sonar refresh still requires valid local authentication; no new Sonar result is claimed yet.

## AUTO-0019 — Refactor badge review maintainability without behavior change

Priority: P1 quality debt  \\
Date: 2026-09-27

The badge review engine contained a cognitive-complexity violation and a nested conditional in the result action. The evaluation behavior was preserved while extracting named helpers for Verified, Trusted, and Wellness requirements and making the approval action branch explicit.

Evidence:

- Backend commit: `e195e8b1720157cbe1c172a30e658b6dc6be7215`, pushed to `codex/final-release-certification`.
- Focused phase-two tests: 7 passed, 0 failed, 0 skipped.
- Complete backend suite after the change: 211 passed, 0 failed, 2 explicit integration skips when the local integration variables are absent; the MinIO/PostgreSQL scenarios are covered in the configured runs already recorded in `NESTYSTAY_STATE.md`.
- No schema, migration, provider, authorization, or business rule was changed.

The next Sonar refresh is still required at `e195e8b`; the latest exported Sonar metrics remain the scan at `6277a95` and are not silently re-labeled as current.

Backend PR #9 check run `36314132207` subsequently passed restore, build, and test. Its staging and production deploy jobs were skipped because the commit is on the protected certification branch.

## AUTO-0018 — Audit founding Gold/Platinum commercial lifecycle

Priority: P0 business-rule integrity  \\
Date: 2026-09-27

Audited the phase-two pricebook, domain rules, in-memory and EF stores, API contracts, frontend admin surface, seed data, and regression tests for Gold/Platinum founding memberships.

Result:

- The implementation is confirmed to use lifetime per-booking guest flat fees (`$36` Gold and `$29` Platinum), with the shared host commission and no term/expiry or membership purchase amount.
- The current amendment describes a different time-limited, tier-specific model, so this is a confirmed `WRONG_LOGIC` finding.
- The repository does not preserve every handwritten amended commercial value in machine-readable form. The missing exact price/term/commission/per-booking sub-values are therefore `NEEDS_EXACT_VALUE`; they must not be guessed.

No source code, migrations, seed data, or tests were changed. The next implementation step is blocked only on the exact approved commercial values; local certification work continues on other unblocked release gates.

## AUTO-0020 — Complete post-refactor backend Sonar refresh

Priority: P1 certification evidence  \\
Date: 2026-09-27

The backend certification branch was re-analyzed on the disposable local SonarQube instance after commit `e195e8b1720157cbe1c172a30e658b6dc6be7215`. The scanner uploaded successfully and the server processed task `34d63bc6-ff30-460e-928c-385ae056a704`.

Evidence:

- Analysis status: `SUCCESS`.
- Quality Gate: `OK` for the configured new-violations-only condition; new violations: 0.
- Bugs/vulnerabilities/security hotspots: 0/0/0.
- Code smells: 805; line coverage: 15.1%; branch coverage: 48.7%; duplication: 6.9%.
- Six OpenCover reports were imported and Sonar identified 48 main source files with coverage. This is genuine server-side coverage import, but it is not a full backend coverage pass.
- The configured local PostgreSQL/MinIO targeted integration test passed 1/1 on a fresh disposable database. The all-at-once API runner was separately observed to hang before summary; its reproducible passing chunks are recorded in the current state/evidence and are not inflated into a single all-at-once pass.

The remaining local quality blockers are the incomplete full-source backend coverage and the maintainability backlog. External/staging blockers remain Brevo delivery, production MinIO proof/backups, deployed SHA parity, and staging role/IDOR evidence.

## AUTO-0021 — Stabilize the combined PostgreSQL API regression

Priority: P1 release evidence  \\
Date: 2026-09-27

### Problem

The API assembly passed when its in-memory tests and PostgreSQL concurrency test were run separately, but the combined run hung when the database-backed fixture executed in parallel with the in-memory fixtures. That prevented an honest complete configured backend regression and coverage run.

### Changes

Placed `PropertyManagerTwoInstancePostgresTests` in a dedicated xUnit collection with parallelization disabled for that collection. This changes test scheduling only; it does not alter production code, migrations, database behavior or authorization.

### Files

- `tests/NestyStay.Api.Tests/PropertyManagerTwoInstancePostgresTests.cs`

### Tests

- API assembly with PostgreSQL enabled: **151 passed, 0 failed, 0 skipped**.
- Full backend solution with PostgreSQL and MinIO enabled: **213 passed, 0 failed, 0 skipped**.
- Focused PostgreSQL concurrency test: **1 passed, 0 failed, 0 skipped**.

### Result

The full configured regression is now deterministic without hiding or skipping the real database test. Commit `0631b112195be3d4f3df1064b6c4cee31dc3aae2` was pushed to the existing protected backend certification branch/PR.

### Discovered Follow-ups

The XPlat coverage collector still stalls after the API test host starts, including a single PostgreSQL API test from a clean detached checkout. The exact test passes without coverage; this remains a tooling/coverage evidence blocker and is not being reported as a test failure. A fresh Sonar upload at the new test-only SHA is still appropriate after the coverage evidence decision.

## AUTO-0031 — Share Property Manager owner-approval enforcement

Priority: P1 maintainability / financial authorization  \\
Date: 2026-09-28

The professional Property Manager store duplicated the full owner-approval lookup and validation in both maintenance and work-order financial paths. The shared helper now preserves each path's source type, property/owner scope, amount/currency checks, expiry rules, threshold bypass, and user-facing error text while reducing drift risk between the two financial workflows.

Evidence:

- Backend commit: `36c9c78`, pushed to `codex/final-release-certification`.
- Focused Property Manager workflow/P0 tests: **19 passed, 0 failed, 0 skipped**.
- Complete non-container backend solution: **225 passed, 0 failed, 2 explicit environment skips** (MinIO and two-instance PostgreSQL).
- `git diff --check`: PASS; no migrations, provider configuration, secrets, staging, production, or protected `main` were changed.

The Docker engine remained unresponsive during a single controlled readiness check, so the post-refactor Docker-backed MinIO/PostgreSQL rerun and fresh Sonar upload remain pending. No current-head container or Sonar result is inferred from the passing non-container run.

