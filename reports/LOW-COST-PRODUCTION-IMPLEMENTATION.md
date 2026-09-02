# NestyStay low-cost production implementation

## Decision

The current tip preserves the locally complete M1–M5 full-stack implementation and adds a low-cost self-host deployment candidate. It is not a production authorization because client-owned infrastructure and live provider credentials are not present in this environment.

| Area | Result | Evidence |
| --- | --- | --- |
| M1–M5 application | PASS locally | Existing hardening reports and browser matrix |
| Email abstraction/outbox | PASS | 3 infrastructure tests; PostgreSQL migration applied |
| Brevo application integration | PASS (credential-gated) | Brevo transport + production config tests |
| Real Brevo delivery | BLOCKED | No client API key/sender domain |
| Alibaba eKYC | PRESERVED | Existing adapter/callback tests |
| Stripe | PRESERVED | Existing adapter/webhook tests; live keys blocked |
| Self-host compose | PASS candidate | Compose config and frontend/API/worker image builds |
| PostgreSQL/Redis/private storage | PASS candidate | Compose health checks, internal app network and volumes |
| Backups | PASS local script / BLOCKED remote | pg_dump/checksum script; no approved remote repository |
| TLS/DNS/monitoring | DOCUMENTED / BLOCKED deployment | Caddy, Cloudflare checklist and observability profile |

## Email provider classification

No legacy Alibaba Mail, Aliyun mail, DirectMail or Alibaba SMTP runtime dependency remains. Historical migration documentation names the old providers only to make the removal auditable. Human business mail is a client choice between Zoho (recommended) and Google Workspace; transactional application mail is Brevo.

## Production-only blockers

Client domain/DNS and VPS, live Stripe and Alibaba eKYC credentials, Brevo API key and verified sender, secret manager, off-server encrypted backups and restore rehearsal, monitoring alert ownership, and privacy/retention sign-off.
