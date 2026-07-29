# Production Readiness Baseline

Generated: 2026-07-28

## Patch Application

Status: PARTIAL

The requested `nestystay-production-readiness-phase0.patch` was not present in the repository root or under `C:\Users\Administrator\.codex\attachments`. `git apply --check` could not be run against the named file because the file was absent. The phase-0 backend and frontend changes described in the handoff were recreated directly in the working tree while preserving existing changes.

## Network Verification

Status: CODE COMPLETE — TESTED LOCALLY

- `curl.exe -sS -D - https://api.nuget.org/v3/index.json -o NUL`: HTTP 200.
- `curl.exe -sS -D - https://cdn.playwright.dev/ -o NUL`: HTTP 400 from Azure Storage, non-403.
- `curl.exe -sS -D - https://playwright.azureedge.net/ -o NUL`: HTTP 307 redirect, non-403.

No `host_not_allowed` response was observed.

## .NET Toolchain

Status: CODE COMPLETE — TESTED LOCALLY

- `dotnet --info`: SDK `10.0.302`; runtimes include `Microsoft.AspNetCore.App 10.0.10` and `Microsoft.NETCore.App 10.0.10`.
- `dotnet --list-sdks`: `8.0.423`, `10.0.302`.
- `dotnet restore NestyStay.sln`: passed.
- `dotnet build NestyStay.sln --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet test NestyStay.sln`: passed after the administrator identity slice.

Fresh backend test totals:

- `NestyStay.Domain.Tests`: 5 passed, 0 failed, 0 skipped, duration 43 ms.
- `NestyStay.Application.Tests`: 20 passed, 0 failed, 0 skipped, duration 823 ms.
- `NestyStay.Infrastructure.Tests`: 11 passed, 0 failed, 0 skipped, duration 3 s.
- `NestyStay.Api.Tests`: 35 passed, 0 failed, 0 skipped.
- Total backend suite: 71 passed, 0 failed, 0 skipped.

## Frontend Toolchain

Status: CODE COMPLETE — TESTED LOCALLY

- `npm install`: passed, up to date; npm audit reported 2 high-severity vulnerabilities.
- `npm run typecheck`: passed.
- `npm test -- --run`: 23 passed across 2 files.
- `npm run build`: passed; Vite emitted a non-fatal circular chunk warning: `vendor -> react -> vendor`.

## Playwright

Status: CODE COMPLETE — TESTED LOCALLY

- `npx playwright install chromium`: passed.
- `npx playwright --version`: `1.61.1`.
- `npx playwright install --dry-run chromium`: Chrome for Testing `149.0.7827.55`, Playwright Chromium `v1228`.
- `npm run test:e2e`: 15 passed, 0 failed, duration 3.9 m.

The local PostgreSQL database was started on port `55432` before E2E execution because the backend returned connection refused when the database was down.

## Phase-0 Validation

Rate limit status: CODE COMPLETE — TESTED LOCALLY

- Typed `RateLimitExceededException` maps to HTTP 429.
- `Retry-After` is returned when retry information is available.
- Problem JSON includes stable code `rate_limit_exceeded`.
- Generic `InvalidOperationException` does not map to 429, including rate-limit-like messages.
- Booking creation limiter allows requests below and at threshold, rejects above threshold, separates guests, resets after the window, returns retry information, and resists concurrent bypass in tests.
- Production path uses persisted quota rows with a unique guest index inside a serializable transaction. In-memory tests use a serialized fallback and are not the production distributed-safety mechanism.

Security header status: CODE COMPLETE — TESTED LOCALLY

- Non-development responses include `Content-Security-Policy`, `Strict-Transport-Security`, `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy`, and `X-Frame-Options`.
- Development keeps OpenAPI usable without production security headers.
- CSP has no wildcard, no unrestricted `https:`, no `unsafe-eval`, no duplicate header, and allows only exact Stripe origins required by the current integration.

## Migrations

Status: CODE COMPLETE — TESTED LOCALLY

