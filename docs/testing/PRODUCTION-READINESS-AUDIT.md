# NestyStay Production Readiness Audit

Audit date: 2026-09-22  
Verdict: **NOT PRODUCTION READY / NOT READY FOR FULL PROFESSIONAL QA**

This is based on current repository `main` commits, completed automated checks,
partial local browser execution, current public HTTP probes, and deployment
workflow outcomes. A healthy root page or `/api/health/ready` response is not
proof that the latest frontend/backend pair is deployed or that storage, email,
payments, uploads, and role-isolated user journeys work end to end.

## Continuation remediation pass

The invalid synthetic browser sessions were removed from the executable
non-admin journeys. The repaired full local matrix completed **224 scheduled,
188 passed, 0 failed, 36 skipped, 0 did not run**. The skips are explicit
configuration/provider gates, chiefly Admin browser access, MinIO-dependent
physical I/O, and external/deployment-only checks. This improves local
certification but does not change the production verdict: MinIO, Brevo,
deployment SHA parity, SonarQube, and full role/IDOR certification remain
unverified or externally blocked.

## Current release version matrix

| Component | GitHub `main` | Latest deployment evidence | Conclusion |
|---|---|---|---|
| Root | `76dfee9a4f25f3b83a8bdb2db3a0177ccdb73301` | Main deploy run failed during SSH configuration; no deployed SHA verified | Unknown |
| Backend | `e362234adf652e09d884f94172f9e4ae4f64173c` | Main run reports backend release switch/restart and public readiness check successful on the staging host; a separate systemd-host archive-copy job failed | Staging host likely at main run SHA; verify directly before certification |
| Frontend | `d52c73a96d1e12435af80b3ae8cdf092daa43cae` | Main run failed at release-archive copy; switch/reload skipped | Latest frontend SHA not verified as deployed |

Current direct HTTP probes returned 200 for `https://staging.nestystay.net/`
and `https://staging.nestystay.net/api/health/ready`. The frontend deployment
failure means the 200 page may be an older bundle. Post-merge workflow details:

