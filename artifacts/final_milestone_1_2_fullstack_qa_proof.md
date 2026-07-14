# Final Milestone 1/2 Full-Stack QA Proof

Date: 2026-07-14

## Summary Verdict

Milestone 1 status: PASS for the local full-stack app.

Milestone 2 status: PASS for the local full-stack app.

Production launch status: NOT PRODUCTION-LAUNCHED. The remaining work is provider credential configuration, deployment, observability, backups, legal/compliance review, and live operational hardening.

This pass inspected the actual repository, ran backend/frontend/database/security checks, added missing automated coverage for seed pricebook and webhook production-secret enforcement, fixed the EF design-time PostgreSQL connection for the no-Docker local setup, ran API smoke through the Vite proxy, ran persistence/restart checks against PostgreSQL, and ran browser route/auth/responsive smoke checks.

## Repository Map

Backend:

- Solution: `backend/NestyStay.sln`
- API: `backend/src/NestyStay.Api`
- Application: `backend/src/NestyStay.Application`
- Domain: `backend/src/NestyStay.Domain`
- Infrastructure/PostgreSQL/EF: `backend/src/NestyStay.Infrastructure`
- Tests:
  - `backend/tests/NestyStay.Api.Tests`
  - `backend/tests/NestyStay.Application.Tests`
  - `backend/tests/NestyStay.Domain.Tests`
  - `backend/tests/NestyStay.Infrastructure.Tests`

Frontend:

- Vite React TypeScript app: `frontend`
- Scripts available: `dev`, `build`, `preview`
- No configured `lint` or `test` npm scripts.

Database:

- Local no-Docker PostgreSQL listener: `127.0.0.1:55432`
- Database: `nestystay_dev`
- User/password: `nestystay` / `nestystay`
- EF migrations:
  - `20260501214102_InitialBackendSchema`
  - `20260621163343_MilestonePersistentStores`

Live local services at the end of QA:

- Backend: `http://localhost:5019`
- Frontend: `http://127.0.0.1:5173`
- PostgreSQL: `127.0.0.1:55432`

## Test Matrix

| Area | Coverage result |
|---|---|
| Registration | PASS: valid registration, duplicate rejection, weak password rejection, 2FA requirement, hashed password covered by tests and smoke. |
| Login | PASS: valid login, wrong password rejection, challenge creation, generic invalid errors covered. |
| 2FA | PASS: correct code, wrong code, expired challenge, reused challenge, missing challenge covered. Browser register/2FA flow passed. |
| Session persistence | PASS: browser verified authenticated dashboard remains after navigating back to the route. |
| Properties | PASS: list/detail/create, unknown property 404, Free-host verification upsell rejection, persistence covered. |
| Booking quote | PASS: valid quote, invalid ranges, overlap blocked by pending hold, totals and guest verification line covered. |
| Booking creation | PASS: eKYC property creates PENDING hold, non-eKYC property approves immediately in tests, invalid inputs rejected. |
| Booking statuses | PASS: PENDING/APPROVED/REJECTED returned by API; conflict transitions rejected. |
| eKYC/webhooks | PASS: approval, rejection, duplicate idempotency, conflict rejection, production shared-secret enforcement covered. |
| Stripe-style payment | PASS: authorization after approval, capture blocked while pending/rejected, capture succeeds after approval, duplicate capture idempotent. |
| Notifications | PASS: approval/rejection host and guest notification records covered, duplicate webhook does not duplicate. |
| Admin booking controls | PASS: frontend booking management pass/reject/capture route smoke; payment status handled. |
| Badge definitions | PASS: Free/Verified/Trusted/Wellness definitions covered. |
| Badge eligibility | PASS: Verified/Trusted/Wellness eligibility rules covered. |
| Badge purchase | PASS: success, failed payment suspended state, duplicate active purchase behavior covered. |
| Feature locking | PASS: Free/Verified/Trusted/Wellness locking and expired/suspended behavior covered. |
| Renewals | PASS: renewal read/pay and expiry extension covered. |
| Pricebook | PASS: read/update, admin auth, invalid negative/zero campaign price guards, persistence covered. |
| Campaigns | PASS: list/create/enroll, expired/invalid campaign rejection, campaign purchase price consistency covered. |
| Founding benefits | PASS: create/read, duplicate claim guard, ineligible rejection, transfer evaluation covered. |
| Commission quotes | PASS: standard/founding fees, zero/negative/large decimal cases covered. |
| PostgreSQL persistence | PASS: migrations apply, database up to date, app-created property and booking survived backend restart. |
| Admin authorization | PASS: no token 401, operator token 403, admin token 200; all admin-only mutation endpoints now have 401 test coverage. |
| Production guards | PASS: production startup fails closed when required provider/admin/webhook settings are missing. |
| Frontend build/typecheck | PASS: `npm run build` and `npx tsc -b` pass. |
| Frontend lint/test | Not configured: `npm run lint` and `npm run test` are missing scripts. |
| Browser desktop smoke | PASS: home, explore, dashboards auth gate, calendar, bookings, payment, admin. |
| Browser auth smoke | PASS: register, receive 2FA code, verify, enter guest dashboard. |
| Browser mobile smoke | PASS at 390px: home, explore, admin show no horizontal overflow or visible failure state. |
| OpenAPI | PASS: `/openapi/v1.json` loads with 39 paths. |
| Vulnerability scans | PASS: npm audit clean; .NET vulnerable package scan clean. |

