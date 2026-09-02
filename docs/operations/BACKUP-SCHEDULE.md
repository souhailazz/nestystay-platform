# Production backup schedule

Install the scripts under a client-owned service account and run them from the
repository checkout. Paths are configurable; the examples keep backup files
outside the application containers.

| UTC time | Command | Purpose |
| --- | --- | --- |
| 02:00 | scripts/backup-postgres.sh | PostgreSQL custom-format dump |
| 02:15 | scripts/backup-object-storage.sh | MinIO/private object archive |
| 02:20 | scripts/backup-config.sh | non-secret deployment configuration |
| 02:30 | scripts/verify-backups.sh | checksum and freshness verification |

Set BACKUP_ROOT, BACKUP_STATUS_ROOT, BACKUP_RETENTION_DAYS, and the
database/MinIO connection variables in the scheduler environment. Each script
returns non-zero on failure and writes a JSON status record containing status,
UTC timestamp, path, size, checksum and exit code. Prometheus can ingest the
latest status through the deployment metrics adapter; until then, schedule a
notification on non-zero exit and inspect the status directory.

RESTIC_REPOSITORY and RESTIC_PASSWORD_FILE remain optional until the client
provides an approved off-server encrypted destination.
