# Current State

## Last Completed Task

AUTO-0027 — Re-run the local authorization and IDOR regression matrix.

## Validation Result

- Backend full solution with local PostgreSQL and MinIO enabled: 213 passed, 0 failed, 0 skipped.
- Frontend unit suite: 146 passed across 34 files.
- Fresh frontend V8 coverage: 61.16% lines (4,634/7,576), 57.12% statements, 48.76% branches, 53.49% functions.
- Frontend typecheck: PASS.
- Frontend production build: PASS.
- Frontend lint: 0 errors, 107 warnings.
- Frontend npm audit at high severity: 0 known vulnerabilities.
- Latest browser regression at frontend `352f2f8`: 224 discovered, 224 started, 213 passed, 0 failed, 11 explicit skips, 0 did-not-run in 27.7 minutes. Desktop Chromium, tablet Chromium, mobile Chromium, Firefox, and WebKit configured scopes ran against the local PostgreSQL-backed application. Evidence: `testing-evidence/final-hardening/07-browser/LOCAL-PLAYWRIGHT-2026-09-27.md`.
- Fresh local SonarQube analyses completed and processed at the current certification heads. The prior baseline remains on isolated server port 9001. A post-refactor backend refresh on port 9002 at `e195e8b` completed server-side: 0 bugs, 0 vulnerabilities, 0 hotspots, 805 code smells, 15.1% Sonar line coverage, 48.7% branch coverage, 6.9% duplication and 0 new violations under the configured new-violations-only gate. Six OpenCover reports imported, but only 48 backend source files had coverage; this is not a production-readiness pass for full coverage or maintainability. Frontend refresh at `352f2f8` completed server-side: Quality Gate `OK`, 0 bugs, 0 vulnerabilities, 0 hotspots, 972 code smells, 56.6% Sonar-imported line coverage, and 1.4% duplication. Root/platform scope remains 0 current issues.
- Sonar evidence: `testing-evidence/sonarqube/CERTIFICATION-SUMMARY-2026-09-27.json` and `.md`, plus `BACKEND-POST-REFACTOR-2026-09-27.*`. The post-refactor backend task is `34d63bc6-ff30-460e-928c-385ae056a704`; prior baseline tasks remain backend `21416d9c-313c-4d75-a8fc-59d9a4ae0575`, frontend `60b5e8ed-0014-4772-be00-7e6feb5d035d`, root `6686f78b-6ceb-4ddf-87c0-fc0c0c13a205`.
- Pushed certification source revisions: root evidence branch `2569a9ef66ebc73b9127e84103b9fff80158c274`, backend `0631b112195be3d4f3df1064b6c4cee31dc3aae2`, frontend `352f2f8c8e131c8f1fd2800a697cb6b4c5a364a0`.
- Root monorepo validation after Gate Guard parity and pricing alignment: backend release tests 186 passed, 0 failed, 0 skipped; focused Gate Guard API tests 2 passed; frontend typecheck/build passed; lint 0 errors / 84 warnings; npm audit 0 known vulnerabilities.
- Follow-up split-backend security matrix: 28 passed, 0 failed, 0 skipped across cross-resource authorization, Property Manager scope, cookie/session, signed-token, and webhook-security tests.
- Follow-up disposable MinIO I/O run: 2 passed, 0 failed, 0 skipped with `MINIO_TEST_ENDPOINT` configured; full backend solution rerun: 212 passed, 0 failed, 0 skipped.
- Read-only staging probe: `/api/health/live` 200; `/api/health/ready` 200 with database `ready` and storage `CONFIGURED`; `/api/health/version` 404; `/version.json` returns the SPA HTML fallback instead of JSON. Deployed SHA parity therefore remains unverified.
- The PostgreSQL multi-instance integration test no longer silently passes when its connection string is absent: it now reports an explicit skip, and it passed 1/1 against the disposable local PostgreSQL container when configured.
- Browser MinIO opt-in rerun with `OBJECT_STORAGE_PROVIDER=minio`: 3 passed, 0 failed, 0 skipped across desktop/tablet/mobile Chromium, including upload, reload, signed download, and byte equality.
- Backend maintainability refactor at `e195e8b`: badge-review evaluation was split into named requirement helpers and the nested `NextAction` conditional was made explicit. Focused phase-two tests passed 7/7; the complete unconfigured backend run passed 211 with 2 explicit integration skips; the configured PostgreSQL/MinIO integration evidence passes when run against the disposable services. A fresh post-refactor Sonar upload and server-side processing also completed successfully; its evidence is under `testing-evidence/sonarqube/BACKEND-POST-REFACTOR-2026-09-27.*`.
- The configured API regression was initially decomposed to diagnose the all-at-once test-host hang: 105 non-Property-Manager tests passed, 45 in-memory Property Manager tests passed, and the PostgreSQL two-instance test passed 1/1. That diagnostic result is superseded by the dedicated non-parallel xUnit collection fix and the subsequent complete 151-test API run recorded below.
- The API integration test is now isolated in a non-parallel xUnit collection. The complete configured solution then passed 213/213 with no skips; the test-only fix is backend commit `0631b11`.
- A clean detached checkout of `0631b112195be3d4f3df1064b6c4cee31dc3aae2` reproduced a separate XPlat Code Coverage collector stall even for the single PostgreSQL API test after the test host started. The same test passes in 6 seconds without coverage, so this is recorded as coverage-tooling evidence uncertainty rather than a product/test failure; full backend Sonar coverage remains unproven.
- GitHub Actions backend PR check `36314132207` passed restore, build, and test for PR #9 at `e195e8b`; staging and production deploy jobs were skipped as expected on the protected feature branch.
- Current-head refresh: backend `0631b112195be3d4f3df1064b6c4cee31dc3aae2` passed the complete configured solution at 213/213 with disposable PostgreSQL and MinIO; frontend `352f2f8c8e131c8f1fd2800a697cb6b4c5a364a0` passed 146/146 unit tests, typecheck, build, and 0-error lint. The frontend refactor reduced lint warnings from 112 to 107 and preserved the local coverage profile. These current-head checks do not change the backend Sonar conclusion because the backend change is test-only; the recorded backend scan remains at `e195e8b`.
- Disposable MinIO persistence recheck: a uniquely prefixed object remained readable after restarting `nestystay-minio-audit` on its mounted Docker volume. This proves local volume persistence only; production backup/restore and staging object lifecycle remain unverified.
- Gitleaks triage: frontend 0 findings; backend 7 historical/example-file generic-api-key matches; root 6,729 raw matches dominated by generated Sonar JSON false positives. Findings were classified by redacted source/path; no active credential requiring rotation was found in the isolated checkouts.
- Fresh frontend Sonar refresh at current head `352f2f8c8e131c8f1fd2800a697cb6b4c5a364a0`: server-side processing succeeded; Quality Gate `OK` under the configured new-code gate, 0 bugs, 0 vulnerabilities, 0 hotspots, 972 open code smells, 56.6% Sonar line coverage, and 1.4% duplication. Evidence: `testing-evidence/sonarqube/FRONTEND-FILTER-REFACTOR-2026-09-27.md`.
- The backend now has a committed `dotnet-coverage` tool manifest and `tools/collect-coverage.ps1` runner at `0e58c1f`. It requires runtime-only disposable PostgreSQL and MinIO variables, restores the pinned collector, builds before instrumentation, and refuses to silently skip either integration path. The runner's credential-absence guard and tool restore passed. An initial runner attempt encountered a stale local Roslyn compiler lock; after that local process was cleared, the final runner produced a valid Cobertura report and four TRX files: Domain **6/6**, Application **24/24**, Infrastructure **32/32**, and API **151/151**—**213 passed, 0 failed, 0 skipped**. Excluding only EF-generated migration designer/snapshot source, the report measured **17,228/33,561 authored lines (51.33%)**. This does not meet the requested 80% backend coverage target.
- A new local Sonar .NET refresh was attempted twice with the full Visual Studio coverage report and generated-migration exclusions. Both analyzer-backed builds stalled before a server upload; the pre-existing backend Sonar result remains the latest server-side result. This is a local scanner/tooling blocker, not a backend product-test failure.
- The local authorization/IDOR regression matrix was rerun at backend `0e58c1f8b490a4ac3f9a905d4d8a14d44de845eb`: **13 passed, 0 failed, 0 skipped**. It covered cross-resource message/attachment, wellness/officer, provider, owner/manager/staff portfolio, Gate Guard invitation/scope/revocation, admin-only endpoint rejection, Property Manager policy rejection, badge ownership, and session invalidation. Evidence: `testing-evidence/final-hardening/04-authorization/LOCAL-AUTHORIZATION-MATRIX-2026-09-27.md`. This strengthens local authorization evidence but does not replace staging role/IDOR verification.

