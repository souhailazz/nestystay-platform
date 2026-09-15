# Final hardening evidence index

All evidence below was generated from the current worktree after the accepted baseline commit `95c14a8b30c017e968b53cedc9165f83c53b9006`; the resumed hardening pass started at `35794c8e8718d3c2f8a76e124a477d9a76f13427`.

| Area | Artifact | Provenance |
|---|---|---|
| Capability inventory | `00-capabilities/CODEX-CAPABILITY-INVENTORY.md` | 309 enabled tools and 65 local skills |
| Signed contract | `../../docs/contracts/NestyStay-Signed-Agreement-April-2026.pdf` | 11-page byte-for-byte copy; SHA-256 verified |
| Security | `02-security/dynamic-security-checks.json` | Playwright API/browser security harness, 7/7 |
| Dependency security | `02-security/npm-audit.json`, `dotnet-vulnerable.txt`, `dotnet-outdated.txt` | npm/.NET CLI audits |
| Authorization | `04-authorization/authorization-matrix.json` | expanded role/ownership matrix, 54/54 |
| Route coverage | `07-browser/route-coverage.json` | Playwright route inventory, 128/128 |
| Browser result | `07-browser/playwright-hardening-results.json` | 7-project hardening config, 84 tests / 24 pass / 60 intentional skip / 0 unexpected |
| M5 Property Manager | `07-browser/m5-*.json` | persisted manager/owner/QR/gate journey, 7/7 projects |
| Lighthouse/tooling | `07-browser/lighthouse.json` | CLI unavailable; browser timing evidence linked and green |
| Accessibility | `08-accessibility/axe-results.json`, `keyboard-navigation.json` | axe and keyboard browser harness |
| Browser performance | `09-performance/browser-performance.json` | nine representative UI routes |
| API performance | `09-performance/api-performance.json` | 30 sequential samples per seven endpoint scenarios |
| Load | `10-load/load-results.json`, `load-test.mjs` | 960 local API requests at concurrency 10/25/50/100 |
| Concurrency | `11-concurrency/concurrency-results.json`, `concurrency-test.mjs` | parallel duplicate registration and QR validation plus backend test discovery |
| Database | `12-database/integrity-audit.sql`, `integrity-audit-results.txt`, `relationship-inventory.md`, `fk-validation.json` | PostgreSQL 18 local database; 145 tables, 45 FKs, 0 enforced M5 orphans |
| Coverage | `15-coverage/frontend-all/coverage-summary.json`, `backend-final/**/coverage.cobertura.xml` | clean unit/backend coverage runs |
| Visual | `16-visual/baselines/laptop-chromium/*` | Playwright screenshot baselines, maxDiffPixelRatio 0.01 |
| Responsive | `17-mobile/responsive-*.json` | five Chromium viewport projects, 60 screens |
| Financial | `18-business-logic/financial-results.json` | 16 API-backed cent/commission/fee assertions |
| Continuation | `HARDENING-CONTINUATION.md` | scope, baseline, environment and pending-gate log |
| Deployment/email | `20-deployment/*` | compose parse/build, MinIO provider/container round-trip and real UI upload/download, EF outbox migrations, clickable email browser flows, monitoring validation, and structured backup-script review |
| Zero-credential continuation | `reports/ZERO-CREDENTIAL-PRODUCTION-PREP-ADDENDUM.md` | current provider boundary, MinIO/email/monitoring/backup validation and production-only blockers |
| Reports | `reports/*.md`, `reports/FINAL-METRICS.json`, `reports/FINAL-HARDENING-M5-ADDENDUM.md` | current M1–M5 decision, session/DB evidence, and machine-readable metrics (mirrored at repository `reports/`) |
