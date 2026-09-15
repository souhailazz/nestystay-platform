# Property Manager scope (Phase 5)

This scope is reconciled to the signed agreement at `docs/contracts/NestyStay-Signed-Agreement-April-2026.pdf` and the client-added governance/proxy requirements. The signed agreement calls for a web-first property-management suite: multi-owner portfolio dashboard, community board and gate communications, invoicing/statements, owner portal, maintenance, utility tracking, owner/tenant verification, document storage, and subscription billing. It does not prescribe permanent Property Manager tier prices; the implementation therefore keeps tier and amount configurable and defaults the local adapter to zero until commercial configuration is supplied.

## Delivered locally

- Manager-scoped PostgreSQL records and signed role authorization.
- Owner invitation/review, property assignment, portfolio dashboard, invoices, line items, payments, ledger statements, and utility charges linked to invoices.
- Owner portal with persisted properties, balances, invoices, statements, maintenance requests, notices, governance and documents.
- Maintenance workflow, vendor register, community notices, gate messages, secure QR issue/validate/revoke and scan history.
- Anonymous governance ballots with eligibility, quorum inputs, duplicate-vote protection, proxy grants and server-side scope checks.
- Validated PDF/JPEG/PNG document storage with size/name/type checks and manager-scoped storage keys.
- Responsive manager, owner and gate routes wired to the real API.

## Boundary decisions

- Stripe application adapters and local test-mode capture are in scope for local verification; live Stripe/Connect credentials and provider validation remain deployment work.
- eKYC application adapters remain separate from live Alibaba validation; no live provider credential is fabricated.
- Native mobile apps are Phase 6 and are not a Phase 5 acceptance blocker.
- Production readiness requires deployment secrets, managed Postgres/backups, observability, provider credentials, storage configuration, and operational runbooks.
