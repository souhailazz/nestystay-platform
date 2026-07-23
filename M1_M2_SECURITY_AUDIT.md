# M1/M2 Security Audit

Audit date: 2026-07-23

Branch: `audit/m1-m2-remediation`

Baseline commit audited: `5465ba77308c17245cfa06b1a29f231104e037b9`

Remediation commit: `214f623ef6dd71e3564f9744d7a3cf3d05f886ef`

CI evidence: Not run in this session. Verification below is local command/test evidence only.

Verdict: PARTIAL / NOT COMPLETE. Critical booking exposure, predictable local bearer-token acceptance, and default admin-token leakage were reduced in this remediation pass, but M1/M2 still have unresolved security blockers.

## Fixed In Remediation Commit

| Area | Evidence |
| --- | --- |
| Signed session tokens | Added `IAccessTokenService` and `SignedAccessTokenService`. Phase 1 email/TOTP and Google sign-in now issue `nsty.v1.*` HMAC-SHA256 bearer tokens. Payload includes user id (`sub`), roles, issued time (`iat`), and expiration (`exp`). |
| Token validation | `AdminTokenAuthenticationHandler` validates signed user bearer tokens into `NameIdentifier` and role claims while preserving hashed admin/operator token support. |
| Token rejection coverage | `SignedAccessTokenSecurityTests` verifies missing, expired, modified-payload, invalid-signature, and legacy predictable tokens are rejected. |
| Legacy local token parser | Removed GUID-derived bearer parsing from `SpecCompletionController`; protected spec endpoints now use authenticated claims. |
| Production secret requirement | `ProductionIntegrationValidator` requires `Security:SessionTokenSecret` or `NESTYSTAY_SESSION_TOKEN_SECRET` in Production and rejects values shorter than 32 characters. `SignedAccessTokenService` also rejects weak secrets. |
| Development fallback boundary | `SignedAccessTokenSecurityTests` verifies Development can run without the session secret; Production cannot. |
| Booking list/detail auth | `BookingsController` requires `[Authorize]`, derives user identity from claims, and filters traveler/host/admin access server-side. |
| Booking creation identity | `POST /api/bookings` ignores submitted `guestUserId` and overrides it with the authenticated user id. |
| Booking capture auth | `POST /api/bookings/{id}/capture-payment` requires admin role or host ownership before capture. Traveler/browser capture is forbidden. |
| Direct verification result auth | `POST /api/bookings/{id}/verification-result` is admin-policy protected. Webhook processing remains separate. |
| Frontend token handling | Booking list/detail/create/capture calls pass bearer tokens. Admin/token UI state no longer defaults to or persists `dev-admin-token`. |
| Tracked QA scripts/examples | Recording scripts now require `NESTYSTAY_ADMIN_TOKEN`; `.env.example` uses placeholders instead of a hardcoded admin token/hash. |

## Remaining Blockers

| Area | Status | Reason |
| --- | --- | --- |
| OTP/code exposure | FAIL | Register/login/spec auth flow responses still expose codes in normal API responses, and the UI still displays local/demo codes. |
| Real TOTP enrollment | FAIL | No real `otpauth://` setup flow, QR generation, enable-after-valid-TOTP, recovery-code confirmation, recovery-code login, or one-time consumption. |
| Recovery-code hashing | FAIL | Spec recovery codes are returned/stored as plaintext milestone data. |
| OAuth provider disabling | PARTIAL | Social config exists, but the auth endpoint still accepts local Google sign-in request data and no Apple/Facebook consent flow is implemented. |
| Stripe Elements and webhook signature | FAIL | Checkout UI does not use Stripe Elements. Stripe webhook replay/signature coverage is incomplete for booking payments. |
| File uploads | FAIL | Messaging attachments remain metadata-only with no signed upload/download URLs, MIME allowlist, 10 MB validation, progress, retry, or expiry enforcement. |
| Host/property ownership | PARTIAL | Booking host ownership now uses `HostUserId`, but property creation/editing, pricing rules, promotions, and review replies still need property-owner validation. |
| Provider ownership | FAIL | Directory provider upsert lacks a provider-owner model and slug ownership enforcement. |
| Payment methods | FAIL | Traveler payment methods are metadata-only and not Stripe SetupIntent backed. |
| Admin operations | FAIL | Sensitive admin actions are still generic cases rather than domain-specific audited services for users, properties, refunds, disputes, support, and fraud. |

## Security Verdict

Do not claim M1/M2 security completion. This pass removes the most obvious predictable bearer-token path and locks down primary booking endpoints, but authentication, ownership, upload, payment, and admin-operation requirements remain materially incomplete.
