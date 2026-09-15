# NestyStay backend deployment guide

This repository is the ASP.NET Core 10 backend release. The client staging
server runs the published API directly under `systemd`; it does not use Docker
or Docker Compose. Nginx terminates TLS and forwards `/api/*`, `/openapi/*` and
`/swagger/*` to the local ASP.NET listener.

## What the client receives

- Registration, login, logout, 2FA/recovery and signed cookie sessions.
- Host properties, booking quotes, PENDING/APPROVED/REJECTED states, date holds
  and overlap protection.
- Stripe payment authorization/capture/refund boundaries and Stripe Identity
  verification flow.
- FREE, VERIFIED, TRUSTED and WELLNESS badges with server-side restrictions.
- PostgreSQL migrations, API tests and a release publish artifact.

Live provider credentials remain deployment-time configuration. Never commit a
populated `.env`, API key, webhook secret, database password or session secret.

## One-time server layout

The deployment user must own the release directory and have passwordless sudo
for restarting the API service:

```text
/opt/nestystay/backend/
  .env                         # server-only secrets; never in Git
  current -> releases/<sha>    # active release
  releases/<sha>/              # published .NET files
```

The `systemd` unit should run the symlinked release, for example:

```ini
[Service]
WorkingDirectory=/opt/nestystay/backend/current
ExecStart=/usr/bin/dotnet /opt/nestystay/backend/current/NestyStay.Api.dll
EnvironmentFile=/opt/nestystay/backend/.env
Restart=always
```

The actual unit name is configurable in GitHub Actions as
`BACKEND_SYSTEMD_SERVICE`; the workflow default is `nestystay-api.service`.

Create the server-only environment from `.env.production.example`. For the
staging domain, the important values include:

```env
ASPNETCORE_ENVIRONMENT=Staging
ConnectionStrings__Postgres=Host=127.0.0.1;Port=5432;Database=nesty_prod;Username=nesty_app;Password=<strong-password>
ConnectionStrings__Redis=127.0.0.1:6379,password=<redis-password>
NESTYSTAY_CORS_ALLOWED_ORIGINS=https://staging.nestystay.net
NESTYSTAY_SESSION_COOKIE_DOMAIN=.nestystay.net
NESTYSTAY_SESSION_COOKIE_SAMESITE=Lax
PUBLIC_APP_URL=https://staging.nestystay.net
EKYC_PROVIDER=stripe_identity
STRIPE_IDENTITY_RETURN_URL=https://staging.nestystay.net/booking/{bookingId}/pending
NESTYSTAY_SESSION_TOKEN_SECRET=<at-least-32-random-bytes>
NESTYSTAY_TOTP_SECRET_PROTECTION_KEY=<different-at-least-32-random-bytes>
NESTYSTAY_WEBHOOK_SHARED_SECRET=<random-webhook-secret>
```

Keep Stripe test mode enabled for staging until production approval. Brevo,
Zoho, Stripe and Stripe Identity values belong only in the server secret
manager or `.env` file. Stripe Identity is the only supported eKYC provider.

## GitHub Actions deployment

`.github/workflows/ci-cd.yml` runs restore, build and the full test suite for
pull requests. A push to `main` then publishes the API, uploads a SHA-named
release archive over SSH, switches `/opt/nestystay/backend/current`, restarts
the `systemd` service and checks `/api/health/ready`.

Create a GitHub `production` environment with these Actions secrets:

| Secret | Value |
|---|---|
| `DEPLOY_HOST` | Client server DNS name or IP |
| `DEPLOY_PORT` | SSH port, normally `22` |
| `DEPLOY_USER` | Non-root deployment user that owns the release path |
| `DEPLOY_SSH_KEY` | Private key matching the server `authorized_keys` |
| `DEPLOY_PATH` | `/opt/nestystay/backend` |
| `BACKEND_HEALTHCHECK_URL` | `https://staging.nestystay.net` |

Create this Actions variable if the unit has a different name:

| Variable | Value |
|---|---|
| `BACKEND_SYSTEMD_SERVICE` | e.g. `NestyStay.Api.service` |

The deploy user must be able to run `sudo -n systemctl restart` and
`sudo -n systemctl is-active` for that one unit. No database or provider
secrets are sent through GitHub Actions.

## Verification and rollback

```bash
sudo systemctl status nestystay-api.service
sudo journalctl -u nestystay-api.service --no-pager -n 200
curl --fail https://staging.nestystay.net/api/health
curl --fail https://staging.nestystay.net/api/health/ready
curl --fail https://staging.nestystay.net/api/properties
```

To roll back, repoint `current` to a known-good release and restart the unit:

```bash
ln -sfn /opt/nestystay/backend/releases/<known-good-sha> /opt/nestystay/backend/current
sudo systemctl restart nestystay-api.service
```

Do not delete PostgreSQL data or migration history during rollback.

## Local validation

```bash
dotnet restore NestyStay.sln
dotnet build NestyStay.sln --configuration Release
dotnet test NestyStay.sln --configuration Release --no-restore
```

Before changing deployment behavior, preserve the public API paths and run the
complete build and test commands above. Do not commit secrets or populated
environment files.
