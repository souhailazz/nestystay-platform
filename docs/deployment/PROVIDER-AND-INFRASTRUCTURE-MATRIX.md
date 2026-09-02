# Provider and infrastructure matrix

| Component | Provider | Self-hosted / external | Cost model | Required for launch | Configured | Tested | Fallback |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Frontend | Vite + Nginx | Self-hosted | VPS only | Yes | PASS (Compose) | PASS (build/browser) | None |
| API | ASP.NET Core | Self-hosted | VPS only | Yes | PASS (Compose; `/api/health` probe) | PASS (109 backend tests) | Local runner |
| PostgreSQL | PostgreSQL 17 | Self-hosted | VPS disk | Yes | PASS (private volume) | PASS (migrations/integrity) | Restore rehearsal required |
| Redis | Redis 7 | Self-hosted | VPS disk | Yes for sidecar topology | PASS (private/password) | PASS (health/config) | Single-instance in-memory limits until distributed wiring |
| Object storage | Local adapter + private volume; MinIO service | Self-hosted | VPS disk | Yes | PASS (local adapter); MinIO service private/bundled, adapter switch pending | PASS (authorization/upload tests; archive/restore rehearsal) | Reviewed MinIO S3 adapter before switching |
| Transactional email | Brevo + PostgreSQL outbox | External | Free allowance, then usage | Yes for email delivery | PASS (application); BLOCKED CREDENTIAL (real) | PASS (queue/config) | In-app + file capture |
| Business email | Zoho (default) or Google Workspace | External | Client mailbox plan | Client decision | CLIENT DECISION | DOCUMENTED | Provider-neutral addresses |
| Payments | Stripe | External | Per-transaction fees | Yes for live payments | PASS (application); BLOCKED CREDENTIAL (real) | PASS (local/idempotency) | Safe local/test mode |
| eKYC | Alibaba Cloud eKYC | External | Per-check fees | Yes where required | PASS (application); BLOCKED CREDENTIAL (real) | PASS (callback/security tests) | Explicit manual admin review only if approved |
| Payout | Audited manual mode | Self-hosted | Bank transfer cost only | Yes initially | PASS | PASS (authorization/idempotency) | Stripe Connect optional |
| Messaging | NestyStay PostgreSQL/attachments | Self-hosted | VPS only | Yes | PASS | PASS (API/browser) | In-app only |
| Notifications | In-app + email worker | Self-hosted + Brevo | VPS + email allowance | Yes | PASS / BLOCKED CREDENTIAL for Brevo | PASS (queue/retry) | In-app |
| Web Push | Standards/VAPID (feature flag) | Self-controlled optional | VPS only | Optional | OPTIONAL (disabled) | Not enabled | In-app/email |
| SMS | None | Disabled | No recurring cost | No | DISABLED | Not applicable | In-app/email |
| Maps | Leaflet/OpenStreetMap-compatible | Self-controlled | Low-traffic public tiles; provider swap later | Optional | PASS (UI); geocoder optional | PASS (responsive routes) | Manual coordinates |
| Monitoring | Prometheus/Grafana | Self-hosted | VPS disk | Yes before go-live | PASS (Compose profile) | PASS (config parse); alerts still require deployment | Health endpoints/logs |
| Logs | Loki | Self-hosted | VPS disk | Recommended | PASS (Compose profile) | PASS (config parse) | Container logs |
| Uptime | Uptime Kuma | Self-hosted | VPS disk | Recommended | PASS (Compose profile) | PASS (config parse) | Manual health checks |
| Backups | pg_dump + tar + optional restic | Self-hosted/off-server | Destination-dependent | Yes | PASS (local); BLOCKED OFF-SERVER | PASS (syntax/checksum/isolated object restore rehearsal) | Local backup only until destination exists |
| TLS | Caddy + Let's Encrypt | Self-hosted/external CA | Free | Yes | PASS (config); BLOCKED DOMAIN | PASS (Caddy validate) | Staging HTTP only |
| DNS/proxy | Cloudflare free | External | Free tier | Yes | CLIENT ACTION | Documented | Registrar DNS |

No mail provider other than Brevo/Zoho/Google appears in the runtime plan. Stripe and Alibaba eKYC remain the only commercial application integrations required by the signed milestones.
