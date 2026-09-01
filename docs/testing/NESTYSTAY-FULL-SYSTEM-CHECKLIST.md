# NestyStay full-system enhancement checklist

Updated 2026-09-01 from the current repository state. This is the active checklist; the July strict gap matrix is retained as historical audit evidence. Statuses intentionally distinguish local application completion from external-provider, client-decision and optional work.

## Source and quality gates

| Area | Status | Evidence |
|---|---|---|
| Signed contract source | PASS | 11-page readable PDF copied byte-for-byte; SHA-256 `0C4AAD0B1A2D015433C0875107DD171C93C4191281D7CCCBBE748B37DB4FD28D`. |
| Backend/API | PASS | 96 tests passed / 0 failed. |
| Frontend unit/component | PASS | 26 Vitest tests passed / 0 failed. |
| TypeScript/Vite build | PASS | `npm run typecheck` and `npm run build`. |
| Lint | PASS | 0 errors; 162 non-blocking legacy warnings. |
| PostgreSQL migration | PASS | `nestystay_dev` updated through `20260901173721_AddDirectoryProviderDocuments`; state queried by API tests. |
| Responsive browser | PASS | M4/M5 targeted suite 18/18; usability suite 13 passed and 2 intentional mobile skips. |
| Fresh disposable database | BLOCKED EXTERNAL | Local `nestystay` role lacks `CREATEDB`; run with an operator-created disposable database before release. |

## M1 Core

| Functionality | Status | Evidence / boundary |
|---|---|---|
| Registration, login, signed sessions, logout and role access | PASS | Auth API/domain tests and browser contract flows. |
| Authenticator-app 2FA, recovery, lockout and replay protection | PASS | TOTP enrollment/challenge/recovery tests and UI states. |
| Verification progress and security feedback | PASS | Auth/eKYC status cards, validation and status announcements. |
| Host create/edit/list/archive/restore/delete | PASS | Owner-scoped property API, host list and wizard routes. |
| Listing draft, completeness, preview and photo ordering | PASS | Wizard local draft plus persisted property-photo prepare/upload/content API. |
| Property photo drag/drop and upload progress/retry | PASS | Host wizard controls and upload API/storage tests. |
| Booking dates, guests, quote, fees, verification and payment steps | PASS | Server-authoritative quote and connected booking UI. |
| PENDING/APPROVED/REJECTED, expiry and overlap protection | PASS | API, PostgreSQL and concurrency evidence. |
| Payment history, refunds, receipts and idempotency boundary | PASS | Local payment ledger/refund APIs and duplicate-payment tests. |
| Live Stripe provider transaction | BLOCKED EXTERNAL | Requires live account, webhook and Connect credentials. |
| Guest dashboard persisted trips/status/payment/QR data | PASS | API-backed traveler routes and browser evidence. |
| Host dashboard occupancy/revenue/payout/property performance | PASS | Real booking/property values; no fabricated analytics. |

## M2 Badges

| Functionality | Status | Evidence / boundary |
|---|---|---|
| FREE, VERIFIED, TRUSTED and WELLNESS catalog/pricing | PASS | Typed pricebook, seed/migration and domain tests. |
| Eligibility checklist, progress, benefits and feature gates | PASS | Server access service plus host/admin UI lock states. |
| Upgrade, expiry, suspension, annual renewal and reactivation | PASS | Lifecycle endpoints, maintenance/review jobs and audit rows. |
| Renewal/payment retry state and expiry visibility | PASS | Renewal records, admin queue and dashboard status controls. |
| Admin search, filters, bulk review and audit history | PASS | Admin badge management UI/API tests. |
| Commercial proration/grace-period policy | CLIENT DECISION REQUIRED | No rule is stated in the signed agreement; pricing/cadence remains configurable. |

## M3 Wellness

| Functionality | Status | Evidence / boundary |
|---|---|---|
| Officer onboarding wizard, documents, availability and save/resume | PASS | Three-step persisted application flow and browser lifecycle. |
| Admin officer approval/rejection/suspension/reactivation | PASS | Queue, reason templates, filters and audit events. |
| Host quote/booking, property ownership and schedule | PASS | Real quote, available officer lookup and persisted visit timeline. |
| Officer assignment, availability and contention protection | PASS | Assignment API and concurrency smoke. |
| Officer photo report upload, compression, captions and offline draft | PASS | Storage/scanner pipeline and responsive officer UI. |
| Host report view, print/save, acknowledgement and status | PASS | Report route with metadata and local PDF/print workflow. |
| Subscription plan, included visit, renewal/cancellation state | PASS | Local subscription APIs and UI controls. |
| Commission, escrow and payout state/history | PASS | Application ledger and admin payout desk. |
| Real banking/Connect payout execution | BLOCKED EXTERNAL | Provider/bank credentials and onboarding required. |
| Automated SMS/email reminders | BLOCKED EXTERNAL | Delivery provider credentials and scheduler operations required. |

