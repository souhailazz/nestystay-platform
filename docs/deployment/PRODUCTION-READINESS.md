# Production readiness

## Current decision

**NO — the low-cost self-host release candidate is prepared, but production is not authorized.** M1–M5 application behavior remains locally complete. Remaining gates are credentials, client-owned infrastructure, external-provider validation, monitoring and restore evidence.

## Application gates

- [x] Cookie sessions, CSRF protection, authorization and audit paths remain enabled.
- [x] PostgreSQL migration for the email outbox applied locally.
- [x] Email queue and retry lifecycle covered by automated tests.
- [x] Admin-only integration status reports providers, email queue, Web Push flag, worker sidecar and backup destination without secrets.
- [x] Checked-SHA deployment runner applies EF migrations inside the private Compose network and performs a health check.
- [x] Production compose, private networks, health checks and restart policies added.
- [ ] Live Stripe and webhook rehearsal.
- [ ] Live Alibaba eKYC callback/signature rehearsal.
- [ ] Real Brevo delivery and complaint handling test.
- [ ] Off-server encrypted backup and restore rehearsal.
- [ ] Domain, Cloudflare and Let's Encrypt validation.
- [ ] Monitoring alerts and incident drill.

## Status separation

M1–M5 contractual functionality is not failed by these external gates. They block full production readiness only.
