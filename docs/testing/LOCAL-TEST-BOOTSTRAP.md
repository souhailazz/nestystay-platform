# Deterministic local test bootstrap

Browser tests require a ready API before any page assertions run. `frontend/e2e/global-setup.ts` checks `PLAYWRIGHT_API_URL` (default `http://localhost:5019/api/health`) and fails with a clear readiness error instead of recording page-only evidence against a dead backend.

## PowerShell

Use local-only values; never copy production secrets into this repository:

```powershell
$env:ConnectionStrings__Postgres = "Host=127.0.0.1;Port=5432;Database=nestystay_dev;Username=<local-user>;Password=<local-password>"
$env:BackgroundJobs__Enabled = "false"
dotnet run --project .\backend\src\NestyStay.Api --configuration Release --launch-profile http
```

In another terminal:

```powershell
Set-Location .\frontend
npm ci
npm run dev -- --host 127.0.0.1 --port 5173
npm run test:e2e
```

The Playwright config starts the backend and Vite automatically when they are not already running. `PLAYWRIGHT_API_URL`, `PLAYWRIGHT_BASE_URL`, and artifact output directories can be overridden for CI. Database migrations and credentials remain environment-owned; no secret or production domain is committed here.
