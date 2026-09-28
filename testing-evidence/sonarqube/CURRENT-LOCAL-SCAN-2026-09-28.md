# Current local SonarQube scan — 2026-09-28

All three scans were submitted to a disposable local SonarQube Community Build `26.9.0.129388` and processed server-side. No token or credential is stored in this evidence.

| Scope | SHA | Bugs | Vulnerabilities | Hotspots | Code smells | Coverage | Duplication | Server status |
|---|---|---:|---:|---:|---:|---:|---:|---|
| Backend | `ff31270f134c599fadb7dd7058f361fb45cc55ff` | 0 | 0 | 0 | 798 | 73.2% | 6.9% | OK, no configured gate conditions |
| Frontend | `20f199f80bea89e58b3632a4e828e04c488c9652` | 0 | 0 | 0 | 972 | 58.8% Sonar executable lines; 63.41% local V8 lines | 1.4% | OK, no configured gate conditions |
| Platform orchestration | `233443236eaab2486c05a0896b286c10ae1759c5` | 0 | 0 | 0 | 0 | n/a | 0.0% | OK, no configured gate conditions |

Backend verification used the current isolated head with disposable PostgreSQL and MinIO services. The unfiltered backend suite passed `229/229` with zero failures and zero skips: Domain 6, Application 24, Infrastructure 32, and API 167. The MinIO round-trip/overwrite/signed-download/invalid-credential check passed `1/1`, and the PostgreSQL two-instance concurrency check passed `1/1`.

Frontend verification passed `157/157` Vitest tests across 37 files. The local V8 report is `63.41%` lines, `59.69%` statements, `51.20%` branches, and `56.30%` functions.

The current-head Playwright matrix at frontend `20f199f` completed `224/224` started tests: `213` passed, `0` failed, `11` explicit skips, and `0` did-not-run in 26.5 minutes. Desktop/tablet/mobile Chromium and configured Firefox/WebKit smoke scopes ran against disposable PostgreSQL with the local Admin fixture. The 11 skips remain explicit provider/deployment-only guards and are not claimed as local product verification.

These are local evidence results only. The disposable Sonar instance has no production acceptance conditions, so `OK` must not be read as proof that the requested 80% backend target, 60% Sonar frontend target, staging provider delivery, deployment SHA parity, or production readiness has passed.
