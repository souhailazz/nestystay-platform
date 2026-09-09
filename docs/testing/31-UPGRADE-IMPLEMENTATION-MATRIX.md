# NestyStay 31-upgrade implementation matrix

This matrix maps every upgrade in the approved plan to the observable UI, API, persistence and verification evidence. `LOCAL/TEST` means the application boundary works against PostgreSQL and deterministic adapters. `LIVE GATE` is intentionally separate and never treated as a local failure.

| # | Upgrade | UI route | API boundary | Persistence / security | Verification | Local/test | Live gate |
|---:|---|---|---|---|---|---|---|
| 1 | Passwordless login | `/login`, `/auth/passwordless` | `/api/auth/passwordless/request`, `/complete` | Hashed, single-use flow rows; cookie session | API + frontend auth tests | PASS | Email provider/domain |
| 2 | SMS 2FA fallback | Login 2FA modal | `/api/auth/2fa/sms/request`, `/verify` | Expiry, attempt limits, audit; no client-trusted IP | API security tests | PASS with local adapter | SMS credentials/provider |
| 3 | Passkeys | Settings and login passkey actions | `/api/auth/passkeys/*` | WebAuthn credential/challenge rows, sign-count replay checks | Backend build/tests; browser capability guard | PASS | RP ID/origin/domain |
| 4 | Trusted devices/sessions | `/profile` session security | `/api/auth/sessions/*` | Per-session metadata, hashed token linkage, revoke-others | Session API regression + browser settings path | PASS | Production cookie/domain policy |
| 5 | Apple Pay/Google Pay | Booking checkout | Stripe Payment Element + Express Checkout | Server-authoritative PaymentIntent state | Stripe test-mode browser + API tests | PASS in test mode | Live Stripe merchant domain/webhooks |
| 6 | External calendar sync | `/calendar`, host property calendar | `/api/properties/{id}/calendar/feeds/*`, export | Feed ETag/Last-Modified, blocks, sync event history; SSRF/size limits | Calendar API tests + worker code path | PASS | External feed availability |
| 7 | eKYC camera capture | `/traveler/identity` | Existing secure identity upload endpoints | Compressed upload, scan status, private object key | Frontend component/browser file fallback | PASS locally | Alibaba credentials/callback |
| 8 | Wellness coverage map | `/officer/wellness` | Officer onboarding/available-officer endpoints | Latitude/longitude/radius validation | API + onboarding browser path | PASS | OSM/geocoder production quota |
| 9 | Wellness onboarding documents | `/officer/wellness` step 2 | `/api/wellness/officers/{id}/documents/*` | Scanned private documents, expiry/review state | API upload/security + browser upload | PASS | Production object storage/retention |
| 10 | Scheduling/rescheduling | `/host/wellness` visit cards | `/api/wellness/visits/{id}/reschedule`, cancel | UTC + IANA timezone, overlap guard, timeline | Wellness lifecycle/API tests | PASS | Reminder provider |
| 11 | Map assignment/workload | Admin wellness operations | Officer availability/assignment endpoints | Coverage/distance/load filtering, assignment history | Wellness API lifecycle tests | PASS | Live map tiles (optional) |
| 12 | Report templates/photo ordering | `/officer/wellness` report form | Report upload/template endpoints | Template versions, ordered photo metadata, scan status | API + browser report upload | PASS | Production object storage |
| 13 | PDF reports/collaboration | Host wellness report cards | `/api/wellness/reports/{id}/pdf`, comments, acknowledge, tasks | Immutable snapshot/PDF and role checks | Wellness API + lifecycle browser | PASS | PDF retention policy |
| 14 | Payout operations | Host/admin wellness payout views | Payout/statement/dispute endpoints | Provider account reference only; payout states/audit | API lifecycle and payout tests | PASS with test provider | Stripe Connect/bank verification |
| 15 | Trades directory enhancements | `/directory/trades`, provider profile | Directory services/quotes endpoints | Owner-scoped provider/service/quote rows | M4 browser + API tests | PASS | Messaging/email delivery |
| 16 | Local-business data | `/directory/businesses` | Business details/reviews endpoints | Structured hours, closures, promotions, moderated reviews | Directory API/browser tests | PASS | Map/directions provider |
| 17 | Provider management tools | `/directory/provider` | Services, analytics, quote/review response endpoints | Provider ownership filters and event aggregates | Provider dashboard browser + API | PASS | External messaging/review moderation |
| 18 | Recently viewed providers | Directory results/profile | `/api/directories/recent-views/*` | User-scoped rows; guest-safe local fallback | Recent-view API/browser path | PASS | None |
| 19 | M4 QR lifecycle | `/traveler/qr`, `/gate/qr` | QR issue/validate/revoke/history | Token hashes, expiry/revoke reason and scan events | QR browser + forged-token tests | PASS | Camera scanner hardware certification |
| 20 | M5 property assignment | PM property/assignment workspace | `/api/property-manager/properties/bulk-assign`, history | Transactional assignment + ownership history | PM API/browser route | PASS | None |
| 21 | M5 invoice management | `/pm/invoices` | Invoice create/update/bulk-issue/overdue endpoints | Server line totals and ledger audit | PM API/browser tests | PASS | Email reminder delivery |
| 22 | M5 payments/refunds | `/pm/payments` | Payment methods, pay, retry, refund endpoints | Idempotency/reconciliation/refund history | PM API round trips + browser | PASS with test provider | Live Stripe/webhooks |
| 23 | M5 utilities | `/pm/utilities` | Meter/schedule/dispute endpoints | Immutable readings, anomaly and dispute records | PM API/browser tests | PASS | Photo object storage |
| 24 | M5 vendors | `/pm/vendors` | Vendor update/compliance endpoints | Service/rating/preferred and expiry metadata | PM API/UI paths | PASS | Compliance verification provider |
| 25 | M5 community board | `/pm/community`, owner portal | Notice/comment/acknowledge endpoints | Audience enforced server-side, publish/expiry state | PM API/browser tests | PASS | Email/push delivery |
| 26 | Gate delivery history | `/pm/gates` | Gate message delivery/retry endpoints | Attempt rows, idempotency and provider references | PM API/UI path | PASS | Gate messaging provider |
| 27 | Governance | `/pm/governance`, owner portal | Proposal/discussion/close/vote endpoints | Quorum/result proof and discussion rows | PM API/security tests | PASS | None |
| 28 | Proxy voting | Owner governance view | Proxy list/create/revoke endpoints | Eligibility, expiry, grant/use/revoke history | API + owner UI path | PASS | None |
| 29 | Document management | `/pm/documents` | Document/version/download/archive/export endpoints | Object-storage metadata, immutable versions, access audit, expiry reminders and async ZIP export | PM API/browser tests | PASS | Production storage read/retention |
| 30 | M5 subscription lifecycle | `/pm/subscription` | Change/events/retry/renew endpoints | Lifecycle event history, provider status, plan limits, auto-renew and scheduled-change state | PM API + worker + desktop/tablet/mobile browser path | PASS | Live billing provider certification |
| 31 | M5 QR lifecycle | PM QR/guard interface | `/api/property-manager/qr/*` | Subject, active/history, scan/revoke/expiry state | PM QR API/browser tests | PASS | Scanner hardware/provider deployment |

## Quality snapshot

- Backend: 135 passed, 0 failed (Domain 5, Application 23, Infrastructure 19, API 88).
- Backend release build: 0 warnings/0 errors; NuGet vulnerability audit reports no vulnerable packages after the `Microsoft.Bcl.Memory 10.0.12` pin.
- Frontend: 35 passed; typecheck and production build pass; full npm audit has 0 vulnerabilities (Vitest/coverage-v8 4.1.11).
- Browser verification: complete configured Playwright matrix **91 passed, 0 failed, 52 intentional credential/provider-gated or viewport-scoped skips** across 143 collected tests; desktop Chromium was 30/30, Property Manager lifecycle passed at desktop/tablet/mobile (3 passes total), and Firefox/WebKit critical smoke passed (2 additional passes).
- PostgreSQL: 194 public tables, migrations through `20260909200136_FixPropertyManagerSubscriptionDefaults`.
- Known non-local gates: live Stripe/Connect, Alibaba eKYC, external SMS/Push/email configuration, full cross-browser multi-role certification, manual screen-reader/WCAG sign-off, and production object-storage read/retention certification.
