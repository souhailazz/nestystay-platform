# Backend post-refactor SonarQube evidence — 2026-09-27

This is a fresh local, server-processed SonarQube analysis of backend commit `e195e8b1720157cbe1c172a30e658b6dc6be7215` on the disposable local SonarQube instance at `http://localhost:9002`.

| Measure | Result |
|---|---:|
| Analysis task | `34d63bc6-ff30-460e-928c-385ae056a704` |
| Analysis status | `SUCCESS` |
| Quality Gate | `OK` for the configured new-violations-only condition |
| New violations | `0` |
| Bugs | `0` |
| Vulnerabilities | `0` |
| Security hotspots | `0` |
| Code smells | `805` |
| Overall line coverage | `15.1%` |
| Branch coverage | `48.7%` |
| Duplication | `6.9%` |

Coverage import was real and server-side: 6 OpenCover reports were imported and Sonar reported 48 main source files with coverage. The report is not a production coverage pass because the current imported reports do not cover the full backend source tree. The earlier complete backend evidence remains separately recorded at the prior audited SHA; this run specifically proves the post-refactor source was analyzed and uploaded.

The 995 open maintainability findings are not silently treated as resolved: 61 critical, 382 major, 239 minor and 313 informational findings remain in this project scope. No scanner token or credential is stored in this evidence.
