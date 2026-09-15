# Client ownership handover

NestyStay is designed so the client can take ownership of every account and the
deployment without changing application code. Transfer ownership through a
password manager and delegated access; never place credentials in Git, tickets
or chat.

## Accounts and resources to transfer

- Domain registrar and DNS/Cloudflare account, including the final domain and
  proxy/TLS settings.
- Production VPS or server, SSH keys, deployment user, Docker volumes and the
  repository release access.
- PostgreSQL, Redis and private object-storage volumes, plus the off-server
  encrypted backup destination and restore operator.
- Stripe account, webhook endpoint and payout owner.
- Brevo organization, verified sending domain, sender and reply-to ownership.
- Zoho Mail (recommended) or Google Workspace business mailbox account and
  aliases such as support, info, admin and billing.
- Alibaba Cloud eKYC production account, callback/signature configuration and
  data-retention owner.

## Handover checklist

1. Client creates or confirms each account under a client-owned email and MFA.
2. Developer receives least-privilege delegated access only where ongoing
   administration is requested.
3. The production `.env` is copied into the client's secret manager; it is not
   committed or emailed.
4. A staging smoke test, backup restore rehearsal and rollback are signed off.
5. Client receives the deployment, operations, DNS/email and backup runbooks,
   plus the final release SHA and current provider matrix.
6. Remove personal recovery addresses, unused API keys and temporary SSH keys.

The current release is intentionally **not production-ready** until the client
owns the domain, server, provider accounts and off-server backup destination.
