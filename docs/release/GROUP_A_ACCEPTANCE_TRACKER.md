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
- Frontend PR #23: 209 tests passed, typecheck passed, build passed, lint 0 errors / 32 existing warnings; hosted CI pending at time of tracker creation.
- Backend directory-audience branch: 308 tests passed, 0 failed, 1 pre-existing MinIO skip; Release build passed.
- Local disposable MinIO: real roundtrip, signed download, wrong-credential denial, traversal/oversize checks and restart persistence passed.
- Staging public health: live 200; ready 200 with database ready and storage configured.
- Staging backend version: `e86be7daa0b4c38cf3525ec8678a2f796b476692`.
- Staging frontend `/version.json`: reports `commitSha: local`; deployed frontend SHA is therefore unknown.

## Acceptance rule

Authenticated staging workflows remain `BLOCKED_EXTERNAL` until Terrence provides legitimate QA credentials/accounts and deploys the relevant PR heads. Local tests do not substitute for staging acceptance.
