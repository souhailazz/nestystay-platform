# NestyStay URL and Backend Architecture Audit

**Audit date:** 20–21 October 2026  
**Audit type:** Read-only source/configuration review with local integrated-test evidence  
**Application scope:** NestyStay frontend, backend API, persistence, authentication, providers, workers, and deployment boundaries  
**Verdict:** **PARTIALLY VERIFIED — the architecture is coherent, but staging/production integration gates remain.**

## 1. Executive summary

NestyStay is a Vite/React single-page application backed by an ASP.NET Core API, EF Core/PostgreSQL persistence, provider adapters, signed sessions, and optional background workers. The intended integrated candidates have:

- a centralized frontend route manifest;
- role-specific workspaces for Guest, Host, Property Manager, Owner, Service Provider, Local Business, Wellness Officer, Gate Guard, and Admin;
- hydration-safe authentication and server-selected active-role handling;
- a separate configurable Admin hostname boundary;
- server-side role, permission, ownership, and tenant checks;
- opt-in migration execution rather than silent API-startup migration;
- configurable local/MinIO storage, file/Brevo email, Stripe, calendar, and worker adapters.

The architecture is not fully accepted from source/local evidence alone. The remaining gates are deployed revision matching, Admin DNS/TLS/CORS/cookie configuration, real MinIO persistence, real Brevo delivery, worker topology/heartbeat, authenticated staging workflow coverage, and shared-database migration verification.

### Important source-state distinction

The umbrella workspace is dirty and contains unrelated source, test, screenshot, scanner, and evidence changes. The clean final candidates below are the authoritative source used for this audit. The dirty nested repositories were not reset, staged, or used to replace the candidates.

| Layer | Authoritative worktree | Branch | Audited revision | State |
|---|---|---|---|---|
| Frontend | `C:\Users\Administrator\AppData\Local\Temp\nestystay-frontend-final-candidate-20261020` | `codex/final-frontend-e2e-alignment` | `fb6edc46024668f719cc28c596703ed2061f656f` | Clean, pushed candidate |
| Backend | `C:\Users\Administrator\AppData\Local\Temp\nestystay-backend-final-candidate-20261020` | `codex/final-backend-integration-fixes` | `090d17d260914d3367b53972ace842322d943bfe` | Clean, pushed candidate |
| Frontend integrated freeze | `C:\Users\Administrator\AppData\Local\Temp\nestystay-frontend-freeze-20261020` | `codex/final-frontend-freeze-20261020` | `2844cea0bb06b1633f11a55f5b6f58ddb391c1e3` | Clean local integration freeze |
| Backend integrated freeze | `C:\Users\Administrator\AppData\Local\Temp\nestystay-backend-freeze-20261020` | `codex/final-backend-freeze-20261020` | `07bccd28a916f7e20617d72396ec896a737c9787` | Clean local integration freeze |

Remote `main` at the time of the audit was frontend `07dc7bc32fc4d92dd4e4dc3cf1ff2887dc487489` and backend `3bd5e2171f89b15b9b195ceff3215856af6ba950`. The candidates are ahead of those bases and are not proof of deployment.

## 2. Static inventory

Counts were taken from the clean candidates, excluding generated `bin`/`obj` output.

| Area | Count | Evidence |
|---|---:|---|
| Frontend canonical screen definitions | 104 | `frontend/src/app/routeManifest.ts:158-284` |
| Frontend public definitions | 35 | `publicDefinition(screen(...))` entries |
| Frontend role-scoped definitions | 53 | `workspaceDefinition(screen(...))` entries |
| Frontend authenticated-all-role definitions | 16 | `authenticatedDefinition(screen(...))` entries |
| Frontend public/workspace roles | 9 | `routeManifest.ts:3-12` |
| Backend API controller files | 25 | `backend/src/NestyStay.Api/Controllers` |
| Backend HTTP action attributes | 483 | Controller source inventory |
| Backend `[Authorize]` attributes | 274 | Controller source inventory |
| Backend `[AllowAnonymous]` attributes | 10 | Controller source inventory |
| EF migration roots | 87 | `src/NestyStay.Infrastructure/Persistence/Migrations` |
| EF migration designer files | 84 | Same directory |
| EF model snapshots | 1 | `NestyStayDbContextModelSnapshot.cs` |
| EF `DbSet` properties | 240 | `NestyStayDbContext.cs` |
| In-process hosted worker registrations | 7 including email delivery | `Program.cs:97-102`, `DependencyInjection.cs:111` |

