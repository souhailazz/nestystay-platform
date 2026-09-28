# Alibaba Reference Audit

Date: 2026-09-23

## Runtime conclusion

- Active backend Alibaba provider: **NO**. The active identity implementation is Stripe Identity.
- Active frontend Alibaba code: **NO**.
- Historical migration snapshots and older documentation/design evidence still contain Alibaba strings. Existing migration files were not edited.

## Concrete references found

The complete prior inventory remains at [`docs/ALIBABA-REFERENCE-INVENTORY.md`](../ALIBABA-REFERENCE-INVENTORY.md). The material categories and representative paths/lines are:

| Path | Lines / content | Classification |
|---|---|---|
| `backend/src/NestyStay.Infrastructure/Persistence/Migrations/*Designer.cs` | Repeated model-snapshot values around lines 2521, 2525, 3821-3822: `vault://.../alibabacloud`, `ProviderName = "AlibabaCloud"`, and legacy vendor-cost seed labels | Immutable historical migration snapshots; do not edit |
| `.env.production.example` | 40-50: legacy Alibaba environment placeholders | Configuration documentation requiring owner review before production |
| `docker-compose.production.yml` | 40-49 and 109-118: legacy Alibaba environment mappings | Deployment configuration requiring owner review; not active provider selection |
| `docs/testing/M1-M2-TRACEABILITY.md` | line 30: real Alibaba validation described as blocked | Historical/test documentation |
| `docs/testing/PROVIDER-CERTIFICATION-MATRIX.md` | line 19: Stripe Identity is active and Alibaba is historical | Current decision record |
| `design-system/design_handoff_nestystay_ui/PLATFORM_SPEC.md` | line 27: external Alibaba redirect in an older design handoff | Historical design artifact |
| `testing-evidence/client-demo/README.md` | line 62: deterministic Alibaba demo path | Stale evidence requiring review; not runtime proof |

No secrets are reproduced in this audit. No migration or provider code was deleted in this pass. Terrence should decide which deployment/documentation references to remove or retain as history.
