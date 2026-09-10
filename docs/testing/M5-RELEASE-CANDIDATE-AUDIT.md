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
| 11 | Owner approval workflows | PARTIAL — LAUNCH BLOCKER | P0 approval decisions and maintenance enforcement work; evidence preview, richer decision queue and all operational approval links are incomplete. |
| 12 | Owner portal | FULL | Owner-scoped properties, agreements, approvals, ledger, statements, payouts and financial views are connected. |
| 13 | Reservation operations | PARTIAL — LAUNCH BLOCKER | New scoped details, status guards, cancellation reason/idempotency, date preview and history exist; complete amendment/rebook journey, payment-state timeline and full browser lifecycle remain. |
| 14 | Portfolio master calendar | PARTIAL — LAUNCH BLOCKER | API aggregates linked reservations, blocks, maintenance, cleaning and inspections; full day/week/month navigation, external feed sync and conflict UX remain. |
| 15 | Owner blocks and booking conflicts | PARTIAL — LAUNCH BLOCKER | UTC half-open overlap checks, linked-listing checks, cancellation and history exist; operational warnings/out-of-service policy and richer conflict recovery remain. |
| 16 | Maintenance lifecycle | PARTIAL — LAUNCH BLOCKER | State machine, quote selection, approval threshold, optimistic concurrency, scoped evidence upload/list/download and P0 expense posting are implemented; receipt-specific accounting, reopening/correction UI and the complete owner decision journey remain. |
| 17 | Work orders, quotes and expenses | PARTIAL — LAUNCH BLOCKER | Scoped quote comparison and selected-quote persistence work; full multi-bid evidence, work-order cost lines, receipts and close/reversal UI remain. |
| 18 | Vendor management | PARTIAL — POST-LAUNCH ENHANCEMENT | Vendor CRUD and quote linkage work; structured services/radius/availability, compliance expiry, contracts and performance history are incomplete. |
| 19 | Cleaning/readiness workflows | PARTIAL — LAUNCH BLOCKER | Persisted checklist and template snapshot plus server READY enforcement work; versioned templates, evidence requirements, photo/offline retry and corrective work links remain. |
| 20 | Inspections | PARTIAL — LAUNCH BLOCKER | Scheduling, persisted checklist updates, required-item sign-off enforcement, findings, evidence, sign-off and idempotent corrective-work creation work; versioned templates, richer remediation status and complete corrective-work lifecycle remain. |
| 21 | Assets and inventory | PARTIAL — POST-LAUNCH ENHANCEMENT | Scoped asset register, evidence JSON, retirement and stale-write protection work; inventory transactions, document/photo vault and maintenance linkage remain. |
| 22 | Incident tracking | PARTIAL — POST-LAUNCH ENHANCEMENT | Scoped severity, evidence references, financial impact and resolution work; richer private evidence and insurance workflow remain. |
| 23 | Utilities and evidence | PARTIAL — LAUNCH BLOCKER | Readings, anomaly flags, schedules and disputes exist; verified private evidence, durable recurring execution, currency-safe charges and P0 adjustment posting remain. |
| 24 | Documents/versioning/expiry/export | PARTIAL — LAUNCH BLOCKER | Upload validation, scoped download, versions and export jobs exist; verified previews, restore-as-new-version, granular content permissions and complete ZIP recovery remain. |
| 25 | Community/gate operations | PARTIAL — POST-LAUNCH ENHANCEMENT | Notices, comments, audience fields, gate messages and QR are connected; complete delivery history/resend UI and all audience administration remain. |
| 26 | Governance and proxy voting | PARTIAL — LAUNCH BLOCKER | Proposal/vote/proxy persistence exists; discussion attachments, frozen eligibility proof, anonymous final aggregate snapshot and complete proxy race handling remain. |
| 27 | Team/member management | PARTIAL — LAUNCH BLOCKER | P0 invite/edit/suspend/revoke and scopes exist; expiring acceptance links, scoped selector UI and complete staff history remain. |
| 28 | RBAC and portfolio/property-scoped permissions | PARTIAL — LAUNCH BLOCKER | P0 finance/approval scope checks are covered; professional operations still resolve a manager actor directly and do not yet apply every staff capability to every endpoint/background job. |
| 29 | Bulk operations/import/export | PARTIAL — POST-LAUNCH ENHANCEMENT | Bulk assignments, invoice issue and document export exist; generalized import, per-record outcomes and operational bulk actions remain. |
| 30 | Notifications/background automation | PARTIAL — POST-LAUNCH ENHANCEMENT | Outbox/retry workers and local reminders exist; external delivery providers and complete operational reminder coverage remain provider/configuration dependent. |
| 31 | Subscription/plan enforcement | FULL | Local tier limits, renewal, retry, cancellation and scheduled downgrade/reactivation are persisted and tested. |
| 32 | Reporting/KPIs | PARTIAL — LAUNCH BLOCKER | New operational dashboard returns real portfolio counts and drill-down tiles; consolidated financial/operational period reports and multi-currency presentation remain. |
| 33 | Mobile/responsive manager workflow | PARTIAL — POST-LAUNCH ENHANCEMENT | Operations UI passes dedicated Chromium desktop/tablet/mobile journeys; universal table-to-card, sticky actions and calendar parity remain. |
| 34 | Audit history/security/concurrency | PARTIAL — LAUNCH BLOCKER | New reservation/maintenance/readiness actions emit scoped audit events and use PostgreSQL advisory locks/concurrency tokens; legacy cross-module timeline and two-instance concurrency coverage remain. |

