# Current State

## Last Completed Task

AUTO-0006 — Complete local release certification and refresh evidence.

## Validation Result

- Backend full solution with local MinIO enabled: 212 passed, 0 failed, 0 skipped.
- Frontend unit suite: 128 passed across 31 files.
- Frontend typecheck: PASS.
- Frontend production build: PASS.
- Frontend lint: 0 errors, 112 warnings.
- Frontend npm audit at high severity: 0 known vulnerabilities.
- Browser baseline: 224 started, 216 passed, 0 failed, 8 explicit skips, 0 did-not-run.
- Fresh local SonarQube analyses completed for backend, frontend, and root/platform scopes. Backend and frontend Quality Gates failed on coverage/new-code debt; the platform orchestration scope passed its new-code gate.

## Confirmed Complete

- Active standard guest platform fee is 10% in backend business rules, pricebook service, seed, frontend estimate/copy, and regression expectations.
- A forward EF migration updates existing pricebook rows to 10% without editing historical migrations.
- Local PostgreSQL and disposable MinIO-backed backend regression is green.
- Gate Guard invitation, acceptance, property scope, authenticated QR validation, PM-workspace denial, and revocation are locally covered by the API authorization matrix.
- Gate Guard is exposed in the frontend role model, navigation, workspace label, and authenticated QR validator.
- The historical `/api/property-manager/staff` endpoint now rejects `GATE_GUARD` instead of creating a membership that cannot grant the real scoped role; the supported `/api/property-manager/p0/members` lifecycle remains covered.
- The admin integration-health endpoint now reports the selected storage provider's actual readiness status and safe detail instead of unconditionally reporting `CONFIGURED`.

## Partial

- M1: external Stripe/Identity/Brevo/storage/payout/insurance and staging SHA parity are not proven.
- M2: Gold/Platinum commercial membership model remains outdated; real Stripe lifecycle is not proven.
- M3: external provider, storage, payout and operational certification remain.
- M4: Gate Guard lifecycle/authentication is locally implemented; staging/browser certification, physical gate hardware, and geocoding remain unverified or out of current scope.
- M5: production storage/email/billing and complete manual responsive certification remain.
- Backend Sonar: 14.4% overall coverage, 7.5% new-code coverage, 11 new violations; gate FAIL.
- Frontend Sonar: 55.6% overall coverage, 64.9% new-code coverage, 4 new violations; gate FAIL.
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
