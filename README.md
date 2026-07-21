# NestyStay Platform

NestyStay is a full-stack property booking and host-services platform built with a Vite React frontend and an ASP.NET Core API. The local app is wired through the Vite `/api` proxy to the backend, with PostgreSQL-backed persistence for the milestone workflows.

## Project Structure

- `frontend`: Vite React TypeScript web app.
- `backend`: ASP.NET Core .NET API with Domain, Application, Infrastructure, and Api layers.
- `artifacts`: Local QA notes, proof files, and recorded walkthrough videos.

## Local Run

Backend:

```powershell
cd backend
dotnet run --project src/NestyStay.Api --urls http://localhost:5019
```

Frontend:

```powershell
cd frontend
npm run dev -- --host 127.0.0.1 --port 5173
```

Development OpenAPI JSON is exposed by the backend at `/openapi/v1.json`.

## Milestone Status

- [x] Milestone 1: Core guest, host, booking, verification, and payment flows are complete locally.
- [x] Milestone 2: Badges, pricing, campaigns, founding benefits, commission logic, and admin controls are complete locally.
- [x] Frontend and backend are linked locally through the Vite proxy and shared API client.
- [x] Local QA pass completed for the full-stack milestone flows.
- [ ] Production launch is not complete yet. Production provider credentials, hosting, deployment, monitoring, backups, and compliance review are still required.

## Milestone 1 Checklist - Core Booking Platform

### Authentication and Account Access

- [x] Guest registration API is implemented.
- [x] Password login API is implemented.
- [x] Two-factor authentication challenge flow is implemented.
- [x] Two-factor verification flow is implemented.
- [x] Google-style sign-in flow is implemented for local verified session creation.
- [x] Frontend auth state persists the active session in local storage.
- [x] Protected guest and host pages require an active frontend session.
- [x] Login page is connected to backend auth endpoints.
- [x] Guest dashboard route is connected to authenticated session data.
- [x] Host dashboard route is connected to authenticated session data.

### Property Listing and Discovery

- [x] Property listing API is implemented.
- [x] Property detail API is implemented.
- [x] Host property creation API is implemented.
- [x] Frontend Explore page reads live property listings from the backend.
- [x] Frontend property detail page reads individual property data from the backend.
- [x] Frontend host property management page can create listings through the backend API.
- [x] Host-created listings persist and appear across Explore, property detail, dashboards, and admin views.
- [x] Property records include host user id, host name, host email, title, location, country, nightly rate, currency, badge level, cancellation policy, verification flags, insurance flags, and highlights.
- [x] Jamaican-styled property imagery is present in the frontend listing and detail experience.

### Booking Quote Flow

- [x] Booking quote API is implemented.
- [x] Frontend booking modal is connected to the quote endpoint.
- [x] Quote flow calculates check-in, check-out, nights, nightly rate, stay subtotal, guest platform fee, total amount, and currency.
- [x] Quote response includes price breakdown lines.
- [x] Quote response indicates whether guest verification is required.
- [x] Quote response indicates whether selected dates are available.
- [x] Quote response includes pending hold expiration data where applicable.

### Booking Creation Flow

- [x] Booking creation API is implemented.
- [x] Frontend booking creation flow calls the backend API.
- [x] Booking records persist guest user id, property id, dates, status, verification status, payment status, totals, and timeline.
- [x] Booking records include property title and host name for dashboard display.
- [x] Booking records include notification queue entries for local workflow proof.
- [x] Booking records include price breakdown lines.
- [x] Guest dashboard reads created bookings from the backend.
- [x] Booking/admin view reads booking records from the backend.

### Date Holds and Overlap Rules

- [x] Date overlap blocking is implemented.
- [x] Pending booking holds are implemented.
- [x] Hold expiration timestamp is returned by the quote and booking flows.
- [x] Backend prevents conflicting bookings for overlapping dates.
- [x] Calendar/read booking view surfaces booking status and date state for local operations.

### eKYC Verification Flow

- [x] eKYC-required booking status flow is implemented.
- [x] Booking creation can start with verification required.
- [x] Backend stores eKYC provider metadata and transaction id.
- [x] Backend stores eKYC transaction URL when available.
- [x] eKYC pass handling is implemented.
- [x] eKYC reject handling is implemented.
- [x] Booking verification result endpoint validates the provider reference.
- [x] Guest dashboard and booking/admin views display verification status.
- [x] Production eKYC provider credentials and signatures are guarded as production configuration work.

