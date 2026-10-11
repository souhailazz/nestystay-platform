# Terrence handoff email

**Subject:** NestyStay Group A — priority PR review and staging acceptance

Hi Terrence,

Thank you for the staging feedback. I reconciled the Group A handoff against the current GitHub PR state and local evidence. The complete A1–A28 matrix is in the tracker, with each item’s PR, head SHA, merge state, evidence boundary and staging action.

## Current review queue

The earlier Group A PRs are already merged. The current reviewable work is:

1. **Frontend #16** — `762b9c3f025f7a222ce9c14eea0d8a0599c64539`: semantic booking/modal reconciliation preserving the pending-listing fix; CI green.
2. **Frontend #26** — `84e0ba19964c020558332b9d71cd4793e9da78f0`: role-specific Patois greeting framework; CI green.
3. **Frontend #27** — `7dfe8898d1a07eac4205aaae8fe9cd7f87f70868`: responsive role/workspace sign-in redesign; CI green.
4. **Frontend #28** — `3e46498b4c71cdb9e237654cc619fa39a57b2caa`: public/staging Admin-route denial follow-up; CI green.
5. **Backend #19** — `795a8f170abd00973a4089d167acc7abb97a1e87`: A27 utility bills, common-area/period/executive-term persistence, attachments and scoped retrieval; additive migration review only; backend CI rerunning after the authorization-matrix baseline was updated.
6. **Frontend #29** — `22db3ce2cd1367a6a7e8018f169d88cc89a9f103`: A27 Property Manager utility-bill/executive-term workspace; frontend CI green.
7. **Backend #20** — `b2caffedf429cc72652726706edb47b12cf53bcf`: A28 governance meetings, agenda/attendance, linked proposals and immutable published minutes; additive migration review only; backend CI green.
8. **Frontend #30** — `0ae5fa10afe63acc22580c0896823fc61f52890f`: A28 governance-meeting/minutes workspace; frontend CI green.

PR16, PR26, PR27, PR28, PR29 and PR30 are open and reviewable; PR29 and PR30 have successful required CI. Backend PR20 is green; Backend PR19 is rerunning its required check after the route-count regression fix. None of these has been merged or deployed by Codex. The exact A1–A28 merged/open status is in the tracker.

## Verified local evidence

- Playwright: **233 started, 213 passed, 0 failed, 20 documented skips, 0 not run**.
- Backend Release tests: **307 passed, 0 failed, 2 environment-gated skips**.
- Frontend Vitest: **208 passed, 0 failed**.
- Focused Gate Guard, QR, authorization, Safety and PM tests: **7 passed**.
- MinIO local I/O, authorization and restart persistence: **passed**.
- Frontend PR #25 improves calendar E2E synchronization and diagnostics; it does **not** establish successful live staging calendar synchronization.
- A28 backend full suite: **309 passed, 0 failed, 2 environment-gated skips** (311 total). The route-count authorization assertion was updated from the old 193 baseline to the current 199 protected descriptions and then passed.
- A27 focused utility-bill persistence/attachment/statement/authorization test: **1 passed**; backend build passed.
- A27 and A28 frontend suites: **223 passed, 0 failed** each; typecheck and production build passed; lint had 0 errors and existing warnings only.

Public staging health is currently live `200`, ready `200` with database ready and storage configured. The backend reports SHA `e86be7daa0b4c38cf3525ec8678a2f796b476692`. Frontend `/version.json` still reports `commitSha: local`, so the deployed frontend SHA is not independently verified.

## Staging acceptance needed

Please merge the approved PRs and deploy them to staging, then provide an authorized QA session for the existing QA accounts. I understand the accounts exist and that you have already verified Super Admin and Property Manager sign-in; the remaining issue is that Codex does not have an authorized authenticated session or credentials to exercise the workflows.

The detailed walkthrough is in `GROUP_A_STAGING_HANDOFF.md`. The priority pages are password reset, the pending/approved booking flows, `/admin/ops/properties`, `/pm/calendar`, `/safety`, `/pm/professional?tab=incidents`, `/pm/team`, `/gate/guard`, `/gate/qr` and the PM record workspace.

Please review/deploy PR26 before accepting A18. A20, A22, A23 and A26 still need authorized staging workflows. A27 and A28 are now implementation PR pairs ready for review; their additive migrations are not applied and require your approval before controlled staging application. For A27, verify `/pm/utilities`; for A28, verify `/pm/governance`. The detailed steps, expected results and authorization checks are in `GROUP_A_STAGING_HANDOFF.md`.

Group B additions remain excluded. No production, provider, pricing, date-hold, migration or deployment-pipeline changes were made.

Regards,

Souhail
