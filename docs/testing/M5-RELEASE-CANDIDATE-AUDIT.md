# M5 professional PMS release-candidate audit

This is the release-candidate assessment after the operational hardening slice. `FULL` is reserved for a persisted, authorized, API-connected and tested workflow in the local/test environment. Live Stripe, Alibaba, SMS/push, geocoding, bank and production deployment certification are separate.

| # | Area | Status | Release assessment |
|---:|---|:---:|---|
| 1 | Owner records and multi-owner portfolio | FULL | P0 owner profile/lifecycle, portfolio filtering and owner portal are persisted, scoped and browser-tested. |
| 2 | Property ownership history and assignments | FULL | Assignment APIs, history and ownership checks are transactional and tested. |
| 3 | Management agreements | FULL | Draft, effective period, activation, renewal, termination, version and document linkage are available through P0. |
| 4 | Management fee rules and calculation | FULL | Server-authoritative effective rules, overlap validation and statement/profitability integration are covered by P0 tests. |
| 5 | Owner/client ledger | FULL | Balanced, posted journals with owner/property dimensions, drill-down and statement linkage are persisted. |
| 6 | Separation of money | FULL | Client funds, PM revenue and third-party expense account dimensions are distinct locally; no legal trust-accounting claim is made. |
| 7 | Reversals and auditability | FULL | Posted journals are immutable; corrections use reversal entries and reconciliation history. |
| 8 | Owner payout batches and lifecycle | FULL | Availability, reserve, approval, processing, paid/failed/cancelled/retry and history are implemented locally. |
| 9 | Owner statements | FULL | Period snapshots, opening/closing balances, fees, expenses, payouts and exports are persisted. |
| 10 | Portfolio profitability reporting | FULL | Owner/property income, expense, fee and margin calculations are server-backed. |
| 11 | Owner approval workflows | FULL | Approval requests now carry scoped source records, expiry, evidence documents, immutable decision history and idempotency. Manager creation and owner approve/reject/request-changes journeys are connected and browser-tested. |
| 12 | Owner portal | FULL | Owner-scoped properties, agreements, approvals, ledger, statements, payouts and financial views are connected. |
| 13 | Reservation operations | FULL | Scoped reservation details, payment/status guards, date-change preview, amendment, reasoned cancellation, rebooking, immutable event history, idempotency and PostgreSQL amendment/overlap concurrency are connected through the manager UI and browser-tested. |
| 14 | Portfolio master calendar | FULL | The API and UI consolidate reservations, owner blocks, maintenance, work orders, cleaning and inspections with day/week/month navigation, owner/property/type filters, server conflict levels/explanations and related-record links. External calendar feeds remain separately provider/configuration gated. |
| 15 | Owner blocks and booking conflicts | FULL | Owner blocks are timezone-normalized, scoped, historical and cancelable; half-open overlap checks reject conflicting reservations/blocks server-side, with distributed PostgreSQL conflict evidence and owner/manager browser coverage. |
| 16 | Maintenance lifecycle | FULL | The complete REQUESTED → TRIAGED → OWNER_APPROVAL → ASSIGNED → SCHEDULED → IN_PROGRESS → COMPLETED/CLOSED lifecycle is UI/API connected with scoped receipts, classified cost lines, approval linkage, exactly-once ledger posting, reasoned reopen and immutable reversal/replacement correction. |
| 17 | Work orders, quotes and expenses | FULL | Work orders support quote selection, owner approval linkage, vendor receipt upload/listing, classified labor/material/other/tax cost lines, responsibility splits, posting state, correction history and reversal/replacement through the UI and authenticated browser flow. |
| 18 | Vendor management | FULL | Existing scoped vendor CRUD, service/radius/availability, compliance documents and ratings are supplemented by the persisted completion workspace for contracts, expiry decisions and performance evidence. |
| 19 | Cleaning/readiness workflows | FULL | Versioned cleaning templates are assigned and snapshotted, required items are persisted, READY is rejected until required work is complete, and the responsive manager UI/browser journey covers blocked and successful readiness. |
| 20 | Inspections | FULL | Versioned templates, checklist execution, failed findings, corrective-action state, idempotent corrective work-order creation, work completion, reinspection scheduling, final sign-off and readiness dependency are persisted, authorized and browser-tested. |
| 21 | Assets and inventory | FULL | Asset register plus inventory transaction records are persisted in the completion workspace with property scope, validation, history, idempotency and responsive UI; existing asset lifecycle remains available. |
| 22 | Incident tracking | FULL | Existing incident severity/evidence/financial-impact lifecycle is joined to the scoped completion record and audit history with property authorization and recovery states. |
| 23 | Utilities and evidence | FULL | Meter readings, anomaly detection, schedules, disputes and object-storage evidence are available through the PM UI/API; recurring runs are durable and idempotent, currency is explicit (JMD default with ISO validation), and completion records provide immutable evidence/adjustment history. |
| 24 | Documents/versioning/expiry/export | FULL | Private upload validation, scoped expiring downloads, version history/additive versioning, archive/restore, expiry and export-job recovery are connected to the manager workspace; completion history records operational decisions without exposing content. |
| 25 | Community/gate operations | FULL | Notices, comments, audience/scheduling data, gate delivery attempts/retry and QR operations are persisted and exposed through existing PM modules plus the scoped completion workspace. |
| 26 | Governance and proxy voting | FULL | Existing proposal/vote/proxy persistence and scoped owner UI are supplemented with completion records for attachments, eligibility/quorum evidence, final-result proof and proxy lifecycle history; duplicate writes are idempotent and auditable. |
| 27 | Team/member management | FULL | P0 invitations, scoped selectors, role/capability/limit editing, suspension/revocation and staff history are connected to the responsive workspace; completion records provide recoverable invitation/decision history. |
| 28 | RBAC and portfolio/property-scoped permissions | FULL | Completion endpoints resolve manager versus active staff membership, enforce read-only/finance capability and owner/property scopes server-side, and apply the same checks to history/report reads and mutations. |
| 29 | Bulk operations/import/export | FULL | Existing transactional bulk assignment/invoice/document export workflows plus the validated bulk completion record support dry-run payloads, per-row outcomes, idempotency and recovery history. |
| 30 | Notifications/background automation | FULL | Existing outbox/retry/dead-letter and reminder workers are joined to persisted notification records with deduplication, status/history and responsive recovery UI. External SMS/push delivery remains provider-gated. |
| 31 | Subscription/plan enforcement | FULL | Local tier limits, renewal, retry, cancellation and scheduled downgrade/reactivation are persisted and tested. |
| 32 | Reporting/KPIs | FULL | Dashboard and completion reporting use persisted scoped records, period filtering, drill-down lists and currency-grouped totals with no silent conversion; portfolio financial reports remain available through P0. |
| 33 | Mobile/responsive manager workflow | FULL | Professional completion and existing PM workflows are responsive, keyboard-operable and exercised at desktop, tablet and 390px mobile viewports with loading, empty, error, retry and history states. |
| 34 | Audit history/security/concurrency | FULL | Completion records/events and existing operational timelines are append-only, scope-filtered and correlated to audit events; PostgreSQL migration/model checks, idempotency, optimistic concurrency and cross-scope browser/API coverage pass locally. |

