# NestyStay Production Readiness Audit

Date: 2026-09-23

## Current certification refresh — 2026-09-28 (latest local evidence)

The latest isolated backend certification head is `d2c8436`. It retains the booking/payment state-machine and bounded input hardening, and adds the legible Gold/Platinum founding membership terms through forward migration `20260928174138_ApplyFoundingMembershipTerms`. With disposable loopback PostgreSQL and MinIO configured, the complete unfiltered suite passes **303/303** with **0 failures and 0 skips** (Domain 6, Application 98, Infrastructure 32, API 167). The real MinIO round-trip/authorization test and PostgreSQL two-instance concurrency test pass. The last Sonar scan was processed at prior head `e215b895` and reported 0 bugs, 0 vulnerabilities, 0 hotspots, 988 code smells, 73.4% line coverage, and 20.4% duplication; a fresh exact-head scan is still required. The disposable Sonar instance has no configured acceptance conditions, so its `OK` status is evidence of processing, not production readiness.

The latest isolated frontend certification branch is `f6253bc`. Its reproducible tracked-only Vitest suite passes **129/129 across 35 files**; V8 coverage is **32.81% lines, 30.98% statements, 24.37% branches, and 32.11% functions**. Typecheck and production build remain passing; lint remains 0 errors with warnings. The last Sonar scan was processed at prior head `e47ace7` and reported 0 bugs, 0 vulnerabilities, 0 hotspots, 972 code smells, 32.7% line coverage, and 1.4% duplication; a fresh exact-head scan is still required. Ignored `src/coverage` helper tests are excluded because they are not part of the pushed branch; the requested frontend coverage gate remains open.

The production verdict remains **NOT READY for promotion**. The remaining release gates are: coverage/maintainability acceptance, real Brevo delivery, MinIO production configuration/durability/backups and upload verification, real external payment/identity verification, staging role/IDOR verification, human accessibility certification, explicit clarification of the handwritten Gold/Platinum `150+150` allocation, and frontend/backend deployed-SHA parity. The legible Gold/Platinum values are implemented and locally tested in backend `d2c8436`; the previous lifetime-fee mismatch is no longer an open implementation defect.

## Verdict

**NOT READY for production promotion.** The local application is substantially exercised, but the required release gates are not all green.

## Gate matrix

| Gate | Result | Evidence / blocker |
|---|---|---|
| Backend restore/build/tests | PASS | 229 passed, 0 failed, 0 skipped at current head with disposable PostgreSQL/MinIO configured |
| Frontend typecheck/build | PASS | Both completed successfully |
| Frontend lint/audit | PASS with warnings | 0 errors, 107 warnings, 0 npm vulnerabilities |
| Browser regression | PASS with explicit skips | Current head 20f199f: 224 started; 213 passed; 11 explicit skips; 0 failures; 0 did-not-run |
| Local MinIO I/O | PASS | Real disposable private MinIO exercised |
| Brevo delivery | BLOCKED_EXTERNAL_CONFIG | No controlled staging key/mailbox available locally |
| Frontend coverage | FAIL / OPEN | Tracked-test V8 reports 32.81% lines; current Sonar line coverage is 32.7%, below the requested 60% threshold |
| Backend coverage | FAIL / OPEN | Current Sonar line coverage is 73.4%, below the requested 80% target; 988 code smells remain |
| Sonar Quality Gates | NOT CERTIFIED | Disposable local instance processed all three scans but has no configured acceptance conditions; bugs/vulnerabilities/hotspots are 0, coverage/maintainability gates remain open |
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
