# Current State

## Authoritative refresh — AUTO-0043 — 2026-09-28

- Backend `e215b895dcb64e43e792362eba4940898d7e6936`: the configured disposable-service suite passed **303/303** with zero failures and zero skips (Domain 6, Application 98, Infrastructure 32, API 167), including real local MinIO and PostgreSQL integration paths. Exact-head Sonar processing reports 73.4% line coverage, 988 code smells, 20.4% duplication, 0 bugs/vulnerabilities/hotspots, and 0 new violations. The requested 80% coverage and maintainability gates remain open.
- Frontend `e47ace79cc1c31cb19ca047e491cb27c118b45b2`: **129/129** tracked Vitest tests across 35 files passed. Reproducible tracked-test V8 coverage is 32.81% lines, 30.98% statements, 24.38% branches, and 32.11% functions. Exact-head Sonar reports 32.7% line coverage, 972 code smells, 1.4% duplication, and 0 bugs/vulnerabilities/hotspots. Ignored `src/coverage` helper tests are excluded because they are not pushed source evidence.
- Existing browser evidence remains valid at runtime-equivalent frontend `20f199f`: **224/224** started, 213 passed, 0 failed, 11 explicit skips, and 0 did-not-run. The later frontend revision is test-only and did not alter runtime source.
- Root certification documentation is being refreshed from these exact heads. Protected `main`, staging, and production remain untouched.

## Historical prior refresh (superseded by AUTO-0043)

AUTO-0042 — Expand frontend behavioral coverage and refresh exact-commit Sonar evidence.

## Validation Result

