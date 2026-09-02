# Account and deployment setup guide

Use this checklist when the client is ready to move the local release candidate to a VPS. Record values in the approved secret manager, not in Git or chat. Confirm each provider's current plans, quotas, and terms directly with the provider; this guide does not promise pricing.

## 1. Domain, VPS and edge

1. Register or select the production domain and set `PUBLIC_APP_URL` to the final HTTPS origin.
2. Provision the VPS with private persistent volumes, firewall rules, backups, and an operator account.
3. Point DNS through the approved edge provider, then validate Caddy/Let's Encrypt certificates and the frontend/API health routes.

## 2. Self-hosted services

1. Create PostgreSQL credentials and database; set `ConnectionStrings__Postgres`.
2. Create private MinIO root and application credentials, bucket, endpoint, region, and `MINIO_USE_SSL` choice. Keep the console/admin endpoint private.
3. Set `OBJECT_STORAGE_PROVIDER=minio`, `EMAIL_PROVIDER=brevo`, `BUSINESS_MAIL_PROVIDER=zoho`, `EKYC_PROVIDER=alibaba`, `PAYMENT_PROVIDER=stripe`, `PAYOUT_MODE=manual`, `WEB_PUSH_ENABLED=false`, and `SMS_ENABLED=false` unless an approved change says otherwise.
4. Configure encrypted off-server backups and run the restore rehearsal before go-live.

## 3. External providers

- Brevo: verify sender/domain, create an API key, configure `BREVO_SENDER_EMAIL`, optional reply-to, and run the clickable verification/reset/invitation flow against the deployed URL.
- Stripe: create live secret/publishable keys, webhook signing secret, endpoint, and run payment plus idempotency/refund checks.
- Alibaba Cloud eKYC: create the production application, callback/signature settings, and run approved success/failure/expiry checks.
- Zoho (default) or Google Workspace: create operational mailboxes and aliases for support, billing, privacy, and security. Alibaba Mail is not used by the application.

## 4. Handover evidence

Attach the final environment validation output (configured/missing only), migration head, backup checksums, restore result, provider test IDs, alert test, and owner sign-off to the deployment record. Rotate bootstrap and temporary credentials after handover.
