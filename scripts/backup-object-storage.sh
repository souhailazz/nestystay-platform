#!/usr/bin/env bash
set -Eeuo pipefail

# Back up the private application object volume. For a MinIO deployment, set
# MINIO_ALIAS and MINIO_BUCKET after configuring `mc`; otherwise the script
# archives the mounted local storage directory. No credentials are embedded.
: "${STORAGE_ROOT:=./storage}"
: "${BACKUP_ROOT:=./backups/object-storage}"
: "${BACKUP_RETENTION_DAYS:=14}"

command -v tar >/dev/null || { echo "tar is required" >&2; exit 1; }
mkdir -p "$BACKUP_ROOT"
stamp="$(date -u +%Y%m%dT%H%M%SZ)"
target="$BACKUP_ROOT/object-storage-${stamp}.tar.gz"

umask 077
if [[ -n "${MINIO_ALIAS:-}" ]]; then
  : "${MINIO_BUCKET:?MINIO_BUCKET is required with MINIO_ALIAS}"
  command -v mc >/dev/null || { echo "mc is required for MinIO backup" >&2; exit 1; }
  staging="$BACKUP_ROOT/.minio-${stamp}"
  trap 'rm -rf "$staging"' EXIT
  mkdir -p "$staging"
  mc mirror --overwrite "$MINIO_ALIAS/$MINIO_BUCKET" "$staging"
  tar -czf "$target" -C "$staging" .
else
  [[ -d "$STORAGE_ROOT" ]] || { echo "Storage root does not exist: $STORAGE_ROOT" >&2; exit 1; }
  tar -czf "$target" -C "$STORAGE_ROOT" .
fi

test -s "$target"
sha256sum "$target" > "$target.sha256"
find "$BACKUP_ROOT" -type f -name '*.tar.gz' -mtime "+$BACKUP_RETENTION_DAYS" -delete
find "$BACKUP_ROOT" -type f -name '*.sha256' -mtime "+$BACKUP_RETENTION_DAYS" -delete

if [[ -n "${RESTIC_REPOSITORY:-}" ]]; then
  : "${RESTIC_PASSWORD_FILE:?RESTIC_PASSWORD_FILE is required when RESTIC_REPOSITORY is set}"
  command -v restic >/dev/null || { echo "restic is required for remote backup" >&2; exit 1; }
  restic backup "$target" "$target.sha256" --tag nestystay-object-storage
  restic forget --keep-daily 14 --keep-weekly 8 --keep-monthly 12 --prune
else
  echo "Local object-storage backup created at $target (remote backup not configured)."
fi
