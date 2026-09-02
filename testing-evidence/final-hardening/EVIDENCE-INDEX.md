# Final hardening evidence index

All evidence below was generated from the current worktree after the accepted baseline commit `95c14a8b30c017e968b53cedc9165f83c53b9006`.

| Area | Artifact | Provenance |
|---|---|---|
| Capability inventory | `00-capabilities/CODEX-CAPABILITY-INVENTORY.md` | 309 enabled tools and 65 local skills |
| Security | `02-security/dynamic-security-checks.json` | Playwright API/browser security harness, 7/7 |
| Dependency security | `02-security/npm-audit.json`, `dotnet-vulnerable.txt`, `dotnet-outdated.txt` | npm/.NET CLI audits |
| Authorization | `04-authorization/authorization-matrix.json` | 37-case role/ownership matrix, 37/37 |
| Route coverage | `07-browser/route-coverage.json` | Playwright route inventory, 127/127 |
| Browser result | `07-browser/playwright-hardening-results.json` | 7-project hardening config, 15 pass/48 intentional skip |
| Accessibility | `08-accessibility/axe-results.json`, `keyboard-navigation.json` | axe and keyboard browser harness |
| Browser performance | `09-performance/browser-performance.json` | nine representative UI routes |
| API performance | `09-performance/api-performance.json` | 30 sequential samples per seven endpoint scenarios |
| Load | `10-load/load-results.json`, `load-test.mjs` | 960 local API requests at concurrency 10/25/50/100 |
| Concurrency | `11-concurrency/concurrency-results.json`, `concurrency-test.mjs` | parallel duplicate registration and QR validation plus backend test discovery |
| Database | `12-database/integrity-audit.sql`, `integrity-audit-results.txt` | PostgreSQL 18 local database, integrity/index/EXPLAIN evidence |
| Coverage | `15-coverage/frontend-all/coverage-summary.json`, `backend-final/**/coverage.cobertura.xml` | clean unit/backend coverage runs |
| Visual | `16-visual/baselines/laptop-chromium/*` | Playwright screenshot baselines, maxDiffPixelRatio 0.01 |
| Responsive | `17-mobile/responsive-*.json` | five Chromium viewport projects, 60 screens |
| Financial | `18-business-logic/financial-results.json` | 16 API-backed cent/commission/fee assertions |
| Continuation | `HARDENING-CONTINUATION.md` | scope, baseline, environment and pending-gate log |
| Reports | `reports/*.md`, `reports/FINAL-METRICS.json` | consolidated scorecard and machine-readable metrics (mirrored at repository `reports/`) |