## M4 Directories and QR

| Functionality | Status | Evidence / boundary |
|---|---|---|
| Custodian, Trades, Local Business and Police directories | PASS | Live PostgreSQL directory API, role/privacy gates and responsive routes. |
| Search, autocomplete, parish/category/availability filters | PASS | Directory controls and API query filters. |
| Sort, pagination, CSV export, list/map view | PASS | Shared list controls and browser checks; map is provider board until coordinates are supplied. |
| Favorites, saved searches, ratings, verified badges and contact | PASS | Connected directory UI state and platform messaging action. |
| Provider onboarding profile, terms, checklist and status timeline | PASS | Persisted provider profile/moderation routes. |
| Provider document binary upload, validation, storage and download | PASS | New scoped document entity/endpoints, scanner and provider vault UI. |
| Admin moderation approve/reject/request-changes/audit | PASS | Queue filters, reasons, comparison and audit events. |
| Badge-gated access and guest verification upsell | PASS | Server authorization and contextual UI with “Not now”. |
| QR issue, templates, expiry presets, preview/download/revoke/history | PASS | Hashed token API, manager/traveler controls and browser validation. |
| QR camera decode with manual/offline fallback | PASS | Browser `BarcodeDetector` path plus explicit manual fallback. |
| Real geocoding/map network | BLOCKED EXTERNAL | Coordinate fields/provider key are a deployment choice; no provider is faked locally. |
| Jamaica emergency 119 action | PASS | Permanent tap-to-call guidance on relevant surfaces. |

## M5 Property Manager

| Functionality | Status | Evidence / boundary |
|---|---|---|
| Role navigation, dashboard KPIs, alerts, portfolio filters and export | PASS | Manager workspace and responsive browser tests. |
| Owner invitation, duplicate warning, resend/cancel/expiry status | PASS | Scoped API and manager controls. |
| Owner verification, property assignment and ownership scope | PASS | Verification and assignment APIs/UI. |
| Owner portal balances, units, invoices, payments and maintenance | PASS | Real owner-scoped PostgreSQL dashboard. |
| Invoices, tax/line items, partial payment, receipt and recurring reminder | PASS | API-backed controls; automation is represented as an explicit reminder state. |
| Statements CSV/PDF-print, date filters and drill-down | PASS | Statement API and manager/owner UI. |
| Utilities usage/history/anomaly guidance and maintenance Kanban/SLA | PASS | Persisted utility/maintenance records and responsive controls. |
| Vendors, community notices, gate messages and delivery history | PASS | Scoped APIs and UI presets/history. |
| Governance, anonymous voting, quorum, results and proxy lifecycle | PASS | Eligibility, hash/count privacy, conflict/expiry/revoke and audit tests. |
| Documents upload, metadata, scoped storage and download | PASS | Validated PDF/JPEG/PNG storage plus new download endpoint/UI. |
| PM subscription status/renewal action | PASS | Configurable Portfolio tier and renewal state. |
| PM QR issue/validate/revoke and guard-first interface | PASS | API-backed QR journey and camera/manual guard UI. |
| Smart-meter, bank reconciliation, live delivery or native mobile apps | OPTIONAL OUT-OF-SCOPE | Requires external products or Phase 6 scope; not silently represented as complete. |

## Cross-cutting product quality

| Area | Status | Evidence / boundary |
|---|---|---|
| Role-based navigation, breadcrumbs, back actions and deep links | PASS | Shared `WorkspaceFrame` and route tests. |
| Consistent primary actions, confirmations and success/error feedback | PASS | Shared buttons, dialog confirmations and live feedback events. |
| Loading skeletons, empty states and retry paths | PASS | Shared `LoadingState`, `EmptyState` and tested workflows. |
| Global stay/directory/workspace search with authorization | PASS | Public query routes and role-scoped workspace search. |
| In-app messaging persistence, participants, read state and attachments | PASS | Conversation/message/attachment APIs and UI. |
| In-app notifications with unread/read/deep-link state | PASS | Notification store, inbox UI and preference test. |
| Keyboard/focus/labels/ARIA/responsive touch targets | PASS locally | Accessibility report and 13 Playwright usability checks; no formal certification claimed. |
| External email/SMS/push notifications | BLOCKED EXTERNAL | Provider credentials and delivery operations required. |
| Production readiness | NO | Managed Postgres/storage, backups, monitoring, secrets, provider validation and independent security review remain release gates. |

## Historical reconciliation

The July `M1_M2_STRICT_GAP_MATRIX.md` contains 47 legacy `PARTIAL` rows. They are preserved as historical evidence. Current locally implementable items have been implemented or verified; current active rows above use only PASS, BLOCKED EXTERNAL, CLIENT DECISION REQUIRED or OPTIONAL OUT-OF-SCOPE.
