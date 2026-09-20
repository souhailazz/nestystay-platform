# NestyStay staging integration configuration

This is the configuration handoff for the current ASP.NET backend. It records the names consumed by the application; it does not contain credentials. Do not commit values from the staging server to Git.

## Stripe and Stripe Identity

The active application provider is Stripe Identity. The backend rejects any other eKYC selector in production.

| Variable | Required | Secret | Purpose | Example format | Default | Restart |
| --- | --- | --- | --- | --- | --- | --- |
| `EKYC_PROVIDER` | Yes for explicit staging selection | No | Selects the identity adapter | `stripe_identity` | `stripe_identity` | Yes |
| `STRIPE_SECRET_KEY` | Yes | Yes | Server-side Stripe API access | `sk_test_...` for staging test mode | None | Yes |
| `STRIPE_PUBLISHABLE_KEY` | Yes | No | Frontend Stripe.js/Elements key | `pk_test_...` for staging test mode | None | Yes |
| `STRIPE_IDENTITY_RETURN_URL` | Yes | No | HTTPS return URL after a verification session | `https://staging.nestystay.net/...` | None | Yes |
| `STRIPE_WEBHOOK_SECRET` | Yes | Yes | Signature validation for `/api/webhooks/stripe/raw` | `whsec_...` | None | Yes |

The configuration-key equivalents are `Integrations:EkycProvider`, `Integrations:StripeSecretKey`, `Integrations:StripePublishableKey`, `Integrations:StripeIdentityReturnUrl`, and `Webhooks:StripeSigningSecret`.

The raw webhook validates the Stripe signature, records provider event IDs persistently, acknowledges duplicates without applying the state transition twice, maps Stripe Identity `verified`, `canceled`, `requires_input`, and `processing` events, and sanitizes Identity payloads before persistence. A provider event that fails during processing is recorded as failed; the next delivery should use a new/retried provider delivery according to the provider’s retry policy.

## Object storage

### Actual provider selection

Set `OBJECT_STORAGE_PROVIDER=local` to select the private server-local file provider. `NESTYSTAY_STORAGE_PROVIDER` remains a backward-compatible selector alias, but `minio`, `s3`, and `r2` are no longer accepted in production. Files are stored on a persistent server volume outside the application/web root and are accessed only through the existing authorized API upload routes or short-lived HMAC-signed API download URLs.

### Exact variables

| Variable | Required | Secret | Purpose | Example format | Default | Restart |
| --- | --- | --- | --- | --- | --- | --- |
| `OBJECT_STORAGE_PROVIDER` | Yes on staging/production | No | Selects server-local storage | `local` | `local` | Yes |
| `NESTYSTAY_STORAGE_PROVIDER` | Optional legacy alias | No | Backward-compatible selector | `local` | None | Yes |
| `NESTYSTAY_STORAGE_LOCAL_ROOT` | Yes on staging/production | No | Absolute persistent directory outside the app/web root | `/var/lib/nestystay/storage` | Temporary local path in development | Yes |
| `NESTYSTAY_STORAGE_SIGNING_SECRET` | Yes on staging/production | Yes | At least 32 random bytes for private download URL HMACs | secret value | Session secret fallback in development only | Yes |

Configuration-key equivalents are `Integrations:StorageProvider`, `Integrations:LocalStorageRoot`, and `Integrations:LocalStorageSigningSecret`.

The service account must own the storage directory and have no broader filesystem access. On Linux, the provider applies `0700` to directories and `0600` to files where permitted; on Windows, apply an equivalent NTFS ACL to the service account and administrators only. Do not place the directory below the frontend build directory, `wwwroot`, a public uploads directory, or `/tmp` on staging/production. Nginx must not alias or serve this directory.

Uploads remain server-mediated: application routes perform authorization, size/type checks, magic-byte validation, hashing, and persistence. Download DTOs contain a short-lived HMAC-signed API URL; the raw file path is never returned. The API validates the signature and expiry before opening the file. There is no public directory listing, direct bucket URL, or anonymous upload route.

### Storage feature inventory

All server-mediated upload endpoints prepare a database metadata row, accept the content through the API, enforce size/content-type checks, calculate a SHA-256 hash, inspect magic bytes through `IFileSafetyScanner`, save the object, and expose an authorization-checked, time-limited download URL. The returned `uploadUrl` is an API content route in the current frontend; it is not an anonymous direct-to-bucket write.