- [Root deployment run 35444437590](https://github.com/souhailazz/nestystay-platform/actions/runs/35444437590) — SSH setup failed.
- [Backend deployment run 35680244324](https://github.com/NestyStayJamaica/NESTY-STAY_Backend/actions/runs/35680244324) — application verification and staging host health succeeded; separate host copy failed.
- [Frontend deployment run 35680319281](https://github.com/NestyStayJamaica/NESTY-STAY_Frontend/actions/runs/35680319281) — typecheck/tests/build succeeded; SSH copy failed and release activation did not run.

Root PR #9 and backend PR #6 remain open. Backend PR #5 and frontend PR #3
are merged. Audit follow-up PRs frontend #4 and backend #7 are also open; no
protected branch was bypassed.

## Build and automated quality

| Check | Result |
|---|---|
| Backend main full solution | 194 passed, 0 failed, 0 skipped; includes restore/build. |
| Backend test-harness PR #7 | Full solution: 193 passed, 0 failed, 1 skipped. The MinIO I/O test is now explicitly skipped without `MINIO_TEST_ENDPOINT`; real MinIO I/O remains unverified. |
| Backend PR #7 CI | Restore/build/test passed. Staging/systemd deploy jobs did not run for the feature PR. GitHub reported obsolete test-helper and Node.js 20 action deprecation warnings. |
| Frontend unit tests | 56 passed on feature PR #4; includes the owner-invitation false-success regression test. |
| Frontend PR #4 CI | Required typecheck/test/build passed; deployment job skipped for the unmerged feature branch. |
| Frontend typecheck/build | Pass. |
| Frontend lint | 0 errors; 84 warnings. |
| npm audit | 0 known vulnerabilities (377 dependency records). |
| NuGet vulnerability audit | No vulnerable packages reported in nine projects. |
| Playwright | Prior run was incomplete; continuation run completed 224 scheduled, 188 passed, 0 failed, 36 skipped, 0 did not run. Admin/provider skips remain explicit gates. |
| Local database | Repository startup reports current migrations/no migration pending; local liveness/readiness 200. Staging migration status is not independently re-read here. |

## Integration readiness

| Area | Code status | Local test status | Staging/live status | Release gate |
|---|---|---|---|---|
| Stripe Identity | Stripe Identity provider and webhook handling are present; no active Alibaba runtime adapter. | Deterministic/application backend tests pass. | No live Stripe Identity session was executed in this audit. | Confirm staging/test mode and run one signed test session/event. |
| Stripe payments | Payment flows and webhook/idempotency tests are in backend suite. | Automated backend coverage; browser suite did not reach final payment acceptance. | No live transaction attempted; current provider mode was not read from server. | Verify safe test-mode keys, payment/capture/refund and duplicate event on staging. |
| MinIO/object storage | Provider, DI selection, production validation, private MinIO service and env template exist in backend `main`. Test PR #7 fixes the absent-endpoint false pass. | No local MinIO service; current dev config selects local storage. Integration test now visibly skips without its endpoint. | No MinIO upload/download/restart/authorization cycle independently verified. | Review/merge PR #7; configure private MinIO with least-privilege credentials; verify signed/authenticated upload and download, restart persistence, unauthorized denial, backup/restore. |
| Brevo | Email provider/outbox/retry implementation and deterministic email tests exist. | Backend suite passes; local email delivery is not a real Brevo send. | Prior owner update said the API key existed but transport was disabled; current server config was not inspected. | Enable via server env, verified sender/domain, restart, verify send and retry/dead-letter behavior. |
| PostgreSQL/migrations | EF migrations and migration-check safeguard are present on backend `main`. | Local DB reported up to date. | Public readiness returned 200; no direct production schema inspection. | Confirm staging migration check on the running SHA and backup before production migration. |
| Notifications/email | Outbox and email retry code have automated coverage. | Local generation/worker code covered. | Actual mailbox delivery/deep-link role lifecycle not revalidated. | Verify Brevo delivery and user-facing notification links. |
| Authentication/roles | Auth/session/RBAC code and tests exist. | Backend tests and frontend unit suite pass; canonical route browser test fails because synthetic Admin has no real session. | Full QA role matrix not repeated. | Use secure seeded/bootstrapped QA roles and complete route/API authorization matrix. |

## Release blockers and ownership

**Application / PR work:**

- Repair the Admin route test fixture; do not weaken server authorization or
  rely on a browser-stored role claim.
- Review/merge frontend PR #4 (`codex/fix-pm-owner-invitation-feedback`); it
  removes the false queued-resend claim. A real resend lifecycle remains
  unimplemented and should be separately scoped if required.
- Review/merge backend PR #7, which makes the MinIO test explicitly skipped
  when unconfigured; run the real MinIO test job before representing uploads
  as verified.
- Finish and archive the full Playwright run from an isolated checkout with all
  evidence outputs redirected.
- Reconcile the root repository from a clean main-based worktree; the current
  root branch has no merge base with root `origin/main`; it had 260 pre-existing
  changed entries at audit start and 267 at final check. Preserve them; the
  three audit reports and browser-test evidence are uncommitted.

**Deployment owner / staging:**

- Repair required SSH configuration/host/path for root, frontend nginx, and the
  separate backend deployment target; rerun workflows and record deployed SHAs.
- Confirm MinIO endpoint, bucket, app credentials, TLS/private access, durable
  volume, backup schedule and restore drill. Do not use server-local filesystem
  storage as a production substitute for the agreed object-store architecture.
- Confirm Brevo is enabled, sender verified, DNS/authentication configured, and
  a real mailbox receives verification, reset, booking, Wellness, and PM mail.
- Confirm Stripe test/live mode before any transaction; validate signed webhook
  replay/idempotency and Identity event mapping safely.
- Verify staging migration status, monitoring/alerts, restore procedures,
  reverse-proxy limits/timeouts, and deployed frontend/backend compatibility.

**External/human verification:**

- Real Stripe Identity and payments require approved provider configuration;
  no production money was moved in this audit.
- Manual screen-reader/keyboard certification and independent penetration
  testing remain outstanding. Automated representative tests do not replace
  human accessibility or security assessment.

## Decision

The code compiles and its backend/frontend unit checks are green, but release
certification is blocked by incomplete browser execution, unverified production
MinIO and email flows, missing deployed-frontend SHA confirmation, failed SSH
deployment jobs, incomplete role isolation/browser testing, and open PRs. Do
not describe the system as production-ready or fully end-to-end verified until
those gates pass on the exact deployed commits.
