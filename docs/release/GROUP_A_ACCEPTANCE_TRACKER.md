# NestyStay Group A acceptance tracker

Last updated: 2026-10-10 (Africa/Casablanca)

This tracker separates local evidence, pushed review work, deployed evidence, and client-controlled staging acceptance. No row is marked `STAGING_VERIFIED` without an authorized staging workflow.

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

## Current local verification

- Frontend PR #22: 208 tests passed, typecheck passed, build passed, lint 0 errors / 32 existing warnings; hosted CI passed.
- Frontend PR #23: 209 tests passed, typecheck passed, build passed, lint 0 errors / 32 existing warnings; hosted CI passed (run `38068235877`, job `114260163666`); deploy skipped.
- Clean frontend current-main local Vitest rerun: 208 tests across 49 files; 208 passed, 0 failed. The previously observed Gate QR assertion passes when rerun in isolation and in the complete suite; no frontend defect was created for it. Frontend PR #12 and PR #13 hosted CI checks are green.
- Frontend current main is `e09dc5c878439c5b2a32dac8f626f2668777c144`. PR #12 (`9aa28fcc53a62ecf9c3a27f033b716755142e557`) and PR #13 (`80cd86a7881bcc4400062bd320e4cb56d0807f6f`) both report `MERGEABLE` against that exact base; GitHub `BLOCKED` reflects protected-branch review requirements, not a merge conflict. Their deployment jobs were skipped.
- Backend current-main clean checkout (`e86be7daa0b4c38cf3525ec8678a2f796b476692`): `dotnet test NestyStay.sln --no-restore --configuration Release` passed with 309 total, 307 passed, 0 failed, and 2 skipped. Release build/test process completed successfully; the skips are environment-gated PostgreSQL integration coverage.
- Local disposable MinIO: real roundtrip, signed download, wrong-credential denial, traversal/oversize checks and restart persistence passed.
- Staging public health: live 200; ready 200 with database ready and storage configured.
- Staging backend version: `e86be7daa0b4c38cf3525ec8678a2f796b476692`.
- Staging frontend `/version.json`: reports `commitSha: local`; deployed frontend SHA is therefore unknown.

## Specification-dependent implementation evidence

- Item 27: `src/NestyStay.Domain/PropertyManagement/PropertyManagementEntities.cs:39-47` has a legacy `UtilityBill` record with community, storage-object, utility type, amount, currency and allocation fields, and `NestyStayDbContext.cs:128` maps it. The exposed PM API instead uses property/owner-scoped charges and readings (`PropertyManagerController.cs:98-123`, `PropertyManagerModels.cs:111-115`); it has no common-area bill create/list/download route, no bill-period field on `UtilityBill`, and no executive-term relation or date rules. This is an implementation gap, but the executive-term definition and authorization model must be specified before adding schema/API behavior.
- Item 28: association persistence exists (`AssociationManagementEntities.cs:5-51`, `NestyStayDbContext.cs:134-139`) for meetings, meeting documents, votes, vote results and proxies, but no API controller exposes those entities. The current PM controller exposes only proposal/discussion/close/vote/proxy endpoints (`PropertyManagerController.cs:183-208`), which are a separate milestone governance model. Meeting creation, agendas, minutes, governing-document availability and their permission/quorum rules therefore remain unconnected and require Terrence's workflow confirmation before implementation.

## Clean local Playwright certification run

Run date: 2026-10-10 (Africa/Casablanca). The run used the clean frontend at current `origin/main` (`e09dc5c878439c5b2a32dac8f626f2668777c144`), the clean backend at current `origin/main` (`e86be7daa0b4c38cf3525ec8678a2f796b476692`), a fresh disposable PostgreSQL database, and a disposable local API/frontend pair. No staging or production systems were changed.

- Discovered: 233
- Started: 230
- Passed: 173
- Failed: 9
- Skipped: 48
- Did not run: 3

Failure evidence:

- `final-hardening-email.spec.ts`: 3 browser-project failures; `GET /api/spec/auth/development/flows/{id}` returned HTTP 404 because the local run did not expose the development secret retrieval configuration. This is not staging evidence and is classified as local test-environment configuration, not a staging PASS.
- `final-hardening-m5.spec.ts`: tablet and mobile failures; the cookie-consent surface intercepted the `Validate access` control.
- `final-contract-validation.spec.ts`: mobile failure; the cookie-consent surface intercepted the booking quote control.
- `m1-m2-real-workflows.spec.ts`: mobile failure; the cookie-consent surface intercepted the booking quote control.
- `mobile-client-booking-gallery.spec.ts`: mobile failure; the cookie-consent surface intercepted the booking quote control.
- `property-manager-mobile-matrix.spec.ts`: mobile failure; the fixed mobile navigation/cookie-consent surfaces intercepted `Record reading`.

The complete suite therefore executed to completion but did not meet the zero-failure / zero-did-not-run acceptance gate. These results must not be represented as full product PASS until the test setup or UI interaction defects are corrected and rerun.

## Acceptance rule

Authenticated staging workflows remain `BLOCKED_EXTERNAL` until Terrence provides legitimate QA credentials/accounts and deploys the relevant PR heads. Local tests do not substitute for staging acceptance.
