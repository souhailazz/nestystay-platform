# NestyStay Property Management focused remediation audit

**Audit date:** 2026-09-22  
**Scope:** only the previously reported M5 partials and their role/scope consequences. This is not a production certification.

The later `M5-RELEASE-CANDIDATE-AUDIT.md` is the current implementation baseline: it records a 34-area local M5 matrix after the professional completion slice. The table below reconciles that claim against the focused evidence instead of leaving historical “PARTIAL” labels unexplained.

| PM area | In scope | UI | API | DB | Auth | Mobile | Result | Remaining action |
|---|---|---|---|---|---|---|---|---|
| Owner evidence/approvals | YES | COMPLETE for current local workflow | COMPLETE | COMPLETE | COMPLETE | Representative | COMPLETE locally | MinIO is required for physical evidence upload/download; staging Brevo delivery is separate |
| Reservations | YES | COMPLETE local manager workflow | COMPLETE | COMPLETE | COMPLETE | Representative | COMPLETE locally | Full cross-account fixture matrix remains in certification backlog |
| Calendar/conflicts | YES | COMPLETE local day/week/month workflow | COMPLETE | COMPLETE | COMPLETE | Representative | COMPLETE locally | External calendar feeds remain provider/configuration-gated |
| Accounting/ledger/statements | YES | COMPLETE local P0 workspace | COMPLETE | COMPLETE | COMPLETE | Representative | COMPLETE locally | Live billing/payout provider certification is external |
| Vendors | YES | COMPLETE local CRUD/completion surfaces | COMPLETE | COMPLETE | COMPLETE | Representative | COMPLETE locally | Physical compliance-document I/O waits for MinIO |
| Cleaning/readiness | YES | COMPLETE local workflow | COMPLETE | COMPLETE | COMPLETE | Representative | COMPLETE locally | Production media/storage and broad manual device certification remain |
| Inspections/remediation | YES | COMPLETE local checklist/corrective/reinspection workflow | COMPLETE | COMPLETE | COMPLETE | Representative | COMPLETE locally | Production media/storage and broad manual device certification remain |
| Documents/version/export | YES | COMPLETE local metadata/version/archive/export UI | COMPLETE | COMPLETE | COMPLETE | Scope checks pass | CONFIG_BLOCKED | Terrence must configure private MinIO, then upload/download/restart/retention tests must rerun |
| Gate/QR delivery | YES | COMPLETE manager QR/gate workspace | COMPLETE | COMPLETE | COMPLETE | Representative | COMPLETE locally | Provider delivery and mobile scanner deployment are external; dedicated Gate Guard provisioning is not required by the current M5 scope unless client changes it |
| Governance/proxy voting | YES | COMPLETE current proposal/vote/proxy workflow | COMPLETE | COMPLETE | COMPLETE | Representative | COMPLETE locally | Full cross-role owner fixture matrix remains to be executed |
| Staff invitation/RBAC | YES | COMPLETE invitation/activation controls locally | COMPLETE | COMPLETE | COMPLETE | Representative | COMPLETE locally | Focused API test confirms invite → accept → activate → scoped read/write; Brevo delivery remains external |
| Bulk operations/import/export | YES | COMPLETE supported bulk assignment/invoice/export controls | COMPLETE | COMPLETE | COMPLETE | Representative | COMPLETE locally | Generalized new bulk features are not part of this scope |
| Notifications/background automation | YES | COMPLETE in-app/outbox states | COMPLETE | COMPLETE | COMPLETE | Representative | CONFIG_BLOCKED externally | Brevo/SMS/push transport and mailbox delivery must be verified on staging |
| KPI/dashboard | YES | COMPLETE persisted PM/reporting surfaces | COMPLETE | COMPLETE | COMPLETE | Representative | COMPLETE locally | Full manual mobile KPI review remains |
| Mobile PM parity | YES | Representative responsive routes plus focused viewport matrix | Complete for tested routes | Complete | Complete | **PASS at 320/360/390/414/430/768/1024 for the focused route matrix** | PASS for the focused matrix | Broader manual certification of every PM table/modal/action remains separate |
| Non-STR PM operation | YES | COMPLETE local owner/PM workflows | COMPLETE | COMPLETE | COMPLETE | Representative | COMPLETE locally | Repeat against clean non-rental staging data after deployment |

## Focused execution evidence

- Backend focused authorization suite: **3 passed, 0 failed** for PM portfolio/staff scope; the cross-resource authorization suite adds **4 passed, 0 failed** for messages/attachments, Wellness, provider-private data, and Admin-policy endpoints.
- The protected PM route-policy sweep enumerated all **192 protected PM API actions** and rejected every enum role outside each declared role policy; no unexpected 2xx was observed.
- Owner/manager cross-portfolio cases passed for invoice, statement, document-download, owner portal, and professional-completion read/create boundaries.
- PM Staff lifecycle passed through real API calls: invitation, acceptance, manager activation, assigned-property read/create, unrelated-property denial, unrelated-owner denial, finance-capability denial, and staff-management denial.
- Existing full backend baseline was **193 passed, 0 failed, 1 legitimate MinIO skip**; after adding the focused suites the full rerun is **200 passed, 0 failed, 1 skipped**.
- The new PM mobile matrix passed at **320x568, 360x800, 390x844, 414x896, 430x932, 768x1024, and 1024x768**. It exercised real PM login/API fixture creation, invoice loading, utility reading persistence, route navigation, overflow checks, clipped-control checks, API 5xx monitoring, and console-error monitoring. The existing PM operational workflow spec also passed on the mobile project.
- Existing browser baseline remains **224 scheduled, 188 passed, 0 failed, 36 explicit skips, 0 not-run**; the focused PM matrix and PM operational workflow each passed separately. The browser run is not a substitute for the direct API matrix.

## Exact remaining classification

The historical PM partial list is therefore reconciled as follows:

- **Application-complete locally:** owners/evidence metadata, reservations, calendar/conflicts, accounting, vendors, cleaning, inspections, gate/QR, governance, staff/RBAC, bulk operations, KPIs, and non-STR operation.
- **Configuration-blocked:** physical document/attachment workflows until private MinIO is configured; real email delivery until Brevo is enabled; live billing/payout/provider delivery where applicable.
- **Still partial:** universal manual/mobile action certification across every PM table/modal/action, and the literal every-resource/every-role IDOR matrix beyond the current focused resource families.
- **Deferred/approved scope boundary:** dedicated Gate Guard provisioning is not required for the current M5 manager QR/gate scope. The domain enum and gate surfaces exist; no new Gate Guard feature is added in this pass.

## Remaining actions by owner

- **SOUHAIL:** retain the new focused API and mobile regressions; extend them only for additional current-scope resource families or PM actions identified during QA.
- **TERRENCE:** configure private MinIO with persistence/backups and provide a legitimate Admin QA account; then rerun physical upload/download/retention and Admin workflows.
- **External providers:** verify Brevo delivery and any live Stripe/Connect/payout/provider workflows.
