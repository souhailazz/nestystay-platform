# SonarQube Full Platform Audit

Date: 2026-09-27
Server: local SonarQube Community Build at `http://localhost:9000`
The temporary analysis token was not committed or included in this report.

## Current local scan summary — 2026-09-27

These are fresh analyses submitted and processed by the local SonarQube server from the isolated certification worktrees. A scanner exit code of zero is not treated as a Quality Gate pass.

| Project | Bugs | Vulnerabilities | Hotspots | Code smells | Coverage | New-code coverage | New violations | Duplication | Quality Gate |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|
| `nestystay-backend` | 0 | 0 | 0 | 996 | 14.4% | 7.5% | 11 | 20.5% | FAIL |
| `nestystay-frontend` | 0 | 0 | 0 | 973 | 55.6% | 64.9% | 4 | 1.4% | FAIL |
| `nestystay-platform` | 120 | 6 | 0 | 199 | 0.0% | not configured | 0 | 8.4% | PASS for new-code gate |

The backend and frontend gates fail because coverage and/or new-code violations do not meet the local gate. The platform scan covers orchestration/configuration paths only; its overall findings do not certify the runtime applications.

## Historical scan summary

| Project | Bugs | Vulnerabilities | Hotspots | Code smells | Coverage | Duplication | Quality Gate |
|---|---:|---:|---:|---:|---:|---:|---|
| `nestystay-backend` | 9 | 31 | 0 | 1009 | 51.2% overall / 50.9% line | 20.6% | FAIL |
| `nestystay-frontend` | 0 | 0 | 0 | 970 | 25.8% line / 21.4% combined | 1.4% | FAIL |
| `nestystay-platform` | 120 | 6 | 0 | 199 | 0.0% | 8.4% | PASS for new-code gate only |

Backend and frontend each have one new violation and fail their new-code gates. Backend coverage is now imported from four OpenCover reports, but new-code coverage remains 45.6% (below 80%). The platform scan has no new violation, but its overall findings are not a clean certification result.

## Evidence exports

- [`backend-issues.json`](../../testing-evidence/sonarqube/backend-issues.json)
- [`frontend-issues.json`](../../testing-evidence/sonarqube/frontend-issues.json)
- [`platform-issues.json`](../../testing-evidence/sonarqube/platform-issues.json)

The exports contain Sonar issue metadata and messages only; they contain no tokens or credentials.

## 2026-09-26 certification update

The 2026-09-26 values below are retained as historical evidence only and must not be used as the current result.

The current frontend scan reports Quality Gate `OK`, 0 bugs, 0 vulnerabilities, 0 hotspots, 978 code smells, 52.5% overall / 56.2% line coverage and 1.6% duplication. Local Vitest line coverage is 60.04%; Sonar's lower executable-line calculation is the governing Sonar value. Critical/major findings were reviewed by rule and affected area and remain a documented maintainability backlog, not a security-gate pass. See [`FINAL-END-TO-END-CERTIFICATION-2026-09-26.md`](FINAL-END-TO-END-CERTIFICATION-2026-09-26.md) for the current release verdict.

## Interpretation

The 2026-09-27 scans genuinely executed and uploaded server-side. Backend and frontend security metrics are currently clean (zero bugs, vulnerabilities and hotspots), but the requested 60% overall frontend coverage and 80% new-code coverage were not achieved. The frontend local Vitest run reports 60.11% line coverage while Sonar reports 55.6% using its executable-line accounting; both values are recorded without conflation. Fresh issue exports are available in `testing-evidence/sonarqube/`. The next required work is behavior-preserving remediation of the fresh new-code findings and additional meaningful tests, followed by a rerun.
