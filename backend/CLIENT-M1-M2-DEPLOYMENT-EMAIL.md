# Email to the client — NestyStay M1–M2 delivery

**Subject:** NestyStay Milestones 1–2 — frontend/backend repositories and automatic deployment

Hello,

The NestyStay Milestones 1 and 2 delivery has been separated into the two
repositories you requested:

- Backend: https://github.com/NestyStayJamaica/NESTY-STAY_Backend
- Frontend: https://github.com/NestyStayJamaica/NESTY-STAY_Frontend

The release scope is the signed-agreement M1 Core Booking System and M2 Badge
System. M1 includes registration/login, 2FA and recovery, property listings,
booking quotes and PENDING/APPROVED/REJECTED state transitions, eKYC
application handling, Stripe application payment boundaries and guest/host
workspace data. M2 includes FREE, VERIFIED, TRUSTED and WELLNESS badges, exact
pricebook/eligibility rules, renewals/review records, feature gating and the
owner/admin badge dashboard.

Each repository contains its own README, Dockerfile, environment template,
deployment guide and GitHub Actions workflow. Logs, build output, test reports,
node_modules, database files and populated environment files are excluded from
the release.

## What you need to provision

1. A Linux Docker server with Docker Compose v2.
2. PostgreSQL, Redis and MinIO storage (the backend compose file can run these
   privately on the server; managed services are also supported).
3. DNS/TLS for `app.example.com` and `api.example.com` under the same parent
   domain.
4. Client-owned Stripe, Alibaba eKYC and email/provider accounts when moving
   beyond local/test mode.
5. A GitHub Container Registry read token and an SSH key for the deployment
   user.

## Required GitHub configuration

Create a protected `production` environment in **both repositories**. Add
`DEPLOY_HOST`, `DEPLOY_PORT`, `DEPLOY_USER`, `DEPLOY_SSH_KEY`, `DEPLOY_PATH`,
`GHCR_USERNAME`, `GHCR_TOKEN` and the appropriate health-check URL as Actions
secrets. The frontend also needs the public Actions variables
`VITE_API_BASE_URL`, `VITE_STRIPE_PUBLIC_KEY` and optional
`VITE_GOOGLE_CLIENT_ID`. Never add backend secrets to the frontend repository.

The workflow behavior is:

- Pull requests run restore/typecheck/build/unit tests.
- A push to `main` deploys only after all checks pass.
- A SHA-tagged Docker image is published to GHCR.
- The server receives the compose file, pulls that exact SHA and restarts only
  the changed service.
- A health check runs after deployment. A failed health check fails the GitHub
  workflow and leaves the previous container available for rollback.

Read `DEPLOYMENT.md` in each repository and follow the first-time server setup
before enabling automatic deployment. Store the backend `.env` only on the
server/secret manager. The API applies EF migrations on startup, so take a
database backup before the first production rollout.

## Important provider distinction

The local/test application integration is included. Production readiness still
requires the client's live Stripe webhook/domain/Connect validation, Alibaba
eKYC credentials and callbacks, email/SMS/push delivery credentials, backups,
monitoring and a production smoke test. Those values cannot be supplied by the
repository and must not be committed.

For future changes, please open a pull request rather than pushing directly to
`main`. Claude can assist, but it should read both `README.md` and
`DEPLOYMENT.md`, preserve the API contract, run all documented tests and never
commit secrets or generated artifacts.

Best regards,

NestyStay delivery team
