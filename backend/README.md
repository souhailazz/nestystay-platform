# NestyStay backend

This directory is a complete ASP.NET Core API repository. It can be pushed to
its own Git repository without depending on the frontend repository.

## Requirements

- .NET SDK 10
- PostgreSQL 17 (local or managed)
- Redis is recommended for the production worker/rate-limit topology
- MinIO/S3-compatible storage for production uploads

## Local development

The API does not automatically load `.env` files. Set variables in the shell,
IDE launch profile, or secret manager.

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ConnectionStrings__Postgres = "Host=127.0.0.1;Port=5432;Database=nestystay_dev;Username=nestystay;Password=YOUR_PASSWORD"
$env:NESTYSTAY_SESSION_TOKEN_SECRET = "a-random-secret-at-least-32-bytes"
$env:NESTYSTAY_TOTP_SECRET_PROTECTION_KEY = "a-different-random-secret-at-least-32-bytes"
$env:PUBLIC_APP_URL = "http://localhost:5173"
$env:NESTYSTAY_EMAIL_PROVIDER = "file"
$env:NESTYSTAY_STORAGE_PROVIDER = "local"
```

The checked-in example uses port `55432` for a containerized database. The
standard local PostgreSQL service uses port `5432`; change the port when needed.
The database and role must exist before migrations run.

Apply migrations once:

```powershell
$env:NESTYSTAY_MIGRATE_ONLY = "true"
dotnet run --project src/NestyStay.Api
Remove-Item Env:NESTYSTAY_MIGRATE_ONLY
```

Start the API:

```powershell
dotnet run --project src/NestyStay.Api --urls http://localhost:5019
```

Health and API documentation:

- `http://localhost:5019/api/health`
- `http://localhost:5019/api/health/ready`
- `http://localhost:5019/openapi/v1.json`

Local mode uses deterministic adapters for Stripe, Alibaba eKYC, email and
object storage. Real credentials are not needed for local milestone testing.

## Split frontend/API deployment

Use two HTTPS origins under the same parent domain, for example:

- Frontend: `https://app.example.com`
- API: `https://api.example.com`

Set `NESTYSTAY_CORS_ALLOWED_ORIGINS=https://app.example.com` and
`NESTYSTAY_SESSION_COOKIE_DOMAIN=.example.com`. Keep
`NESTYSTAY_SESSION_COOKIE_SAMESITE=Lax` for same-site subdomains. Use
`SameSite=None` only when the frontend and API are genuinely cross-site, and
always use HTTPS in that case.

The API accepts an explicit CORS allow-list instead of being limited to
localhost. Never use `*` with credentialed browser requests.

If the frontend and API use unrelated parent domains, cookie sessions cannot be
shared. Host them under one parent domain or use a same-origin reverse proxy.

## Production container

Build and run the standalone image:

```bash
docker build -t nestystay-api:release .
docker run --rm -p 8080:8080 --env-file .env nestystay-api:release
```

The production environment must include PostgreSQL, session/TOTP/webhook
secrets, Stripe, Alibaba eKYC, InsuraGuest, MinIO, Brevo and CORS settings.
Use `.env.production.example` as the complete API-side template. Do not put
secrets in Git, Dockerfiles, logs or chat.

Run migrations as a one-shot release step with
`Database__ApplyMigrationsOnly=true` or `NESTYSTAY_MIGRATE_ONLY=true`, then
start the normal API process. Keep PostgreSQL, Redis and MinIO private.

## Validation

```powershell
dotnet restore NestyStay.sln
dotnet build NestyStay.sln -c Release
dotnet test NestyStay.sln -c Release
```

Before go-live, verify the deployed health endpoints, CORS preflight, cookie
login/2FA, booking/payment, eKYC callback, uploads, email delivery, backups and
the production smoke suite from the frontend repository.

## Claude handoff

Ask Claude to read this file first. It should preserve the API contract, never
invent environment variables, never commit populated env files, and run build,
test and health checks after changes. Provider credentials must be supplied by
the client through its secret manager.