## AUTO-0018 — Audit founding Gold/Platinum commercial lifecycle

Priority: P0 business-rule integrity  \\
Date: 2026-09-27

The active founding-tier implementation was inspected in both the in-memory and EF-backed phase-two stores, the domain business rules, pricebook/seed data, API DTOs, frontend admin surface, and the existing phase-two regression tests.

Confirmed current behavior:

- Gold and Platinum are stored as founding tiers with lifetime per-booking guest flat fees (`$36` and `$29` respectively).
- Founding tiers use the shared host commission pricebook percentage rather than a tier-specific percentage.
- Founding records expose `IsLifetimeGuestFee` and have no term/expiry or membership purchase amount.
- The same behavior is asserted by `PhaseTwoWorkflowTests` and persisted by `MilestonePersistenceTests`.

The current audit evidence describes a later handwritten amendment with time-limited founding memberships, tier-specific host percentages, and additional commercial values. The repository does not contain a machine-readable source that establishes every exact amended price, term, commission, or per-booking value with sufficient certainty. The signed-rule interpretation therefore classifies the existing model as `WRONG_LOGIC`, while the unreadable mandatory sub-values are `NEEDS_EXACT_VALUE`.

No pricing code, schema, seed, migration, or test was changed. Inventing the missing values would create a new commercial rule and could make production pricing incorrect. This is the exact human business-input blocker for the founding-tier remediation.

