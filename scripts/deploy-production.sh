#!/usr/bin/env bash
set -Eeuo pipefail

# Safe, repeatable release procedure for the single-VPS Docker Compose setup.
# The populated env file is supplied by the operator/secret manager and is
# never read from or written to Git. An initial deployment must explicitly set
# ALLOW_INITIAL_DEPLOY=true because there is no existing database to back up.

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
COMPOSE_FILE="${COMPOSE_FILE:-$ROOT_DIR/docker-compose.production.yml}"
ENV_FILE="${ENV_FILE:-$ROOT_DIR/.env}"
BACKUP_ROOT="${BACKUP_ROOT:-$ROOT_DIR/backups/postgres}"
RELEASE_SHA="${RELEASE_SHA:-}"
ALLOW_INITIAL_DEPLOY="${ALLOW_INITIAL_DEPLOY:-false}"

cd "$ROOT_DIR"
command -v docker >/dev/null || { echo "docker is required" >&2; exit 1; }
[[ -f "$COMPOSE_FILE" ]] || { echo "Compose file not found: $COMPOSE_FILE" >&2; exit 1; }
[[ -f "$ENV_FILE" ]] || { echo "Secret env file not found: $ENV_FILE" >&2; exit 1; }

if [[ -n "$RELEASE_SHA" ]]; then
  actual_sha="$(git rev-parse HEAD)"
  [[ "$actual_sha" == "$RELEASE_SHA" ]] || {
    echo "Checked-out SHA $actual_sha does not match RELEASE_SHA $RELEASE_SHA" >&2
    exit 1
  }
fi

compose=(docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE")
"${compose[@]}" config --quiet

# Keep a pre-deploy database backup. The explicit initial-deploy escape hatch
# prevents a first install from pretending that a backup exists.
"${compose[@]}" up -d postgres
postgres_id="$("${compose[@]}" ps -q postgres)"
for attempt in {1..30}; do
  health="$(docker inspect --format '{{.State.Health.Status}}' "$postgres_id" 2>/dev/null || true)"
  [[ "$health" == "healthy" ]] && break
  [[ "$attempt" == 30 ]] && { echo "PostgreSQL did not become healthy" >&2; exit 1; }
  sleep 2
done

if [[ "$ALLOW_INITIAL_DEPLOY" == "true" ]]; then
  echo "Initial deployment explicitly authorized; no pre-deploy database exists."
else
  mkdir -p "$BACKUP_ROOT"
  umask 077
  stamp="$(date -u +%Y%m%dT%H%M%SZ)"
  dump="$BACKUP_ROOT/pre-deploy-${stamp}.dump"
  "${compose[@]}" exec -T postgres sh -c 'pg_dump --format=custom --no-owner --no-acl -U "$POSTGRES_USER" -d "$POSTGRES_DB"' > "$dump"
  test -s "$dump"
  sha256sum "$dump" > "$dump.sha256"
  echo "Pre-deploy backup: $dump"
fi

"${compose[@]}" build api worker frontend

"${compose[@]}" run --rm -e Database__ApplyMigrationsOnly=true api

# The compose stack intentionally keeps the database private; migration-only
# mode runs the reviewed EF migrations from inside the private app network.
"${compose[@]}" up -d api worker frontend caddy
"${compose[@]}" ps
curl --fail --silent --show-error --retry 10 --retry-delay 2 http://127.0.0.1/api/health >/dev/null
echo "Deployment health check passed. Run the documented production smoke test before sign-off."
