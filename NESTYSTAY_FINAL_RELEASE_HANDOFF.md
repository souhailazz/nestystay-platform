# NestyStay Final Release Handoff

**Evidence date:** 20–21 October 2026  
**Scope:** frontend URL architecture, backend architecture, integrated local release candidate, tests, PR state, and release blockers  
**Verdict:** **PARTIALLY VERIFIED — BLOCKERS REMAIN**

This is the single release handoff document for the current engineering pass. It records what was verified locally, what is published to GitHub, and what remains controlled by Terrence or by external staging/provider configuration.

## 1. Executive summary

NestyStay is a React/Vite single-page frontend backed by an ASP.NET Core API, Entity Framework Core, PostgreSQL, signed cookie/bearer authentication, provider adapters, and hosted background services.

The integrated local candidates are functional for the tested scope:

- Frontend candidate: `codex/final-frontend-e2e-alignment`, commit `fb6edc46024668f719cc28c596703ed2061f656f`.
- Backend candidate: `codex/final-backend-integration-fixes`, commit `090d17d260914d3367b53972ace842322d943bfe`.
- Both branches are pushed to their respective GitHub repositories and have clean working trees.
- The existing feature PR chain remains the review/merge mechanism; no duplicate final integration PR was opened because the candidate branches contain the accumulated integrated history and would produce oversized duplicate diffs against `main`.
- No protected branch was merged, no deployment was performed, and no shared database migration was applied.

The local candidate passed the deterministic application test gates, including the full local Playwright matrix. It is **not** staging-certified, and Terrence still controls merging, deployment, provider configuration, and staging acceptance.

The existing final corrections were subsequently fast-forwarded into focused PR branches. The published follow-up set is accounted for below: nine focused correction PR heads are listed here, and Backend #72 is recorded separately because its changes are already an ancestor of the integrated backend candidate.

- Backend #67 at `0cab8aea565988d12cac6617062cb40762d2ea14` — [PR](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/pull/67), [CI run](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/actions/runs/39499205411).
- Backend #69 at `3b59188b54ed4b83a4e9f4c0717281a4983c443f` — [Admin payment-hold release](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/pull/69); required build/test passed and deployment jobs were skipped by policy.
- Backend #51 at `b48ce147d18cb26a72682e650157372118d75cc5` — [PR](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/pull/51), [CI run](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/actions/runs/39499203172).
- Frontend #91 at `1ac952279e7d41f3c0633f6471a6621ee6b872ff` — [PR](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/91), [CI run](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/actions/runs/39499203518).
- Frontend #89 at `21d7db141eaea4fc4f2195a0fc99299482ded747` — [PR](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/89), [CI run](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/actions/runs/39499204095).
- Frontend #74 was updated to `5946858a28c9e40c1ac06f9d00bc2513cf22bf14` with the legacy InsuraGuest entry-point hardening described below. Required CI passed in run [39501544481](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/actions/runs/39501544481); the deployment job was skipped by policy. GitHub reports the PR content mergeable, with branch protection still blocking merge.
- Frontend #94 — [Admin payment-hold client contract](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/94) — head `e5c8851e0cd0d5ffad4568bcfc3dda862de16ed3`, based on frontend `main`. It sends the required `BookingDecisionRequest` body to the Backend #69 `void-payment` endpoint and adds a client regression test. Local focused tests, typecheck, production build, changed-file lint, and diff validation pass; required CI run [39502716486](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/actions/runs/39502716486) passed and its deploy job was skipped by policy. It must merge after the backend endpoint is available.
- Backend #73 — [SMS preferred-role fallback](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/pull/73) — head `9fce4e7138afad3c6b7aa03539699fb918bb1744`, based on Backend #57's `codex/persist-preferred-role` branch. Required CI run [39504518558](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/actions/runs/39504518558) passed, including build/test and migration-script artifact generation; deploy jobs were skipped. It must merge after #57.
- Backend #74 — [anonymous profile probe credential handling](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/pull/74) — head `f1cd71380cadf18d37fd6190329e9cbbed8d155a`, based on Backend `main`. A no-credential profile probe returns 204, while invalid/revoked bearer or session-cookie credentials remain 401. Remote CI run [39519661475](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/actions/runs/39519661475) passed: Application 98, Domain 6, Infrastructure 32 plus 1 skipped, API 180 plus 1 skipped; build, tests, and idempotent migration-script generation passed. Deployment jobs were skipped. Content is mergeable and branch policy remains Terrence-controlled.

