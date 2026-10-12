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

The pending-listing error-heading behavior is already in merged Frontend PR #15/current main. Frontend PR #31 is a redundant/conflicting follow-up and should not be treated as a required implementation. After the deployed current-main behavior is exercised, the quote rejection must show `Booking unavailable` with `This property is not available for booking yet.`; unrelated transport/server errors must retain the generic error state.

### Safety and Gate Guard

1. Authorized user opens `/safety` and reviews emergency guidance.
2. PM creates an incident at `/pm/professional?tab=incidents`; reload and retrieve it; test a role without scope and expect denial.
3. PM opens `/pm/team`, selects the owner/property scope, enters an existing account ID, selects `GATE_GUARD` and submits the invitation. Merged Frontend PR #32 adds this route-level workflow while retaining the existing server-authoritative `/api/property-manager/p0/members` implementation. The fresh-branch browser flow passed **1/1** for UI invitation plus persisted membership; the Guard must still accept through the real invitation, sign in and be checked on staging.
4. Assign the Guard to one property, validate a valid QR at `/gate/qr`, then test malformed, expired, revoked and wrong-property codes.
5. Confirm `/gate/guard` exposes only the assigned property and the audit record persists.

### PM records

Use explicitly synthetic records and clean them up only after evidence is captured. Local evidence is strongest for persisted API workflows: owner/invoice/utility/payment/statement round-trip, document download/expiry, subscription lifecycle, community notices, gate/QR history, governance/proxy scope, professional completion records, maintenance/readiness and cross-portfolio authorization. Browser evidence currently covers PM dashboard owner/unit/invoice/utility rendering and QR validation in `e2e/m5-property-manager.spec.ts`; it does not prove a complete browser-backed create/reload/update/export/download journey for every PM area. On staging, demonstrate create, reload, update/status, permission denial and export/download where applicable for owner portal, invoices, statements, payments, utilities, maintenance, vendors, community, documents and gate communication.

For A27, open `/pm/utilities` as an authorized Property Manager. Create an executive term, record a utility bill for a selected property, mark it as common-area when appropriate, attach a PDF/JPG/PNG, filter the list by period and term, reload, and confirm the corresponding owner statement/ledger entry. Expected result: the bill and attachment persist, out-of-scope writes are rejected, term overlap is rejected, and the owner statement exposes an authorized attachment action that downloads the private document. Local code evidence now covers the statement attachment mapping and authorized download path; the additive A27 migration is review-only until Terrence approves and applies it. The remaining status is staging acceptance, not an identified code gap.

For A28, open `/pm/governance` as an authorized Property Manager. Upload a property-scoped by-law/rules PDF through **Governing documents**, reload the page, open the document, then create a draft meeting with agenda and attendance, link an existing proposal, save and reload, publish minutes, retrieve the generated private PDF, and attempt a second edit/publication. Expected result: governing documents are listed for the selected property, the authorized owner can read the property document, other-property access is denied, the legal notice is present, the published minutes are immutable, publication is single-use, and proposal links remain scoped. The existing additive A28 meeting migration is review-only until Terrence approves and applies it.

### Remaining implementation and acceptance boundaries

