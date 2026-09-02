# NestyStay operations runbook

- Check Caddy, API, worker, PostgreSQL and Redis health before investigating user reports.
- Inspect `notification_queue_item` delivery status; replay only `RETRYING` items after confirming the provider response. Escalate `DEAD_LETTER` items with the recorded error and correlation id.
- Rotate application/provider secrets through the secret manager, then restart only the affected service.
- For payment or eKYC incidents, preserve provider event ids and signatures; never manually mark a booking paid or verified without an auditable event.
- For QR incidents, revoke the code, record reason and issue a replacement; gate staff must use the explicit invalid/expired/revoked result.
- Review backup age, disk usage, TLS expiry, rate-limit errors and failed health checks daily.
- Run a restore rehearsal at least quarterly and after schema changes.
