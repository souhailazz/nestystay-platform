# Client verification guide

1. Start PostgreSQL, the API on `http://localhost:5019`, and the Vite app on `http://localhost:5173`.
2. Register a `Property manager` and an `Owner` account. Complete the development 2FA challenge in local development.
3. Open `/pm/dashboard`: invite the registered owner, assign a unit, issue an invoice, allocate a utility, publish a notice, add a vendor, create a gate message, open a governance proposal, store a PDF/JPEG/PNG, and issue a QR.
4. Open `/owner/dashboard` as the owner: confirm only assigned units, invoices, utility-linked invoice, statement, maintenance request, notices, documents and governance are returned. Pay a balance and print the statement.
5. Open the QR link or `/gate?token=...`: click **Validate access** and confirm approved, wrong-property, expired and revoked decisions are visible.
6. Run `dotnet test backend/NestyStay.sln --no-restore`, `npm test`, `npm run build`, and `npx playwright test e2e/m5-property-manager.spec.ts`.

Live Stripe and Alibaba checks require client-provided credentials and are intentionally not simulated.
