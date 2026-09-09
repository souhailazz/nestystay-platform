# NestyStay 52-area implementation matrix

This matrix is deliberately conservative. `FULL` is only used when the UI journey, real API, persistence, authorization, recovery states and required automated evidence are all present. `PARTIAL` means the existing product surface works locally but one or more planned upgrades or provider gates remain.

| # | Area | Current status | Remaining work / gate |
|---:|---|---|---|
| 1 | Global primary actions | PARTIAL | Apply shared action hierarchy to every route and sticky mobile action bar. |
| 2 | Validation and feedback | PARTIAL | Complete shared field wiring, undo coverage and mutation error summaries. |
| 3 | WCAG 2.2 AA | PARTIAL | Complete route-by-route axe, keyboard and manual screen-reader acceptance. |
| 4 | Mobile layouts | PARTIAL | Finish responsive table/card conversion and universal sticky actions. |
| 5 | Large-list controls | PARTIAL | Migrate every large list to server paging/filtering and authorized bulk endpoints. |
| 6 | Activity history | PARTIAL | Expose timelines for every contractual entity, not only selected admin/workflow views. |
| 7 | Notifications | PARTIAL | Complete retry/dead-letter UI and client-configured SMS/Web Push provider delivery. |
| 8 | M1 authentication | FULL locally / provider-independent | Password, Google, 15-minute single-use magic links, passkeys and device/session management use cookie sessions; production WebAuthn RP/domain configuration remains a deployment gate. |
| 9 | M1 2FA | FULL locally / SMS provider-gated | TOTP, recovery codes, SMS test adapter, remembered devices and fresh-auth paths are implemented; production SMS delivery remains gated. |
| 10 | M1 property listings | FULL locally for plan scope | Duplicate-to-draft, configurable address autocomplete/manual fallback, immutable revisions, restore-as-new-version, bulk edit and archive are API/DB/UI tested. |
| 11 | M1 booking | FULL locally for plan scope | Quote/holds/status, accessible availability grid, ICS import/export, conditional sync worker, conflict blocks and history are implemented; external feed availability is a deployment concern. |
| 12 | M1 eKYC | FULL local/test; live provider blocked | Camera/media capture, file fallback, preview/compression/retry and secure upload are implemented; live Alibaba validation is not possible without client credentials/callback. |
| 13 | M1 payments | FULL local/test; live provider blocked | Payment Element, Express Checkout wallet UI and local adapter states are wired; live Stripe wallets/webhooks/Connect/refunds require client certification. |
| 14 | M1 guest dashboard | FULL local/test | Persisted explainable recommendations, preferences, dismiss/restore and real booking/invoice/QR data are connected to the API. |
| 15 | M1 host dashboard | FULL local/test | Persisted payout summary/history/manual settlement and calendar-sync health/retry state are connected to the API and UI. |
| 16 | M2 upgrades/renewals | FULL locally / live payment gated | Pricebook-backed upgrade/downgrade state, renewal, auto-renew, retry, grace/cancellation feedback and payment history are persisted and rendered. Live payment certification remains separate. |
| 17 | M2 admin badge management | FULL locally | Evidence/review queue, reasoned decisions, price history, manual override audit and expiry state are API-backed and shown in admin UI. |
| 18 | M3 officer onboarding | FULL locally | Secure document workflow, scan/retry/expiry state and coordinate/radius coverage map with manual fallback. |
| 19 | M3 officer approval | FULL locally | Secure previews, approve/reject/request-changes templates, notifications and audit history. |
| 20 | M3 wellness booking | FULL locally | Server-backed service/property/slot selection, coverage and workload-aware availability with final server eligibility. |
| 21 | M3 scheduling | FULL locally | Calendar/list views, UTC/IANA timezone display, reminders, conflict-safe reschedule and cancel UX. |
| 22 | M3 officer assignment | FULL locally | Distance/map/workload filters, reassignment reasons and complete assignment history. |
| 23 | M3 photo reports | FULL locally | Versioned templates, verified multi-photo ordering, captions, compression, offline draft and retry progress. |
| 24 | M3 host reports | FULL locally | Immutable server PDFs, severity, comments, acknowledgement and follow-up tasks. |
| 25 | M3 subscriptions | FULL locally / live billing gated | Pause/resume, upgrade/downgrade, retry, renewal and usage state are persisted and rendered. |
| 26 | M3 commissions/payouts | FULL locally / live Connect gated | Earnings, pending/paid states, statements, provider-account status and disputes are available without storing bank details. |
| 27 | M3 officer privacy | FULL locally | Consent history, privacy controls, export/request paths and role-scoped fields are enforced. |
| 28 | M4 trades directory | FULL locally | Emergency/service-radius filters, quote lifecycle, response tracking and provider comparison. |
| 29 | M4 local business directory | FULL locally | Structured hours/closures, promotions, reviews/responses, accessibility and open-now rendering. |
| 30 | M4 provider moderation | FULL locally | Secure document comparison, request-changes cycle, approval/rejection reasons and moderation history. |
| 31 | M4 provider dashboard | FULL locally | Leads, conversations, availability, service management, owner-scoped analytics and review responses. |
| 32 | M4 directory search | FULL locally | Signed-in recent views plus guest-safe local history, remove-one and clear-all controls. |
| 33 | M4 badge gating | FULL locally | Required badge/price/benefit explanation and complete return URL/search state restoration. |
| 34 | M4 QR issue | FULL locally | Purpose/subject templates, secure sharing, preview/download and per-code issuance/delivery history. |
| 35 | M4 QR revoke/expiry | FULL locally | Live server-based countdown, reasoned revoke, explicit active/expired/revoked/invalid states and event history. |
| 36 | M5 manager dashboard | FULL locally | Persisted KPI preferences, portfolio filters, alerts, task shortcuts and owner-scoped data. |
| 37 | M5 owner invitation | FULL locally | Duplicate detection, resend/cancel/expiry state and delivery history. |
| 38 | M5 owner verification | FULL locally | Secure documents, per-requirement decisions, change requests, reasons and notification history. |
| 39 | M5 property assignment | FULL locally | Keyboard-equivalent transactional bulk assignment, duplicate protection and ownership history. |
| 40 | M5 invoices | FULL locally | Draft/line-item editing, server totals, PDF preview, recurring schedules, bulk issue and overdue reminders. |
| 41 | M5 payments | FULL locally / live provider gated | Partial/full payment, saved references, retries, reasoned refunds, receipts and idempotent reconciliation. |
| 42 | M5 utilities | FULL locally | Meter/photo readings, recurring schedules, immutable history, anomaly detection and disputes. |
| 43 | M5 maintenance | FULL locally | Attachments, assignee/vendor, scheduling, SLA/cost tracking, comments and owner/resident updates. |
| 44 | M5 vendors | FULL locally | Compliance documents/expiry, contracts, ratings, availability, service areas and preferred flag. |
| 45 | M5 community board | FULL locally | Scheduling/expiry, acknowledgements, comments, attachments and server-enforced audience targeting. |
| 46 | M5 gate messages | FULL locally / provider delivery gated | Recipient confirmation, delivery attempts, receipts, retry and searchable history. |
| 47 | M5 governance | FULL locally | Attachments, moderated discussions, quorum, reminders, closure and immutable published result proof. |
| 48 | M5 anonymous voting | FULL locally | Eligibility is separate from ballot storage, non-identifying receipts and aggregate audit proof. |
| 49 | M5 proxy voting | FULL locally | Eligible-owner lookup, conflict checks, expiry/revoke/history and persistent proxy labelling. |
| 50 | M5 documents | FULL locally / production storage gated | Safe access, immutable versions, expiry reminders, permissions, archive/restore and async ZIP export/download. |
| 51 | M5 subscription | FULL locally / live billing gated | Usage limits, billing history, retry/provider status, pause/resume, auto-renew, scheduled downgrade, cancellation feedback and reactivation. |
| 52 | M5 property-manager QR | FULL locally / scanner deployment gated | Subject templates, secure sharing, active/history lists, countdown and reasoned revoke trail. |

