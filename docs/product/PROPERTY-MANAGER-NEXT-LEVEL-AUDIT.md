# NestyStay Property Manager — next-level product audit

**Audit date:** 2026-09-02  
**Scope:** Property Manager / owner-portal product only (M5). M1–M4 features are out of scope except where a dependency affects PM operations.  
**Audit mode:** Analysis only. No production code, API, database migration, or frontend behavior was changed for this audit. The only artifact created is this report.  
**Audited commit:** `750f879dde7ff7366f7ee72bc14f638fb7f823d6` (`Record final release evidence SHA`)  
**Worktree at audit start:** clean.

## Executive decision

NestyStay has a real, manager-scoped Phase 5 foundation: owner invitations and review, property assignment, invoices and payments, ledger statements, utility allocation, maintenance, vendors, community notices, governance/proxy voting, document storage, gate messages, QR lifecycle, subscription renewal, and a real owner portal. The manager and owner records are PostgreSQL-backed, server-authorized, and exercised through real API/UI journeys.

The product is **not yet a professional multi-owner PMS**. It is best classified as **INTERMEDIATE**: materially beyond a visual prototype and suitable for a local Phase 5 web release candidate, but missing the operating system that makes a management business scalable and financially controlled. The largest gaps are management agreements and fee rules, reservations/master calendar/channel operations, owner money/payout/trust accounting, work orders and inspections, team/RBAC, bulk operations, workflow automation, and truthful live reporting.

The current PM surface also has an important evidence distinction:

- `/pm/dashboard` is the main real API-backed workspace and includes the dashboard, forms, lists, and enhancement hub.
- `/owner/dashboard` is a real owner-scoped API-backed portal.
- `/gate` is a real QR validation interface.
- `/pm/utilities`, `/pm/verification`, and `/pm/reports` are routed to static design-spec screens in `frontend/src/pages/SpecScreens.tsx`; their sample values are explicitly marked with `SampleDataChip` and are not connected to the PM API.
- `/pm/invoices`, `/pm/maintenance`, `/pm/governance`, `/pm/documents`, and `/pm/gates` currently resolve to the same `PropertyManagerDashboardPage`, so navigation works but does not provide dedicated focused screens.

Therefore, “M5 locally complete” in the acceptance documents means the signed Phase 5 acceptance journeys pass; it does not mean the product already contains every professional PMS module listed in this audit.

## Evidence inspected

| Evidence | Finding |
|---|---|
| `backend/src/NestyStay.Api/Controllers/PropertyManagerController.cs` | 26 HTTP actions cover dashboard, owners, subscription, properties, invoices, statements, utilities, maintenance, vendors, notices, governance, documents, gate messages, QR, and owner portal. |
| `backend/src/NestyStay.Application/PropertyManager/PropertyManagerModels.cs` | DTOs and request models show the actual M5 contract surface; there are no agreement, fee-rule, payout, reservation, inspection, work-order, expense, task, or team models. |
| `backend/src/NestyStay.Infrastructure/Persistence/Milestones/EfPropertyManagerStore.cs` | Real EF/PostgreSQL reads/writes, manager/owner scope checks, ledger entries, local payment adapter, storage provider, audit writes, governance and QR lifecycle. |
| `backend/src/NestyStay.Infrastructure/Persistence/Milestones/MilestoneEntities.cs` | M5 tables are manager, owner, property, invoice, invoice line, payment, ledger entry, utility, vendor, maintenance, notice, proposal/voter/vote/proxy, document, gate message, QR access and QR scan. |
| `frontend/src/pages/PropertyManagerPages.tsx` | Real dashboard, owner portal and guard interface. Most manager actions call `frontend/src/lib/api.ts`. |
| `frontend/src/pages/PropertyManagerEnhancementHub.tsx` | Real statement/payment/verification/document-download actions mixed with local-only staged preferences and labels for future persistence. |
| `frontend/src/pages/SpecScreens.tsx` | Static/sample utility, verification, portfolio report, and insurance screens. |
| `frontend/src/App.tsx` | PM route aliases and component mapping; several routes render the same dashboard component. |
| `frontend/e2e/m5-property-manager.spec.ts` | API-seeded owner/property/invoice/utility data is shown in manager and owner UI; real QR validation is shown. |
| `frontend/e2e/final-hardening-m5.spec.ts` | Real API/PostgreSQL seed, cookie session, manager dashboard, maintenance update, UI QR issue/revoke, owner maintenance, gate denial, axe and overflow checks. |
| `docs/testing/M1-M5-TRACEABILITY.md` and `docs/testing/M5-PROPERTY-MANAGER-ACCEPTANCE.md` | Local M5 acceptance is PASS for the defined Phase 5 scope; unsupported commercial/provider decisions are called out. |
| `docs/deployment/PRODUCTION-READINESS.md` and `reports/FINAL-HARDENING-REPORT.md` | Production is NO; live Stripe/Alibaba/email, client infrastructure, backups, observability, distributed coordination, and operational controls remain external gates. |
| `testing-evidence/final-hardening/07-browser/m5-*.json` | Seven viewport/browser artifacts exist. Six are clean; `m5-desktop-firefox.json` records an external Google Fonts console error and `passed: false`, while the addendum summarizes the functional journey as 7/7. |

