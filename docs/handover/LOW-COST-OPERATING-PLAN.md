# Low-cost operating plan

Run the web app, API, PostgreSQL, Redis and private object-storage volume on one client-owned VPS with Docker Compose. Caddy terminates TLS. Keep observability services behind the private network and enable the `observability` profile when the operator has capacity.

Use Brevo's transactional tier for application mail and Zoho for human mail. Keep SMS, premium maps, Stripe Connect and managed cloud databases disabled until usage justifies them. Maintain daily encrypted PostgreSQL backups with an off-server restic repository; local-only backups are a recovery convenience, not a production control.

The worker sidecar runs the same release image with `BackgroundJobs__Enabled=true`; the API has background jobs disabled to prevent duplicate processing. Scale to multiple API replicas only after Redis-backed distributed coordination is enabled and tested.
