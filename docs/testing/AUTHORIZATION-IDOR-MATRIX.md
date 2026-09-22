# NestyStay role, authorization and IDOR certification

**Focused remediation pass:** 2026-09-22  
**Rule:** all results below are server/API results. Frontend route hiding and localStorage role metadata are not authorization evidence.

## Actual roles

The authoritative enum is `backend/src/NestyStay.Domain/Enums.cs`: `Guest`, `Host`, `Owner`, `PropertyManager`, `AssociationExecutive`, `Tenant`, `ServiceProvider`, `LocalBusiness`, `Officer`, `GateGuard`, and `Admin`.

`PM Staff` is not a separate `UserRole` enum value. It is an active `MilestoneP0StaffMembership` with a manager, role (`OPERATIONS`, `FINANCE`, or `APPROVER`), owner/property scopes, capabilities, approval limit, status, and audit history. The staff member must accept the invitation before activation.

## Protected API inventory

The five PM controllers expose **193 HTTP actions**. One is the intentionally anonymous QR validation action; the other **192 actions are protected**. The focused test enumerates all 192 protected actions through ASP.NET API Explorer and calls every declared-role endpoint with every enum role outside its declared policy. No out-of-policy role received a 2xx response.

| API family | Protected policy | Ownership/scope rule | Executed evidence |
|---|---|---|---|
| `/api/property-manager/*` | Endpoint-specific `PropertyManager`, `Owner`, `Admin`, or signed-in checks | Manager portfolio, owner relationship, property assignment, or owner-self rule | Full declared-role denial sweep; focused owner/manager cross-portfolio test |
| `/api/property-manager/p0/*` | Signed-in; business authorization in the store | Manager/admin; active staff membership and owner/property scopes; owner-self reads/decisions where supported | Invitation → accept → activate → scoped read/mutation and cross-scope denial |
| `/api/property-manager/professional/*` | `PropertyManager` or `Admin` | Manager portfolio only | Declared-role denial sweep and existing manager cross-portfolio tests |
| `/api/property-manager/professional-completion/{area}` | Signed-in; active manager/staff context | Manager portfolio plus staff owner/property scope; finance areas require finance capability | Assigned staff read/create, unrelated property/owner and finance denial |
| `/api/property-manager/owner/*` | `Owner` | Authenticated owner’s own managed properties | Existing owner block tests and declared-role denial sweep |
| `/api/property-manager/qr/validate` | Anonymous by design | Token, expiry, revocation, property binding, and signed-in context | Existing QR tests; intentionally excluded from role sweep |

## Role/API matrix

| Route/API group | Resource | Guest | Host | Owner | PM | PM Staff | Wellness | Provider | Local business | Admin | Ownership rule | Expected result |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `/property-manager/dashboard` and manager mutations | PM portfolio | DENY | DENY | DENY | ALLOW | DENY unless explicit completion scope | DENY | DENY | DENY | ALLOW | PORTFOLIO_ONLY | 401/403 outside policy |
| `/property-manager/invoices/{id}`, payments, statements | Financial record | DENY | DENY | OWN_RESOURCE_ONLY | PORTFOLIO_ONLY | ASSIGNED_ONLY where staff API supports it | DENY | DENY | DENY | ALLOW | owner or manager portfolio | 200 only for owned/managed resource |
| `/property-manager/documents` and downloads | Private document | DENY | DENY | ASSIGNED_ONLY | PORTFOLIO_ONLY | ASSIGNED_ONLY | DENY | DENY | DENY | ALLOW | owner/property/manager scope | 401/403/404 outside scope |
| `/property-manager/professional/*` | Reservation/calendar/operations | DENY | DENY | DENY | PORTFOLIO_ONLY | NOT_APPLICABLE to manager-only legacy route | DENY | DENY | DENY | ALLOW | managed property | 200 only for managed portfolio |
| `/property-manager/professional-completion/*` | Completion record | DENY | DENY | DENY | PORTFOLIO_ONLY | ASSIGNED_ONLY; finance capability for finance areas | DENY | DENY | DENY | ALLOW | manager plus owner/property scope | 200 only inside scope |
| `/property-manager/p0/members` | Staff membership | DENY | DENY | DENY | MANAGER/ADMIN mutation | DENY | DENY | DENY | DENY | manager portfolio | 401/403 outside manager/admin |
| `/property-manager/owner/owner-blocks` | Owner block | DENY | DENY | OWN_RESOURCE_ONLY | DENY | DENY | DENY | DENY | DENY | owner’s assigned property | 200 only for owner’s property |
| `/property-manager/qr` and gate messages | QR/gate management | DENY | DENY | ASSIGNED_ONLY where supported | PORTFOLIO_ONLY | ASSIGNED_ONLY where supported | DENY | DENY | DENY | ALLOW | property/portfolio scope | 401/403/404 outside scope |
| `/property-manager/qr/validate` | QR validation | ALLOW | ALLOW | ALLOW | ALLOW | ALLOW | ALLOW | ALLOW | ALLOW | ALLOW | token/property validation | safe validation result only |

## Direct execution results

| Focused case | Result | Evidence |
|---|---|---|
| All 192 protected PM actions: roles outside declared policy | PASS | `PropertyManagerAuthorizationMatrixTests.EveryRoleRestrictedPropertyManagerEndpointRejectsRolesOutsideItsDeclaredPolicy` |
| Owner B → Owner A invoice/document/statement | PASS | Safe 404/denial; no Owner A invoice ID in Owner B portal |
| PM B → PM A invoice/completion read/create | PASS | Cross-portfolio requests denied |
| PM Staff assigned Property A → Property A completion read/create | PASS | Invitation accepted, manager activated membership, assigned operations succeeded |
| PM Staff → Property B/Owner B | PASS | Cross-property and cross-owner requests denied |
| PM Staff → finance completion without finance capability | PASS | Request denied |
| PM Staff → staff-management mutation | PASS | Request denied |

## IDOR status

| Case | Expected | Executed result | Status |
|---|---|---|---|
| Owner A → Owner B invoice/statement/document | 403/404 | Denied or safe 404 | PASS |
| PM A → PM B portfolio | 403/404 | Denied or safe empty own scope | PASS for executed families |
| PM Staff → unrelated property/owner | 403/404 | Denied | PASS |
| Host A → Host B private records | 403/404 | Existing host ownership regressions | PASS for covered M1 families |
| Provider A → Provider B private data | 403/404 | Existing provider ownership coverage | PASS for covered M4 families |
| Officer A → another officer/private report | 403/404 | Existing Wellness authorization coverage | PASS for covered M3 families |
| Non-participant → private message/attachment | 403/404 | Existing security coverage; not rerun in this focused pass | NOT_FULLY_REEXECUTED |

## Certification boundary

**Unexpected authorized 200 responses in the executed focused cases: 0.** The declared route-policy sweep is complete, but a literal every-resource/every-role certification is **NOT COMPLETE**: dedicated fixtures are still needed for cross-account messages/attachments, Wellness assignments/reports, provider private documents/jobs, and Admin-only resources. Physical document I/O remains blocked until MinIO is available.

### Remaining blockers

- **SOUHAIL/application tests:** add the remaining dedicated cross-account fixtures and execute those API rows with real sessions.
- **TERRENCE/server:** configure private MinIO with persistence, then execute physical document/attachment download and signed-access checks.
- **TERRENCE/server:** provision a legitimate Admin QA account for Admin workflow certification; no synthetic Admin role is accepted.
