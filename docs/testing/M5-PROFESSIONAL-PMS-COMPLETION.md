# NestyStay M5 professional PMS completion pass

This matrix records the state of the Property Manager scope after the additive professional-operations implementation. `FULL` means the persistence, API authorization, frontend journey and automated evidence are complete for the stated local/test scope. `PARTIAL` means a usable slice exists but one or more professional workflow requirements remain. Live Stripe, Alibaba, SMS, push and production deployment are reported separately.

| # | Area | Status | Evidence / exact remaining gap |
|---:|---|:---:|---|
| 1 | Owner records and multi-owner portfolio | FULL | P0 owner profiles, lifecycle and portfolio paging are persisted and scoped. |
| 2 | Property ownership history and assignments | FULL | Transactional P0 assignment API, history and browser coverage. |
| 3 | Management agreements | FULL | Draft/version/activate/renew/terminate, effective dates and document linkage are persisted and tested. |
| 4 | Management fee rules and calculation | FULL | Server-authoritative pricebook rules, overlap checks and posting integration. |
| 5 | Owner/client ledger | FULL | Balanced immutable journals, statement linkage and drill-down. |
| 6 | Separation of money | FULL | Client, PM and third-party account flags and server posting rules. |
| 7 | Reversals and auditability | FULL | Posted rows reject edits/deletes; reversals and reconciliation are append-only. |
| 8 | Owner payout batches and lifecycle | FULL | Availability, reserve, approve, process/fail/cancel/retry and history. |
| 9 | Owner statements | FULL | Finalized period snapshots, balances and CSV/PDF-compatible exports. |
| 10 | Portfolio profitability | FULL | Owner/property income, expense, fee and PM-margin calculations. |
| 11 | Owner approval workflows | PARTIAL | P0 decisions and history work; evidence is still ID-based rather than upload/preview in the PM UI, and maintenance/expense linkage is incomplete. |
| 12 | Owner portal | FULL | Owner-authorized agreements, approvals, statements, transactions, payouts and property financials. |
| 13 | Reservation operations | PARTIAL | New searchable reservation workspace supports scoped status/date changes and notes; guest-detail permissions, full modify/cancel UX and payment/action timeline remain incomplete. |
| 14 | Portfolio master calendar | PARTIAL | New API aggregates reservations, owner blocks and manager events; visual day/week/month grid, keyboard date interaction and conflict presentation remain. |
| 15 | Owner blocks and booking conflicts | PARTIAL | Persisted UTC blocks, category/notes, overlap validation, booking integration, manager + owner cancellation/history and desktop/tablet/mobile API browser coverage exist; richer conflict-resolution UX and operational conflict policies remain. |
| 16 | Maintenance lifecycle | PARTIAL | New durable REQUESTED→…→CLOSED transitions, optimistic concurrency, quotes and history; attachment/report/PDF and complete accounting linkage are not finished. |
| 17 | Work orders, quotes and expenses | PARTIAL | Quote records now have a scoped list/comparison endpoint and the UI can select a quote into an assigned work order; receipts, attachment evidence and ledger posting workflow remain. |
| 18 | Vendor management | PARTIAL | Existing vendor CRUD/documents and new maintenance quote linkage; contracts, licence/insurance expiry, service-radius/availability and performance UI remain. |
| 19 | Cleaning/readiness | PARTIAL | Persisted readiness tasks, checklist JSON, booking link and READY/NOT_READY status; photo/checklist template/offline/resumable UX and reservation readiness enforcement remain. |
| 20 | Inspections | PARTIAL | Persisted schedule/checklist/evidence/findings/sign-off API and UI; corrective-action/work-order linkage and full evidence workflow remain. |
| 21 | Assets and inventory | PARTIAL | New scoped register persists tag, category, description, serial/reference, purchase cost/date, warranty expiry, condition, location, quantity, JSON evidence, status and retire operation with stale-write protection; photos/docs, maintenance history and consumable transactions remain. |
| 22 | Incident tracking | PARTIAL | New scoped incident record with severity, evidence references, financial impact and resolution; privacy-specific views, upload/evidence timeline and insurance workflow remain. |
| 23 | Utilities and evidence | PARTIAL | Meter history, anomaly and disputes exist; evidence upload/preview, recurring billing execution and adjustment-to-ledger UX remain. |
| 24 | Documents/versioning/expiry/export | PARTIAL | Version/archive/export APIs exist; browser-safe preview, restore/version selection, granular permissions and ZIP status UX remain incomplete. |
| 25 | Community/gate operations | PARTIAL | Notices/comments/audience, gate delivery and QR flows exist; full delivery history/resend UX and cross-property audience administration remain. |
| 26 | Governance and proxy voting | PARTIAL | Proposal/vote/proxy persistence and basic UI exist; discussion attachments, quorum visualization, final proof and complete proxy lookup/revoke labelling remain. |
| 27 | Team/member management | PARTIAL | P0 invite/edit/suspend/revoke and scoped authorization are persisted; invitee acceptance/deep-link frontend and complete staff activity UX remain. |
| 28 | RBAC and scoped permissions | FULL | Server-side manager/owner/staff scope and finance/approval checks are covered by API/security tests. |
| 29 | Bulk operations/import/export | PARTIAL | Bulk property assignment/invoice issue/document export exist; generalized import, per-record result UX and all operational bulk actions remain. |
| 30 | Notifications/background automation | PARTIAL | Outbox, retries and scheduled services exist; provider-backed SMS/push delivery and complete operational reminders remain feature-flagged/provider-dependent. |
| 31 | Subscription/plan enforcement | FULL | Tier limits, lifecycle, retry, cancellation and scheduled downgrade are persisted and tested locally. |
| 32 | Reporting/KPIs | PARTIAL | P0 profitability and PM reports exist; maintenance/readiness/vendor/incident SLA KPI views are not consolidated. |
| 33 | Mobile/responsive manager workflow | PARTIAL | Existing responsive shell and new operations route pass desktop/tablet/mobile Chromium; full mobile calendar/table-to-card and sticky-action parity remain. |
| 34 | Audit history/security/concurrency | PARTIAL | New operations emit audit events and use EF concurrency/advisory reservation locks; complete cross-module timeline and distributed locking for every legacy workflow remain. |

## Verification evidence

- Backend Release tests: `dotnet test backend/NestyStay.sln --configuration Release --no-restore` — 148 passed, 0 failed (5 Domain, 23 Application, 19 Infrastructure, 101 API).
- EF/PostgreSQL: migrations `20260910095406_AddProfessionalPropertyManagerOperations`, `20260910103500_AddProfessionalOperationalConcurrency` and `20260910111240_AddAssetRegisterDetails`; `dotnet ef migrations has-pending-model-changes` reports no pending model changes against local PostgreSQL.
- Frontend: `npm test -- --run` (37 passed), `npm run typecheck`, and `npm run build` pass. `npm run lint` has 0 errors and 156 pre-existing warnings.
- Browser: `property-manager-professional.spec.ts` passes on desktop, tablet and mobile Chromium (3/3); the complete Playwright suite passes 106 with 52 intentionally skipped. The manager-scoped invitation rate-limit fix removes cross-test network throttling without weakening account/destination limits.
- Security/package checks: `dotnet list NestyStay.sln package --vulnerable --include-transitive` and `npm audit --omit=dev`.

This document does not claim Jamaican legal trust-accounting compliance and does not certify live external providers.