## AUTO-0019 — Refactor badge review maintainability without behavior change

Priority: P1 quality debt  \\
Date: 2026-09-27

The badge review engine contained a cognitive-complexity violation and a nested conditional in the result action. The evaluation behavior was preserved while extracting named helpers for Verified, Trusted, and Wellness requirements and making the approval action branch explicit.

Evidence:

- Backend commit: `e195e8b1720157cbe1c172a30e658b6dc6be7215`, pushed to `codex/final-release-certification`.
- Focused phase-two tests: 7 passed, 0 failed, 0 skipped.
- Complete backend suite after the change: 211 passed, 0 failed, 2 explicit integration skips when the local integration variables are absent; the MinIO/PostgreSQL scenarios are covered in the configured runs already recorded above.
- No schema, migration, provider, authorization, or business rule was changed.

The post-refactor Sonar refresh at `e195e8b` is now recorded. The remaining Sonar blockers are full backend source coverage and the open maintainability backlog; the successful new-violations-only gate is not being treated as production certification.

## AUTO-0013 — Complete local Playwright regression

Priority: P1 release evidence  \\
Date: 2026-09-27

The complete browser matrix was rerun after starting the isolated backend with a real server-issued disposable Admin bootstrap account and the local PostgreSQL database. No browser identity was forged and no authentication bypass was added.

Evidence:

- 224 tests across 36 files were discovered and all 224 started.
- 213 passed, 0 failed, 11 explicit skips, and 0 did-not-run.
- Desktop Chromium, tablet Chromium, mobile Chromium, and the configured Firefox/WebKit critical smoke entries executed.
- The separate admin-token-gated group passed 12/12.
- The 11 skips are explicit provider/credential or viewport-scope guards: browser MinIO opt-in, deployment smoke credentials, and mobile-only checks. They are not hidden failures.

The local browser gate is therefore green for executed journeys, and the separate local MinIO browser path is green. Real staging Brevo delivery, staging MinIO I/O/backups, exact deployed SHA parity, and Sonar backend/frontend gate remediation remain open.

## Confirmed Complete

