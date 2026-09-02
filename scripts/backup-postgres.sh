#!/usr/bin/env bash
set -Eeuo pipefail

# Creates a PostgreSQL custom-format backup. The script deliberately refuses to
# guess an off-server destination: set RESTIC_REPOSITORY and RESTIC_PASSWORD_FILE
# explicitly when remote copies are approved.
: "${PGHOST:=localhost}"
: "${PGPORT:=5432}"
: "${PGDATABASE:?Set PGDATABASE}"
: "${PGUSER:?Set PGUSER}"
: "${BACKUP_ROOT:=./backups/postgres}"
: "${BACKUP_RETENTION_DAYS:=14}"

command -v pg_dump >/dev/null || { echo "pg_dump is required" >&2; exit 1; }
mkdir -p "$BACKUP_ROOT"
stamp="$(date -u +%Y%m%dT%H%M%SZ)"
target="$BACKUP_ROOT/${PGDATABASE}-${stamp}.dump"

umask 077
pg_dump --format=custom --no-owner --no-acl --file="$target"
test -s "$target"
sha256sum "$target" > "$target.sha256"

find "$BACKUP_ROOT" -type f -name '*.dump' -mtime "+$BACKUP_RETENTION_DAYS" -delete
find "$BACKUP_ROOT" -type f -name '*.sha256' -mtime "+$BACKUP_RETENTION_DAYS" -delete

if [[ -n "${RESTIC_REPOSITORY:-}" ]]; then
  : "${RESTIC_PASSWORD_FILE:?RESTIC_PASSWORD_FILE is required when RESTIC_REPOSITORY is set}"
  command -v restic >/dev/null || { echo "restic is required for remote backup" >&2; exit 1; }
  restic backup "$target" "$target.sha256" --tag nestystay-postgres
  restic forget --keep-daily 14 --keep-weekly 8 --keep-monthly 12 --prune
else
  echo "Local backup created at $target (remote backup not configured)."
fi
