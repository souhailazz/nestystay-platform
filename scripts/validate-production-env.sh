#!/usr/bin/env bash
set -Eeuo pipefail

is_placeholder() {
  local value="$1"
  [[ -z "$value" || "$value" == *replace-with* || "$value" == *example.* || "$value" == *your-* || "$value" == *localhost* ]]
}

check() {
  local name="$1"
  local value="$2"
  if is_placeholder "$value"; then
    printf '%s: missing\n' "$name"
  else
    printf '%s: configured\n' "$name"
  fi
}

check PUBLIC_APP_URL "${PUBLIC_APP_URL:-}"
check POSTGRES_DB "${POSTGRES_DB:-}"
check POSTGRES_USER "${POSTGRES_USER:-}"
check POSTGRES_PASSWORD "${POSTGRES_PASSWORD:-}"
check REDIS_PASSWORD "${REDIS_PASSWORD:-}"
check MINIO_ENDPOINT "${MINIO_ENDPOINT:-}"
check MINIO_BUCKET "${MINIO_BUCKET:-}"
check MINIO_ACCESS_KEY "${MINIO_ACCESS_KEY:-}"
check MINIO_SECRET_KEY "${MINIO_SECRET_KEY:-}"
check NESTYSTAY_SESSION_TOKEN_SECRET "${NESTYSTAY_SESSION_TOKEN_SECRET:-}"
check NESTYSTAY_TOTP_SECRET_PROTECTION_KEY "${NESTYSTAY_TOTP_SECRET_PROTECTION_KEY:-}"
check NESTYSTAY_WEBHOOK_SHARED_SECRET "${NESTYSTAY_WEBHOOK_SHARED_SECRET:-}"
check CSRF "${CSRF_MODE:-api-issued-double-submit}"
check NESTYSTAY_DOMAIN "${NESTYSTAY_DOMAIN:-}"
check BACKUP_CONFIGURATION "${BACKUP_ROOT:-}"
check OBJECT_STORAGE_PROVIDER "${OBJECT_STORAGE_PROVIDER:-minio}"
check EMAIL_PROVIDER "${EMAIL_PROVIDER:-${NESTYSTAY_EMAIL_PROVIDER:-brevo}}"
check BUSINESS_MAIL_PROVIDER "${BUSINESS_MAIL_PROVIDER:-zoho}"
check EKYC_PROVIDER "${EKYC_PROVIDER:-alibaba}"
check PAYMENT_PROVIDER "${PAYMENT_PROVIDER:-stripe}"
check PAYOUT_MODE "${PAYOUT_MODE:-manual}"

if [[ "${BREVO_ENABLED:-true}" != "false" && "${EMAIL_PROVIDER:-${NESTYSTAY_EMAIL_PROVIDER:-brevo}}" == "brevo" ]]; then
  check BREVO_API_KEY "${BREVO_API_KEY:-}"
  check BREVO_SENDER_EMAIL "${BREVO_SENDER_EMAIL:-}"
fi
if [[ "${PAYMENT_PROVIDER:-stripe}" == "stripe" ]]; then
  check STRIPE_SECRET_KEY "${STRIPE_SECRET_KEY:-}"
  check STRIPE_PUBLISHABLE_KEY "${STRIPE_PUBLISHABLE_KEY:-}"
  check STRIPE_WEBHOOK_SECRET "${STRIPE_WEBHOOK_SECRET:-}"
fi
if [[ "${EKYC_PROVIDER:-alibaba}" == "alibaba" ]]; then
  check ALIBABA_EKYC_TRANSACTION_URL_BASE "${ALIBABA_EKYC_TRANSACTION_URL_BASE:-}"
fi
if [[ -n "${RESTIC_REPOSITORY:-}" ]]; then
  check RESTIC_REPOSITORY "${RESTIC_REPOSITORY:-}"
  check RESTIC_PASSWORD_FILE "${RESTIC_PASSWORD_FILE:-}"
else
  printf 'OFF_SERVER_BACKUP: missing\n'
fi
