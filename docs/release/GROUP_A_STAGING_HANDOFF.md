# Group A staging handoff

This is a non-secret handoff for Terrence-controlled staging acceptance. Do not put passwords, API keys, session cookies, database credentials or provider tokens in this file.

## Confidentiality note

The current handoff repository `souhailazz/nestystay-platform` is public. These documents contain no passwords, tokens, credentials, private QA records or customer data, but they do disclose internal architecture, staging URLs, defect status and operational acceptance details. Treat the public branch as publication-risk material; prefer an authorized private repository or direct client delivery for further sharing. Do not change repository visibility or delete history without authorization.

## Required staging accounts

Provision or confirm disposable accounts through the normal application/invitation paths. Report only account email, role, status and expected page.

| Role | Required status | Primary page | Scope |
|---|---|---|---|
| Guest | Active and verified as required | `/explore` | Public stays, quote and booking checks |
| Host / Property Owner | Active | `/host/properties`, `/calendar` | Owned listing and calendar fixtures |
| Property Manager | Active | `/pm/dashboard`, `/pm/calendar`, `/pm/team` | Disposable portfolio and PM records |
| Super Admin | Provisioned through the protected Admin process | `https://admin.nestystay.net/admin/sign-in` | Moderation and integration status only |
| Wellness / Safety role | Active and authorized | `/safety`, `/pm/professional?tab=incidents` | Safety and incident checks |
| Gate Guard | Invited, accepted and property-assigned | `/gate/guard`, `/gate/qr` | Assigned-property QR checks only |
| Service Provider | Active | `/directory/provider` | Audience-filter checks |
| Local Business | Active | `/directory/provider`, `/directory/businesses` | Business directory checks |

Do not convert `qa-pmstaff` into Gate Guard by directly editing the database. Use the PM invitation flow so the invitation, acceptance, assignment, revocation and audit records are exercised.

Terrence has confirmed that staging QA accounts exist and has independently verified Super Admin and Property Manager sign-in. This handoff does not reproduce passwords, tokens or private QA records. The current Codex boundary is the absence of an authorized authenticated staging session/credential injection, not evidence that the QA accounts are absent. Confirm the exact email, role and active/invited status through the approved handoff channel before testing.

## Dedicated Admin host

After Frontend PR #23 and its public-route follow-up PR #28 are deployed:

1. Create DNS `admin.nestystay.net` pointing to the same approved frontend origin/server as staging.
2. Issue TLS for `admin.nestystay.net`; redirect HTTP to HTTPS.
3. Serve the same frontend bundle for the Admin host. The client route resolver treats every non-`/admin` path on that host as the protected Admin sign-in screen.
4. Keep public `staging.nestystay.net` on the normal marketplace routes. Do not add public Admin registration.
5. If the API is cross-origin, include the exact approved frontend origin in the backend CORS allow-list and preserve the existing HttpOnly session/CSRF cookie settings. Do not use wildcard credentialed CORS.
6. Verify `/admin/sign-in`, `/admin`, `/admin/ops/properties`, logout, refresh and direct navigation. Ordinary users must receive a denial and must not see Admin data.

The existing Caddy deployment pattern can use the same common proxy block for both hosts, with `admin.nestystay.net` added as a separate site label. The Admin host must not be routed to a public-login-only server block.

## Acceptance sequence

### Booking and moderation

1. Admin opens `/admin/ops/properties`, reviews a disposable pending listing, and approves it.
2. Guest opens the approved listing, selects dates and guests, and confirms the booking modal retains those values.
3. Repeat with a pending listing. Quote and booking must be rejected with the typed business message; no payment authorization or approval email may be created.
4. Reject a test booking after authorization in Stripe test mode and verify the authorization is voided exactly once.

### Calendar

1. Host opens `/calendar`, adds a manual block, edits it, reloads, and releases it.
2. Host connects a disposable ICS feed, runs Sync now, checks history and blocked dates, then disconnects it.
3. Confirm the private export link contains no guest details and can be revoked.
4. Property Manager opens `/pm/calendar` and verifies a disposable reservation/event persists after reload.
5. Check the worker status/heartbeat before claiming scheduled synchronization; manual calendar operations alone do not prove the worker is running.

The pending-listing error-heading correction is in Frontend PR #31. After it is merged and deployed, the quote rejection must show `Booking unavailable` with `This property is not available for booking yet.`; unrelated transport/server errors must retain the generic error state.

### Safety and Gate Guard

1. Authorized user opens `/safety` and reviews emergency guidance.
2. PM creates an incident at `/pm/professional?tab=incidents`; reload and retrieve it; test a role without scope and expect denial.
3. PM opens `/pm/team`, selects the owner/property scope, enters an existing account ID, selects `GATE_GUARD` and submits the invitation. Frontend PR #32 adds this missing route-level workflow while retaining the existing server-authoritative `/api/property-manager/p0/members` implementation. The fresh-branch browser flow passed **1/1** for UI invitation plus persisted membership; the Guard must still accept through the real invitation, sign in and be checked on staging.
4. Assign the Guard to one property, validate a valid QR at `/gate/qr`, then test malformed, expired, revoked and wrong-property codes.
5. Confirm `/gate/guard` exposes only the assigned property and the audit record persists.

### PM records

