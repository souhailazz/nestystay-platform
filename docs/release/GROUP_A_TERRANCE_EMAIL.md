# Terrence handoff email

**Subject:** NestyStay Group A — priority PR review and staging acceptance

Hi Terrence,

Thank you for the staging feedback. I reconciled the Group A handoff against the current GitHub PR state and local evidence. The complete A1–A28 matrix is in the tracker, with each item’s PR, head SHA, merge state, evidence boundary and staging action.

## Current review queue

The earlier Group A implementation PRs are merged. The current reviewable work is:

1. **Backend #19** — `2be9a65019cb63941491211c5be3ffff8fe7364f`: A27 utility bills, common-area/period/executive-term persistence, attachments, scoped retrieval and statement attachment mapping. The additive migration remains review-only. [PR #19](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/pull/19)
2. **Frontend #34** — `fe16c77d0bc5fd511da5a5072827b3548e6a1229`: current-main A27 successor for the utility-bill workspace and statement attachment actions; local focused tests 4/4, typecheck, build and targeted persisted utility browser flow 1/1 passed; required hosted CI passed in run `38175769454` and deployment was skipped. It preserves current Gate Guard/staff changes and supersedes conflicting Frontend #29. [PR #34](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/34)
3. **Backend #20** — `6724afa674a2a9c59a960e67a5f596a1e293ad0b`: A28 governance meetings, agenda/attendance, linked proposals, immutable minutes and property-scoped governing-document endpoints. The existing meeting migration remains review-only; exact-head CI passed. [PR #20](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/pull/20)
4. **Frontend #33** — `a9939b7895c9d191298738d8028274382977b8af`: current-main A28 successor with governing-document upload/list/open actions; exact-head CI passed. It preserves current-main Gate Guard changes and supersedes conflicting Frontend #30. [PR #33](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/33)

Frontend #29 and #30 are superseded/conflicting. Frontend #31 is a redundant A8 follow-up because merged Frontend #15/current main already contains the pending-property business-error behavior. A1–A26 implementation PRs referenced by the matrix are merged or their implementation is already in current main. No merge or deploy was performed by Codex.

## Verified local evidence

- Playwright: **233 started, 213 passed, 0 failed, 20 documented skips, 0 not run**.
- Backend Release tests: **309 passed, 0 failed, 2 environment-gated skips**.
- Frontend Vitest: the prior baseline remains **208 passed, 0 failed**; the A18 follow-up focused run passed **32 tests**. PR16's focused booking run passed **4 tests**; its broader local run exposed **3 unrelated existing failures**, so no blanket frontend-suite pass is claimed for that branch.
- Focused Gate Guard, QR, authorization, Safety and PM tests: prior focused API checks passed; the latest PM release-blocker browser run passed **3/4**, with **1 documented environment skip**.
- New targeted local browser evidence: Gate Guard/QR **2/2**, calendar plus PM calendar/QR **3/3**, officer/wellness plus PM completion **2/2**, PM professional completion **1/1**, and incident create/reload/resolve/authorization **1/1**. These runs used a disposable local API/database and are not staging acceptance.
- MinIO local I/O, authorization and restart persistence: **passed**.
- Frontend PR #25 improves calendar E2E synchronization and diagnostics; it does **not** establish successful live staging calendar synchronization.
- A28 backend full suite after the governing-document endpoints: **310 passed, 0 failed, 2 environment-gated skips** (312 total). The route-count authorization assertion now covers 201 property-manager routes.
- A27 focused utility-bill persistence/attachment/statement/authorization test: **1 passed**; backend build passed.
- A27 and A28 frontend suites: **223 passed, 0 failed** each; typecheck and production build passed; lint had 0 errors and existing warnings only.
- A27 successor PR #34 local checks: **4 focused Vitest tests passed**, typecheck passed, production build passed, lint had 0 errors/33 existing warnings, and the targeted persisted utility browser flow passed **1/1**; hosted required CI passed in run `38175769454`.
- A26 browser follow-up on the A27 successor: the earlier **21 total, 15 passed, 3 failed, 3 skipped** run exposed stale pre-A27 selectors and the intentionally collapsed Advanced workspace. The corrected A27 utility persistence/attachment/reload flow passes **1/1**; the focused PM/professional set passes **7/7**, and the additional M5/mobile/PM set passes **6** with **2 environment-gated skips**. This still does not constitute a complete browser demonstration of every PM tool.
- A22 follow-up Backend PR #21: the new regression and the complete `BadgeAuthorizationTests` class (**5 passed**) passed locally; the full backend solution passed **309** with **2 environment-gated skips**; required CI is green and no staging acceptance is claimed.
- A20 follow-up Frontend PR #32: `/pm/team` now renders the scoped Gate Guard invitation UI. The focused module suite passed **3/3**, and the fresh-branch browser flow passed **1/1**. Follow-up Frontend PR #35 (`7d6208eddf0c32afd57a890b80441cc17be71c28`) corrects the role-matrix assertion to verify the actual `/gate/guard` workspace and QR entry point; desktop and mobile role-matrix runs passed **1/1** each, and required CI passed in run `38176917921`. This is local evidence only; staging acceptance still requires real invitation acceptance, email, login, property scope and QR checks.

Public staging health was observed live `200`, ready `200` with database ready and storage configured. The last observed staging backend SHA was `e86be7daa0b4c38cf3525ec8678a2f796b476692`, while the latest fetched backend `main` is `a53ccb3e0fb1040cebde5aa515b6bd2ef9bd51b9`; staging parity is therefore not current. Frontend `/version.json` still reports `commitSha: local`, so the deployed frontend SHA is not independently verified.

## Staging acceptance needed

Please merge the approved PRs and deploy them to staging, then provide an authorized QA session for the existing QA accounts. I understand the accounts exist and that you have already verified Super Admin and Property Manager sign-in; the remaining issue is that Codex does not have an authorized authenticated session or credentials to exercise the workflows.

The detailed walkthrough is in `GROUP_A_STAGING_HANDOFF.md`. The priority pages are password reset, the pending/approved booking flows, `/admin/ops/properties`, `/pm/calendar`, `/safety`, `/pm/professional?tab=incidents`, `/pm/team`, `/gate/guard`, `/gate/qr` and the PM record workspace.

Please review/deploy Backend #19 + Frontend #34 for A27 and Backend #20 + Frontend #33 for A28. For A27, approve/apply the additive migration after backup, then verify `/pm/utilities` and the owner-statement attachment action. For A28, approve/apply the existing additive meeting migration after backup, then verify `/pm/governance`, meetings, immutable minutes and governing documents. The detailed steps, expected results and authorization checks are in `GROUP_A_STAGING_HANDOFF.md`.

## Conditional delivery dates

- A26: one focused PM acceptance session, targeted for October 14 if the authorized disposable PM/Owner session is available by October 13.
- A27: the code/test package is reviewable in Backend #19 and Frontend #34; after your review, migration approval/application and deployment, the focused statement/attachment acceptance is targeted for October 14 if those prerequisites complete by October 13.
- A28: code/test package is ready now; after your review, migration approval/application and deployment, the focused governance/document acceptance is targeted for October 15 if those prerequisites complete by October 14.

These are conditional targets, not staging acceptance claims. If a prerequisite arrives later, the acceptance session moves to the next working day after it is available.

Group B additions remain excluded. No production, provider, pricing, date-hold, migration or deployment-pipeline changes were made.

Regards,

Souhail