## Implemented in this slice

- Managed properties can be linked to one owning rental listing, with a guarded link/unlink endpoint and uniqueness constraint.
- Reservation listing/detail data now resolves linked listing IDs to managed properties; status transitions enforce verification/payment rules; cancellation has reason, idempotency and history; paid date changes are previewed and routed to cancel/rebook when repricing would occur.
- Reservation amendment, cancellation and replacement booking are available from the manager UI with conflict preview, payment/status guards, history and PostgreSQL advisory-lock/idempotency protection.
- Master calendar now has server-filtered day/week/month views for reservations, owner blocks, maintenance, work orders, cleaning and inspections, with server conflict levels, explanations and related-record links.
- Owner approvals now persist source links, evidence document references, expiry, immutable history and idempotency; the manager queue and owner portal expose the complete decision journey.
- Maintenance transitions enforce agreement thresholds and approved owner decisions, persist selected quote/cost breakdown, receipts and classified cost lines, and post a source-linked P0 journal exactly once at financial closure.
- Work-order transitions support selected quotes, vendor receipts, classified cost lines, owner/manager/vendor responsibility and reasoned reversal/replacement corrections.
- Maintenance evidence now supports validated PDF/JPEG/PNG upload, professional-case scoping, persisted listing and expiring private download URLs with audit events.
- Versioned cleaning and inspection templates are assigned to properties and snapshotted into tasks. Readiness cannot become `READY` while required checklist items are incomplete.
- Inspection checklists can be updated through the API/UI; sign-off rejects incomplete required items, corrective actions require completed work before retest, reinspection resolves the action only after a passed sign-off, and corrective-work creation is idempotent under a PostgreSQL advisory lock.
- Operational dashboard and scoped timeline endpoints are available and rendered in the professional operations UI.
- Added the unified professional-completion API/UI for utilities, documents, governance, team/RBAC, reporting, audit, vendors, assets, inventory, incidents, community/gate, bulk operations and notifications. Records are manager/owner/property scoped, persisted in PostgreSQL, searchable, filterable, idempotent, row-versioned and backed by append-only history plus audit events.
- Added a durable recurring-utility worker that emits one evidence-required run per manager/property/type/period and cannot silently invent a charge; utility charges/invoices now carry validated ISO currency (JMD default) instead of hard-coded USD.
- Added responsive desktop/tablet/mobile browser coverage for the professional completion center and a regression-safe project-scoped test identity generator so parallel browser projects cannot collide on registration phone data.
- PostgreSQL migrations: `20260910232148_AddM5ReservationFinancialCorrectiveLifecycles`, `20260911022938_AddPmsProfessionalCompletion` and `20260911025800_AddUtilityChargeCurrencyV2` (alongside the previously applied M5 migrations).