### Payment Authorization and Capture

- [x] Stripe-style payment authorization flow is implemented locally.
- [x] Booking records store payment provider, authorization reference, client secret, capture reference, and payment status.
- [x] Payment capture endpoint is implemented.
- [x] Payment capture is blocked until booking approval or verification completion allows capture.
- [x] Payment capture after approval is implemented.
- [x] Payment confirmation route is present in the frontend.
- [x] Payment confirmation/capture flow is represented in the UI.
- [x] Production Stripe live keys and webhook validation remain production configuration work.

### Guest, Host, Calendar, and Admin Views

- [x] Guest dashboard is implemented.
- [x] Host dashboard is implemented.
- [x] Host property management page is implemented.
- [x] Booking/admin view is implemented.
- [x] Calendar/read booking view is implemented.
- [x] Payment confirmation page is implemented.
- [x] Admin dashboard reads core platform data and milestone status.

## Milestone 2 Checklist - Badges, Pricing, Campaigns, and Founding Benefits

### Badge Definitions

- [x] Badge definition API is implemented.
- [x] Badge definitions are persisted.
- [x] Frontend admin dashboard reads badge definitions.
- [x] Free host badge definition is present.
- [x] Verified host badge definition is present.
- [x] Trusted host badge definition is present.
- [x] Wellness host badge definition is present.
- [x] Badge definitions include key, level, subject type, annual price, currency, and unlocked features.

### Badge Eligibility

- [x] Badge eligibility endpoint is implemented.
- [x] Verified badge eligibility checks are implemented.
- [x] Trusted badge eligibility checks are implemented.
- [x] Wellness badge eligibility checks are implemented.
- [x] Missing eligibility requirements are returned to the frontend.
- [x] Trusted badge requires an active Verified badge.
- [x] Eligibility checks are exposed in Admin.

### Badge Purchases and Assignments

- [x] Badge purchase endpoint is implemented.
- [x] Badge assignment persistence is implemented.
- [x] Badge assignment list endpoint is implemented.
- [x] Badge assignments include badge key, level, subject type, subject id, status, earned date, paid-through date, expiration date, amount charged, currency, payment status, payment reference, and unlocked features.
- [x] Badge purchase requires successful payment input for paid tiers.
- [x] Duplicate active badge assignment protection is implemented.
- [x] Frontend admin controls can purchase and inspect badge assignments.

### Feature Locking and Unlocking

- [x] Badge feature access endpoint is implemented.
- [x] Active badge level is resolved for a subject.
- [x] Unlocked features are returned by the backend.
- [x] Locked features are returned by the backend.
- [x] Frontend admin controls can inspect feature access for a host or subject.

### Expiration, Suspension, and Renewals

- [x] Admin-protected badge expiration endpoint is implemented.
- [x] Admin-protected badge suspension endpoint is implemented.
- [x] Renewal list endpoint is implemented.
- [x] Renewal payment endpoint is implemented.
- [x] Renewal payment flow updates renewal payment state.
- [x] Frontend admin controls expose expiration, suspension, renewal list, and renewal payment actions.

### Pricebook

- [x] Badge pricebook list endpoint is implemented.
- [x] Badge pricebook item endpoint is implemented.
- [x] Admin-protected pricebook update endpoint is implemented.
- [x] Pricebook items include key, label, amount, currency, cadence, applies-to target, configurable flag, active flag, and active date range.
- [x] Frontend admin pricebook controls read current pricebook values.
- [x] Frontend admin pricebook controls can update configurable values locally with the admin token.

### Campaigns

- [x] Campaign list endpoint is implemented.
- [x] Admin-protected campaign creation endpoint is implemented.
- [x] Campaign enrollment endpoint is implemented.
- [x] Campaign records include key, name, campaign type, override amount, applies-to target, open date, close date, and active flag.
- [x] Campaign enrollment records include campaign key, subject type, subject id, and enrollment timestamp.
- [x] Frontend admin controls can create campaigns.
- [x] Frontend admin controls can enroll a subject in a campaign.
- [x] Frontend admin dashboard lists live campaigns.

### Founding Benefits

