# NestyStay backup and restore certification

## Current result

**NOT COMPLETE for production.** The repository contains PostgreSQL/object-storage/config backup scripts and a restore runbook. A local isolated rehearsal is documented, but an off-server encrypted backup and restore on the client infrastructure has not been independently evidenced here.

## Required evidence

| Check | Result | Exact action |
|---|---|---|
| PostgreSQL dump | READY / local script | Run `scripts/backup-postgres.sh` with server-only variables and record checksum/status JSON. |
| Object archive | READY / local script | Run `scripts/backup-object-storage.sh` against the private storage root or MinIO alias. |
| Config backup | READY / local script | Run `scripts/backup-config.sh`; confirm secrets are excluded. |
| Off-server destination | PENDING | Configure approved encrypted restic repository or equivalent separate host. |
| Encryption/custody | PENDING | Confirm password-file custody and access separation. |
| Scheduled retention | PENDING | Configure schedule and retention; inspect a successful scheduled run. |
| Isolated restore | PENDING on client host | Verify SHA-256, restore PostgreSQL into an isolated database, extract storage into an isolated root and apply migrations. |
| Application validation | PENDING on restored host | Run health, auth, booking, notification and QR smoke checks. |
| Recovery/alerting | PENDING | Simulate failed backup and verify alert reaches the intended operator. |

Never restore over production without an approved change/incident record.
