# Current local SonarQube scan — 2026-09-28

All three current isolated scopes were scanned and processed server-side by a fresh disposable local SonarQube Community Build `26.9.0.129388`. No token or credential is stored in this evidence.

| Scope | SHA | Bugs | Vulnerabilities | Hotspots | Code smells | Coverage | Duplication | Server status |
|---|---|---:|---:|---:|---:|---:|---:|---|
| Backend | `eeab0d2e97a18fc8ec0f909bba8944e54fc542ab` | 0 | 0 | 0 | 994 | 73.4% | 20.3% | OK, no configured gate conditions |
| Frontend | `fe01b881b5afa40b9a2c173cd20c83ab38f5bf35` | 0 | 0 | 0 | 972 | 60.0% Sonar executable lines; 64.41% tracked-test V8 lines | 1.4% | OK, no configured gate conditions |
| Platform orchestration | `ad2735e88f50891a8d1cbd29823a8999535db336` | 0 | 0 | 0 | 0 | n/a | 0.0% | OK, no configured gate conditions |

Backend verification at current isolated head `eeab0d2` used disposable PostgreSQL and MinIO services. The unfiltered backend suite passed `303/303` with zero failures and zero skips: Domain 6, Application 98, Infrastructure 32, and API 167. The MinIO round-trip/overwrite/signed-download/invalid-credential check passed `1/1`, and the PostgreSQL two-instance concurrency check passed `1/1`. Migration `20260928174138_ApplyFoundingMembershipTerms` applied successfully. Exact-head Sonar line coverage is 73.4% with 994 code smells.

Frontend verification at current isolated head `fe01b88` passed `177/177` tracked Vitest tests across 45 files. The local V8 report is `64.41%` lines, `60.79%` statements, `52.67%` branches, and `57.24%` functions. Exact-head Sonar line coverage is `60.0%`. The promoted `src/coverage` helpers are tracked behavioral tests and are included in this evidence.

The current-head Playwright matrix at frontend `20f199f` completed `224/224` started tests: `213` passed, `0` failed, `11` explicit skips, and `0` did-not-run in 26.5 minutes. Desktop/tablet/mobile Chromium and configured Firefox/WebKit smoke scopes ran against disposable PostgreSQL with the local Admin fixture. The 11 skips remain explicit provider/deployment-only guards and are not claimed as local product verification. The later frontend commit `14455cc` is test-only and did not alter the runtime source exercised by that matrix.

These are local evidence results only. The fresh disposable Sonar instance has no configured production acceptance conditions, so `OK` must not be read as proof that the requested 80% backend target, 60% Sonar frontend target, staging provider delivery, deployment SHA parity, or production readiness has passed.
