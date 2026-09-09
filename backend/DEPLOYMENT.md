# NestyStay M1–M2 deployment guide

This repository is the backend release for the contractual Milestones 1 and 2
(Core Booking and Badges). It is an ASP.NET Core 10 API with EF Core migrations.
The repository also contains shared application modules used by the existing
platform; the production deployment scope in this guide is M1–M2.

## What the client receives

- Registration, login, logout, 2FA/recovery and signed cookie sessions.
- Host properties, booking quotes, PENDING/APPROVED/REJECTED booking states,
  date holds and overlap protection.
- Application-level Stripe authorization/capture/refund boundaries.
- Guest eKYC application flow and verification state handling.
- FREE, VERIFIED, TRUSTED and WELLNESS badges, pricebook, eligibility and
  server-side feature restrictions.
- Annual renewal/review records and owner/admin badge dashboard APIs.
- PostgreSQL migrations, API tests and a production Docker image.

Live Stripe, Alibaba eKYC, email/SMS/push and payout certification require the
client's provider accounts and are intentionally configured as deployment-time
secrets. Do not paste them into GitHub, source files, Dockerfiles or chat.

## Server prerequisites

- Linux VPS or managed container host with Docker Engine 24+ and Compose v2.
- DNS and TLS for `api.example.com` and `app.example.com` (the frontend guide
  is in the frontend repository).
- A firewall that exposes only 22/tcp, 80/tcp and 443/tcp. PostgreSQL, Redis
  and MinIO must stay on the private Docker network.
- A GitHub deploy key with access to the server and a GitHub Container Registry
  read-only token (`read:packages`).
- A managed backup or scheduled PostgreSQL dump/restore process.

## First-time server setup

Create a private deployment directory and copy the compose file from this
repository to it:

```bash
sudo mkdir -p /opt/nestystay/backend
sudo chown "$USER":"$USER" /opt/nestystay/backend
cp deploy/docker-compose.yml /opt/nestystay/backend/docker-compose.yml
chmod 700 /opt/nestystay/backend
```

Create `/opt/nestystay/backend/.env` from `.env.production.example`. The file
must contain the database, session, CORS, provider and storage values. At
minimum, set:

```env
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__Postgres=Host=postgres;Port=5432;Database=nesty_prod;Username=nesty_app;Password=<strong-password>
ConnectionStrings__Redis=redis:6379,password=<redis-password>
POSTGRES_DB=nesty_prod
POSTGRES_USER=nesty_app
POSTGRES_PASSWORD=<same-strong-password>
REDIS_PASSWORD=<redis-password>
MINIO_ROOT_USER=<minio-admin-user>
MINIO_ROOT_PASSWORD=<minio-admin-password>
NESTYSTAY_CORS_ALLOWED_ORIGINS=https://app.example.com
NESTYSTAY_SESSION_COOKIE_DOMAIN=.example.com
NESTYSTAY_SESSION_COOKIE_SAMESITE=Lax
PUBLIC_APP_URL=https://app.example.com
NESTYSTAY_SESSION_TOKEN_SECRET=<at-least-32-random-bytes>
NESTYSTAY_TOTP_SECRET_PROTECTION_KEY=<different-at-least-32-random-bytes>
NESTYSTAY_WEBHOOK_SHARED_SECRET=<random-webhook-secret>
```

Use the real Stripe/Alibaba/Brevo values only when the client has completed
provider onboarding. Keep local/test adapters enabled until live certification
is approved. The API applies additive EF migrations during startup; take a
backup before the first production upgrade.

## Manual first deployment

```bash
cd /opt/nestystay/backend
printf '%s' '<read-packages-token>' | docker login ghcr.io --username '<github-user>' --password-stdin
BACKEND_IMAGE=ghcr.io/nestystayjamaica/nesty-stay-backend:<commit-sha> \
  docker compose pull backend
BACKEND_IMAGE=ghcr.io/nestystayjamaica/nesty-stay-backend:<commit-sha> \
  docker compose up -d backend
curl --fail https://api.example.com/api/health/ready
```

The API runs migrations before serving requests. If a migration fails, stop
the release, restore the database backup if necessary, inspect the API logs and
do not delete migration history.

## GitHub Actions automatic deployment

The checked-in `.github/workflows/ci-cd.yml` runs on pull requests and on every
push to `main`. A push is deployed only after restore, build and the complete
backend test suite pass. It publishes an immutable SHA-tagged image to GHCR,
copies the compose file, pulls that exact image over SSH and checks
`/api/health/ready`.

Create a GitHub `production` environment and add these **Actions secrets**:

| Secret | Value |
|---|---|
| `DEPLOY_HOST` | Server DNS name or IP |
| `DEPLOY_PORT` | SSH port, normally `22` |
| `DEPLOY_USER` | Non-root deployment user in the `docker` group |
| `DEPLOY_SSH_KEY` | Private key matching the server's `authorized_keys` |
| `DEPLOY_PATH` | `/opt/nestystay/backend` |
| `GHCR_USERNAME` | GitHub account allowed to read the package |
| `GHCR_TOKEN` | Fine-grained token with `read:packages` only |
| `BACKEND_HEALTHCHECK_URL` | `https://api.example.com` |

Do not put database or provider secrets in GitHub Actions. Keep them in the
server-only `.env`/secret manager. Protect `main` with required pull-request
checks and require approval for the `production` environment.

## Verification and rollback

```bash
docker compose ps
docker compose logs --tail=200 backend
curl --fail https://api.example.com/api/health
curl --fail https://api.example.com/api/health/ready
```

To roll back, choose a previous immutable image SHA and run:

```bash
BACKEND_IMAGE=ghcr.io/nestystayjamaica/nesty-stay-backend:<known-good-sha> \
  docker compose up -d --no-deps backend
```

Never roll back by deleting PostgreSQL data or migration rows. After deployment,
the frontend repository's smoke test should complete registration/login, 2FA,
property listing, booking state and badge-gating checks.

## Claude handoff

Tell Claude to read this file and `README.md` before changing anything. It must
preserve API paths and response shapes, never invent production environment
variables, never commit secrets/logs/artifacts, run `dotnet build` and
`dotnet test`, and wait for client approval before changing provider mode.
