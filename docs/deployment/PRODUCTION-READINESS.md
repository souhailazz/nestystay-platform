# Production readiness

## Current decision

**NO — the low-cost self-host release candidate is prepared, but production is not authorized.** M1–M5 application behavior remains locally complete. Remaining gates are credentials, client-owned infrastructure, external-provider validation, monitoring and restore evidence.

## Application gates

- [x] Cookie sessions, CSRF protection, authorization and audit paths remain enabled.
- [x] PostgreSQL migration for the email outbox applied locally.
- [x] Email queue and retry lifecycle covered by automated tests.
- [x] Admin-only integration status reports providers, email queue, Web Push flag, worker sidecar and backup destination without secrets.
- [x] Checked-SHA deployment runner applies EF migrations inside the private Compose network and performs a health check.
- [x] Production compose, private networks, health checks and restart policies added; API health probes `/api/health`.
- [x] Non-destructive deployed smoke suite covers homepage, login, listing, booking quote, badges, wellness, directories, PM/owner portals, gate QR and all health endpoints.
- [x] Local object-storage archive checksum and isolated restore rehearsal completed.
- [ ] Live Stripe and webhook rehearsal.
- [ ] Live Alibaba eKYC callback/signature rehearsal.
- [ ] Real Brevo delivery and complaint handling test.
- [ ] Client domain and `PUBLIC_APP_URL` validation, including client approval of
      the current code/token-based auth and invitation notification format. No
      production email contains a localhost link; URL-bearing templates remain
      disabled until the deployed URL and link contract are approved.
- [ ] Off-server encrypted backup and restore rehearsal.
- [ ] Domain, Cloudflare and Let's Encrypt validation.
- [ ] Monitoring alerts and incident drill.
- [ ] Run `npm run test:e2e:production` with `PRODUCTION_BASE_URL` after deployment (the authenticated check additionally needs a dedicated smoke account).

## Status separation

M1–M5 contractual functionality is not failed by these external gates. They block full production readiness only.

See [PRODUCTION-CREDENTIAL-REQUIREMENTS.md](PRODUCTION-CREDENTIAL-REQUIREMENTS.md) for the client-owned inputs and [PRODUCTION-SMOKE-SUITE.md](PRODUCTION-SMOKE-SUITE.md) for the deployment test command.
