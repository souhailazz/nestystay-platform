# Email provider migration

## Decision

Runtime email is provider-neutral. Application workflows enqueue `EmailMessage` records through `IEmailSender`; `EmailDeliveryWorker` drains the PostgreSQL outbox and sends through the configured transport. The default local transport writes `.eml` files. Production should set `NESTYSTAY_EMAIL_PROVIDER=brevo` and configure a verified Brevo sender.

There is no Alibaba Mail, Aliyun mail, DirectMail or Alibaba SMTP runtime dependency. Alibaba eKYC is a separate identity integration and is intentionally preserved.

## Delivery lifecycle

`PENDING → PROCESSING → SENT` is the success path. Transient failures become `RETRYING` with exponential backoff; after five attempts they become `DEAD_LETTER`. The queue stores an idempotency key, attempt count, next-attempt timestamp, provider message id and safe error detail.

## Configuration

| Variable | Required | Purpose |
| --- | --- | --- |
| `NESTYSTAY_EMAIL_PROVIDER` | production | `brevo` or `file` |
| `BREVO_ENABLED` | production/staging | Set `true` for Brevo; set `false` only for controlled local/file capture |
| `BREVO_API_KEY` | Brevo | API key, secret manager only |
| `BREVO_SENDER_EMAIL` | Brevo | Verified sender address |
| `BREVO_SENDER_NAME` | optional | Display name (defaults to NestyStay) |
| `BREVO_REPLY_TO_EMAIL` | optional | Client-owned support mailbox (Zoho/Google or other) |
| `BREVO_REPLY_TO_NAME` | optional | Reply-to display name |
| `NESTYSTAY_EMAIL_OUTBOX_ROOT` | local/file | Private spool directory |

The Brevo transport uses `https://api.brevo.com/v3/smtp/email`, HTTPS and an API-key header. Secrets are never logged or placed in templates.

## Migration steps

1. Create/verify a Brevo account and sender/domain.
2. Store the API key in the server secret manager and set the variables above.
3. Run the API and worker with `BackgroundJobs__Enabled=false` on the API and `true` on the worker sidecar.
4. Send a staging test to an approved mailbox; verify `SENT`, provider id and audit logs.
5. Keep the file transport available for local development and incident replay only.