These are source inventories. They do not mean every route or API action is staging-verified.

## 3. End-to-end architecture

```text
Browser / hostname / path
        |
        v
Vite React SPA
  routeManifest.ts -> parseRoute -> getRouteAccess -> App render
  api.ts -> fetch(..., credentials: include, bearer/CSRF headers)
        |
        | /api/* same-origin in production or configured API base/proxy
        v
ASP.NET Core API
  forwarded headers -> security/exception middleware -> CORS
  authentication -> CSRF -> rate limiting -> authorization
  controllers -> application stores/services
        |
        v
Domain rules and resource authorization
        |
        v
EF Core/PostgreSQL, object storage, email, payment/identity adapters, workers
```

The frontend is a presentation/router layer. The backend remains authoritative for identity, membership, permissions, ownership, tenant isolation, payment state, and resource access.

## 4. Frontend URL architecture

### 4.1 Route source of truth

`src/app/routeManifest.ts:158-284` is the central registry. A screen definition contains an ID, canonical path, aliases/patterns, auth mode, role access, shell mode, product area, component key, and navigation metadata. `parseRoute()` matches normalized paths and returns a route object; unknown paths resolve to the branded 404 route.

The access flow is:

```text
window.location
  -> normalize path
  -> Admin hostname boundary
  -> manifest pattern match
  -> getRouteAccess(session, hydrating)
  -> public page, workspace page, sign-in, 403, or 404
```

Evidence: `routeManifest.ts:337-430` and `App.tsx:585-704`.

### 4.2 Public URL groups

| Area | Canonical and alias examples | Access |
|---|---|---|
| Discovery | `/`, `/explore`, `/explore/map`, `/properties/:propertyId` | Public shell; listing visibility is backend-controlled |
| Content/legal | `/about`, `/trust`, `/help`, `/contact`, `/terms`, `/privacy`, `/cookies`, `/refund-policy`, `/maintenance` | Public |
| Experiences/content | `/experiences`, `/experiences/:slug`, `/journal`, `/journal/:slug`, `/blog`, `/blog/:slug` | Public |
| Directories | `/directory/trades`, `/directory/businesses`, `/directory/providers/:slug`, `/hosts/:slug` | Public reads; writes are API-authorized |
| Authentication | `/login`, `/register`, `/sign-in`, `/login/{role}`, `/auth/*`, `/logout` | Public entry points; role context does not grant permission |
| Booking states | `/booking/:bookingId/{review,quote,identity,checkout,pending,success,failure,rejected,cancelled,invoice,receipt}` | URL is not proof of booking ownership |
| QR entry | `/gate`, `/gate/qr` | Token/validation surface; access policy is server-side |
| System states | `/401`, `/403`, `/404`, `/500`, `/loading`, `/empty/*` | Safe public/authenticated states |

### 4.3 Role workspace URL matrix

| Role | Primary URL | Representative protected URLs |
|---|---|---|
| Guest | `/guest-dashboard` | `/traveler/reservations`, `/traveler/qr`, `/traveler/favorites`, `/traveler/invoices`, `/traveler/identity`, `/traveler/reviews`, `/traveler/notifications`, `/messages`, `/profile` |
| Host | `/host-dashboard` | `/host/properties`, `/host/properties/new`, `/host/properties/edit`, `/bookings`, `/calendar`, `/host/wellness`, `/host/verification`, `/profile` |
| Property Manager | `/pm/dashboard` | `/pm/calendar`, `/pm/professional`, `/pm/invoices`, `/pm/maintenance`, `/pm/governance`, `/pm/documents`, `/pm/payments`, `/pm/vendors`, `/pm/community`, `/pm/subscription`, `/pm/team`, `/pm/inspections`, `/pm/cleaning`, `/pm/utilities`, `/pm/reports` |
| Owner | `/owner/dashboard` | `/owner/p0`, `/profile`, owner-scoped PM APIs |
| Wellness Officer | `/officer/wellness` | Officer assignments, reports, documents, and wellness workflows |
| Service Provider | `/directory/provider` | Provider profile, quotes, reviews, insights, onboarding |
| Local Business | `/directory/provider` | Shared provider UI with server-authorized Local Business role |
| Gate Guard | `/gate/guard` | `/gate/qr`, messages, alerts, profile |
| Super Admin | Configured Admin host, `/admin/sign-in` | `/admin`, `/admin/ops/*`, `/admin/kpis`, `/admin/reports` |