- **A5/A17:** Frontend PR #16 is merged in `61585ea3a895d68203fad409af3ef0769cc867b3`; its former conflict is resolved. It preserves PR #15 booking-state and pending-listing restrictions while providing the responsive booking/sign-out modal layout. Staging still needs the deployed visual and keyboard checks.
- **A8:** Frontend PR #15 (`ec0640980190cdd0c45f525c38aaf81064667e0b`) owns the known pending-listing heading correction. The expected business-rule response is `Booking unavailable` with `This property is not available for booking yet.` Frontend #31 is redundant and should not be merged as a duplicate.
- **A14:** Frontend PRs #23 and #28 are merged in current `main`; `admin.nestystay.net` is the required dedicated Admin host and public-host `/admin` paths are denied outside local development. Terrence must configure the equivalent staging DNS/TLS host before formal acceptance.
- **A15:** Frontend PR #27 (`7dfe8898d1a07eac4205aaae8fe9cd7f87f70868`) is merged. Follow-up Frontend PR #36 (`f51bfca7fcb75b2e2a7c92ce287dcf2c8104a951`) fixes the reproduced 1024px tablet collapse by delaying the split layout to `xl` and adding a minimum form column. AuthModalSuite passed 13/13, the tablet Playwright regression passed 1/1, and required CI passed in run `38178347340`; desktop/tablet/mobile screenshots are local evidence only. Terrence still needs to review/merge/deploy PR #36 and perform the staging visual check.
- **A18:** Frontend PR #26 (`5feaf7836b4c0d263ce83261e60524f22e09ea94`) is merged and provides the role-specific Patois greeting registry and derives sign-in, post-login and sign-out copy from authenticated roles, with the NestyStay emblem included in the displayed greeting. The mapping is presentation-only and does not grant access. Focused registry/AuthModal tests passed (32), with hosted CI passing; staging still needs each supported role visually checked.
- **A20:** Frontend PR #32 (`39328572381b6f1b53bdc8813fa270dd2d84bc4c`) is merged and exposes the existing scoped Gate Guard invitation API at `/pm/team`. Focused module tests passed (3), the fresh-branch `/pm/team` browser flow passed (1), and hosted required CI passed. Follow-up Frontend PR #35 (`7d6208eddf0c32afd57a890b80441cc17be71c28`) corrects the role-matrix browser assertion to verify the real post-login `/gate/guard` workspace and its QR entry point; desktop and mobile role-matrix runs each passed (1/1), and required CI passed in run `38176917921`. Use the exact staging invitation/acceptance/QR sequence above.
- **A27:** implementation is reviewable in current-main Backend PR #22 at `14a9731e63856f6c46667e03d27b87a62fa3fed5` and current-main Frontend successor PR #34 at `fe16c77d0bc5fd511da5a5072827b3548e6a1229`. Backend #22 is based on current backend `main` `38984ecf686c0786cc961c45e6509e2a2b3a545c` and replaces stale Backend #19; the frontend successor is based on frontend `main` `335c565c140cd741285a499835916acff0be0832` and preserves current Gate Guard/staff changes while replaying the utility-bill workspace and attachment-linking fix. Backend #22 passed 310 tests with 0 failures and 2 environment-gated skips; required CI passed in run `38179088477`. Frontend #34 local focused tests 4/4, typecheck, build and targeted persisted utility browser flow 1/1 passed; hosted required CI passed in run `38175769454`. The migration is additive and unexecuted. Staging remains blocked until review, merge, deployment, Terrence-controlled schema application and authorized statement/attachment verification. Old Backend #19 and Frontend #29 are superseded/stale or conflicting references.
- **A28:** implementation is reviewable in current-main Backend PR #23 at `fce2d70c8ca848b0220bf4802d70aac1affabb53` and Frontend PR #33 at `a9939b7895c9d191298738d8028274382977b8af`. Backend #23 is based on current backend `main` `38984ecf686c0786cc961c45e6509e2a2b3a545c` and replaces stale Backend #20; the frontend successor is based on frontend `main` `335c565c140cd741285a499835916acff0be0832` and preserves the current-main Gate Guard team-route changes while adding the property-scoped governing-document list/upload/open workflow. Backend #23 passed 311 tests with 0 failures and 2 environment-gated skips; required CI passed in run `38179088277`. Frontend #33 required CI passed and deploy jobs were skipped. The existing meeting migration is additive and unexecuted. Staging remains blocked until review, merge, deployment, schema application and authorized workflow verification. Old Backend #20 and Frontend #30 are superseded/stale or conflicting references.

### Delivery timing

**A28 follow-up correction (2026-10-12):** Backend PR #24 (`bac0bc4dd8cf672371f6d2dcb2ea5ce66ed9b507`) is a dependent, single-purpose follow-up to Backend #23. It includes linked proposal title/status, eligible-voter count, vote count and non-identifying tally in the immutable published-minutes PDF. Its focused regression passed 1/1 and its full Release suite passed 311/0/2; it adds no migration. Review/merge order is Backend #23 → Backend #24, alongside Frontend #33. Staging acceptance remains blocked until deployment, approved schema application and authorized verification.