Backend #72 — [runtime cancellation and calendar-feed error handling](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/pull/72) — head `0b12ed998bacc0ce1bd78439b7a8a26f098fbfc5` — is already an ancestor of the integrated backend candidate and the final backend freeze. It adds client-abort debug handling, retryable calendar DNS/socket failures, and focused regression tests. Its required CI is green and deployment checks are skipped. Do not cherry-pick or reimplement it a second time; preserve one copy when the dependency chain is merged.

Each required build/test job succeeded; deployment jobs were skipped by policy. No new duplicate PR was opened.

## 2. Source revisions and worktrees

### Release candidates

| Area | Worktree | Branch | Exact commit | Working tree |
|---|---|---|---|---|
| Frontend | `C:\Users\Administrator\AppData\Local\Temp\nestystay-frontend-final-candidate-20261020` | `codex/final-frontend-e2e-alignment` | `fb6edc46024668f719cc28c596703ed2061f656f` | Clean |
| Backend | `C:\Users\Administrator\AppData\Local\Temp\nestystay-backend-final-candidate-20261020` | `codex/final-backend-integration-fixes` | `090d17d260914d3367b53972ace842322d943bfe` | Clean |

### Frozen follow-up integration candidate

For the final focused corrections, a disposable clean frontend freeze was created from the frontend candidate and applied in sequence:

1. Frontend candidate `fb6edc46024668f719cc28c596703ed2061f656f`.
2. Frontend #74 legacy InsuraGuest hardening.
3. Frontend #94 payment-hold request-body correction.

The resulting local-only freeze is:

| Area | Worktree | Branch | Exact commit | Remote state |
|---|---|---|---|---|
| Frontend freeze | `C:\Users\Administrator\AppData\Local\Temp\nestystay-frontend-freeze-20261020` | `codex/final-frontend-freeze-20261020` | `2844cea0bb06b1633f11a55f5b6f58ddb391c1e3` | Disposable local verification branch; not pushed |
| Backend freeze | `C:\Users\Administrator\AppData\Local\Temp\nestystay-backend-freeze-20261020` | `codex/final-backend-freeze-20261020` | `07bccd28a916f7e20617d72396ec896a737c9787` | Disposable local verification branch; not pushed |

Frontend #84 did not add a new effective change in this freeze because its mobile-gallery assertion was already present in the integrated candidate; Git reported the cherry-pick as empty and it was not duplicated.

### Preserved worktrees

The following existing directories were not reset, overwritten, or deleted:

- `C:\Users\Administrator\Desktop\nestystayPLATFORM\frontend`
- `C:\Users\Administrator\Desktop\nestystayPLATFORM\backend`
- `C:\Users\Administrator\Desktop\nstestay-frontend-multirole-integrated`
- `C:\Users\Administrator\Desktop\nstestay-backend-multirole-integrated`

The nested umbrella repositories contain independent/uncommitted work and were not used as release evidence.

## 3. URL architecture audit

### Public and authentication routes

The frontend route manifest is centralized in `src/app/routeManifest.ts` and consumed by `src/App.tsx`, `src/app/routeManifest.test.ts`, post-auth routing, and the dedicated Admin-host gate.

| Surface | Representative routes | Boundary |
|---|---|---|
| Public discovery | `/`, `/explore`, `/explore/:slug`, `/coming-soon` | Public; listing visibility and backend authorization still apply |
| Public auth | `/login`, `/register`, password reset routes | Public shell; session/profile hydration determines destination |
| Guest | `/guest-dashboard`, `/bookings`, `/messages`, `/profile` | Guest membership and backend authorization |
| Host / owner | `/host-dashboard`, `/host/properties`, `/host/insurance` | Host/owner membership and property ownership policies |
| Property Manager | `/pm/dashboard`, `/pm/calendar`, `/pm/team`, `/pm/properties`, `/pm/professional` | PM membership, property assignment, staff scope |
| Service Provider | Provider workspace routes | Service-provider membership and server-side policy |
| Local Business | Business workspace routes | Local-business membership and server-side policy |
| Wellness Officer | `/wellness`, officer workflow routes | Officer membership/onboarding; identity linkage is privacy-restricted |
| Gate Guard | `/gate/guard`, `/gate/qr` | Gate Guard invitation, property assignment, QR/property authorization |
| Safety Center | `/safety`, `/pm/professional?tab=incidents` | Authenticated role-specific access and incident authorization |
| Super Admin | Dedicated hostname plus `/admin/sign-in`, `/admin` | Dedicated Admin hostname; public-host Admin routes are blocked/404-gated |

### URL security findings

