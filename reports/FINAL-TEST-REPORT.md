# Final test report

| Suite | Result |
|---|---|
| `dotnet test backend/NestyStay.sln` | 97 passed, 0 failed |
| `npm test` | 26 passed, 0 failed |
| Frontend lint/typecheck/build | Passed; lint has warnings only, no errors; build has no chunk warning |
| Original Playwright desktop suite | 31 passed, 0 failed, 8 skipped |
| Final hardening Playwright matrix | 15 passed, 0 failed, 48 intentional skips, 0 flaky |
| Authorization/security browser harness | 37/37 and 7/7 |
| Financial correctness harness | 16/16 |
| API concurrency harness | 4/4 |
| PostgreSQL integrity audit | 0 violations |

Skipped browser cases are deliberate: route/axe/performance/security checks are laptop-only where specified, responsive checks run on the five Chromium viewport projects, and cross-browser smoke is limited to stable representative journeys. No skipped case is counted as a pass.
