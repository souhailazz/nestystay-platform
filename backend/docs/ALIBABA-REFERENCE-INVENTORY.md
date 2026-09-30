# Alibaba reference inventory

Audit date: 2026-09-21

This inventory records tracked text-file matches for `Alibaba`, `AlibabaCloud`,
and `Alibaba-Cloud` in the root, backend, and frontend repositories. The scan
included tracked artifacts, archived testing evidence, and coverage reports;
it excluded Git metadata, build output, dependency folders, and `TestResults`.
The inventory file itself is excluded from its own count. Text scanning does
not OCR binary screenshots/media. No migration file was edited or deleted for
this audit.

## Summary

- Backend runtime source/config (`backend/src`, excluding migrations): no Alibaba match.
- Frontend runtime source (`frontend/src`): no Alibaba match found in the current audit scope.
- Root/orchestration configuration: active-looking Alibaba environment mappings remain in `docker-compose.production.yml` and must be reviewed before production.
- Migration designer snapshots: 15 immutable snapshots retain the historical provider/cost values listed below.
- The tracked-repository scan found 246 text matches across 96 files (including archived test and coverage output). A broader `--no-ignore` scan also found 57 matches in 10 ignored, untracked local scratch files; those files are not part of the repositories and are not included below. Documentation, design specifications, reports, and evidence contain a mixture of historical facts and stale/contradictory claims; they are listed so the deployment owner can decide what may be cleaned up.

## Root orchestration/configuration references

`docker-compose.production.yml` contains the following mappings for both the
backend service block and the worker/service block:

- `docker-compose.production.yml:40-49` — maps `ALIBABA_CLOUD_ACCESS_KEY_ID`, `ALIBABA_CLOUD_ACCESS_KEY_SECRET`, `ALIBABA_CLOUD_SECURITY_TOKEN`, `ALIBABA_EKYC_REGION`, `ALIBABA_EKYC_ENDPOINT`, `ALIBABA_EKYC_PRODUCT_CODE`, `ALIBABA_EKYC_SCENE_CODE`, `ALIBABA_EKYC_CALLBACK_URL`, `ALIBABA_EKYC_RETURN_URL`, and `ALIBABA_EKYC_CALLBACK_TOKEN` into `Integrations__...` settings.
- `docker-compose.production.yml:103-112` — repeats the same ten Alibaba environment mappings for the second service block.

These are current orchestration-file entries, not migration history. The
application owner should decide whether they are removed or retained for
historical compatibility before production deployment.

## Immutable migration snapshots

Each file below contains the same historical seed/config snapshot at these
exact lines:

- `2521`: `EncryptedConfigReference = "vault://nestystay/ekyc/alibabacloud"`
- `2525`: `ProviderName = "AlibabaCloud"`
- `3821`: `Key = "alibaba-ekyc-vendor-cost"`
- `3822`: `Label = "alibaba ekyc vendor cost"`

Files:

- `src/NestyStay.Infrastructure/Persistence/Migrations/20260909235056_AddPropertyManagerP0Model.Designer.cs`
- `src/NestyStay.Infrastructure/Persistence/Migrations/20260910002207_AddP0PayoutCreatedByLink.Designer.cs`
- `src/NestyStay.Infrastructure/Persistence/Migrations/20260910005142_AddP0OwnerBillingMetadata.Designer.cs`
- `src/NestyStay.Infrastructure/Persistence/Migrations/20260910012744_AddP0StatementIdempotency.Designer.cs`
- `src/NestyStay.Infrastructure/Persistence/Migrations/20260910014625_AddP0ReversalUniqueness.Designer.cs`
- `src/NestyStay.Infrastructure/Persistence/Migrations/20260910095406_AddProfessionalPropertyManagerOperations.Designer.cs`
- `src/NestyStay.Infrastructure/Persistence/Migrations/20260910103500_AddProfessionalOperationalConcurrency.Designer.cs`
- `src/NestyStay.Infrastructure/Persistence/Migrations/20260910111240_AddAssetRegisterDetails.Designer.cs`
- `src/NestyStay.Infrastructure/Persistence/Migrations/20260910112305_AddInspectionCorrectiveWorkOrder.Designer.cs`
- `src/NestyStay.Infrastructure/Persistence/Migrations/20260910125540_AddM5ReleaseOperationalControls.Designer.cs`
- `src/NestyStay.Infrastructure/Persistence/Migrations/20260910130734_AddManagerAuditScope.Designer.cs`
- `src/NestyStay.Infrastructure/Persistence/Migrations/20260910131316_AddReservationCancellationIdempotency.Designer.cs`
- `src/NestyStay.Infrastructure/Persistence/Migrations/20260910232148_AddM5ReservationFinancialCorrectiveLifecycles.Designer.cs`
- `src/NestyStay.Infrastructure/Persistence/Migrations/20260911022938_AddPmsProfessionalCompletion.Designer.cs`
- `src/NestyStay.Infrastructure/Persistence/Migrations/20260911025800_AddUtilityChargeCurrencyV2.Designer.cs`

