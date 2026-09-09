# NestyStay 31-upgrade implementation status

This status is based on the current worktree after the M1 security, calendar, camera, wallet, recommendation, payout, document-export and subscription-lifecycle implementation pass. “Local/test” means the complete application boundary is exercised with PostgreSQL and deterministic test adapters. Live-provider certification is intentionally reported separately.

## Current implementation

| Upgrade area | Local/test status | Evidence and boundary |
|---|---|---|
| Passwordless login | Implemented locally | Hashed, single-use 15-minute magic-link flow, cookie-session completion, request/complete UI and API regression coverage. |
| SMS 2FA fallback | Implemented with local adapter | OTP expiry, throttling, resend and audit path are wired; external SMS credentials/provider delivery remain blocked. |
| Passkeys | Implemented locally | WebAuthn registration/assertion/removal, challenge expiry, sign-counter replay protection and settings/login UI. Production RP/domain configuration remains a deployment gate. |
| Trusted devices/session management | Implemented locally | Individual sessions, metadata, 30-day remembered devices, revoke-one and revoke-others (current session preserved) are persisted and exposed in settings. |
| Apple Pay / Google Pay | Local/test boundary wired | Stripe Express Checkout and Payment Element wallet paths are rendered; live merchant-domain, webhook and wallet certification require client Stripe credentials. |
| External calendar synchronization | Implemented locally | ICS import/export, ETag/Last-Modified conditional sync, bounded hosted worker, atomic availability blocks, retry state, history, disconnect and manual retry UI. |
| True eKYC camera capture | Implemented locally | `getUserMedia` preview/capture, mobile file fallback, JPEG re-encode/compression, retry and secure upload UI; live Alibaba callbacks remain blocked. |
| Wellness coverage map | Implemented locally | Coordinates/radius validation and OpenStreetMap-compatible preview with manual fallback. |
| Wellness onboarding documents | Implemented locally | Secure, scanned PDF/JPEG/PNG uploads, replace/retry state, expiry metadata and role-restricted admin review. |
| Wellness scheduling/rescheduling | Implemented locally | UTC storage with IANA timezone, conflict-safe reschedule/cancel endpoints, participant timeline and UI controls. |
| Wellness assignment/workload | Implemented locally | Coverage, availability and workload-aware officer selection with reassignment history and authorization. |
| Wellness reports | Implemented locally | Versioned templates, ordered photo uploads, server PDF/report collaboration, host acknowledgement and follow-up task APIs/UI. |
| Wellness payouts | Implemented locally/test | Earnings and payout lifecycle, provider-account reference/status, statements and dispute paths are present; live Connect/bank verification remains provider-gated. |
| Trades directory | Implemented locally | Services, emergency availability, radius filtering, quote request/response and provider comparison are API-backed. |
| Local-business data | Implemented locally | Structured hours/closures, promotions and review/response APIs are persisted and rendered. |
| Provider management | Implemented locally | Service/profile controls, quote/review responses and owner-scoped analytics are wired; production moderation/browser breadth remains a gate. |
| Recently viewed providers | Implemented locally | Signed-in PostgreSQL history plus guest-safe local fallback, remove-one and clear-all controls. |
| M4 QR lifecycle | Implemented locally | Active/expired/revoked state, history, reasoned revoke, countdown and forged/wrong-property validation paths. |
| M5 property assignment | Implemented locally | Transactional bulk assignment, duplicate protection, keyboard-accessible controls and ownership history. |
| M5 invoices | Implemented locally | Draft/line-item editing, server totals, bulk issue and overdue reminder enqueue path. |
| M5 payments | Implemented with test provider | Saved references, partial/full payment, idempotent refunds, retry and reconciliation history; live Stripe remains gated. |
| M5 utilities | Implemented locally | Meter reading/photo fields, recurring schedule model, anomaly detection, immutable history and disputes. |
| M5 vendors | Implemented locally | Services, availability/radius, preferred flag, ratings and compliance documents/expiry metadata. |
| M5 community/gates | Implemented locally | Targeted notices, comments/acknowledgements, scheduled publish/expiry, gate delivery attempts, retry and searchable history. |
| M5 governance/proxy | Implemented locally | Discussions, proposal lifecycle, quorum/result proof, proxy grant/expiry/revoke/history and proxy labelling. |
| M5 documents | Implemented locally | Object-storage metadata, immutable versions, access history, archive/restore, expiry-date capture/reminders and asynchronous manager-scoped ZIP export/download are implemented. Production storage read/retention certification remains a gate. |
| M5 subscription | Implemented locally | Tier changes, billing events, retry/provider status, plan limits, pause/resume, scheduled downgrade worker, auto-renew, cancellation/reactivation and cancellation reasons are persisted and exposed in the subscription UI. Live billing certification remains provider-gated. |
| M5 QR lifecycle | Implemented locally | Subject selection, active/history state, scan/revoke events and explicit expiry/revocation status. |

## Verification run

- Backend: **135 passed, 0 failed** — Domain 5, Application 23, Infrastructure 19, API 88 (`dotnet test NestyStay.sln --configuration Release --no-build --no-restore`).
- Backend release build: **0 compiler/MSBuild warnings and 0 errors** after pinning patched `Microsoft.Bcl.Memory 10.0.12`; `dotnet list package --vulnerable --include-transitive` reports no vulnerable packages.
- Frontend: **35 passed, 0 failed**; `npm run typecheck` and `npm run build` pass. Full `npm audit --audit-level=high` is clean after updating Vitest/coverage-v8 to 4.1.11. ESLint reports **0 errors and 156 existing warnings** (baseline cleanup remains).
- Real browser: the complete configured Playwright matrix passed **91/91 with 0 failures** (52 intentional credential/provider-gated or viewport-scoped skips across 143 collected tests). This includes desktop Chromium **30/30**, the Property Manager lifecycle at tablet/mobile (**2/2**), and Firefox/WebKit critical smoke (**2/2**). Skipped provider/admin/live-provider journeys require `NESTYSTAY_E2E_ADMIN_TOKEN` or client credentials.
- PostgreSQL: local cluster on port `55432`, database `nestystay_dev`; **194 public tables** and migrations through `20260909200136_FixPropertyManagerSubscriptionDefaults`, including sessions/passkeys/calendar-sync/recommendation/payout/document-export/subscription lifecycle fields.
- Security: session revocation, token hashing/expiry, SSRF/calendar limits, QR forgery and upload validation are covered by the backend/API and browser suites.

## Explicit remaining gates

1. Live Stripe wallet/webhook/refund/Connect certification with the client account, merchant domains and webhook secret.
2. Live Alibaba eKYC callback certification and production document-retention configuration.
3. External SMS, Web Push and production email provider credentials/domain/DNS setup.
4. Full authenticated multi-role browser matrix at every supported viewport and Firefox/WebKit smoke acceptance (the local suites above are the verified subset).
5. Manual NVDA/VoiceOver and formal WCAG 2.2 AA acceptance for every route; automated checks and keyboard paths are in place but are not a certification.
6. Production object-storage read/retention policy and operational certification for document ZIP exports and expiry reminders; the local worker, API and UI paths are implemented and tested.

These gates affect production readiness and provider certification; they do not invalidate the locally verified application flows.
