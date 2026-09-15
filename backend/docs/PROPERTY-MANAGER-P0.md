# Property Manager P0: Money & Authority

P0 adds the owner-management and financial-control foundation without changing
the existing M1–M4 endpoints. It is available under `/api/property-manager/p0`
and in the web workspace at `/pm/p0` (the owner-facing view is `/owner/p0`).

## Operating rules

- Balances are separate per currency (`JMD` and `USD`); the service never
  performs an implicit FX conversion.
- Owner/client funds, PM revenue, third-party payable and suspense are separate
  account classes. Every posted journal has equal debit and credit totals.
- Posted journals and lines are immutable in EF and PostgreSQL. Corrections are
  made with a reasoned reversal journal; reconciliation is an append-only row.
- Statements are accrual summaries. Payout availability is cash-based and only
  reconciled cash is payable. Suspense blocks final statements and payouts.
- A payout creator cannot approve the same batch. Team membership scopes and
  finance/approval capabilities are checked on the server for every P0 route.
- This is an auditable application ledger; it is not a claim of Jamaican legal
  trust-accounting compliance.

## Local verification

```powershell
dotnet ef database update --project src/NestyStay.Infrastructure --startup-project src/NestyStay.Api
dotnet test NestyStay.sln
cd ../frontend
npm run typecheck
npm run build
npm test -- --run
```

Use the existing test/local provider configuration. Production Stripe,
connected payouts, SMS and any external provider certification remain separate
release gates and require client-owned credentials.

## Main workflow

1. Link an owner to the manager and save the owner profile.
2. Create and activate a versioned management agreement, optionally linked to a
   scoped document.
3. Create an effective, non-overlapping fee rule.
4. Post balanced rent/expense journals and reconcile bank activity.
5. Build a period statement, resolve suspense, then finalize an immutable
   snapshot and export CSV/JSON.
6. Create a payout batch from reconciled availability. A different finance
   approver approves it, then the manager processes the local/test payout.

All mutations create an audit event and applicable owner notification outbox
item. The generated OpenAPI document exposes the request/response contracts.