Use explicitly synthetic records and clean them up only after evidence is captured. Local evidence is strongest for persisted API workflows: owner/invoice/utility/payment/statement round-trip, document download/expiry, subscription lifecycle, community notices, gate/QR history, governance/proxy scope, professional completion records, maintenance/readiness and cross-portfolio authorization. Browser evidence currently covers PM dashboard owner/unit/invoice/utility rendering and QR validation in `e2e/m5-property-manager.spec.ts`; it does not prove a complete browser-backed create/reload/update/export/download journey for every PM area. On staging, demonstrate create, reload, update/status, permission denial and export/download where applicable for owner portal, invoices, statements, payments, utilities, maintenance, vendors, community, documents and gate communication.

For A27, open `/pm/utilities` as an authorized Property Manager. Create an executive term, record a utility bill for a selected property, mark it as common-area when appropriate, attach a PDF/JPG/PNG, filter the list by period and term, reload, and confirm the corresponding owner statement/ledger entry. Expected result: the bill and attachment persist, out-of-scope writes are rejected, and term overlap is rejected. Current code evidence shows the owner statement DTO/UI does not yet expose the bill's `AttachmentDocumentId`, so the statement-link part is **INCOMPLETE** until a focused code change adds that link and re-runs authorization/download tests. The additive A27 migration is review-only until Terrence approves and applies it.

For A28, open `/pm/governance` as an authorized Property Manager. Create a draft meeting with agenda and attendance, link an existing proposal, save and reload, publish minutes, retrieve the generated private PDF through the authorized document path, and attempt a second edit/publication. Expected result: the legal notice is present, the published document is immutable, publication is single-use, the proposal link is scoped, and unauthorized users cannot read or mutate the meeting. Current code evidence shows no dedicated governing-document attachment/association operation on the governance record; Terrence must either accept the existing property-document workflow as the governing-document path or request a small focused addition. The additive A28 migration is review-only until Terrence approves and applies it.

### Remaining implementation and acceptance boundaries

- **A18:** Frontend PR #26 (`5feaf7836b4c0d263ce83261e60524f22e09ea94`) provides the role-specific Patois greeting registry and derives sign-in, post-login and sign-out copy from authenticated roles, with the NestyStay emblem included in the displayed greeting. The mapping is presentation-only and does not grant access. Focused registry/AuthModal tests passed (32), with typecheck/build/lint passing locally; required CI passed and the deploy job was skipped. After merge/deploy, confirm each supported role visually and confirm that authorization is unchanged.
- **A20:** Frontend PR #32 (`39328572381b6f1b53bdc8813fa270dd2d84bc4c`) exposes the existing scoped Gate Guard invitation API at `/pm/team`. Focused module tests passed (3), the fresh-branch browser flow passed (1), and hosted required CI passed; the deploy job was skipped. After review/merge/deploy, use the exact staging invitation/acceptance/QR sequence above.
- **A27:** implementation is reviewable in Backend PR #19 and Frontend PR #29. Both required CI checks are green, but the statement attachment link is not implemented. The migration is additive and unexecuted. Staging remains blocked until review, merge, deployment, Terrence-controlled schema application and the statement-link correction.
- **A28:** implementation is reviewable in Backend PR #20 and Frontend PR #30. Both required CI checks are green, but there is no dedicated governing-document association operation; the minutes/private-document path is present. The migration is additive and unexecuted. Staging remains blocked until review, merge, deployment, schema application and Terrence's decision on the governing-document path.

### Delivery timing

- **By 2026-10-15:** the reviewable code and handoff evidence are ready now; Terrence’s review/merge decision is required for the open PRs, especially A5/A8/A14/A15/A18/A20/A22/A27/A28.
- **After each approved deployment:** staging acceptance can be run immediately for the deployed item when the required QA role, disposable records and provider/service configuration are available. No staging completion date is asserted by Codex because deployment, migrations and account provisioning remain Terrence-controlled.
- **Not complete:** Group A is not declared complete. The matrix contains 28 accounted-for rows, but several remain blocked by unmerged code, missing authorized staging workflows, unexecuted additive migrations or missing provider/service evidence.

## Current evidence boundary

Local tests and public health endpoints do not constitute staging acceptance. Staging rows remain `BLOCKED_EXTERNAL` until the correct deployed PR heads and authorized accounts are used in the workflows above. The latest focused backend run passed the pending-property/card-hold cancellation persistence test (**1/1**) and the selected calendar/Gate Guard/Property Manager authorization API tests (**15/15**). This does not establish staging behavior or real provider delivery.

Latest broad baseline remains Playwright 233 started / 213 passed / 0 failed / 20 documented skips / 0 not run; A28 backend full suite 309 passed / 0 failed / 2 environment-gated skips (311 total); A27 focused utility persistence/authorization test passed; A27/A28 frontend suites each 223 passed with typecheck and production build passing; local MinIO I/O, authorization and restart persistence passed. New targeted local browser evidence on 2026-10-11: Gate Guard/QR **2/2**, calendar plus PM calendar/QR **3/3**, officer/wellness plus PM completion **2/2**, PM professional completion **1/1**, and incident create/reload/resolve/authorization **1/1**. These are local-only results. Frontend PR #25 improves calendar E2E synchronization and diagnostics only; it does not prove live staging synchronization. No A27/A28 staging workflow has been claimed.

The A22 public-badge follow-up is Backend PR #21 (`2c6e1bf0f03d81febe364be5c35b76fd24665076`). It fixes the reproduced case where a listing remained `Free` in public Explore data after its host received an active Verified assignment. The focused regression, all five `BadgeAuthorizationTests`, and the full backend solution (309 passed, 2 environment-gated skips) passed locally; the required GitHub CI check is green and deploy jobs were skipped. After deployment, use an authorized host/admin fixture to change the host badge, reload `/explore` and `/explore/map`, and confirm the public listing badge changes while host/admin private views remain correct.
