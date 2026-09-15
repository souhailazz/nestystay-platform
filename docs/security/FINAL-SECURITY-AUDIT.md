# NestyStay Final Adversarial Security Audit

Audit date: 2026-09-15
Scope: current NestyStay release-candidate worktrees and the clean local development runtime. No staging or production data was modified.

## Executive result

The audit found and fixed one high-severity server-side request forgery (SSRF) weakness in user-controlled iCalendar feed synchronization. The fix is in backend commit `c88e85a` and is covered by seven regression tests. The legacy Property Manager direct-base64 upload gap was then closed in backend commit `ba75107`: property documents, document versions, vendor documents and maintenance/work-order attachments now all use the shared server-side magic-byte/checksum scanner, with spoofed-signature regression coverage. The complete backend suite after these changes is green: **180 passed, 0 failed, 0 skipped**.

There are **0 confirmed application Critical, High or Medium findings** remaining in the reviewed source. Trivy still reports four low deployment hygiene notices (missing Docker HEALTHCHECKs), and ZAP reports local Development-mode header observations; these are documented below and are not being represented as production verification.

**NESTYSTAY LOCAL SECURITY GO: YES, conditional on the documented deployment notes.** Local code, dependency, authorization, dynamic, Semgrep, Gitleaks, Trivy, ZAP and regression evidence is available, with no confirmed application Critical/High/Medium finding after remediation. **NESTYSTAY PRODUCTION SECURITY GO: NO** until the client-controlled staging/production proxy, TLS, provider credentials/webhooks, egress policy and human review are verified.

## Audited revisions

- Backend: `codex/m1-m2-runtime-hardening`, upload hardening commit `ba75107` (including SSRF fix `c88e85a`).
- Frontend: `codex/m1-m2-runtime-hardening`, non-root nginx hardening commit `a3d64a4` (including the prior release candidate `e389373`).
- Root/orchestration: `codex/root-main-integration-current`, audited integration commit `b60c710` (security/code tree propagated from candidate `de40846`).
- The protected remote `main` branches were not bypassed. The existing pull requests remain subject to client approval.

## Tool and evidence inventory

### Executed successfully

- `dotnet test NestyStay.sln --no-restore`: **180 passed, 0 failed, 0 skipped** across Domain 5, Application 23, Infrastructure 27 and API 125. The only output was existing obsolete-test warnings for the legacy application badge test helper.
- `dotnet list NestyStay.sln package --vulnerable --include-transitive`: no vulnerable packages reported for any project.
- `npm audit --omit=dev`: **0 vulnerabilities**.
- Gitleaks 8.30.1 ran against root/backend/frontend working trees and Git history with full redaction. Root current-tree findings (493) were dominated by ignored PostgreSQL WAL, local email-outbox/build artifacts and ignored local environment files; backend current-tree findings (2) were in the ignored local `.env`; frontend current-tree findings were 0. History findings were root 18, backend 7 and frontend 0, limited to example/development/test configuration and historical workflow/template matches. No live credential value was printed or committed by this audit; local ignored Stripe values must still be rotated if they are not disposable test credentials.
- Semgrep 1.177.0 ran successfully on tracked C# sources (274 targets) and tracked frontend JS/TS sources (158 targets): **0 findings**. The initial directory invocation hit a Windows parser error on empty `.gitignore` comment lines; the final tracked-file/source-only scans completed successfully.
- Trivy 0.74.0 ran with `vuln,misconfig,secret` against filtered source/deployment trees. After the non-root frontend fix: **0 dependency CVEs, 0 secrets**, and four LOW `DS-0026` “no HEALTHCHECK” notices across the backend/API/frontend Dockerfiles. The prior HIGH `DS-0002` root-user frontend findings are gone.
- OWASP ZAP 2.17.0 ran passively against local frontend/API public URLs: **23 observations** (12 Medium header/policy observations, 7 Low, 4 Informational). A safe active scan was run against the local frontend URL and produced **0 active alerts**, but ZAP found no active-scan nodes in the SPA root, so this is not authenticated API coverage.
- CodeQL: no local CLI and no CodeQL workflow exists in the inspected GitHub Actions workflows. This remains an external CI/GitHub security-scanning item; it was not fabricated as a passing result.
- Existing dynamic security checks: **7/7 passed** for CORS allow/deny behavior, malformed JSON handling, SQL-like input as data and stored-XSS output encoding.
- Existing authorization matrix: **54/54 passed** for unauthenticated access, role boundaries, ownership/tenant boundaries and safe error responses.
- Existing browser regression evidence: **182 passed, 0 failed, 9 intentional skips** across desktop, tablet and mobile projects. The skips are documented in `docs/testing/LOCAL-M1-M5-COMPLETION.md` and are external-fixture, deployed-credential or viewport-scoped checks.

