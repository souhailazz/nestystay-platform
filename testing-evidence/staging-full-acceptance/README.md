# NestyStay staging acceptance evidence

This folder contains historical, read-only black-box evidence captured from `https://staging.nestystay.net/` on 2026-09-18, before the remediation pass.

The evidence capture itself changed no source code, database data, server configuration, provider dashboard, environment variable, or Git state. Later source remediation is documented in `docs/testing/STAGING-FULL-ACCEPTANCE.md`.

The complete matrix and bug reports are in:

`docs/testing/STAGING-FULL-ACCEPTANCE.md`

Evidence includes:

- Desktop public homepage, Explore, property detail, login, and Gate screenshots.
- Tablet Explore screenshot at 1024×768.
- Mobile homepage, Explore, and property detail screenshots captured with an iPhone 13 viewport.
- Missing cookie-policy and directory-route screenshots.
- Invalid QR 404 screenshot.
- Booking quote screenshot showing the stale Alibaba eKYC wording.

Role-protected M1–M5 workflows are explicitly marked BLOCKED where no valid staging role account or safe external-provider test setup was available. Blocked is not counted as a pass.
