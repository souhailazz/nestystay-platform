# M1/M2 Test Evidence

Audit date: 2026-07-24

Branch: `audit/m1-m2-remediation`

Baseline commit audited: `5465ba77308c17245cfa06b1a29f231104e037b9`

Current evidence scope: incremental local verification for the Playwright/CI evidence slice. The exact commit SHA is the branch head containing this report update.

CI evidence: GitHub Actions workflow `.github/workflows/m1-m2-acceptance.yml` is active and has passed on this branch for backend Debug/Release build/test, package scans, migrations, frontend audit/typecheck/lint/test/build, Playwright, and artifact upload. Local command results below are from `C:\Users\Administrator\Desktop\nestystayPLATFORM`.

## 2026-07-24 Command Output Summary

| Command | Result |
| --- | --- |
| `dotnet build backend\src\NestyStay.Api\NestyStay.Api.csproj --configuration Debug` | Exit 0. `Build succeeded. 0 Warning(s) 0 Error(s)`. |
| `dotnet test backend\tests\NestyStay.Api.Tests\NestyStay.Api.Tests.csproj --configuration Debug --no-build` | Exit 0. API tests: 26 passed. |
| `cd frontend; npm run typecheck` | Exit 0. `tsc -b`. |
| `cd frontend; npm run lint` | Exit 0. `eslint "src/**/*.{ts,tsx}"`. |
| `cd frontend; npm test` | Exit 0. Vitest `src` suite: 1 file, 13 tests passed. |
| `cd frontend; npm run build` | Exit 0. `tsc -b && vite build`; Vite transformed 2065 modules and built successfully. |
| `cd frontend; npm run test:e2e` | Exit 0. Playwright: 9 tests passed across desktop, tablet, and mobile Chromium projects. |
| `git diff --check` | Exit 0; only CRLF normalization warnings were emitted. |
| `git grep -n -E "sk_live_|pk_live_|BEGIN (RSA|OPENSSH|PRIVATE) KEY" -- .` | Exit 0 with historical report text only; no live payment/private-key secret patterns found. |
| `git grep -n -E "dev-admin-token|local-phase1-token|local-google-token" -- .` | Exit 0 with historical report text, development config hashes, and explicit negative tests only; no runtime app-code acceptance path found. |

## Automated Tests Added Or Updated

| Test file | Evidence |
| --- | --- |
| `frontend/e2e/m1-m2-acceptance-smoke.spec.ts` | Seeds local spec data, creates signed traveler/host sessions via the real auth flow, captures representative public/auth/traveler/host/profile/directory/message/admin/error screens, verifies profile photo upload, and fails on page console errors. |
| `frontend/playwright.config.ts` | Runs desktop, tablet, and mobile Chromium projects, starts the API and Vite dev server, writes screenshots under `artifacts/m1-m2-visual`, and keeps failure traces under `artifacts/playwright-results`. |
| `.github/workflows/m1-m2-acceptance.yml` | Adds pull-request/push CI for backend Debug/Release build/test, package scans, migrations, frontend audit/typecheck/lint/test/build, and Playwright artifact upload. |
| `backend/src/NestyStay.Infrastructure/Persistence/Milestones/EfSpecCompletionStore.cs` | Hardens spec/traveler seed writes against concurrent duplicate-key races observed during parallel Playwright route loads. |
| `frontend/src/pages/CompletionPages.tsx` | Avoids unauthenticated admin operations fetches until an admin token is entered, preventing protected-route 401 console noise in visual smoke runs. |
| `frontend/package.json` | Keeps Vitest scoped to `src` so Playwright specs are executed only by Playwright. |

## Evidence Delta

The repository now has representative Playwright coverage and visual artifacts for 19 screen/view states across 9 families: PUB, AUTH, TRAV, HOST, HPRO, DIR, MSG, ADM, and ERR. This is real progress, not full acceptance.

## Missing Required Test Coverage

The complete DOCX acceptance suite is still missing. Remaining gaps include screen-by-screen Playwright coverage for every M1/M2 row, keyboard-navigation checks, dialog focus trapping, route-authorization tests for every protected view, visual review of every desktop/tablet/mobile screenshot, full payment/Stripe Elements flows, complete webhook signature/replay coverage, domain-specific admin operation tests, and per-screen loading/empty/error/mobile interaction tests.

## Test Verdict

PARTIAL. Local backend, frontend, and representative Playwright checks pass, and CI workflow coverage has been added. M1/M2 still must not be marked complete until the full DOCX screen matrix and interaction-state evidence pass.
