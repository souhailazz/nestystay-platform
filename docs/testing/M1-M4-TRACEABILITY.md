# NestyStay Milestones 1–4 Contract Traceability (Final Local Audit)

Primary contractual source: [signed April 2026 agreement](../../docs/contracts/NestyStay-Signed-Agreement-April-2026.pdf), 11 pages, SHA-256 `0C4AAD0B1A2D015433C0875107DD171C93C4191281D7CCCBBE748B37DB4FD28D`. The signed PDF was read in full and reconciled against the implementation and the existing M1–M2 interpretation documents. A requirement is `PASS` when local behavior is implemented and exercised; provider-only validation is separated below and is not used to fail local milestone functionality.

| Milestone | Contract requirement | Implementation and evidence | Status |
|---|---|---|---|
| M1 | Account registration, login, signed sessions, logout, 2FA/recovery and role access | Auth API/domain tests; browser contract suite; persisted auth/session evidence | PASS |
| M1 | Host properties/listings, ownership, search and listing controls | Property API/store; host UI; API/browser/PostgreSQL evidence | PASS |
| M1 | Booking popup/quote, server-authoritative 9% fee, pending hold, approval/rejection, expiry and overlap protection | Booking workflow tests; live API; concurrency evidence; `final-contract-validation` | PASS |
| M1 | Optional per-property guest eKYC upsell; pending/approved/rejected state and date release | eKYC adapter/state machine and booking UI; API/security tests; browser pending/release flow | PASS |
| M1 | Stripe application authorization/capture boundary and payment state | Payment gateway abstraction, idempotent API/webhook tests and test-mode UI boundary | PASS |
| M2 | FREE, VERIFIED, TRUSTED and WELLNESS badge catalog and contract prices | Typed pricebook, EF seeds/migrations, badge API/domain tests and PostgreSQL migration evidence | PASS |
| M2 | Badge eligibility/prerequisites and feature unlocks | Server-side authorization/access service, direct HTTP tests, host/admin UI | PASS |
| M2 | Upgrade, suspension, expiry, annual renewal, review/audit trail | Badge lifecycle endpoints/jobs, audit rows and PhaseTwo workflow tests | PASS |
| M2 | Host/admin badge dashboards and authenticator-app 2FA | Host/admin UI and auth/browser/API evidence | PASS |
| M3 | Active off-duty JCF officer onboarding; retired/inactive exclusion; admin review lifecycle | Officer self-registration/onboarding, admin approve/reject/suspend/reactivate, API tests and 3-viewport browser lifecycle | PASS |
| M3 | Wellness quote, $25–$50 visit pricing, $19 monthly plan with one included visit, parish scheduling and host ownership | Quote/store/controller, subscription start/renew/cancel, visit booking UI/API, 48 API tests and live smoke | PASS |
| M3 | Officer assignment, availability and race/overlap protection | Assignment gate and concurrency smoke: one success plus one expected conflict | PASS |
| M3 | Photo report upload, magic-byte scan, report submission, completion, 8% pricebook commission and payout state | Officer browser lifecycle, report/photo/payout APIs, PostgreSQL state and security tests | PASS |
| M3 | Badge-ID-only officer privacy and platform-controlled communication | Privacy-safe DTOs, Police restrictions, direct HTTP tests and browser evidence | PASS |
| M3 | Admin queue, financial inspection and transition event trail | Admin endpoints/dashboard, audit/event persistence and browser/API evidence | PASS |
| M4 | Custodian, Trades and Local Business directories; provider onboarding, moderation, search and shared provider dashboard | Generic persisted directory model with category rules, self-service registration, admin moderation, filtered UI/API and route inventory | PASS |
| M4 | Badge-gated directory access: Custodian VERIFIED, Trades TRUSTED, Local Business VERIFIED | Server-side `RequireDirectoryAccess`, direct HTTP bypass tests, host UI lock states | PASS |
| M4 | Police directory: active off-duty JCF only, badge ID only, no names/contact, platform messaging only, host wellness access | Officer-to-directory projection, privacy-safe Police API and locked anonymous/non-Wellness paths | PASS |
| M4 | Guest verification upsell reused from M1 per property | Existing eKYC property/booking flow and browser/API evidence | PASS |
| M4 | Secure booking/property/guest QR, valid dates, wrong-property rejection, revocation and scan logging | Hashed token QR issue/validate/revoke endpoints, ownership/booking checks and live/API/browser evidence | PASS |
| M4 | Jamaica emergency number 119 visible on wellness/listing experience | Wellness quote/UI constant and 3-viewport browser evidence | PASS |

## Provider and production split

| Separate status | Decision | Reason |
|---|---|---|
| Stripe application integration | PASS | Local gateway and test-mode application boundary are implemented and tested. |
| Real Stripe provider validation | BLOCKED | Requires live Stripe account/webhook/Connect credentials and network validation. |
| eKYC application integration | PASS | Local provider adapter, state transitions, callback signature/replay protection and UI are implemented. |
| Real Alibaba provider validation | BLOCKED | Requires Alibaba sandbox credentials/signature material. |
| Local security validation | PASS | Authorization, ownership, privacy, QR token, upload scan and webhook/replay tests pass. |
| Full production readiness | NO | External credentials, deployment/TLS/R2/notifications/monitoring/backups/compliance remain production work. |

All M1–M4 contractual rows are locally complete; the only `BLOCKED` statuses are explicitly external-provider or production validation gates.
