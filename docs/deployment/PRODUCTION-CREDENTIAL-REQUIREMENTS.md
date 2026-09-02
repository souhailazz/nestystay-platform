# NestyStay production credential requirements

This inventory contains requirements only. Never store live credentials, private
keys, webhook secrets, recovery codes, or mailbox passwords in this repository.
Populate the deployment secret environment from an approved secret manager on
the client-owned server.

| Service | Required | Client must provide | Current status |
|---|---|---|---|
| VPS/server | Yes | Server IP, SSH access, OS access, firewall policy, resource and storage allocation | BLOCKED — client server not supplied |
| Domain registrar | Yes | Registered production domain and DNS-management access | BLOCKED — production domain not supplied |
| Cloudflare | Yes for the planned edge setup | Cloudflare account/zone access, DNS records, proxy/WAF decision | BLOCKED — client zone not supplied |
| MinIO/S3-compatible object storage | Yes for production uploads | Private endpoint, application access key/secret, bucket, region, TLS choice and persistent-volume ownership | BLOCKED INFRASTRUCTURE — MinIO adapter and Compose service are ready |
| Brevo | Yes for transactional application email | API key, verified sender domain/address, sender name, reply-to address, webhook/complaint access | BLOCKED CREDENTIAL — application adapter is ready |
| Zoho OR Google Workspace | Yes for human/business mail | Selected mailbox provider, domain verification, support/info/billing mailboxes, operator access | CLIENT DECISION — Zoho is the documented default; Google Workspace is the alternative |
| Stripe | Yes for live payments | Live publishable/secret keys, webhook signing secret and endpoint, test/live account access, refund/payment test authorization | BLOCKED CREDENTIAL — application integration is ready |
| Alibaba eKYC | Yes where contractual identity verification is used | Production/sandbox credentials, merchant/business identifier, signed callback secret/material, endpoint and provider test authorization | BLOCKED CREDENTIAL — application integration is ready |
| Off-server backup destination | Yes before production sign-off | Approved encrypted destination (restic repository or equivalent), retention policy, encryption/password-file custody and restore access | BLOCKED DESTINATION — local backup scripts are ready |

## Boundary decisions

- Alibaba Mail is not used. Brevo is the transactional application-email provider.
- Zoho is the default human/business mailbox choice until the client selects
  Google Workspace instead.
- Alibaba Cloud remains the eKYC provider only; it is not an email dependency.
- Stripe Connect is optional while audited manual payouts remain enabled.

## Safe handover sequence

1. Client supplies the server, domain/Cloudflare access and the selected business
   mailbox provider.
2. Client supplies Brevo, Stripe and Alibaba eKYC credentials through the secret
   manager; the developer never receives or commits them in source control.
3. Client supplies the off-server backup destination and approves a restore
   rehearsal.
4. The operator deploys the checked release SHA, runs staging provider tests,
   completes the production smoke suite and records client visual evidence.