The backend role model also contains internal/domain roles such as `AssociationExecutive` and `Tenant`; they are not public role cards and need an explicit route/product decision before exposure.

### 4.4 Authentication hydration and redirects

`useAuth.ts:16-70` starts in an auth-hydrating state and asks the backend for `/api/auth/profile`, including when browser storage is empty. `App.tsx:587-680` returns a neutral “Checking your NestyStay session…” state while hydration is pending. This prevents a protected URL from briefly rendering the Guest workspace before the session is known.

`getRouteAccess()` then evaluates the server session's `activeRole` or roles against the route's `roleAccess`. A missing session produces a sign-in-required route with a same-origin return target; an invalid role produces the branded 403 route. Role cards and query-string workspace context are hints only.

### 4.5 Return-to safety

The route flow accepts internal paths only. External URLs, protocol-relative URLs, newline injection, unknown protected paths, and paths not allowed for the authenticated role must be rejected. The backend session and role remain authoritative after redirect.

### 4.6 Super Admin isolation

`routeManifest.ts:346-379` implements:

- `VITE_ADMIN_HOSTNAME` as the only configured Admin hostname;
- `/admin` and `/admin/*` resolving to 404 on a non-admin production hostname;
- non-Admin paths on the dedicated Admin host resolving to Admin sign-in;
- no silent fallback to `admin.nestystay.net` when the variable is unset;
- normalization of case, trailing dots, and ports.

This is a frontend boundary, not a security boundary. Backend Admin policies, DNS, TLS, CORS, and cookie configuration must agree with it.

### 4.7 URL architecture risks

1. `/gate/qr` is represented by a public compatibility definition while the authenticated Gate Guard workspace is `/gate/guard`. This is intentional for token validation compatibility, but it is easy to misunderstand; the API must distinguish anonymous token validation from authenticated Gate Guard scanning and property scope.
2. Booking, property, QR, user, and conversation IDs in URLs do not establish ownership. Every data read/write must re-check the authenticated principal and resource scope.
3. Several aliases map to the same component. They reduce migration friction but need a generated route catalog so an alias is not accidentally left outside navigation, analytics, or authorization tests.
4. Admin pages are still present in the same SPA bundle. Host routing hides them from the public host, but the backend and deployment boundary must not depend on source hiding.

## 5. Backend architecture

### 5.1 Project layering

The backend solution contains:

```text
NestyStay.Api
  HTTP controllers, auth handler, policies, middleware, health/OpenAPI
NestyStay.Application
  stores, services, provider abstractions, DTO/workflow contracts
NestyStay.Domain
  entities, enums, business rules, role/state definitions
NestyStay.Infrastructure
  EF Core/PostgreSQL, storage, email, payment adapters, workers
```

### 5.2 API route families

| Controller family | Base route | Responsibility |
|---|---|---|
| Auth | `/api/auth`, `/api/auth/passkeys` | Register/login, Google, profile, roles, workspaces, sessions, logout, reset, 2FA, passkeys |
| Properties | `/api/properties` | Listings, visibility, ownership, revisions, media upload/content |
| Bookings | `/api/bookings` | Quotes, bookings, capture, reject, refund, void/payment hold |
| Calendar | `/api/properties/{propertyId}/calendar`, `/api/calendar/export` | Feeds, sync, blocks, history, export tokens, ICS export |
| PM core | `/api/property-manager` | Manager workspace, properties, owners, invoices, payments, gates, documents, vendors |
| PM owner/P0 | `/api/property-manager/owner`, `/api/property-manager/p0` | Owner lifecycle, assignments, agreements, fees, statements, approvals, staff |
| PM professional | `/api/property-manager/professional`, `/professional-completion` | Operations, reservations, maintenance, work orders, cleaning, incidents |
| Wellness | `/api/wellness` | Officer onboarding, approval, visits, reports, documents, payouts, disputes |
| Directories | `/api/directories` | Public provider/business reads, profiles, quotes, reviews, moderation |
| Access/QR | `/api/access`, `/api/qr` | QR access records, scan history, canonical validation and compatibility paths |
| Admin/config | `/api/platform`, `/api/backend-schema`, `/api/backend-jobs`, `/api/badges-pricing`, health integrations | Admin diagnostics, configuration, jobs, moderation and pricing controls |
| Storage | `/api/storage` | Authorized/signed object access |
| Payments/insurance | `/api/insurance` plus booking/payment actions | Provider contracts; InsuraGuest lifecycle mutations are currently blocked by the referral-only contract |
| Webhooks | `/api/webhooks` | Stripe/provider callbacks, signature/shared-secret checks, replay/idempotency controls |

