# Low-cost operating plan

## What NestyStay controls

One client-owned VPS can run the web app, API, PostgreSQL, Redis, private file
storage, background worker, QR, messaging, in-app notifications, Property
Manager, invoices/statements, governance, and optional monitoring through
Docker Compose. Caddy provides TLS and the application never needs Alibaba Mail,
Twilio, AWS RDS/S3 or a paid analytics platform.

## Likely unavoidable costs

- Domain registration and a server/VPS with enough disk for database and files.
- Stripe transaction fees and Alibaba eKYC checks when those services are used.
- The selected Zoho or Google Workspace business-mail plan, if the client's
  mailbox tier is paid.
- Brevo charges only if the free transactional allowance is exceeded.
- A separate encrypted backup destination if the client requires off-server
  disaster recovery (recommended for production).

## Optional or deferred costs

SMS, premium map/geocoding tiles, Stripe Connect, managed databases, paid
monitoring and managed object storage are deliberately disabled or optional.
Enable them only after a real usage, compliance or reliability requirement is
approved.

Maintain daily encrypted PostgreSQL and object-storage backups. Local-only
backups are a recovery convenience, not a production control. The worker
sidecar runs the same release image with `BackgroundJobs__Enabled=true`; the API
keeps background jobs disabled to prevent duplicate processing. Scale to
multiple API replicas only after Redis-backed distributed coordination is
enabled and tested.
