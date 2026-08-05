# Secrets Rotation Candidates

Generated: 2026-07-29

This inventory covers secret-like values found or implied during the tracked-config audit. No real production secret values were found in tracked appsettings, but several development defaults were previously committed and must not be reused outside local or isolated CI environments.

## Rotate Before Production

| Setting | Candidate | Action |
| --- | --- | --- |
| `ConnectionStrings:Postgres` / `ConnectionStrings__Postgres` | Prior tracked development connection strings used the `nestystay_dev` database and a fixed local password. | Provision a production-only database user/password in the deployment secret manager. Rotate any shared environment that reused the prior development password. |
| `Security:AdminTokenSha256` / `NESTYSTAY_ADMIN_TOKEN_SHA256` | A static administrator token hash was previously present in development appsettings. | Prefer named administrator sessions. If legacy static tokens are explicitly enabled, issue a new random token, store only its SHA-256 hash, and remove it after migration. |
| `Security:OperatorTokenSha256` / `NESTYSTAY_OPERATOR_TOKEN_SHA256` | A static operator token hash was previously present in development appsettings. | Issue a new random token only if the legacy operator path is still required outside development. |
| `Security:SessionTokenSecret` / `NESTYSTAY_SESSION_TOKEN_SECRET` | A development-only signing secret was previously present in development appsettings. | Replace with at least 32 bytes of production entropy and invalidate sessions from any environment that used the development value. |
| `Security:TotpSecretProtectionKey` / `NESTYSTAY_TOTP_SECRET_PROTECTION_KEY` | A development-only TOTP protection key was previously present in development appsettings. | Replace with at least 32 bytes of production entropy; re-enroll affected TOTP secrets if any non-local environment used the development key. |
| `Webhooks:SharedSecret` / `NESTYSTAY_WEBHOOK_SHARED_SECRET` | Example config previously used a fixed development webhook secret. | Generate a new shared webhook secret per environment and rotate downstream callers. |
| `Webhooks:StripeSigningSecret` / `STRIPE_WEBHOOK_SECRET` | Tests use `whsec_test*`; production must use a live Stripe endpoint secret. | Configure the live Stripe webhook signing secret from Stripe Dashboard and rotate if a real value was ever copied into tracked files. |
| `Integrations:StripeSecretKey` / `STRIPE_SECRET_KEY` | Examples now use placeholders; tests intentionally use `sk_test*`. | Configure a live restricted key in production and rotate if any real key was ever committed outside this audit. |
| `Integrations:StripePublishableKey` / `STRIPE_PUBLISHABLE_KEY` / `VITE_STRIPE_PUBLIC_KEY` | Frontend previously had a mock publishable-key fallback. | Configure the correct live publishable key for production frontend builds. Publishable keys are not secret, but wrong test keys must not ship. |
| `NESTYSTAY_ADMIN_BOOTSTRAP_PASSWORD` | New first-admin bootstrap password is externalized. | Generate a one-time temporary password, bootstrap the first admin, then disable bootstrap and rotate/remove the password. |

## No Tracked Real Secret Found

| Area | Result |
| --- | --- |
| Alibaba eKYC | Only provider URL placeholders/examples were found. |
| Cloudflare R2 | Only upload/download URL bases were found; no access key, secret key, or token was found in tracked config. |
| InsuraGuest | Only API base URL examples were found. |
| Google OAuth | Only `GOOGLE_AUTH_CLIENT_ID` / `VITE_GOOGLE_CLIENT_ID` placeholders and client-id usage were found. No client secret was found. |
| Email/SMTP | No tracked SMTP password or API key was found in the audited config files. |

## Validation Notes

- `appsettings.json` and `appsettings.Development.json` no longer contain concrete database passwords, administrator token hashes, session signing secrets, or TOTP protection keys.
- `.env.example` is an inventory of placeholders and required environment keys, not a source of usable credentials.
- The GitHub Actions acceptance workflow now uses passwordless trust authentication for its isolated Postgres service instead of a committed fixed password.
- Production startup validation rejects missing required settings, placeholder/development values, development database strings, Stripe test keys, missing/invalid legacy admin hashes when legacy tokens are enabled, and placeholder administrator bootstrap credentials.
