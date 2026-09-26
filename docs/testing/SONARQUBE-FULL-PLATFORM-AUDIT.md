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

## 2026-09-26 certification update

The current release-candidate backend scan was rerun and submitted successfully from commit `ebb85f72168610be68586340bfdcba4493f37777`. Sonar processed it with Quality Gate `OK`, 0 bugs, 0 vulnerabilities, 0 hotspots, 1,007 code smells, 11.2% overall coverage, 9.6% line coverage, 38.2% branch coverage and 20.6% duplication. The coverage number is incomplete because the fresh Infrastructure/API OpenCover collector repeatedly stalled; the normal backend suite independently passed 209/209 tests. The previous backend values above are historical and must not be used as the current result.

The current frontend scan reports Quality Gate `OK`, 0 bugs, 0 vulnerabilities, 0 hotspots, 978 code smells, 52.5% overall / 56.2% line coverage and 1.6% duplication. Local Vitest line coverage is 60.04%; Sonar's lower executable-line calculation is the governing Sonar value. Critical/major findings were reviewed by rule and affected area and remain a documented maintainability backlog, not a security-gate pass. See [`FINAL-END-TO-END-CERTIFICATION-2026-09-26.md`](FINAL-END-TO-END-CERTIFICATION-2026-09-26.md) for the current release verdict.

## Interpretation

The scans genuinely executed and uploaded server-side. A green scanner exit code is not treated as a green Quality Gate. The requested 60% overall frontend coverage and 80% new-code coverage were not achieved, and the existing high/critical maintainability/security findings have not all been refactored in this pass. Local frontend LCOV reports 25.13% line coverage; Sonar reports 25.8% line and 21.4% combined coverage because Sonar applies its own executable-line/condition accounting. The frontend remediation pass removed all previously reported frontend bugs and vulnerabilities.