1. Frontend route protection is UX only; backend policies remain authoritative.
2. The public host must not be treated as an Admin host. The integrated frontend applies `VITE_ADMIN_HOSTNAME`/Admin-host routing rather than exposing Admin pages through normal user navigation.
3. Dynamic route parameters are constrained by the route manifest and validated before the page attempts to fetch data.
4. Direct navigation to another role's URL is expected to render a role-aware denial state and must also receive backend 401/403 when an API request is attempted.
5. Auth hydration is asynchronous. The frontend waits for profile/session resolution before choosing the post-login destination, preventing the Guest route from winning during a refresh race.
6. Role switching is session-backed and server-authoritative. A stale or revoked membership cannot remain usable merely because browser storage contains an old role.
7. Super Admin is intentionally separate from normal role workspaces. Admin hostname reachability is not equivalent to Admin authorization.

### URL architecture risks remaining

- The dedicated Admin hostname, CORS allow-list, cookie policy, DNS, TLS, and deployed frontend variable must be verified together in staging.
- The local candidate cannot prove that the deployed reverse proxy preserves the expected host and cookie boundaries.
- Public SPA shell responses may be HTTP 200 even when the client route renders a branded 404; infrastructure monitoring should use route-aware browser/API checks rather than status code alone.

## 4. Backend architecture audit

### Layers

| Layer | Evidence | Responsibility |
|---|---|---|
| API | `src/NestyStay.Api/Controllers`, middleware, auth handlers | HTTP contracts, authentication, policy checks, error mapping, health/diagnostics |
| Application/domain | `src/NestyStay.Application`, `src/NestyStay.Domain` | Use cases, invariants, role and booking rules |
| Persistence | `src/NestyStay.Infrastructure/Persistence` | EF Core, PostgreSQL, milestone stores, tenant/property scoping |
| Providers | `src/NestyStay.Infrastructure/Storage`, notification/payment adapters | MinIO/S3-compatible storage, email, Stripe and external integrations |
| Workers | `src/NestyStay.Infrastructure/Services` | Calendar sync, exports, expiry, subscription maintenance, notifications and retries |
| Tests | `tests/NestyStay.*.Tests` | Unit, application, persistence, API, authorization and integration coverage |

### Backend security boundaries

- Authentication supports cookie and bearer modes; CSRF is required for state-changing cookie-session requests.
- Role and membership authorization is enforced server-side; frontend route guards are not used as a security boundary.
- Property Manager and staff operations are scoped to assigned properties/tenants.
- Gate Guard QR validation is property-scoped and rejects invalid, expired, revoked, and cross-property use cases.
- Wellness officer identity linkage is not exposed to roles that should only see wellness availability or workflow state.
- Admin diagnostics and development schema/job diagnostics have explicit authorization/configuration gates in the candidate chain.
- Webhook authentication uses the configured signature/shared-secret boundary outside Development and Testing; production/staging secrets remain external configuration.
- Database query logging was reduced to the approved Warning default in the backend changeset; production deployment still needs verification of the effective environment configuration.

### Persistence and migration architecture

- EF Core migrations are the authoritative schema history.
- The disposable local database `nestystay_final_20261020` was created from scratch and migrated through `20261019224554_AddPreferredRole`.
- No staging or production migration was executed.
- The preferred-role and membership migrations add the schema needed by multi-role sessions; Terrence must apply them in the approved deployment sequence.
- Migration generation and application remain separate operations. The release process must preserve the idempotent-script review gate and backup requirement.

### Provider and worker architecture

- Local verification used file email capture and local storage to avoid uncontrolled external delivery.
- The repository contains MinIO/S3-compatible storage integration; the browser-level MinIO contract and native MinIO round-trip passed against the disposable local MinIO container.
- Background services cover calendar synchronization, document export/expiry, subscription maintenance, notifications/retries, and maintenance workflows.
- Worker startup and runtime diagnostics are guarded; a deployed worker process and schedule still require staging verification.
- Stripe/payment logic was not changed in this final pass. Existing authorization/void/refund tests ran locally; no live transaction was executed.

## 5. Final changes made

### Backend commit `090d17d`

1. Fixed PostgreSQL admin account search by using `EF.Functions.JsonContains` for Npgsql while preserving the InMemory test path in `EfPropertyManagerP0Store.cs`.
2. Made Guest role addition immediately active for an already active account in `EfPhaseOneStore.cs`; approval/verification policy remains for Host, Owner, PM, Officer, Service Provider, and Local Business.
3. Corrected `AddAccountRoleResponse.Added` when the new role is active in `AuthController.cs`.
4. Added regression coverage for immediate Guest workspace activation in `MultiRoleAccountTests.cs`.

The integrated candidate ancestry also contains the previously published runtime/logging correction from Backend #72 (`0b12ed9`), the deterministic worker corrections from #70/#71, the calendar authorization correction from #68, and the payment endpoint from #69. The final freeze retains these changes without duplication.

