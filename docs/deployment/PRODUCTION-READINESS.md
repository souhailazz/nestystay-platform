# Production readiness

## Current decision

**NO — local full-stack acceptance is complete, but production launch is not yet authorized.** M1–M5 application behavior is locally implemented and tested. Production-only blockers are external configuration and operational controls, not missing local UI/API behavior.

## Required before launch

- Live Stripe account, Connect configuration, webhook signing secret and refund/reconciliation rehearsal.
- Live Alibaba/eKYC credentials, callback signing material and failure/retention policy.
- Managed PostgreSQL with backups, restore test, migrations policy and least-privilege service account.
- Production object storage, malware scanning, CDN/access policy and document retention policy.
- Secret manager, TLS/domain configuration, rate-limit/WAF policy, structured logs, metrics, alerts and incident runbook.
- Fresh clean-database migration run by an operator with `CREATEDB` (the local `nestystay` role is intentionally not allowed to create databases).