- Current backend head `ff31270` was rerun with disposable local PostgreSQL and MinIO: Domain 6, Application 24, Infrastructure 32, and API 167; **229 passed, 0 failed, 0 skipped**. The real MinIO round-trip/overwrite/signed-download/invalid-credential test passed 1/1, and the PostgreSQL two-instance concurrency test passed 1/1.
- Frontend unit suite: **162 passed across 40 files** at frontend `14455cc`.
- Fresh frontend V8 coverage at `14455cc`: **63.99% lines (4,848/7,576), 60.16% statements, 51.61% branches, 56.77% functions**. The exact-commit Sonar refresh reports **59.3% executable-line coverage**, so the requested Sonar 60% threshold remains open.
- Frontend typecheck: PASS.
- Frontend production build: PASS.
- Frontend lint: 0 errors, 111 warnings.
- Frontend npm audit at high severity: 0 known vulnerabilities.
- Latest current-head browser regression at frontend `20f199f` (runtime-equivalent before the test-only `14455cc` commit): 224 discovered, 224 started, 213 passed, 0 failed, 11 explicit skips, 0 did-not-run in 26.5 minutes. Desktop Chromium, tablet Chromium, mobile Chromium, Firefox, and WebKit configured scopes ran against the disposable PostgreSQL-backed application with the local Admin fixture. Evidence: `testing-evidence/final-hardening/07-browser/LOCAL-PLAYWRIGHT-2026-09-28-CURRENT.md`.
- Fresh current-head SonarQube analyses were processed server-side on a disposable local instance. Backend `ff31270`: 0 bugs, 0 vulnerabilities, 0 hotspots, 798 code smells, 73.2% coverage, 6.9% duplication. Frontend `14455cc`: 0 bugs, 0 vulnerabilities, 0 hotspots, 972 code smells, 59.3% Sonar executable-line coverage, 1.4% duplication; the local V8 line report is 63.99%. Root runtime/orchestration `2334432`: 0 bugs, 0 vulnerabilities, 0 hotspots, 0 code smells. These disposable Quality Gate `OK` results have no configured conditions and do not close the requested coverage gates. Evidence: `testing-evidence/sonarqube/CURRENT-LOCAL-SCAN-2026-09-28.md` and the three JSON exports.
- Sonar evidence for this refresh is committed under `testing-evidence/sonarqube/BACKEND-CURRENT-2026-09-28.json`, `FRONTEND-CURRENT-2026-09-28.json`, `PLATFORM-CURRENT-2026-09-28.json`, and `CURRENT-LOCAL-SCAN-2026-09-28.md`. Backend analysis ID: `abba376b-fd73-4ce7-bebf-9654c6c34b8b`. The frontend scanner emitted non-fatal highlight warnings for long CSS lines; no application source was changed for that analyzer warning.
- Pushed certification source revisions: root evidence branch `8566b0cfee71c7ac507a8f7cad4726825409f686`, backend `ff31270f134c599fadb7dd7058f361fb45cc55ff`, frontend `14455cc69c96bd97917a35bc8ab4a66b030c645e`.
- Root monorepo validation after Gate Guard parity and pricing alignment: backend release tests 186 passed, 0 failed, 0 skipped; focused Gate Guard API tests 2 passed; frontend typecheck/build passed; lint 0 errors / 84 warnings; npm audit 0 known vulnerabilities.
- Follow-up split-backend security matrix: 28 passed, 0 failed, 0 skipped across cross-resource authorization, Property Manager scope, cookie/session, signed-token, and webhook-security tests.
- Follow-up disposable MinIO I/O run: 2 passed, 0 failed, 0 skipped with `MINIO_TEST_ENDPOINT` configured; full backend solution rerun: 212 passed, 0 failed, 0 skipped.
- Read-only staging probe: `/api/health/live` 200; `/api/health/ready` 200 with database `ready` and storage `CONFIGURED`; `/api/health/version` 404; `/version.json` returns the SPA HTML fallback instead of JSON. Deployed SHA parity therefore remains unverified.
- The PostgreSQL multi-instance integration test no longer silently passes when its connection string is absent: it now reports an explicit skip, and it passed 1/1 against the disposable local PostgreSQL container when configured.
- Browser MinIO opt-in rerun with `OBJECT_STORAGE_PROVIDER=minio`: the current full browser matrix includes the MinIO UI journey in desktop, tablet, and mobile Chromium; standalone current-head MinIO object I/O passed 1/1 with the disposable private container.
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
- A fresh disposable SonarQube server-side backend analysis now succeeded at `0e58c1f8b490a4ac3f9a905d4d8a14d44de845eb`: 213/213 coverage-runner tests passed, 0 bugs, 0 vulnerabilities, 0 hotspots, 995 open code smells, 20.4% duplication, and 72.4% line coverage. The server reported `OK` with no conditions configured on that disposable instance; the requested 80% coverage target is still unmet. Evidence: `testing-evidence/sonarqube/BACKEND-FRESH-CERTIFICATION-2026-09-27.md`.
- After adding behavioral coverage for signed storage downloads and platform metadata endpoints, the current backend head `393edd1e20269350ed565fff8dda53b997792e39` passed the full configured suite **222/222** with zero failures/skips. A fresh server-side Sonar refresh processed successfully: 0 bugs, 0 vulnerabilities, 0 hotspots, 996 code smells, 20.4% duplication, and raw line coverage **62.1%**. The same server’s file measures calculate **72.8%** when all EF migration files are excluded; neither reaches the requested 80%. Evidence: `testing-evidence/sonarqube/BACKEND-CURRENT-HEAD-2026-09-27.md`.
- The local authorization/IDOR regression matrix was rerun at current backend `393edd1e20269350ed565fff8dda53b997792e39`: **13 passed, 0 failed, 0 skipped**. It covered cross-resource message/attachment, wellness/officer, provider, owner/manager/staff portfolio, Gate Guard invitation/scope/revocation, admin-only endpoint rejection, Property Manager policy rejection, badge ownership, and session invalidation. Evidence: `testing-evidence/final-hardening/04-authorization/LOCAL-AUTHORIZATION-MATRIX-2026-09-27.md`. This strengthens local authorization evidence but does not replace staging role/IDOR verification.
- Current backend head `4ece56f` adds passkey credential ownership/lifecycle coverage: list isolation, persisted revocation, inactive lifecycle visibility, repeat-delete behavior, and cross-user delete denial. Focused tests passed **3/3** and the full configured suite passed **225/225** (Domain 6, Application 24, Infrastructure 32, API 163). The current XML coverage artifact was generated, but the local Docker daemon stopped before the fresh disposable Sonar server could process it; the latest verified server-side Sonar metrics therefore remain at `393edd1` and must not be treated as current-head measurements. Evidence: `testing-evidence/15-coverage/PASSKEY-OWNERSHIP-LIFECYCLE-2026-09-28.md`.
- Current backend head `b726d6d` centralizes Property Manager staff roles/statuses plus Gate Guard and finance validation shared by invitation and update paths. Focused Property Manager P0, authorization, and passkey tests passed **21/21**. The complete non-container suite passed **225/225** with **2 explicit integration skips** (MinIO and two-instance PostgreSQL); a current-head Docker-backed rerun is pending local Docker recovery. The exported Sonar issue inventory is triaged by rule/severity and runtime risk in `testing-evidence/sonarqube/BACKEND-MAINTAINABILITY-TRIAGE-2026-09-28.md`.
- Current backend head `36c9c78` centralizes the duplicated owner-approval query/validation used by maintenance and work-order financial workflows. This is behavior-preserving: source-specific scope and the existing error messages remain unchanged. Focused Property Manager workflow/P0 tests passed **19/19**; the complete non-container solution passed **225/225** with **2 explicit integration skips**. Docker remains unavailable for the post-refactor MinIO/PostgreSQL rerun, and no fresh Sonar upload is claimed.
- Current backend head `ece2fb0` extracts approval-decision normalization, actor/portfolio authorization, and expiry persistence from the PM finance decision endpoint. The status/reason/idempotency rules, owner/admin boundary, expiry event, concurrency checks, and response behavior are preserved. Focused PM P0/authorization/professional tests passed **24/24**; the complete non-container solution passed **225/225** with **2 explicit integration skips**. Docker remains unavailable for the post-refactor MinIO/PostgreSQL rerun, and no fresh Sonar upload is claimed.
- Current backend head `966aead` splits manager resolution into owner-manager selection, active staff lookup, staff-scope validation, and direct-manager authorization helpers. Gate Guard denial, finance/payout capability checks, owner/property scope checks, owner portfolio selection, and direct manager/admin behavior are preserved. Focused Property Manager tests passed **45/45** with no failures and one explicit PostgreSQL multi-instance skip; the complete non-container solution passed **225/225** with **2 explicit integration skips**. Docker remains unavailable for the post-refactor MinIO/PostgreSQL rerun, and no fresh Sonar upload is claimed.
- Current backend head `ef562f0` splits production integration validation into identity, required-secret, legacy-admin, Stripe, Brevo, storage, PostgreSQL, and bootstrap helpers. Existing production safety behavior and messages are preserved; the dedicated validator tests passed **6/6**, and the complete non-container solution passed **225/225** with **2 explicit integration skips**. Docker remains unavailable for the post-refactor MinIO/PostgreSQL rerun, and no fresh Sonar upload is claimed.