- Active standard guest platform fee is 10% in backend business rules, pricebook service, seed, frontend estimate/copy, and regression expectations.
- A forward EF migration updates existing pricebook rows to 10% without editing historical migrations.
- Local PostgreSQL and disposable MinIO-backed backend regression is green.
- Gate Guard invitation, acceptance, property scope, authenticated QR validation, PM-workspace denial, and revocation are locally covered by the API authorization matrix.
- Gate Guard is exposed in the frontend role model, navigation, workspace label, and authenticated QR validator.
- The historical `/api/property-manager/staff` endpoint now rejects `GATE_GUARD` instead of creating a membership that cannot grant the real scoped role; the supported `/api/property-manager/p0/members` lifecycle remains covered.
- The root monorepo now matches the split backend for Gate Guard invitation constraints, role synchronization, authenticated property-scoped QR validation, revocation, and PM-workspace denial.
- The admin integration-health endpoint now reports the selected storage provider's actual readiness status and safe detail instead of unconditionally reporting `CONFIGURED`.

## Partial

- M1: external Stripe/Identity/Brevo/storage/payout/insurance and staging SHA parity are not proven.
- M2: Gold/Platinum commercial membership model remains outdated; real Stripe lifecycle is not proven.
- M3: external provider, storage, payout and operational certification remain.
- M4: Gate Guard lifecycle/authentication is locally implemented; staging/browser certification, physical gate hardware, and geocoding remain unverified or out of current scope.
- M5: production storage/email/billing and complete manual responsive certification remain.
- Backend Sonar: the latest server-side scan at code-equivalent `e195e8b` reports 15.1% imported line coverage and 805 code smells. The now-reproducible `0e58c1f` collector completed all 213 backend tests and measures 51.33% authored-line coverage after excluding only EF-generated migration designer/snapshot source. A refreshed Sonar upload using that report is blocked by a local analyzer-build stall. The requested backend quality/maintainability target is not complete.
- Frontend Sonar: fresh scan at `352f2f8c8e131c8f1fd2800a697cb6b4c5a364a0` reports 56.6% Sonar-imported line coverage and 972 maintainability issues. The local V8 run is 61.16% lines, above the requested 60% local threshold, but the 80% new-code requirement is not claimed as passed.
- Platform Sonar: gate PASS for new-code scope; overall scanned orchestration findings remain separate from runtime certification.
- Full staging role/IDOR certification remains.
- Brevo real transport/mailbox delivery remains blocked by external configuration.

## Missing

- Geocoding provider/workflow.
- Native mobile application source, which is future scope unless explicitly promoted.
- Exact deployed staging frontend/backend SHA evidence.

## Broken

- None identified in this remediation slice. The anonymous QR validator remains a guest/pass compatibility route; Gate Guard scans use the separate authenticated, property-scoped endpoint.

## Wrong Business Logic

- Gold/Platinum still use lifetime flat guest fees and shared host commission instead of the current amended term-based commercial model.

## P0

- Resolve the Gold/Platinum commercial model and any unreadable mandatory values.
- Confirm production/staging provider configuration and deployed SHA parity before release.

## P1

- Verify private MinIO configuration, backup/retention and upload workflows.
- Verify Brevo acceptance, receipt, retry and duplicate behavior.
- Complete current staging role/IDOR matrix.
- Complete current staging/browser role and IDOR checks, including the new Gate Guard boundary.
- Review and remediate the fresh Sonar findings, then rerun the gates after behavior-preserving fixes and additional tests.
- Diagnose the local analyzer-backed Sonar build stall and complete an uploaded backend coverage refresh; do not infer coverage from the passing no-coverage regression. The committed `dotnet-coverage` runner makes the full disposable-service coverage input reproducible.

## P2

- Decide whether geocoding is current contractual scope.
- Consolidate/document historical SpecScreens and aliases.
- Complete human accessibility and operational mobile certification.

## P3

- Reduce existing frontend lint warnings and maintainability debt after release blockers are handled.

## External Blockers

- Safe staging/production credentials and access for MinIO, Brevo, Stripe, Stripe Identity, Stripe Connect, InsuraGuest, and deployed build metadata.
- Exact handwritten Gold/Platinum values where the source cannot be read with certainty.
- Human accessibility/legal/privacy certification.

## Newly Discovered Problems

