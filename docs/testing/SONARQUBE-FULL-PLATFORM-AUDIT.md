# SonarQube Full Platform Audit

Date: 2026-09-27
Server: local SonarQube Community Build at `http://localhost:9000`
The temporary analysis token was not committed or included in this report.

## Current local scan summary — 2026-09-27

These are fresh analyses submitted and processed by the local SonarQube server from the isolated certification worktrees. A scanner exit code of zero is not treated as a Quality Gate pass. Current analyzed revisions are backend `6277a950d563ce32e01a51223776fac9981529dd`, frontend `96cbac8209f87381508bcebf0de13b8bb716ff07`, and platform `437804636b944d0e5c348185f0a2b76912e8c9f5`.

| Project | Bugs | Vulnerabilities | Hotspots | Code smells | Coverage | New-code coverage | New violations | Duplication | Quality Gate |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|
| `nestystay-backend` | 0 | 0 | 0 | 996 | 10.3% | 6.9% | 11 | 20.4% | FAIL |
| `nestystay-frontend` | 0 | 0 | 0 | 973 | 55.6% | 64.9% | 4 | 1.4% | FAIL |
| `nestystay-platform` | 120 | 6 | 0 | 199 | 0.0% | not configured | 0 | 8.4% | PASS for new-code gate |

The backend gate fails on 6.9% new-code coverage, 3.00926% new-code duplication, and 11 new violations. The frontend gate fails on 64.9% new-code coverage and 4 new violations. Both scans report zero bugs, vulnerabilities, and security hotspots. The platform scan covers orchestration/configuration paths only; its overall findings do not certify the runtime applications.

## Historical scan summary

| Project | Bugs | Vulnerabilities | Hotspots | Code smells | Coverage | Duplication | Quality Gate |
|---|---:|---:|---:|---:|---:|---:|---|
| `nestystay-backend` | 9 | 31 | 0 | 1009 | 51.2% overall / 50.9% line | 20.6% | FAIL |
| `nestystay-frontend` | 0 | 0 | 0 | 970 | 25.8% line / 21.4% combined | 1.4% | FAIL |
| `nestystay-platform` | 120 | 6 | 0 | 199 | 0.0% | 8.4% | PASS for new-code gate only |

The historical table is retained for comparison only. The current scan above supersedes it. The backend scan imported the available current OpenCover reports; the full Coverlet run was stopped after instrumentation exceeded the normal-suite runtime, so the imported coverage is explicitly partial rather than treated as complete backend coverage.

## Evidence exports

- [`backend-issues.json`](../../testing-evidence/sonarqube/backend-issues.json)
- [`frontend-issues.json`](../../testing-evidence/sonarqube/frontend-issues.json)
- [`platform-issues.json`](../../testing-evidence/sonarqube/platform-issues.json)
- [`CURRENT-SCAN-2026-09-27.json`](../../testing-evidence/sonarqube/CURRENT-SCAN-2026-09-27.json)

The current summary records the server-side API results and analysis task IDs without tokens or credentials. The larger issue exports are retained as same-day Sonar issue evidence; the current gate/measure values above and in the current summary are authoritative for this run.

## 2026-09-26 certification update

The 2026-09-26 values below are retained as historical evidence only and must not be used as the current result.

The current frontend scan reports Quality Gate `OK`, 0 bugs, 0 vulnerabilities, 0 hotspots, 978 code smells, 52.5% overall / 56.2% line coverage and 1.6% duplication. Local Vitest line coverage is 60.04%; Sonar's lower executable-line calculation is the governing Sonar value. Critical/major findings were reviewed by rule and affected area and remain a documented maintainability backlog, not a security-gate pass. See [`FINAL-END-TO-END-CERTIFICATION-2026-09-26.md`](FINAL-END-TO-END-CERTIFICATION-2026-09-26.md) for the current release verdict.

## Interpretation

The 2026-09-27 scans genuinely executed and uploaded server-side. Backend and frontend security metrics are currently clean (zero bugs, vulnerabilities and hotspots), but the requested 60% overall frontend coverage and 80% new-code coverage were not achieved. The frontend local Vitest run reports 60.11% line coverage while Sonar reports 55.6% using its executable-line accounting; both values are recorded without conflation. The local scan also reports 996 backend and 973 frontend maintainability issues in the current project scopes. The next required work is behavior-preserving remediation of the fresh new-code findings and additional meaningful tests, followed by a rerun.
