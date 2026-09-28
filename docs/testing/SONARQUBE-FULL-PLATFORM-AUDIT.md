# SonarQube Full Platform Audit

Date: 2026-09-27
Servers: local SonarQube Community Build at `http://localhost:9001` (prior baseline) and disposable refresh at `http://localhost:9002`
The temporary analysis token was not committed or included in this report.

## Post-scan frontend test coverage update — 2026-09-28

Frontend commits `f1f935b`, `9ccfb75`, and `e74c701` added behavioral tests for the landing SearchBar, Property Manager modules, booking identity-verification handoff, and invoice/receipt workflows. The complete local Vitest suite at `e74c701` passes **157/157 across 37 files** and V8 reports **63.41% line, 59.68% statement, 51.20% branch, and 56.27% function coverage**. This is local test-run evidence only; a new Sonar analysis has not been run for `e74c701`, so the Sonar metrics below remain attributed to the prior scanned frontend SHA and are not silently relabeled.

## Current local scan summary — 2026-09-27

These are fresh analyses submitted and processed by isolated local SonarQube servers from the certification worktrees. A scanner exit code of zero is not treated as a production Quality Gate pass. The prior baseline remains backend `6277a950d563ce32e01a51223776fac9981529dd`, frontend `3b350a28a06f8e3f8b38d8c2448af60b6145becb`, and platform/root evidence `b5d06dba3fe855f36e41c6b3123f249ee86f7b10`. A post-refactor backend refresh was subsequently uploaded and processed at `e195e8b1720157cbe1c172a30e658b6dc6be7215`; see [`BACKEND-POST-REFACTOR-2026-09-27.md`](../../testing-evidence/sonarqube/BACKEND-POST-REFACTOR-2026-09-27.md) and the JSON evidence beside it.

## Latest backend refresh at `e195e8b`

The post-refactor analysis task `34d63bc6-ff30-460e-928c-385ae056a704` completed server-side with `SUCCESS`. It reported 0 bugs, 0 vulnerabilities, 0 security hotspots, 805 code smells, 15.1% overall line coverage, 48.7% branch coverage, 6.9% duplication and 0 new violations under the configured new-violations-only gate. Six OpenCover reports were imported, but they covered only 48 backend source files; therefore this is a valid static-analysis refresh, not evidence that the requested full backend coverage target has passed.

| Project | Bugs | Vulnerabilities | Hotspots | Code smells | Coverage | New-code coverage | New violations | Duplication | Quality Gate |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---|
| `nestystay-backend-certification` | 0 | 0 | 0 | 996 | 52.2% line | not emitted | 0 | 20.4% | OK — new-violations-only isolated gate |
| `nestystay-frontend-certification` | 0 | 0 | 0 | 977 | 63.3% line | not emitted | 0 | 1.4% | OK — new-violations-only isolated gate |
| `nestystay-platform-certification` | 0 | 0 | 0 | 0 | n/a | n/a | 0 | n/a | OK |

The fresh backend and frontend scans report zero bugs, vulnerabilities and hotspots. Their isolated Quality Gate is not the requested release gate: it only evaluates new violations. Backend coverage is 52.2% line / 51.8% branch and frontend coverage is 63.3% line / 48.8% branch; backend/frontend maintainability backlogs remain at 996 and 977 code smells. The platform scan covers orchestration/configuration paths only and is not runtime application certification.

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

The current summary records the server-side API results and analysis task IDs without tokens or credentials. The larger issue exports are retained as same-day Sonar issue evidence; the current gate/measure values above and in [`CERTIFICATION-SUMMARY-2026-09-27.md`](../../testing-evidence/sonarqube/CERTIFICATION-SUMMARY-2026-09-27.md) are authoritative for this run.

## 2026-09-26 certification update

The 2026-09-26 values below are retained as historical evidence only and must not be used as the current result.

The current frontend scan reports Quality Gate `OK`, 0 bugs, 0 vulnerabilities, 0 hotspots, 978 code smells, 52.5% overall / 56.2% line coverage and 1.6% duplication. Local Vitest line coverage is 60.04%; Sonar's lower executable-line calculation is the governing Sonar value. Critical/major findings were reviewed by rule and affected area and remain a documented maintainability backlog, not a security-gate pass. See [`FINAL-END-TO-END-CERTIFICATION-2026-09-26.md`](FINAL-END-TO-END-CERTIFICATION-2026-09-26.md) for the current release verdict.

## Interpretation

The 2026-09-27 scans genuinely executed and uploaded server-side. Backend and frontend security metrics are currently clean (zero bugs, vulnerabilities and hotspots). Sonar frontend line coverage is 63.3%, above the requested 60% overall line threshold, while local Vitest line coverage is 61.17%; these are recorded without conflation. The requested 80% new-code acceptance was not emitted by the isolated baseline and is not claimed as passed. The local scan reports 996 backend and 977 frontend maintainability issues in the current project scopes. The next required work is behavior-preserving remediation of the maintainability/new-code backlog and a release-gate scan against the real protected main baseline.