| Feature | API route | Authorization | Key namespace | DB reference/download | Limit and types |
| --- | --- | --- | --- | --- | --- |
| Property photos | `POST/PUT /api/properties/{propertyId}/photos/...` | Host owner of property | `properties/{propertyId}/photos/...` | Property photo metadata; authorized property response/download | 10 MB; JPEG/PNG/WebP |
| Profile photos | `POST/PUT /api/auth/profile/photo/uploads/...` | Authenticated owner | `profiles/{userId}/photos/...` | Profile photo metadata; self-only download | 10 MB; JPEG/PNG/WebP |
| Wellness report photos | `POST/PUT /api/wellness/visits/{visitId}/report/photos/...` and admin completion route | Assigned officer or authorized admin | Visit/report photo namespace | Report photo metadata; scoped download | 10 MB; JPEG/PNG/WebP |
| Wellness officer documents | `POST/PUT /api/wellness/officers/{officerId}/documents/...` | Officer owner or admin | Officer document namespace | Document metadata; scoped download | 25 MB; PDF/JPEG/PNG |
| Directory provider documents | `POST/PUT /api/spec/directories/providers/{providerId}/documents/...` | Provider owner or admin | Provider document namespace | Provider document metadata; scoped download | 25 MB; PDF/JPEG/PNG |
| Conversation attachments | `POST/PUT /api/spec/messages/conversations/{id}/attachments/...` | Conversation participant | Conversation attachment namespace | Attachment metadata; participant-only download | 10 MB; JPEG/PNG/WebP/PDF |
| Admin case evidence | `POST/PUT /api/spec/admin/cases/{caseId}/evidence/...` | Admin with user-management permission | Admin case evidence namespace | Case evidence metadata; admin-only download | 10 MB; JPEG/PNG/WebP/PDF |
| PM documents/versions | `POST /api/property-manager/documents` and version routes | Manager or scoped owner | `property-manager/{managerId}/documents/...` | Document/version metadata; manager/owner-scoped download | 25 MB; PDF/JPEG/PNG |
| PM maintenance attachments | `/api/property-manager/maintenance/attachments` | Manager scope | Manager maintenance namespace | Attachment metadata; manager-scoped download | 25 MB; PDF/JPEG/PNG |
| PM vendor documents | PM document/version routes | Manager scope | Manager vendor namespace | Vendor/document metadata; manager-scoped download | 25 MB; PDF/JPEG/PNG |
| Export archives | PM export routes | Manager scope | Manager export namespace | Export metadata; owner-manager scoped download | 100 MB; ZIP |
| Insurance claim evidence | No storage upload route found | N/A | None | `EvidenceJson` is metadata supplied to the insurance claim record | Not implemented as object upload |

Stripe Identity owns the provider verification documents. NestyStay stores the verification/session references and sanitized webhook state; it does not copy Stripe’s identity documents into the NestyStay object store. The separate traveler identity-document routes are application-managed uploads and are not the Stripe Identity document store.

### Terrence’s staging steps

1. Create a persistent private directory on the server, owned by the .NET systemd service account, for example `/var/lib/nestystay/storage`.
2. Set `OBJECT_STORAGE_PROVIDER=local`, `NESTYSTAY_STORAGE_LOCAL_ROOT` to that absolute directory, and a generated `NESTYSTAY_STORAGE_SIGNING_SECRET` in the backend service environment. Keep values only on the server.
3. Confirm nginx/Caddy has no public alias for the storage directory and that the service account cannot write outside it.
4. Restart the .NET systemd service; environment changes are not read by the running process.
5. Confirm `/api/health/live` and `/api/health/ready` remain `200`.
6. Using the QA host/manager/officer/provider accounts, run one property-photo, PM-document, wellness-photo, and provider-document upload, then download each through its authorized UI. Verify a wrong-role download is denied.
7. Keep the storage volume and its backups persistent across backend restarts; do not use `/tmp` for staging data.

## Brevo transactional email

