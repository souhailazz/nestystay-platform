# NestyStay full-system checklist

| Layer | Result | Evidence |
|---|---|---|
| Contract source | PASS | Signed PDF present, readable and byte-identical copy |
| M1–M4 regression | PASS locally | Existing milestone suites and 19-route browser inventory |
| M5 backend/API | PASS | 3 focused API tests; live PostgreSQL smoke |
| Frontend | PASS | TypeScript/Vite build; 25 Vitest tests |
| Real browser | PASS | 6 PM/owner/QR tests across desktop/tablet/mobile |
| PostgreSQL migration | PASS on `nestystay_dev` | Phase 5 migration applied and tables queried |
| Authorization/security | PASS locally | Owner isolation, validation, input and anonymous-ballot checks |
| Concurrency | PASS locally | Same-key concurrent payment produced 200/200, one payment row, correct balance |
| Fresh database | BLOCKED | `nestystay` PostgreSQL role lacks `CREATEDB`; operator action required |
| External providers | BLOCKED | Live Stripe and Alibaba credentials not present |
| Production readiness | NO | Deployment controls remain outstanding |
