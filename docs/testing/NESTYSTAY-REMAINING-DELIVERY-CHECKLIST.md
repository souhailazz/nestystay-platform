# NestyStay remaining delivery checklist

Updated 2026-08-31 from the current M1–M4 checkout. The signed agreement is present and readable; the prior “SIGNED AGREEMENT MISSING” blocker is removed.

## Local acceptance

- [x] Signed agreement copied byte-for-byte into `docs/contracts/`.
- [x] M1–M4 regression retained.
- [x] M5 manager, owner, governance, document, gate and QR API paths implemented.
- [x] M5 API tests, frontend tests/build and responsive Playwright flows pass.
- [x] PostgreSQL migration applied to `nestystay_dev`.
- [x] Manager/owner scope, input validation, idempotency and anonymous ballot checks exercised.
- [ ] Fresh disposable database: local PostgreSQL role lacks `CREATEDB`; run with an operator that can create a database before release.

## Production acceptance

- [ ] Configure live Stripe/Connect keys and webhook endpoint, then run provider validation.
- [ ] Configure Alibaba/eKYC credentials and signed callbacks, then run provider validation.
- [ ] Configure managed Postgres, backups, storage, monitoring, alerting and deployment secrets.
- [ ] Complete independent security review and operational runbook rehearsal.