- **By 2026-10-15:** the reviewable code and handoff evidence are ready now; Terrence’s review/merge decision is required for the remaining open successors A27/A28. A1–A26 implementation PRs are merged or present in current main; their staging acceptance remains separate.
- **A26:** the application implementation is already in current `main`. The remaining browser-backed PM demonstration is estimated at one focused acceptance session (about half a day) after Terrence provides an authorized disposable PM/Owner session and cleanup approval. Conditional target: **2026-10-14** if access is available by **2026-10-13**; otherwise the target moves to the next working day after access is supplied.
- **A27:** the code and tests are reviewable in current-main PR #22/#34 now. The remaining work is Terrence’s review/merge, additive migration approval/application after backup, deployment and one focused statement/attachment acceptance session (about half a day). Conditional target: **2026-10-14** if approval, schema application and deployment complete by **2026-10-13**; otherwise the target is the next working day after deployment.
- **A28:** the code and tests are reviewable in current-main Backend #23 and Frontend #33 now. The remaining work is Terrence’s review/merge, additive meeting-migration approval/application after backup, deployment and one focused governance/document acceptance session (about half a day). Conditional target: **2026-10-15** if approval, schema application and deployment complete by **2026-10-14**; otherwise the target is the next working day after deployment.
- **Other staging rows:** acceptance can be run immediately after the correct deployed heads, authorized QA roles, disposable records and provider/service configuration are available. These dates are conditional because deployment, migrations, DNS/TLS, account provisioning and provider configuration remain Terrence-controlled.
- **Not complete:** Group A is not declared complete. The matrix contains 28 accounted-for rows, but several remain blocked by unmerged code, missing authorized staging workflows, unexecuted additive migrations or missing provider/service evidence.

## Current evidence boundary

Local tests and public health endpoints do not constitute staging acceptance. Staging rows remain `BLOCKED_EXTERNAL` until the correct deployed PR heads and authorized accounts are used in the workflows above. The latest focused backend run passed the pending-property/card-hold cancellation persistence test (**1/1**) and the selected calendar/Gate Guard/Property Manager authorization API tests (**15/15**). This does not establish staging behavior or real provider delivery.

Latest broad baseline remains Playwright 233 started / 213 passed / 0 failed / 20 documented skips / 0 not run; A28 backend full suite 309 passed / 0 failed / 2 environment-gated skips (311 total); A27 focused utility persistence/authorization test passed; A27/A28 frontend suites each 223 passed with typecheck and production build passing; local MinIO I/O, authorization and restart persistence passed. New targeted local browser evidence on 2026-10-11: Gate Guard/QR **2/2**, calendar plus PM calendar/QR **3/3**, officer/wellness plus PM completion **2/2**, PM professional completion **1/1**, incident create/reload/resolve/authorization **1/1**, corrected A27 utility persistence/attachment/reload **1/1**, focused PM/professional workflows **7/7**, and M5/mobile/PM release workflows **6 passed / 2 environment-gated skips**. The earlier focused PM run was **21 total: 15 passed, 3 failed, 3 skipped**; those failures were stale pre-A27 selectors and an intentionally collapsed Advanced workspace, and are now corrected in the A27 successor test. These are local-only results. Frontend PR #25 improves calendar E2E synchronization and diagnostics only; it does not prove live staging synchronization. No A27/A28 staging workflow has been claimed.

The A22 public-badge follow-up is Backend PR #21 (`2c6e1bf0f03d81febe364be5c35b76fd24665076`). It fixes the reproduced case where a listing remained `Free` in public Explore data after its host received an active Verified assignment. The focused regression, all five `BadgeAuthorizationTests`, and the full backend solution (309 passed, 2 environment-gated skips) passed locally; the required GitHub CI check is green and deploy jobs were skipped. After deployment, use an authorized host/admin fixture to change the host badge, reload `/explore` and `/explore/map`, and confirm the public listing badge changes while host/admin private views remain correct.
