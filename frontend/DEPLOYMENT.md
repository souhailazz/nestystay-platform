# NestyStay frontend deployment guide

This repository is the Vite + React + TypeScript SPA. The client staging
server serves the built static bundle directly from nginx; it does not use
Docker or Docker Compose. Nginx also forwards `/api/*` to the ASP.NET backend
on the same `https://staging.nestystay.net` origin.

## One-time server layout

The deployment user must own the release directory:

```text
/var/www/nestystay/
  current -> releases/<sha>    # nginx document root
  releases/<sha>/              # extracted Vite dist files
```

Nginx should use `/var/www/nestystay/current` as its document root and include
SPA fallback routing to `index.html`. It should proxy `/api/`, `/openapi/` and
`/swagger/` to the client-managed ASP.NET systemd service. The deployment user
needs passwordless sudo for `nginx -t` and `systemctl reload nginx`.

## Build variables

Vite values are embedded into the browser bundle at build time and are not
backend secrets:

```env
VITE_API_BASE_URL=/api
VITE_STRIPE_PUBLIC_KEY=pk_test_or_pk_live_public_key
VITE_GOOGLE_CLIENT_ID=optional-google-client-id
```

The staging default is `/api`, which keeps cookies and API calls same-origin.
Never put a Stripe secret key, webhook secret, database password, session
secret or identity-provider credential in this repository or a `VITE_*` value.

## GitHub Actions deployment

`.github/workflows/ci-cd.yml` runs typecheck, unit tests and a production build
for pull requests. A push to `main` builds the SPA, uploads a SHA-named `dist`
archive over SSH, switches `/var/www/nestystay/current`, validates nginx and
reloads it. It optionally checks the public frontend URL.

Create a GitHub `production` environment with these Actions variables:

| Variable | Value |
|---|---|
| `VITE_API_BASE_URL` | `/api` |
| `VITE_STRIPE_PUBLIC_KEY` | Client-owned staging or production publishable key |
| `VITE_GOOGLE_CLIENT_ID` | Client-owned Google OAuth client ID |

Add these Actions secrets:

| Secret | Value |
|---|---|
| `DEPLOY_HOST` | Client server DNS name or IP |
| `DEPLOY_PORT` | SSH port, normally `22` |
| `DEPLOY_USER` | Non-root deployment user that owns the release path |
| `DEPLOY_SSH_KEY` | Private key matching the server `authorized_keys` |
| `DEPLOY_PATH` | `/var/www/nestystay` |
| `FRONTEND_HEALTHCHECK_URL` | `https://staging.nestystay.net` |

The backend `.env` and all provider secrets stay on the server. GitHub Actions
does not need database, Stripe secret, Brevo, Zoho or identity-provider
credentials for the frontend deployment.

## Verification and rollback

```bash
sudo nginx -t
sudo systemctl reload nginx
curl --fail https://staging.nestystay.net/
curl --fail https://staging.nestystay.net/api/health
curl --fail https://staging.nestystay.net/api/properties
```

To roll back, repoint the nginx document-root symlink and reload nginx:

```bash
ln -sfn /var/www/nestystay/releases/<known-good-sha> /var/www/nestystay/current
sudo nginx -t
sudo systemctl reload nginx
```

The SPA must load `/login`, `/host-dashboard`, `/guest-dashboard` and the
badge-gated routes through nginx fallback routing.

## Local validation

```bash
npm ci
npm run typecheck
npm test -- --run
npm run build
```

Do not commit secrets or populated environment files. Keep the frontend API
paths and response contracts aligned with the backend repository.
