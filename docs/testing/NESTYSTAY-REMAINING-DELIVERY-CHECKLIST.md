# NestyStay remaining delivery checklist

Updated 2026-09-09 from the current M1–M5 checkout. The signed agreement is present and readable; the prior “SIGNED AGREEMENT MISSING” blocker is removed.

## Local acceptance

- [x] Signed agreement copied byte-for-byte into `docs/contracts/`.
- [x] M1–M4 regression retained.
- [x] M5 manager, owner, governance, document, gate and QR API paths implemented.
- [x] M5 subscription lifecycle persists plan limits, billing history, retry/provider state, pause/resume, auto-renew, scheduled downgrade and cancellation/reactivation.
- [x] Provider and manager binary document upload/download paths use scoped storage and safety validation.
- [x] QR camera decode path with manual/offline fallback is available in the guard interface.
- [x] M5 API tests, frontend tests/build and responsive Playwright flows pass (135 backend, 35 frontend; configured Playwright matrix 91 passed, 0 failed, 52 intentional credential/provider-gated or viewport-scoped skips across 143 tests; PM lifecycle passed desktop/tablet/mobile; Firefox/WebKit critical smoke passed).
- [x] Accessibility and global usability review recorded in `docs/testing/ACCESSIBILITY-REPORT.md`.
- [x] PostgreSQL migration applied to `nestystay_dev`.
- [x] Manager/owner scope, input validation, idempotency and anonymous ballot checks exercised.
- [x] M1 auth/session/passkey, calendar worker, availability, recommendations and host payout persistence migrations applied and covered by focused tests.
- [ ] Fresh disposable database: local PostgreSQL role lacks `CREATEDB`; run with an operator that can create a database before release.

## Production acceptance

- [ ] Configure live Stripe/Connect keys and webhook endpoint, then run provider validation.
- [ ] Configure Alibaba/eKYC credentials and signed callbacks, then run provider validation.
- [ ] Configure external SMS, Web Push and production email credentials/DNS and run delivery validation.
- [x] Complete local asynchronous document ZIP export and expiry-reminder worker; production object-storage read/retention certification remains separate.
- [ ] Configure managed Postgres, backups, storage, monitoring, alerting and deployment secrets.
- [ ] Complete independent security review and operational runbook rehearsal.
