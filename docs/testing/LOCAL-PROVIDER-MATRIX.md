# Local provider matrix

| Provider/capability | Local implementation | Production/staging action |
|---|---|---|
| Stripe payments | Deterministic Stripe-compatible adapter; local publishable key placeholder; no real secret inherited | Set real keys, webhook secret, signature validation and verify authorization/capture/refund/idempotency |
| Stripe Identity | `stripe_identity` application boundary with deterministic local result | Set Stripe Identity secret/return URL/events and run a real signed session |
| Stripe Connect | Local persisted account/transfer adapter supports onboarding, paid, pending, failed and disputed scenarios | Configure connected-account onboarding, bank verification, transfer and dispute handling |
| Email | File outbox under `%TEMP%\nestyStay-email-outbox` for E2E and queued templates | Configure Brevo sender/domain/API key and verify delivery/retry/dead-letter |
| Storage | Local private file storage | Configure MinIO/S3/R2 credentials, buckets, lifecycle and restore policy |
| Map/geocoder | Coordinates plus list/map/manual fallback | Configure chosen provider, tiles, geocoding and quotas |
| SMS/Web Push | Not enabled for local launch; queued state remains explicit | Add provider credentials and device delivery tests |
| InsuraGuest | No local live claim | Configure only if required by signed scope |

No API secrets, passwords, tokens or connection strings are committed by the local scripts.
