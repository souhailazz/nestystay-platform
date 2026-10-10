# NestyStay Group A acceptance tracker

Last updated: 2026-10-10 (Africa/Casablanca)

This tracker separates local evidence, pushed review work, deployed evidence, and client-controlled staging acceptance. No row is marked `STAGING_VERIFIED` without an authorized staging workflow.

Revalidation on 2026-10-10 confirmed the listed frontend/backend PR heads remain open and `MERGEABLE` against `main` with successful required CI checks; protected-branch review is still the reason their merge state is `BLOCKED`. Public staging checks remain live `200`, ready `200` with database ready and storage configured, backend version `e86be7daa0b4c38cf3525ec8678a2f796b476692`, and frontend version SHA `local` (deployed frontend SHA still unknown).

| Item | Requirement | Status | Local / PR evidence | Staging status | Remaining owner/action |
|---:|---|---|---|---|---|
| 1 | Branded password-reset email with usable link | PR_OPEN | Backend PR #16 (`aa73f4b`), branded template and regression test | BLOCKED_EXTERNAL | Terrence merge/deploy, then exercise delivery |
| 2 | Reset link carries required identifiers; no manual request ID | PR_OPEN | Frontend PR #14 (`50a90c1`) and reset-flow tests | BLOCKED_EXTERNAL | Terrence merge/deploy, then complete authorized flow |
| 3 | Visible reset confirmation and Login action | PR_OPEN | Frontend PR #14 (`50a90c1`) | BLOCKED_EXTERNAL | Terrence merge/deploy, then desktop/mobile check |
| 4 | Listing-selected dates and guests reach booking | PR_OPEN | Frontend PR #15 (`ec06409`), booking modal tests | BLOCKED_EXTERNAL | Terrence merge/deploy, then authorized booking check |
| 5 | Compact centered responsive booking modal | PR_OPEN | Frontend PR #16 (`d3b4579`), modal tests | BLOCKED_EXTERNAL | Terrence merge/deploy, then desktop/tablet/mobile check |
| 6 | Pending listings never show instant approval | PR_OPEN | Frontend PR #15; backend moderation state is authoritative | BLOCKED_EXTERNAL | Terrence merge/deploy, then moderation fixture check |
| 7 | Pending listings cannot quote or book | PR_OPEN | Frontend PR #15; backend booking hardening on current main | BLOCKED_EXTERNAL | Terrence merge/deploy, then authorized negative test |
| 8 | Clear pending-listing business error | PR_OPEN | Frontend PR #15 message and regression coverage | BLOCKED_EXTERNAL | Terrence merge/deploy, then verify API/UI response |
| 9 | Authorized Admin review details instead of public listing page | PR_OPEN | Frontend PR #17 (`949ca56`), review modal test | BLOCKED_EXTERNAL | Terrence merge/deploy, then Admin acceptance |
| 10 | Approval/rejection confirmation and duplicate-submit protection | PR_OPEN | Frontend PR #17; focused Admin tests | BLOCKED_EXTERNAL | Terrence merge/deploy, then Admin acceptance |
| 11 | Searchable Admin entity selectors | PR_OPEN | Frontend PR #12 (`9aa28f`), CI green | BLOCKED_EXTERNAL | Terrence merge/deploy, then Admin form acceptance |
| 12 | Campaign timestamps displayed in Jamaica time | PR_OPEN | Frontend PR #13 (`80cd86a`), CI green | BLOCKED_EXTERNAL | Terrence merge/deploy, then Campaigns acceptance |
| 13 | Worker status reflects default in-process jobs | PR_OPEN | Backend PR #17 (`ec6e327`), 14 focused health tests passed | BLOCKED_EXTERNAL | Terrence merge/deploy; verify runtime heartbeat/config |
| 14 | Separate Super Admin host/login | PR_OPEN | Frontend PR #23 (`78310ae`), 209 tests/typecheck/build/lint pass; hosted CI passed | BLOCKED_EXTERNAL | Terrence configure DNS/TLS/server routing and verify |
| 15 | Welcoming role-specific sign-in presentation | EXISTING_IMPLEMENTATION | Current main role cards and image assets; auth tests pass | BLOCKED_EXTERNAL | Authorized staging visual check |
| 16 | Remove developer/debug text from user screens | PR_OPEN | Frontend PR #18 (`b1a030f`), full frontend checks pass | BLOCKED_EXTERNAL | Terrence merge/deploy, then visual check |
| 17 | Center booking/sign-out and affected modals | PR_OPEN | Frontend PR #16 (`d3b4579`); modal accessibility tests | BLOCKED_EXTERNAL | Terrence merge/deploy, then browser check |
| 18 | Role-specific Patois greeting framework | NEEDS_CLARIFICATION | Existing `PatoisProvider`/`PatoisPhrase`; final approved wording not supplied | BLOCKED_EXTERNAL | Terrence approve exact wording and role mapping |
| 19 | Approved wellness terminology | PR_OPEN | Frontend PR #19 (`cc5a320`), 209 tests/typecheck/build pass | BLOCKED_EXTERNAL | Terrence merge/deploy, then terminology sweep |
| 20 | Gate Guard invitation, assignment, QR and scope | EXISTING_IMPLEMENTATION | Current main Gate Guard routes/tests and local backend workflow coverage | BLOCKED_EXTERNAL | Terrence provision authorized staging PM/Guard accounts |
| 21 | Separate guest and operations provider audiences | PR_OPEN | Backend PR #18 (`d01132b`) + Frontend PR #21 (`434aeef`), CI green | BLOCKED_EXTERNAL | Terrence merge/deploy, then role-filtered directory checks |
| 22 | Verified badge on Explore listings | EXISTING_IMPLEMENTATION | Explore/map consume server `badgeLevel`; public properties require Approved status | BLOCKED_EXTERNAL | Authorized staging status-change check |
| 23 | Calendar, Safety Center, incidents and Stripe release demonstrations | BLOCKED_EXTERNAL | Local calendar/Safety/incident authorization tests pass; live Stripe/provider not exercised | BLOCKED_EXTERNAL | Terrence provide QA roles and test-mode/provider access |
| 24 | PM tools accessible from workspace top navigation | PR_OPEN | Frontend PR #20 (`f7562fc`), focused PM tests pass | BLOCKED_EXTERNAL | Terrence merge/deploy, then navigation walkthrough |
| 25 | PM layout and sample-value cleanup | PR_OPEN | Frontend PR #22 (`4daaa7c`), CI green | BLOCKED_EXTERNAL | Terrence merge/deploy, then desktop/tablet/mobile check |
| 26 | Real PM record-based workflow demonstrations | EXISTING_IMPLEMENTATION | Local PM API/UI tests cover owner, finance, utility, maintenance, vendor, notice, document, gate and QR actions | BLOCKED_EXTERNAL | Authorized disposable staging records and cleanup |
| 27 | Common-area utility bills and executive-term retrieval | NEEDS_CLARIFICATION | Utility/document persistence exists; executive-term rules are not defined in available specification | BLOCKED_EXTERNAL | Terrence define executive-term dates before any schema proposal |
| 28 | Meetings, minutes, agendas and governing documents with voting | NEEDS_CLARIFICATION | Proposals/quorum/anonymous ballots exist; meeting/minutes workflow mapping is incomplete | BLOCKED_EXTERNAL | Terrence confirm required meeting/minutes rules |