## Exact Commands Run

Backend restore/build/test:

```powershell
dotnet restore NestyStay.sln
dotnet build NestyStay.sln -m:1 -v minimal
dotnet build NestyStay.sln -c Release --no-restore -m:1 -v minimal
dotnet test NestyStay.sln -m:1 -v minimal
dotnet test NestyStay.sln -c Release --no-build -m:1 -v minimal
```

Backend database/migrations:

```powershell
dotnet ef migrations list --project src/NestyStay.Infrastructure/NestyStay.Infrastructure.csproj --startup-project src/NestyStay.Api/NestyStay.Api.csproj
dotnet ef database update --project src/NestyStay.Infrastructure/NestyStay.Infrastructure.csproj --startup-project src/NestyStay.Api/NestyStay.Api.csproj
```

Backend security/package scan:

```powershell
dotnet list NestyStay.sln package --vulnerable --include-transitive
```

Production startup guard check:

```powershell
dotnet run --project backend/src/NestyStay.Api/NestyStay.Api.csproj --no-launch-profile --urls http://127.0.0.1:5999
```

with production environment and required provider/admin/webhook environment variables removed.

Frontend install/build/type/audit:

```powershell
npm ci
npm run build
npx tsc -b
npm audit --audit-level=low
npm run lint
npm run test
npm run
```

Local smoke:

```powershell
Invoke-RestMethod http://localhost:5019/api/health
Invoke-WebRequest http://127.0.0.1:5173/
Invoke-RestMethod http://localhost:5019/openapi/v1.json
```

Browser smoke:

- In-app Browser desktop route pass.
- In-app Browser register/login/2FA pass.
- In-app Browser mobile viewport pass at 390x844.

## Command Results

Backend Debug tests:

```text
NestyStay.Api.Tests:             10 passed
NestyStay.Application.Tests:     16 passed
NestyStay.Domain.Tests:           5 passed
NestyStay.Infrastructure.Tests:   5 passed
Total:                           36 passed, 0 failed
```

Backend Release tests:

```text
NestyStay.Api.Tests:             10 passed
NestyStay.Application.Tests:     16 passed
NestyStay.Domain.Tests:           5 passed
NestyStay.Infrastructure.Tests:   5 passed
Total:                           36 passed, 0 failed
```

Builds:

```text
dotnet build Debug:   succeeded, 0 warnings, 0 errors
dotnet build Release: succeeded, 0 warnings, 0 errors
npm run build:        succeeded
npx tsc -b:           succeeded
```

Frontend script availability:

```text
npm run lint: missing script
npm run test: missing script
Available scripts: dev, build, preview
```

Package audits:

```text
npm audit --audit-level=low: found 0 vulnerabilities
dotnet list package --vulnerable --include-transitive: no vulnerable packages in all projects
```

EF:

```text
dotnet ef migrations list: InitialBackendSchema, MilestonePersistentStores
dotnet ef database update: No migrations were applied. The database is already up to date.
```

Known EF note:

```text
The Entity Framework tools version '10.0.2' is older than that of the runtime '10.0.9'.
```

This is a tools-version warning, not a vulnerable package warning and not a migration failure.

## API Smoke Result

Smoke ran through `http://127.0.0.1:5173/api`, exercising the backend via Vite proxy.

```json
{
  "registered": true,
  "duplicateRegistration": 400,
  "weakRegistration": 400,
  "bad2fa": 400,
  "sessionUserMatches": true,
  "initialPropertyCount": 3,
  "createdProperty": "c041c45b-4ccc-4bed-b343-c44e5452b315",
  "unknownProperty": 404,
  "freeUpsell": 400,
  "quoteNights": 3,
  "invalidQuote": 400,
  "bookingStatus": "PENDING",
  "overlapQuote": 400,
  "pendingCapture": 400,
  "approvedStatus": "APPROVED",
  "conflictWebhook": 400,
  "capturedStatus": "CAPTURED",
  "rejectedStatus": "REJECTED",
  "replacementDatesAvailable": true,
  "guestBookings": 2,
  "noAdmin": 401,
  "operator": 403,
  "priceUpdateStatus": 200,
  "badgeDefinitions": 4,
  "eligible": true,
  "badgeLevel": "Verified",
  "featureLevel": "Verified",
  "renewalPayStatus": 200,
  "campaign": "qa-4a6de3c32e",
  "enrollment": "qa-4a6de3c32e",
  "founding": "Silver",
  "transfer": true,
  "commissionRevenue": 81.0
}
```

OpenAPI:

```text
/openapi/v1.json loaded with 39 paths.
```

## Persistence Restart Result

Live PostgreSQL persistence/restart smoke:

```json
{
  "propertyPersisted": true,
  "bookingPersisted": true,
  "bookingStatus": "PENDING",
  "datesHeld": true
}
```

This confirms app-created milestone records survive backend restart and reload from PostgreSQL.

## Browser Smoke Result

Desktop routes checked:

- `/`
- `/explore`
- `/guest-dashboard`
- `/host-dashboard`
- `/calendar`
- `/bookings`
- `/payment-confirmation`
- `/admin`

Desktop result:

- No visible `Request failed`, `Admin action failed`, `undefined`, or not-found text.
- Generated stay images loaded with real dimensions on home/explore.
- Logged-out dashboards showed expected auth gates.
- Calendar/bookings/payment/admin routes loaded live API data.

Auth UI result:

- Browser registration succeeded.
- 2FA challenge rendered the API demo code.
- 2FA verification entered the guest dashboard.
- Navigating back to `/guest-dashboard` preserved the authenticated dashboard view.

Mobile routes checked at 390x844:

- `/`
- `/explore`
- `/admin`

Mobile result:

- No visible failure state.
- Mobile menu present.
- No horizontal overflow detected.

## Bugs Found And Fixed In This Pass

| Finding | Fix |
|---|---|
| EF design-time factory hardcoded `localhost:5432`, while no-Docker local PostgreSQL runs on `127.0.0.1:55432`. EF migrations/update could fail even when the runtime app was healthy. | Updated `NestyStayDbContextFactory` to read `ConnectionStrings__Postgres`, falling back to the active local no-Docker connection string. |
| `/api/backend-schema/seed/pricebook` had no explicit API test coverage. | Added API test asserting seed pricebook endpoint returns expected seed keys. |
| Production webhook shared-secret enforcement was covered manually but not automated. | Added controller-level tests for missing and matching `X-NestyStay-Webhook-Secret` in Production. |
| Admin-only mutation endpoint test covered pricebook but not every admin mutation route. | Added API test proving all admin-only mutation routes reject missing bearer token with 401. |

## Files Changed In This Pass

```text
backend/src/NestyStay.Infrastructure/Persistence/NestyStayDbContextFactory.cs
backend/tests/NestyStay.Api.Tests/HealthEndpointTests.cs
backend/tests/NestyStay.Api.Tests/PhaseTwoEndpointTests.cs
backend/tests/NestyStay.Api.Tests/WebhookSecurityTests.cs
artifacts/final_milestone_1_2_fullstack_qa_proof.md
```

Related local changes from earlier hardening/frontend work remain present in the worktree and were not reverted.

## Scenario Coverage Table

| Scenario group | Automated coverage | Live/API coverage | Browser coverage |
|---|---:|---:|---:|
| Registration/login/2FA | Yes | Yes | Yes |
| Session persistence | Partial | N/A | Yes |
| Properties | Yes | Yes | Yes |
| Booking quote/create/overlap | Yes | Yes | Partial UI route smoke |
| eKYC approval/rejection | Yes | Yes | Admin booking controls route smoke |
| Payment capture | Yes | Yes | Payment route smoke |
| Notifications | Yes | Indirect | No dedicated UI |
| Badge definitions/eligibility/purchase | Yes | Yes | Admin route smoke |
| Feature access | Yes | Yes | Admin route smoke |
| Renewals | Yes | Yes | Admin route smoke |
| Pricebook | Yes | Yes | Admin route smoke |
| Campaigns | Yes | Yes | Admin route smoke |
| Founding benefits | Yes | Yes | Admin route smoke |
| Commission quote | Yes | Yes | Admin route smoke |
| PostgreSQL persistence | Yes with EF/in-memory context reload; live restart smoke with PostgreSQL | Yes | N/A |
| Admin authorization | Yes | Yes | Admin form token path present |
| Production config guards | Yes | Yes | N/A |
| OpenAPI/health/schema/jobs | Yes | Yes | Admin route smoke |
| Responsive behavior | N/A | N/A | Yes at 390px |

## Remaining Production-Only Items

These are not local Milestone 1/2 code gaps. They remain before production launch:

- Configure real Stripe live keys and production webhook validation.
- Configure real Alibaba Cloud eKYC credentials and provider webhook/signature settings.
- Configure real Cloudflare R2 credentials/upload signing.
- Configure real InsuraGuest API credentials/integration.
- Generate and set production `NESTYSTAY_ADMIN_TOKEN_SHA256`.
- Set production `NESTYSTAY_WEBHOOK_SHARED_SECRET`.
- Apply production database hosting, backups, migration workflow, and restore drills.
- Configure production domain, SSL, hosting, deployment, and CDN/caching.
- Configure real email/SMS/push providers or outbox workers beyond local test-safe notification records.
- Add public admin user/role management if it becomes product scope.
- Add production observability, structured logging, alerting, uptime checks, and incident runbooks.
- Complete legal/privacy/payment compliance review.

## Final Verdict

Milestone 1 and Milestone 2 are complete locally for the full-stack app and pass the final QA audit.

The app is not production-launched. Remaining work is production provider configuration, deployment, and launch hardening.
