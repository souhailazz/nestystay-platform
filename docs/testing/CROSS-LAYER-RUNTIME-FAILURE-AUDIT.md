# NestyStay Cross-Layer Runtime Failure-Mode Audit

Audit date: 2026-09-21. Scope: the current local NestyStay workspace, the nested backend/frontend repositories, and read-only checks against `https://staging.nestystay.net`. Staging and production were not modified. QA credentials and provider secrets are intentionally not recorded.

## Executive verdict

The application source has three confirmed client-side timing/state defects fixed in the frontend feature branch, plus one deployment-state mismatch that still requires the normal PR/review/redeploy path:

1. Regular password login could route a newly authenticated non-Guest role to the Guest dashboard because React state had not re-rendered yet. Fixed in `424a90b` and retained in the current branch.
2. Passwordless completion duplicated the old router and sent ServiceProvider/LocalBusiness users to the Guest dashboard. Fixed in the current branch.
3. A 401/403 profile response could leave a revoked or forbidden session in React state because the old cleanup condition was inverted. Fixed in the current branch.
4. An authenticated Admin was not included in the shared post-auth router and could fall through to Guest. Fixed in the current branch.
5. The deployed staging frontend still behaves like the older bundle: role-specific URL shells can render before the current source guard is deployed. Backend authorization remains correct. This is a deployment mismatch, not permission to weaken the API.

The current frontend fix is committed and pushed as `5bd5270eefe81ef5a1c5726b0e74b3bb42c9e7ee` on `codex/m1-m2-runtime-hardening`. The local backend suite is green at 192/192. The frontend unit suite, typecheck, and production build are green. The complete Playwright matrix was enumerated but not run in this pass because the local frontend/backend services are stopped; the staging smoke endpoints were checked read-only and returned 200.

## Evidence collected

| Area | Evidence | Result |
|---|---|---|
| Backend tests | `dotnet test NestyStay.sln --no-restore` | 192 passed, 0 failed, 0 skipped |
| Frontend unit tests | `npm test -- --run --reporter=dot` | 55 passed across 11 files |
| Frontend typecheck | `npm run typecheck` | Passed |
| Frontend production build | `npm run build` | Passed; Vite transformed 2,154 modules |
| Frontend lint | `npm run lint` | 0 errors, 84 existing warnings |
| Staging liveness | `GET /api/health/live` | HTTP 200 |
| Staging readiness | `GET /api/health/ready` | HTTP 200 |
| Staging properties | `GET /api/properties` | HTTP 200 |
| Browser matrix | `npx playwright test --list` | 221 tests in 35 files enumerated; execution blocked by stopped local services |
| Staging role smoke | Read-only Chromium/API checks with the supplied runtime QA mechanism | Guest, Host, Owner, PM, Officer, Service Provider, and Local Business login responses were accepted; PM Staff remains intentionally invited |
| Staging authorization | Same-origin API check | Guest → PM dashboard API returned 403; PM → same API returned 200 |

The deployed staging UI did not receive the current feature-branch bundle during this audit. Therefore source-level route-guard tests and staging browser route behavior must not be conflated.

## Failure table

| ID | Layer | Trigger | Expected | Actual before fix/observation | Root cause | Severity | Status / regression |
|---|---|---|---|---|---|---|---|
| CLR-001 | Frontend auth/routing | Password login succeeds for PM, Owner, Host, Officer, or provider | Route directly to the role workspace | Guest fallback could win before React session state updated | `finishSignIn` read stale React state | High | Fixed; `postAuthRoute` tests, local staging-backed browser check, commit `424a90b` |
| CLR-002 | Frontend auth/routing | Passwordless login completes for ServiceProvider or LocalBusiness | Route to `/directory/provider` | Duplicate passwordless router handled only PM/Owner/Host/Officer and fell back to Guest | Two routing implementations drifted | High | Fixed; provider route unit coverage in `postAuthRoute.test.ts` |
| CLR-003 | Frontend session state | Profile hydration returns 401/403 for expired, revoked, suspended, or forbidden session | Clear cached session and pending challenge | Old condition only cleared state when 401 occurred while no cached session existed | Inverted cleanup guard | High | Fixed; jsdom `useAuth` regression test |
| CLR-004 | Frontend auth/routing | Authenticated Admin completes login | Route to `/admin` | Admin was not represented in the post-auth priority chain and could fall back to Guest | Missing Admin case in shared router | High | Fixed; Admin route unit coverage |
| CLR-005 | Frontend redirect safety | Login modal receives a return target | Navigate only to a same-origin internal path | Any string beginning with `/` was accepted, including protocol-relative targets | Weak return-target validation | Medium | Fixed; rejects `//`, absolute URLs, and CR/LF |
| CLR-006 | Deployment/runtime | A user visits a protected workspace on currently deployed staging frontend | Current route manifest guard should enforce role access | Older deployed UI can render a workspace shell for a wrong role, while the API correctly returns 403 | Staging frontend bundle is behind the source feature branch | High | Not a code bypass; requires PR merge and staging redeploy, then browser re-test |
| CLR-007 | Contract/schema | Backend role enum contains `AssociationExecutive`, `Tenant`, and `GateGuard` | Every supported role should have an intentional web representation | Frontend `UserRole` omits those values | Cross-layer role contract is ahead of/unclear in current web scope | Medium / scope | Unresolved scope item; Gate Guard is documented as later/unclear and was not added in this pass |
| CLR-008 | Test validity | Synthetic localStorage role is used by some browser tests | Authorization result should be proven against server identity | Synthetic sessions can prove UI rendering but cannot prove backend authorization | Test fixture boundary | Medium | Test limitation; separate staging API checks prove PM/Guest authorization, full browser matrix still needs services |

