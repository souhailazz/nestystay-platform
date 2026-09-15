# PostgreSQL + pgAdmin4 Local Setup

Milestone 1/2 backend persistence targets local PostgreSQL database `nestystay_dev`.

## Connection String

Default local connection string:

```text
Host=localhost;Port=5432;Database=nestystay_dev;Username=nestystay;Password=nestystay
```

Override it without changing code by setting:

```powershell
$env:ConnectionStrings__Postgres="Host=localhost;Port=5432;Database=nestystay_dev;Username=nestystay;Password=<your-password>"
```

## Create Database In pgAdmin4

1. Open pgAdmin4 and connect to your local PostgreSQL server.
2. Open Query Tool as a superuser.
3. Run:

```sql
CREATE ROLE nestystay WITH LOGIN PASSWORD 'nestystay';
CREATE DATABASE nestystay_dev OWNER nestystay;
GRANT ALL PRIVILEGES ON DATABASE nestystay_dev TO nestystay;
```

If the role already exists, update its password instead:

```sql
ALTER ROLE nestystay WITH PASSWORD 'nestystay';
```

## Apply EF Migrations

From repo root:

```powershell
dotnet ef database update --project backend/src/NestyStay.Infrastructure/NestyStay.Infrastructure.csproj --startup-project backend/src/NestyStay.Api/NestyStay.Api.csproj
```

An offline SQL script was also generated at:

```text
artifacts/milestone_1_2_persistence_migration.sql
```

## Admin API Token

Admin mutation endpoints require `Authorization: Bearer <token>` where the SHA-256 hash of the token is configured.

Generate a local token/hash:

```powershell
$token = [Guid]::NewGuid().ToString("N")
$hash = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($token))).ToLower()
$token
$hash
```

Set the hash:

```powershell
$env:NESTYSTAY_ADMIN_TOKEN_SHA256="<hash>"
```

Use the token in requests:

```text
Authorization: Bearer <token>
```

Non-admin/operator token hash can be configured with `NESTYSTAY_OPERATOR_TOKEN_SHA256` for authenticated-but-not-admin callers.
