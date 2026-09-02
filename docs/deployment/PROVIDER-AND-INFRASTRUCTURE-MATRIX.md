# Provider and infrastructure matrix

| Capability | Local implementation | Production choice | Current status |
| --- | --- | --- | --- |
| Frontend/API | Vite + ASP.NET Core | Docker + Caddy | Compose ready; admin integration status is available at `/admin/ops/integrations`; not deployed |
| PostgreSQL | EF Core/Npgsql | Self-hosted PostgreSQL volume | Local migration verified |
| Redis | Reserved private service | Self-hosted Redis with password | Compose ready; wire distributed cache/rate limits before scale-out |
| Object storage | `IStorageProvider` with private persistent volume | Self-hosted volume/MinIO service | Local persistent adapter active; MinIO service available for future S3 adapter |
| Email | PostgreSQL outbox + file transport | Brevo transactional | Application PASS; real delivery needs credentials |
| Business mailbox | Provider-neutral docs | Zoho or Google Workspace | Client decision |
| Payments | Stripe abstraction + local gateway | Stripe live | Application PASS; real provider blocked |
| Identity | Alibaba eKYC abstraction/callbacks | Alibaba eKYC | Preserved; real credentials blocked |
| TLS/DNS | Caddy config | Cloudflare free DNS + Let's Encrypt | Domain action required |
| Monitoring | Health endpoints/logging | Prometheus/Grafana/Loki/Uptime Kuma profile | Compose profile ready |
| Backups | pg_dump + checksum + optional restic | Client-owned off-server encrypted storage | Local script PASS; destination required |
| SMS/push | Optional provider-neutral seam | Disabled unless separately approved | Not a release dependency |

No mail provider other than Brevo/Zoho/Google appears in the runtime plan. Stripe and Alibaba eKYC remain the only commercial application integrations required by the signed milestones.
