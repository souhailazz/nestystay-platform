# SonarQube certification summary — 2026-09-27

Fresh analyses were executed on the isolated local SonarQube server at `http://localhost:9001` using the current certification heads. Temporary scanner credentials were revoked and are not recorded.

| Project | SHA | Bugs | Vulnerabilities | Hotspots | Code smells | Line coverage | Branch coverage | Duplication | Result |
|---|---|---:|---:|---:|---:|---:|---:|---:|---|
| Backend | `6277a950d563ce32e01a51223776fac9981529dd` | 0 | 0 | 0 | 996 | 52.2% | 51.8% | 20.4% | Security clean; maintainability backlog remains |
| Frontend | `3b350a28a06f8e3f8b38d8c2448af60b6145becb` | 0 | 0 | 0 | 977 | 63.3% | 48.8% | 1.4% | Overall line threshold met; new-code threshold not emitted |
| Root/platform | `b5d06dba3fe855f36e41c6b3123f249ee86f7b10` | 0 | 0 | 0 | 0 | n/a | n/a | n/a | Orchestration/configuration scope clean |

Backend coverage imported four fresh OpenCover reports covering 211 source files. The full backend test run completed with 210 passed, 0 failed and 2 explicit skips; the two skips are covered separately by the Docker-backed MinIO/PostgreSQL integration runs.

The Sonar server returned `OK` for the isolated backend and frontend projects because their only configured gate condition is new violations. That does not certify the requested backend coverage, frontend new-code coverage, or maintainability backlog. Production readiness remains blocked by those acceptance gaps, real Brevo delivery, and deployed SHA surfaces on staging.

## Post-refactor backend refresh

Backend commit `e195e8b1720157cbe1c172a30e658b6dc6be7215` was re-scanned on a disposable local SonarQube instance at `http://localhost:9002`. The analysis was processed successfully (task `34d63bc6-ff30-460e-928c-385ae056a704`) with 0 bugs, 0 vulnerabilities, 0 security hotspots, 805 code smells, 15.1% overall line coverage, 48.7% branch coverage, 6.9% duplication and 0 new violations under the configured new-violations-only gate. Six OpenCover reports imported and 48 source files had coverage; this refresh is valid evidence of execution, but it does not pass the requested full backend coverage or maintainability gates.