### Frontend commit `fb6edc4`

1. Updated the team-invitation browser flow to use the approved searchable account selector instead of a stale raw user-ID field.
2. Updated the PM browser assertion to the approved customer-facing workspace wording.

No pricing, commission, date-hold, provider, deployment workflow, protected branch, or shared migration behavior was changed by these final commits.

### InsuraGuest referral-only hardening on Frontend #74

The existing referral-only route already rendered `InsuraGuestUnavailable`, and the backend lifecycle endpoints already return HTTP 410 without persisting a policy. A stale compatibility component still contained the retired plan, premium, renewal, cancellation, and claim controls. Frontend #74 now replaces that component with a non-transactional wrapper and adds regression coverage so an older route import cannot re-expose those controls.

- Commit: `5946858a28c9e40c1ac06f9d00bc2513cf22bf14`.
- Files: `src/features/host/HostInsurance.tsx`, `src/features/host/HostInsurance.test.tsx`, and the stale lifecycle assertion in `src/coverage/host-surfaces.test.tsx`.
- No backend, provider, pricing, payment, migration, or deployment changes.
- The existing backend referral-only tests and browser contract remain unchanged and continue to cover HTTP 410/no-persistence behavior.
- CI: `Typecheck, test and build` **success**; `Build and deploy static bundle to nginx host` **skipped** by policy.

### Admin payment-hold client contract on Frontend #94

The frontend previously sent an empty POST for `POST /api/bookings/{id}/void-payment`, while the backend contract requires a `BookingDecisionRequest`. Frontend #94 sends the existing neutral release reason and tests the URL, bearer token, content type, and body. It changes only `src/lib/api.ts` and `src/lib/api.test.ts`; no pricing, payment-provider, capture, migration, or deployment behavior changes are included.

## 6. Test evidence

### Frontend deterministic tests

| Check | Result |
|---|---|
| Vitest | **288 passed, 0 failed** across 62 files |
| Typecheck | **PASS** |
| Production build | **PASS**, Vite 7.3.6; 2,169 modules transformed |
| ESLint | **PASS**, 0 errors, 33 warnings |
| Relevant warnings | Existing test `any`/unused-import warnings and one unused `ManagedInsurance` import; no lint errors |

### Backend deterministic tests

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Domain | 6 | 0 | 0 |
| Application | 99 | 0 | 0 |
| Infrastructure | 37 | 0 | 0 |
| API | 211 | 0 | 0 |
| **Total** | **353** | **0** | **0** |

The native MinIO round-trip passed 1/1 against the disposable local MinIO container, and the two-instance PostgreSQL concurrency fixture passed 1/1 against a fresh disposable database. The backend Release build passed with 0 warnings and 0 errors.

### Playwright

Final local run against the final frontend/backend services:

- **266 scheduled**
- **255 passed**
- **0 failed**
- **11 explicitly skipped**
- **0 not run**
- Runtime: approximately 44.3 minutes
- Projects included desktop Chromium, desktop Firefox, desktop WebKit, tablet Chromium, and mobile Chromium.
- Report: `C:\Users\Administrator\Desktop\nestystayPLATFORM\artifacts\final-playwright-report-20261020-final2`
- Results: `C:\Users\Administrator\Desktop\nestystayPLATFORM\artifacts\final-playwright-results-20261020-final2`

The 11 skips were explicit environment/provider/project-matrix gates, not silent skips. The authoritative final line-reporter summary is `artifacts/final-playwright-results-20261020-final2/FINAL_RUN_SUMMARY.md`; the retained HTML report was generated by an earlier run and must not be used for the final totals. The test run covered login, role routing, multi-role restart/tabs, booking, calendar, admin/security, Safety Center, Gate Guard, InsuraGuest referral-only behavior, privacy, accessibility, responsive behavior, performance, PM workflows, SEO, Stripe test mode, and usability.

### Focused verification

- Repaired root-failure browser files: **18 passed, 0 failed** across desktop/tablet/mobile.
- Multi-role and PM endpoint focused backend tests: **24 passed, 0 failed, 0 skipped**.
- InsuraGuest hardening branch: **264 Vitest tests passed, 0 failed**; focused InsuraGuest/host-surface tests **9 passed, 0 failed**; typecheck, production build, and lint completed with no errors. The branch has existing non-blocking lint warnings in unrelated coverage fixtures.
- Local API liveness: HTTP 200.
- Local API readiness: HTTP 200, database ready, storage configured.
- Local API version endpoint: HTTP 200.
- Final focused freeze candidate: **27 Vitest tests passed, 0 failed** across the InsuraGuest, host-surface, and API-client regressions; typecheck and production build passed; changed-file lint reported 0 errors and only five existing fixture warnings.
- Real mobile-milestone workflow against the freeze frontend and disposable local API: **3 passed, 0 failed** across desktop Chromium, tablet Chromium, and mobile Chromium. The referral-only InsuraGuest mutation returned HTTP 410 and the gallery completed on all three viewports.

