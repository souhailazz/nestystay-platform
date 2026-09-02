# Final hardening metrics

| Area | Measured result |
|---|---:|
| Backend tests | 97 passed / 0 failed |
| Frontend unit tests | 26 passed / 0 failed |
| Original browser regression | 31 passed / 0 failed / 8 skipped |
| Hardening browser matrix | 15 applicable passed / 0 failed / 48 intentional skips |
| Route coverage | 127 / 127, 0 failures |
| Authorization matrix | 37 / 37 |
| Dynamic security checks | 7 / 7 |
| Financial assertions | 16 / 16 |
| API concurrency checks | 4 / 4; 13 backend concurrency/idempotency tests discovered |
| Accessibility | 12 pages; 0 critical, 0 serious, 0 moderate, 0 minor |
| Responsive checks | 60 screens, 1,750 controls, 0 failures |
| API performance | 210 samples, 0 errors, max 43.97 ms |
| Load harness | 960 requests, 0 application errors; 16 expected QR rate-limit responses |
| Frontend source coverage | Statements 4.97%, branches 4.36%, functions 3.81%, lines 5.66% |
| Database | 145 tables, 250 indexes, 32 migrations, 0 integrity violations |
| Dependency audit | npm 0 vulnerabilities; .NET 0 vulnerable packages |
| Initial JavaScript bundle | 93,950 bytes raw / 22,770 gzip; largest JS 248,941 bytes |

Coverage is reported honestly over all included source files; browser/E2E evidence is tracked separately and is not converted into unit coverage percentages.
