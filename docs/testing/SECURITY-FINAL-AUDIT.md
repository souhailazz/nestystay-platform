# NestyStay Security Final Audit

## Focused role/IDOR continuation — 2026-09-22

- Added direct API regression coverage for owner-to-owner, manager-to-manager, scoped PM Staff access boundaries, messages/attachments, Wellness resources, provider private records, and Admin-only policies.
- Enumerated all 192 protected PM actions and verified that enum roles outside each declared route policy did not receive a successful response.
- Focused suites: **7 passed, 0 failed**. Full backend suite after the addition: **200 passed, 0 failed, 1 skipped**.
- The executed focused cases produced **0 unexpected authorized 200 responses** across the requested message/attachment, Wellness, provider-private-data, Admin-policy, and PM scope cases. Physical storage I/O remains separately untested.

**Audit date:** 2026-09-22  
**Result:** substantial controls present; complete security certification is NOT complete. The repaired executable browser matrix completed at 188 passed, 0 failed, 36 explicit skips, 0 not-run.

## Controls reviewed

- Stripe webhook signature/idempotency code is present. Live webhook replay was not independently executed in this local run.
- Stripe Identity is the active runtime identity implementation found in backend/frontend source. No active Alibaba provider was found in runtime source.
- MinIO provider code uses private object access, signed download validation, path traversal rejection, size limits, and authorization at the API boundary. Physical MinIO I/O and restart persistence were not tested because Docker/MinIO was unavailable.
- Authentication/session code rejects invalid or stale sessions and rehydrates from the server. The browser failures caused by empty localStorage tokens and an admin token presented as a cookie are fixture defects; no application auth bypass was introduced.
- Server-side role/ownership checks exist. The protected PM route-policy sweep and focused owner/manager/staff, message/attachment, Wellness, provider, and Admin-policy IDOR cases pass; physical-storage I/O remains unexecuted.
- Backend NuGet vulnerability audit: no vulnerable packages reported across the nine projects. Frontend `npm audit --audit-level=high`: 0 known vulnerabilities. Root-level npm dependencies are unrelated to the frontend audit and were not used as a product result.
- Gitleaks: root 18 candidates, backend 7, frontend 0. Reviewed path/type context without printing values; candidates are examples/development placeholders or historical workflow/config material. No confirmed active credential was established. Historical exposure/rotation status remains owner-confirmation required.
- Alibaba search: current repository search found 295 matching lines, primarily immutable migration snapshots, historical docs/evidence, and root compose mappings. No active backend/frontend runtime provider was found. Existing migrations were not modified.

## Browser-auth remediation

The prior 25 failed checks were concentrated in admin routes and synthetic host-workspace tests across configured browser projects. The app correctly displayed “Sign in required” because the fixtures did not establish a valid server session. The fixtures were repaired to register/login through the real API and install the issued HttpOnly cookie. Current classification: **FIXED TEST_FIXTURE_BUG** for non-admin journeys. Admin browser paths remain **CONFIG_BLOCKED** without a legitimate provisioned Admin account; no client-side role trust or authentication bypass was added.

## Not yet certified

- SonarQube vulnerabilities/hotspots and quality gates: not run.
- Real MinIO authorization, invalid-type/magic-byte/oversize behavior, and restart persistence: not run.
- Full role/IDOR matrix: focused requested resource families pass; a literal every-resource/every-role certification remains broader than this pass.
- Full browser suite: executable tests green at 188 passed; 36 explicit configuration/provider skips remain.
- Human screen-reader, forced-color, reduced-motion, and manual keyboard certification: not complete.
- Real Brevo transport and mailbox delivery: not verified.

No production or staging changes were made during this audit.