## What is implemented well today

1. **Scope isolation and authorization.** Every M5 aggregate carries manager/owner scope, manager and owner reads are filtered server-side, and the authorization matrix reports 54/54 cases passed. Cross-owner invoice, statement, document, maintenance, property, governance, QR, and portal access is tested.
2. **Real financial primitives.** Invoice lines are server-calculated, tax is rounded server-side, payments use an idempotency key and local payment adapter, utilities create invoice and ledger rows transactionally, and statements calculate opening/closing balances.
3. **Owner-facing value.** The owner portal returns assigned properties, invoices, payments/statements, utilities, maintenance, notices, proposals, and documents. Owners can pay an invoice, report maintenance, vote, and grant a proxy.
4. **Operational basics.** Managers can invite/review owners, assign properties, issue invoices, allocate utilities, create/update maintenance, register vendors, publish notices, open proposals, store/download approved document types, communicate with gate staff, and issue/revoke QR access.
5. **Security and integrity baseline.** HttpOnly cookie sessions plus CSRF, manager/owner scope checks, 45 reviewed M5 FKs, zero enforced M5 orphan rows, storage validation, hashed QR tokens, audit writes, rate limits, and local concurrency/financial checks are present.
6. **Responsive web foundation.** The exercised manager, owner, and gate journey has desktop/tablet/mobile coverage, no measured overflow, and zero axe violations in the representative hardening scan.

## Current maturity matrix

The ratings describe the product today, not the amount of code that could be added. “GOOD” means a usable Phase 5 workflow, not parity with a professional PMS. “PARTIAL” means a real slice exists but the main screen, data model, or workflow is incomplete. “MISSING” means no meaningful PM capability was found in the inspected implementation.

