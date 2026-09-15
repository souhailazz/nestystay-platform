# NestyStay staging certification

Target: `https://staging.nestystay.net`  
Architecture: nginx reverse proxy, direct ASP.NET backend, systemd service. Docker, Docker Compose and Caddy are not assumed for this staging host.

## Required recording fields

| Field | Value |
|---|---|
| Frontend SHA deployed | PENDING |
| Backend SHA deployed | PENDING |
| Date/time/timezone | PENDING |
| Tester/accounts | Dedicated demo accounts only; record role names, never passwords |
| Database/seed | Dedicated staging data; no production data |

## Preflight

- [ ] `https://staging.nestystay.net/` loads with no broken assets.
- [ ] `/api/health` returns 200.
- [ ] `/api/health/live` returns 200.
- [ ] `/api/health/ready` returns 200.
- [ ] `/api/properties` returns 200 and real seeded data.
- [ ] API proxy preserves the `/api` prefix.
- [ ] systemd service is active, restarts after failure and starts after reboot.
- [ ] nginx access/error logs contain no unresolved API proxy errors.

## Role journeys

- [ ] M1 guest booking approval journey.
- [ ] M1 guest booking rejection with persisted reason.
- [ ] M1 Stripe Identity processing/verified/requires-input/canceled/failed.
- [ ] M1 Stripe payment/capture/refund/receipt/trip/notification.
- [ ] M2 all four badge states, purchase/lifecycle/admin.
- [ ] M3 officer → host → admin → report → payout state.
- [ ] M4 provider onboarding → moderation → public directory and QR/gate.
- [ ] M5 manager → owner → finance/maintenance/documents/governance/gate.

## Provider and security evidence

- [ ] Stripe test/live mode documented and webhook signatures verified.
- [ ] Stripe Identity events signed, correlated and replay-safe.
- [ ] Brevo delivery, complaint/failure/retry evidence recorded.
- [ ] Storage private-access and restart evidence recorded.
- [ ] No secrets, tokens or private identity documents appear in screenshots/logs.

Current result: **NOT COMPLETE — no independent full staging certification was performed in this task.**
