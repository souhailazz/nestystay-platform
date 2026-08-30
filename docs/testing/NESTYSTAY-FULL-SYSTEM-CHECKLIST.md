# NestyStay Full-System Checklist (M1–M4 Final Audit)

Audit basis: the complete signed agreement at `docs/contracts/NestyStay-Signed-Agreement-April-2026.pdf`, current source tree, PostgreSQL migrations/state, automated tests, live API smoke, concurrency smoke and real-browser evidence. Contractual local behavior is separated from external-provider and production-readiness work.

| Area | Checklist item | Status | Evidence / remaining boundary |
|---|---|---|---|
| Authentication | Registration, login, logout, signed sessions, JWT/session persistence | IMPLEMENTED + VERIFIED | Backend suites, contract browser suite and auth evidence |
| Authentication | Password rules, lockout, 2FA and recovery codes | IMPLEMENTED + VERIFIED | Auth/API/browser tests |
| Authentication | Roles and authorization | IMPLEMENTED + VERIFIED | Signed-token policy and direct HTTP tests |
| Users / Profiles | Guest, Host, Admin, Officer, Service Provider and Local Business roles | IMPLEMENTED + VERIFIED | Registration, role binding and provider/officer tests |
| Properties | CRUD/archive, ownership, photos, amenities, pricing, availability, eKYC setting and public listing/search | IMPLEMENTED + VERIFIED | Property API/UI, PostgreSQL and browser evidence |
| Bookings | Quote, fees, holds, approval/rejection, expiry, cancellation and double-booking protection | IMPLEMENTED + VERIFIED | Workflow tests, live API and concurrency evidence |
| Payments | Stripe application authorization/capture, idempotency, webhook boundary and payout state | IMPLEMENTED + VERIFIED LOCALLY | Real Stripe/Connect account validation remains external-blocked |
| eKYC | Host/guest application boundary, callbacks, pass/fail/timeout/replay transitions | IMPLEMENTED + VERIFIED LOCALLY | Real Alibaba credentials/sandbox remain external-blocked |
| Badges | FREE, VERIFIED, TRUSTED, WELLNESS definitions, contract prices, prerequisites and unlocks | IMPLEMENTED + VERIFIED | Pricebook seeds, lifecycle tests and DB migrations |
| Badges | Upgrade, suspension, expiration, annual renewal, review engine and audit trail | IMPLEMENTED + VERIFIED | PhaseTwo workflow/admin/API evidence |
| Wellness | Active off-duty JCF officer model, verification, approval, suspension/reactivation and privacy | IMPLEMENTED + VERIFIED | Officer API + 3-viewport browser lifecycle |
| Wellness | Quote, exact $25–$50 visit pricing, $19/month plan, one included visit, scheduling and assignment | IMPLEMENTED + VERIFIED | Wellness API/UI, renewal test, live smoke and browser lifecycle |
| Wellness | Photo/report scan, completion, 8% pricebook commission and payout eligibility/paid state | IMPLEMENTED + VERIFIED LOCALLY | API/security tests, PostgreSQL and browser lifecycle; real payout provider external-blocked |
| Directories | Custodian, Trades, Local Business, Police categories; onboarding, moderation, search and provider dashboard | IMPLEMENTED + VERIFIED | Directory API/UI, provider self-registration, moderation and route inventory |
| Directories | Contract badge-gated access and direct HTTP bypass protection | IMPLEMENTED + VERIFIED | `RequireDirectoryAccess`, API tests and UI lock states |
| Police privacy | Active off-duty JCF only, badge/ID-only display, no contact leakage, platform messaging | IMPLEMENTED + VERIFIED | Police DTO/controller tests and browser evidence |
| QR / Gate access | Secure booking/property/guest QR, valid dates, wrong-property rejection, revocation and scan logging | IMPLEMENTED + VERIFIED | Hashed token API, active traveler QR controls, public `/gate/qr` validator, live smoke and browser evidence |
| Notifications | In-app/event abstraction for contractual transitions | IMPLEMENTED + VERIFIED LOCALLY | Event trail is local; email/SMS/push delivery requires provider credentials |
| Messaging | Platform-controlled communication and ownership authorization | IMPLEMENTED + VERIFIED | Messaging/API security tests |
| Database | PostgreSQL persistence, constraints, indexes, migrations and concurrency | IMPLEMENTED + VERIFIED | Migration history, state snapshot and race smoke |
| Frontend | Contractual routes/forms/dashboards, responsive behavior, accessibility and console/network health | IMPLEMENTED + VERIFIED | 19-route × 3-viewport inventory plus targeted lifecycle suites, including the QR gate journey |
| Admin | Moderation, officer/provider verification, badges, audits and financial controls | IMPLEMENTED + VERIFIED LOCALLY | Admin/API/browser evidence; external provider actions remain blocked |
| Deployment | Hosting/TLS/domain/R2/monitoring/backups/compliance | PRODUCTION-ONLY | Outside M1–M4 contractual functional completion |
| Phase 5/6 | Property Manager and native mobile applications | OUT OF M1–M4 SCOPE | Later agreement phases |

## Final status totals

- M1–M4 contractual local requirements: **all implemented and verified**.
- Local contractual blockers: **0**.
- External/provider validation blockers: **4** (real Stripe, real Alibaba, real payout/Connect, real notification delivery).
- Production-only blockers: hosting/TLS/R2/monitoring/backups/compliance and operational launch controls.