| Area | Current capability | Current maturity | Upgrade possibilities | Business value | Priority |
|---|---|---:|---|---|---:|
| Manager dashboard | PostgreSQL-backed totals for owners, properties, balances, invoices due, open maintenance, pending verification, and gate scans; configurable KPI visibility is client-local. | GOOD | Server-persisted layouts, role-aware widgets, portfolio filters, alert center, task queue, trend charts, saved views, dashboard deep links. | Faster daily triage and less spreadsheet work. | P1 HIGH VALUE |
| Owner records | Invite an existing owner account by email, duplicate detection, verification status, invitation status, community link, manager scoping. | BASIC | Contact profile, multiple ownership interests, ownership percentage, entities/trusts, tags, contacts/consents, invitation resend/cancel/expiry history, CRM timeline. | Accurate owner communication and fee allocation. | P0 CRITICAL |
| Multi-owner management | One manager can have many owner rows; each property has one owner ID. | PARTIAL | Joint owners, ownership percentages, beneficial owners, owner groups, primary contact, portfolio/company hierarchy, historical assignments. | Supports real portfolios and avoids incorrect statements/payouts. | P0 CRITICAL |
| Management agreements | No agreement entity, template, effective date, signed copy, terms, or renewal workflow. | MISSING | Agreement templates, e-sign, fee schedule, authority limits, notice period, signature/audit chain, versioning, expiry alerts. | Legal authority and fee calculation foundation. | P0 CRITICAL |
| Management fees | Invoices can contain arbitrary lines; no fee-rule engine tied to agreement or revenue. | BASIC | Percentage/fixed/hybrid rules, per-booking/per-unit fees, tax handling, overrides, effective dates, proration, owner approval. | Predictable margin and correct owner statements. | P0 CRITICAL |
| Owner ledger | Charge/payment/utility rows exist and statements calculate balances per owner/manager. | BASIC | Double-entry ledger, credits/refunds/adjustments, allocation dimensions, immutable entries, reconciliation, period close, audit explanations. | Trustworthy client-money reporting. | P0 CRITICAL |
| Owner payouts | No owner payout entity, approval, batch, bank destination, status, or settlement workflow. | MISSING | Payable calculation, approval queue, payout batches, bank/Connect rail, failed/reversed states, remittance advice, reconciliation. | Core promise of a management business. | P0 CRITICAL |
| Wellness payouts | Wellness officer payout domain exists separately; PM owner payouts are not conflated with it. | BASIC | Explicit cross-domain reporting boundary, no shared owner money path, separate permissions and reconciliations. | Prevents money and reporting contamination. | P1 HIGH VALUE |
| Owner statements | API statement with date filters, opening/closing balance, entries, invoices and payments; owner can print and manager can CSV export client-side. | GOOD | Server-generated PDF/CSV, transaction drill-down, owner/property filters, statement lock, branding, delivery and acknowledgement. | Fewer disputes and better owner trust. | P1 HIGH VALUE |
| Reservation operations | General guest booking exists elsewhere; no PM reservation queue, owner booking attribution, approval, or PM booking endpoint. | MISSING | Reservation inbox, search/filter, status transitions, guest identity, owner/property attribution, cancellation/refund, payment reconciliation. | Turns the PM into the operational system of record. | P0 CRITICAL |
| Master calendar | No PM portfolio calendar combining bookings, owner blocks, maintenance, cleaning, inspections, and gate events. | MISSING | Unified calendar, resource lanes, conflict detection, timezone support, drag/reschedule, iCal/channel sync. | Prevents double bookings and missed work. | P0 CRITICAL |
| Owner stays/blocks | Property has only `occupancyStatus`; no owner stay/block records. | MISSING | Owner-use requests, approval, blackout blocks, minimum notice, conflict rules, owner calendar. | Protects owner access without manual calendars. | P1 HIGH VALUE |
| Channel management | No Airbnb/Booking.com/VRBO/channel adapter or mapping. | MISSING | Channel connections, listing mapping, availability/rate sync, import idempotency, error queue, audit and disconnect. | Expands distribution and reduces manual updates. | P2 USEFUL |
| Cleaning | No turnover/cleaning task entity or schedule. | MISSING | Auto-generated turnovers, cleaner assignment, checklist, supplies, photos, QA, cost and invoice link. | Better readiness and fewer guest issues. | P1 HIGH VALUE |
| Inspections | No inspection record, checklist, finding, photo, or sign-off model. | MISSING | Move-in/out and routine inspections, templates, findings, severity, owner approval, remediation link. | Preserves condition evidence and reduces disputes. | P1 HIGH VALUE |
| Inventory/assets | No asset, appliance, inventory count, warranty, serial number, or depreciation model. | MISSING | Asset register, room/area inventory, replacement cycles, warranty expiry, stock minimums, barcode/QR. | Controls replacement cost and unit readiness. | P2 USEFUL |
| Maintenance | Real owner request and manager status/vendor/schedule/cost/notes flow; list and client-only Kanban. | BASIC | Work orders, assignment, quotes, approval thresholds, SLA timestamps, attachments, recurring preventive tasks, resident updates, expense posting. | Shorter resolution time and controlled spend. | P0 CRITICAL |
| Owner approvals | Owner verification and governance vote/proxy exist; maintenance/expense/quote approval does not. | PARTIAL | Approval policies by amount/type, approve/reject/request changes, delegated authority, reminders, full decision log. | Keeps managers within client authority. | P0 CRITICAL |
| Vendors | Real manager-scoped create/register record with category, contact, notes, status and active flag. | BASIC | Insurance/licence expiry, rates, service radius, availability, ratings, contracts, quotes, preferred tags, spend history. | Safer sourcing and better vendor performance. | P1 HIGH VALUE |
| Work orders | No separate work-order aggregate; maintenance is the work item. | PARTIAL | Work-order number, trade, job scope, quote/PO, assignment, dispatch, status SLA, parts/labour, invoice match, closeout. | Operational control and auditable cost. | P0 CRITICAL |
| Check-in/out | Guest booking check-in/out data exists in the general booking domain, not in PM operations. | PARTIAL | PM arrival/departure board, late checkout, key/QR issuance, cleaning trigger, incident handoff. | Makes daily operations actionable. | P1 HIGH VALUE |
| Readiness | No unit readiness state or turnover checklist; QR/gate and maintenance are separate. | MISSING | Ready/dirty/inspected/out-of-order states, blockers, owner/guest arrival deadline, housekeeping SLA. | Prevents selling unready units. | P1 HIGH VALUE |
| Incidents/damages | No PM incident or damage case entity. | MISSING | Incident intake, severity, parties, evidence, notifications, linked booking/inspection, resolution and insurer workflow. | Limits loss and creates defensible records. | P1 HIGH VALUE |
| Security deposit / claims | No deposit or claims workflow was found; this audit does not assume it is a signed M5 requirement. | MISSING | Optional policy module with jurisdictional rules, authorization, evidence, notice, dispute, refund and ledger integration. | Revenue protection only after legal review. | P2 USEFUL |
| Expenses | Maintenance cost is a field; no expense, receipt, coding, approval, payable, or reimbursement model. | PARTIAL | Expense capture, OCR/manual receipt, property/unit/vendor/category, bill approval, reimbursement, owner allocation and ledger link. | Explains profitability and supports tax evidence. | P0 CRITICAL |
| Receipts/docs | Validated PDF/JPEG/PNG storage and scoped download via storage abstraction; no document workflow. | BASIC | Folders, tags, search, preview, version history, expiry reminders, retention, permissions, bulk download, e-sign links. | Lower operational risk and faster compliance response. | P1 HIGH VALUE |
| Utilities | Dashboard form creates a real utility charge, invoice and ledger row transactionally. Dedicated `/pm/utilities` screen is static sample data. | PARTIAL | Meter readings, photo proof, allocation rules, bill import, usage history, anomaly detection, approval, paid/unpaid and reconciliation. | Accurate bill-back and lower leakage. | P0 CRITICAL |
| Owner portal | Real scoped portal with properties, balance, invoices/payments, statement, utilities, maintenance, notices, governance, proxy and documents. | GOOD | Owner home, payouts, approvals, reservations, statements, documents, messaging, profile, preferences, multi-manager support. | Retention, transparency and lower support load. | P1 HIGH VALUE |
| Portfolio reporting | `/pm/reports` is a static sample report with hardcoded revenue, occupancy, spend and net payout. | PARTIAL | Live portfolio KPIs, owner statements, occupancy/revenue/margin, exception reports, CSV/PDF/tax exports, scheduled delivery. | Enables decisions and client reporting. | P0 CRITICAL |
| PM business dashboard | Basic portfolio KPIs and alert strip; no PM company revenue, cost, margin, utilization, SLA, or staff views. | BASIC | Company P&L, client profitability, work backlog, collections, occupancy, staffing, cash forecast, saved dashboards. | Runs the management company, not just properties. | P1 HIGH VALUE |
| Profitability | No PM-specific revenue/cost/margin aggregation; sample reports are not evidence. | MISSING | Property/owner/unit profitability, fee revenue, pass-through costs, contribution margin, forecast and variance. | Identifies profitable clients and pricing needs. | P0 CRITICAL |
| Client vs company money | Separate manager/owner ledger rows exist, but no trust account, bank account, double-entry, reconciliation, or legal controls. | BASIC | Conceptual accounting boundary, chart of accounts, trust/client accounts, company operating account, transfers, three-way reconciliation, close and approvals. | Financial safety and jurisdictional compliance. | P0 CRITICAL |
| Team / employees / RBAC | Role-level API authorization exists for PropertyManager, Owner, Admin and other platform roles; no PM staff/team model. | BASIC | Staff invitations, least-privilege permissions, property/owner scopes, approval limits, shift/assignment, offboarding and access review. | Supports delegation without overexposure. | P0 CRITICAL |
| Portfolio organization | Manager/owner/property/community IDs provide basic scoping; no building/portfolio/region hierarchy. | BASIC | Portfolio → community → building → unit hierarchy, tags, custom fields, ownership history and saved filters. | Scales navigation and reporting. | P1 HIGH VALUE |
| Bulk operations | Owner list has client-side search/sort/pagination/export; staged assignment is local-only. | PARTIAL | Bulk assign, approve, invoice, notify, archive, export, import preview and undo. | Major time saving at portfolio scale. | P1 HIGH VALUE |
| Import/export | Owner CSV export and client-side statement CSV; print-to-PDF only. | BASIC | Validated CSV/XLSX imports, dry-run, duplicate resolution, export jobs, branded PDF, tax/accounting formats. | Accelerates migration and accountant handoff. | P1 HIGH VALUE |
| Document management | Real storage and authorized download with basic category/title/access scope. | BASIC | Document taxonomy, metadata, versioning, expiry, retention, OCR/index, signatures, access log, bulk actions. | Makes every owner/property file retrievable. | P1 HIGH VALUE |
| E-sign | No e-sign provider or signature envelope model. | MISSING | Template fields, signing order, reminders, decline/void, signed artifact and audit certificate. | Closes agreements and approvals faster. | P0 CRITICAL |
| Tasks | Maintenance is the only durable task-like record; reminders are local feedback. | PARTIAL | Task entity, assignee, due date, priority, dependencies, recurring tasks, SLA, comments and notification. | Creates accountability across the team. | P1 HIGH VALUE |
| Automation rules | No server rule engine; some UI copy says reminders are queued or local preferences are saved. | MISSING | Event/condition/action rules, dry run, retries, pause, audit, notification templates and human approval gates. | Reduces repetitive PM work. | P1 HIGH VALUE |
| Activity timeline | Writes create `MilestoneAuditEvent` records; there is no PM activity feed/read API. | BASIC | Entity timeline, filters, actor/reason, before/after diff, export, retention and owner-visible subset. | Traceability and dispute resolution. | P1 HIGH VALUE |
| Owner CRM | Owner name/email/status are stored; no CRM notes, contacts, reminders, segmentation, communication history or consent center. | BASIC | Owner 360, contacts, tags, lifecycle, communication preferences, opportunities, tasks and message history. | Better retention and service. | P2 USEFUL |
| Property onboarding | Add-property form creates an active vacant unit with title/unit/address. | BASIC | Guided intake, required evidence, readiness checklist, photos, utilities, access, owner agreement and approval. | Faster and safer client onboarding. | P1 HIGH VALUE |
| Offboarding | No property/client termination workflow. | MISSING | Notice period, freeze new reservations, final statement/payout, document export, access revocation, archive and retention. | Prevents messy exits and data mistakes. | P1 HIGH VALUE |
| Audit/compliance | Audit writes, FK hardening, auth tests and storage checks exist; no PM compliance dashboard or retention policy UI. | BASIC | Compliance calendar, evidence packs, consent/retention, access review, incident register and exportable audit report. | Reduces operational and legal risk. | P1 HIGH VALUE |
| Mobile | Responsive web and guard-first QR interface pass measured viewport checks; no native PM app/offline sync. | GOOD | Mobile task inbox, camera receipts/inspections, push, offline drafts, background sync and biometric unlock. | Enables field work. | P2 USEFUL |
| Notifications/messaging | Invitation auth flow and platform email/outbox infrastructure exist; PM-specific delivery history, templates and in-app inbox are absent. | BASIC | Notification center, templates, event preferences, SMS/push, delivery/retry/complaint status. | Fewer missed approvals and visits. | P1 HIGH VALUE |
| Optional AI | No PM AI feature found. | MISSING | Optional assistant for triage, anomaly explanations, draft notices, document extraction and suggested replies with human approval. | Productivity, only after data controls. | P3 OPTIONAL |