## Implemented in this slice

- Managed properties can be linked to one owning rental listing, with a guarded link/unlink endpoint and uniqueness constraint.
- Reservation listing/detail data now resolves linked listing IDs to managed properties; status transitions enforce verification/payment rules; cancellation has reason, idempotency and history; paid date changes are previewed and routed to cancel/rebook when repricing would occur.
- Maintenance transitions enforce agreement thresholds and approved owner decisions, persist selected quote/cost breakdown, and post a source-linked P0 journal exactly once at financial closure.
- Maintenance evidence now supports validated PDF/JPEG/PNG upload, professional-case scoping, persisted listing and expiring private download URLs with audit events.
- Readiness cannot become `READY` while required checklist items are incomplete.
- Inspection checklists can be updated through the API/UI; sign-off rejects incomplete required items, and corrective work-order creation is sign-off-gated and idempotent under a PostgreSQL advisory lock.
- Operational dashboard and scoped timeline endpoints are available and rendered in the professional operations UI.
- PostgreSQL migrations: `20260910125540_AddM5ReleaseOperationalControls`, `20260910130734_AddManagerAuditScope`, `20260910131316_AddReservationCancellationIdempotency`.

## Verification recorded

- Backend Release solution: 150 passed, 0 failed (5 Domain, 23 Application, 19 Infrastructure, 103 API).
- Frontend Vitest: 38 passed; typecheck and production build pass; lint has 0 errors and 156 pre-existing warnings.
- PostgreSQL: migrations applied; `dotnet ef migrations has-pending-model-changes` reports no pending model changes.
- Browser: dedicated professional operations 6/6 and final-hardening M5 3/3 across desktop/tablet/mobile Chromium; the complete configured Playwright suite passed 106 tests with 52 intentional provider/credential skips and 0 failures.
- Concurrency: the PostgreSQL-backed harness passed 4/4 checks (parallel duplicate registration and parallel QR validation); the backend suite includes the existing idempotency, overlap, refund, proxy and revocation scenarios. A true two-independent-server distributed concurrency certification is still outstanding.
- Package/security: `npm audit --omit=dev` reports 0 vulnerabilities; `dotnet list NestyStay.sln package --vulnerable --include-transitive` reports no vulnerable packages. Secret scan contains only documented placeholders and test fixtures.

## Decision

M5 is **not yet a professional/deployable PMS**: the money foundation is locally strong, but reservation, approval, maintenance/readiness, document, governance, utilities and staff-scope blockers remain. Do not merge to `main` or deploy until the launch-blocker rows above have complete persistence → authorization → API → frontend → browser and PostgreSQL/concurrency evidence. Live provider certification remains a separate gate.

## Release commits

- Backend (`codex/pm-p0-money-authority`): `9aa66883e194ad5dc1d469e635740c2307eec5ec` (prior release `310ab47`; inspection/evidence hardening in `9aa6688`).
- Frontend (`codex/pm-p0-money-authority`): `3288ccd052d754ef9fabc85cc082db7a126fb028` (maintenance evidence and inspection checklist UI/API client).
- Generated browser screenshots/reports and runtime evidence were intentionally excluded from the commits.