### Tool limitations and classification

- ZAP’s Medium results came from direct Development-mode responses where the reverse-proxy security headers are not present. They require staging capture through the client’s nginx configuration before production approval; they are not treated as an application authorization or injection finding.
- Trivy’s remaining LOW results are image-health metadata recommendations. The frontend images now run nginx as non-root on port 8080; the existing backend/API images already run as UID 10001. Adding a HEALTHCHECK is a deployment hygiene follow-up, not a confirmed vulnerability.
- CodeQL requires GitHub Actions/organization enablement or a separately installed runner. No local or remote CodeQL result is claimed.

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

### M-001 — Direct Property Manager document uploads lack magic-byte verification — FIXED

The authenticated M5 direct-base64 document, vendor-document, document-version and maintenance/work-order attachment paths previously validated filenames, declared MIME types and sizes without comparing bytes to the declared type.

Remediation in backend `ba75107`:

- Reused the registered `IFileSafetyScanner` for all four legacy direct-base64 paths.
- Normalized and allow-listed PDF, JPEG and PNG content types before storage.
- Rejected empty content, invalid base64, size mismatches and spoofed signatures before calling the storage provider.
- Kept manager ownership scoping unchanged; the existing unrelated-manager document download test still returns 404.

Regression evidence:

- API tests now reject HTML/text bytes declared as PDF for property documents, document versions, vendor documents and maintenance attachments.
- The targeted M5 security workflow passed 3/3 tests, and the complete backend suite passed 180/180.

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
- Legacy Property Manager direct-base64 documents, versions, vendor documents and maintenance attachments now use the same magic-byte/checksum scanner before storage (M-001 fixed).
- MinIO signing and local/R2 fallback paths enforce canonical object paths and private download authorization.

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

1. Enable CodeQL in GitHub Actions (or on the client security runner) against the exact protected-main candidate SHAs.
2. Repeat ZAP against a disposable production-mode deployment or staging with approved test credentials, including authenticated role contexts.
3. Capture staging headers/TLS/CORS and verify Stripe Identity, payment webhooks, Connect, storage and notification-provider failure behavior with client-owned test credentials.
4. Complete manual attacker-pair tenant checks and human review of sensitive data, accessibility, reduced motion and forced colors.

## Security gate

| Gate | Result |
|---|---|
| Known Critical findings after remediation | 0 |
| Known High findings after remediation | 0 |
| Known application Medium findings after remediation | 0 confirmed; ZAP Development-mode header observations are classified as deployment verification items |
| Active committed secrets | None found; local ignored environment values were not printed |
| Dependency audit | npm 0 vulnerabilities; .NET no vulnerable packages reported |
| Dynamic local security checks | 7/7 checks passed; authorization matrix 54/54 passed |
| Independent security tooling | Gitleaks, Semgrep, Trivy and ZAP executed; CodeQL requires external CI enablement |
| Production/staging security verification | Incomplete — external environment and credentials required |
| NestyStay local security GO | **YES, conditional on documented LOW/deployment follow-ups** |
| NestyStay production security GO | **NO — client staging/provider/human verification remains** |
