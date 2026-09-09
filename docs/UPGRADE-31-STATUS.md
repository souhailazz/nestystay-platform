# NestyStay 31-upgrade implementation status

This file records the state after the current implementation pass. An upgrade is only marked **implemented locally** when a real API, persistence path, UI path, and automated regression test exist. Live-provider certification is tracked separately.

| Upgrade area | Local status | Evidence |
|---|---|---|
| Passwordless login | Implemented locally | `/api/spec/auth/passwordless/request` and `complete`; hashed, single-use 15-minute flows; cookie-session completion; `PasswordlessLoginEndpointTests`; request/completion UI. |
| Apple Pay / Google Pay | Test-mode UI wired | Stripe Express Checkout is rendered beside Payment Element; provider wallet/domain certification remains client-credential blocked. |
| External calendar synchronization | Implemented locally | ICS feed connect/sync/export endpoints, PostgreSQL feed/block rows, SSRF checks, CalendarPage controls, `CalendarEndpointTests`. |
| eKYC camera capture | Existing local implementation verified | Identity upload uses mobile `capture="environment"` with preview/compression/retry and secure upload; live Alibaba validation remains separate. |
| Wellness coverage map data | Implemented locally | Officer latitude/longitude/radius fields, validation, migration, OSM map links in onboarding/admin UI. |
| Wellness scheduling/rescheduling | Implemented locally | UTC timestamps plus IANA timezone, server conflict guard, host UI action, audit timeline, lifecycle regression test. |
| Wellness workload balancing | Improved locally | Available-officer query now orders by recent assignment load and excludes overlaps. |
| Trades/local-business metadata | Implemented locally | Services, opening-hours text, emergency availability and service radius persisted and shown in provider UI. |
| M5 invoice editing | Implemented locally | Authorized `PUT /api/property-manager/invoices/{id}`, unpaid-only invariant, line replacement, ledger correction, API regression test and manager UI. |
| M5 property assignment | Implemented locally (API + UI) | Transactional bulk assignment, duplicate-safe assignment history/audit rows, keyboard-accessible manager module route and authenticated desktop/tablet/mobile browser coverage. |
| M5 payments/refunds/reconciliation | Implemented locally (test provider) | Payment listing/filtering, saved references, partial/full refund with idempotency, retry and reconciliation fields are persisted and covered by API round-trip tests. Live Stripe certification is separate. |
| M5 utilities | Implemented locally | Meter readings, continuity/duplicate checks, historical anomaly detection, schedules, invoice linkage and dispute decision endpoints/UI are persisted through PostgreSQL model/migration. |
| M5 maintenance | Implemented locally | SLA timestamps, workflow activity timeline, object-storage attachments, vendor assignment and completion history are wired to API and UI. |
| M5 vendors | Implemented locally | Service area/availability/rate/preferred/suspended metadata and compliance document storage are persisted; provider analytics/rating automation remains a follow-up. |
| M5 community/gates/governance | Implemented locally | Notices support category, publish scheduling, expiry, acknowledgement deadlines and server-enforced owner targeting; comments/acknowledgements, gate delivery attempt rows/idempotency, proposal discussion/close proof and UI actions are API-backed. |
| M5 documents | Implemented locally (core) | Object-storage metadata, initial version rows, archive/restore, access history and version query are wired; asynchronous ZIP export/expiry reminder worker remains. |
| M5 subscription/invitations/verification | Implemented locally (core) | Lifecycle events, dashboard preferences, invitation/verification history and real decision routes are persisted and exposed in the manager workspace. |
| Professional PMS foundation | Implemented locally (foundation) | Agreements, fee rules, owner payouts/approvals, staff scopes, calendar, work orders, cleaning tasks, inspections and aggregate reports have PostgreSQL entities, APIs and module routes. |
| M5 QR active/history list | Implemented locally | Authorized `GET /api/property-manager/qr`, server-derived active/expired/revoked state, manager dashboard history cards and revoke action. |
| QR lifecycle (M4) | Implemented locally | Active/history endpoints, reasoned revoke, expiry countdown, UI history, PostgreSQL migration and API tests. |
| Recently viewed providers | Implemented locally | User-scoped PostgreSQL view rows, deduplicated timestamps, remove/clear endpoints, signed-in UI history and regression test. |
| M5 QR event history | Implemented locally | Manager-scoped scan-history endpoint, persisted validation events, dashboard history viewer, revocation states and API coverage. |
| Proxy revoke/history | Implemented locally | Owner-scoped proxy list/revoke endpoints, expiry status, owner portal controls and existing voting authorization. |

The remaining items are now explicit rather than hidden: live Stripe wallet/webhook certification; external SMS/Push credentials; a production-grade asynchronous reminder worker (the notification outbox and retry state are present); full object-storage ZIP export/expiry reminder worker; and authenticated multi-role browser certification across every supported tablet/mobile/browser combination. Provider analytics, review responses, and structured community notice scheduling are implemented locally and covered by API/UI paths; their live-provider and broad certification gates remain separate.

## Verification run

- Backend: `dotnet test NestyStay.sln --configuration Release --no-restore` — **84 API + 19 infrastructure + 23 application + 5 domain tests passed (131 total)**, including notice scheduling/audience scoping and the assignment/payment/refund/meter/work-order round trips.
- Frontend: `npm test -- --run` — **35 passed**; `npm run typecheck` and `npm run build` passed after the provider analytics/review-response and scheduled-notice UI updates.
- Browser: combined desktop Chromium M1–M5 workflow run — **12 passed**; provider dashboard analytics/reply controls and scheduled notice UI are included. Hardening security run — **2 passed**; quality/security combined run — **4 passed, 5 intentional project skips**. Core M3/M4/M5 journeys pass against the local API with cookie-session authentication and PostgreSQL-backed data.
- Browser PM route: `npx playwright test e2e/property-manager-pms.spec.ts --project=desktop-chromium --workers=1` — **3 passed** in the latest route-specific run (desktop/tablet/mobile configuration coverage).
- Database: additive migrations through `20260909112812_AddPropertyManagerWorkflowLifecycle` applied successfully to the local PostgreSQL cluster on port `55432` (`nestystay_dev`), with **186 application tables** present. Client-database deployment validation remains required.
- Live Stripe wallets and Alibaba eKYC remain provider-credential/domain certification steps.
