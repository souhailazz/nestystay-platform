# Current local SonarQube scan — 2026-09-28

All three scans were submitted to a disposable local SonarQube Community Build `26.9.0.129388` and processed server-side. No token or credential is stored in this evidence.

| Scope | SHA | Bugs | Vulnerabilities | Hotspots | Code smells | Coverage | Duplication | Server status |
|---|---|---:|---:|---:|---:|---:|---:|---|
| Backend | `e215b895dcb64e43e792362eba4940898d7e6936` | 0 | 0 | 0 | 988 | 73.4% | 20.4% | OK, no configured gate conditions |
| Frontend | `e47ace79cc1c31cb19ca047e491cb27c118b45b2` | 0 | 0 | 0 | 972 | 32.7% Sonar executable lines; 32.81% tracked-test V8 lines | 1.4% | OK, no configured gate conditions |
| Platform orchestration | `233443236eaab2486c05a0896b286c10ae1759c5` | 0 | 0 | 0 | 0 | n/a | 0.0% | OK, no configured gate conditions |

Backend verification used the current isolated head with disposable PostgreSQL and MinIO services. The unfiltered backend suite passed `303/303` with zero failures and zero skips: Domain 6, Application 98, Infrastructure 32, and API 167. The MinIO round-trip/overwrite/signed-download/invalid-credential check passed `1/1`, and the PostgreSQL two-instance concurrency check passed `1/1`.

Frontend verification passed `129/129` tracked Vitest tests across 35 files. The local V8 report is `32.81%` lines, `30.98%` statements, `24.38%` branches, and `32.11%` functions. The exact committed frontend head is `e47ace7`. Ignored local `src/coverage` helper tests were excluded from this evidence.

The current-head Playwright matrix at frontend `20f199f` completed `224/224` started tests: `213` passed, `0` failed, `11` explicit skips, and `0` did-not-run in 26.5 minutes. Desktop/tablet/mobile Chromium and configured Firefox/WebKit smoke scopes ran against disposable PostgreSQL with the local Admin fixture. The 11 skips remain explicit provider/deployment-only guards and are not claimed as local product verification. The later frontend commit `14455cc` is test-only and did not alter the runtime source exercised by that matrix.

These are local evidence results only. The disposable Sonar instance has no production acceptance conditions, so `OK` must not be read as proof that the requested 80% backend target, 60% Sonar frontend target, staging provider delivery, deployment SHA parity, or production readiness has passed.
