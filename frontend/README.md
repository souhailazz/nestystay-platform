# NestyStay frontend

This directory is a complete Vite + React TypeScript frontend repository. It
can be pushed and deployed independently from the backend repository.

## Requirements

- Node.js 22 or newer
- npm 10 or newer
- A reachable NestyStay API

## Local development with the API repository

Install dependencies:

```powershell
npm ci
```

Create `frontend/.env.local`:

```env
VITE_API_BASE_URL=/api
```

Start the development server:

```powershell
npm run dev -- --host 127.0.0.1 --port 5173
```

The Vite configuration proxies `/api` to `http://localhost:5019`. Start the
backend API on that port, or set `VITE_API_BASE_URL` to an absolute API URL:

```env
VITE_API_BASE_URL=https://api.example.com/api
```

When using an absolute URL, the backend must allow the frontend origin through
`NESTYSTAY_CORS_ALLOWED_ORIGINS` and configure the shared session-cookie domain.

## Production build

Copy `.env.production.example` to `.env.production` and replace the values:

```env
VITE_API_BASE_URL=https://api.example.com/api
VITE_STRIPE_PUBLIC_KEY=pk_live_your_real_publishable_key
VITE_GOOGLE_CLIENT_ID=optional_google_client_id
```

Only public values may be placed in `VITE_*` variables. Never put database
passwords, session secrets, Stripe secret keys, webhook secrets or provider
private keys in this file.

Build and preview:

```powershell
npm run typecheck
npm run build
npm run preview -- --host 0.0.0.0 --port 4173
```

The SPA server must route every application path back to `index.html`. The
included `Dockerfile` and `nginx.conf` provide the standalone container setup.

Build the container:

```bash
docker build \
  --build-arg VITE_API_BASE_URL=https://api.example.com/api \
  --build-arg VITE_STRIPE_PUBLIC_KEY=pk_live_your_real_publishable_key \
  --build-arg VITE_GOOGLE_CLIENT_ID= \
  -t nestystay-frontend:release .
docker run --rm -p 8080:80 nestystay-frontend:release
```

The production Stripe publishable key must exist during the Docker build; the
image does not read Vite variables at container runtime. Build arguments are
public values and will be present in the browser bundle.

## Verification

```powershell
npm run lint
npm test
npm run build
npm run test:e2e
```

After deployment, set `PRODUCTION_BASE_URL` and run the non-destructive smoke
suite. Use `SMOKE_EMAIL` and `SMOKE_PASSWORD` only through the client secret
manager for an approved smoke account.

## Claude handoff

Ask Claude to read this file first, preserve the API paths and response shapes,
keep all secrets out of source control, and run typecheck, tests and build after
changes. The frontend repository owns browser configuration only; provider
secret keys belong exclusively in the backend deployment environment.
