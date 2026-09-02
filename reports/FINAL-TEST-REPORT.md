# Final test report

| Suite | Result |
|---|---|
| `dotnet test backend/NestyStay.sln` | 101 passed, 0 failed |
| `npm test` | 30 passed, 0 failed |
| Frontend lint/typecheck/build | Passed; lint has warnings only, no errors; build has no chunk warning |
| Original Playwright desktop suite | 31 passed, 0 failed, 8 skipped |
| Final hardening Playwright matrix | 22 passed, 0 failed, 48 intentional skips (70 planned), 0 flaky |
| Authorization/security browser harness | 54/54 and 7/7 |
| Financial correctness harness | 16/16 |
| API concurrency harness | 4/4 |
| PostgreSQL integrity/FK audit | 0 violations; 45 reviewed FKs, 0 enforced M5 orphans |

Skipped browser cases are deliberate: route/axe/performance/security checks are laptop-only where specified, responsive checks run on the five Chromium viewport projects, and cross-browser smoke is limited to stable representative journeys. No skipped case is counted as a pass.
