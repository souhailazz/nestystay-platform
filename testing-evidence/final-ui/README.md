# NestyStay final UI evidence

Captured 2026-09-16 from the real local NestyStay frontend and ASP.NET API with PostgreSQL running locally. These are browser screenshots, not mockups or generated image sequences.

## Capture contract

- Desktop: 1440 × 1000
- Mobile: 390 × 844
- Tablet: 768 × 1024
- Browser: Playwright-controlled Chromium, device scale factor 1
- Capture result: 81 screenshots, 27 canonical production-critical surfaces × 3 viewports
- Every capture completed with `result: PASS` and an empty `pageErrors` array
- The screenshot script is [final-ui-capture.mjs](../../frontend/e2e/final-ui-capture.mjs)

The script creates isolated local sessions for Guest, Host, Owner, Property Manager, Officer, Service Provider, and Admin roles. It creates only local synthetic records (`Palm Gardens Demo Stay`, `Palm Gardens Community`, and named demo users); it does not use production data or secrets. Browser authentication is cookie-backed through the local test API.

## Coverage

| Milestone | Captured surfaces | Viewports |
|---|---:|---|
| M1 Core Booking | 7 | desktop / mobile / tablet |
| M2 Badges | 2 | desktop / mobile / tablet |
| M3 Wellness | 3 | desktop / mobile / tablet |
| M4 Directories / Trust / QR | 4 | desktop / mobile / tablet |
| M5 Property Manager | 11 scene labels covering 9 unique routes | desktop / mobile / tablet |

The existing [client-demo evidence](../client-demo/README.md) remains in the repository and contains the broader M1–M5 interaction screenshots and videos, including the previously captured 1920 × 1080 client-demo recordings and additional responsive evidence. This directory is the final exact-viewport UI certification set for the critical product surfaces.

## Provider and release boundary

The local capture uses the repository's deterministic development/test adapters and local PostgreSQL. It does not represent a live Stripe Identity session, live Brevo delivery, live object storage, or a production deployment. Those require the client's staging secrets and staging smoke tests. Human screen-reader certification is also outside an automated screenshot run.

## Final source revisions

- Frontend: `939db33a0f1c6b795463396b4771c9f705af95d2`
- Backend: `d581f3452135760d4755fb60c3bae6e2448b4479`
- Root documentation/evidence revision: recorded in the final root commit and the protected root consolidation PR
