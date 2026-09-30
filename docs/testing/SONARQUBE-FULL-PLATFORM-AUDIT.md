# SonarQube Full Platform Audit

**Audit date:** 2026-09-23  
**Execution:** completed against the local repository checkout; no source was uploaded outside the local SonarQube container.  
**SonarQube:** Community Build 26.9.0.129388, `http://localhost:9000`, status `UP`  
**Source SHAs:** root `b6ace52aa34d253d4f4b8b70235fb306e12607a6`; backend `181e1205d96ab4dac98d0b6830336579a876076d`; frontend `98684a0a1455b7e21bc0b7b3bda4f66fb61062a4`.

## Analysis evidence

| Project | Analysis ID | Dashboard | Task result |
|---|---|---|---|
| Backend | `c0d93d42-5475-402d-9a12-52bcd1f35e35` | [backend dashboard](http://localhost:9000/dashboard?id=nestystay-backend) | [SUCCESS](http://localhost:9000/api/ce/task?id=62fdae46-329c-4d3c-9bce-48d9cc05e974) |
| Frontend | `5e4d19b5-c717-4603-9b02-91d03148e47d` | [frontend dashboard](http://localhost:9000/dashboard?id=nestystay-frontend) | [SUCCESS](http://localhost:9000/api/ce/task?id=c03f0c93-a72e-467d-b590-7758d1aa6e3e) |
| Platform/root | `7bd551f6-f6cb-4bb9-ad45-5bb6e9ffc552` | [platform dashboard](http://localhost:9000/dashboard?id=nestystay-platform) | [SUCCESS](http://localhost:9000/api/ce/task?id=45a3c00c-5b31-4d44-9afd-acee1aa4e510) |

## Measures and quality gates

| Project | Bugs | Vulnerabilities | Hotspots | Code smells | Coverage | Duplication | Quality Gate |
|---|---:|---:|---:|---:|---:|---:|---|
| `nestystay-backend` | 9 | 31 | 0 | 1,008 | 51.4% | 20.6% | **OK** |
| `nestystay-frontend` | 6 | 3 | 0 | 971 | 19.3% | 1.4% | **OK** |
| `nestystay-platform` | 0 | 0 | 0 | 0 | n/a | 0.0% | **OK** |

Branch coverage was 51.2% for the backend and 31.9% for the frontend. The default Sonar quality gate is green, but open findings remain; the gate is not a claim that every finding is fixed.

## Manual review classification

- **Security hotspots:** 0 open-to-review hotspots in each project.
- **Backend vulnerabilities:** the 31 findings are predominantly rule matches on conditional HTTPS/secure-cookie configuration, explicit upload request limits, deterministic development/seed values, TOTP's RFC-compatible HMAC-SHA1, deterministic MD5-based seed GUIDs, and the configured MinIO HTTP development endpoint. These are not all equivalent. The MinIO development fallback and production configuration enforcement remain **NEEDS_REVIEW** for production deployment; the deterministic seed/TOTP matches are **FALSE_POSITIVE / EXPECTED USE** rather than leaked credentials.
- **Frontend vulnerabilities:** the three `Math.random()` findings are fallback ID generation after `crypto.randomUUID()` and are **NEEDS_REVIEW** only if those IDs are ever used as security tokens; current call sites are client-side upload/UI identifiers, not authorization credentials.
- **High/critical bugs:** backend cancellation-token findings in exception middleware and frontend sort/unreachable-code findings are **SONAR_STATIC_ONLY** in this run. Existing backend and browser tests did not show a runtime failure. The frontend `return` after the checklist expression is a genuine maintainability defect candidate, but no fix was applied during this audit.
- **Code smells:** the remaining findings are maintainability/accessibility/duplication observations. They are recorded for remediation; no broad refactor was made during the audit.

## Gitleaks

The current redacted scan inspected 290 root commits, 122 backend commits, and 49 frontend commits. It found 18 root candidates, 7 backend candidates, and 0 frontend candidates. The candidates are confined to `.env*.example`, development appsettings, workflow placeholders, and historical paths; no active production credential was identified in the redacted review. No secret values are included in the audit or repository.

## Related runtime verification

- Backend full suite: 200 passed, 0 failed, 1 expected MinIO skip in the normal no-fixture run.
- MinIO disposable-container test: 2 passed, including upload/overwrite, signed download, wrong-credential denial, traversal rejection, and size rejection. The browser MinIO fixture was not enabled in the full matrix because it requires `NESTYSTAY_MINIO_E2E=true`; this is distinct from the provider-level I/O pass.
- Full Playwright matrix: 227 scheduled/started, 186 passed, 3 failed, 38 intentional/configuration skips, 0 not-run. The three failures are visual snapshots only: register on desktop, login on tablet, and login on mobile. No functional/browser security test failed.
- Frontend unit tests: 56 passed. Typecheck and production build passed. Lint: 0 errors, 84 warnings. `npm audit`: 0 known vulnerabilities.

## Limitations

Real Brevo delivery was not attempted because no safe runtime sender/API credentials were supplied to this local audit. Staging deployed SHAs were not exposed by the local checkout and therefore remain unknown. Human screen-reader certification and external provider verification remain separate from this local Sonar run.

This file intentionally does not contain Sonar tokens, passwords, API keys, raw secret candidates, or production data.
