# Property Manager P0 evidence

This record covers the P0 release only. It does not start P1/P2 work and does
not certify live payment, messaging, identity or banking providers.

## Scope and bases

- Backend frozen base: `c69f5321910a04020395d117dd4c503942c3a1de`
- Frontend frozen base: `d8f85f4af94720d8f08e1f89866dfafafadc015e`
- Delivery branches: `codex/pm-p0-money-authority` in both repositories
- P0 API prefix: `/api/property-manager/p0`
- Manager workspace: `/pm/p0` (aliases `/pm/finance`, `/pm/agreements`,
  `/pm/approvals`, `/pm/team`)
- Owner money workspace: `/owner/p0`

## P0 implementation matrix

| Area | UI journey | API/persistence | Local/test status |
| --- | --- | --- | --- |
| Owner management | Manager owner selector, profile editor, lifecycle timeline, portfolio filters and assignment history | `owners/*`, `portfolio`, `assignments/*`; P0 owner/profile/lifecycle/assignment tables | FULL — local/test |
| Management agreements | Draft editor, document association, activate, renew and terminate actions; owner read view | `agreements/*`; version, supersedes, effective-period and scope columns | FULL — local/test |
| Management fee engine | Rule editor, deterministic preview and posting | `fees/*`; percentage/fixed/minimum/markup/combined rules, effective-period overlap checks and fee journals | FULL — local/test |
| Financial ledger | Chart of accounts, balanced journal form, drill-down, reconcile and reverse controls | `accounting/*`; separate JMD/USD owner/PM/third-party accounts, balanced/one-sided constraints, immutable triggers, suspense/reconciliation | FULL — local/test |
| Owner payouts | Availability, create, second-person approval, process, cancel/retry and history controls | `payouts/*`; payout batches/items/events, idempotency, row versions and owner-funds payout journal | FULL — local/test |
| Statements and profitability | Period preview/finalize/export and owner/property profitability cards | `statements/*`, `profitability`; immutable hashed snapshots and CSV/JSON export | FULL — local/test |
| Owner approvals | Manager queue and owner portal approve/reject/request-changes controls | `approvals/*`; threshold, agreement/fee links, evidence IDs, append-only status history and outbox notification | FULL — local/test |
| Team and RBAC | Invite, accept, scope, capability, limit, suspend/revoke and staff-history controls | `members/*`; owner/property scopes, finance/payout capabilities, optimistic row versions and event history | FULL — local/test |
| Owner Portal 2.0 | Owner-only agreements, approvals, ledger, statement, payouts and property financials | `owner/portal`; manager relation and owner identity are checked server-side | FULL — local/test |

`FULL — local/test` means the complete persistence → business rule →
authorization → API → frontend journey is covered by the local PostgreSQL/
browser gate. It does not mean live provider certification or legal accounting
certification.

## Database evidence

The additive EF migrations are:

- `20260909235056_AddPropertyManagerP0Model`
- `20260910002207_AddP0PayoutCreatedByLink`
- `20260910005142_AddP0OwnerBillingMetadata`
- `20260910012744_AddP0StatementIdempotency`
- `20260910014625_AddP0ReversalUniqueness`
- `20260910120000_AddPropertyManagerP0Foundation`

The local PostgreSQL verification database contained 16 `milestone_p0_*`
tables, balanced-journal and one-sided-line checks, 9 append-only rejection
triggers (including property-assignment history), and the P0
idempotency/reversal indexes. Posted journal, line, reconciliation, snapshot,
lifecycle, assignment and decision history rows cannot be updated or deleted;
corrections use a new reversal or event.

## Automated and browser evidence

Run from the repository roots:

```powershell
# Backend repository root
dotnet build NestyStay.sln --no-restore
dotnet test NestyStay.sln --no-build --no-restore

# Frontend repository root (use a second shell or return to the parent)
npm run typecheck
npm test -- --run
npm run build
npm run lint

# critical P0 browser journeys (PostgreSQL-backed API)
$env:ConnectionStrings__Postgres = "Host=127.0.0.1;Port=55432;Database=nestystay_dev;Username=nestystay"
$env:BackgroundJobs__Enabled = "false"
$env:PLAYWRIGHT_API_URL = "http://localhost:5019/api/health"
npx playwright test e2e/property-manager-p0.spec.ts e2e/property-manager-p0-browser.spec.ts --project=desktop-chromium --project=tablet-chromium --project=mobile-chromium
```

The P0 API tests cover owner/profile metadata, agreements, fee posting,
currency separation, third-party payable separation, balanced immutable
journals, reversals, suspense blocking, statement snapshots, idempotency,
second-person payout approval, staff history, owner-portal isolation and
property-scoped staff isolation. The browser journey covers manager sign-in,
profile save, agreement activation, real journal posting, statement preview,
owner sign-in and owner-scoped persisted data.

Latest local gate results:

- Backend: 146 passed (5 Domain, 23 Application, 19 Infrastructure, 99 API)
  in both Debug and Release test runs; zero failures.
- Frontend: 36 passed across 6 Vitest files; typecheck and production build
  passed; lint exited 0 with 156 pre-existing warnings and no errors.
- Playwright: 100 passed and 52 credential/provider-gated skips in the full
  152-test matrix; the dedicated P0 manager/owner lifecycle passed 9/9 at
  desktop (1440px), tablet (1024px) and mobile (390px) Chromium sizes.
- EF model gate: `dotnet ef migrations has-pending-model-changes` reported no
  pending model changes.

## Provider and production separation

- Stripe local/test Payment Element and wallet UI: PASS where configured.
  Live wallets, webhook signatures, Connect accounts and live refunds remain
  BLOCKED until client-owned Stripe credentials, merchant domains and webhook
  endpoints are supplied.
- SMS local/test adapter: PASS when the feature flag is enabled. Real SMS
  delivery remains BLOCKED until the client supplies provider credentials.
- Alibaba eKYC local/file/camera application path: PASS locally. Real Alibaba
  callbacks and production validation remain BLOCKED until client credentials
  and provider configuration are supplied.
- No claim of Jamaican legal trust-accounting compliance is made by this
  application ledger.

## Release decision

P0 is complete for local/test use when the commands above are green and both
repository worktrees are clean. Production readiness remains NO until live
provider, operational, legal/accounting and deployment sign-off gates are
completed separately.
