# M1/M2 Security Audit

Audit date: 2026-07-24

Branch: `audit/m1-m2-remediation`

Baseline commit audited: `5465ba77308c17245cfa06b1a29f231104e037b9`

CI evidence: `.github/workflows/m1-m2-acceptance.yml` is active and has passed on this branch. Local command/test evidence is listed in `M1_M2_TEST_EVIDENCE.md`.

Verdict: PARTIAL / NOT COMPLETE. Predictable bearer-token acceptance, major booking exposure, several upload gaps, and default admin-token leakage have been reduced, but authentication, ownership, payment, provider, and admin-operation requirements remain materially incomplete.

## Fixed Or Improved

| Area | Evidence |
| --- | --- |
| Signed session tokens | Added `IAccessTokenService` and `SignedAccessTokenService`. Phase 1 email/TOTP and Google sign-in issue `nsty.v1.*` HMAC-SHA256 bearer tokens with subject, roles, issued time, and expiration. |
| Token validation | `AdminTokenAuthenticationHandler` validates signed user bearer tokens into `NameIdentifier` and role claims while preserving hashed admin/operator token support. |
| Token rejection coverage | `SignedAccessTokenSecurityTests` verifies missing, expired, modified-payload, invalid-signature, and legacy predictable tokens are rejected. |
| Legacy local token parser | Removed GUID-derived bearer parsing from `SpecCompletionController`; protected spec endpoints use authenticated claims. |
| Production secret requirement | `ProductionIntegrationValidator` requires a strong session token secret in Production, and `SignedAccessTokenService` rejects weak secrets. |
| Booking list/detail auth | `BookingsController` requires `[Authorize]`, derives user identity from claims, and filters traveler/host/admin access server-side. |
| Booking creation identity | `POST /api/bookings` ignores submitted `guestUserId` and overrides it with the authenticated user id. |
| Booking capture auth | `POST /api/bookings/{id}/capture-payment` requires admin role or host ownership before capture. Traveler/browser capture is forbidden. |
| Direct verification result auth | `POST /api/bookings/{id}/verification-result` is admin-policy protected. Webhook processing remains separate. |
| Upload validation/storage | Message attachments, property photos, wellness report photos, identity documents, admin case evidence, and profile photos now use prepared upload records, storage-backed content writes, MIME/extension/size checks, scan status, and ownership checks in the current implemented surfaces. |
| Frontend upload surfaces | Messaging, host property photos, wellness report photos, traveler identity documents, admin case evidence, and profile photo flows have UI upload states with progress/cancel/retry style affordances where implemented. |
| Frontend token handling | Booking list/detail/create/capture calls pass bearer tokens. Admin/token UI state no longer defaults to or persists `dev-admin-token`; admin operations wait for explicit token entry. |
| Tracked QA scripts/examples | Recording scripts require `NESTYSTAY_ADMIN_TOKEN`; `.env.example` uses placeholders instead of a hardcoded admin token/hash. |
| Seed race hardening | `EfSpecCompletionStore` now tolerates PostgreSQL duplicate-key races during seed-only writes observed under parallel Playwright route loads. |

## Remaining Blockers

| Area | Status | Reason |
| --- | --- | --- |
| OTP/code exposure | FAIL | Register/login/spec auth flow responses still expose development codes in normal API responses, and the UI still displays local/demo code helpers. |
| Real TOTP enrollment | FAIL | Complete production TOTP enrollment, QR/URI proof, enable-after-valid-TOTP, recovery-code confirmation, recovery-code login, and one-time consumption are not complete. |
| Recovery-code hashing | FAIL | Spec recovery codes are still returned/stored as plaintext milestone data. |
| OAuth provider disabling | PARTIAL | Social config exists, but the auth endpoint still accepts local Google sign-in request data and no Apple/Facebook consent flow is implemented. |
| Stripe Elements and webhook signature | FAIL | Checkout UI does not use Stripe Elements. Stripe webhook replay/signature coverage remains incomplete for booking payments. |
| File upload evidence | PARTIAL | Upload pipelines now exist for several surfaces, but full UI state evidence, download/access tests, expiry proof, virus-scan provider behavior, and every DOCX upload state are not complete. |
| Host/property ownership | PARTIAL | Booking host ownership now uses `HostUserId`, but property creation/editing, pricing rules, promotions, and review replies still need property-owner validation. |
| Provider ownership | FAIL | Directory provider upsert lacks a complete provider-owner model and slug ownership enforcement. |
| Payment methods | FAIL | Traveler payment methods remain metadata-oriented and are not fully Stripe SetupIntent backed in the user-facing flow. |
| Admin operations | FAIL | Sensitive admin actions are still generic cases rather than domain-specific audited services for users, properties, refunds, disputes, support, and fraud. |

## Security Verdict

Do not claim M1/M2 security completion. This branch has closed several high-risk baseline issues and now includes representative Playwright smoke evidence, but the remaining auth, ownership, payment, upload-evidence, and admin-operation gaps keep the security verdict at PARTIAL / NOT COMPLETE.
