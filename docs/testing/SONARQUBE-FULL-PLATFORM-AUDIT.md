# SonarQube Full Platform Audit

Date: 2026-09-23
Server: local SonarQube Community Build at `http://localhost:9000`
The temporary analysis token was not committed or included in this report.

## Final local scan summary

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

## Interpretation

The scans genuinely executed and uploaded server-side. A green scanner exit code is not treated as a green Quality Gate. The requested 60% overall frontend coverage and 80% new-code coverage were not achieved, and the existing high/critical maintainability/security findings have not all been refactored in this pass. Local frontend LCOV reports 25.13% line coverage; Sonar reports 25.8% line and 21.4% combined coverage because Sonar applies its own executable-line/condition accounting. The frontend remediation pass removed all previously reported frontend bugs and vulnerabilities.