## AUTO-0035 — Expand frontend behavioral coverage for landing search and Property Manager modules

Priority: P1 quality evidence
Date: 2026-09-28

The frontend certification branch added behavioral tests for the landing SearchBar and the Property Manager module dispatcher/content. The tests exercise destination filtering and empty states, date/guest controls, keyboard and dismissal behavior, submit URL construction, and Property Manager invoice, payment, utility, maintenance, calendar, report, subscription, gate/QR, insurance, vendor, document, governance, and community actions.

Evidence:

- Frontend commit: `f1f935b`, pushed to `codex/final-release-certification`.
- Full Vitest suite: **153 passed across 36 files, 0 failed**.
- V8 coverage: **62.84% lines**, **59.21% statements**, **50.80% branches**, **55.93% functions**.
- Typecheck: PASS.
- Production build: PASS.
- Lint: **0 errors / 107 existing warnings**.
- No production code, provider configuration, migration, staging or production environment was changed.

This closes the local frontend line-coverage threshold of 60% but does not claim the separate Sonar 80% new-code gate, current Sonar refresh, staging role/IDOR verification, real Brevo delivery, MinIO production durability, or deployed SHA parity.

## AUTO-0036 — Cover the booking identity-verification handoff

Priority: P0 identity-flow evidence
Date: 2026-09-28

