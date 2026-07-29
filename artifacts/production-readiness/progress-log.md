# Production Readiness Progress Log

Generated: 2026-07-28

## 2026-07-28 Phase-0 Baseline

Completed:

- Confirmed `nestystay-production-readiness-phase0.patch` is absent from the repo and attachments.
- Recreated phase-0 backend behavior from the handoff:
  - Typed `RateLimitExceededException`.
  - HTTP 429 middleware mapping with `Retry-After`, `application/problem+json`, and stable code `rate_limit_exceeded`.
  - Persisted booking creation rate-limit table with unique `GuestUserId`.
  - Relational serializable transaction path for distributed-safe enforcement.
  - In-memory serialized fallback for local tests.
  - Production security headers middleware.
- Recreated phase-0 frontend behavior:
  - API errors carry stable code and retry seconds.
  - Booking review shows a human-readable retry message for rate limits.
  - Booking review route hydrates from an existing booking id instead of issuing conflicting default quotes.
- Stabilized Playwright:
  - Added booking E2E coverage for `BOOK-01` through `BOOK-10`.
  - Made booking E2E dates run-unique.
  - Switched route navigation to `domcontentloaded`.
  - Capped workers at 2 and increased timeout to 120 seconds for screenshot-heavy local runs.

Validation run:

- `curl.exe -sS -D - https://api.nuget.org/v3/index.json -o NUL`: HTTP 200.
- `curl.exe -sS -D - https://cdn.playwright.dev/ -o NUL`: HTTP 400, non-403.
- `curl.exe -sS -D - https://playwright.azureedge.net/ -o NUL`: HTTP 307, non-403.
- `dotnet restore NestyStay.sln`: passed.
- `dotnet build NestyStay.sln --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet test NestyStay.sln --no-build`: 66/66 passed.
- `npm install`: passed; 2 high-severity audit findings reported.
- `npm run typecheck`: passed.
- `npm test -- --run`: 23/23 passed.
- `npm run build`: passed with Vite circular chunk warning.
- `npx playwright install chromium`: passed.
- `npm run test:e2e`: 12/12 passed.

Migration run:

- `dotnet ef migrations add AddMilestoneBookingCreationRateLimit --project src\NestyStay.Infrastructure\NestyStay.Infrastructure.csproj --startup-project src\NestyStay.Api\NestyStay.Api.csproj --context NestyStayDbContext`: succeeded.
- `dotnet ef database update --project src\NestyStay.Infrastructure\NestyStay.Infrastructure.csproj --startup-project src\NestyStay.Api\NestyStay.Api.csproj --context NestyStayDbContext`: succeeded.
- `dotnet ef database update 20260723205152_AddMilestoneUserProfilePhotos --project src\NestyStay.Infrastructure\NestyStay.Infrastructure.csproj --startup-project src\NestyStay.Api\NestyStay.Api.csproj --context NestyStayDbContext`: rollback succeeded.
- `dotnet ef database update --project src\NestyStay.Infrastructure\NestyStay.Infrastructure.csproj --startup-project src\NestyStay.Api\NestyStay.Api.csproj --context NestyStayDbContext`: reapply succeeded.

Environmental notes:

- Local PostgreSQL was down initially; `/api/spec/seed` failed because `127.0.0.1:55432` refused connections.
- Started the existing PostgreSQL data directory with PostgreSQL 18 on port `55432`.
- After PostgreSQL startup, `/api/spec/seed` returned 200.
- Manual backend process on port `5019` was stopped after Playwright validation.
- Local PostgreSQL was stopped with `pg_ctl stop -m fast` after validation.

Next exact vertical slice:

Administrator identity. Audit the existing user, role, session, TOTP, token, and authorization infrastructure; then replace bearer-token admin workflows with named administrator accounts, bootstrap, sessions, permissions, backend policies, and frontend admin login/logout/session handling.

## 2026-07-28 Administrator Identity Slice

Completed:

- Added a named administrator bootstrap path guarded by explicit configuration or environment variables.
- Added persisted administrator status and permission fields to milestone users.
- Added a small administrator permission catalog covering super administration, booking, refunds, payments, users, property moderation, officers, financial reporting, audit-log access, and system configuration.
- Added authorization policies for privileged backend workflows and replaced coarse admin-token policies on existing controllers.
- Hardened legacy static administrator tokens so production only accepts them when `Security:AllowLegacyAdminTokens=true` is explicitly configured.
- Resolved signed administrator sessions through persisted users, requiring Admin role, Active status, no lockout, and no session revocation after token issue time.
- Blocked self-service `Admin` registration.
- Preserved TOTP challenge behavior for admin login and returned permissions with login/session responses.
- Updated frontend sessions to persist permissions, restored older sessions with empty permissions, guarded admin routes by role/permission, and removed visible admin-token paste fields from privileged admin flows.
- Wired the frontend login modal to complete login-time TOTP challenges.
- Stabilized screenshot-heavy Playwright smoke evidence by using a visible-main wait plus short network-idle settle and a 180 second per-test timeout.

Validation run:

- `dotnet build NestyStay.sln --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet test tests\NestyStay.Api.Tests\NestyStay.Api.Tests.csproj`: 35/35 passed.
- `dotnet test NestyStay.sln`: 71/71 passed.
- `dotnet ef migrations add AddMilestoneAdministratorIdentity --project src\NestyStay.Infrastructure\NestyStay.Infrastructure.csproj --startup-project src\NestyStay.Api\NestyStay.Api.csproj --context NestyStayDbContext`: succeeded.
- `dotnet ef database update --project src\NestyStay.Infrastructure\NestyStay.Infrastructure.csproj --startup-project src\NestyStay.Api\NestyStay.Api.csproj --context NestyStayDbContext`: succeeded.
- `dotnet ef database update 20260728174241_AddMilestoneBookingCreationRateLimit --project src\NestyStay.Infrastructure\NestyStay.Infrastructure.csproj --startup-project src\NestyStay.Api\NestyStay.Api.csproj --context NestyStayDbContext`: rollback succeeded.
- `dotnet ef database update --project src\NestyStay.Infrastructure\NestyStay.Infrastructure.csproj --startup-project src\NestyStay.Api\NestyStay.Api.csproj --context NestyStayDbContext`: reapply succeeded.
- `npm run typecheck`: passed.
- `npm test -- --run`: 23/23 passed.
- `npm run build`: passed with the existing Vite circular chunk warning.
- `npm run test:e2e`: 12/12 passed in 5.2 m.

Environmental notes:

- Local PostgreSQL was restarted on port `55432` for migration and Playwright validation.
- EF CLI warning observed again: tools `10.0.2` are older than runtime `10.0.9`.

Next exact vertical slice:

Officer zero-linkage controls if the referenced specification file is available; otherwise secrets externalization audit and rotation inventory.

## 2026-07-29 Audit Attribution Slice

Completed:

- Added `AuditActorContext`, `PrivilegedAuditRecord`, and `IPrivilegedAuditStore` while reusing the existing milestone audit event table.
- Extended `MilestoneAuditEvent.MetadataJson` usage to store effective permission, request correlation ID, previous state JSON, and new state JSON.
- Extended `AuditEventDto` and frontend `AuditEvent` typing with optional metadata fields.
- Enriched existing admin case audit events for case creation, resolution, evidence upload preparation, evidence upload, evidence quarantine, and evidence download.
- Added privileged audit writes for booking verification overrides, payment capture, payment refunds, officer approval/rejection/suspension/reactivation, wellness visit officer assignment/cancellation/admin completion/admin report-photo override, payout paid marking, pricebook updates, badge assignment expiration/suspension, campaign creation, and founding benefit updates.
- Tightened admin payment capture so an admin session must hold `payment_management`; host capture remains allowed for the owning host.
- Split the screenshot-heavy traveler Playwright evidence flow into route capture and profile-upload tests to keep local E2E stable.

Validation run:

- `dotnet build NestyStay.sln --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet test tests\NestyStay.Api.Tests\NestyStay.Api.Tests.csproj`: 35/35 passed with audit metadata assertions.
- `dotnet test NestyStay.sln`: 71/71 passed.
- `npm run typecheck`: passed.
- `npm test -- --run`: 23/23 passed.
- `npm run build`: passed with the existing Vite circular chunk warning.
- `npm run test:e2e`: 15/15 passed in 3.9 m.

