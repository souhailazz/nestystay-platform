# NestyStay M1–M5 client visual evidence

This is a non-technical viewing guide for the current local NestyStay implementation. Open the MP4 files in each milestone folder for slow, readable browser demonstrations. PNGs are the before/result stills and responsive references.

## How to review

1. Start with [the full-system demo](Full-System/videos/NestyStay-M1-M5-Full-System-Demo.mp4).
2. Review the milestone folders in order: M1 Core, M2 Badges, M3 Wellness, M4 Directories + QR, and M5 Property Manager.
3. Use [EVIDENCE-MANIFEST.md](EVIDENCE-MANIFEST.md) to jump to any specific functionality.

## What the videos prove

Each capture is a clean Chromium browser view at 1920×1080. The browser pauses after navigation and visible state changes so a client can read the screen. The recorded journeys use real frontend routes and the running local API/database fixture.

The recordings prove application behavior, not external-provider certification. Stripe checkout is shown at the application/test boundary; real Stripe provider validation remains pending. eKYC is shown at the application checkpoint; real Alibaba provider validation remains pending.

## Evidence boundary

Client visuals contain no terminal, test runner, API JSON, source code, or secrets. Technical reports, Playwright traces, API results, PostgreSQL evidence, and security evidence remain separate in testing-evidence/milestones-1-5/ and the repository testing documentation.

## Package contents

- [Demo data](DEMO-DATA.md) — synthetic names and records used.
- [Evidence manifest](EVIDENCE-MANIFEST.md) — every required M1–M5 functionality folder.
- [Video validation report](VIDEO-VALIDATION-REPORT.md) — codec, resolution, duration, and playback checks.
- [Secret review](SECRET-REVIEW.md) — client-media and documentation review.

Functional counts: M1 8, M2 8, M3 11, M4 14, M5 20.