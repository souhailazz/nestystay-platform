# Claude instructions for the frontend repository

- Read `README.md` and the existing route/API client code before editing.
- Preserve backend endpoint paths and response shapes; the API is a separate repository.
- Only `VITE_*` values may be used in frontend configuration. Never place secrets in the browser bundle.
- Never commit populated `.env`, `.env.local` or `.env.production` files.
- Keep SPA fallback routing and `/api` or absolute `VITE_API_BASE_URL` behavior working.
- Run `npm run typecheck`, `npm run lint`, `npm test` and `npm run build` after changes.
- Run the relevant Playwright suite for changed user journeys and report skipped external-provider checks clearly.