- [x] Admin-protected founding benefit upsert endpoint is implemented.
- [x] Founding benefit read endpoint is implemented.
- [x] Founding benefit records include property id, tier, guest flat fee, host commission percent, lifetime guest fee flag, transferability flag, and forfeiture flag.
- [x] Founding tiers are implemented.
- [x] Founding benefit eligibility handling is implemented.
- [x] Frontend admin controls can save and load founding benefits.

### Founding Transfer Evaluation

- [x] Founding transfer evaluation endpoint is implemented.
- [x] Previous owner verification requirement is checked.
- [x] Previous owner Trusted status requirement is checked.
- [x] Property id requirement is checked.
- [x] Current tax receipt requirement is checked.
- [x] Missing transfer requirements are returned to the frontend.
- [x] Frontend admin controls expose transfer evaluation.

### Commission Quote Calculation

- [x] Commission quote endpoint is implemented.
- [x] Commission quote supports booking value, nights, and founding tier.
- [x] Host commission percent is calculated.
- [x] Host commission amount is calculated.
- [x] Guest fee amount is calculated.
- [x] NestyStay revenue is calculated.
- [x] Frontend admin controls expose commission quote calculation.

### Admin Protection

- [x] Admin mutation endpoints use bearer-token protection.
- [x] Local development admin token is supported.
- [x] Pricebook update is admin protected.
- [x] Campaign creation is admin protected.
- [x] Founding benefit writes are admin protected.
- [x] Badge expiration is admin protected.
- [x] Badge suspension is admin protected.
- [x] Production startup guard requires production admin token hash configuration.

## Local Platform Hardening and QA

- [x] PostgreSQL-backed persistence is implemented for milestone workflows.
- [x] Entity Framework persistence layer is wired through Infrastructure.
- [x] Backend health endpoint is implemented.
- [x] Backend OpenAPI is enabled for development.
- [x] Backend schema read views are implemented.
- [x] Backend business rules read views are implemented.
- [x] Backend jobs read views are implemented.
- [x] Frontend Vite proxy is connected to the backend API.
- [x] Frontend API client covers the main Milestone 1 and Milestone 2 read and mutation flows.
- [x] Production startup guards exist for missing provider secrets.
- [x] Production webhook shared-secret requirement is implemented.
- [x] .NET vulnerable package scan was clean after local fixes.
- [x] npm audit was clean after local fixes.
- [x] Frontend build passed locally.
- [x] Backend tests passed locally.
- [x] API smoke tests passed through the Vite proxy during local QA.
- [x] Browser desktop and mobile smoke passes were completed during local QA.

## Demo and Proof Artifacts

- [x] Full HD Milestones 1 and 2 slideshow-style video: `artifacts/nestystay-full-hd-60fps-milestones-1-2-demo.mp4`.
- [x] Real browser 60fps Milestones 1 and 2 walkthrough video: `artifacts/nestystay-real-60fps-milestones-1-2-walkthrough.mp4`.
- [x] Reusable slideshow recorder script: `artifacts/record-full-hd-60fps-milestones-1-2.mjs`.
- [x] Reusable real browser recorder script: `artifacts/record-real-60fps-milestones-1-2.mjs`.

## Still Not Production Done

- [ ] Configure real Stripe live keys.
- [ ] Configure real Stripe webhook validation.
- [ ] Configure real Alibaba Cloud eKYC credentials.
- [ ] Configure real eKYC webhook/provider signatures.
- [ ] Configure Cloudflare R2 credentials and upload signing.
- [ ] Configure InsuraGuest API credentials and integration details.
- [ ] Set production `NESTYSTAY_ADMIN_TOKEN_SHA256`.
- [ ] Set production `NESTYSTAY_WEBHOOK_SHARED_SECRET`.
- [ ] Apply production database hosting.
- [ ] Apply production database backups.
- [ ] Apply production migration workflow.
- [ ] Configure production domain.
- [ ] Configure production SSL.
- [ ] Configure production hosting and deployment pipeline.
- [ ] Configure real email provider.
- [ ] Configure real SMS provider.
- [ ] Configure real push notification provider if required.
- [ ] Add real public admin user and role management if required.
- [ ] Add production observability, structured logging, and alerting.
- [ ] Complete legal, privacy, payment, insurance, and compliance review.