## Timing, state, and transition audit

The requested audit covers cold start/configuration, authentication, role routing, protected-route authorization, booking transitions, identity/payment transitions, notifications, uploads/email, responsive behavior, accessibility, and deployment/runtime parity. The checklist was reviewed against the current source and existing tests; only the executable checks listed above are counted as run in this pass.

### Authentication and routing

- Session persistence is read synchronously after login before choosing the destination.
- Password and passwordless flows now share the same role-priority function.
- Admin is routed to `/admin`.
- Unsafe external and protocol-relative return targets are rejected.
- Profile 401/403 responses clear the cached session, pending challenge, and React auth state.
- Existing route-manifest role guards remain server-independent UI guards; API authorization remains authoritative.
- A full post-redeploy check is still required because the deployed frontend currently predates these changes.

### Booking, identity, payment, and notification transitions

- Backend tests cover booking, authentication, persistence, rejection, verification, payments, refunds, webhooks, and idempotency: 192/192 passed.
- This audit did not claim a live external Stripe Identity session or live payment as locally verified.
- A complete browser state-machine execution was not possible while local services were stopped.
- Notification deep-link/count behavior remains a source/test area that needs the full browser matrix after services are started; no new implementation was invented here.

### Upload, email, and provider boundaries

- Current application documentation and source use private server-local storage, not MinIO, with `OBJECT_STORAGE_PROVIDER`, `NESTYSTAY_STORAGE_LOCAL_ROOT`, and `NESTYSTAY_STORAGE_SIGNING_SECRET` as the relevant environment contract.
- Brevo is implemented behind the provider selector but staging enablement and mailbox delivery remain deployment-owner responsibilities.
- Stripe Identity is the active implementation; Alibaba is not active. Immutable historical references are not treated as runtime providers.
- Upload and email failures were not reclassified as application failures merely because staging configuration is external.

## 80-phase coverage map

The 80-phase request is treated as a coverage checklist, not as permission to fabricate a pass rate. The following domains were reviewed; “partial” means source evidence exists but execution depends on a running service, external provider, or human verification.

| Domain | Coverage reviewed | Current result |
|---|---|---|
| 1. Process/bootstrap | startup, health, migration/config assumptions | Partial; staging health green, local app services stopped |
| 2. Database/state | persistence, stale state, restart boundaries | Backend automated tests green; live local restart sequence not run |
| 3. Authentication | login, passwordless, session hydration, 401/403 cleanup | Source/unit verified; staging role smoke partially verified |
| 4. Role routing | Guest, Host, Owner, PM, Officer, providers, Admin | Source/unit verified; deployed staging bundle stale |
| 5. Authorization | wrong-role and API scope boundaries | PM/Guest staging API check verified; full matrix not rerun |
| 6. Booking state machine | quote, pending, approval/rejection, cancellation/status UI | Backend suite verified; browser execution blocked locally |
| 7. Identity/payment | Stripe Identity, PaymentIntent, capture/refund/webhook/idempotency | Backend tests verified; external/live session not claimed |
| 8. Notifications/email | unread/deep links, outbox/provider boundaries | Source and prior tests exist; complete runtime pass pending services/config |
| 9. Storage/uploads | private server-local storage and upload consumers | Code/config contract documented; staging storage verification external |
| 10. UI quality/deployment | focus, mobile, reduced motion, browser matrix, deploy parity | Build/lint/unit green; full browser execution and redeploy parity pending |

This is why the final verdict is not “80/80 runtime certified”: a stopped local stack prevents honest end-to-end execution of every timing and persistence phase.

## Remaining blockers and ownership

### Souhail / application and PR

- Ensure the current frontend branch PR contains `5bd5270eefe81ef5a1c5726b0e74b3bb42c9e7ee`.
- Keep the route/session regression tests in the PR.
- Do not add Gate Guard provisioning or other new scope based only on this audit; the role contract needs a product decision first.
- After deployment, rerun the cross-role browser tests and confirm the deployed bundle matches the branch.

### Terrence / staging workflow

- Review and merge the protected frontend PR; the source fix is not live until the normal deployment runs.
- Redeploy staging from the merged frontend commit.
- Re-run the staging browser matrix against the merged build.
- Keep server-local storage and Brevo configuration in server secrets/environment only; no values belong in Git.

### External/provider or human verification

- Real Stripe Identity/payment behavior remains dependent on the configured safe test environment and provider credentials.
- Brevo delivery requires sender/domain configuration and mailbox verification.
- Upload QA requires the server-local storage directory, permissions, persistence, TLS, and backup configuration to be verified on staging.
- Screen-reader, forced-color, and full human accessibility certification remain human/manual activities beyond automated tests.

## Final audit verdict

- Confirmed source defects found and fixed: **4 functional routing/session defects plus 1 redirect-safety hardening defect**.
- Backend regression suite: **PASS — 192 passed, 0 failed, 0 skipped**.
- Frontend unit/typecheck/build: **PASS — 55 tests passed; typecheck and build passed**.
- Frontend lint: **PASS with warnings — 0 errors, 84 warnings**.
- Staging API health/readiness: **PASS — all checked endpoints returned 200**.
- Current deployed frontend parity: **BLOCKED — redeploy required**.
- Full local browser matrix: **NOT RUN — services stopped; 221 tests/35 files enumerated**.
- No staging or production data was changed.
- The release is not certified as fully cross-layer runtime complete until the PR is merged, staging redeploys, and the browser/state-machine matrix is rerun against the resulting build.