## Detailed audit and upgrade design

### 1. Multi-owner and management-relationship model

The current model is one `MilestoneManagerOwner` row per manager/owner pair and one `OwnerUserId` on each managed property. This is enough to prove manager scoping, but it cannot represent joint ownership, an owning company/trust, ownership percentages, a primary contact, or a property changing hands over time. The first foundational module should be a versioned ownership/management relationship model:

- `OwnerParty` (person, company, trust) and contacts.
- `PropertyOwnership` with percentage/units, effective dates, primary contact, and historical rows.
- `ManagementAgreement` with start/end, authority limits, fee schedule, notice period, signed artifact, status, and version.
- A relationship-level authorization policy used by every invoice, statement, reservation, maintenance, document, and payout query.

Do not put these fields into the existing property row as ad-hoc columns. They are time-dependent business facts and need effective-dated records plus an immutable audit trail.

### 2. Fees, ledger, payouts, and trust accounting

The current ledger is useful as a Phase 5 balance projection: charges are positive, payments are negative, and statements sum entries. It is not a client-money accounting system. A production-grade design must keep three concepts distinct:

1. **Owner/client money:** rent or booking proceeds held for the owner, owner-paid expenses, reserves, refunds, and the owner payable balance.
2. **Management-company money:** management fees, subscription revenue, mark-ups, reimbursements, and operating expenses.
3. **Third-party/pass-through money:** taxes, utilities, commissions, wellness charges, deposits, and other amounts that are not PM revenue.

