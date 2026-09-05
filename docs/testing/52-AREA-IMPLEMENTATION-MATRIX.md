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
| 8 | M1 authentication | PARTIAL | Magic-link session issuance, passkeys and device/session management. |
| 9 | M1 2FA | PARTIAL | SMS fallback, passkeys, trusted devices and full recovery wizard. |
| 10 | M1 property listings | PARTIAL (enhanced) | Address autocomplete is now configurable; duplicate/publish, bulk archive, immutable revisions and restore-as-draft are implemented and tested. |
| 11 | M1 booking | PARTIAL | Visual availability calendar and ICS sync/conflict recovery. |
| 12 | M1 eKYC | PARTIAL | Camera capture/compression and live Alibaba callback certification. |
| 13 | M1 payments | PARTIAL | Stripe wallet UI and live provider/webhook/refund certification. |
| 14 | M1 guest dashboard | PARTIAL | Persisted explainable recommendations and dismissal. |
| 15 | M1 host dashboard | PARTIAL | Payout history and calendar-sync health. |
| 16 | M2 upgrades/renewals | PARTIAL | Server proration, grace/dunning, auto-renew and complete payment history. |
| 17 | M2 admin badge management | PARTIAL | Evidence queue, reason templates, price history and override audit UI. |
| 18 | M3 officer onboarding | PARTIAL | Secure document workflow and coordinate/radius coverage map. |
| 19 | M3 officer approval | PARTIAL | Secure previews, request-changes cycle and notification automation. |
| 20 | M3 wellness booking | PARTIAL | Server-backed slot selection and workload-aware availability UI. |
| 21 | M3 scheduling | PARTIAL | Calendar views, timezone, reminders and conflict-safe rescheduling. |
| 22 | M3 officer assignment | PARTIAL | Distance/map filters, reassignment reason and assignment history UI. |
| 23 | M3 photo reports | PARTIAL | Resumable uploads, offline drafts and verified multi-photo ordering. |
| 24 | M3 host reports | PARTIAL | Server PDF generation, acknowledgement and follow-up tasks. |
| 25 | M3 subscriptions | PARTIAL | Pause/resume, upgrade/downgrade and retry states. |
| 26 | M3 commissions/payouts | PARTIAL | Statements, disputes and provider-account status. |
| 27 | M3 officer privacy | PARTIAL | Consent history, export requests and full field-level visibility rules. |
| 28 | M4 trades directory | PARTIAL | Emergency/service-radius filters, quote lifecycle and comparison. |
| 29 | M4 local business directory | PARTIAL | Structured hours/holidays, promotions, reviews and accessibility data. |
| 30 | M4 provider moderation | PARTIAL | Document comparison and request-changes cycle with complete history UI. |
| 31 | M4 provider dashboard | PARTIAL | Leads, conversations, availability, analytics and review responses. |
| 32 | M4 directory search | PARTIAL | Persisted recent views and guest clear-history controls. |
| 33 | M4 badge gating | PARTIAL | Restore complete return URL and search/filter state after upgrade/login. |
| 34 | M4 QR issue | PARTIAL | Purpose/subject templates, secure share delivery and per-code history. |
| 35 | M4 QR revoke/expiry | PARTIAL | Live countdown, reasoned revoke and explicit event history. |
| 36 | M5 manager dashboard | PARTIAL | Persist KPI layout/order and configurable portfolio task state. |
| 37 | M5 owner invitation | PARTIAL | CSV/vCard import, expiry/cancel and delivery history. |
| 38 | M5 owner verification | PARTIAL | Secure documents, per-requirement decisions and change requests. |
| 39 | M5 property assignment | PARTIAL | Keyboard fallback, transactional bulk assignment and ownership history. |
| 40 | M5 invoices | PARTIAL | Draft/line-item workflow, tax, PDF preview, bulk issue and reminders. |
| 41 | M5 payments | PARTIAL | Partial payments, refunds, receipts and idempotent reconciliation UI. |
| 42 | M5 utilities | PARTIAL | Meter uploads, recurring billing, anomaly detection and disputes. |
| 43 | M5 maintenance | PARTIAL | Attachments, assignees, SLA timers, scheduling and resident updates. |
| 44 | M5 vendors | PARTIAL | Compliance expiry, contracts, ratings, availability and preferred flag. |
| 45 | M5 community board | PARTIAL | Scheduling, acknowledgements, comments, attachments and targeting. |
| 46 | M5 gate messages | PARTIAL | Recipient confirmation, delivery receipts, retry and history. |
| 47 | M5 governance | PARTIAL | Attachments, discussions, quorum, reminders and published summaries. |
| 48 | M5 anonymous voting | PARTIAL | Independent eligibility/ballot storage and non-identifying receipts. |
| 49 | M5 proxy voting | PARTIAL | Eligibility lookup, conflict checks, expiry/revoke and persistent proxy label. |
| 50 | M5 documents | PARTIAL | Safe previews, versions, expiry reminders, permissions and ZIP export. |
| 51 | M5 subscription | PARTIAL | Usage limits, retry, cancellation feedback and scheduled downgrade. |
| 52 | M5 property-manager QR | PARTIAL | Subject templates, secure sharing, active/history lists and revoke trail. |

## Verification snapshot (2026-09-05)

- Backend solution: 112 passed, 0 failed (`dotnet test NestyStay.sln --no-restore --configuration Release`).
- Frontend unit tests: 31 passed, 0 failed; typecheck and production build pass.
- Browser usability smoke: 4 passed, 1 intentional mobile skip on the local stack. The first run exposed an outdated test fixture that persisted a bearer token in local storage; the fixture now uses the secure cookie-session shape.
- Property lifecycle API test: duplicate → owned draft → revision restore → publish → bulk archive passes against the test persistence provider.
- Live Stripe, Alibaba, external email/SMS/push, and production PostgreSQL certification remain separate provider/deployment gates.
