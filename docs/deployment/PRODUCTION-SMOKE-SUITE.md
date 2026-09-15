# Production smoke suite

The non-destructive Playwright smoke suite runs against a deployed URL and does
not seed, mutate, delete, pay, issue, or revoke production records. It checks
the public route shell for the major M1–M5 journeys and verifies liveness,
readiness and the aggregate API health endpoint.

Run it from `frontend` after deployment:

```powershell
$env:PRODUCTION_BASE_URL = "https://stay.example.com"
npm run test:e2e:production
```

To exercise the optional authenticated login check, supply a dedicated,
non-production or explicitly approved smoke account through the process
environment. Do not place these values in Git or the command history:

```powershell
$env:SMOKE_EMAIL = "smoke@example.com"
$env:SMOKE_PASSWORD = "provided-through-secret-manager"
npm run test:e2e:production
```

The authenticated check is skipped when those variables are absent. It never
creates a user or changes application data. A production sign-off still
requires the client-approved staging/provider tests, synthetic-data smoke and
restore rehearsal described in the deployment and operations runbooks.
