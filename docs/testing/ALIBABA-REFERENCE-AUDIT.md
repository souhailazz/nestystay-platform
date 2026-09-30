# Alibaba Reference Audit

Audit date: 2026-09-22  
Active eKYC decision: **Stripe Identity**  
Existing migrations: **not edited or deleted**

## Scope and result

Inspected current GitHub `main` trees for the root/orchestration repository
(`76dfee9a4f25f3b83a8bdb2db3a0177ccdb73301`), backend
(`e362234adf652e09d884f94172f9e4ae4f64173c`), and frontend
(`d52c73a96d1e12435af80b3ae8cdf092daa43cae`). Searches covered source,
configuration, migrations/snapshots, docs, tests, scripts, and retained evidence.

- Backend runtime source (API, application, provider wiring, and storage) has
  **no Alibaba provider reference**. Stripe Identity is the selected runtime
  provider.
- Frontend `src` has **no Alibaba reference**.
- Historical matches remain in migration designer snapshots, generated SQL,
  coverage files, archived evidence, contract/spec documents, and old
  deployment instructions. Their presence does not select an active provider.
- Current root deployment configuration still carries Alibaba settings despite
  the active Stripe Identity decision. These are configuration drift, not
  evidence of an active Alibaba adapter.

The backend repository contains an exhaustive categorized path/line inventory:
[`backend/docs/ALIBABA-REFERENCE-INVENTORY.md`](../backend/docs/ALIBABA-REFERENCE-INVENTORY.md).
That tracked-text scan recorded 246 matches across 96 files, including archived
test/coverage artifacts; it lists the exact path, line, and context. It reports
15 EF migration designer snapshots with the same historical seed data. No
historical migration was changed during this audit.

## Current active-looking deployment references

| File and lines in root `main` | Reference | Classification |
|---|---|---|
| `docker-compose.production.yml:40-49` | `ALIBABA_CLOUD_ACCESS_KEY_ID`, `ALIBABA_CLOUD_ACCESS_KEY_SECRET`, `ALIBABA_CLOUD_SECURITY_TOKEN`, `ALIBABA_EKYC_REGION`, `ALIBABA_EKYC_ENDPOINT`, `ALIBABA_EKYC_PRODUCT_CODE`, `ALIBABA_EKYC_SCENE_CODE`, `ALIBABA_EKYC_CALLBACK_URL`, `ALIBABA_EKYC_RETURN_URL`, `ALIBABA_EKYC_CALLBACK_TOKEN` mapped into `Integrations__...` | Current Compose mappings; backend runtime has no Alibaba adapter. Owner should decide whether to remove them. |
| `docker-compose.production.yml:109-118` | Repeats the same ten mappings in the second service block | Same classification; verify worker does not need obsolete variables. |
| `.env.production.example:40-50` | Legacy Alibaba names and example/default values | Template drift; do not populate for current Stripe Identity. |
| Backend `src/NestyStay.Infrastructure/Persistence/Migrations/*Designer.cs` | Historical `vault://.../ekyc/alibabacloud`, `ProviderName = "AlibabaCloud"`, and Alibaba eKYC vendor cost seed fields in 15 snapshots | Historical schema snapshot; immutable unless the owner explicitly directs a migration-history change. |

## Other reference categories

The linked inventory lists exact per-file lines for:

- old webhook/API inventories and prior completion reports;
- deployment handoff and credential checklists that still request Alibaba;
- signed-scope/contract interpretations and historical product decisions;
- design-system screen comments describing an Alibaba redirect;
- archived QA reports, generated SQL, old staging screenshots/readmes, and
  coverage XML naming the prior provider/controller;
- documentation that correctly says Stripe Identity is active but mentions
  Alibaba to explain historical context.

Some retained items are contradictory rather than merely historical—for
example old instructions to configure live Alibaba credentials or an old
`EKYC_PROVIDER=alibaba` summary. The inventory flags them for owner review; this
audit did not silently rewrite them.

## Decision and safe next step

There is **no active Alibaba eKYC provider in backend runtime**. Do not configure
Alibaba keys for the current release. Ask the deployment owner to approve a
separate cleanup PR for obsolete Compose/example mappings and stale operator
instructions. Preserve migration files and snapshots. Continue using Stripe
Identity and validate only with approved test credentials.