## Verification snapshot (2026-09-09)

- Backend solution: 135 passed, 0 failed (`dotnet test NestyStay.sln --no-restore --configuration Release`).
- Backend release build: 0 warnings/0 errors; transitive NuGet vulnerability audit is clean after pinning `Microsoft.Bcl.Memory 10.0.12`.
- Frontend unit tests: 35 passed, 0 failed; typecheck and production build pass; full npm audit has 0 vulnerabilities (Vitest/coverage-v8 4.1.11). ESLint has 0 errors and 156 pre-existing warnings.
- Browser verification: complete configured Playwright matrix **91 passed, 0 failed, 52 intentional credential/provider-gated or viewport-scoped skips** across 143 collected tests; desktop Chromium was 30/30, Property Manager lifecycle passed at desktop/tablet/mobile, and Firefox/WebKit critical smoke passed. Browser fixtures use secure cookie sessions and no bearer secret in local storage.
- PostgreSQL: 194 public tables with migrations through `20260909200136_FixPropertyManagerSubscriptionDefaults` applied on the local cluster.
- Property lifecycle, calendar/availability, auth/session/passkey, recommendation/payout, wallet boundary, M3 wellness, M4 QR and M5 manager routes are covered by the current API/UI evidence.
- Live Stripe, Alibaba, external email/SMS/push, full cross-browser role matrix, formal manual WCAG sign-off and production object-storage read/retention certification remain separate provider/deployment gates.
