# NestyStay Environment Configuration Audit

**Audit date:** 2026-09-22  
Values are intentionally omitted. This document lists names and status only.

## Storage variables consumed by the backend

| Name | Required when selected | Secret | Purpose | Default/alias | Current evidence |
|---|---:|---:|---|---|---|
| `OBJECT_STORAGE_PROVIDER` | Yes | No | Selects `minio`, `s3`, or local provider | Falls back to `NESTYSTAY_STORAGE_PROVIDER` | Local example selects MinIO; staging values not independently readable |
| `MINIO_ENDPOINT` | Yes for MinIO/S3 | No | S3-compatible endpoint | `Integrations:MinioEndpoint` | Present in example; live value UNKNOWN |
| `MINIO_ACCESS_KEY` | Yes | Yes | Storage access identity | `Integrations:MinioAccessKey` | Present in example; live value UNKNOWN |
| `MINIO_SECRET_KEY` | Yes | Yes | Storage access secret | `Integrations:MinioSecretKey` | Present in example; live value UNKNOWN |
| `MINIO_BUCKET` | Yes | No | Private bucket name | `Integrations:MinioBucket` | Present in example; live value UNKNOWN |
| `MINIO_REGION` | Yes/configured default | No | Signature region | `Integrations:MinioRegion` | Present in example |
| `MINIO_USE_SSL` | Yes for TLS deployment | No | HTTPS/TLS storage transport | `Integrations:MinioUseSsl` | Present in example; live value UNKNOWN |
| `MINIO_ROOT_USER` / `MINIO_ROOT_PASSWORD` | Only for MinIO server bootstrap | Yes | MinIO server bootstrap credentials | Used as access/secret fallback | Present in compose/example; never use root credentials for app access |
| `NESTYSTAY_STORAGE_LOCAL_ROOT` | Only local provider | No | Development local storage path | None | Not a production substitute |
| `NESTYSTAY_STORAGE_SIGNING_SECRET` | Signed local links | Yes | Local signed-link secret | None | Required only for local provider |

The production validator rejects missing/placeholder MinIO endpoint, bucket, access key, or secret when MinIO/S3 is selected. The provider performs private object access, size/path checks, signed downloads, and logical archive/delete behavior. Physical provider I/O was not executed because Docker/MinIO was unavailable.

## Email variables consumed by the backend

| Name | Required when Brevo selected | Secret | Purpose | Default/alias | Current evidence |
|---|---:|---:|---|---|---|
| `EMAIL_PROVIDER` | Yes | No | Selects email transport | Falls back to `NESTYSTAY_EMAIL_PROVIDER` | Example selects Brevo; staging transport not verified |
| `BREVO_ENABLED` | Yes to send through Brevo | No | Enables Brevo transport | None | Deployment owner reported key present but transport disabled |
| `BREVO_API_KEY` | Yes | Yes | Brevo server API credential | None | Key reported present on staging; value not inspected |
| `BREVO_SENDER_EMAIL` | Yes | No | Verified sender | None | Must be configured and verified in Brevo |
| `BREVO_SENDER_NAME` | Recommended | No | Sender display name | None | Example provided |
| `BREVO_REPLY_TO_EMAIL` | Optional | No | Reply-to address | None | Example provided |
| `BREVO_REPLY_TO_NAME` | Optional | No | Reply-to display name | None | Example provided |

## Stripe and identity variables

`EKYC_PROVIDER`, `STRIPE_IDENTITY_RETURN_URL`, `STRIPE_SECRET_KEY`, `STRIPE_PUBLISHABLE_KEY`, and `STRIPE_WEBHOOK_SECRET` are consumed by the application. Stripe Identity and webhook code are present; external/live verification was not performed locally. Test-mode use is environment-dependent and must not be treated as production payment readiness.

## Configuration conclusion

Local configuration files contain examples/placeholders only. Staging HTTP health and property endpoints returned 200, but private server values and the deployed SHA were not inspected. No secret values belong in this report or in source control.