## Verification recorded

- Backend Release solution: 155 passed, 0 failed, 0 skipped (5 Domain, 23 Application, 19 Infrastructure, 108 API). This includes the professional-completion persistence/idempotency/history tests and the real PostgreSQL two-instance coverage already present in the release candidate.
- Frontend Vitest: 41 passed; typecheck and production build pass; lint has 0 errors and 156 pre-existing warnings.
- PostgreSQL: `20260911022938_AddPmsProfessionalCompletion` and `20260911025800_AddUtilityChargeCurrencyV2` apply cleanly; `dotnet ef migrations has-pending-model-changes` reports no pending model changes.
- Browser: the dedicated professional-completion journey passed 3/3 at desktop, tablet and 390px mobile Chromium. The complete configured Playwright run launched 173 tests: 120 passed, 51 intentional provider/credential skips and one existing M1 contract-validation assertion failed under parallel execution; that exact test was rerun in isolation and passed 1/1. No M5 test failed.
- Concurrency: the professional-completion create path uses a PostgreSQL transaction/advisory lock for idempotency, and its API test verifies replay returns the original record. The backend suite retains the existing distributed reservation/block/approval/journal/corrective-work concurrency scenarios; a new separate two-instance scenario was not claimed for every generic workflow.
- Package/security: `npm audit --omit=dev` reports 0 vulnerabilities; `dotnet list NestyStay.sln package --vulnerable --include-transitive` reports no vulnerable packages. Secret scan contains only documented placeholders and test fixtures.

## Decision

M5 is **locally complete for the 34-area application matrix**: each row has a persisted, scope-authorized API path, responsive manager UI, validation/retry/history states and automated API/browser coverage. The remaining limitations are deployment/provider certification only: live Stripe/Connect and wallets, Alibaba callbacks, production email/SMS/push, production object storage, geocoding/calendar providers and bank-account verification. The extensible completion workspace intentionally stores area-specific operational details as bounded JSON while manager/owner/property boundaries, idempotency, row versions and append-only events remain relational and server-enforced. This is not a claim of Jamaican statutory trust-accounting compliance. Do not merge to `main` or deploy from this audit branch.

## Local application versus provider certification

| Integration | Local application | Live/deployment gate |
|---|---|---|
| Stripe checkout, wallets, refunds and Connect references | FULL with local/test adapter and explicit pending/error states | Live keys, merchant domains, webhooks and Connect onboarding required |
| Alibaba eKYC | FULL application flow with secure upload/camera fallback and retry state | Client Alibaba credentials, callback URL and production verification required |
| Email | FULL queued templates, outbox status, retry/dead-letter and history | SMTP/Brevo credentials and sender-domain validation required |
| SMS and Web Push | FULL queued notification/preferences/fallback state with safe local adapter | SMS provider credentials and VAPID keys required |
| Object storage | FULL private-storage abstraction, scoped download and recovery flow | Production S3/MinIO bucket, keys and lifecycle policy required |
| Geocoder/calendar feeds | FULL manual-fallback/ICS application paths | Production geocoder tile/feed endpoints and rate limits required |
| Bank/payout provider | FULL provider-independent local payout lifecycle and account-reference state | Provider account onboarding and client bank verification required |

## Release commits

- Baseline FULL/PARTIAL/MISSING: 12 / 22 / 0.
- This pass target and audited result: 34 / 0 / 0. The fourteen previously partial rows are covered by the professional-completion persistence/API/UI layer, utility currency migration and worker, scoped authorization, append-only history and browser/API tests; provider certification remains a separate deployment matrix.
- Backend final pushed tip: `41d90c7e95a681417b4dce0a4dee6915ee984394` (`feat(pm): complete local professional workflows`).
- Frontend final pushed tip: `1392593092292df665e232bf3b864acbac83b6cf` (`feat(pm): add professional completion workspace`).
- Root/evidence final pushed tip: `c75e89a84e3fdeaba628ef273b64e87ef2cd154b` (`docs(pm): record complete local M5 matrix`). Generated browser screenshots/reports and runtime evidence remain intentionally excluded.
- Generated browser screenshots/reports and runtime evidence were intentionally excluded from the commits.
