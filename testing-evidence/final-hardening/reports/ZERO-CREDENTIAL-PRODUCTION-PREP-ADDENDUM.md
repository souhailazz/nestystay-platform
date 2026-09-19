# Zero-credential production preparation addendum

Generated 2026-09-02 from the clean pre-deployment continuation at `e6acef631eb7e096f23e7ce524a2d9a946ed740e`.

## Local verification

- Backend: **111 passed / 0 failed** (Domain 5, Application 23, Infrastructure 19, API 64).
- Frontend: **30 passed / 0 failed**; lint 0 errors (existing warnings only), typecheck, production build, and npm audit (0 high vulnerabilities) pass.
- Browser hardening: **84 tests, 24 passed / 60 intentional project skips / 0 unexpected**, with route coverage **128/128** and dynamic security **7/7**.
- Transactional email: local `.eml` capture and real Playwright completion passed for verification, owner invitation, and password reset; links are `PUBLIC_APP_URL`-based and single-use.
- MinIO: real disposable MinIO upload/overwrite/download/MIME/hash/unauthorized-access test **2/2 passed**, plus a real browser profile-photo upload/reload/signed-download flow **PASS**; the production endpoint remains client-owned and unconfigured here.
- Compose: default and observability profiles parse; frontend/API/worker images build; all services have restart policies and healthchecks, with MinIO/DB/Redis/monitoring volumes private to the app network.
- Monitoring: Prometheus config and 12 starter rules validated with `promtool`; Grafana dashboard JSON and Loki/Uptime Kuma files parse.
- Backups: PostgreSQL dump, object archive, configuration archive, structured status JSON, checksum and three-class freshness verification passed in isolated fixtures; off-server destination remains blocked.

## Provider and ownership boundary

Application flags are centralized (`EMAIL_PROVIDER=brevo`, `BUSINESS_MAIL_PROVIDER=zoho`, `EKYC_PROVIDER=alibaba`, `PAYMENT_PROVIDER=stripe`, `PAYOUT_MODE=manual`, `OBJECT_STORAGE_PROVIDER=minio`, Web Push/SMS disabled). Brevo, Stripe, Alibaba Cloud, domain/VPS, monitoring destinations, and off-server backup are not called or claimed without client credentials. Alibaba Mail is not a runtime dependency.

See `docs/deployment/WHAT-I-CAN-DO-WITHOUT-CLIENT-CREDENTIALS.md`, `ACCOUNT-SETUP-GUIDE.md`, and `OWNERSHIP-HANDOVER.md` for the exact handover boundary.