The transport and outbox are implemented. Selection is `EMAIL_PROVIDER=brevo`; `NESTYSTAY_EMAIL_PROVIDER` is a legacy fallback alias, not a second required variable. `BREVO_ENABLED` is an optional explicit gate; any value other than `false` allows Brevo when selected. The production validator now uses the same selector resolution as DI, so the modern selector cannot bypass required Brevo validation.

| Variable | Required | Secret | Purpose | Default | Restart |
| --- | --- | --- | --- | --- | --- |
| `EMAIL_PROVIDER` | Yes to enable | No | Preferred provider selector | `file` | Yes |
| `NESTYSTAY_EMAIL_PROVIDER` | Only as legacy fallback | No | Backward-compatible selector alias | None | Yes |
| `BREVO_ENABLED` | No, recommended explicit `true` | No | Explicit enable/disable gate | enabled unless `false` | Yes |
| `BREVO_API_KEY` | Yes for Brevo | Yes | Brevo API authentication | None | Yes |
| `BREVO_SENDER_EMAIL` | Yes for Brevo | No | Verified sender address | None | Yes |
| `BREVO_SENDER_NAME` | No | No | Sender display name | `NestyStay` | Yes |
| `BREVO_REPLY_TO_EMAIL` | No | No | Reply-to address | None | Yes |
| `BREVO_REPLY_TO_NAME` | No | No | Reply-to display name | None | Yes |

Configuration-key equivalents are `Email:Provider`, `Email:Brevo:Enabled`, `Email:Brevo:ApiKey`, `Email:Brevo:SenderEmail`, `Email:Brevo:SenderName`, `Email:Brevo:ReplyToEmail`, and `Email:Brevo:ReplyToName`.

Enablement is staging configuration only; no further application code change is required for the transport. The backend queues emails into the PostgreSQL outbox, uses idempotency keys, has a five-attempt retry/dead-letter policy, polls from the hosted worker every five seconds, and keeps the business transaction independent of delivery. The staging service must run with `BackgroundJobs:Enabled=true` (the application default is true) or an equivalent worker deployment. Brevo must have a verified sender/domain; retrieve the actual SPF/DKIM records from Brevo. DMARC is strongly recommended. Do not invent DNS records.

## Admin QA provisioning

Normal public registration rejects `Admin`. The supported method is startup bootstrap:

`NESTYSTAY_ADMIN_BOOTSTRAP_ENABLED`, `NESTYSTAY_ADMIN_BOOTSTRAP_EMAIL`, `NESTYSTAY_ADMIN_BOOTSTRAP_PASSWORD`, optional `NESTYSTAY_ADMIN_BOOTSTRAP_DISPLAY_NAME`, optional `NESTYSTAY_ADMIN_BOOTSTRAP_PERMISSIONS`, and optional `NESTYSTAY_ADMIN_BOOTSTRAP_REQUIRE_TOTP`.

The configuration equivalents are under `Security:AdminBootstrap:Enabled`, `Email`, `Password`, `DisplayName`, `Permissions`, and `RequireTotp`. Bootstrap is disabled by default and is idempotent. Terrence should only use it during a controlled staging window: set temporary values, restart the service, confirm exactly one QA Admin exists, disable/remove the bootstrap values, restart again, verify bootstrap is disabled, and rotate the temporary password through a secure channel. Do not enable public Admin registration and do not leave bootstrap credentials in the service environment.

## Gate Guard and QR

`GateGuard` exists in the domain enum and blueprint, but there is no registration, invite, role-assignment, policy, or authenticated Gate Guard portal path. Existing QR validation is a public/token-mediated workflow and can optionally record a signed-in user ID; it does not provision or enforce a Gate Guard role. The signed agreement places a dedicated gate-guard interface in the later mobile-app phase, while the current web phases describe QR entry and manager gate communications. This is therefore **UNCLEAR for current M4/M5 provisioning**, not silently implemented here. No code change was made. Obtain a written scope decision before adding a new provisioning flow.

## Maps and geocoding

The codebase is not map-free: public discovery and property details use OpenStreetMap iframe embeds, and the host wizard calls an OpenStreetMap-compatible Nominatim endpoint (overridable with `VITE_GEOCODER_URL`) for suggestions while allowing manual coordinates. This is a partial external-embed/geocoder implementation, not a first-party map/geocoding service or certification. No map code was added in this pass. Check the provider’s usage policy and attribution requirements before production scale.
