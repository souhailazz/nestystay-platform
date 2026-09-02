# Backup and restore runbook

## Backup

Set `PGHOST`, `PGPORT`, `PGDATABASE`, `PGUSER`, `PGPASSWORD` (or `.pgpass`) and `BACKUP_ROOT`, then run `scripts/backup-postgres.sh`. It creates a mode-600 custom-format dump, SHA-256 sidecar, and structured status JSON under `BACKUP_STATUS_ROOT`, prunes local files after `BACKUP_RETENTION_DAYS`, and optionally uploads with restic only when `RESTIC_REPOSITORY` and `RESTIC_PASSWORD_FILE` are explicitly set.

Back up uploaded files with `scripts/backup-object-storage.sh`. Mount the private application volume at `STORAGE_ROOT`, or configure an authenticated `mc` alias plus `MINIO_ALIAS`/`MINIO_BUCKET` for MinIO. It creates a checksummed archive and structured status JSON with the same retention and optional restic rules. `scripts/backup-config.sh` captures non-secret deployment/monitoring configuration, and `scripts/verify-backups.sh` checks freshness and sidecar hashes. Copy all backup classes to a different server or approved encrypted destination; a local volume is not an off-server backup.

If the remote destination is absent, the script intentionally stops at a local backup and reports that fact; it never guesses a bucket or silently claims off-server protection.

## Restore rehearsal

1. Provision an isolated PostgreSQL database and least-privilege restore operator.
2. Verify the checksum (`sha256sum -c file.dump.sha256`).
3. Restore with `pg_restore --clean --if-exists --no-owner --dbname <isolated-db> file.dump`.
4. Verify the object archive checksum, extract it into an isolated storage root, or use `mc mirror` into an isolated MinIO bucket; never overwrite production during rehearsal.
5. Apply pending EF migrations, start the API against the isolated database and storage root, then run health, auth, booking, notification and QR smoke tests.
6. Record timestamp, dump names, migration head, duration, row-count sanity, representative document download and operator sign-off.

Never restore over production without an approved incident/change record.
