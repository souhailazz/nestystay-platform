# First server setup

1. Provision a client-owned Linux VPS with Docker Engine/Compose, automatic security updates, a non-root operator and SSH keys.
2. Point the domain's web A/AAAA record to the VPS through Cloudflare (free DNS/WAF). Keep database, Redis, MinIO and monitoring ports private.
3. Clone this repository at a release commit and create a secret-managed `.env` from the variables in `docker-compose.production.yml`.
4. Set strong unique PostgreSQL, Redis, MinIO and application secrets. Set live Stripe, Alibaba eKYC and Brevo values only in the secret manager.
5. Run `docker compose -f docker-compose.production.yml config` and review the rendered configuration for accidental secrets or public ports.
6. Run `ALLOW_INITIAL_DEPLOY=true scripts/deploy-production.sh` for a new database (or set
   `RELEASE_SHA` and run it normally for an existing install). The script validates Compose,
   waits for PostgreSQL, takes a pre-deploy dump when data exists, runs EF migrations in the
   private network, and starts `api`, `worker`, `frontend` and `caddy`.
7. Verify `/health`, login, booking, payment test mode in staging, eKYC callback handling, email delivery, QR validation and an admin audit event.
8. Schedule `scripts/backup-postgres.sh`, copy encrypted backups off-server, and complete a restore rehearsal before go-live.

Production launch remains blocked until the client supplies domain/provider credentials and a recorded staging/restore rehearsal.