The largest maintainability concern is controller concentration. `PropertyManagerController` and `SpecCompletionController` contain many actions and mix several milestone surfaces; this increases policy-review cost even though the application/store layer is present.

### 5.3 Startup and middleware order

`src/NestyStay.Api/Program.cs` configures explicit-origin CORS with credentials, controllers, custom authentication, authorization policies, rate limiting, calendar-feed safety, OpenAPI, application/infrastructure services, optional workers, and optional migration-only mode.

Normal request order is:

```text
ForwardedHeaders
 -> ApiExceptionMiddleware
 -> production security headers
 -> optional HTTPS redirect
 -> CORS
 -> authentication
 -> CookieCsrfMiddleware
 -> rate limiting
 -> authorization
 -> controllers
```

Evidence: `Program.cs:56-140`.

Migration-only mode is controlled by `Database:ApplyMigrationsOnly` or `NESTYSTAY_MIGRATE_ONLY=true`, applies EF migrations, and exits before HTTP/background services. Normal API startup does not silently migrate.

### 5.4 Authentication, session, and authorization

The custom authentication handler in `Auth/AdminTokenAuthenticationHandler.cs` accepts a bearer access token or the `nestyStay.session` cookie, validates its signature, checks session activity, and re-checks active ordinary-role membership. `SessionCookieAuth.cs` issues an HttpOnly session cookie plus a readable CSRF double-submit cookie. Unsafe cookie requests require `X-CSRF-Token`.

Authorization is layered:

1. `[Authorize]` and role policies;
2. Admin permission policies from `AdminAuthorizationPolicies.cs`;
3. `CurrentUserContext` and signed-session activity;
4. `ResourceAuthorizationService` checks for signed-in user, host, expected owner, and expected host owner;
5. application/store tenant and resource checks;
6. provider/payment/workflow state checks.

This is a strong defense-in-depth design. The main architectural risk is the absence of a global fallback policy: a future controller action that omits `[Authorize]` and an internal authorization guard could become anonymous. Keep endpoint-inventory tests as a release gate and prefer explicit authorization on every non-public action.

### 5.5 Persistence and migrations

EF Core uses one `NestyStayDbContext` over PostgreSQL with 240 `DbSet` properties, 87 migration roots, 84 designer files, and one model snapshot. The newest candidate migrations are:

- `20261018165658_AddUserRoleMemberships`
- `20261019224554_AddPreferredRole`

The snapshot is a merge-conflict hotspot. The safe release sequence is: build the reviewed binary, generate/review an idempotent script, back up the target database, apply the script separately, verify the EF migration history, then start the compatible API/worker revision. Do not apply migrations from an ordinary API startup or directly to shared environments during an audit.

### 5.6 Providers and external integrations

| Integration | Source architecture | Verification boundary |
|---|---|---|
| Object storage | DI selects local private storage or `MinioStorageProvider` based on configuration; objects are outside web root and served through authorized/signed paths | Native disposable MinIO round trip passed; deployed provider is unverified |
| Email | DI selects file capture or Brevo when provider and `BREVO_ENABLED`/credentials are configured; queued delivery is worker-backed | Local outbox tests pass; real staging mailbox delivery is unverified |
| Stripe payments | Adapter supports authorization, capture, refund, and void semantics | No live financial transaction was run |
| Stripe Identity | Adapter and return URL configuration are present | Credentials and test-mode staging flow remain external gates |
| Stripe Connect/payouts | Provider-neutral/manual or configured adapter path | Payout acceptance remains external |
| Calendar | Bounded ICS fetch/parser, persisted feed/block/history, manual blocks, conflict checks, ICS export | Local contract/error tests pass; recurring deployed worker execution is unverified |
| InsuraGuest | Provider remains registered, but lifecycle mutations return referral-only/HTTP 410 in the current hardening candidate | Deployment/product acceptance must confirm the intended screen and API behavior |
| Webhooks | Raw/signature/shared-secret checks are enforced outside Development/Testing and include replay/idempotency protections | Authorized staging secrets and controlled provider callbacks are required for acceptance |

### 5.7 Worker topology

When `BackgroundJobs:Enabled` is true, the API registers six maintenance workers plus email delivery through infrastructure DI: milestone maintenance, calendar synchronization, document expiry, document export, subscription maintenance, professional maintenance, and email delivery. Deployment also supports a worker sidecar mode.

