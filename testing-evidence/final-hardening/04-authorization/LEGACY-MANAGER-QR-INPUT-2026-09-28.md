# Legacy Property Manager QR Input Hardening

Date: 2026-09-28
Backend commit: `732c71058a37c3d7a7691e898182da1f33382c5c`
Branch: `codex/final-release-certification`

## Change

The legacy Property Manager QR validation endpoint backed by `EfPropertyManagerStore.ValidateQrAsync` now rejects blank or oversized QR tokens before hashing or performing a database lookup. The maximum accepted token length is 256 characters, matching the bound already enforced by the primary QR access path. Malformed input receives the existing safe invalid-result shape and does not reveal lookup details.

## Regression evidence

- Focused oversized-token regression: **1 passed, 0 failed**.
- Full `PropertyManagerEndpointTests`: **11 passed, 0 failed**.
- Complete current-head non-container backend solution: **226 passed, 0 failed, 2 explicit environment skips**.
- The two explicit skips are the MinIO integration test and the PostgreSQL two-instance concurrency test when disposable Docker services are unavailable.

## Scope and limitations

This is a behavior-preserving input-validation hardening change. It does not modify migrations, provider configuration, staging, production, or protected `main`. The current-head Docker-backed MinIO/PostgreSQL rerun and a new current-head Sonar upload remain blocked by the local Docker environment; this evidence does not claim either one.