Migration run:

- No migration was created; the audit attribution slice uses the existing `milestone_audit_event.metadata_json` column.

Environmental notes:

- Local PostgreSQL was restarted on port `55432` for Playwright validation and stopped afterward.
- `artifacts/production-readiness/officer-zero-linkage.md` is still absent; only the handoff reference exists.

Next exact vertical slice:

Secrets externalization audit and rotation inventory while the officer zero-linkage specification remains unavailable.

## 2026-07-29 Secrets Externalization Slice

Completed:

- Removed concrete database connection strings, static admin/operator token hashes, session signing secret, and TOTP protection key from tracked API appsettings.
- Replaced `.env.example` with a placeholder-only environment inventory for database, session signing, TOTP protection, admin bootstrap, legacy static tokens, webhooks, Stripe, storage, eKYC, insurance, and optional Google OAuth settings.
- Changed local infrastructure and design-time database fallbacks to omit a hard-coded password while still honoring `ConnectionStrings__Postgres`.
- Tightened production startup validation to require externalized Postgres, session/TOTP, webhook, Stripe, eKYC, Cloudflare R2 upload/download, and InsuraGuest settings.
- Added production validation for placeholder/development values, development database strings, Stripe test keys, legacy static admin hashes when explicitly enabled, and administrator bootstrap credentials.
- Removed the fixed tracked Postgres password from the GitHub Actions acceptance workflow by using trust auth for the isolated CI database service.
- Removed the frontend Stripe mock publishable-key fallback; checkout now fails closed when `VITE_STRIPE_PUBLIC_KEY` is missing and `.env.example` documents the required key.
- Added `secrets-rotation-candidates.md` with rotation actions for prior tracked development defaults and launch-time bootstrap credentials.

Validation run:

- `rg -n "Password=|AdminTokenSha256|OperatorTokenSha256|SessionTokenSecret|TotpSecretProtectionKey|dev-webhook-secret|whsec_|sk_(test|live)|pk_(test|live)|replace-with|development-only|POSTGRES_PASSWORD|ConnectionStrings" backend frontend .github artifacts/production-readiness ...`: remaining hits are tests, explicit validator rejection cases, non-production guarded fallbacks, or documented rotation candidates.
- `dotnet test tests\NestyStay.Api.Tests\NestyStay.Api.Tests.csproj`: 35/35 passed.
- `dotnet test NestyStay.sln`: 71/71 passed.
- `dotnet build NestyStay.sln --no-restore`: passed, 0 warnings, 0 errors.
- `npm run typecheck`: passed.
- `npm test -- --run`: 24/24 passed.
- `npm run build`: passed with the existing Vite circular chunk warning.
- `npm run test:e2e`: 15/15 passed in 3.5 m with local runtime env values supplied explicitly.

Migration run:

- No migration was created for the secrets externalization slice.

Environmental notes:

- Local PostgreSQL was started on port `55432` for Playwright with `ConnectionStrings__Postgres` supplied in the shell environment.
- Playwright also supplied `NESTYSTAY_ADMIN_TOKEN_SHA256`, `Security__EnableDevelopmentAuthCodes`, and `VITE_STRIPE_PUBLIC_KEY` explicitly because these values are no longer committed in tracked config.
- Local PostgreSQL was stopped with `pg_ctl stop -m fast` after validation.
- `artifacts/production-readiness/officer-zero-linkage.md` remains absent; only the handoff reference exists.

Next exact vertical slice:

Officer zero-linkage controls when the referenced specification file is provided. If that file remains unavailable and the P0 blocker is accepted as external/spec-blocked, continue into Sprint 2 booking/payment state machines.

## 2026-07-29 Booking/Payment State-Machine Slice

Completed:

- Added `BookingStateConflictException` with stable code `booking_state_conflict`, operation, current booking status, and current payment status.
- Added shared `BookingPaymentStateMachine` rules for verification resolution, booking status transitions, capture start, refund start, payment status transitions, and webhook transition applicability.
- Mapped booking/payment state conflicts to HTTP 409 problem details in API middleware while preserving generic validation as HTTP 400.
- Applied the shared state-machine rules to both the in-memory PhaseOne store and EF-backed milestone store.
- Internal booking status now advances from `Approved` to `PaymentCaptured` when payment capture succeeds, while the existing external milestone DTO status remains `APPROVED`.
- Refunds now raise typed conflicts unless the payment is captured, and missing capture references are treated as state conflicts.
- eKYC-start failure now marks payment state as `Cancelled`, matching explicit rejection and hold-expiry behavior.
- Stripe webhook application now ignores stale failed/cancelled/captured updates that would downgrade an already captured or refunded payment.

Validation run:

- `dotnet test NestyStay.sln`: 74/74 passed.
- `dotnet build NestyStay.sln --no-restore`: passed, 0 warnings, 0 errors.
- `npm run test:e2e`: 15/15 passed in 1.9 m with local runtime env values supplied explicitly.

Migration run:

- No migration was required for booking/payment state-machine hardening.

Environmental notes:

- Local PostgreSQL was started on port `55432` for Playwright and stopped afterward.
- `artifacts/production-readiness/officer-zero-linkage.md` remains absent.

Next exact vertical slice:

Stripe local implementation: idempotent payment creation, signed webhook fixtures, raw-body signature verification, webhook event persistence, and duplicate/out-of-order safety.

## 2026-07-29 Stripe Local Implementation Slice

Completed:

- Added `IProviderEventStore` and provider-event record/receipt/result contracts to the application provider abstractions.
- Added an EF-backed provider-event store and an in-memory application fallback.
- Extended `provider_event` with provider event id, payload SHA-256, status, subject attribution, processing result, and processed timestamp.
- Added a unique provider-event index on kind, provider name, and event id for persistent duplicate detection.
- Added `/api/webhooks/stripe/raw` for raw-body Stripe webhook processing; production Stripe wrapper webhooks are rejected so the signed raw-body path is required.
- Verified Stripe signatures against the raw request body before JSON parsing.
- Persisted raw Stripe event receipt before applying booking updates, then marked events as processed, ignored, failed, or duplicate.
- Kept existing Stripe idempotency-key usage for setup intents, payment intents, captures, and refunds.
- Added signed Stripe webhook fixtures for invalid signatures, valid raw events, duplicate replay, and payment intent booking updates.
- Added provider-event persistence tests for duplicate event ids and processed state.

Validation run:

- `dotnet test NestyStay.sln`: 76/76 passed.
- `dotnet build NestyStay.sln --no-restore`: passed, 0 warnings, 0 errors.
- `npm run test:e2e`: 15/15 passed in 3.4 m with local runtime env values supplied explicitly.

Migration run:

- `dotnet ef migrations add AddProviderWebhookEventTracking --project src\NestyStay.Infrastructure\NestyStay.Infrastructure.csproj --startup-project src\NestyStay.Api\NestyStay.Api.csproj --context NestyStayDbContext`: succeeded.
- `dotnet ef database update --project src\NestyStay.Infrastructure\NestyStay.Infrastructure.csproj --startup-project src\NestyStay.Api\NestyStay.Api.csproj --context NestyStayDbContext`: succeeded.
- `dotnet ef database update 20260728222012_AddMilestoneAdministratorIdentity --project src\NestyStay.Infrastructure\NestyStay.Infrastructure.csproj --startup-project src\NestyStay.Api\NestyStay.Api.csproj --context NestyStayDbContext`: rollback succeeded.
- `dotnet ef database update --project src\NestyStay.Infrastructure\NestyStay.Infrastructure.csproj --startup-project src\NestyStay.Api\NestyStay.Api.csproj --context NestyStayDbContext`: reapply succeeded.

Environmental notes:

- EF CLI warning observed again: tools `10.0.2` are older than runtime `10.0.9`.
- Local PostgreSQL was started on port `55432` for migration and Playwright validation and stopped afterward.
- Playwright supplied `ConnectionStrings__Postgres`, `NESTYSTAY_ADMIN_TOKEN_SHA256`, `Security__EnableDevelopmentAuthCodes`, and `VITE_STRIPE_PUBLIC_KEY` explicitly.

Next exact vertical slice:

Cancellation/refund/ledger: Sprint 3 cancellation policies, refund states, immutable host ledger, balances, and reconciliation.