The deployment must select exactly one supported topology and monitor it. Running both without distributed coordination risks duplicate processing; running neither breaks email retries, calendar synchronization, exports, expiry, subscriptions, and maintenance. The health integration endpoint reports configuration/runtime status but does not prove that a deployed scheduler is making successful progress.

## 6. Deployment and environment architecture

### 6.1 Configuration boundaries

The backend configuration is environment-driven:

- `Cors:AllowedOrigins`/`NESTYSTAY_CORS_ALLOWED_ORIGINS` controls explicit credentialed origins;
- `Security:SessionCookieDomain`/`NESTYSTAY_SESSION_COOKIE_DOMAIN` controls cookie scope;
- `VITE_ADMIN_HOSTNAME` controls the frontend Admin host;
- `OBJECT_STORAGE_PROVIDER`/MinIO variables select object storage;
- `EMAIL_PROVIDER`, `BREVO_ENABLED`, and Brevo sender/API key select email;
- `BackgroundJobs:Enabled` or `WORKER_ENABLED` controls in-process jobs;
- `Database:ApplyMigrationsOnly`/`NESTYSTAY_MIGRATE_ONLY` controls migration-only execution.

The production example is configured for MinIO and Brevo, while the ordinary application settings default to local storage and provider-neutral behavior. This is safe only when the deployment secret/configuration layer explicitly overrides those defaults and the resulting runtime is checked.

### 6.2 Admin hostname and cookie risk

The dedicated Admin host requires:

1. DNS and TLS for the exact host;
2. frontend build variable `VITE_ADMIN_HOSTNAME` matching that host;
3. backend CORS allow-list entries if the API is cross-origin;
4. cookie domain/SameSite/Secure settings that match the intended session sharing model;
5. backend Admin role and permission checks;
6. a browser test proving ordinary users cannot use the Admin host.

The production example uses `.nestystay.net`, which allows a session cookie to be sent to both the public and Admin subdomains. That may be intentional for one session, but it is a security decision. If Admin isolation requires separate sessions, use host-only or distinct Admin cookies/audiences and test logout/revocation on both hosts.

### 6.3 Version and acceptance checks

Before staging or production acceptance, compare:

- frontend `/version.json` commit SHA;
- backend `/api/health/version` commit SHA and environment;
- database migration history against the binary;
- storage provider/readiness;
- email provider/queue/last success;
- worker runtime entries/last success;
- Admin DNS/TLS/host routing;
- authenticated role/resource matrix.

## 7. Verification evidence

### 7.1 Local integrated candidate

The final local freeze used a fresh disposable PostgreSQL database, local API/frontend services, and disposable MinIO. No shared database, staging, production, or live payment system was modified.

| Check | Result |
|---|---:|
| Frontend Vitest | 288 passed, 0 failed across 62 files |
| Frontend typecheck | Passed |
| Frontend production build | Passed |
| Frontend lint | 0 errors, 33 existing warnings |
| Backend full solution tests | 353 passed, 0 failed, 0 skipped |
| Backend Release build | 0 warnings, 0 errors |
| Native MinIO integration | 1 passed, 0 failed, 0 skipped |
| PostgreSQL two-instance concurrency | 1 passed, 0 failed, 0 skipped |
| Playwright | 266 scheduled; 255 passed; 0 failed; 11 explicit skips; 0 not run |

The authoritative Playwright summary is `artifacts/final-playwright-results-20261020-final2/FINAL_RUN_SUMMARY.md`. The retained HTML report is from an older reporter run and must not be used to infer the final totals.

The 11 explicit Playwright skips were environment/matrix gates: MinIO browser profile-photo configuration, real production smoke credentials, and intentional viewport/project restrictions. Native MinIO and local provider tests were run separately. No Playwright test failed or remained unrun in the final campaign.

### 7.2 Staging/public evidence boundary

A fresh non-mutating probe of `https://staging.nestystay.net` on 20 October 2026 returned:

