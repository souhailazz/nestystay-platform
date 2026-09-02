# Client account checklist

## Required now

| Account/resource | Account owner | Developer access required | Credentials/config needed | Current status |
| --- | --- | --- | --- | --- |
| Domain registrar + Cloudflare | Client | Delegated DNS access | Domain, DNS records, proxy and SSL mode | CLIENT ACTION |
| VPS/server | Client | SSH/Docker operator access | Hostname/IP, SSH key, maintenance window | CLIENT ACTION |
| PostgreSQL/Redis/object storage | Client | Least-privilege operator access | Secret-managed passwords, volume owner | Compose ready; provision required |
| Off-server backup destination | Client | Backup/restore operator access | Restic repository and password file | BLOCKED OPERATIONAL |
| Brevo | Client | Restricted organization/API access | API key, verified sender/domain, reply-to | Application PASS; credential required |
| Zoho Mail (default) or Google Workspace | Client | Optional delegated admin access | Chosen mailbox aliases and recovery MFA | CLIENT DECISION |
| Stripe | Client | Restricted developer/webhook access | Live keys, webhook secret, payout owner | Application PASS; live validation required |
| Alibaba eKYC | Client | Restricted integration access | Production endpoint, credentials, callback/signature setup | Application PASS; live validation required |

## Optional later

| Capability | Account owner | Developer access required | Credentials/config needed | Current status |
| --- | --- | --- | --- | --- |
| SMS provider | Client | Only if enabled | Provider key, sender and opt-in policy | DISABLED |
| Web Push | Client | Deployment config only | VAPID keys/subject | OPTIONAL_DISABLED |
| Stripe Connect | Client | Restricted Connect access | Connected-account and payout configuration | OPTIONAL |
| Premium map/geocoder | Client | Provider configuration access | Tile/geocoder key and usage limit | OPTIONAL |
| Managed infrastructure | Client | Vendor support/deployment access | Vendor account and migration plan | OPTIONAL |

Credentials are exchanged through the client's password manager or secret
manager, never by commit, ticket or chat.

Credentials are exchanged through the client's password manager or secret manager, never by commit, ticket or chat.
