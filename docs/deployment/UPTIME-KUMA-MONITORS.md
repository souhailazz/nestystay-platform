# Uptime Kuma monitor plan

Uptime Kuma runs on the private app network in the production Compose profile.
Create HTTP monitors after the public domain exists:

| Monitor | URL | Expected |
| --- | --- | --- |
| Frontend liveness | https://<PUBLIC_DOMAIN>/health/live | HTTP 200 |
| Frontend readiness | https://<PUBLIC_DOMAIN>/health/ready | HTTP 200 |
| API liveness | https://<PUBLIC_DOMAIN>/api/health/live | HTTP 200 |
| API readiness | https://<PUBLIC_DOMAIN>/api/health/ready | HTTP 200 |
| Gate page (optional) | https://<PUBLIC_DOMAIN>/gate/qr | HTTP 200 |

Do not configure authenticated monitors or place credentials in Kuma. Use a
client-owned notification channel and document the escalation owner during
handover. The public frontend health aliases are served by Caddy and must be
kept free of private data.
