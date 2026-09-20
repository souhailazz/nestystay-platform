# Zero-credential production preparation

This repository contains the complete local implementation and deployment wiring for the NestyStay M1–M5 release candidate. It does not contain client-owned provider credentials, domains, VPS access, or a remote backup destination.

## Completed locally

- Server-local storage is a private `IStorageProvider` implementation with atomic writes, HMAC-signed API downloads, MIME/magic-byte/size/hash validation, safe object keys, and local round-trip/path/signature tests.
- Transactional email is queued in PostgreSQL and rendered with HTML/plain-text buttons and fallback URLs for verification, password reset, and owner invitations. Links use `PUBLIC_APP_URL` and single-use hashed tokens.
- Admin integration health reports PostgreSQL, Redis, private storage, worker, Brevo, Stripe, Stripe Identity, backups, monitoring, Web Push, and email backlog without exposing secrets.
- Prometheus alert rules, Grafana dashboards, Loki configuration, Uptime Kuma monitor definitions, backup status/checksum scripts, and a restore runbook are checked into `deploy/` and `scripts/`.
- Compose configuration, health checks, private networks, restart policies, image builds, frontend routes, backend tests, security tests, and real browser email-link journeys have been exercised locally.

## Still requires client access

- VPS/domain/DNS/TLS and an approved `PUBLIC_APP_URL`.
- PostgreSQL, private storage volume/ACLs, storage signing secret, and encrypted secret storage on the deployment host.
- Brevo API key, verified sender/domain and reply-to mailbox.
- Live Stripe payment/Identity keys and webhook endpoint configuration.
- Approved off-server Restic repository and restore operator/sign-off.
- Monitoring/Uptime Kuma notification destinations and incident owner.

No provider is marked production-ready until its real endpoint is exercised with client-owned credentials. No secret values belong in Git, browser bundles, logs, screenshots, or this documentation.
