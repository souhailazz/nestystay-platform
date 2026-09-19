# M1–M5 local GO record

Decision: **LOCAL DEVELOPMENT GO**

This is a local engineering GO only. It authorizes continued staging preparation; it is not a production release approval.

Acceptance evidence:

- Repository-owned PostgreSQL 18 on port 55432: healthy.
- Migrations current and deterministic seed successful.
- API liveness/readiness, properties, public content, experiences, directories, frontend root and Explore: HTTP 200.
- Backend: 180 passed, 0 failed, 0 skipped.
- Frontend: 48 unit tests passed; typecheck and production build passed; lint 0 errors / 92 warnings.
- Playwright: 182 passed, 0 failed, 9 intentional skips across desktop/tablet/mobile Chromium plus Firefox/WebKit smoke.

Release hold points:

1. Merge candidate branches through the protected GitHub `main` PR workflow.
2. Run the same suite from the resulting `main` SHAs.
3. Configure and verify real Stripe payments, Stripe Identity, Connect payouts and required webhook events/signatures.
4. Configure Brevo, storage, map/geocoder and SMS/Web Push where launch scope requires them.
5. Complete staging role journeys and human accessibility/device certification.
