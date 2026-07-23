# M1/M2 Test Evidence

Audit date: 2026-07-23

Branch: `audit/m1-m2-remediation`

Baseline commit audited: `5465ba77308c17245cfa06b1a29f231104e037b9`

Remediation commit: `214f623ef6dd71e3564f9744d7a3cf3d05f886ef`

CI evidence: Not run in this session. All results below are local evidence from `C:\Users\Administrator\Desktop\nestystayPLATFORM`.

## Required Command Output Summary

| Command | Result |
| --- | --- |
| `dotnet build backend\NestyStay.sln` | Exit 0 after earlier stale local API lock was stopped. `Build succeeded. 0 Warning(s) 0 Error(s)`. |
| `dotnet test backend\NestyStay.sln` | Exit 0. Domain 5 passed, Application 16 passed, Infrastructure 5 passed, API 17 passed. |
| `dotnet build backend\NestyStay.sln -c Release` | Exit 0. `Build succeeded. 0 Warning(s) 0 Error(s)`. |
| `dotnet test backend\NestyStay.sln -c Release` | Exit 0. Domain 5 passed, Application 16 passed, Infrastructure 5 passed, API 17 passed. |
| `dotnet list backend\NestyStay.sln package --vulnerable --include-transitive` | Exit 0. No vulnerable packages reported for all backend projects. |
| `cd frontend; npm audit` | Exit 0. `found 0 vulnerabilities`. |
| `cd frontend; npm run typecheck` | Exit 0. `tsc -b`. |
| `cd frontend; npm run lint` | Exit 0. `eslint "src/**/*.{ts,tsx}"`. |
| `cd frontend; npm test` | Exit 0. Vitest 1 file / 3 tests passed. |
| `cd frontend; npm run build` | Exit 0. `tsc -b && vite build`; Vite transformed 2016 modules and built successfully. |
| `git diff --check` | Exit 0; only CRLF normalization warnings were emitted. |
| `git grep -n -E "dev-admin-token|local-phase1-token|local-google-token"` | Exit 0 with two intentional negative assertions in backend tests; no runtime app-code acceptance path found. |
| `rg -n "dev-admin-token|local-phase1-token|local-google-token|sk_live_|pk_live_|BEGIN (RSA|OPENSSH|PRIVATE) KEY" .` | Exit 0 with historical report text and explicit rejection tests only for legacy tokens; no live payment/private-key secret patterns found. |

## Automated Tests Added Or Updated

| Test file | Evidence |
| --- | --- |
| `backend/tests/NestyStay.Application.Tests/PhaseOneWorkflowTests.cs` | Verifies issued session tokens no longer use `local-phase1-token-*` or `local-google-token-*`. |
| `backend/tests/NestyStay.Api.Tests/SignedAccessTokenSecurityTests.cs` | Verifies missing tokens, legacy predictable tokens, expired tokens, modified payloads, bad signatures, and signed non-admin tokens are rejected; verifies configured admin token is accepted; verifies Production requires a strong session token secret and Development can run without it. |
| `backend/tests/NestyStay.Api.Tests/HealthEndpointTests.cs` | Verifies unauthenticated booking list rejection, client-submitted `guestUserId` override, cross-user booking-detail rejection, traveler direct verification-result rejection, traveler capture forbidden, and host capture after approval. |
| `backend/tests/NestyStay.Api.Tests/SpecCompletionEndpointTests.cs` | Uses signed test bearer tokens instead of local GUID-derived tokens for traveler, host, messaging, directory, and admin-path coverage. |
| `frontend/src/lib/api.test.ts` | Verifies API client bearer-token headers, no Authorization header when token omitted, JSON request bodies, and `ApiError` propagation with status codes. |

## Missing Required Test Coverage

Playwright E2E, keyboard-navigation, dialog focus trapping, route-authorization tests, visual screenshot checks, file-upload validation, Stripe Elements, webhook signature/replay, admin domain-operation tests, and per-screen loading/empty/error/mobile tests are still missing.

## Test Verdict

PARTIAL. Backend and basic frontend local checks now pass, but the full DOCX acceptance suite still does not exist. Do not claim M1/M2 complete.
