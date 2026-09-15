# NestyStay M1–M4 Evidence Index

Every artifact below records the surface tested and its result. The original implementation/evidence baseline was clean-room validated at `470016e99f4d13be6ae241af199ccef2b168d3b7`; the final QR gate frontend completion is source commit `1abeb0fda57d5b090a7be424172f6a06653fa3d6`, followed by documentation-only evidence updates.

| Gate | Command | Result |
|---|---|---|
| Backend | `dotnet test backend/NestyStay.sln --no-restore` | 90 passed / 0 failed |
| Frontend | `npm test -- --run` | 25 passed / 0 failed |
| Build | `npm run build` | PASS |
| Lint | `npm run lint` | 0 errors / 165 warnings |
| API smoke | `node testing-evidence/milestones-1-4/api/m3-m4-live-api-smoke.mjs` | 15/15 |
| Browser | Playwright M1–M4 matrix plus QR gate journey | 18 passed / 0 failed |
| Concurrency | `node testing-evidence/milestones-1-4/concurrency/m3-m4-concurrency-smoke.mjs` | invariant PASS |
| Database | PostgreSQL migration/state snapshot query | PASS |

## Contract

- [Signed agreement verification](contract/signed-agreement-verification.txt)
- [Repository signed agreement](../../docs/contracts/NestyStay-Signed-Agreement-April-2026.pdf)
- [Final traceability](../../docs/testing/M1-M4-TRACEABILITY.md)
- [Final acceptance checklist](../../docs/testing/M1-M4-ACCEPTANCE-CHECKLIST.md)

## Automated and live validation

- [Domain test result](backend/m1-m4-final-domain.trx)
- [Application test result](backend/m1-m4-final-application.trx)
- [Infrastructure test result](backend/m1-m4-final-infrastructure.trx)
- [API test result](backend/m1-m4-final-api.trx)
- [Live API smoke (15/15)](api/m3-m4-live-api-smoke.json)
- [Assignment concurrency invariant](concurrency/m3-m4-concurrency-smoke.json)
- [PostgreSQL state snapshot](database/m3-m4-postgres-state.txt)
- [Route inventory report](reports/ROUTE-INVENTORY-REPORT.md)
- [Security validation](security/SECURITY-VALIDATION.md)

## Browser evidence

- [`m3-m4-acceptance.spec.ts`](../../frontend/e2e/m3-m4-acceptance.spec.ts)
- [`m3-wellness-lifecycle.spec.ts`](../../frontend/e2e/m3-wellness-lifecycle.spec.ts)
- [`final-contract-validation.spec.ts`](../../frontend/e2e/final-contract-validation.spec.ts)
- [`m1-m4-route-inventory.spec.ts`](../../frontend/e2e/m1-m4-route-inventory.spec.ts)
- [`m4-qr-gate.spec.ts`](../../frontend/e2e/m4-qr-gate.spec.ts) — issue, scan, wrong-property, revoke and invalid UI journey
- [Route inventory JSON](browser/route-inventory.json)
- [Wellness lifecycle screenshots](wellness/)
- [Directory/officer/provider/wellness screenshots](screenshots/)
