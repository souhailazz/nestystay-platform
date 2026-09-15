# NestyStay Final Adversarial Security Audit

Audit date: 2026-09-15
Scope: current NestyStay release-candidate worktrees and the clean local development runtime. No staging or production data was modified.

## Executive result

The audit found and fixed one high-severity server-side request forgery (SSRF) weakness in user-controlled iCalendar feed synchronization. The fix is in backend commit `c88e85a` and is covered by seven regression tests. The complete backend suite after the fix is green: **180 passed, 0 failed, 0 skipped**.

There are **0 known Critical findings and 0 known High findings remaining** in the reviewed code after that fix. One Medium defense-in-depth item remains open in the legacy Property Manager direct-base64 document paths: those paths enforce filename, declared MIME type and size, but do not run the shared magic-byte scanner before storing the private object. The paths are authenticated, manager-scoped and not publicly executable; this remains a hardening item, not a release claim.

**Security GO: NO.** This is not because a known Critical or High vulnerability remains. A final security GO is blocked because Semgrep, CodeQL, Gitleaks, Trivy and OWASP ZAP were unavailable on this runner, production-mode headers/provider behavior was not independently exercised, and human security/accessibility certification is outside automated local evidence.

## Audited revisions

- Backend: `codex/m1-m2-runtime-hardening`, security fix `c88e85a`.
- Frontend: `codex/m1-m2-runtime-hardening`, release-candidate commit `e389373`.
- Root/orchestration: `codex/frontend-hardening-release-candidate`, final audited candidate commit `6c1f23c`.
- The protected remote `main` branches were not bypassed. The existing pull requests remain subject to client approval.

## Tool and evidence inventory

### Executed successfully

- `dotnet test NestyStay.sln --no-restore --configuration Release`: **180 passed, 0 failed, 0 skipped** across Domain 5, Application 23, Infrastructure 27 and API 125.
- `dotnet list NestyStay.sln package --vulnerable --include-transitive`: no vulnerable packages reported for any project.
- `npm audit --omit=dev`: **0 vulnerabilities**.
- Existing redacted secret scan of the current tree/history and deployment/test material: no active credential values committed. Pattern hits were reviewed as placeholders, examples or test fixtures; secret values were not printed.
- Existing dynamic security checks: **7/7 passed** for CORS allow/deny behavior, malformed JSON handling, SQL-like input as data and stored-XSS output encoding.
- Existing authorization matrix: **54/54 passed** for unauthenticated access, role boundaries, ownership/tenant boundaries and safe error responses.
- Existing browser regression evidence: **182 passed, 0 failed, 9 intentional skips** across desktop, tablet and mobile projects. The skips are documented in `docs/testing/LOCAL-M1-M5-COMPLETION.md` and are external-fixture, deployed-credential or viewport-scoped checks.

### Not available on this runner

- Semgrep — executable not installed.
- GitHub CodeQL CLI — executable not installed.
- Gitleaks — executable not installed.
- Trivy — executable not installed.
- OWASP ZAP/ZAP CLI — executable not installed.

These are coverage limitations, not passing scan results. They must run in CI or on a security-tooling runner before a production security approval.

## Findings and remediation

### H-001 — Calendar feed SSRF and DNS-rebinding exposure — FIXED

Affected behavior: authenticated calendar feed synchronization accepted a user-controlled HTTP(S) URL and used a default `HttpClient`. Validation only rejected a small set of literal IPv4/private values. A hostname resolving to loopback, RFC1918, link-local, IPv6 ULA or other restricted space could therefore be used to target internal services. Automatic redirects and unbounded chunked response reads increased the exposure.

Remediation in backend `c88e85a`:

- Centralized feed URL validation for HTTP(S), no embedded credentials and only ports 80/443.
- Rejected localhost, loopback, private, link-local, multicast, ULA, documentation/test and other non-public destinations.
- Resolved hostnames before fetching and repeated the destination check at the socket connection boundary to close the DNS-rebinding gap.
- Disabled proxy use, cookies and automatic redirects for the calendar client.
- Rejected redirect responses and bounded response-body reads at 5 MiB, including chunked responses.
- Applied the same controls to both interactive sync and the background maintenance service.

Regression evidence:

- Seven tests cover loopback, IPv6 loopback, cloud metadata/link-local, RFC1918, non-standard ports, embedded credentials and allowed public web ports.
- The full unfiltered backend suite passed after the change.

### M-001 — Direct Property Manager document uploads lack magic-byte verification — OPEN

The authenticated M5 direct-base64 document, vendor-document and document-version paths validate path-safe filenames, an allow-listed declared MIME type and a 25 MiB size limit. The shared magic-byte scanner is used by the presigned/spec/wellness upload flows, but these legacy direct-base64 paths do not independently compare the bytes to the declared type before storage.

Impact is reduced because the routes require authenticated Property Manager/Admin access, storage keys are manager-scoped/private, and the inspected public download/export paths do not serve arbitrary content inline. Nevertheless, content-type spoofing can place a mismatched object in private storage and weakens defense in depth.