## Documentation, design, reports, and acceptance evidence

The following are non-migration matches. They are grouped by purpose; the
line numbers are the exact matching lines from the audit.

### Product/deployment/API documents

- `M1_M2_API_INVENTORY.md:45` — lists `POST /webhooks/alibaba-ekyc`.
- `M1_M2_API_INVENTORY.md:144` — says production startup guards require Alibaba Cloud eKYC among external integrations.
- `M1_M2_FULL_COMPLETION_REPORT.md:293` — instructs configuring real Alibaba Cloud eKYC credentials/provider signatures.
- `README.md:272` — checklist item for real Alibaba Cloud eKYC credentials.
- `docs/UPGRADE-31-STATUS.md:15,50` — live Alibaba callback/document-retention references.
- `docs/deployment/PRODUCTION-CREDENTIAL-REQUIREMENTS.md:22` — says Alibaba Mail is not used; `:25` — says Stripe Identity is selected and Alibaba is not active.
- `docs/deployment/ACCOUNT-SETUP-GUIDE.md:23` — says Alibaba Mail is not used.
- `docs/deployment/STAGING-CLOUD-AI-SETUP.md:60` — says Alibaba remains an old reference.
- `docs/deployment/SPLIT-REPOSITORY-HANDOFF.md:47` — includes Alibaba eKYC in the integration handoff list.
- `docs/delivery/CLIENT-VERIFICATION-GUIDE.md:10` — says live Stripe and Alibaba checks require client credentials.
- `docs/product/PROPERTY-MANAGER-SCOPE.md:18` — refers to Alibaba validation as separate from the application adapter.
- `docs/product/PROPERTY-MANAGER-NEXT-LEVEL-AUDIT.md:40,293` — lists live Alibaba as an external production gate.
- `docs/handover/LOW-COST-OPERATING-PLAN.md:8,14` — mentions Alibaba Mail/eKYC in the operating plan.
- `docs/handover/CLIENT-OWNERSHIP-HANDOVER.md:20` — requests Alibaba production account/callback/signature configuration.
- `docs/handover/CLIENT-ACCOUNT-CHECKLIST.md:14` — lists Alibaba eKYC client credentials.

### Reports and integration notes

- `reports/SECURITY-AUDIT.md:21` — describes incomplete Alibaba/Stripe production configuration.
- `reports/LOW-COST-PRODUCTION-METRICS.json:14` — records `realAlibabaEkyc` as `BLOCKED_CREDENTIAL`.
- `reports/LOW-COST-PRODUCTION-IMPLEMENTATION.md:13` — records Alibaba eKYC as `PRESERVED`; `:22` — says Alibaba mail/SMTP is absent while eKYC is preserved; `:26` — lists live Alibaba credentials as external.
- `reports/FINAL-SCORECARD.md:14` — records the real Alibaba provider as blocked.
- `reports/FINAL-METRICS.json:20` — records `realAlibabaValidated: false`.
- `reports/FINAL-HARDENING-REPORT.md:39` — says real Alibaba calls were not executed.
- `docs/security/FINAL-SECURITY-AUDIT.md:79,81` — says active runtime Alibaba matches are clean and describes the remaining references as historical documentation/migration records.
- `docs/integrations/EMAIL-PROVIDER-MIGRATION.md:7` — says Alibaba Mail/SMTP is absent and Alibaba eKYC is preserved.
- `docs/integrations/BUSINESS-EMAIL-OPTIONS.md:13` — distinguishes Alibaba Mail/SMTP from preserved Alibaba Cloud eKYC.

### Testing/traceability/acceptance documents