- Frontend `PublicSearchMap` contained a second hardcoded 9% estimate independent of the backend quote.
- The persistent pricebook migration `20260830144204_AlignSignedContractM1M2PricingV2` writes the old 9% value, requiring a forward correction migration rather than history rewriting.
- The root monorepo copies of the split backend/frontend sources still contained active 9% guest-fee logic after the split repositories had been corrected. This was a repository-alignment defect, not a new business-rule decision.
- A deeper cross-repository pricing scan found the root domain constant `ContractGuestPlatformFeePercent = 9m`; it is now 10m with a root and split-backend regression assertion. Active 9% pricing logic is now absent from root and split runtime source; historical migrations and unrelated CSS ratios remain excluded by design.
- The .NET XPlat Code Coverage collector stalls on the API test host in both the working checkout and a clean detached checkout, while the exact API tests pass without coverage; Sonar coverage evidence therefore cannot yet be refreshed from a complete API run.

## AUTO-0012 — Align root monorepo Gate Guard lifecycle

Priority: P1 access-control integrity  \\
Date: 2026-09-27

The root monorepo was missing the split backend's current Gate Guard lifecycle and authenticated QR authorization even though the split backend branch already contained it. This created repository-level behavior drift: a root checkout could accept an unscoped legacy Gate Guard assignment or fail to enforce the scoped authenticated flow.

Changes:

- Added the property-scoped `GATE_GUARD` invitation constraints and role synchronization on invite, acceptance, update, and revocation.
- Added `POST /api/property-manager/qr/validate-authenticated` and denied Gate Guard users ordinary PM finance/professional workflows.
- Kept the anonymous compatibility validator and made the legacy `/api/property-manager/staff` route reject Gate Guard assignments.
- Added root API regression tests and updated stale root fee expectations to the approved 10% rule.

Evidence:

- Root commit: `20e6d9e411f112e263f4fbe9ee3c1012cfd82546`.
- Root backend release suite: 186 passed, 0 failed, 0 skipped.
- Root Gate Guard regression tests: 2 passed, 0 failed, 0 skipped.
- Protected main was not bypassed; PR #10 remains the review boundary.

## AUTO-0007 — Align root monorepo pricing source

Priority: P0 pricing integrity  \\
Date: 2026-09-27

The root monorepo is tracked separately from the split backend/frontend repositories and still contained active 9% guest-fee values in its backend service/seed/test copies and frontend estimate/copy. Those values could produce an incorrect estimate when the root checkout was used for local builds or orchestration.

Changes:

- Updated root backend pricebook resolution, seed values, and schema expectation from 9% to the approved 10% standard guest fee.
- Updated root frontend search estimate and booking copy/comment to 10%.
- Generated forward EF migration `20260927072014_ApplyApprovedGuestFee` with a reversible three-row pricebook update; no historical migration was edited.
- Re-scanned active non-migration source: no pricing 9% references remain; the remaining `0.09` match is unrelated CSS letter-spacing.

Evidence:

- Root backend release suite: 183 passed, 0 failed, 0 skipped.
- Root frontend focused tests: 48 passed; typecheck PASS; production build PASS; lint 0 errors / 84 warnings; npm audit 0 known vulnerabilities.
- Original dirty workspace, staging, production, and protected `main` were not modified.

## Decisions

- Historical migrations and historical audit documents are preserved.
- Standard guest fee correction is complete; Gold/Platinum is not guessed.
- Local deterministic providers are never presented as live external verification.
- Original dirty workspace remains untouched; remediation is isolated to certification branches.
- Sonar scanner success is not treated as a Quality Gate pass; the current exported metrics and issues are recorded in `testing-evidence/sonarqube/`.

## AUTO-0006 — Local release certification refresh

Priority: P1 release evidence  \\
Date: 2026-09-27

The local certification run was completed from isolated worktrees without touching the original dirty workspace, staging, production, protected `main`, or historical migrations.

Evidence:

- Backend forward migration `20260927055223_ApplyCurrentModelAlignment` was generated and applied. It creates no tables and only aligns three existing active pricebook seed rows from the obsolete 9% guest fee to the approved 10%; historical migrations were not edited.
- The missing `milestone_badge_review` migration was applied locally; the badge Admin endpoints returned 200 after migration.
- Backend full solution with local PostgreSQL and disposable MinIO enabled: **212 passed, 0 failed, 0 skipped**.
- Focused MinIO provider I/O tests: **2 passed, 0 failed, 0 skipped**.
- Frontend unit suite: **128 passed across 31 files**; Vitest line coverage **60.11%**. Typecheck and production build passed; lint had **0 errors / 112 warnings**; npm audit reported **0 known vulnerabilities**.
- Complete local Playwright run: **224 discovered, 224 started, 216 passed, 0 failed, 8 explicit skips, 0 did-not-run**. The skips are explicit project/provider guards; the production authenticated-smoke guard still requires runtime `SMOKE_EMAIL`/`SMOKE_PASSWORD`.
- Fresh local SonarQube analyses completed server-side for backend, frontend, and root/platform. Backend and frontend gates still fail on coverage/new-code debt; the full issue/measure exports are under `testing-evidence/sonarqube/`.

Current release blockers are external/staging proof and quality debt: real Brevo delivery, staging MinIO/upload verification, exact staging frontend/backend SHA parity, complete staging role/IDOR evidence, and remediation of the fresh backend/frontend Sonar gates. The local application/browser gate is green; that does not equal production readiness.

## Next Best Action

Resolve the Gold/Platinum model only after exact mandatory values are confirmed; otherwise remediate the fresh backend/frontend Sonar findings and continue local security/IDOR hardening while external providers remain blocked.

## AUTO-0005 — Fresh local SonarQube certification

Priority: P1 release evidence  \
Date: 2026-09-27

Fresh analyses were submitted to the local SonarQube server at the current isolated certification SHAs. The complete server-side exports are tracked under `testing-evidence/sonarqube/`.

Evidence:

- Backend SHA: `094b539f5d1b7abb2fb6724a8c77d47c68bebf3f`; 0 bugs, 0 vulnerabilities, 0 hotspots, 996 code smells, 14.4% overall coverage; Quality Gate **FAIL** because new coverage is 7.5%, new duplication is 3.20724%, and new violations are 11.
- Frontend SHA: `e9fde6e353120712a7c5f9b0e8fa51e6044191dc`; 0 bugs, 0 vulnerabilities, 0 hotspots, 973 code smells, 55.6% overall coverage; Quality Gate **FAIL** because new coverage is 64.9% and new violations are 4.
- Root/platform SHA: `36c3ccbe627bbe6dbf6029e806c042b961611f3c`; Quality Gate **PASS** for the scanned orchestration scope with 0 new violations. Its overall findings are not runtime application certification.
- The completed backend CI run for `094b539f` is `36295420610`: restore/build/test succeeded; deploy jobs were correctly skipped on the protected feature branch.

These results replace the previous `NOT_RUN` statement for fresh Sonar evidence. They do not constitute production readiness: storage/email delivery, staging SHA parity, complete staging authorization, exact Gold/Platinum values, and Sonar remediation remain open.

## AUTO-0003 — Harden legacy Gate Guard assignment path

Priority: P1 access-control integrity  \\
Date: 2026-09-27

The historical `/api/property-manager/staff` route accepted arbitrary role text, including `GATE_GUARD`, but persisted only a legacy staff row and did not create the scoped P0 membership or synchronize the effective user role. It now rejects that role with an explicit error so callers must use the property-scoped P0 invitation/acceptance/revocation flow.

Evidence:

- Backend commit: `d2a3ac3`.
- Full backend solution with local MinIO: 212 passed, 0 failed, 0 skipped.
- Focused Property Manager authorization tests: 5 passed, 0 failed, 0 skipped.

## AUTO-0004 — Make integration health report actual storage readiness

Priority: P1 operational correctness  \\
Date: 2026-09-27

The admin integration-health response now calls the configured `IStorageProvider` readiness check, reports its provider name and status, and returns a safe `UNAVAILABLE` result if the provider check throws. This prevents dashboards and deployment checks from presenting unavailable storage as configured.

Evidence:

- Backend commit: `094b539`.
- Health endpoint tests: 14 passed, 0 failed, 0 skipped.
- Full backend solution with local MinIO: 212 passed, 0 failed, 0 skipped.

## Loop Status

CONTINUE
