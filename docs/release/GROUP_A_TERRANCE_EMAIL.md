# Terrence handoff email

**Subject:** NestyStay Group A remediation — review and staging acceptance handoff

Hi Terrence,

The current Group A work is prepared for review. The relevant pull requests are based on the current repository `main` branches, are mergeable, and have green required CI checks:

- Frontend PR [#12](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/12) — searchable authorized Admin entity selectors.
- Frontend PR [#13](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/13) — Campaign timestamps displayed in `America/Jamaica` while API persistence remains UTC.
- Frontend PR [#14](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/14) and Backend PR [#16](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/pull/16) — complete password-reset flow.
- Frontend PRs [#15](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/15) and [#16](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/16) — booking-state/pending-listing protection and responsive booking modal.
- Frontend PRs [#17](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/17), [#18](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/18), [#19](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/19), [#20](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/20), [#21](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/21), [#22](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/22), and [#23](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/23) — moderation, user-facing cleanup, wellness terminology, PM navigation/layout, directory audiences, and dedicated Admin host routing.
- Backend PRs [#17](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/pull/17) and [#18](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/pull/18) — worker status reporting and directory audience enforcement.
- Frontend PR [#24](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/24) — test-harness fix for origin-scoped consent state; no product behavior change.

Local evidence:

- Frontend Vitest: 208 passed, 0 failed.
- Backend Release tests: 307 passed, 0 failed, 2 environment-gated skips.
- Focused Gate Guard, QR, authorization, Safety/PM tests: 7 passed.
- Full Playwright matrix: 233 discovered and started, 212 passed, 20 explicit skips, 0 not run. One tablet calendar assertion was flaky in the full run and passed in isolated desktop/tablet reruns.

Public staging checks currently return live `200`, ready `200`, and storage `CONFIGURED`. The backend reports SHA `e86be7daa0b4c38cf3525ec8678a2f796b476692`. The frontend `/version.json` still reports `local`, so its deployed SHA is not yet independently verified.

Please deploy the approved heads and provide authorized staging accounts for the following acceptance walkthroughs:

1. Property Manager: `https://staging.nestystay.net/pm/calendar` — create/edit/synchronize a calendar event and verify persistence/conflict behavior.
2. Super Admin: `https://admin.nestystay.net/admin/sign-in` and `https://staging.nestystay.net/admin/ops/properties` — review, approve, reject, and verify listing visibility/booking restrictions.
3. Property Manager/Safety role: `https://staging.nestystay.net/safety` and `https://staging.nestystay.net/pm/professional?tab=incidents` — create, retrieve, and authorize an incident workflow.
4. PM and Gate Guard: `https://staging.nestystay.net/pm/team`, `/gate/guard`, and `/gate/qr` — invite, activate, assign a property, validate QR codes, and verify cross-property denial.

Items requiring your decision remain the approved Patois wording, executive-term rules for common-area utility retrieval, and the exact meeting/minutes/governance workflow. No pricing, date-hold, provider, migration, production, or deployment-pipeline changes were made.

Regards,

Souhail