The frontend certification branch adds behavioral coverage for the booking identity page: Stripe Identity provider messaging, document selection, the hosted verification-session handoff, pending navigation, and the provider-error loading path.

Evidence:

- Frontend commit: `9ccfb75`, pushed to `codex/final-release-certification`.
- Full Vitest suite: **155 passed across 37 files, 0 failed**.
- V8 coverage: **63.10% lines**, **59.44% statements**, **51.04% branches**, **56.14% functions**.
- Typecheck: PASS; production build: PASS; lint: **0 errors / 107 existing warnings**.
- No application production code, provider configuration, migration, staging, production, secret, or protected `main` was changed.

This improves local evidence for M1 identity UX but does not prove an external Stripe Identity session, staging deployment parity, or any other external release gate.

## AUTO-0037 — Cover booking invoice and receipt workflows

Priority: P1 booking evidence
Date: 2026-09-28

Added behavioral frontend coverage for the booking invoice and receipt screens. The tests verify invoice rendering, print behavior, authenticated download requests, receipt download success/failure handling, and the user-facing error path without creating a download link when the receipt request fails.

Evidence:

- Frontend commit: `e74c701`, pushed to `codex/final-release-certification`.
- Full Vitest suite: **157 passed across 37 files, 0 failed**.
- V8 coverage: **63.41% lines**, **59.68% statements**, **51.20% branches**, **56.27% functions**.
- Typecheck: PASS; production build: PASS; lint: **0 errors / 107 existing warnings**.
- No application production code, provider configuration, migrations, staging, production, secrets, or protected `main` were changed.

This improves local M1 booking evidence only. It does not claim a real payment/receipt provider transaction, staging role/IDOR proof, provider delivery, MinIO durability, Sonar refresh, or deployed-SHA parity.

## AUTO-0038 — Re-verify the frontend certification head after test typing fix

Priority: P1 release evidence integrity
Date: 2026-09-28

Corrected the invoice/receipt test mocks to use an explicit intentional double-cast to the existing `AuthController` contract. This is test-only and does not change application behavior.

Evidence:

- Frontend commit: `20f199f`, pushed to `codex/final-release-certification`.
- Full Vitest suite at the corrected head: **157 passed across 37 files, 0 failed**.
- Focused invoice/receipt suite: **14 passed, 0 failed**.
- Typecheck: PASS.
- Production build: PASS.
- Lint: **0 errors / 107 warnings**.

The V8 coverage measurement remains the preceding `e74c701` run at **63.41% lines, 59.68% statements, 51.20% branches, and 56.27% functions** because the follow-up only changed TypeScript test casting. No new Sonar analysis is claimed.

## AUTO-0039 — Bound legacy Property Manager QR validation input

Priority: P1 security hardening  \
Date: 2026-09-28

The legacy Property Manager QR validation path now rejects blank or oversized tokens before hashing or database lookup. The existing 256-character bound used by the primary QR validation path is now enforced consistently by `EfPropertyManagerStore.ValidateQrAsync`, with a safe invalid-result response for malformed input. No migration, provider configuration, staging, production, or protected `main` was changed.

Evidence:

- Backend commit: `732c71058a37c3d7a7691e898182da1f33382c5c`, pushed to `codex/final-release-certification`.
- Focused oversized-token regression: **1 passed, 0 failed**.
- Full `PropertyManagerEndpointTests`: **11 passed, 0 failed**.
- Complete current-head non-container backend solution: **226 passed, 0 failed, 2 explicit environment skips** (MinIO and two-instance PostgreSQL).
- Current-head disposable MinIO/PostgreSQL and Sonar reruns are complete. Production/staging storage durability, backups, Brevo delivery, role/IDOR evidence, and deployed SHA parity remain unverified.

Detailed evidence: `testing-evidence/final-hardening/04-authorization/LEGACY-MANAGER-QR-INPUT-2026-09-28.md`.

## AUTO-0040 — Bound signed storage download inputs

Priority: P1 security hardening  \
Date: 2026-09-28

The anonymous signed-storage download endpoint now rejects missing, oversized, or invalid UTF-8 object-key payloads and oversized access-token input before provider validation or object access. Encoded keys are bounded at 2,048 characters, decoded key bytes at 1,024, and access tokens at 512 characters. Existing signed URL behavior and provider authorization are unchanged.

