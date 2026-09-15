# NestyStay final hardening report

Baseline: `95c14a8b30c017e968b53cedc9165f83c53b9006`
Resumed hardening SHA: `35794c8e8718d3c2f8a76e124a477d9a76f13427`

> **Current authoritative result:** Read [FINAL-HARDENING-M5-ADDENDUM.md](FINAL-HARDENING-M5-ADDENDUM.md) for the completed M1–M5 decision, current test totals, cookie/CSRF boundary, M5 browser journeys, and PostgreSQL relationship validation.

The final hardening pass was resumed from the current worktree. Existing user changes and generated visual evidence were preserved. The implementation now has named API rate limits, production-safe security headers, generic payment-provider failures, lazy-loaded frontend route bundles, a focus-managed accessible modal, contrast fixes, evidence-backed query indexes, deterministic seeded parent identities for legacy properties/bookings, and a race-safe host property editor.

Local release-candidate decision: **YES** for contractual M1–M5, subject to the explicit production-only blockers below. Production readiness: **NO**.

## Verification result

- Backend baseline: 97 passed, 0 failed across Domain, Application, Infrastructure, and API suites. The current resumed pass is 101 passed, 0 failed (including cookie-session and cookie-2FA coverage).
- Frontend: clean `npm ci`, lint, typecheck, 26 unit tests, full-source coverage, and production build all passed.
- Browser baseline: original regression 31 passed/8 skipped. Current hardening matrix: 70 planned, 22 passed, 0 failed, 48 intentional project skips; M5 is 7/7 across configured browser/viewport projects.
- Security baseline: 37/37 authorization cases. Current authorization matrix is 54/54, dynamic checks 7/7, dependency audits clean, and no live credential/private-key material found (the `sk_live_`/`pk_live_`/`whsec_` matches are sample strings in validation/tests/docs).
- Database baseline: 0 integrity violations and 0 FKs. Current database remains at 145 tables, has 45 reviewed FKs, and has 0 enforced M5 orphans via migrations `20260902125555_FinalHardeningM5Relationships` and `20260902125706_FixMaintenanceVendorForeignKey`.
- Financial/concurrency: 16/16 monetary-rule assertions and 4/4 parallel API assertions passed.

## Remaining findings

1. The local browser session boundary is now HttpOnly cookie + CSRF protected; production still requires deployment of Secure cookies behind TLS and a shared session/revocation strategy.
2. The load harness demonstrates process-local rate limiting and intentionally records 429 responses at the protective threshold; a multi-instance deployment needs a shared limiter (for example Redis) and distributed coordination.
3. Real Stripe live/Connect, Alibaba Cloud eKYC, email/SMS/push, and payout-provider calls were not executed because external credentials/providers are outside the local environment.

## Production-only blockers

Live provider credentials and webhook validation, managed PostgreSQL/object storage, TLS/WAF/edge configuration, centralized secrets, distributed rate limiting/locks, backups/restore drills, observability/alerting, privacy/retention review, and external penetration testing remain required before a production-ready declaration.

Evidence is indexed in `testing-evidence/final-hardening/EVIDENCE-INDEX.md`; machine-readable values are in `reports/FINAL-METRICS.json`.
