# NestyStay operations runbook

- Check Caddy, API, worker, PostgreSQL and Redis health before investigating user reports.
- Deploy a checked-out release with `RELEASE_SHA=<commit> scripts/deploy-production.sh`;
  use `ALLOW_INITIAL_DEPLOY=true` only for a new empty database. The runner validates the
  rendered Compose file, creates a pre-deploy dump, applies EF migrations privately and
  performs an HTTP health check.
- Inspect `notification_queue_item` delivery status; replay only `RETRYING` items after confirming the provider response. Escalate `DEAD_LETTER` items with the recorded error and correlation id.
- Rotate application/provider secrets through the secret manager, then restart only the affected service.
- For payment or eKYC incidents, preserve provider event ids and signatures; never manually mark a booking paid or verified without an auditable event.
- For QR incidents, revoke the code, record reason and issue a replacement; gate staff must use the explicit invalid/expired/revoked result.
- Review backup age, disk usage, TLS expiry, rate-limit errors and failed health checks daily.
- Run a restore rehearsal at least quarterly and after schema changes.

## Health and uptime monitors

Keep Uptime Kuma on the private network and configure public, unauthenticated
HTTP monitors for:

- `https://<production-domain>/` (frontend)
- `https://<production-domain>/api/health/live` (API liveness)
- `https://<production-domain>/api/health/ready` (PostgreSQL readiness)

Do not monitor authenticated admin endpoints or place credentials in monitor
URLs. Prometheus/Grafana should consume application and container metrics only
after retention and disk alerts are configured.