Required follow-up: route every direct-base64 M5 upload through the same magic-byte/checksum scanner and add negative API tests for mismatched PDF/JPEG/PNG content. This item was not changed during the SSRF remediation so that existing M5 upload semantics are not silently altered without its targeted test update.

### Informational — Legacy Alibaba references remain outside active runtime wiring

The active backend provider wiring selects Stripe Identity and does not contain an Alibaba provider implementation. Repository matches are limited to historical migration designer snapshots and older handoff/product documentation. Those references are not credentials and are not runtime provider selection, but the stale documentation should be cleaned up separately so deployment instructions cannot be misread.

## Control review

### Authentication, sessions and CSRF

- Cookie sessions are HttpOnly, SameSite=Lax and configurable for Secure use.
- State-changing cookie-authenticated requests require the separate CSRF cookie/header pair; safe methods are exempt.
- Authorization bearer/session paths are role checked, and legacy admin/operator token support is restricted outside production configuration.
- Authentication and sensitive-action rate limits exist with IP and authenticated-user dimensions.
- Existing auth/authorization tests passed; no confirmed auth bypass was found.

### IDOR, ownership and tenant isolation

- Booking, property, host, admin, directory, wellness and Property Manager stores consistently scope queries by actor/owner/manager where inspected.
- Existing authorization evidence passed 54/54, including cross-owner invoice/document access cases and role restrictions.
- This is strong local evidence, not an exhaustive independent attacker-pair test of every route; that remains a recommended CI security job.

### Payments, Stripe Identity and webhooks

- Payment totals, currency, provider references, capture/refund state and idempotency are server-authoritative.
- Badge PaymentIntent amounts and lifecycle state are persisted server-side; the frontend does not control success through a `PaymentSucceeded` trust flag.
- Webhook processing uses signature validation in configured provider mode and persisted event/idempotency handling in the local/test path.
- Stripe Identity is the active application provider; local deterministic coverage exists. A real external Stripe Identity session, live credentials and staging callback were not run here.
- Connect payouts and external bank rails remain provider/configuration gates.

### Uploads and object storage

- Presigned/spec/wellness uploads have path-safe object keys, size limits and magic-byte scanning.
- MinIO signing and local/R2 fallback paths enforce canonical object paths and private download authorization.
- M-001 above remains for the legacy direct-base64 M5 paths.

### Injection, XSS and unsafe parsing

- No unsafe frontend HTML sink was found in the reviewed source; dynamic checks confirmed React output encoding for stored payloads.
- SQL-like input remained data in the existing dynamic check. No unparameterized user-controlled SQL sink was found in the static review.
- JSON parsing uses `System.Text.Json`/bounded application parsers; no unsafe binary deserializer was found.
- No command execution path fed by user input was found.

### SSRF, redirects and outbound requests

- Calendar feed synchronization was the confirmed SSRF finding and is fixed as H-001.
- Other inspected outbound clients use configured provider endpoints or fixed service endpoints; no additional user-controlled outbound URL sink was confirmed.
- Production egress controls and network-level allow/deny policy still need deployment verification.

### Headers, TLS and browser policy

- Production security middleware adds CSP, HSTS, frame denial, MIME sniffing protection, referrer policy and restrictive Permissions Policy.
- CORS uses explicit configured origins and credentials are not combined with a wildcard origin.
- These controls were reviewed in source/configuration; local Development mode does not exercise the production-only middleware, so staging/production header capture is still required.

### Logging and privacy

- API exception responses return safe status/title/trace information rather than stack traces; expected validation text can still be returned to clients.
- No active secrets were printed or committed by this audit.
- Identity documents, private storage objects and police/provider privacy surfaces require production retention/log review by the client.

## Required closure actions

1. Run Semgrep, CodeQL, Gitleaks and Trivy in CI against the exact protected-main candidate SHAs.
2. Run OWASP ZAP against a disposable production-mode local deployment or staging with test credentials, including authenticated role contexts where approved.
3. Add the M-001 direct-base64 magic-byte fix and negative tests.
4. Capture staging headers/TLS/CORS and verify Stripe Identity, payment webhooks, Connect, storage and notification provider failure behavior with client-owned test credentials.
5. Complete manual attacker-pair tenant checks and human review of sensitive data, accessibility, reduced motion and forced colors.

## Security gate

| Gate | Result |
|---|---|
| Known Critical findings after remediation | 0 |
| Known High findings after remediation | 0 |
| Known Medium findings | 1 open (M-001) |
| Active committed secrets | None found; placeholder matches reviewed without exposing values |
| Dependency audit | npm 0 vulnerabilities; .NET no vulnerable packages reported |
| Dynamic local security checks | 7/7 checks passed; authorization matrix 54/54 passed |
| Independent security tooling | Incomplete — tools unavailable on runner |
| Production/staging security verification | Incomplete — external environment and credentials required |
| Final Security GO | **NO** |
