# Ownership and operations handover

| Area | Client/operator owner | Evidence to retain |
| --- | --- | --- |
| Domain, DNS, TLS, VPS | Client infrastructure owner | DNS/TLS checks and access inventory |
| PostgreSQL and MinIO | Client infrastructure owner | Migration head, volume/backup checksums, restore rehearsal |
| Brevo transactional email | Client communications owner | Verified sender, provider message IDs, clickable-link test |
| Stripe payments | Client finance owner | Live webhook/payment/refund evidence |
| Alibaba eKYC | Client compliance owner | Callback/signature and retention approval |
| Zoho/Google business mail | Client operations owner | Mailbox aliases and recovery contacts |
| Monitoring and incidents | Named on-call operator | Alert delivery test and escalation rota |
| Privacy and retention | Client legal/compliance owner | Data-retention and access review |

The repository supplies code, configuration templates, scripts, and runbooks. Client owners supply credentials, legal approvals, external accounts, destination infrastructure, and the final go-live decision.