| Endpoint | Result | Evidence |
|---|---|---|
| `/version.json` | HTTP 200 | Frontend commit `07dc7bc32fc4d92dd4e4dc3cf1ff2887dc487489`; built `2026-10-16T01:04:55.994Z` |
| `/api/health` | HTTP 200 | API status `ok`; PostgreSQL architecture reported |
| `/api/health/ready` | HTTP 200 | Database `ready`; storage `CONFIGURED` |
| `/api/health/version` | HTTP 200 | Backend commit `3bd5e2171f89b15b9b195ceff3215856af6ba950`; built `2026-10-15T19:47:01Z`; environment `Development` |
| `https://staging-admin.nestystay.net/` | HTTP 200 | Dedicated staging Admin hostname resolves and serves the frontend shell |
| `https://staging-admin.nestystay.net/admin/sign-in` | HTTP 200 | Dedicated staging Admin sign-in path serves the frontend shell |
| `https://staging.nestystay.net/admin` and `/admin/sign-in` | HTTP 200; rendered title `Not found · NestyStay` | Browser render shows the branded Not Found page on the public host |
| `https://admin.staging.nestystay.net/` | DNS failure | Expected alternate hostname is not configured |

The focused browser render also showed `Super Admin sign-in · NestyStay` at `staging-admin.nestystay.net/admin/sign-in` and the branded Not Found page at both public-host Admin paths. Each unauthenticated page emitted one `401` response for `/api/auth/profile`; this is the staging-main console-race observation and should be compared with the corrected local candidate before acceptance. Staging is live, ready, currently matches the recorded remote `main` heads, and has a reachable `staging-admin` hostname. It does **not** prove that the newer integrated candidates (`fb6edc4` frontend and `090d17d` backend) are deployed. `storage: CONFIGURED` is a readiness label, not proof of MinIO selection or object persistence. Provider details, worker progress, real mailbox delivery, MinIO persistence, calendar scheduling, moderation, Safety Center, and Gate Guard workflows remain unverified until authorized staging workflows are exercised against the deployed candidate.

## 8. Findings and priority

| ID | Priority | Finding | Evidence/status |
|---|---|---|---|
| ARCH-01 | P1 | Staging currently runs the recorded remote `main` heads, not the newer integrated candidates. | `/version.json` and `/api/health/version` both returned HTTP 200 with the remote `main` SHAs on 20 October 2026. |
| ARCH-02 | P1 | Admin isolation depends on coordinated frontend host routing and backend/deployment config. | Current staging render passes public-host denial and dedicated-host sign-in; DNS/TLS/CORS/cookie/session authorization acceptance remains required for the integrated candidate. |
| ARCH-13 | P1 | Staging main emits an unauthenticated `/api/auth/profile` 401 in the browser console on the public and Admin sign-in pages. | Focused browser run on 20 October 2026; compare against the local candidate’s hydration-race correction before acceptance. |
| ARCH-03 | P1 | `.nestystay.net` cookie scope can share sessions across public/Admin subdomains. | Requires an explicit security decision and browser verification. |
| ARCH-04 | P1 | Worker topology can be in-process or sidecar. | Select one, expose heartbeat/last-success, and prevent duplicate execution. |
| ARCH-05 | P1 | Storage and email provider choice is configuration-dependent. | Local MinIO passed; deployed MinIO/Brevo delivery unverified. |
| ARCH-06 | P1 | Migration snapshot and binary/schema sequencing are operationally sensitive. | Use reviewed idempotent script, backup, apply, verify, then start app. |
| ARCH-07 | P1 | No global deny-by-default authorization policy exists. | Keep static endpoint authorization tests mandatory for every release. |
| ARCH-08 | P2 | `/gate/qr` public compatibility and `/gate/guard` authenticated workspace can be confused. | Document contracts and keep QR/property scope server-enforced. |
| ARCH-09 | P2 | Large PM/spec controllers increase authorization review cost. | Refactor only as bounded future work; do not block current release. |
| ARCH-10 | P2 | Multiple aliases and internal screen routes increase catalog drift risk. | Generate an API/frontend route catalog in CI. |
| ARCH-11 | P2 | Internal roles exist without public workspace mapping. | Decide future routes for `AssociationExecutive`/`Tenant` before exposure. |
| ARCH-12 | P2 | InsuraGuest remains registered as a provider despite referral-only lifecycle blocking. | Keep the UI/API contract explicit and confirm the product decision. |

## 9. Recommended actions

