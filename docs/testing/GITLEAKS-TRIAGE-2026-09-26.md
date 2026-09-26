# Gitleaks Candidate Triage — 2026-09-26

The scans were run against the current certification branches with values redacted. No secret value is included here.

## Current tracked-source results

| Repository | Scan | Findings | Result |
|---|---|---:|---|
| Frontend | tracked Git history | 0 | PASS |
| Backend | tracked Git history | 7 | All classified below; no active production credential found |
| Root | tracked current release archive | 1 source/config finding plus 2,237 generated-evidence matches | Source finding classified below; generated evidence is a false positive class |

The root Git-history scan is not used as the release candidate count because it includes archived testing evidence and repeated Sonar diagnostic strings. Those findings are not credentials and are not deployed runtime configuration.

## Backend findings

| File | Line | Rule | Classification | Rotation |
|---|---:|---|---|---|
| `.env.production.example` | 32–33 | `generic-api-key` | TEST_VALUE / placeholder Stripe webhook value | NO |
| `.env.production.example` | 32–33 | `generic-api-key` | Same historical template value in a second commit | NO |
| `src/NestyStay.Api/appsettings.Development.json` | 6–7 | `generic-api-key` | HISTORICAL development connection/security values; not present in current file | NO for production; rotate if ever reused outside development |
| `src/NestyStay.Api/appsettings.Development.json` | 7 | `generic-api-key` | HISTORICAL development admin/operator hash values; not present in current file | NO for production; rotate if ever reused outside development |
| `src/NestyStay.Api/appsettings.Development.json` | 7 | `generic-api-key` | Same historical development values in an earlier commit | NO for production; rotate if ever reused outside development |
| `.env.example` | 4 | `generic-api-key` | TEST_VALUE / database placeholder | NO |
| `.env.example` | 6 | `generic-api-key` | TEST_VALUE / `<postgres-password>` placeholder | NO |

The current backend files contain placeholders or development-only configuration; no active Stripe, database, MinIO, Brevo or admin credential was found in the tracked current source. Existing Git history was not rewritten.

## Root findings

| File | Line | Rule | Classification | Rotation |
|---|---:|---|---|---|
| `.env.production.example` | 43–44 | `generic-api-key` | HISTORICAL/placeholder Alibaba environment entry; empty security-token value and example provider configuration | NO |

The Alibaba line is configuration history/template material, not an active runtime provider or credential. It is preserved for deployment-owner review as required; it was not deleted or rewritten.

## Generated evidence findings

The tracked Sonar issue exports contain repeated diagnostic text that matches `generic-api-key`. They are generated evidence, not secret material, and have no runtime effect. They require no rotation. Future scans should exclude generated evidence when measuring source-secret exposure.

## Verdict

**No active credential requiring rotation was identified by this triage.** The only remaining action is normal repository hygiene: keep template values clearly marked as placeholders, keep generated evidence out of secret scans where supported, and never commit real environment values.