This is a conceptual product recommendation, not legal/accounting advice. Trust-account rules, segregation, licensing, tax treatment, and record-retention obligations must be confirmed with Jamaican counsel and an accountant before implementation. The application should not label the current ledger as a compliant trust ledger.

Recommended sequence:

- Define a chart of accounts and money ownership for every transaction type.
- Record immutable double-entry journal entries with source document and approval references.
- Add bank/client-account abstractions and reconciliation states.
- Calculate owner statement and payout from a closed period, not from mutable invoice totals.
- Add a separate management-company P&L and a separate Wellness officer payout/reporting boundary.
- Add payout approval, batch, settlement, reversal, failed-payment, remittance and dispute states.

### 3. Reservation, calendar, and channel operations

The general platform has booking entities, but no PM-specific operational view was found. A manager cannot currently see all reservations for a portfolio, block dates for owner use, coordinate cleaning, or resolve a maintenance conflict from the PM workspace. These are not cosmetic upgrades; they are the operational spine of a PMS.

The target is a portfolio calendar with lanes for property, reservation, owner block, maintenance/work order, cleaning, inspection, and gate/visitor event. Every event must have a source, timezone, status, conflict policy, actor, and reschedule history. Channel adapters should be asynchronous and idempotent, with a visible sync/error queue rather than silently overwriting local state.