Evidence:

- Backend commit: `ff31270f134c599fadb7dd7058f361fb45cc55ff`, pushed to `codex/final-release-certification`.
- Focused `StorageAndPlatformControllerTests`: **10 passed, 0 failed**.
- Complete current-head non-container backend solution: **227 passed, 0 failed, 2 explicit environment skips** (MinIO and two-instance PostgreSQL).
- Current-head Docker-backed MinIO/PostgreSQL rerun and Sonar refresh remain blocked by local Docker recovery; no current-head container or Sonar result is inferred.

No migration, provider configuration, staging, production, secret, or protected `main` was changed.

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
- The anonymous signed-storage download endpoint now bounds encoded keys, decoded key bytes, access-token length, and UTF-8 decoding before provider validation; malformed input cannot reach object access.

## Partial

- M1: external Stripe/Identity/Brevo/storage/payout/insurance and staging SHA parity are not proven.
- M2: Gold/Platinum commercial membership model remains outdated; real Stripe lifecycle is not proven.
- M3: external provider, storage, payout and operational certification remain.
- M4: Gate Guard lifecycle/authentication is locally implemented; staging/browser certification, physical gate hardware, and geocoding remain unverified or out of current scope.
- M5: production storage/email/billing and complete manual responsive certification remain.
- Backend Sonar: the current-head scan at `ff31270f134c599fadb7dd7058f361fb45cc55ff` reports 73.2% line coverage, 798 code smells, 6.9% duplication, 0 bugs, 0 vulnerabilities, and 0 hotspots. The requested 80% coverage and maintainability target is not complete.
- Frontend Sonar: current-head scan at `20f199f80bea89e58b3632a4e828e04c488c9652` reports 58.8% executable-line coverage and 972 maintainability issues. The local V8 run is 63.41% lines, but Sonar's executable-line calculation remains below the requested 60% threshold and the 80% new-code requirement is not claimed as passed.
- Platform Sonar: gate PASS for new-code scope; overall scanned orchestration findings remain separate from runtime certification.
- Full staging role/IDOR certification remains.
- Brevo real transport/mailbox delivery remains blocked by external configuration.
- Signed storage download input hardening is locally verified at backend `ff31270`; real MinIO I/O and deployed storage durability remain external/runtime blockers.

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
- The anonymous signed-storage download route accepted unbounded base64 key and token input before provider validation; this is fixed and covered by backend `ff31270`.

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

Keep the exact Gold/Platinum values blocked on human business input; the current frontend browser matrix is already complete for the runtime-equivalent `20f199f` head, and the test-only `14455cc` commit is pushed. Update staging-dependent blockers only after Terrence deploys/verifies the corresponding SHAs and provider configuration. Do not treat the disposable Sonar `OK` status as a release gate.

## AUTO-0041 — Re-run current-head disposable integration services and Sonar certification

Priority: P1 release evidence  \
Date: 2026-09-28

Docker recovered sufficiently to start isolated loopback-only PostgreSQL and MinIO containers. The current backend head was tested with both runtime services configured, removing the two previous environment skips from the full suite. A fresh local SonarQube instance processed current backend, frontend, and root runtime/orchestration scans.

Evidence:

- Backend `ff31270`: **229 passed, 0 failed, 0 skipped**; MinIO I/O **1/1**; PostgreSQL two-instance concurrency **1/1**.
- Frontend `20f199f`: **157 passed, 0 failed**; V8 line coverage **63.41%**.
- Current local Sonar metrics: backend **73.2%** coverage / 798 smells / 0 bugs-vulnerabilities-hotspots; frontend **58.8%** Sonar coverage / 972 smells / 0 bugs-vulnerabilities-hotspots; root runtime scope **0** issues.
- Full evidence is under `testing-evidence/sonarqube/CURRENT-LOCAL-SCAN-2026-09-28.md` and the three JSON exports.

This closes the local Docker execution blocker only. It does not verify production/staging MinIO backups, Brevo delivery, real Stripe flows, staging authorization, human accessibility, exact Gold/Platinum values, or deployed SHA parity.

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
