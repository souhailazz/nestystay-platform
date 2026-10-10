# Terrence handoff email

**Subject:** NestyStay Group A — priority PR review and staging acceptance

Hi Terrence,

Thank you for the staging feedback. I reconciled the Group A handoff against the current GitHub PR state and local evidence. The complete A1–A28 matrix is in the tracker, with each item’s PR, head SHA, merge state, evidence boundary and staging action.

## Priority review queue

1. **Password reset:** [Backend #16](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/pull/16), `aa73f4b6112c5f8fd1b3868e2d10a727cfcda4a0`; [Frontend #14](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/14), `50a90c1919553ec135bae7dbf5e86581aae64ff2`.
2. **Booking correctness:** [Frontend #15](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/15), `ec0640980190cdd0c45f525c38aaf81064667e0b`.
3. **Booking modal:** [Frontend #16](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/16), `d3b4579127ae55983e656490b0ad3b5e1abf7d62`.
4. **Admin:** [Frontend #12](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/12), [#13](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/13) and [#17](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/17).
5. **Remaining Group A fixes:** Frontend [#18](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/18), [#19](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/19), [#20](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/20), [#21](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/21), [#22](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/22), [#23](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/23), [#24](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/24), [#25](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/25); Backend [#17](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/pull/17) and [#18](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/pull/18).

All listed PRs are currently **OPEN**, **MERGEABLE**, and have successful required build/test checks; protected review is why GitHub reports their merge state as `BLOCKED`. Deployment jobs are skipped. They are therefore not yet merged, deployed or staging-verified.

Recommended order is Backend #16 → Frontend #14; Frontend #15 → #16; Frontend #12 → #13 → #17; Backend #17 after Backend #16 because both touch health-test coverage; Backend #18 separately; then the remaining UI/host/navigation PRs after rechecking the shared-file overlaps. Frontend #19/#23, #20/#22 and #18/#21 share files, so GitHub mergeability should be rechecked after each earlier merge. No branch currently requires a rebase based on its present `MERGEABLE` state.

## Verified local evidence

- Playwright: **233 started, 213 passed, 0 failed, 20 documented skips, 0 not run**.
- Backend Release tests: **307 passed, 0 failed, 2 environment-gated skips**.
- Frontend Vitest: **208 passed, 0 failed**.
- Focused Gate Guard, QR, authorization, Safety and PM tests: **7 passed**.
- MinIO local I/O, authorization and restart persistence: **passed**.
- Frontend PR #25 improves calendar E2E synchronization and diagnostics; it does **not** establish successful live staging calendar synchronization.

Public staging health is currently live `200`, ready `200` with database ready and storage configured. The backend reports SHA `e86be7daa0b4c38cf3525ec8678a2f796b476692`. Frontend `/version.json` still reports `commitSha: local`, so the deployed frontend SHA is not independently verified.

## Staging acceptance needed

Please merge the approved PRs and deploy them to staging, then provide an authorized QA session for the existing QA accounts. I understand the accounts exist and that you have already verified Super Admin and Property Manager sign-in; the remaining issue is that Codex does not have an authorized authenticated session or credentials to exercise the workflows.

The detailed walkthrough is in `GROUP_A_STAGING_HANDOFF.md`. The priority pages are password reset, the pending/approved booking flows, `/admin/ops/properties`, `/pm/calendar`, `/safety`, `/pm/professional?tab=incidents`, `/pm/team`, `/gate/guard`, `/gate/qr` and the PM record workspace.

Before A18 can be completed, please provide the approved role-specific Patois wording and translations. Before A27/A28 implementation can begin, please confirm executive-term utility rules and the required meetings/minutes/agendas/governing-document lifecycle, permissions and quorum rules. Those items are intentionally not invented or migrated without approval.

Group B additions remain excluded. No production, provider, pricing, date-hold, migration or deployment-pipeline changes were made.

Regards,

Souhail
