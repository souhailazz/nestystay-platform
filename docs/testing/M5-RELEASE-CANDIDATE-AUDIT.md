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
| 18 | Vendor management | PARTIAL — POST-LAUNCH ENHANCEMENT | Vendor CRUD and quote linkage work; structured services/radius/availability, compliance expiry, contracts and performance history are incomplete. |
| 19 | Cleaning/readiness workflows | FULL | Versioned cleaning templates are assigned and snapshotted, required items are persisted, READY is rejected until required work is complete, and the responsive manager UI/browser journey covers blocked and successful readiness. |
| 20 | Inspections | FULL | Versioned templates, checklist execution, failed findings, corrective-action state, idempotent corrective work-order creation, work completion, reinspection scheduling, final sign-off and readiness dependency are persisted, authorized and browser-tested. |
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
| 34 | Audit history/security/concurrency | PARTIAL — LAUNCH BLOCKER | New reservation/approval/maintenance/work-order/readiness actions emit scoped audit events and the two-host PostgreSQL harness covers six duplicate/conflict scenarios; a unified cross-module audit timeline and complete legacy background-job capability enforcement remain. |

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
- PostgreSQL migration: `20260910232148_AddM5ReservationFinancialCorrectiveLifecycles` (alongside the previously applied M5 migrations).

## Verification recorded

- Backend Release solution: 153 passed, 0 failed, 0 skipped (5 Domain, 23 Application, 19 Infrastructure, 106 API), with the real PostgreSQL two-instance test enabled.
- Frontend Vitest: 39 passed; typecheck and production build pass; lint has 0 errors and 156 pre-existing warnings.
- PostgreSQL: migrations applied; `dotnet ef migrations has-pending-model-changes` reports no pending model changes.
- Browser: the dedicated M5 release-blocker suite passed 12/12 across desktop/tablet/mobile Chromium (reservation amendment/cancellation/rebook, calendar, owner approval, maintenance accounting, readiness and inspection/reinspection). The complete configured Playwright suite passed 115 tests with 52 intentional provider/credential skips and 0 failures.
- Concurrency: one real PostgreSQL test launches two independent API hosts and passes six scenarios—identical reservation amendment replay, overlapping owner-block conflict, duplicate owner decision, duplicate journal reversal, exactly-once maintenance journal posting and idempotent corrective work-order creation. The backend suite also retains the existing idempotency, overlap, refund, proxy and revocation scenarios.
- Package/security: `npm audit --omit=dev` reports 0 vulnerabilities; `dotnet list NestyStay.sln package --vulnerable --include-transitive` reports no vulnerable packages. Secret scan contains only documented placeholders and test fixtures.

## Decision

M5 is **locally complete for the six release-blocker workflows covered by this pass, but not yet a professional/deployable PMS overall**: the remaining launch blockers are utilities/evidence, document recovery/permissions, governance/proxy proof, team/RBAC completeness, consolidated reporting and cross-module audit coverage. Do not merge to `main` or deploy until those remaining rows have complete persistence → authorization → API → frontend → browser and PostgreSQL/concurrency evidence. Live Stripe, Alibaba, SMS/push, geocoding, bank and production deployment certification remain separate gates.

## Release commits

- Baseline FULL/PARTIAL/MISSING: 12 / 22 / 0.
- This pass FULL/PARTIAL/MISSING: 20 / 14 / 0. The FULL count increased by eight (owner approvals, reservation operations, master calendar, owner blocks/conflicts, maintenance, work orders, cleaning/readiness and inspections).
- Backend commits: `ef9704f` (implementation/migration) and `73df48b` (workflow/concurrency tests).
- Frontend commits: `d196355` (professional operations UI) and `867702b` (M5 blocker browser journeys).
- Final pushed repository tip SHAs and the root/evidence SHA are recorded in the release handoff after push; generated browser screenshots/reports and runtime evidence remain intentionally excluded.
- Generated browser screenshots/reports and runtime evidence were intentionally excluded from the commits.