- `docs/testing/31-UPGRADE-IMPLEMENTATION-MATRIX.md:13,46` — references Alibaba credentials/callbacks as a live-provider gate.
- `docs/testing/52-AREA-IMPLEMENTATION-MATRIX.md:18,68` — states Alibaba is not active runtime and historical references remain.
- `docs/testing/INSURAGUEST-CONTRACT-ACCEPTANCE.md:60` — says Alibaba is not used for that requirement and Stripe Identity is the path.
- `docs/testing/CONTRACT-VS-CURRENT-DECISIONS.md:17,19` — records Alibaba eKYC as superseded by Stripe Identity and Alibaba hosting as an implementation variation.
- `docs/testing/CROSS-LAYER-RUNTIME-FAILURE-AUDIT.md:73` — states Stripe Identity is active and Alibaba is not active.
- `docs/testing/STAGING-INTEGRATION-READINESS.md:21,23` — states Stripe Identity is the supported selector and Alibaba is not active.
- `docs/testing/M1-M5-FINAL-GAP-AUDIT.md:31,140,165` — states the active backend has no Alibaba provider wiring and migration matches are historical.
- `docs/testing/M1-M4-TRACEABILITY.md:36` — lists real Alibaba validation as blocked.
- `docs/testing/M1-M4-ACCEPTANCE-CHECKLIST.md:56` — lists real Alibaba sandbox validation as blocked.
- `docs/testing/M1-M2-TRACEABILITY.md:30` — distinguishes deterministic application coverage from real Alibaba validation.
- `docs/testing/M1-M2-CONTRACT-INTERPRETATION.md:20,22,29,51` — reproduces signed-contract references to Alibaba and records the later Stripe Identity decision.
- `docs/testing/NESTYSTAY-REMAINING-DELIVERY-CHECKLIST.md:23` — lists Alibaba/eKYC credentials as a remaining checklist item.
- `docs/testing/M5-PROFESSIONAL-PMS-COMPLETION.md:5` — lists live Alibaba as a separate production gate.
- `docs/testing/M5-RELEASE-CANDIDATE-AUDIT.md:3` — states Alibaba is not active and migration history is retained.
- `docs/testing/PROVIDER-CERTIFICATION-MATRIX.md:19` — states Stripe Identity is the only active provider and Alibaba is historical.
- `docs/testing/SECURITY-FINAL-CERTIFICATION.md:7` — records the active Alibaba scan as clean except for historical snapshots.
- `docs/testing/SIGNED-CONTRACT-TRACEABILITY.md:79,89` — records legacy Alibaba hosting/eKYC references and the decision not to restore Alibaba.
- `docs/testing/STAGING-FULL-ACCEPTANCE.md:11,50-52,72,141-152,229,288` — contains old deployed acceptance evidence, including an Alibaba quote-text defect and remediation notes.

### Design specifications

- `design-system/design_handoff_nestystay_ui/PLATFORM_SPEC.md:27` — describes eKYC as an external Alibaba redirect.
- `design-system/design_handoff_nestystay_ui/screens/BOOK-07.dc.html:19` — comment describes an external Alibaba URL for booking verification.
- `design-system/design_handoff_nestystay_ui/screens/BOOK-03.dc.html:19` — comment describes an external Alibaba eKYC URL.
- `design-system/design_handoff_nestystay_ui/screens/PM-VERIFY.dc.html:19` — vision-screen comment describes an Alibaba redirect.

### Other tracked QA/security artifacts

- `artifacts/final_milestone_1_2_fullstack_qa_proof.md:359` — historical full-stack QA proof reference.
- `artifacts/m1_m2_spec_inventory.json:327` — contract/spec inventory reference.
- `artifacts/milestone_1_2_backend_qa_proof.md:7,42,128,167` — historical backend QA proof references.
- `artifacts/milestone_1_2_persistence_admin_security_proof.md:120` — persistence/security proof reference.
- `artifacts/milestone_1_2_persistence_migration.sql:1273,1324` — generated persistence SQL contains old provider/config seed values; it was not changed.
- `artifacts/milestone_3_wellness_proof.md:246` — historical milestone proof reference.
- `artifacts/production-readiness/secrets-rotation-candidates.md:26` — Alibaba-related secret/config rotation candidate reference; values are not reproduced here.

### Archived testing evidence and generated coverage

These are retained evidence artifacts, not current runtime source. Their
presence explains why a broad repository search finds historic Alibaba
implementation names even though current backend runtime source and frontend
`src` have no Alibaba match.

