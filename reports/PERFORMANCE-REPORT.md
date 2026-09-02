# Performance report

Measurements are local, synthetic, and unthrottled; they are release-candidate evidence, not a production SLO claim.

- API latency harness: 210 sequential samples across health, property browse, directory search, public page, experiences, platform modules, and QR validation; 0 errors; maximum observed 43.97 ms.
- Load harness: four rounds of 240 requests (960 total) at concurrency 10/25/50/100; 0 application errors and 0 timeouts. At concurrency 100, 16 QR requests were intentionally rate limited (429) by the protective limiter.
- Browser performance harness: nine representative routes with TTFB/FCP/LCP/CLS/TBT/transfer evidence in `testing-evidence/final-hardening/09-performance/browser-performance.json`.
- Database plans: property, booking guest/host, wellness host, conversation participant, invoice owner, and payment invoice access paths have model/migration indexes; PostgreSQL integrity audit is clean.

Production still needs representative data-volume benchmarks, a distributed load test, external CDN/object-storage measurements, and agreed SLO thresholds.
