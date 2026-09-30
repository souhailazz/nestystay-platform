# NestyStay Business Functional Audit

**Audit date:** 2026-09-22  
**Evidence rule:** code presence is separated from automated/local runtime verification and external-provider verification.

## Milestone 1 — Core Booking

**Status: PARTIALLY VERIFIED.** Implemented areas include authentication/session flows, Explore search and filters, dates/guests, property details, availability, galleries and property metadata, favorites, map/list routes, quote and booking creation, pending state, host approval/rejection with persisted reason, guest rejection messaging, Stripe payment/webhook/idempotency code, Stripe Identity adapter/events, trips, receipts/invoices, QR, and notifications. Backend tests passed for the covered booking/auth/payment paths; local health and `/api/properties` returned 200; many browser flows passed.

Remaining evidence gaps: the browser suite is not green because several authenticated/admin fixtures create no valid server session; MinIO-backed upload/download was skipped; real Stripe Identity/payment-provider verification and Brevo delivery were not performed; full mobile checkout and human accessibility certification remain incomplete.

## Milestone 2 — Badge System

**Status: PARTIALLY VERIFIED.** FREE, VERIFIED, TRUSTED, and WELLNESS definitions, eligibility, benefits, authorization, purchase/renewal/expiry/suspension, server-authoritative Stripe PaymentIntent records, webhook/idempotency handling, refund handling, and frontend/admin routes are present. Backend badge tests passed and non-admin badge flows were exercised.

Remaining evidence gaps: admin badge browser checks hit the invalid admin-session fixture; physical Stripe PaymentIntent behavior was not tested against configured external credentials; all four seeded states were not visually verified end-to-end in a live environment.

## Milestone 3 — Wellness

**Status: PARTIALLY VERIFIED.** Officer, host wellness, plan/visit, assignment/scheduling, reporting, privacy/authorization, pricing/commission/payout state code, and related routes/tests exist. Host/officer representative flows passed locally.

Remaining evidence gaps: admin wellness routes were blocked by the test-session fixture; report/photo uploads require MinIO; email and real payout/provider integrations were not verified; full operational mobile and human accessibility testing is incomplete.

## Milestone 4 — Directories, Trust, and QR

**Status: PARTIALLY VERIFIED.** Custodian/trades/local-business/officer directory structures, search/filter/provider lifecycle, moderation code, ratings/reviews, QR issue/validate/expire/revoke/history/fallback flows, privacy checks, and gate communication routes are present. Representative provider and QR browser paths passed.

Remaining evidence gaps: admin moderation browser checks were blocked by the invalid admin fixture; the complete cross-role/IDOR matrix was not run; map/geocoder/tile-provider behavior is partial and not externally certified; Gate Guard provisioning remains a scope decision and is not claimed as complete.

## Milestone 5 — Property Manager

**Status: PARTIALLY VERIFIED.** Manager dashboard/portfolio scoping, owners, finance/invoices, utilities, maintenance, vendors, community, governance, documents, gate/QR, subscriptions, staff/team, calendar/work orders, cleaning/inspection, and reporting areas are represented in code and many representative browser paths passed.

Known incomplete or partial areas from the existing PM acceptance evidence: owner approval evidence/upload preview; reservation modification/cancellation/full timeline; richer calendar/conflict UX; accounting ledger/receipt detail; vendor contracts/licences/performance UI; cleaning/photo/offline/readiness enforcement; inspection remediation/versioned templates; asset photos/consumables; incident privacy/upload timeline; document preview/restore/granular permissions/ZIP UX; gate delivery history/resend/cross-property audience; governance visualizations/proxy labels; staff invitation acceptance/deep-link; generalized bulk operations; provider notifications; consolidated KPI views; and full mobile table/card/calendar parity.

## Cross-cutting result

Backend: **193 passed, 0 failed, 1 skipped**. Frontend unit: **56 passed**; typecheck/build pass; lint 0 errors/84 warnings; frontend `npm audit` reports 0 known vulnerabilities. Playwright: **224 scheduled, 179 passed, 25 failed, 8 skipped, 12 did not run**. This supports substantial implementation, not full platform certification.