- `testing-evidence/client-demo/DEMO-DATA.md:13` — says real Alibaba validation is not claimed.
- `testing-evidence/client-demo/README.md:62` — describes a deterministic local Alibaba eKYC adapter and approval/rejection demo path; this conflicts with the current Stripe Identity direction and should be treated as stale until reviewed.
- `testing-evidence/final-enhancement-audit/FINAL-ENHANCEMENT-REPORT.md:51` — lists live Alibaba/eKYC credentials as an external gate.
- `testing-evidence/final-hardening/06-frontend/FRONTEND-COVERAGE-MATRIX.md:11,30` — records Alibaba external validation as pending.
- `testing-evidence/final-hardening/HARDENING-CONTINUATION.md:50` — says Alibaba eKYC remains unchanged.
- `testing-evidence/final-hardening/reports/FINAL-HARDENING-M5-ADDENDUM.md:58` — lists Alibaba as a production-only blocker.
- `testing-evidence/final-hardening/reports/FINAL-HARDENING-REPORT.md:25` — says real Alibaba calls were not executed.
- `testing-evidence/final-hardening/reports/FINAL-METRICS.json:20` — records `realAlibabaValidated: false`.
- `testing-evidence/final-hardening/reports/FINAL-SCORECARD.md:14` — records real Alibaba provider validation as blocked.
- `testing-evidence/final-hardening/reports/FINAL-TEST-REPORT.md:13` — says Alibaba certification is blocked on provider credentials/domains.
- `testing-evidence/final-hardening/reports/FRONTEND-COVERAGE-MATRIX.md:11,30` — records Alibaba external validation as pending.
- `testing-evidence/final-hardening/reports/LOW-COST-PRODUCTION-IMPLEMENTATION.md:5` — says real Alibaba credentials remain a production gate.
- `testing-evidence/final-hardening/reports/SECURITY-AUDIT.md:20` — describes incomplete Alibaba/Stripe production configuration.
- `testing-evidence/final-hardening/reports/ZERO-CREDENTIAL-PRODUCTION-PREP-ADDENDUM.md:18` — contains an old `EKYC_PROVIDER=alibaba` configuration summary.
- `testing-evidence/milestones-1-4/FINAL-SYSTEM-REPORT.md:16,46,84,88` — lists real Alibaba provider validation as blocked and as a remaining external deployment gate.
- `testing-evidence/milestones-1-4/backend/m1-m4-final-api.trx:2294` — archived test stack trace names the old `ReceiveAlibabaEkyc` endpoint/controller.
- `testing-evidence/milestones-1-4/contract/signed-agreement-verification.txt:22` — signed-scope evidence refers to Alibaba eKYC.
- `testing-evidence/milestones-1-5/FINAL-DELIVERY-REPORT.md:33` — lists real Alibaba provider validation as blocked.
- `testing-evidence/staging-full-acceptance/README.md:18` — refers to old staging Alibaba quote text/evidence.

Generated coverage XML also contains symbols from earlier builds. Exact
artifact paths and matching line numbers:

- `testing-evidence/final-hardening/15-coverage/backend-all/2868dd15-1a37-4a5e-81b8-bb0fb14f9db7/coverage.cobertura.xml:1781,7091,31931`
- `testing-evidence/final-hardening/15-coverage/backend-all/66fa3b94-3947-468b-8b1c-25e58ec3b221/coverage.cobertura.xml:21329`
- `testing-evidence/final-hardening/15-coverage/backend-all/968ca715-ec1b-45b5-add5-5011cbf80e01/coverage.cobertura.xml:1781,7091,31931`
- `testing-evidence/final-hardening/15-coverage/backend-all/fe56aea9-569e-4736-be7c-e7cc0fc7c752/coverage.cobertura.xml:21329`
- `testing-evidence/final-hardening/15-coverage/backend-final/NestyStay.Api.Tests/25a8bb3a-64e8-47b3-bb35-b9eb718bd88c/coverage.cobertura.xml:1781,7091,31931`
- `testing-evidence/final-hardening/15-coverage/backend-final/NestyStay.Infrastructure.Tests/7f98392f-8d2e-452c-9d62-8ec810d3d365/coverage.cobertura.xml:21329`
- `testing-evidence/final-hardening/15-coverage/backend/39c3cb54-824e-4c1e-94f3-c69d9e43f2ce/coverage.cobertura.xml:20279`
- `testing-evidence/final-hardening/15-coverage/backend/bbbf8e10-ff07-4d84-a18e-1336c3ede90c/coverage.cobertura.xml:1781,7091,31931`
- `testing-evidence/final-hardening/15-coverage/backend/fff90431-9e01-4303-a49e-09872d4d7468/coverage.cobertura.xml:1439,6343,29655`

## Interpretation for review

The backend branch restored in this PR still has Stripe Identity as the active
identity provider and contains no Alibaba provider implementation in backend
runtime source. The root production Compose file does, however, still pass
Alibaba settings into containers, and several older docs/design/evidence files
describe Alibaba. Terrence should decide whether the root Compose entries and
non-immutable historical documents are to be removed, corrected, or retained.
Immutable migration designer snapshots were intentionally left untouched.