1. Publish and merge only the reviewed frontend/backend candidates in their documented dependency order; do not use the dirty umbrella copies as release sources.
2. Set and verify the Admin hostname separately for staging and production; test public-host denial, Admin-host login, ordinary-user rejection, logout, and revocation.
3. Decide whether Admin sessions are intentionally shared. If not, change to host-only/distinct cookies in a separately approved security change.
4. Choose one worker topology and add operational monitoring for heartbeat, last success, retries, and duplicate execution.
5. Verify runtime storage and email with a controlled staging upload/download and one authorized QA mailbox; never infer provider behavior from labels alone.
6. Generate and review an idempotent migration script, back up the database, apply it separately, verify migration history, then start the compatible API/worker revision.
7. Keep the endpoint authorization inventory test as a CI release gate; fail builds when a new non-public action lacks explicit policy/guard coverage.
8. Generate a route/API catalog from the frontend manifest and OpenAPI and review aliases/legacy compatibility paths for drift.
9. Run the authorized staging matrix for each role and resource boundary: Guest, Host, Property Manager, Owner, Service Provider, Local Business, Wellness Officer, Gate Guard, and Super Admin.

## 10. Final conclusion

**PARTIALLY VERIFIED — ARCHITECTURE IS COHERENT, BUT DEPLOYMENT AND EXTERNAL-INTEGRATION GATES REMAIN.**

The URL architecture is centralized and role-aware. The backend architecture is layered and includes server-side authentication, permissions, ownership/resource checks, persistence, provider adapters, migrations, and workers. The integrated local candidate passed the recorded frontend, backend, MinIO, PostgreSQL, and Playwright checks.

The remaining uncertainty is operational, not a reason to assume success: the newer integrated candidate must be merged/deployed or explicitly compared against the current staging `main` revision, the Admin hostname/cookie model must be confirmed, the selected worker topology must be alive, MinIO and Brevo must be exercised in the target environment, and authorized staging workflows must verify real data persistence and cross-role denial.

No application source, migration history, shared database, staging environment, or production environment was modified while preparing this audit.

## 11. Current-state refresh — 21 October 2026

This refresh records the source and review state after the original architecture pass. It does not replace the clean integrated freeze used for the architectural inventory above.

### 11.1 Source-state ambiguity is explicit

The desktop workspace does not contain one clean, authoritative checkout:

| Checkout | Branch | Revision | State | Audit treatment |
|---|---|---|---|---|
| Root umbrella | `codex/frontend-hardening-release-candidate` | `cf31733fdf9b73e344403a35570aba74e5a65f44` (audited source baseline) | Dirty; evidence, screenshots, generated files, and subsequent audit-document commits | Not used as a release source |
| Nested frontend | `codex/fix-pm-owner-invitation-feedback` | `c0269a95ca7f283f2f0cbb38524a44decc4116a7` | Dirty; source and test changes | Not used as the clean architecture baseline |
| Nested backend | `codex/fix-minio-test-skip` | `a7a45f4bc819d215b7eaf62eb0e2d55189412481` | Dirty; generated test output | Not used as the clean architecture baseline |
| Clean frontend freeze | `codex/final-frontend-freeze-20261020` | `2844cea0bb06b1633f11a55f5b6f58ddb391c1e3` | Clean local integration freeze | **Authoritative frontend architecture baseline** |
| Clean backend freeze | `codex/final-backend-freeze-20261020` | `07bccd28a916f7e20617d72396ec896a737c9787` | Clean local integration freeze | **Authoritative backend architecture baseline** |

No dirty checkout was reset, staged, committed, or overwritten. The freeze remains the only internally consistent source used for the counts and architecture conclusions in this report.

### 11.2 Current route inventory confirmation

The clean frontend freeze contains 104 canonical screen definitions: 36 public definitions, 54 role/workspace definitions, and 17 authenticated-all-role definitions. The nine public role identities are Guest, Host, Property Manager, Owner, Service Provider, Local Business, Officer, Admin, and the shared authenticated account surface. Super Admin is represented by the Admin role but is constrained by the dedicated hostname boundary and server-side authorization.

The route tests cover:

- canonical and alias resolution;
- route-to-component and route-to-navigation consistency;
- public, hydrating, authenticated, wrong-role, and correct-role decisions;
- dedicated `/gate/qr` compatibility behavior;
- post-login routing for Property Manager, Admin, and multi-role sessions;
- safe return-to handling.

The principal URL risks remain the same: URL IDs are not authorization, `/gate/qr` is intentionally compatible with an anonymous/token flow while `/gate/guard` is a role workspace, and aliases require continued catalog tests to prevent drift.

### 11.3 Current backend inventory confirmation

The clean backend freeze contains 25 controllers, 483 HTTP action attributes, 274 `[Authorize]` attributes, 10 `[AllowAnonymous]` attributes, 87 migration roots, 84 migration designer files, one EF model snapshot, and 240 `DbSet` properties. It registers six maintenance workers from `Program.cs` when background jobs are enabled and the email delivery worker from infrastructure dependency injection.

