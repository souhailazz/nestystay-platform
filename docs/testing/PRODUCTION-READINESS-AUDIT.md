# NestyStay Production Readiness Audit

Date: 2026-09-23

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
