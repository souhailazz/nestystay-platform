# NestyStay Production Readiness Audit

Date: 2026-09-23

## Current certification refresh — 2026-09-28

The latest isolated backend certification head is `ff31270`. It adds bounded, strict input handling to the anonymous signed-storage download path, following the 256-character bound already added to legacy Property Manager QR validation. The complete current-head non-container backend suite passes **227/227** with **0 failures and 2 explicit environment skips** for MinIO and two-instance PostgreSQL when local Docker is unavailable. No current-head Docker-backed or Sonar result is claimed.

The latest isolated frontend certification branch is `20f199f`. Its complete local Vitest suite passes **157/157 across 37 files**; the preceding coverage run at `e74c701` reports **63.41% lines, 59.68% statements, 51.20% branches, and 56.27% functions**. Typecheck and production build pass; lint remains **0 errors / 107 warnings**. This exceeds the local 60% frontend line target. No new Sonar analysis is claimed for these test-only commits, so the existing Sonar findings, 80% new-code requirement, staging/provider checks, and deployed SHA parity remain open.

The production verdict remains **NOT READY for promotion**. The remaining release gates are unchanged: current-head Sonar/maintainability and backend coverage acceptance, real Brevo delivery, MinIO production configuration/durability and upload verification, staging role/IDOR verification, and exact frontend/backend deployed-SHA parity.

## Verdict

**NOT READY for production promotion.** The local application is substantially exercised, but the required release gates are not all green.

## Gate matrix

| Gate | Result | Evidence / blocker |
|---|---|---|
| Backend restore/build/tests | PASS | 202 passed, 0 failed, 0 skipped |
| Frontend typecheck/build | PASS | Both completed successfully |
| Frontend lint/audit | PASS with warnings | 0 errors, 85 warnings, 0 npm vulnerabilities |
| Browser regression | PASS with explicit skips | 224 started; 213 passed; 11 provider/privileged skips; 0 failures |
| Local MinIO I/O | PASS | Real disposable private MinIO exercised |
| Brevo delivery | BLOCKED_EXTERNAL_CONFIG | No controlled staging key/mailbox available locally |
| Frontend coverage | FAIL | 25.13% honest all-source line coverage; Sonar reports 25.8% line / 21.4% combined coverage |
| Backend coverage | FAIL | OpenCover is now imported correctly: 51.2% overall / 50.9% line / 52.1% branch; new-code coverage remains 45.6% |
| Sonar Quality Gates | FAIL | Backend and frontend gates fail; root gate has no new violation but overall legacy findings remain |
| Staging SHA parity | UNKNOWN | Existing staging returns 404 for `/api/health/version`; `/version.json` is currently SPA HTML |
| Production safety | PASS | Production not touched; no secrets committed |

## Required next actions

1. Review and merge the certification PRs through the protected workflow.
2. Deploy them to staging and verify the new metadata endpoints return the exact GitHub main SHAs.
3. Configure and test staging MinIO with private storage, persistence and backup policy.
4. Enable Brevo with a verified sender and controlled QA mailbox, then verify outbox delivery and retry behavior.
5. Raise frontend coverage honestly, raise backend new-code coverage to the gate, review Sonar bugs/vulnerabilities and reduce the new-violation count to zero.
6. Re-run staging browser role/IDOR checks after deployment.

Production must remain unchanged until these gates are green and reviewed.
