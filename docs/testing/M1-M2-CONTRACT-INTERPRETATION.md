# NestyStay Milestones 1-2 Contract Interpretation

Date: 2026-08-30 (Africa/Casablanca)

## Contract source gate

Primary source: [`docs/contracts/NestyStay-Signed-Agreement-April-2026.pdf`](../contracts/NestyStay-Signed-Agreement-April-2026.pdf).

- Source: `C:\Users\Administrator\Downloads\Nesty Stay Agreement April 2026- Signed.pdf`
- Repository copy: `docs/contracts/NestyStay-Signed-Agreement-April-2026.pdf`
- PDF: 11 pages, readable, unencrypted, no forms or JavaScript
- Source and repository copy are byte-identical (785,845 bytes)
- SHA-256: `0C4AAD0B1A2D015433C0875107DD171C93C4191281D7CCCBBE748B37DB4FD28D`
- Extraction/render evidence: `tmp/pdfs/NestyStay-Signed-Agreement-April-2026.txt` and `tmp/pdfs/agreement-pages/page-01.png` through `page-11.png`

All pages were read, including the signed handwritten notes on page 4 and signatures on page 11. OCR artifacts in the extracted text were checked against the rendered pages before making the decisions below.

## M1 contractual interpretation

Section 9 (page 9) calls Phase 1 **Core** and lists: registration, login, 2FA, Alibaba eKYC, property listings, a booking popup with PENDING/APPROVED/REJECTED flow, and Stripe payments, web and all devices. Section 11 (page 11) repeats the same Phase 1 deliverables as the “Core Booking System Live” payment milestone. Section 2 (pages 1-2) defines the guest flow: listing -> Book Now popup -> Alibaba identity check when enabled -> PENDING with dates held and host notification -> APPROVED after verification -> payment after approval -> REJECTED on failure with dates released. The typed economics on page 2 state a 3% host commission and 9% guest platform fee; page 4 contains a handwritten “post launch” 3% host / 10% guest note, recorded as a conflict rather than silently substituted.

**Decision:** M1 contractual functional scope is PASS locally. Stripe application integration and deterministic Alibaba application integration are PASS; real Stripe provider validation and real Alibaba validation are separately BLOCKED and are not counted as local M1 functional failures.

## M2 contractual interpretation

Section 9 (page 9) calls Phase 2 **Badges** and lists: all four badge levels, annual renewals, an automated review engine, owner dashboard, and authenticator app 2FA. Sections 4-5 (pages 3-6) define the four levels and feature gates:

- FREE: $0; basic profile/photos; listings, calendar, messaging, QR code access, 97% payout.
- VERIFIED: $0 to host (NestyStay pays the $0.14 Alibaba check); government ID via Alibaba eKYC; Verified, Custodian, Local Business, and guest-verification-upsell access.
- TRUSTED: $49 one-time; Verified plus 3 bookings; Trusted, Trades, search boost, and referral access.
- WELLNESS: $25-$50 per visit or $19/month; Verified plus property address; Police, wellness, in-person guest ID, Wellness badge, and Police Verified filter access.

The application now exposes those prices/cadences, exact feature labels, server-side eligibility, ownership controls, annual renewal records for paid assignments, minute-cadence review/expiry/reminder maintenance, live owner dashboard reads, and authenticator-app TOTP enrollment.

**Decision:** M2 local and contractual functional implementation is **PASS** for the section 9 badge scope. The conflicting section 11 payment-row label is recorded for commercial clarification, but it is not a functional blocker because section 9 unambiguously defines Phase 2 as Badges and section 9 Phase 3 contains the wellness deliverables.

## Internal contractual conflict

Section 11’s `$600 Phase 2 - Badge System Live` row lists officer onboarding/verification, wellness booking, scheduled visits with photo reports, and commission/payouts. Section 9 assigns those wellness deliverables to Phase 3, while Section 9 Phase 2 is badges. This is an internal commercial-label inconsistency in the signed source. Section 9 is the controlling development-phase mapping for this audit; the row is retained as a clarification note and does not make M2 functionality fail or pull Phase 3 work into M1.

## Differences found and fixed

1. Guest fee calculation was tiered 8/10/12%; it now uses the typed contract 9% base fee, with the signed page 4 post-launch 10% note documented as an unresolved commercial conflict.
2. Verified/Trusted/Wellness badge price/cadence metadata was not contract-accurate; it now reflects included/$0, $49 one-time (renewal capability), and $19 monthly respectively.
3. Badge unlock labels were corrected to the signed feature comparison, including QR code access, Trusted badge, in-person guest ID check, drive-by patrol, and Police Verified filter.
4. Wellness eligibility incorrectly required a subscription in addition to Verified plus property address; that extra gate was removed.
5. Persistent milestone pricebook/badge rows are upgraded by migration `20260830144204_AlignSignedContractM1M2PricingV2`; domain badge seed rows are aligned by `20260830150405_AlignSignedContractDomainBadgeSeeds`. PostgreSQL evidence confirms the corrected rows.

## Explicitly separate later phases and providers

The signed agreement places wellness officer onboarding/features in Phase 3 (section 9), directories/guest upsell/QR entry in Phase 4, Property Manager in Phase 5, and mobile apps in Phase 6. Those are not M1/M2 contractual gates except for the contradictory Phase 2 payment row noted above. Real Stripe, real Alibaba, production notifications, hosting/TLS/CDN, insurance, monitoring, backups, retention, and external security review are provider/production statuses, not evidence that the local M1/M2 functional code fails.
