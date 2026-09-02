# Final hardening metrics

| Area | Measured result |
|---|---:|
| Backend tests | 101 passed / 0 failed |
| Frontend unit tests | 30 passed / 0 failed |
| Original browser regression | 31 passed / 0 failed / 8 skipped |
| Hardening browser matrix | 22 passed / 0 failed / 48 intentional skips (70 planned) |
| Route coverage | 127 / 127, 0 failures |
| Authorization matrix | 54 / 54 |
| Dynamic security checks | 7 / 7 |
| Financial assertions | 16 / 16 |
| API concurrency checks | 4 / 4; 13 backend concurrency/idempotency tests discovered |
| Accessibility | 12 pages; 0 critical, 0 serious, 0 moderate, 0 minor |
| Responsive checks | 60 screens, 1,750 controls, 0 failures |
| API performance | 210 samples, 0 errors, max 43.97 ms |
| Load harness | 960 requests, 0 application errors; 16 expected QR rate-limit responses |
| Frontend source coverage | Vitest included-source: statements 44.73%, branches 44.29%, functions 29.61%, lines 44.22%; historical full-source baseline 4.97/4.36/3.81/5.66% |
| Database | 145 tables, 250 indexes, 34 migrations, 45 FKs (0 before), 0 enforced M5 orphans, 0 integrity violations |
| Dependency audit | npm 0 vulnerabilities; .NET 0 vulnerable packages |
| JavaScript bundle | Initial 93,950 / 22,770 gzip; total 1,107,530 / 303,358 gzip; largest JS 248,941 bytes |
| CSS bundle | 130,639 bytes raw / 24,449 gzip |

Coverage is reported honestly over all included source files; browser/E2E evidence is tracked separately and is not converted into unit coverage percentages.

M5 Property Manager is 7/7 in the real browser workflow across the configured Chromium, Firefox, WebKit, desktop, laptop, tablet, mobile, and small-mobile projects. Lighthouse is recorded as tooling-blocked (CLI not installed); browser timing evidence remains green.