## 7. Defects found and corrected during integration

### Confirmed application defects

1. PostgreSQL JSONB account search translated a string `Contains` expression into an invalid `jsonb NOT LIKE` query. Fixed with native JSON containment for Npgsql and retained the InMemory-compatible branch.
2. Guest role addition returned a pending membership for an already active account, conflicting with the approved acceptance flow requiring an immediately usable Guest workspace. Fixed without weakening approval requirements for controlled roles.

### Test/fixture defects corrected

1. Team invitation E2E still filled a removed raw user-ID field; it now uses the searchable account selector.
2. PM E2E asserted removed technical wording; it now asserts the approved customer-facing workspace label.

### Intermediate failures not counted as final failures

Earlier local Playwright failures were traced to mismatched local configuration rather than silently ignored:

- Email outbox scan used a different directory than the test fixture.
- Booking capture required the explicit local-only E2E admin token configuration.
- The JSONB search defect was fixed before the final run.
- Guest role activation was fixed before the final run.

## 8. GitHub PR and merge state

The existing PR chain contains the feature work and focused fixes. CI shown on the existing PRs was green for their recorded checks, but many are `BLOCKED` because their base branches are intentionally stacked or because `main` has not yet received their prerequisites.

### Main integration PRs

- Backend #45: [multi-role account workspaces](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/pull/45) — blocked against `main` until its dependency chain is merged.
- Frontend #73: [multi-role workspace switcher](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/73) — blocked against `main` until its dependency chain is merged.
- Backend #46: [InsuraGuest referral-only contract](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/pull/46).
- Frontend #74: [InsuraGuest referral-only state](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/pull/74).

### Backend focused PRs

Open focused PRs include #47, #48, #49, #50, #51, #52, and #54–#72. The exact current inventory and bases are visible in GitHub; important dependency examples are:

- #47 is based on `codex/multi-role-integrated`.
- #51 is based on #47's branch.
- #54–#64 are a stacked integrated chain.
- #65 → #66 is the diagnostics dependency.
- #67–#72 are focused fixes currently based directly on `main` and show `BLOCKED` until their intended prerequisites are merged/rebased.

### Frontend focused PRs

Open focused PRs include #75–#93. Important dependency examples are:

- #75 → #76 is the role-policy UI chain.
- #77 and #79 are based on the integrated multi-role branch.
- #78 → #81 → #83 → #85 is the persisted-role preference and browser coverage chain.
- #82, #86, #87–#93 are focused changes currently based directly on `main` and show `BLOCKED` until their intended prerequisites are merged/rebased.

### Final candidate branches

- Backend candidate branch: [codex/final-backend-integration-fixes](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/tree/codex/final-backend-integration-fixes), commit `090d17d260914d3367b53972ace842322d943bfe`.
- Frontend candidate branch: [codex/final-frontend-e2e-alignment](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/tree/codex/final-frontend-e2e-alignment), commit `fb6edc46024668f719cc28c596703ed2061f656f`.

No new PR was opened for these branches because they contain the accumulated integrated history of the existing PR chain; opening them against `main` would duplicate the existing PRs and produce oversized, conflict-prone review diffs. They are release-candidate evidence branches, not a substitute for merging the existing PR chain.

### Final focused correction heads

| Existing PR | Correction carried | Latest commit | Required CI | Merge state |
|---|---|---|---|---|
| Backend #67 | Native PostgreSQL JSONB filtering for authorized account search | `0cab8ae` | Success, run `39499205411` | Branch-policy blocked |
| Backend #69 | Authorized/idempotent Admin payment-hold release endpoint and audit trail | `3b59188` | Success; required build/test passed; deploy jobs skipped | Content mergeable; branch-policy blocked; Frontend #94 depends on it |
| Backend #51 | Immediate Guest activation for an active account, with regression test | `b48ce14` | Success, run `39499203172` | Stacked dependency; review/merge pending |
| Frontend #91 | Gate Guard invitation E2E uses searchable account selector | `1ac9522` | Success, run `39499203518` | Clean against its base; merge pending |
| Frontend #89 | PM E2E asserts approved customer-facing wording | `21d7db1` | Success, run `39499204095` | Branch-policy blocked |
| Frontend #74 | Remove legacy InsuraGuest transactional UI from compatibility entry point | `5946858` | Success, run `39501544481`; deploy job skipped | Content mergeable; branch-protection blocked; no merge/deploy |
| Frontend #94 | Send required decision body for Admin payment-hold release | `e5c8851` | Success, run `39502716486`; deploy job skipped | Content mergeable; depends on Backend #69; no merge/deploy |
| Backend #73 | Preserve persisted preferred role through SMS 2FA fallback | `9fce4e7` | Success, run `39504518558`; migration artifact passed; deploy jobs skipped | Content mergeable; depends on Backend #57; no merge/deploy |