Created migration:

- `20260728174241_AddMilestoneBookingCreationRateLimit`

Migration discipline completed:

- Generated normally with `dotnet ef migrations add AddMilestoneBookingCreationRateLimit`.
- Inspected migration and model snapshot.
- Applied forward with `dotnet ef database update`.
- Rolled back to `20260723205152_AddMilestoneUserProfilePhotos`.
- Reapplied forward with `dotnet ef database update`.

EF CLI warning observed: tools `10.0.2` are older than runtime `10.0.9`.

## Administrator Identity Validation

Status: CODE COMPLETE — TESTED LOCALLY

- Named administrator accounts can be bootstrapped only through explicit bootstrap configuration or environment variables.
- Self-service registration rejects the `Admin` role.
- Admin login/session responses include persisted permission claims.
- Signed administrator bearer tokens resolve back to active persisted users and reject disabled, locked, or revoked sessions.
- TOTP-enabled administrator login requires challenge completion before issuing a usable admin session.
- Legacy static admin/operator tokens remain available for development and tests, but production rejects static admin tokens unless `Security:AllowLegacyAdminTokens=true` is explicitly configured.
- Privileged endpoint policies now use named permissions for booking, refund, payment, user, property moderation, officer, financial reporting, audit-log, system configuration, and super-admin workflows.
- Frontend admin routes require an admin session with the needed permission, admin token paste fields were removed from privileged flows, and controls are permission-aware.

Created migration:

- `20260728222012_AddMilestoneAdministratorIdentity`

Migration discipline completed:

- Generated normally with `dotnet ef migrations add AddMilestoneAdministratorIdentity`.
- Inspected migration and model snapshot.
- Applied forward with `dotnet ef database update`.
- Rolled back to `20260728174241_AddMilestoneBookingCreationRateLimit`.
- Reapplied forward with `dotnet ef database update`.

## Audit Attribution Validation

Status: CODE COMPLETE — TESTED LOCALLY

- The existing `milestone_audit_event.metadata_json` column now carries effective permission, request correlation ID, previous state JSON, and new state JSON.
- Existing admin case audit actions are enriched rather than duplicated.
- Privileged booking, payment, refund, officer, payout, and system-configuration mutations now record enriched audit events.
- The audit API projects the new metadata fields, and the frontend `AuditEvent` type accepts them.
- Admin payment capture now requires `payment_management`; host-owner payment capture remains supported.

Test coverage:

- Admin case resolution asserts actor role, effective permission, correlation ID, and previous/new state.
- Booking capture/refund asserts host/admin attribution and payment-state snapshots.
- Officer review and payout tests assert officer/financial permissions and state snapshots.
- Phase-two system-configuration tests assert pricebook, campaign, and founding-benefit audit entries.

Migration discipline:

- No migration was required because the existing `MetadataJson` column is now used for the richer attribution payload.

## Secrets Externalization Validation

Status: CODE COMPLETE — TESTED LOCALLY

- Tracked API appsettings no longer contain concrete database passwords, static admin/operator token hashes, session signing secrets, or TOTP protection keys.
- `.env.example` documents required environment keys with placeholders only.
- Production startup validation now requires externalized Postgres, session/TOTP, webhook, Stripe, eKYC, Cloudflare R2 upload/download, and InsuraGuest settings.
- Production startup validation rejects placeholder/development values, `nestystay_dev` or fixed `nestystay` database credentials, Stripe test keys, missing or invalid legacy admin token hashes when legacy tokens are explicitly enabled, and placeholder administrator bootstrap credentials.
- The GitHub Actions acceptance workflow no longer commits a fixed Postgres service password; its isolated CI database uses trust auth and passwordless connection strings.
- Frontend checkout no longer falls back to a mock Stripe publishable key; it renders a configuration error if `VITE_STRIPE_PUBLIC_KEY` is missing.
- Rotation candidates are documented in `secrets-rotation-candidates.md`.

Test coverage:

- Production validator tests assert missing session secret, short session/TOTP secrets, Stripe test secret/publishable/webhook keys, development database strings, placeholder shared secrets, missing/invalid legacy admin hashes, bootstrap placeholder rejection, and a valid production-shaped configuration.
- Frontend booking tests assert both configured Stripe checkout rendering and the missing-key fail-closed state.

Latest validation:

- `dotnet build NestyStay.sln --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet test NestyStay.sln`: 71/71 passed.
- `npm run typecheck`: passed.
- `npm test -- --run`: 24/24 passed.
- `npm run build`: passed with the existing Vite circular chunk warning.
- `npm run test:e2e`: 15/15 passed with local runtime env values supplied explicitly.

Migration discipline:

- No migration was required for secrets externalization.

## Booking/Payment State-Machine Validation

Status: CODE COMPLETE — TESTED LOCALLY

- Shared state-machine rules now guard verification, booking status, payment capture, refund, authorization, and payment webhook transitions across both PhaseOne store implementations.
- API state conflicts return HTTP 409 problem details with stable code `booking_state_conflict`; generic validation errors still return HTTP 400.
- Successful capture advances the internal booking status to `PaymentCaptured`; existing external milestone status remains `APPROVED` for current API/frontend compatibility.
- Refunds require captured payment and return typed conflicts when attempted too early.
- eKYC-start failure now cancels the payment state, matching explicit verification rejection and hold-expiry behavior.
- Stale failed/cancelled/captured Stripe webhook updates are ignored when they would downgrade an already captured or refunded payment.

Test coverage:

- API middleware asserts 409 problem details for `BookingStateConflictException`.
- API booking flow asserts conflicting verification webhook returns `booking_state_conflict`.
- In-memory PhaseOne tests assert typed capture/refund/verification conflicts and stale webhook no-downgrade behavior.
- EF persistence tests assert typed refund conflicts, persisted internal `PaymentCaptured` booking status, and stale webhook no-downgrade behavior.

Latest validation:

- `dotnet build NestyStay.sln --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet test NestyStay.sln`: 74/74 passed.
- `npm run test:e2e`: 15/15 passed.

Migration discipline:

- No migration was required for booking/payment state-machine hardening.

## Stripe Webhook And Event Persistence Validation

Status: CODE COMPLETE — TESTED LOCALLY

- Production Stripe webhook ingestion now uses `/api/webhooks/stripe/raw` and verifies the Stripe signature against the raw request body before JSON parsing.
- The legacy wrapper-shaped Stripe webhook path is rejected in production so it cannot bypass raw-body signature verification.
- Stripe events are persisted to `provider_event` before booking updates are applied.
- Provider events now store event id, event type, raw payload JSON, payload SHA-256, status, subject type/id, processing result, received timestamp, and processed timestamp.
- A unique provider-event index on kind, provider name, and event id provides persistent duplicate replay detection.
- Duplicate raw Stripe events return accepted duplicate responses without reapplying booking updates.
- Existing Stripe gateway calls continue to send idempotency keys for setup intents, payment intents, captures, and refunds.

Test coverage:

- Signed raw Stripe webhook fixtures assert invalid signature rejection, valid event acceptance, duplicate replay handling, and booking payment update mapping.
- Provider-event persistence tests assert duplicate event-id detection and processed-state persistence.
- Booking/payment state-machine tests assert stale failed webhooks do not downgrade captured payments.

Created migration:

- `20260728235336_AddProviderWebhookEventTracking`

Migration discipline completed:

- Generated normally with `dotnet ef migrations add AddProviderWebhookEventTracking`.
- Inspected migration and model snapshot.
- Applied forward with `dotnet ef database update`.
- Rolled back to `20260728222012_AddMilestoneAdministratorIdentity`.
- Reapplied forward with `dotnet ef database update`.

Latest validation:

- `dotnet build NestyStay.sln --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet test NestyStay.sln`: 76/76 passed.
- `npm run test:e2e`: 15/15 passed.