### 4. Maintenance, work orders, cleaning, inspection, inventory, and incidents

Current maintenance is a good request-to-status seed, but status plus notes is not a work-order system. The target workflow is:

`request → triage → quote/approval → work order → assignment → scheduled → in progress → inspection/QA → expense/receipt → owner update → closed`

Attach photos, vendor quote, work scope, SLA timers, parts/labour, owner approval threshold, and linked accounting entry. Cleaning and inspection should consume booking and readiness events. Incidents/damages should link to booking, inspection, QR/gate event, evidence, notification and resolution. Security deposit/claims should remain a separately reviewed policy module, because the legal rules are jurisdiction-specific.

### 5. Owner approvals, portal, CRM, and communication

The owner portal is already valuable and should remain owner-scoped. The next layer is not more cards; it is a reliable owner decision loop:

- approval inbox for quotes, expenses, reservations, owner blocks, payouts and documents;
- plain-language decision summary, amount, deadline, supporting evidence, approve/reject/request changes;
- delegated approver and expiry handling;
- acknowledgement and notification history;
- owner 360 profile with properties, agreements, statements, payout history, maintenance, messages and contacts;
- multiple manager relationships handled explicitly instead of selecting the first manager in the portal.

The current proxy form asks owners to paste proposal and proxy IDs. A production experience should present eligible proposals and owners from API data, show expiry/conflict rules, and provide revoke/history actions.

### 6. Reporting, dashboard, and profitability

The static `/pm/reports` screen is useful as a design direction but must not be treated as live business reporting. Its hardcoded values include portfolio revenue, average occupancy, maintenance spend, utilities billed back, PM fees, and net payout. These metrics need definitions before implementation:

- gross booking revenue vs PM fee revenue;
- owner payable vs company revenue;
- occupancy denominator and blocked/owner-use treatment;
- maintenance committed vs paid;
- utility billed vs collected;
- forecast vs actual;
- period close and restatement policy.

The next dashboard should expose exceptions first (overdue invoices, unapproved quotes, expiring agreements, unready arrivals, SLA breaches, failed syncs, pending payouts), then trends and drill-downs. Every number should link to the source records and export with a report timestamp and filter definition.

### 7. Team, portfolio hierarchy, bulk work, and controls

Role-level authorization is a strong baseline, but a property-management company cannot give every employee the same manager role. Add staff users, team membership, property/community scopes, approval limits, shift/on-call assignment, and access-review/offboarding. Keep the existing server-side resource checks and extend them to staff scope claims.

Bulk operations should use a preview/confirm pattern: select records, show impact, validate duplicates/permissions, commit as a job, show per-row results, and offer a recoverable undo where possible. This is safer than client-only checkboxes and staged local state.

### 8. Documents, e-sign, onboarding, and offboarding

The storage abstraction and MIME/size/name checks are good foundations. The product still needs document metadata and lifecycle: folder/type, owner/property/manager scope, expiry, retention, version, signature status, access history, and download/export. E-sign should create a signed artifact and audit certificate, not merely mark a checkbox.

Property onboarding should collect agreement, ownership proof, unit details, photos, utilities, access instructions, vendor defaults, pricing, readiness and owner approval in a resumable checklist. Offboarding should freeze new work, produce a final statement/payout/export, revoke access, archive records, and apply retention rules.

### 9. Mobile and field operations

Responsive web is currently a GOOD baseline and is appropriate for the signed web-first scope. The next mobile investment should target field actions rather than reproducing every desktop screen: scan/validate, maintenance task, inspection checklist, photo/receipt capture, call/message vendor, readiness update, and offline draft/sync. Native apps remain optional and should not block the current local M5 release decision.

### 10. Optional AI