The source still has no global deny-by-default authorization fallback. This is a design risk, not proof of an exposed endpoint: the release must retain explicit authorization inventory tests and review every new controller action.

### 11.4 Anonymous profile probe follow-up

The live staging-main browser evidence recorded on 20 October showed an unauthenticated `401` from `/api/auth/profile` on public and Admin sign-in pages. A focused backend successor was created:

| Item | Current state |
|---|---|
| PR | [Backend #74](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/pull/74) |
| Head | `f1cd71380cadf18d37fd6190329e9cbbed8d155a` |
| Base | `main` at `3bd5e2171f89b15b9b195ceff3215856af6ba950` |
| Intended change | Anonymous profile probe returns 204; invalid/revoked bearer or session-cookie credentials remain 401 |
| Remote CI | **Passed** in run [39519661475](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/actions/runs/39519661475): Application 98, Domain 6, Infrastructure 32 + 1 skipped, API 180 + 1 skipped; build, tests, and idempotent migration-script generation |
| Merge state | Content mergeable; branch/review policy remains Terrence-controlled |

This is an important architecture boundary: an absent credential may be treated as an anonymous probe, but an invalid or stale bearer/session credential must continue to receive 401. The first PR head failed this distinction; the current follow-up head restores it and is CI-green. It remains excluded from staging acceptance until Terrence merges and deploys it. The frontend dirty workspace contains an alternative local hydration change, but it is uncommitted and not part of this audit baseline.

### 11.5 Updated conclusion

The architecture remains **PARTIALLY VERIFIED**. The URL and backend boundaries are understandable and testable, and the clean integrated freeze has strong local evidence. The anonymous-profile successor is now CI-green, but the workspace still has multiple dirty revisions and staging/provider acceptance remains external. The correct next action is to merge/deploy the approved heads, then compare deployed SHAs and authenticated staging behavior against the clean release candidate.

### 11.6 Integrated backend compatibility recheck — 21 October 2026

The Backend #74 behavior was applied semantically in a disposable worktree on top of the clean backend freeze `07bccd28a916f7e20617d72396ec896a737c9787`; no reviewed branch or shared environment was modified.

| Check | Result |
|---|---|
| Focused auth/health/security regressions | 30 passed, 0 failed, 0 skipped |
| Full backend solution | 353 passed, 0 failed, 2 environment-gated skips |
| Release build | 0 warnings, 0 errors |
| `git diff --check` | Passed |
| Temporary worktree | `C:\Users\Administrator\AppData\Local\Temp\nestystay-backend-integrated-profile-20261021` |

This confirms that allowing a credential-free profile probe to return 204, while retaining 401 for invalid or revoked credentials, is compatible with the integrated role, payment, worker, migration, and API test candidate. The two skips are known environment-gated integration prerequisites; they are not failures. Staging remains unverified until the PR is merged and deployed.

### 11.7 Diagnostics exposure source verification — 21 October 2026

The known staging observation that unauthenticated requests reached development diagnostics was compared with the clean integrated backend freeze rather than treated as a current source defect. The freeze contains the two focused protections:

| Surface | Source evidence in the integrated freeze | Candidate behavior |
|---|---|---|
| Platform metadata | `src/NestyStay.Api/Controllers/PlatformController.cs`, commit `4b4d0b8` | Requires `Admin.SystemConfiguration` for `/api/platform/*` |
| Schema diagnostics | `src/NestyStay.Api/Controllers/BackendSchemaController.cs`, included with the diagnostics protection | Requires `Admin.SystemConfiguration`; payloads are additionally limited to Testing or explicitly allowed Development diagnostics |
| Job diagnostics | `src/NestyStay.Api/Controllers/BackendJobsController.cs`, commit `0b4dc5d` | Requires `Admin.SystemConfiguration`; returns `404` outside Testing or explicitly allowed Development diagnostics |

The corresponding controller/health tests are present in `tests/NestyStay.Api.Tests/BackendJobsControllerTests.cs`, `HealthEndpointTests.cs`, and `StorageAndPlatformControllerTests.cs`, and were included in the integrated backend campaign. This is not staging acceptance: staging currently reports backend `3bd5e2171f89b15b9b195ceff3215856af6ba950` with `environment=Development`, while the integrated freeze is `07bccd28a916f7e20617d72396ec896a737c9787`. Terrence must merge and deploy the protected candidate, then repeat anonymous and authorized diagnostics probes.
