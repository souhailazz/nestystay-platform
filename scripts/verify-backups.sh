#!/usr/bin/env bash
set -Eeuo pipefail

: "${BACKUP_ROOT:=./backups}"
: "${BACKUP_MAX_AGE_SECONDS:=90000}"
: "${BACKUP_STATUS_ROOT:=$BACKUP_ROOT/status}"

command -v sha256sum >/dev/null || { echo "sha256sum is required" >&2; exit 1; }
mkdir -p "$BACKUP_STATUS_ROOT"
stamp="$(date -u +%Y%m%dT%H%M%SZ)"
status_file="$BACKUP_STATUS_ROOT/verification-${stamp}.json"
failure=0
checked=0

verify_class() {
  local directory="$1"
  local pattern="$2"
  local latest
  latest="$(find "$directory" -maxdepth 1 -type f -name "$pattern" -printf '%T@ %p\n' 2>/dev/null | sort -nr | head -n1 | cut -d' ' -f2- || true)"
  if [[ -z "$latest" ]]; then
    failure=1
    return
  fi
  checked=$((checked + 1))
  [[ "$(date -u +%s)" -le "$(( $(stat -c %Y "$latest") + BACKUP_MAX_AGE_SECONDS ))" ]] || failure=1
  [[ -f "$latest.sha256" ]] || { failure=1; return; }
  sha256sum -c "$latest.sha256" >/dev/null || failure=1
}

verify_class "${POSTGRES_BACKUP_ROOT:-$BACKUP_ROOT/postgres}" '*.dump'
verify_class "${OBJECT_BACKUP_ROOT:-$BACKUP_ROOT/object-storage}" '*.tar.gz'
verify_class "${CONFIG_BACKUP_ROOT:-$BACKUP_ROOT/config}" '*.tar.gz'

status="SUCCEEDED"
[[ "$failure" == "0" && "$checked" == "3" ]] || status="FAILED"
printf '{"backup":"verification","status":"%s","timestamp":"%s","checkedClasses":%s,"exitCode":%s}\n' \
  "$status" "$stamp" "$checked" "$failure" > "$status_file"
if [[ "$failure" != "0" ]]; then
  echo "Backup verification failed; inspect $status_file" >&2
  exit 1
fi
echo "Backup verification passed; inspect $status_file"
