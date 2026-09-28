# Current-head Playwright regression — 2026-09-28

## Scope

- Frontend branch: `codex/final-release-certification`
- Frontend SHA: `20f199f80bea89e58b3632a4e828e04c488c9652`
- Backend: isolated certification head `ff31270f134c599fadb7dd7058f361fb45cc55ff`
- Database: disposable loopback PostgreSQL container; migrations already applied
- Storage: disposable private MinIO container; runtime-only credentials
- Email: local file provider for deterministic browser verification
- Admin: disposable server-issued local bootstrap fixture; no source credential
- Projects: desktop Chromium, tablet Chromium, mobile Chromium, Firefox smoke, WebKit smoke

## Result

| Metric | Result |
|---|---:|
| Discovered | 224 |
| Started | 224 |
| Passed | 213 |
| Failed | 0 |
| Explicitly skipped | 11 |
| Did not run | 0 |
| Duration | 26.5 minutes |

The complete current-head matrix exercised the M1–M5 public, authenticated, responsive, accessibility, security, SEO, MinIO UI, Property Manager, wellness, directory, QR, and booking workflows available locally. The 11 skips are explicit provider/deployment-only guards (real staging credentials, external delivery, or production-only surfaces); they were not silent omissions and are not represented as passing external verification.

No staging or production system was changed, and no credentials are stored in this evidence.
