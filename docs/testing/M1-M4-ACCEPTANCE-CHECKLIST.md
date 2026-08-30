# NestyStay M1–M4 Acceptance Checklist (Final)

Checked items are backed by current executable evidence under `testing-evidence/milestones-1-4/` and retained M1–M2 evidence. Provider credentials are listed separately and do not block the local contractual decision.

## M1 Core

- [x] Registration, login, signed session, logout, 2FA and recovery.
- [x] Host listings/properties and ownership.
- [x] Booking quote/popup with server-authoritative 9% fee.
- [x] PENDING hold, APPROVED, REJECTED, expiry and overlap protection.
- [x] Optional per-property guest eKYC upsell and rejection date release.
- [x] Stripe application authorization/capture boundary.

## M2 Badges

- [x] FREE, VERIFIED, TRUSTED and WELLNESS catalog and exact contract pricing.
- [x] Eligibility, prerequisites and backend feature-access enforcement.
- [x] Upgrade, suspension, expiration, annual renewal and audit paths.
- [x] Host/admin badge dashboard and authenticator-app 2FA.

## M3 Wellness

- [x] Officer application/account-role binding and active off-duty JCF-only validation.
- [x] Admin review, approve, reject, suspend and reactivate.
- [x] Privacy-safe badge-ID-only officer display and platform-controlled communication.
- [x] Host quote/booking, exact $25–$50 visit pricing and $19 plan/one included visit.
- [x] Parish/service-area scheduling, availability and host ownership.
- [x] Assignment conflict/concurrency protection.
- [x] Officer verified photo/report upload and scan status.
- [x] Commission, escrow/payment state, payout eligibility and paid state.
- [x] Host report visibility and transition event trail.
- [x] Desktop/tablet/mobile browser lifecycle.

## M4 Directories and Access

- [x] Custodian onboarding, moderation, search and shared provider dashboard.
- [x] Trades onboarding, moderation, search and shared provider dashboard.
- [x] Local Business brick-and-mortar onboarding, moderation, search and dashboard.
- [x] Police directory privacy and parish filtering.
- [x] Badge-gated access and direct HTTP bypass protection.
- [x] Guest verification upsell reused from M1.
- [x] Secure booking/property/guest QR generation.
- [x] QR validation, valid dates, wrong-property rejection, revocation and scan logging.
- [x] Gate-facing QR verification UI/API.
- [x] Emergency number 119 displayed on wellness/listing surfaces.

## Explicit external/provider gates

- [ ] Real Stripe live account/webhook/Connect validation — BLOCKED pending live credentials.
- [ ] Real Alibaba eKYC sandbox validation — BLOCKED pending credentials/signature material.
- [ ] Real email/SMS/push delivery — BLOCKED pending provider credentials.
- [ ] Real payout/Stripe Connect execution — BLOCKED pending provider capability.

## Final local decision

M1 contractual functional completion: **PASS**. M2 contractual functional completion: **PASS**. M3 and M4 contractual functionality: **PASS**. Production readiness: **NO** until the explicitly external/production gates are configured and validated.
