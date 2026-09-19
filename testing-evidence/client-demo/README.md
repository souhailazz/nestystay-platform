# NestyStay client-demo videos

These are client-safe walkthroughs of the real NestyStay frontend running in Chromium against the real ASP.NET API and an isolated PostgreSQL demo database. The recordings use genuine browser navigation, typing, clicking, scrolling, modal/form interaction, and backend-driven status changes.

## Deliverables

### 01 — Milestone 1: Core Booking System

- [MP4 — `01_Milestone_1_Core_Booking_System.mp4`](01_Milestone_1_Core_Booking_System.mp4)
- [Original WebM — `01_Milestone_1_Core_Booking_System.webm`](01_Milestone_1_Core_Booking_System.webm)
- Duration: `03:02.00`
- Resolution: `1920×1080`
- Final MP4 size: `19,187,228` bytes
- WebM size: `13,319,564` bytes

Demonstrates:

- public NestyStay landing page and real sign-in
- real 2FA challenge and code entry
- Explore discovery, host-badge filtering, property detail, gallery/content, and booking action
- booking popup with dates, guest count, quote, fees, and total
- booking creation into `PENDING`
- NestyStay identity-verification flow with the deterministic eKYC test result driving `APPROVED`
- checkout/payment summary, local payment-test authorization, and captured/confirmed success
- a second real booking resolved through the deterministic rejection path, showing `REJECTED` and the cancelled-booking view
- final return to the confirmed booking receipt/trip state

### 02 — Milestone 2: Badge System

- [MP4 — `02_Milestone_2_Badge_System.mp4`](02_Milestone_2_Badge_System.mp4)
- [Original WebM — `02_Milestone_2_Badge_System.webm`](02_Milestone_2_Badge_System.webm)
- Duration: `02:01.48`
- Resolution: `1920×1080`
- Final MP4 size: `14,766,517` bytes
- WebM size: `12,524,106` bytes

Demonstrates:

- real public host profiles for `FREE`, `VERIFIED`, `TRUSTED`, and `WELLNESS`
- badge visibility and badge-linked profile/listing features
- Explore filters for all four badge levels and the corresponding real listings
- admin sign-in and the real Badge management surface
- searching/filtering badge assignments, viewing the catalog/history area, and reviewing lifecycle information without changing non-demo data
- final navigation through the four real seeded profiles so the progression is clear without a fabricated comparison graphic

## Demo data and accounts

The run used dedicated synthetic records in the isolated database `nestystay_demo_videos_20260912`.

- Guest used in the final Milestone 1 recording: `client-demo-guest-final5-20260912@nestystay.local` (2FA enabled)
- Admin: `client-demo-admin@nestystay.local`
- FREE host: `demo.free.host@nestystay.local` — Maya Free
- VERIFIED host: `demo.verified.host@nestystay.local` — Naomi Verified
- TRUSTED host: `demo.trusted.host@nestystay.local` — Andre Trusted
- WELLNESS host: `demo.wellness.host@nestystay.local` — Priya Wellness

Passwords, OTP values, tokens, and connection details are intentionally not recorded in this document. The hosts and admin are dedicated local demo accounts. The seed preparation is implemented by `frontend/e2e/client-demo/demo-video-helpers.mjs` and calls the development seed endpoint before creating or updating only these demo records.

## Provider modes and honesty boundary

- Stripe: deterministic local payment adapter/test mode. The application visibly labels the checkout as `Local payment test mode`; no live Stripe account or production card data was used.
- Alibaba/eKYC: deterministic local development adapter for the configured Alibaba Cloud eKYC integration. The real NestyStay identity page opens its configured provider flow, and the recording remains on NestyStay; no Alibaba webpage is fabricated or presented as live. The approval and rejection states come from real booking API state transitions using the returned deterministic transaction reference.
- Email: local/file outbox mode. No external email delivery is claimed.
- Storage: local persistent object storage.
- SMS: local development sender.
- Backend/database: real ASP.NET Core backend with real PostgreSQL persistence in the isolated demo database above.

## Recording setup

- Chromium desktop, clean recording context, `1920×1080`, 100% zoom, device scale factor 1, 25 fps
- Final MP4: H.264, `yuv420p`, video-only; original VP8 WebM retained
- No audio track; silence is intentional
- A temporary Playwright-injected cursor (`window.__NESTYSTAY_DEMO_CURSOR__`) follows real Playwright mouse events, includes a subtle shadow/outline and click pulse, and is not part of the production application bundle
- Dedicated scripts: `frontend/e2e/client-demo/milestone1-video.spec.mjs`, `milestone2-video.spec.mjs`, and `demo-video-helpers.mjs`

## Exact source revisions

- Root/frontend application SHA used for the recording: `687cf1986c8dc9a791141a02b85507daf040ad9c`
- Backend application SHA: `8381b2eeec5fc6b04dafffcf2eb99bd71c8ecb55` (backend source was unchanged during the frontend hardening commits)
- The dedicated recording scripts and media are evidence-run working-tree additions; they do not alter the production bundle.

## Validation

- Both MP4 files opened and fully decoded successfully with ffmpeg.
- Representative opening, mid-flow, status, admin, and ending frames were inspected at native `1920×1080`; the injected cursor was visible.
- The recordings contain real Chromium interaction and application transitions, not screenshots, slideshows, fake HTML states, or DOM result replacement.
- No terminals, devtools, test runner, secrets, tokens, private identity documents, or unrelated browser tabs are visible.
- Aspect ratio, codec, pixel format, frame rate, and durations were verified with ffprobe.
- No deployment or merge to `main` was performed.
