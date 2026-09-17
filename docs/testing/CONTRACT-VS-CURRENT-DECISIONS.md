# Contract vs Current Implementation Decisions

Date: 2026-09-17 (Africa/Casablanca)

Source of contractual requirements: [`docs/contracts/NestyStay-Signed-Agreement-April-2026.pdf`](../contracts/NestyStay-Signed-Agreement-April-2026.pdf). The PDF is 11 pages, unencrypted, and was reviewed page-by-page, including handwritten page 4, blank continuation page 5, badge-matrix continuation page 6, and signed delivery page 11.

## Controlling interpretation

- Page 9 is the primary M1–M5 phase map: M1 Core, M2 Badges, M3 Wellness, M4 Directories, M5 Property Manager.
- Page 11 repeats the commercial delivery rows but visibly mixes the M2 and M3 descriptions. The detailed feature sections and page 9 resolve the functional interpretation; the page 11 wording discrepancy remains documented, not silently ignored.
- Native iOS/Android apps are Phase 6 and are outside the five M1–M5 milestones.

## Provider and technology decisions

| Signed contract | Current decision | Classification | Reason |
|---|---|---|---|
| Alibaba Cloud eKYC | Stripe Identity | SUPERSEDED_BY_LATER_WRITTEN_INSTRUCTION | The client later explicitly instructed removal of Alibaba and use of Stripe Identity. Alibaba is not reintroduced. The current backend provider selector exposes Stripe Identity only. |
| Stripe / PayPal payment stack | Stripe application boundary plus deterministic local adapter | IMPLEMENTATION_VARIATION | Local verification uses deterministic test behavior. Live Stripe account, webhook signature, Identity sessions, Connect and payout rails require staging credentials and are not claimed as externally verified. |
| React/Next, Node/Python, Auth0/JWT, Alibaba hosting, Cloudflare | React/Vite frontend, ASP.NET backend, PostgreSQL, repository provider seams and nginx/systemd deployment context | IMPLEMENTATION_VARIATION / NO_FUNCTIONAL_IMPACT | The agreement names a technology stack, but the functional requirements do not require blindly replacing the working architecture. |
| Firebase/Twilio/AWS SES notifications | Provider-neutral notification outbox with Brevo integration boundary | IMPLEMENTATION_VARIATION / EXTERNAL VERIFICATION REQUIRED | Local email is queued/file-backed; Brevo delivery and production sender configuration are not locally proven. |
| InsuraGuest | Local provider adapter, plan catalog and property/manager coverage flags | PARTIAL / EXTERNAL PROVIDER REQUIRED | The contract describes an optional host insurance add-on with plans. Local code exposes plan metadata and eligibility/coverage flags, but no live InsuraGuest policy purchase/claims integration is claimed. |

## Commercial conflicts not silently reconciled

- Typed guest platform fee: **9%**.
- Handwritten post-launch guest fee: **10%**.
- Current code uses the typed 9% baseline. The 10% value requires written commercial confirmation.
- Typed host commission: **3%** / host keeps 97%.
- Handwritten note also says post-launch host 3%; this does not change the coded baseline, but launch timing remains commercial.
- Typed Trusted price: **$49 one-time**.
- Handwritten Gold/Platinum values, durations, $36/$29 booking references and fee-for-life/property notes are ambiguous and are not hard-coded.

## Page 11 delivery-row discrepancy

Page 11 labels Phase 2 “Badge System Live” but describes officer onboarding, wellness booking, photo reports and officer payouts; it labels Phase 3 “Wellness Features Live” but starts with badge levels and annual renewal. Page 9 and the detailed headings consistently define M2 as Badges and M3 as Wellness. This audit therefore maps the implementation to page 9 while retaining the page 11 wording as a contract clarification item.

## Release boundary

The local release candidate is operationally verified, but that is not the same as staging/production acceptance. The remaining release boundary is external provider/infrastructure verification (live Stripe and Stripe Identity, Brevo, storage, maps, notifications, backups/monitoring, TLS/domain) plus human accessibility and operational certification. These are not converted into false “complete” claims in the traceability matrix.