## 9. Remaining blockers and ownership

### Blocker 1 — GitHub merge order and rebasing

**Owner:** Terrence/repository maintainer.  
**Action:** The focused correction heads, including Backend #73, Backend #74, and Frontend #74, have green required CI. Terrence still needs to merge or rebase the broader stacked PR chain in dependency order, then rerun CI on any resulting rewritten heads. Do not merge a `BLOCKED` PR merely because its individual historical CI run was green.

### Blocker 2 — Staging deployment and acceptance

**Owner:** Terrence.  
**Action:** Deploy the approved backend/frontend heads, then verify `/version.json`, `/api/health/version`, migrations, role sessions, Admin host, PM, Gate Guard, Safety Center, calendar, moderation, provider integrations, and worker schedules on staging.

### Blocker 3 — Provider and infrastructure gates

**Owner:** Terrence/infrastructure.  
**Action:** Verify MinIO/S3 destination, Brevo delivery, worker process, Stripe webhook secrets, CORS/cookie domains, DNS/TLS, and reverse-proxy host routing with controlled QA accounts. No production secrets were used locally.

### Blocker 4 — External provider and clean-room browser gates

**Owner:** Release environment owner.  
**Action:** The previously skipped backend MinIO and PostgreSQL gates now pass locally with disposable services. The final Playwright run leaves 11 explicit provider/project-matrix gates for the environment owner to exercise; they are listed in `artifacts/final-playwright-results-20261020-final2/FINAL_RUN_SUMMARY.md`.

### Blocker 5 — Visual approval

**Owner:** Terrence/product owner.  
**Action:** Review the A15 visual evidence and approve intentional screenshot changes before replacing protected visual baselines.

### Blocker 6 — Staging-main authentication console race

**Owner:** Release environment owner / merge reviewer.  
**Action:** Backend #74 now contains the focused correction and its exact-head CI is green, but it has not been merged or deployed. Terrence must merge/deploy it with the approved sequence and repeat the staging browser check; the staging-main 401 observation remains open until that is evidenced.

## 10. Recommended merge and release sequence