AI is optional and should follow, not precede, the data model and permissions work. Safe first uses are draft notices, summarize a maintenance thread, explain a utility anomaly, extract receipt fields for human confirmation, and suggest task priority. Do not let AI approve payouts, change accounting, or expose owner documents without explicit human authorization and an audit event.

## Top 10 upgrades ranked by business impact

| Rank | Upgrade | Why it is first | Backend/data shape | Frontend experience | Complexity |
|---:|---|---|---|---|---:|
| 1 | Management agreements + fee rules | Everything financial and authoritative depends on who manages what and at what price. | Agreement, party, effective-dated fee rules, signatures, authority limits. | Agreement wizard, fee preview, renewal/expiry, owner approval. | XL |
| 2 | Double-entry owner ledger + payout batches | Current balances are useful but not safe enough for client-money operations. | Journal, accounts, allocations, reconciliation, payout batch/settlement. | Owner statement drill-down, payout approval, remittance, exceptions. | XL |
| 3 | Portfolio reservation inbox + master calendar | Managers cannot run a portfolio without one operational timeline. | Reservation view, blocks, events, conflicts, sync jobs. | Calendar lanes, filters, drag/reschedule, conflict resolution. | XL |
| 4 | Work orders + approval/expense workflow | Maintenance must become controlled spend and accountable execution. | Work order, quote, approval, attachment, SLA, expense/receipt links. | Kanban/list, assignment, quote approval, vendor updates, closeout. | L |
| 5 | Live reporting/profitability | Static report figures cannot drive decisions or client trust. | Metric definitions, snapshots, dimensions, period close/export jobs. | Dashboard drill-down, owner packs, PDF/CSV/tax exports. | L |
| 6 | Team/RBAC and portfolio hierarchy | A real PM firm needs delegated work with least privilege. | Staff/team, scopes, approval limits, hierarchy, access review. | Team admin, assignment, scoped navigation, audit view. | L |
| 7 | Owner approval inbox + upgraded portal | Centralizes the decisions that currently happen outside the product. | Approval requests, decisions, reminders, acknowledgement. | Mobile-friendly inbox, evidence drawer, approve/reject/request changes. | M |
| 8 | Cleaning/readiness/inspection/incident module | Converts reservations into reliable physical operations. | Turnover, checklist, inspection, asset, incident, evidence. | Field checklist, photos, readiness board, owner/guest updates. | L |
| 9 | Document lifecycle + e-sign | Makes agreements, receipts and compliance evidence durable. | Version, expiry, retention, signature envelope, access log. | Search/filter, preview, signing, expiry queue, bulk export. | M/L |
| 10 | Bulk jobs/import/export + automation | Unlocks scale after core records are trustworthy. | Job queue, validation, rules, retries, audit/undo. | Preview/confirm, progress, row errors, saved automations. | L |

## Quick wins (high value, low-to-medium effort)

These are product improvements that can be delivered after the foundational roadmap without changing the accounting boundary:

1. Split the PM dashboard into real focused route pages or anchor sections; preserve the current API-backed data but make deep links land on the selected area.
2. Remove or clearly label `/pm/utilities`, `/pm/verification`, and `/pm/reports` as design previews until their API wiring exists; replace hardcoded trend/report figures with “not connected” states.
3. Add server-backed list endpoints with paging and filters for invoices, maintenance, vendors, documents, notices and QR access; do not rely on the dashboard’s first-20 slices.
4. Add QR active-list/history, revoke reason, expiry countdown, property name, and scan-result filters.
5. Replace raw proposal/proxy ID entry with API-backed owner/proposal selectors and add proxy revoke/expiry states.
6. Persist KPI layout, saved filters and portfolio preferences per manager instead of local React state.
7. Add real owner invitation resend/cancel/status history; the current resend button only announces locally.
8. Add maintenance validation for status/cost/notes, comments, attachments, vendor selector, and a persisted SLA timestamp rather than a label-only SLA.
9. Add document preview metadata, archive action, search result counts, and access history; keep the existing storage and scope checks.
10. Add in-app PM notification center backed by the existing outbox/event infrastructure, with delivery status and retry visibility.

## Major future modules

