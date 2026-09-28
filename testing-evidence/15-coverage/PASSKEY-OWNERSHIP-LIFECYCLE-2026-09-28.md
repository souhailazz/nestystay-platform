# Passkey ownership and lifecycle coverage — 2026-09-28

## AUTO-0029

The passkey controller was an uncovered security-sensitive surface. This focused API integration slice uses real signed test sessions and the application's in-memory API host; it does not bypass authorization or call a fake controller implementation.

## Covered behavior

- An authenticated user lists only that user's passkeys.
- Removing an owned passkey persists a revocation timestamp.
- A revoked credential remains visible as inactive, preserving account security/audit visibility.
- A second removal returns `404` rather than mutating the credential again.
- One user cannot revoke another user's credential; the API returns `404` and the target credential remains active.

## Evidence

| Item | Result |
|---|---|
| Backend commit | `4ece56f` |
| Focused `PasskeysEndpointTests` | 3 passed, 0 failed, 0 skipped |
| Complete configured backend suite | 225 passed, 0 failed, 0 skipped |
| Totals | Domain 6; Application 24; Infrastructure 32; API 163 |
| Coverage artifact | `backend/TestResults/sonar-certification/coverage-current-4ece.xml` generated successfully with the full suite |

The disposable Docker daemon stopped before the new coverage artifact could be uploaded to a fresh SonarQube instance. Therefore the latest server-side Sonar metrics remain the prior verified scan at `393edd1`; no newer coverage percentage is claimed here.

No migration, production, staging, credential, or provider configuration was changed.