1. Rebase or merge the integrated backend foundation (#45 and its approved dependencies) and integrated frontend foundation (#73 and its approved dependencies) according to the PR bases shown in GitHub.
2. Merge backend auth/membership policy chain before frontend role-switching follow-ups.
3. Merge InsuraGuest backend/frontend referral-only changes without reintroducing activation or premium collection.
4. Merge storage, diagnostics, webhook, worker, calendar, moderation, and admin-selector fixes in their dependency order.
5. Merge frontend role preference, route-parameter, PM workflow, calendar, and accessibility/test follow-ups after the foundation they target.
6. Run CI on the resulting final PR heads; record actual results, not historical check results.
7. Terrence merges protected `main` branches.
8. Terrence deploys staging, applies approved migrations from backup, verifies versions and workflows, then decides production promotion.

## 11. What was deliberately not done

- No merge into protected `main`.
- No production or staging deployment.
- No shared/staging/production migrations.
- No pricing, commissions, Stripe capture semantics, date-hold behavior, provider configuration, DNS, TLS, or deployment workflow changes.
- No live financial transaction.
- No customer data modification.
- No force-push.
- No destructive reset or discard of pre-existing dirty worktrees.

## 12. Existing architecture audit

The broader URL/backend architecture inventory remains in [NESTYSTAY_URL_BACKEND_ARCHITECTURE_AUDIT_2026-10-20.md](NESTYSTAY_URL_BACKEND_ARCHITECTURE_AUDIT_2026-10-20.md). That document contains the detailed controller/action, route, migration, staging-probe, and deployment-topology inventory. This handoff supersedes its older local-candidate test numbers with the final candidate results in Section 6.

## 13. Handoff checklist

- [x] Final backend candidate committed and pushed.
- [x] Final frontend candidate committed and pushed.
- [x] Backend full deterministic suite passes 353/353 with disposable PostgreSQL and MinIO enabled.
- [x] Frontend Vitest, typecheck, build, and lint complete with no errors.
- [x] Full local Playwright matrix passes with zero failures and explicit skips only.
- [x] Disposable PostgreSQL database created and migrated locally only.
- [x] No protected branch merged.
- [x] No deployment performed.
- [x] Required CI completed for the published follow-up set: the nine listed focused correction PR heads plus Backend #72, whose changes are already included in the integrated backend candidate.
- [x] Frontend #94 payment-hold client correction published with focused tests, typecheck, build, lint, and required CI passing; merge remains Terrence-controlled.
- [x] Frontend #74 InsuraGuest hardening published at `5946858`; local tests and required CI pass, deployment job skipped, merge remains Terrence-controlled.
- [ ] CI completed on one final post-merge integrated head after the stacked PR chain is resolved.
- [ ] Stacked PRs rebased/merged in dependency order.
- [ ] Staging deployed and version-matched.
- [ ] MinIO, Brevo, worker, Admin host, PM, Gate Guard, Safety Center, calendar, moderation, and payment-provider acceptance completed on staging.
- [ ] Terrence visual approval received for protected screenshot baselines.

**Final status:** **PARTIALLY VERIFIED — BLOCKERS REMAIN.** The local developer-owned candidate is tested and published, but merge, CI-on-final-head, deployment, provider verification, and staging acceptance remain open.

## 14. Final integrated campaign evidence — 20 October 2026

The final clean integrated freeze was verified after the focused fixes were combined:

- Frontend freeze: `2844cea0bb06b1633f11a55f5b6f58ddb391c1e3`.
- Backend freeze: `07bccd28a916f7e20617d72396ec896a737c9787`.
- Vitest: **288 passed, 0 failed** across 62 files.
- Typecheck: **passed**.
- Production build: **passed**.
- Backend solution tests: **353 passed, 0 failed, 0 skipped**, with disposable PostgreSQL and MinIO enabled.
- Backend Release build: **0 warnings, 0 errors**.
- Playwright: **266 scheduled; 255 passed; 0 failed; 11 explicit skips; 0 not run**.
- Final Playwright summary: `C:\Users\Administrator\Desktop\nestystayPLATFORM\artifacts\final-playwright-results-20261020-final2\FINAL_RUN_SUMMARY.md`.
- Historical HTML report: `C:\Users\Administrator\Desktop\nestystayPLATFORM\artifacts\final-playwright-report-20261020-final2` (earlier HTML-reporter run; not authoritative for the final totals).

The completed browser campaign used a fresh disposable PostgreSQL database, the matching local API, and the matching Vite proxy. Privileged admin browser paths were enabled with an isolated local test token. MinIO browser certification and real-account production smoke login were explicitly gated because those credentials/configuration were not enabled; native MinIO and local provider tests ran separately. The earlier interrupted runs caused by a proxy mismatch and an email-outbox path mismatch were discarded; only the corrected run above is the final result. The disposable database was removed after testing. No staging/production deployment, protected-branch merge, shared migration, live payment, or provider configuration change occurred.

The final local result is therefore **developer-side complete for the tested candidate, awaiting Terrence-controlled merge and staging acceptance**. It is not production certification. The remaining gates are listed below and must stay open until evidenced on staging.

### 17. Latest public staging identity probe — 20 October 2026

A read-only probe of `https://staging.nestystay.net` returned HTTP 200 for `/version.json`, `/api/health`, `/api/health/ready`, and `/api/health/version`:

| Endpoint | Observed result |
|---|---|
| `/version.json` | Frontend `07dc7bc32fc4d92dd4e4dc3cf1ff2887dc487489`, built `2026-10-16T01:04:55.994Z` |
| `/api/health` | API status `ok`; PostgreSQL architecture |
| `/api/health/ready` | Database `ready`; storage `CONFIGURED` |
| `/api/health/version` | Backend `3bd5e2171f89b15b9b195ceff3215856af6ba950`, built `2026-10-15T19:47:01Z`, environment `Development` |
| `https://staging-admin.nestystay.net/` | HTTP 200; dedicated staging Admin hostname resolves |
| `https://staging-admin.nestystay.net/admin/sign-in` | HTTP 200; dedicated Admin sign-in path serves the frontend shell |
| `https://staging.nestystay.net/admin` and `/admin/sign-in` | HTTP 200; browser renders `Not found · NestyStay` on the public host |
| `https://admin.staging.nestystay.net/` | DNS failure; alternate hostname is not configured |

Focused browser verification rendered `Super Admin sign-in · NestyStay` at `staging-admin.nestystay.net/admin/sign-in` and the branded Not Found page at both public-host Admin paths. Each unauthenticated page emitted one `/api/auth/profile` HTTP 401 console response; this staging-main console-race observation should be compared with the corrected local candidate. These are the current remote `main` revisions, not the newer local integrated candidate SHAs. The readiness label does not identify MinIO or prove object persistence. This probe did not authenticate, mutate records, send email, exercise payments, or verify worker/calendar/role workflows.

## 15. Remote PR state recheck — 20 October 2026

The exact remote heads and required checks were re-read after the final local campaign:

| PR | Exact head | Base | Merge state | Required checks |
|---|---|---|---|---|
| Backend #69 | `3b59188b54ed4b83a4e9f4c0717281a4983c443f` | `main` | Content-mergeable; branch policy blocked | Restore/build/test **success**; deploy checks **skipped** |
| Backend #73 | `9fce4e7138afad3c6b7aa03539699fb918bb1744` | `codex/persist-preferred-role` | **clean** on its dependency base | Restore/build/test **success**; deploy checks **skipped** |
| Frontend #74 | `5946858a28c9e40c1ac06f9d00bc2513cf22bf14` | `main` | Content-mergeable; branch policy blocked | Typecheck/test/build **success**; deploy check **skipped** |
| Frontend #94 | `e5c8851e0cd0d5ffad4568bcfc3dda862de16ed3` | `main` | Content-mergeable; branch policy blocked | Typecheck/test/build **success**; deploy check **skipped** |

Remote `main` heads remain Frontend `07dc7bc32fc4d92dd4e4dc3cf1ff2887dc487489` and Backend `3bd5e2171f89b15b9b195ceff3215856af6ba950`. The final candidate branches remain pushed separately and are not merged. This confirms the release state; it does not authorize merging or deployment.

### Duplicate-diff check

Frontend #89 and #91 current heads are not ancestry-identical to the final candidate because those PRs were updated after the candidate branch was assembled. A changed-file comparison confirms their intended application changes are already present in the candidate: #89's `Property Manager workspace` wording and #91's searchable existing-account invitation flow. Frontend #74 and #94 are represented in the disposable freeze by their applied content rather than their original commit IDs. These should be sequenced or closed according to Terrence's review plan, not blindly cherry-picked again.

## 16. Latest multi-role follow-up

The final auth review identified and corrected one SMS fallback edge case. When a multi-role account completed SMS 2FA without an explicit workspace in the request, the API selected its generic role-order default instead of the persisted preferred role. Backend PR [#73](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/pull/73), commit `9fce4e7138afad3c6b7aa03539699fb918bb1744`, uses the persisted `profile.ActiveRole` fallback and adds a real endpoint regression test. It is based on Backend PR #57's branch and must be merged after #57.

Evidence: focused endpoint test **1/1**, complete `MultiRoleAccountTests` **7/7**, API build **0 errors**, and `git diff --check` passed. GitHub required run [39504518558](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/actions/runs/39504518558) passed build/test, idempotent migration-script generation, and artifact upload; deployment jobs were skipped. No migration was applied and no pricing, payment, provider, deployment, or shared-database change was made.

The disposable integrated backend freeze then applied the same correction on top of the final backend candidate at `07bccd28a916f7e20617d72396ec896a737c9787`. Its `MultiRoleAccountTests` class passed **12/12**, the API build passed with **0 errors**, and `git diff --check` passed. The freeze was not pushed, merged, or deployed.

## 18. Latest focused authentication follow-up — 21 October 2026

Backend PR [#74](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/pull/74) was corrected after its first CI run exposed an authorization regression. The final head is `f1cd71380cadf18d37fd6190329e9cbbed8d155a`.

| Check | Result |
|---|---|
| Anonymous `GET /api/auth/profile` | Local and remote tests expect HTTP 204 with an empty body |
| Invalid bearer profile probe | Local regression test expects HTTP 401 and passes |
| Revoked/stale session profile probe | Existing cookie/session regression tests expect HTTP 401 and pass |
| Focused local API set | 27 passed, 0 failed, 0 skipped |
| Remote CI run `39519661475` | Application 98, Domain 6, Infrastructure 32 + 1 skipped, API 180 + 1 skipped; build, full configured test job, idempotent migration-script generation, and artifact upload passed |
| Deploy jobs | Skipped; no deployment was performed |
| Files changed | `AuthController.cs`, `HealthEndpointTests.cs`, `M1AuthenticationAndRecommendationTests.cs` only |

The first PR head `761853e` was not retained as the accepted state because `[AllowAnonymous]` alone changed invalid/stale credential responses from 401 to 204. The follow-up commit restores that distinction. This PR is ready for Terrence’s review, but it is not staging acceptance evidence until merged and deployed.
