# NestyStay final hardening report

Baseline: `95c14a8b30c017e968b53cedc9165f83c53b9006`
Resumed hardening SHA: `35794c8e8718d3c2f8a76e124a477d9a76f13427`

Deployment continuation (2026-09-02): the low-cost production implementation is recorded in
`LOW-COST-PRODUCTION-IMPLEMENTATION.md` and its final commit SHA is reported in the handover response.

Zero-credential production preparation (2026-09-02): the MinIO `IStorageProvider`,
`PUBLIC_APP_URL` transactional-email links, protected admin integration matrix,
observability profile, structured three-class backup verification, and real browser
email-link journeys are implemented and verified. See
`../testing-evidence/final-hardening/reports/ZERO-CREDENTIAL-PRODUCTION-PREP-ADDENDUM.md`.

Pre-deployment verification (2026-09-02): `cf1f4fd381396d0a4ae171be3fb464e5358d925c` adds the
client credential inventory, a configurable non-destructive production smoke suite, the
`PUBLIC_APP_URL` Compose wiring, and the corrected API container `/api/health` probe. Compose
and observability profiles rendered successfully, all release images built, Caddy validated,
and an isolated object-storage archive restore matched its source checksum. The smoke suite is
ready but cannot be executed against a public deployment until the client supplies the URL.

> **Current authoritative result:** [M1–M5 final hardening addendum](../testing-evidence/final-hardening/reports/FINAL-HARDENING-M5-ADDENDUM.md) supersedes historical baseline counts below and records the completed M5, cookie/CSRF, authorization, and PostgreSQL validation.

The final hardening pass was resumed from the current worktree. Existing user changes and generated visual evidence were preserved. The implementation now has named API rate limits, production-safe security headers, generic payment-provider failures, lazy-loaded frontend route bundles, a focus-managed accessible modal, contrast fixes, evidence-backed query indexes, deterministic seeded parent identities for legacy properties/bookings, and a race-safe host property editor.

Local release-candidate decision: **YES** for contractual M1–M5 and the self-host deployment candidate, subject to the explicit production-only blockers below. Production readiness: **NO**.

## Verification result

- Backend: 111 passed, 0 failed across Domain, Application, Infrastructure, and API suites (including MinIO, email outbox, owner-invitation token, and production-email configuration tests).
- Frontend: clean `npm ci`, lint, typecheck, 30 unit tests, full-source coverage, and production build all passed.
- Browser: original regression 31 passed/8 skipped; current hardening matrix 84 tests with 24 passed, 0 failed, and 60 intentional project skips; route coverage 128/128, the real MinIO UI upload/download path passed, and Firefox/WebKit smoke passed.
- Security: 54/54 authorization cases, 7/7 dynamic security checks, dependency audits clean, and no live credential/private-key material found (the `sk_live_`/`pk_live_`/`whsec_` matches are sample strings in validation/tests/docs).
- Database: 0 integrity violations after seeding the three legacy host parent rows; reviewed relationship/index, email outbox, and self-hosted provider migrations applied locally.
- Financial/concurrency: 16/16 monetary-rule assertions and 4/4 parallel API assertions passed.

## Remaining findings

1. Real Stripe live/Connect, Alibaba Cloud eKYC and Brevo delivery calls were not executed because external credentials/providers are outside the local environment.
2. MinIO application integration and a real disposable MinIO round-trip pass; the client-owned production endpoint, credentials, TLS choice, and bucket remain deployment inputs.
3. Multi-instance deployment still requires Redis-backed distributed coordination and an operator-approved worker topology.
4. Off-server encrypted backup destination, monitoring alert ownership/incident drill, and domain/TLS validation remain open.

## Production-only blockers

Live provider credentials and webhook validation, client-owned PostgreSQL/object storage host, TLS/WAF/edge configuration, centralized secrets, distributed rate limiting/locks, off-server backups/restore drills, observability/alerting, privacy/retention review, and external penetration testing remain required before a production-ready declaration.

Evidence is indexed in `testing-evidence/final-hardening/EVIDENCE-INDEX.md`; machine-readable values are in `reports/FINAL-METRICS.json`.
