# Milestone 1/2 Persistence, Admin Auth, Security Proof

Date: 2026-06-21

## Backend Changes Completed

- Replaced API/runtime Phase 1 and Phase 2 in-memory stores with EF Core stores backed by `NestyStayDbContext`.
- Added PostgreSQL migration `20260621163343_MilestonePersistentStores`.
- Added persistent milestone tables:
  - `milestone_user`
  - `milestone_two_factor_challenge`
  - `milestone_property`
  - `milestone_booking`
  - `milestone_pricebook_entry`
  - `milestone_badge_definition`
  - `milestone_badge_assignment`
  - `milestone_badge_renewal`
  - `milestone_campaign`
  - `milestone_campaign_enrollment`
  - `milestone_founding_benefit`
- Added indexes/unique constraints for email, 2FA challenge IDs, property/date booking lookup, pricebook keys, campaign keys, campaign enrollments, and founding benefit property ownership.
- Switched local PostgreSQL defaults to `nestystay_dev`.
- Added policy-based admin authorization for admin mutation endpoints.
- Fixed NU1903 by pinning `System.Security.Cryptography.Xml` to patched `10.0.6`.
- Generated offline SQL migration script: `artifacts/milestone_1_2_persistence_migration.sql`.

## Admin-Protected Endpoints

These now require the `AdminOnly` policy:

- `PUT /api/badges-pricing/pricebook/{key}`
- `POST /api/badges-pricing/badges/assignments/{assignmentId}/expire`
- `POST /api/badges-pricing/badges/assignments/{assignmentId}/suspend`
- `POST /api/badges-pricing/campaigns`
- `POST /api/badges-pricing/founding-benefits`

Authentication uses bearer token SHA-256 hashes from configuration/environment:

- `Security:AdminTokenSha256` or `NESTYSTAY_ADMIN_TOKEN_SHA256`
- `Security:OperatorTokenSha256` or `NESTYSTAY_OPERATOR_TOKEN_SHA256`

## Verification Commands

Restore:

```text
dotnet restore backend/NestyStay.sln
```

Result: success, no NU1903 warning after pinning `System.Security.Cryptography.Xml` to `10.0.6`.

Build:

```text
dotnet build backend/NestyStay.sln --no-restore
```

Result: success, 0 warnings, 0 errors.

Tests:

```text
dotnet test backend/NestyStay.sln --no-build
```

Result: success, 32 passed, 0 failed.

Vulnerability audit:

```text
dotnet list backend/NestyStay.sln package --vulnerable --include-transitive
```

Result: every project reported no vulnerable packages with current sources.

Migration generation:

```text
dotnet ef migrations add MilestonePersistentStores --project backend/src/NestyStay.Infrastructure/NestyStay.Infrastructure.csproj --startup-project backend/src/NestyStay.Api/NestyStay.Api.csproj --output-dir Persistence/Migrations
```

Result: success.

Migration list:

```text
dotnet ef migrations list --project backend/src/NestyStay.Infrastructure/NestyStay.Infrastructure.csproj --startup-project backend/src/NestyStay.Api/NestyStay.Api.csproj
```

Result: build succeeded and migrations were listed:

- `20260501214102_InitialBackendSchema`
- `20260621163343_MilestonePersistentStores`

Local database status could not be determined because PostgreSQL rejected the default `nestystay` password for `nestystay_dev` with `28P01: password authentication failed for user "nestystay"`.

Database update:

```text
dotnet ef database update --project backend/src/NestyStay.Infrastructure/NestyStay.Infrastructure.csproj --startup-project backend/src/NestyStay.Api/NestyStay.Api.csproj
```

Result: build succeeded, then local PostgreSQL rejected authentication for `nestystay`. Apply `artifacts/postgres_pgadmin4_local_setup.md`, then rerun the command.

Offline SQL script:

```text
dotnet ef migrations script --project backend/src/NestyStay.Infrastructure/NestyStay.Infrastructure.csproj --startup-project backend/src/NestyStay.Api/NestyStay.Api.csproj --output artifacts/milestone_1_2_persistence_migration.sql
```

Result: success. Script contains the `milestone_*` table creation and indexes.

## Test Proof Added

- Infrastructure persistence tests prove Phase 1 state survives new DbContext/store instances:
  - registration
  - login + 2FA challenge verification
  - property seeding/listing
  - pending booking hold
  - Alibaba eKYC approval
  - Stripe authorization after approval
  - notifications/timeline persistence
- Infrastructure persistence tests prove Phase 2 state survives new DbContext/store instances:
  - pricebook update
  - badge assignment
  - renewal queue
  - campaign persistence
  - founding benefit persistence
- API tests prove admin mutation endpoints return:
  - `401 Unauthorized` with no token
  - `403 Forbidden` with non-admin token
  - `200 OK` with admin token

## Milestone Status

Backend-first Milestone 1/2 persistence and admin hardening is complete in code and test proof. The only local action left is configuring PostgreSQL credentials/database in pgAdmin4 so the generated EF migration can be applied to your machine.
