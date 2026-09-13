# NestyStay staging deployment — Cloud AI handoff

Target public URL: `https://staging.nestystay.net`

## The issue this fixes

The staging hostname currently serves the frontend, but `/api/health` and
`/api/properties` return an empty `404 Not Found`. That is not a browser CORS
failure. It means the deployed edge is not forwarding `/api/*` to the ASP.NET
API, or the API container was not deployed.

The release must use the root `docker-compose.production.yml` and
`deploy/Caddyfile.production` together. Caddy serves the frontend and routes:

```text
/api/*      -> api:8080
/openapi/*  -> api:8080
/swagger/*  -> api:8080
everything else -> frontend:80
```

Do not use the standalone `frontend/deploy/docker-compose.yml` for staging;
that compose file intentionally runs only the static frontend.

## Server-only environment

Create the populated `.env` on the deployment server or in its secret manager.
Never commit the populated file, paste secrets into GitHub, or put backend
secrets in `VITE_*` variables.

At minimum, set these public/routing values:

```env
ASPNETCORE_ENVIRONMENT=Staging
NESTYSTAY_DOMAIN=staging.nestystay.net
PUBLIC_APP_URL=https://staging.nestystay.net
NESTYSTAY_CORS_ALLOWED_ORIGINS=https://staging.nestystay.net
NESTYSTAY_SESSION_COOKIE_DOMAIN=.nestystay.net
VITE_API_BASE_URL=/api
```

Also set the database, Redis, MinIO/object-storage, session, CSRF/webhook,
email, payment, monitoring and backup values from `.env.production.example`.
Use dedicated staging data and test-mode provider accounts.

## Provider modes

- Transactional email: Brevo (`EMAIL_PROVIDER=brevo` and
  `NESTYSTAY_EMAIL_PROVIDER=brevo`). Verify `nestystay.net` as a sender domain
  in Brevo and use a sender such as `no-reply@nestystay.net`.
- Business mail: Zoho (`BUSINESS_MAIL_PROVIDER=zoho`). Create the support,
  billing, privacy and security mailboxes in Zoho and configure their DNS.
- Payments: Stripe test mode for staging. Use a `pk_test_` public key in the
  frontend and `sk_test_`/test webhook values on the server.
- Identity verification: the current backend release still contains the
  Alibaba eKYC adapter. Stripe Identity is the intended replacement, but the
  Stripe Identity adapter and webhook contract must be merged and tested
  before changing `EKYC_PROVIDER` to a Stripe value. Do not claim Stripe
  Identity is active until that code is deployed and its test flow passes.

## Cloud AI deployment checklist

1. Deploy the latest pushed root commit, including `docker-compose.production.yml`
   and `deploy/Caddyfile.production`.
2. Configure the server-only environment above.
3. Start the complete compose stack, including `caddy`, `frontend`, `api`,
   `worker`, `postgres`, `redis`, `minio` and observability services.
4. Confirm the DNS A/AAAA record for `staging.nestystay.net` points to the
   server and that ports 80/443 reach Caddy.
5. Run migrations and seed only the dedicated staging/demo data.
6. Confirm these URLs return successful responses from the public hostname:

```text
https://staging.nestystay.net/
https://staging.nestystay.net/api/health
https://staging.nestystay.net/api/health/ready
https://staging.nestystay.net/api/properties
https://staging.nestystay.net/openapi/v1.json
```

If `/api/health` is still 404, inspect the edge/container routing first; do
not try to solve it by adding permissive CORS or by hard-coding API responses
in the frontend.

## Email to send to the deployment operator

**Subject:** NestyStay staging deployment — configure `staging.nestystay.net`

Hello,

Please deploy the latest NestyStay frontend and ASP.NET backend release from
the pushed GitHub commit. The public staging URL is:

`https://staging.nestystay.net`

The current hostname serves the frontend, but `/api/properties` returns a
Cloudflare 404 because the API is not connected to the hostname. Deploy the
full root Compose stack and the checked-in Caddy configuration. Caddy must
forward `/api/*`, `/openapi/*` and `/swagger/*` to the `api:8080` container;
the frontend-only compose file is not sufficient.

On the server/secret manager, please create and populate the environment file
yourself. Use these routing values exactly:

```env
ASPNETCORE_ENVIRONMENT=Staging
NESTYSTAY_DOMAIN=staging.nestystay.net
PUBLIC_APP_URL=https://staging.nestystay.net
NESTYSTAY_CORS_ALLOWED_ORIGINS=https://staging.nestystay.net
NESTYSTAY_SESSION_COOKIE_DOMAIN=.nestystay.net
VITE_API_BASE_URL=/api
```

Populate the remaining server variables from `.env.production.example` using
dedicated staging credentials: PostgreSQL, Redis, MinIO/object storage,
session/TOTP/webhook secrets, monitoring, backups and provider settings. Keep
the populated file outside GitHub and do not put backend secrets in frontend
build variables.

Configure the providers as follows:

- Brevo for transactional email. Verify `nestystay.net` as a sender domain,
  add the required DNS records, and set the Brevo API key, sender and reply-to
  values in the server secret manager.
- Zoho for business mail. Create the operational NestyStay mailboxes and
  aliases (support, billing, privacy and security), then configure their DNS
  and mailbox security.
- Stripe test mode for staging. Set the test publishable/secret keys and test
  webhook signing secret, and use only Stripe test data.
- Do not enable or describe Stripe Identity as active yet. The current backend
  release still has the Alibaba eKYC adapter; Stripe Identity requires its
  adapter/webhook implementation to be merged and validated first. Keep the
  current configured eKYC mode until that release is approved, or coordinate
  the provider migration as a separate code change.

After deployment, verify from outside the server:

```text
https://staging.nestystay.net/
https://staging.nestystay.net/api/health
https://staging.nestystay.net/api/health/ready
https://staging.nestystay.net/api/properties
https://staging.nestystay.net/openapi/v1.json
```

The API health and properties endpoints must return 200 responses. Please
also test login, 2FA, property discovery, booking, the staging identity flow,
Stripe test payment boundaries, email delivery and badge/admin routes using
dedicated demo data. Do not run destructive actions against production data.

Regards,

NestyStay delivery team
