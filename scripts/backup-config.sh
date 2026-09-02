#!/usr/bin/env bash
set -Eeuo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
: "${BACKUP_ROOT:=$ROOT_DIR/backups/config}"
: "${BACKUP_STATUS_ROOT:=$BACKUP_ROOT/status}"
mkdir -p "$BACKUP_ROOT" "$BACKUP_STATUS_ROOT"
stamp="$(date -u +%Y%m%dT%H%M%SZ)"
target="$BACKUP_ROOT/config-${stamp}.tar.gz"
status_file="$BACKUP_STATUS_ROOT/config-${stamp}.json"
files=(docker-compose.production.yml deploy/Caddyfile.production deploy/monitoring)

write_status() {
  local exit_code="$?"
  local status="FAILED"; [[ "$exit_code" == "0" ]] && status="SUCCEEDED"
  local size="0"; [[ -f "$target" ]] && size="$(wc -c < "$target" | tr -d ' ')"
  local checksum=""; [[ -f "$target.sha256" ]] && checksum="$(awk '{print $1}' "$target.sha256")"
  printf '{"backup":"configuration","status":"%s","timestamp":"%s","path":"%s","sizeBytes":%s,"sha256":"%s","exitCode":%s}\n' "$status" "$stamp" "$target" "$size" "$checksum" "$exit_code" > "$status_file"
}
trap write_status EXIT

cd "$ROOT_DIR"
tar -czf "$target" "${files[@]}"
test -s "$target"
sha256sum "$target" > "$target.sha256"
test -s "$target.sha256"
echo "Configuration backup created at $target"
