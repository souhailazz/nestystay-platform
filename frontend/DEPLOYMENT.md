# NestyStay M1–M2 frontend deployment guide

This repository is the browser release for the contractual Milestones 1 and 2
(Core Booking and Badges). It is a Vite + React + TypeScript SPA and is served
from an Nginx container.

## Server prerequisites

- Docker Engine 24+ and Compose v2 on the client server.
- DNS/TLS for `staging.nestystay.net`.
- The backend deployed behind the same-origin root Caddy proxy. The proxy must
  forward `https://staging.nestystay.net/api/*` to the ASP.NET API.
- A GitHub Container Registry read-only token and SSH deployment key.

## Build variables

Vite values are embedded into the browser bundle at build time. They are not
backend secrets:

```env
VITE_API_BASE_URL=/api
VITE_STRIPE_PUBLIC_KEY=pk_live_or_test_public_key
VITE_GOOGLE_CLIENT_ID=optional-google-client-id
```

Never put a Stripe secret key, webhook secret, database password, session secret
or identity-provider credential in this repository or any `VITE_*` variable.

## First-time server setup

```bash
sudo mkdir -p /opt/nestystay/frontend
sudo chown "$USER":"$USER" /opt/nestystay/frontend
cp deploy/docker-compose.yml /opt/nestystay/frontend/docker-compose.yml
chmod 700 /opt/nestystay/frontend
```

The standalone compose file binds the frontend to `127.0.0.1:8081` and is
frontend-only. For staging, use the root Compose stack so Caddy can serve this
frontend and route `/api/*` to the backend. Do not expose a frontend-only site
as `staging.nestystay.net`.

## Manual first deployment

```bash
cd /opt/nestystay/frontend
printf '%s' '<read-packages-token>' | docker login ghcr.io --username '<github-user>' --password-stdin
FRONTEND_IMAGE=ghcr.io/nestystayjamaica/nesty-stay-frontend:<commit-sha> \
  docker compose pull frontend
FRONTEND_IMAGE=ghcr.io/nestystayjamaica/nesty-stay-frontend:<commit-sha> \
  docker compose up -d frontend
curl --fail http://127.0.0.1:8081/
```

## GitHub Actions automatic deployment

The checked-in `.github/workflows/ci-cd.yml` runs typecheck, unit tests and a
production build on pull requests. Every push to `main` that passes those
checks builds an immutable SHA-tagged image, pushes it to GHCR, copies the
compose file over SSH, deploys that exact image and optionally checks the public
frontend URL.

Create a GitHub `production` environment and add these **Actions variables**:

| Variable | Value |
|---|---|
| `NESTYSTAY_VITE_API_BASE_URL` | `/api` |
| `VITE_STRIPE_PUBLIC_KEY` | Client-owned `pk_test_...` or `pk_live_...` |
| `VITE_GOOGLE_CLIENT_ID` | Optional public Google client ID |

Add these **Actions secrets**:

| Secret | Value |
|---|---|
| `DEPLOY_HOST` | Server DNS name or IP |
| `DEPLOY_PORT` | SSH port, normally `22` |
| `DEPLOY_USER` | Non-root deployment user in the `docker` group |
| `DEPLOY_SSH_KEY` | Private key matching the server's `authorized_keys` |
| `DEPLOY_PATH` | `/opt/nestystay/frontend` |
| `GHCR_USERNAME` | GitHub account allowed to read the package |
| `GHCR_TOKEN` | Fine-grained token with `read:packages` only |
| `NESTYSTAY_PUBLIC_URL` | `https://staging.nestystay.net` |

Protect `main`, require the workflow checks before merge and require approval
for the `production` environment. Do not store backend secrets here.

## Verification and rollback

```bash
docker compose ps
docker compose logs --tail=100 frontend
curl --fail https://staging.nestystay.net/
curl --fail https://staging.nestystay.net/api/properties
```

The SPA must load directly at `/login`, `/host-dashboard`, `/guest-dashboard`
and badge-gated routes through the reverse proxy. Nginx is configured to fall
back to `index.html` for client-side routes. Roll back with a known-good image:

```bash
FRONTEND_IMAGE=ghcr.io/nestystayjamaica/nesty-stay-frontend:<known-good-sha> \
  docker compose up -d --no-deps frontend
```

## Validation commands

```bash
npm ci
npm run typecheck
npm test -- --run
npm run build
```

Run the production smoke suite only with an approved test account and keep its
credentials in the client secret manager.

## Claude handoff

Tell Claude to read this file and `README.md` first. It must preserve API paths,
never invent backend responses, never commit secrets/logs/build artifacts, and
run typecheck, tests and build after every change.
