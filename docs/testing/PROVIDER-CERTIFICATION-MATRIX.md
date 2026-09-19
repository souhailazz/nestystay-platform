# Provider certification matrix

No credentials or secrets are stored here. Replace each `PENDING` with dated evidence only after testing the deployed candidate.

| Provider | Feature | Local mode/evidence | Staging requirement | Current result | Exact action |
|---|---|---|---|---|---|
| Stripe Payments | Booking, badge, PM billing, refunds | Deterministic adapter/lifecycle and idempotency tests pass | Test/live account, keys, webhook secret and approved test data | PENDING | Run amount/currency, capture, refund, duplicate/replay, invalid signature and tampering tests. |
| Stripe Identity | Guest/host verification | Stripe Identity boundary and deterministic local results | Identity enabled, secret/publishable keys, return URL and signed Identity events | PENDING | Run processing, verified, requires-input, canceled, failed, correlation, replay and PII checks. |
| Stripe Connect | Host/officer/owner payouts | Persisted local payout states | Connected-account onboarding and bank verification | PENDING | Run onboarding, payout success/failure/retry, dispute, webhook and reconciliation tests. |
| Brevo | Transactional email | File provider/outbox/retry tests | API key, verified `nestystay.net` sender and webhook/complaint access | PENDING | Send booking, approval, rejection, payment, refund, identity, receipt and failure/retry messages. |
| MinIO/S3/R2 | Private uploads | Local storage abstraction and authorization tests | Endpoint, bucket, keys, TLS and retention policy | PENDING | Upload/download/restart/expiry/unauthorized-access/restore test. |
| Google OAuth | Login | Configurable OAuth path | Authorized origin and redirect for deployed domain | STAGING REPORTED | Recheck after final SHA deployment and record redirect/login evidence. |
| SMS/Web Push | Reminders/notifications | Safe local queued adapter | Provider credentials/VAPID keys only if launch-enabled | PENDING/OPTIONAL | Configure only if launch scope requires it; test delivery/failure/retry. |
| Map/geocoder | Explore/directories | Coordinates and manual fallback | Chosen tile/geocoder provider and key/rate limits | PENDING | Test tiles, marker, lookup, bad address, outage, rate limit and mobile fallback. |
| InsuraGuest | Insurance | No live provider evidence | API base URL, credentials and webhook if launch-required | PENDING | Confirm scope, configure, then run quote/policy/webhook tests. |

## Identity rule

Stripe Identity is the only active runtime identity provider. Alibaba is not an active provider. Historical migration snapshots are not runtime integrations and must not be used as certification evidence.