## Complete A1–A28 PR, implementation and staging matrix

This is the authoritative item-by-item accounting requested for handoff. `OPEN` means the implementation PR is reviewable but not merged. `IN MAIN` means the cited implementation is already present on the current repository `main` and therefore has no new implementation PR. No staging row is marked PASS without an authorized, authenticated workflow on staging.

| Item | Actual PR link(s) | Latest commit | Merged status | Implementation evidence / incomplete workflow | Staging acceptance |
|---:|---|---|---|---|---|
| A1 | [Backend #16](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/pull/16) | `aa73f4b6112c5f8fd1b3868e2d10a727cfcda4a0` | OPEN — not merged | Branded password-reset template/link implementation and regression coverage. | BLOCKED — PR not deployed and no authorized reset account/mailbox. |
| A2 | [Frontend #14](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/14) | `50a90c1919553ec135bae7dbf5e86581aae64ff2` | OPEN — not merged | Reset UI consumes the secure link identifiers and removes the unavailable manual request-ID step. | BLOCKED — PR not deployed and no authorized reset account/mailbox. |
| A3 | [Frontend #14](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/14) | `50a90c1919553ec135bae7dbf5e86581aae64ff2` | OPEN — not merged | Reset confirmation and Login action are implemented in `src/pages/CompletionPages.tsx` with E2E coverage. | BLOCKED — PR not deployed and no authorized reset account. |
| A4 | [Frontend #15](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/15) | `ec0640980190cdd0c45f525c38aaf81064667e0b` | OPEN — not merged | Booking modal preserves listing-selected dates/guests and sends them to quote/booking tests. | BLOCKED — PR not deployed; authorized booking fixture unavailable. |
| A5 | [Frontend #16](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/16) | `d3b4579127ae55983e656490b0ad3b5e1abf7d62` | OPEN — not merged | Responsive centered booking/modal layout in `BookingModal.tsx`, `Modal.tsx` and `src/index.css`, with modal tests. | BLOCKED — PR not deployed; staging visual acceptance unavailable. |
| A6 | [Frontend #15](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/15) | `ec0640980190cdd0c45f525c38aaf81064667e0b` | OPEN — not merged | Pending listing state no longer presents instant approval in `PropertyDetailPage.tsx`/`PublicSearchMap.tsx`. | BLOCKED — requires deployed PR and authorized pending-listing fixture. |
| A7 | [Frontend #15](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/15) | `ec0640980190cdd0c45f525c38aaf81064667e0b` | OPEN — not merged | Pending listing no longer exposes misleading price/Book action; backend moderation state remains authoritative. | BLOCKED — requires deployed PR and authorized pending-listing fixture. |
| A8 | [Frontend #15](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/15) | `ec0640980190cdd0c45f525c38aaf81064667e0b` | OPEN — not merged | Clear pending-listing business error and regression coverage are in the booking/public surfaces. | BLOCKED — requires deployed PR and authorized negative booking test. |
| A9 | [Frontend #17](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/17) | `949ca56f3638c5bca6c96d980bec662e36b49762` | OPEN — not merged | Admin review details remain in the moderation workspace instead of linking to the public guest page. | BLOCKED — Admin QA account and deployed PR required. |
| A10 | [Frontend #17](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/17) | `949ca56f3638c5bca6c96d980bec662e36b49762` | OPEN — not merged | Approve/reject confirmation and duplicate-submit protection are covered by focused Admin tests. | BLOCKED — Admin QA account and deployed PR required. |
| A11 | [Frontend #12](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/12) | `9aa28fcc53a62ecf9c3a27f033b716755142e557` | OPEN — not merged | Searchable authorized property/host/entity selectors replace raw IDs in Admin forms; CI green. | BLOCKED — PR not deployed; authorized Admin workflow required. |
| A12 | [Frontend #13](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/13) | `80cd86a7881bcc4400062bd320e4cb56d0807f6f` | OPEN — not merged | `src/lib/campaignTime.ts` preserves UTC API values and displays/submits `America/Jamaica` local time; tests and CI green. | BLOCKED — PR not deployed; Campaigns QA account/workflow required. |
| A13 | [Backend #17](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/pull/17) | `ec6e32720d14d19c3477ae5376f9039e4de30d3a` | OPEN — not merged | Health/integration status reports the default hosted worker configuration; focused health tests pass. | BLOCKED — PR not deployed; runtime worker heartbeat/config must be verified. |
| A14 | [Frontend #23](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/23) | `78310ae189895db59b1aae5266f9be46b7ea9637` | OPEN — not merged | Dedicated Admin host routing is defined in `src/app/routeManifest.ts` and tested. | BLOCKED — Terrence must configure `admin.nestystay.net` DNS/TLS/reverse-proxy routing, then perform Admin login. |
| A15 | — | `e09dc5c878439c5b2a32dac8f626f2668777c144` (frontend `main`) | IN MAIN — no PR required | Current-main role-specific sign-in cards/presentation and auth coverage exist; this is not a missing implementation PR. | BLOCKED — authorized staging visual/auth check required. |
| A16 | [Frontend #18](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/18) | `b1a030fd19cab0240bbae9fd8fb73abe980b5a28` | OPEN — not merged | Internal/debug copy removal in `CompletionPages.tsx` and `PropertyManagerPages.tsx`; CI green. | BLOCKED — PR not deployed; visual staging check required. |
| A17 | [Frontend #16](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/16) | `d3b4579127ae55983e656490b0ad3b5e1abf7d62` | OPEN — not merged | Centered booking/sign-out/modal presentation with accessibility tests. | BLOCKED — PR not deployed; desktop/tablet/mobile staging check required. |
| A18 | — | `e09dc5c878439c5b2a32dac8f626f2668777c144` (frontend `main`) | NO PR — specification incomplete | Reusable `frontend/src/lib/patois.tsx:41-69` exists, but approved role wording and role mapping were not supplied; no safe implementation can be claimed. | BLOCKED — Terrence must approve exact wording and role mapping. |
| A19 | [Frontend #19](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/19) | `cc5a32015d1200e70aeeb809e25141977bfa2df6` | OPEN — not merged | Wellness Officer terminology is updated across auth/layout/product/safety surfaces and tests. | BLOCKED — PR not deployed; authorized terminology sweep required. |
| A20 | — | `e09dc5c878439c5b2a32dac8f626f2668777c144` + `e86be7daa0b4c38cf3525ec8678a2f796b476692` (`main`) | IN MAIN — no implementation PR | Gate Guard routes/dashboard, invitation/assignment/QR authorization and local persistence/security tests exist (`GateGuardDashboardPage.tsx`, `PropertyManagerController.cs`, `m4-qr-gate.spec.ts`). Missing evidence is the real staging PM invitation, activation, property assignment, QR validation and cross-property denial workflow. | BLOCKED — Terrence must provision authorized PM and Gate Guard accounts/fixture and deploy current main. |
| A21 | [Backend #18](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/pull/18) + [Frontend #21](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/21) | `d01132be7a519ae7f0caa23b6da0a857499e4e86` + `434aeef808c0d5ae2da2e3b5baa8a7f15e4e0aba` | OPEN — not merged | Backend audience enforcement and frontend directory filtering are implemented with CI-green tests. | BLOCKED — both PRs must deploy; role-filtered staging accounts required. |
| A22 | — | `e09dc5c878439c5b2a32dac8f626f2668777c144` (frontend `main`) | IN MAIN — no PR required | Explore/map consume server `badgeLevel` and public listings require Approved moderation state. | BLOCKED — authorized moderation/status-change staging check required. |
| A23 | — | `e09dc5c878439c5b2a32dac8f626f2668777c144` + `e86be7daa0b4c38cf3525ec8678a2f796b476692` (`main`) | PARTIAL IN MAIN — no single PR | Calendar, Safety Center and incident routes/local authorization tests exist; real staging calendar/provider behavior, Stripe release/provider verification and authenticated demonstrations remain unperformed. | BLOCKED_EXTERNAL — authorized role accounts, staging fixtures and test-mode provider access required. |
| A24 | [Frontend #20](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/20) | `f7562fc731ce443a2af189b71b4341ac926b2738` | OPEN — not merged | PM tools are surfaced in workspace top navigation with focused tests. | BLOCKED — PR not deployed; PM staging walkthrough required. |
| A25 | [Frontend #22](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/22) | `4daaa7c30f9756c7867732cfc68f8905da0cf9bd` | OPEN — not merged | PM responsive layout and sample-value cleanup are covered by CI-green tests. | BLOCKED — PR not deployed; desktop/tablet/mobile PM walkthrough required. |
| A26 | — | `e09dc5c878439c5b2a32dac8f626f2668777c144` + `e86be7daa0b4c38cf3525ec8678a2f796b476692` (`main`) | IN MAIN — no implementation PR required | Local persisted PM API/UI workflows cover owner, finance, utility, maintenance, vendor, notice, document, gate and QR actions; no new code PR is currently indicated. | BLOCKED — Terrence must provide authorized disposable staging records and cleanup approval for the PM walkthrough. |
| A27 | — | `e86be7daa0b4c38cf3525ec8678a2f796b476692` (backend `main`) | NO PR — incomplete scope | `UtilityBill` persistence exists, but no common-area bill period/executive-term relation or scoped API workflow exists; executive-term rules are undefined. | BLOCKED — Terrence must define executive-term dates, permissions and retrieval rules before schema/API work. |
| A28 | — | `e86be7daa0b4c38cf3525ec8678a2f796b476692` (backend `main`) | NO PR — incomplete scope | Association entities persist, but no API exposes the requested meeting/minutes/agenda/governing-document workflow; current proposal/vote/proxy API is not equivalent. | BLOCKED — Terrence must approve meeting lifecycle, permissions, quorum, minutes and document rules before implementation. |

## Current local verification

## Reviewable PR index

All entries below target `main`, report `MERGEABLE`, have a successful required build/test check, and have deployment jobs skipped. `BLOCKED` is the protected-branch review state, not a merge conflict.

| Repository / PR | Head SHA | Scope and changed files |
|---|---|---|
| Frontend [#12](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/12) | `9aa28fcc53a62ecf9c3a27f033b716755142e557` | Admin selectors: `src/pages/ProductPages.tsx`, `src/coverage/admin-surfaces.test.tsx`, `src/coverage/product-surfaces.test.tsx` |
| Frontend [#13](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/13) | `80cd86a7881bcc4400062bd320e4cb56d0807f6f` | Jamaica time: `src/lib/campaignTime.ts`, test, `src/pages/ProductPages.tsx` |
| Frontend [#14](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/14) | `50a90c1919553ec135bae7dbf5e86581aae64ff2` | Password reset UI: `src/pages/CompletionPages.tsx`, `e2e/final-hardening-email.spec.ts` |
| Frontend [#15](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/15) | `ec0640980190cdd0c45f525c38aaf81064667e0b` | Booking state/pending listings: `src/components/booking/BookingModal.tsx`, `src/features/public/PropertyDetailPage.tsx`, `src/features/public/PublicSearchMap.tsx`, tests |
| Frontend [#16](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/16) | `d3b4579127ae55983e656490b0ad3b5e1abf7d62` | Booking/modal layout: `BookingModal.tsx`, `Modal.tsx`, `src/index.css`, tests |
| Frontend [#17](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/17) | `949ca56f3638c5bca6c96d980bec662e36b49762` | Admin moderation review: `src/features/admin/AdminProperties.tsx` and test |
| Frontend [#18](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/18) | `b1a030fd19cab0240bbae9fd8fb73abe980b5a28` | Remove debug copy: `CompletionPages.tsx`, `PropertyManagerPages.tsx` |
| Frontend [#19](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/19) | `cc5a32015d1200e70aeeb809e25141977bfa2df6` | Wellness terminology across auth/layout/product/safety files and tests |
| Frontend [#20](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/20) | `f7562fc731ce443a2af189b71b4341ac926b2738` | PM workspace top navigation and test |
| Frontend [#21](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/21) | `434aeef808c0d5ae2da2e3b5baa8a7f15e4e0aba` | Directory audience API client/filtering/UI and tests |
| Frontend [#22](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/22) | `4daaa7c30f9756c7867732cfc68f8905da0cf9bd` | PM responsive layout/sample-value cleanup and tests |
| Frontend [#23](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/23) | `78310ae189895db59b1aae5266f9be46b7ea9637` | Dedicated admin host routing: `src/app/routeManifest.ts` and test |
| Frontend [#24](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/24) | `da3d2f661878525db3cb801fee45971bfc328e87` | Playwright consent state is generated for the configured test origin: `playwright.config.ts` |
| Frontend [#25](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/25) | `5fcf9c78c33940e75639a0e3d74e65e12ba31589` | Calendar E2E synchronization/response diagnostics: `e2e/calendar-operations.spec.ts` |
| Backend [#16](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/pull/16) | `aa73f4b6112c5f8fd1b3868e2d10a727cfcda4a0` | Branded password-reset delivery and API regression test |
| Backend [#17](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/pull/17) | `ec6e32720d14d19c3477ae5376f9039e4de30d3a` | Worker integration status and health tests |
| Backend [#18](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/pull/18) | `d01132be7a519ae7f0caa23b6da0a857499e4e86` | Directory audience enforcement/API/store/tests |

- Frontend PR #22: 208 tests passed, typecheck passed, build passed, lint 0 errors / 32 existing warnings; hosted CI passed.
- Frontend PR #23: 209 tests passed, typecheck passed, build passed, lint 0 errors / 32 existing warnings; hosted CI passed (run `38068235877`, job `114260163666`); deploy skipped.
- Clean frontend current-main local Vitest rerun: 208 tests across 49 files; 208 passed, 0 failed. The previously observed Gate QR assertion passes when rerun in isolation and in the complete suite; no frontend defect was created for it. Frontend PR #12 and PR #13 hosted CI checks are green.
- Frontend PR #24 (`da3d2f661878525db3cb801fee45971bfc328e87`) contains only the Playwright harness fix for origin-scoped cookie consent state. Local typecheck and targeted ESLint passed; hosted `Typecheck, test and build` passed (run `38076289423`, job `114283800762`); deploy job skipped.
- Frontend PR #25 (`5fcf9c78c33940e75639a0e3d74e65e12ba31589`) contains only calendar E2E synchronization and response diagnostics. Local typecheck/lint and desktop, tablet and mobile calendar tests passed; hosted `Typecheck, test and build` passed (run `38077200968`, job `114286553878`); deploy job skipped.
- Frontend current main is `e09dc5c878439c5b2a32dac8f626f2668777c144`. PR #12 (`9aa28fcc53a62ecf9c3a27f033b716755142e557`) and PR #13 (`80cd86a7881bcc4400062bd320e4cb56d0807f6f`) both report `MERGEABLE` against that exact base; GitHub `BLOCKED` reflects protected-branch review requirements, not a merge conflict. Their deployment jobs were skipped.
- Backend current-main clean checkout (`e86be7daa0b4c38cf3525ec8678a2f796b476692`): `dotnet test NestyStay.sln --no-restore --configuration Release` passed with 309 total, 307 passed, 0 failed, and 2 skipped. Release build/test process completed successfully; the skips are environment-gated PostgreSQL integration coverage.
- Local disposable MinIO: real roundtrip, signed download, wrong-credential denial, traversal/oversize checks and restart persistence passed.
- Staging public health: live 200; ready 200 with database ready and storage configured.
- Staging backend version: `e86be7daa0b4c38cf3525ec8678a2f796b476692`.
- Staging frontend `/version.json`: reports `commitSha: local`; deployed frontend SHA is therefore unknown.

## Specification-dependent implementation evidence

- Item 18: `frontend/src/lib/patois.tsx:41-69` provides a reusable `PatoisPhrase` and a global opt-in toggle, but it has no role-to-greeting mapping. Because Terrence has not supplied the final approved wording for the eight requested roles, adding phrases or assigning existing phrases to roles would invent product copy; this remains a copy/specification blocker rather than an authorization defect.
- Item 27: `src/NestyStay.Domain/PropertyManagement/PropertyManagementEntities.cs:39-47` has a legacy `UtilityBill` record with community, storage-object, utility type, amount, currency and allocation fields, and `NestyStayDbContext.cs:128` maps it. The exposed PM API instead uses property/owner-scoped charges and readings (`PropertyManagerController.cs:98-123`, `PropertyManagerModels.cs:111-115`); it has no common-area bill create/list/download route, no bill-period field on `UtilityBill`, and no executive-term relation or date rules. This is an implementation gap, but the executive-term definition and authorization model must be specified before adding schema/API behavior.
- Item 28: association persistence exists (`AssociationManagementEntities.cs:5-51`, `NestyStayDbContext.cs:134-139`) for meetings, meeting documents, votes, vote results and proxies, but no API controller exposes those entities. The current PM controller exposes only proposal/discussion/close/vote/proxy endpoints (`PropertyManagerController.cs:183-208`), which are a separate milestone governance model. Meeting creation, agendas, minutes, governing-document availability and their permission/quorum rules therefore remain unconnected and require Terrence's workflow confirmation before implementation.

## Clean local Playwright certification runs

Run date: 2026-10-10 (Africa/Casablanca). The runs used the clean frontend at current `origin/main` (`e09dc5c878439c5b2a32dac8f626f2668777c144`), the clean backend at current `origin/main` (`e86be7daa0b4c38cf3525ec8678a2f796b476692`), a fresh disposable PostgreSQL database, and a disposable local API/frontend pair. No staging or production systems were changed.

The corrected harness was exercised on the isolated frontend branch for PR #24. Targeted consent/email coverage passed: 11 passed and 5 skipped; the email workflow passed in all three Chromium viewports. The combined PR #24 + PR #25 test worktree then ran all discovered tests:

- Discovered: 233
- Started: 233
- Passed: 213
- Failed: 0
- Skipped: 20
- Did not run: 0

The prior full-matrix calendar assertion was eliminated by the test-only synchronization in PR #25. The combined run passed the calendar workflow on desktop, tablet and mobile Chromium and produced no failures or not-run tests. No application calendar code was changed.

Focused local authorization/Gate Guard/PM/Safety evidence rerun: 7 Playwright tests passed (`final-hardening-security`, `m4-qr-gate`, `m5-professional-completion`, and `m5-property-manager`) on desktop Chromium against the disposable API/database.

The earlier baseline run is retained in git history; it is superseded for current evidence because it used static consent state tied to a different local origin and lacked the disposable worker/development-auth setup.

## Acceptance rule

Authenticated staging workflows remain `BLOCKED_EXTERNAL` until Terrence provides legitimate QA credentials/accounts and deploys the relevant PR heads. Local tests do not substitute for staging acceptance.

## Staging acceptance scripts

The following are the exact staging pages and checks still awaiting authorized execution. A route returning HTML or HTTP 200 is not acceptance evidence for an authenticated workflow.

| Items | Staging page | Required role | Acceptance actions and expected result | Current actual / owner |
|---:|---|---|---|---|
| 1–3 | `https://staging.nestystay.net/auth/forgot-password`, then the emailed reset link | Test mailbox/user | Request reset; open branded link; set password; sign in; repeat expired/used-token checks. | `BLOCKED_EXTERNAL` — Terrence provide controlled mailbox and deploy PR #14/#16. |
| 4–5 | `https://staging.nestystay.net/properties/<approved-id>` | Guest | Select dates/guests; open booking; confirm values persist; verify compact centered modal on desktop/tablet/mobile. | `BLOCKED_EXTERNAL` — Terrence provide approved test listing and guest account; PR #15/#16. |
| 6–8 | `https://staging.nestystay.net/properties/<pending-id>` and booking quote/review | Guest + Admin/Host fixture | Open pending listing; confirm no book/quote/instant-approval action; API returns business rejection; approved listing remains bookable. | `BLOCKED_EXTERNAL` — Terrence provide pending/approved fixtures; PR #15. |
| 9–10 | `https://staging.nestystay.net/admin/ops/properties` | Super Admin | Open Review Details; inspect authorized listing/host/history; approve and reject; confirm status, queue counts, toast and duplicate-submit protection. | `BLOCKED_EXTERNAL` — Terrence provide Admin account and deploy PR #17. |
| 11 | `https://staging.nestystay.net/admin` or dedicated Admin host | Super Admin | Search/select a host and property by name; submit; verify only authorized entities appear and no raw sample IDs are required. | `BLOCKED_EXTERNAL` — Terrence verify Admin host and deploy PR #12. |
| 12 | `https://staging.nestystay.net/admin` or dedicated Admin host | Super Admin | Create/edit a campaign at a known Jamaica-local time; reload; confirm same local value and correct UTC API persistence. | `BLOCKED_EXTERNAL` — Terrence provide Admin access and deploy PR #13. |
| 13 | `https://staging.nestystay.net/admin` | Super Admin | Inspect Integration Status after worker startup; distinguish configured/running/healthy from disabled or unknown; compare with worker heartbeat. | `BLOCKED_EXTERNAL` — Terrence deploy PR #17 and provide service status/log evidence. |
| 14 | `https://admin.nestystay.net/admin/sign-in` | Super Admin | Sign in; refresh/direct-navigate; sign out; confirm public user cannot use Admin routes. | `BLOCKED_EXTERNAL` — Terrence configure DNS/TLS/server host routing and deploy PR #23. |
| 15 | `https://staging.nestystay.net/login` and role aliases | Any authorized role | Check desktop/mobile first viewport, role-specific visual context, visible form and no unnecessary scroll. | `BLOCKED_EXTERNAL` — Terrence provide role accounts. |
| 16 | Role-specific workspace pages | Any affected role | Inspect screens for internal variable names, phase labels, debug copy and unsupported provider buttons. | `BLOCKED_EXTERNAL` — Terrence deploy PR #18 and provide role accounts. |
| 17 | Booking, sign-out and affected modal routes | Guest/Host/PM | Open/close, press Escape, tab through controls, resize viewport; confirm centered overlay, focus trap and visible actions. | `BLOCKED_EXTERNAL` — Terrence deploy PR #16 and provide accounts. |
| 18 | `/login`, `/logout`/confirmation surfaces and role workspaces | All supported roles | Review approved Patois wording, English translation, logo and role mapping after Terrence supplies final copy. | `NEEDS_CLARIFICATION` — Terrence approve wording; no copy invented. |
| 19 | `https://staging.nestystay.net/login`, role dashboards and directories | Wellness Officer/Host/Admin | Confirm public wording is “Property wellness check services”; verify persistent role identifiers and authorization remain unchanged. | `BLOCKED_EXTERNAL` — Terrence deploy PR #19 and provide accounts. |
| 20 | `https://staging.nestystay.net/pm/team`, `/gate/guard`, `/gate/qr` | PM + Gate Guard | Invite; accept; log in; confirm assigned property; validate valid/invalid/expired/revoked QR; deny cross-property access; inspect audit record. | `BLOCKED_EXTERNAL` — Terrence provision PM/Guard accounts and mail transport; existing implementation. |
| 21 | `/directory/trades`, `/directory/custodians`, `/directory/police`, `/directory/businesses` | Guest, Host, PM, Provider, Local Business | Confirm guest-facing taxi/cook/babysitter/tour categories and owner/PM trades; verify role filters and cross-role denial. | `BLOCKED_EXTERNAL` — Terrence deploy Backend #18 + Frontend #21 and provide roles. |
| 22 | `/explore` and `/explore/map` | Guest + Admin/Host fixture | Compare verified/unverified records; confirm badge refresh after status change and no false Verified badge on desktop/mobile. | `BLOCKED_EXTERNAL` — Terrence provide disposable moderation fixtures. |
| 23 | `/pm/calendar`, `/safety`, `/pm/professional?tab=incidents`, booking rejection flow | PM, Admin, Host, authorized safety role | Create/edit/sync calendar event; create/retrieve incident; execute emergency guidance; reject Stripe test authorization; verify release/idempotency. | `BLOCKED_EXTERNAL` — Terrence provide QA roles and test-mode/provider access. |
| 24 | `/pm/dashboard` | Property Manager | Confirm top navigation reaches Owner invite, Unit assignment, Community, Vendors, Portfolio, QR history and Subscription without dead links. | `BLOCKED_EXTERNAL` — Terrence deploy PR #20 and provide PM account. |
| 25 | `/pm/dashboard`, `/pm/vendors`, `/pm/professional` | Property Manager | Check desktop/tablet/mobile layout, date inputs, Advanced controls, viewport boundaries and absence of misleading sample records. | `BLOCKED_EXTERNAL` — Terrence deploy PR #22 and provide PM account. |
| 26 | `/pm/p0`, `/pm/invoices`, `/pm/payments`, `/pm/utilities`, `/pm/maintenance`, `/pm/vendors`, `/pm/community`, `/pm/documents`, `/pm/gates` | Property Manager + disposable records | Create each authorized record; reload; verify relationship/status/export/download; test denial; clean only synthetic fixtures. | `BLOCKED_EXTERNAL` — Terrence provide disposable staging scope and PM account. |
| 27 | `/pm/utilities` and `/pm/documents` | Property Manager | Create common-area bill with attachment; retrieve by period; retrieve by executive term once term rules are defined; verify storage authorization. | `NEEDS_CLARIFICATION` — Terrence define executive-term rules before schema/API work. |
| 28 | `/pm/governance` | Property Manager / association-authorized role | Create meeting/date/agenda; attach governing document; add voting item; verify quorum, anonymous ballot, minutes and results retrieval. | `NEEDS_CLARIFICATION` — Terrence confirm workflow and permissions; existing proposal/ballot model is not enough evidence. |