1. **Client and agreement registry** — owners, legal entities, agreements, contacts, authority, fees and renewals.
2. **Portfolio and unit master** — hierarchy, ownership history, unit configuration, assets, utilities, readiness and access.
3. **Reservations and distribution** — reservation inbox, calendar, owner blocks, channels, rates, availability and reconciliation.
4. **Financial operations** — chart of accounts, owner/client ledger, company ledger, expenses, receipts, trust/client accounts, reconciliation, statements and payouts.
5. **Operations** — maintenance/work orders, cleaning, inspections, inventory, incidents, damages and vendor performance.
6. **Owner experience** — approval inbox, portal, documents, e-sign, statements, payouts, messages and CRM.
7. **Team and workflow** — staff/RBAC, tasks, automation rules, notification center, escalation and audit timeline.
8. **Reporting and compliance** — live KPIs, profitability, owner reporting packs, tax exports, compliance calendar and evidence bundles.
9. **Field/mobile** — camera-first inspections, receipts, QR/gate, offline drafts and sync.
10. **Optional intelligence** — human-reviewed AI assistance over authorized records only.

## Recommended build sequence

### Release A — make the money and authority trustworthy

- Owner parties, ownership history and management agreements.
- Fee rules and agreement-linked invoice calculation.
- Double-entry ledger design and transaction classification.
- Expenses/receipts and approval thresholds.
- Owner payout batches and explicit Wellness payout separation.
- Live statement and profitability definitions.

### Release B — make daily operations manageable

- Reservation inbox and master calendar.
- Owner stays/blocks and conflict detection.
- Work orders, quotes, vendor assignment, cleaning and readiness.
- Inspections, assets, incidents and evidence.
- Team/RBAC and portfolio hierarchy.

### Release C — make the system scale and retain clients

- Owner approval inbox and upgraded portal.
- Document lifecycle and e-sign.
- Bulk jobs, import/export, task engine and automation.
- Live reporting, scheduled owner packs and compliance dashboard.
- Mobile field workflows, notifications and optional AI assistance.

## Professional PMS comparison

| Capability | Current NestyStay | Professional PMS expectation | Gap |
|---|---|---|---|
| Web foundation | Responsive manager/owner/gate web routes, real API and PostgreSQL scope. | Expected. | Small |
| Owner/client accounting | Basic invoice/payment/ledger statement. | Double-entry client accounting, trust controls, payout/reconciliation. | Large |
| Agreements/fees | Not modeled. | Effective-dated contracts and fee engine. | Critical |
| Reservations/calendar | General booking exists outside PM workspace. | Portfolio inbox, calendar, owner blocks, channels. | Critical |
| Maintenance | Request/status/vendor/cost seed. | Work orders, SLA, quotes, approvals, spend and closeout. | Large |
| Housekeeping/readiness | Not modeled. | Turnover, inspections and readiness board. | Large |
| Vendor management | Basic register. | Contracts, insurance, rates, performance and work orders. | Medium/large |
| Owner portal | Useful real scoped portal. | Approvals, payouts, reports, documents, messaging and self-service. | Medium |
| Reporting | Static sample PM report route plus dashboard totals. | Live, drillable, reconciled financial and operating reporting. | Large |
| Team/RBAC | Platform roles. | Staff scopes, teams, approvals and access reviews. | Large |
| Documents/e-sign | Validated storage/download. | Lifecycle, signatures, versions, retention and audit. | Medium/large |
| Automation/mobile | Responsive web, little PM automation. | Event-driven workflows and field/mobile execution. | Medium/large |

**Final maturity:** **INTERMEDIATE PROPERTY MANAGEMENT SYSTEM**.  
**Not yet:** ADVANCED or PROFESSIONAL PMS. The missing areas above are operating and financial control gaps, not merely visual polish.

## Explicit boundaries and non-blockers

- This audit does not reclassify the signed Phase 5 acceptance result. `docs/testing/M5-PROPERTY-MANAGER-ACCEPTANCE.md` remains PASS for its defined local scope.
- Native mobile apps, live Stripe/Connect, live Alibaba eKYC, real email/SMS/push delivery, client-owned infrastructure, production backups, monitoring delivery, WAF/TLS, distributed coordination, privacy-retention review, and external penetration testing remain production-only gates as documented in `docs/deployment/PRODUCTION-READINESS.md`.
- Security deposit/claims, trust accounting, tax, e-sign, and channel integrations require legal/commercial/provider decisions before being treated as contractual commitments.
- Wellness officer payouts are a separate domain and must not be silently merged with owner/client payouts.

## Audit-only conclusion

The current M5 implementation is a credible local Phase 5 web foundation with real persistence, authorization, and tested core workflows. It is not “everything a property manager needs” yet. The top next step is not another cosmetic dashboard pass: establish agreements, fee rules, trustworthy owner money/payout accounting, and the reservation/calendar/work-order operating spine. Only after those foundations should the product claim advanced or professional PMS maturity.
