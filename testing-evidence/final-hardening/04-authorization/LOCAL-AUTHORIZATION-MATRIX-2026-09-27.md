# Local Authorization Matrix — 2026-09-27

## Scope

This is a local API regression result from backend certification commit `393edd1e20269350ed565fff8dda53b997792e39`. It uses real HTTP requests and server-issued test sessions; it does not forge browser sessions or alter staging/production.

## Executed checks

Command:

```text
dotnet test tests/NestyStay.Api.Tests/NestyStay.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~CrossResourceAuthorizationMatrixTests|FullyQualifiedName~PropertyManagerAuthorizationMatrixTests|FullyQualifiedName~BadgeAuthorizationTests"
```

Result: **13 passed, 0 failed, 0 skipped**.

Covered boundaries:

- Message conversations and attachments are restricted to participants.
- Wellness visits, reports, photos, and officer documents are scoped to the assigned officer/property.
- Service-provider insights, business details, and documents reject cross-provider access.
- All discovered admin-only API endpoints reject every non-admin role.
- Owner and Property Manager records do not cross portfolio boundaries.
- Property Manager staff scope prevents cross-property and cross-owner access.
- Gate Guard invitation, acceptance, property-scoped QR validation, PM-workspace denial, and revocation.
- The legacy unscoped Gate Guard staff endpoint rejects the assignment.
- Role-restricted Property Manager endpoints reject roles outside their declared policy.
- Host badge/property ownership and logout/session invalidation remain covered by the badge authorization suite.

## Certification boundary

This is strong local authorization evidence. It does not replace the staging role/IDOR matrix, because staging QA credentials and deployed runtime access are external inputs. Staging must still verify the same boundaries across Guest, Host, Owner, Property Manager, PM Staff, Wellness Officer, Service Provider, Local Business, Gate Guard, and Admin accounts.
