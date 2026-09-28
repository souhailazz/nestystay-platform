# Local Playwright Regression — 2026-09-27

This is the latest complete local browser run from the isolated certification worktrees. The original dirty workspace, protected `main` branches, staging, and production were not modified.

## Code under test

| Repository | Branch | SHA |
|---|---|---|
| Root/orchestration | `codex/final-release-certification` | `d1e49efc6ac5d1b3c1c8bf7d04585917be097772` |
| Backend | `codex/final-release-certification` | `6277a950d563ce32e01a51223776fac9981529dd` |
| Frontend | `codex/final-release-certification` | `96cbac8209f87381508bcebf0de13b8bb716ff07` |

## Result

- Inventory: **224 tests across 36 files**.
- Started: **224**.
- Passed: **213**.
- Failed: **0**.
- Explicitly skipped: **11**.
- Did not run: **0**.
- Projects exercised: desktop Chromium, tablet Chromium, mobile Chromium, plus the configured Firefox/WebKit critical smoke projects.
- Separate admin-token-gated check: **12 passed, 0 failed, 0 skipped**.
- MinIO browser opt-in rerun with `OBJECT_STORAGE_PROVIDER=minio` and the disposable private container: **3 passed, 0 failed, 0 skipped** across desktop, tablet, and mobile Chromium. The flow covered browser upload, API/MinIO persistence, reload, signed download, and byte equality.

## Skip classification

The skips are explicit test guards, not unstarted tests: browser MinIO evidence requires `NESTYSTAY_MINIO_E2E=true` and a backend configured for the disposable MinIO provider; the authenticated deployment smoke requires runtime `SMOKE_EMAIL`/`SMOKE_PASSWORD`; and the remaining skips are intentional viewport/project guards for mobile-only checks. No credentials were written to source, evidence, or logs.

## Interpretation

The local browser regression is green for every executed journey, including booking, badges, wellness, directories, QR, Property Manager, authorization, accessibility, responsive layouts, uploads, SEO, and Stripe test-mode UI paths. The separate MinIO browser run is also green. This does not certify real staging Brevo delivery, staging MinIO persistence/backups, deployed SHA parity, or production provider configuration.
