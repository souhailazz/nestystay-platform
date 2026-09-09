# Split-repository deployment handoff

NestyStay is intentionally organized into two top-level applications:

```text
frontend/  Vite React browser application
backend/   ASP.NET Core API, domain, application and infrastructure layers
```

The client may push each directory to a separate Git repository. Do not move
source files between the two repositories without updating the API contract and
the deployment configuration.

The current workspace also contains nested frontend Git metadata from an older
Next.js starter repository. Treat the current Vite files as the source of truth;
do not mix that old history into the new frontend repository unless the client
explicitly requests a history migration. A clean export or a new repository
initialized from the current frontend contents is safest.

## Repository boundaries

### Frontend repository

Push the contents of `frontend/`. It contains its own `package.json`, lockfile,
Vite configuration, tests, Playwright suites, `.env.example`, production env
example, Dockerfile and Nginx configuration.

Required production build variable:

```env
VITE_API_BASE_URL=https://api.example.com/api
VITE_STRIPE_PUBLIC_KEY=pk_live_...
```

`VITE_GOOGLE_CLIENT_ID` is optional. Vite variables are public and are embedded
in the build; no backend secret belongs in the frontend repository. For a
Docker build, pass these values as `--build-arg` values to the standalone
frontend Dockerfile.

### Backend repository

Push the contents of `backend/`. It contains its own solution, migrations,
tests, API project, `.env.example`, production env example, Dockerfile and
backend README.

Required production settings include PostgreSQL, session/TOTP/webhook secrets,
CORS, Stripe, Alibaba eKYC, InsuraGuest, MinIO and Brevo values. The backend
README explains local migration, container startup and split-origin cookies.

## Recommended hosting topology

Use the same parent domain:

```text
https://app.example.com  -> frontend repository/container
https://api.example.com  -> backend repository/container
```

Set on the backend:

```env
NESTYSTAY_CORS_ALLOWED_ORIGINS=https://app.example.com
NESTYSTAY_SESSION_COOKIE_DOMAIN=.example.com
NESTYSTAY_SESSION_COOKIE_SAMESITE=Lax
PUBLIC_APP_URL=https://app.example.com
```

This keeps the HttpOnly session and readable CSRF cookie available to the
frontend while retaining credentialed CORS. If the two services are hosted on
unrelated domains, use a same-origin reverse proxy instead; a cookie cannot be
shared across unrelated registrable domains.

## Client deployment sequence

1. Create the frontend and backend repositories from the two directories.
2. Provision the client-owned VPS, PostgreSQL, Redis and MinIO.
3. Add backend secrets to the client secret manager; never commit `.env`.
4. Add frontend `VITE_*` values at build time.
5. Build and deploy the backend image, apply EF migrations once, then start API
   and worker processes.
6. Build and deploy the frontend image or static bundle.
7. Configure DNS/TLS/reverse proxy and verify CORS and cookie login.
8. Run health checks, provider validation, backups/restore rehearsal and the
   frontend production smoke suite.

## Git split commands

Run from the current monorepo when the client provides the destination URLs:

```bash
git subtree split --prefix=frontend -b frontend-release
git push <frontend-remote> frontend-release:main

git subtree split --prefix=backend -b backend-release
git push <backend-remote> backend-release:main
```

The root deployment files remain useful for the current combined deployment;
the standalone Dockerfiles allow each new repository to build independently.

## Claude instructions for the client

In each repository, start Claude with:

> Read `README.md` and the repository files before changing anything. Do not
> invent API endpoints or environment variables. Do not commit populated env
> files or secrets. Run the documented typecheck/build/test commands after any
> change. Treat the other repository as an external API dependency and preserve
> the documented origin, cookie and CORS settings.

Production readiness remains dependent on client-owned credentials, domain,
infrastructure, provider validation, monitoring and backup evidence.
