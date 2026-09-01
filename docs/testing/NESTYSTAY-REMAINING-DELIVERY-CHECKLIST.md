# NestyStay remaining delivery checklist

Updated 2026-09-01 from the current M1–M5 checkout. The signed agreement is present and readable; the prior “SIGNED AGREEMENT MISSING” blocker is removed.

## Local acceptance

- [x] Signed agreement copied byte-for-byte into `docs/contracts/`.
- [x] M1–M4 regression retained.
- [x] M5 manager, owner, governance, document, gate and QR API paths implemented.
- [x] Provider and manager binary document upload/download paths use scoped storage and safety validation.
- [x] QR camera decode path with manual/offline fallback is available in the guard interface.
- [x] M5 API tests, frontend tests/build and responsive Playwright flows pass (96 API, 26 frontend, 18/18 targeted browser checks; full matrix 88 passed, 2 intentional skips).
- [x] Accessibility and global usability review recorded in `docs/testing/ACCESSIBILITY-REPORT.md`.
- [x] PostgreSQL migration applied to `nestystay_dev`.
- [x] Manager/owner scope, input validation, idempotency and anonymous ballot checks exercised.
- [ ] Fresh disposable database: local PostgreSQL role lacks `CREATEDB`; run with an operator that can create a database before release.

## Production acceptance

- [ ] Configure live Stripe/Connect keys and webhook endpoint, then run provider validation.
- [ ] Configure Alibaba/eKYC credentials and signed callbacks, then run provider validation.
- [ ] Configure managed Postgres, backups, storage, monitoring, alerting and deployment secrets.
- [ ] Complete independent security review and operational runbook rehearsal.
