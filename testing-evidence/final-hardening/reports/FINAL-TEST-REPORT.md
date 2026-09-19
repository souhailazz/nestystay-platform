# Final test report

| Suite | Result |
|---|---|
| `dotnet test backend/NestyStay.sln --configuration Release --no-restore` | 131 passed, 0 failed (84 API, 19 infrastructure, 23 application, 5 domain) |
| `npm test -- --run` | 35 passed, 0 failed |
| Frontend typecheck/build | Passed; production Vite build completed without errors |
| M1–M5 desktop Chromium workflow suite | 12 passed, 0 failed (includes provider analytics/replies and scheduled notices) |
| Hardening security suite | 2 passed, 0 failed |
| Quality suite | 2 passed, 0 failed, 5 intentional project skips |
| PostgreSQL migration/schema verification | Applied through `20260909112812_AddPropertyManagerWorkflowLifecycle`; 186 tables, 51 migrations, 359 indexes and 50 foreign keys |

Skipped browser cases are deliberate project-specific coverage gates; no skipped case is counted as a pass. Live Stripe wallet/webhook and Alibaba eKYC certification remain blocked until client provider credentials and domains are supplied.
