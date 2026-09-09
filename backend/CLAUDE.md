# Claude instructions for the backend repository

- Read `README.md` and the existing API/domain/application code before editing.
- Preserve existing endpoint paths, JSON shapes, authorization policies and EF migrations.
- Never commit populated `.env`, credentials, webhook secrets or private keys.
- Use `backend/.env.example` and `backend/.env.production.example`; do not invent variable names.
- For split hosting, keep CORS explicit and keep frontend/API origins under the same parent domain unless the deployment uses a same-origin reverse proxy.
- Run `dotnet build NestyStay.sln -c Release` and the relevant `dotnet test` command after changes.
- Report external-provider limitations honestly; do not replace real Stripe/eKYC validation with fake success.
