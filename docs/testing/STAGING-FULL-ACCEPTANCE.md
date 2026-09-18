# NestyStay Staging Full Acceptance Test

Date: 2026-09-18  
Environment: `https://staging.nestystay.net/`  
Scope: black-box/client-side testing only. No application code, database, server, environment variable, provider, or Git state was changed during this audit.

## Executive result

**STAGING STATUS: PARTIAL**

The public marketplace is reachable and the main discovery surfaces render. Explore returns six seeded properties, badge filtering and sorting work, the map loads, property detail pages render, booking quote calculation works, and unauthenticated protected-route redirects work. The deployed build is not ready for end-to-end client acceptance because the health endpoints return 404, the booking quote still displays Alibaba Cloud eKYC text, homepage search popovers do not render their controls, property text search does not narrow results, invalid QR validation returns 404, and privileged M1–M5 workflows were not certifiable without staging role accounts.

Staging also returns `X-Robots-Tag: noindex, nofollow`, which is appropriate for a staging environment. However, `/robots.txt` and `/sitemap.xml` incorrectly return the SPA HTML with `Content-Type: text/html` instead of their respective formats.

## Pre-flight evidence

| URL | Result | Evidence |
|---|---|---|
| `/` | PASS, HTTP 200, TLS/HTTPS reachable | `00-public/home-desktop.png` |
| `/api/health/live` | FAIL, HTTP 404 | `network/preflight.txt` |
| `/api/health/ready` | FAIL, HTTP 404 | `network/preflight.txt` |
| `/api/properties` | PASS, HTTP 200 JSON; six seeded properties returned | `network/preflight.txt` |
| `/robots.txt` | FAIL, HTTP 200 but HTML and `X-Robots-Tag: noindex, nofollow` | `network/preflight.txt` |
| `/sitemap.xml` | FAIL, HTTP 200 but HTML and `X-Robots-Tag: noindex, nofollow` | `network/preflight.txt` |

## Acceptance matrix

Result values: PASS, FAIL, PARTIAL, BLOCKED, NOT_APPLICABLE.  
Blocked means the workflow could not be honestly certified because credentials, external provider configuration, or test data were unavailable.

| ID | Milestone | Role | Route | Workflow | Result | Evidence | Root cause | Severity |
|---|---|---|---|---|---|---|---|---|
| PRE-01 | Platform | Guest | `/` | HTTPS homepage load | PASS | `00-public/home-desktop.png` | None observed | LOW |
| PRE-02 | Platform | System | `/api/health/live` | Live health endpoint | FAIL | `network/preflight.txt` | Deployed route returned 404 | HIGH |
| PRE-03 | Platform | System | `/api/health/ready` | Ready health endpoint | FAIL | `network/preflight.txt` | Deployed route returned 404 | HIGH |
| PRE-04 | Platform | Guest | `/api/properties` | Public property API | PASS | `network/preflight.txt` | None observed | LOW |
| PRE-05 | SEO/config | Crawler | `/robots.txt` | Robots document | FAIL | `network/preflight.txt` | SPA fallback served HTML | HIGH |
| PRE-06 | SEO/config | Crawler | `/sitemap.xml` | Sitemap document | FAIL | `network/preflight.txt` | SPA fallback served HTML | HIGH |
| PUB-01 | Public | Guest | `/` | Hero, navigation, imagery, CTA | PASS | `00-public/home-desktop.png`, `mobile/home.png` | None observed | LOW |
| PUB-02 | Public | Guest | `/` | Search CTA opens Explore with guest count | PASS | Browser trace | CTA navigated to `/explore?adults=2` | LOW |
| PUB-03 | Public | Guest | `/` | Destination/date/guest popovers | FAIL | Browser trace | Buttons changed to expanded state but no picker/input content appeared in the DOM | MEDIUM |
| PUB-04 | Public | Guest | `/explore` | Marketplace grid and seeded cards | PASS | `M1-core-booking/explore-desktop.png`, `mobile/explore.png` | None observed | LOW |
| PUB-05 | M1 | Guest | `/explore` | Badge filters Free/Verified/Trusted | PASS | Browser trace | Counts and visible cards changed correctly | LOW |
| PUB-06 | M1 | Guest | `/explore` | Wellness filter empty state | PASS | Browser trace | Clear empty-state message rendered | LOW |
| PUB-07 | M1 | Guest | `/explore` | Location/property text search | FAIL | Browser trace | Entering `Kingston` or an unmatched string left all six cards visible; the value appeared in the local filter but results did not narrow | HIGH |
| PUB-08 | M1 | Guest | `/explore` | Sorting by price | PASS | Browser trace | Prices reordered `$110`, `$140`, `$175`... | LOW |
| PUB-09 | M1 | Guest | `/explore` | Unauthenticated favorite | PASS | Browser trace | Redirected to login instead of mutating state | LOW |
| PUB-10 | M1 | Guest | `/explore/map` | Interactive map/list discovery | PASS | `00-public/tablet-explore.png` plus browser trace | OpenStreetMap iframe and six live stays rendered | LOW |
| PUB-11 | M1 | Guest | `/properties/{id}` | Property detail/gallery/host/badge/pricing | PASS | `M1-core-booking/property-detail-desktop.png`, `mobile/property-detail.png` | None observed | LOW |
| PUB-12 | M1 | Guest | `/properties/{id}` | Booking modal opens | PASS | `M1-core-booking/booking-quote-alibaba-text.png` | Login requirement is clearly shown | LOW |
| PUB-13 | M1 | Guest | `/properties/{id}` | Quote calculation and fees | PASS | `M1-core-booking/booking-quote-alibaba-text.png` | Total and fee breakdown rendered | LOW |
| PUB-14 | M1 | Guest | `/properties/{id}` | Provider messaging in quote | FAIL | `M1-core-booking/booking-quote-alibaba-text.png` | UI still says `Alibaba Cloud eKYC verification required` despite the stated Stripe Identity requirement | HIGH |
| PUB-15 | Public | Guest | `/experiences` | Experiences list/search surface | PASS | Browser trace | Three seeded experiences rendered | LOW |
| PUB-16 | Public | Guest | `/experiences/{slug}` | Experience detail/request surface | PASS | Browser trace | Detail and request controls rendered | LOW |
| PUB-17 | Public | Guest | `/trust` | Trust content | PARTIAL | Browser trace | Content loads, but page heading is level 2 and title is generic `Public content · NestyStay` | MEDIUM |
| PUB-18 | Public | Guest | `/help` | Help/FAQ content | PARTIAL | Browser trace | Content loads, but page heading is level 2 and title is generic | MEDIUM |
| PUB-19 | Public | Guest | `/contact` | Contact/WhatsApp link | PASS | Browser trace | Link rendered | LOW |
| PUB-20 | Public | Guest | `/journal` | Journal landing | PASS | Browser trace | Page rendered with H1 | LOW |
| PUB-21 | Legal | Guest | `/terms` | Terms page | PARTIAL | Browser trace | Content loads, but title is generic `Public content · NestyStay` | MEDIUM |
| PUB-22 | Legal | Guest | `/privacy` | Privacy page | PARTIAL | Browser trace | Content loads, but title is generic `Public content · NestyStay` | MEDIUM |
| PUB-23 | Legal | Guest | `/cookies` | Cookie policy | FAIL | `errors/cookies-404.png` | Route resolves to the 404 page | HIGH |
| PUB-24 | Legal | Guest | `/refund-policy` | Refund policy | FAIL | Browser trace | Route resolves to the 404 page | HIGH |
| AUTH-01 | Auth | Guest | `/login` | Login form and Google/2FA/passwordless entry points | PASS | `01-auth/login-desktop.png` | UI entry points present | LOW |
| AUTH-02 | Auth | Guest | `/login` | Invalid login error | PASS | Browser trace | User-facing `Invalid email or password.` alert rendered | LOW |
| AUTH-03 | Auth | Guest | `/register` | Registration fields and account roles | PASS | Browser trace | Guest/Host/PM/Owner/Officer/Provider/Business options present | LOW |
| AUTH-04 | Auth | Guest | `/register` | Invalid registration validation | PARTIAL | Browser trace | Form did not submit, but no clear inline validation message was visible after invalid data | MEDIUM |
| AUTH-05 | Auth | Guest | `/login` | Password reset entry | PASS | Browser trace | Reset email form rendered | LOW |
| AUTH-06 | Auth | Guest | `/login` | Successful login, 2FA, recovery, Google callback, passwordless delivery | BLOCKED | None | No dedicated staging account/OTP/provider callback was supplied | HIGH |
| AUTH-07 | Authorization | Anonymous | `/host-dashboard`, `/host/wellness`, `/admin`, `/messages` | Protected-route behavior | PASS | Browser trace | Redirected to sign-in with return URL | LOW |
| M1-01 | M1 | Guest | Booking flow | Persisted booking creation | BLOCKED | Booking modal evidence | Requires authenticated guest account | HIGH |
| M1-02 | M1 | Host | Host dashboard | Approve/reject with persisted reason | BLOCKED | None | No host account supplied; no authenticated browser session | HIGH |
| M1-03 | M1 | Guest | Booking flow | Stripe Identity success/failure/webhook | BLOCKED | Quote evidence | No safe Stripe test identity session was run; deployed quote still references Alibaba | HIGH |
| M1-04 | M1 | Guest | Payment | Payment authorization/capture/refund/webhook | BLOCKED | None | No authenticated booking and payment test credentials | HIGH |
| M1-05 | M1 | Guest | Trips/notifications | Trip status, unread count, deep link | BLOCKED | `/trips` was not a known deployed route; no guest session | HIGH |
| M2-01 | M2 | Guest | `/explore` | Free/Verified/Trusted public badge appearance | PASS | Explore screenshots/browser trace | Public cards and filters render these seeded levels | LOW |
| M2-02 | M2 | Guest | `/explore` | Wellness badge public result | PARTIAL | Browser trace | Filter exists, but it returned zero stays | MEDIUM |
| M2-03 | M2 | Host/Admin | Protected badge screens | Eligibility, purchase, renewal, suspension, admin management | BLOCKED | None | No host/admin test accounts and no authenticated staging session | HIGH |
| M3-01 | M3 | Guest | `/experiences` | Public wellness-adjacent experience listing | PASS | Browser trace | Experience list/detail surfaces render | LOW |
| M3-02 | M3 | Officer/Host/Admin | Protected wellness workflows | Onboarding, approval, assignment, report, payout | BLOCKED | None | Missing role accounts and external payment/provider certification | HIGH |
| M4-01 | M4 | Guard | `/gate` | Gate interface and manual QR fallback | PASS | `M4-directories-qr/gate-desktop.png` | UI rendered and token input enabled | LOW |
| M4-02 | M4 | Guard | `/gate` | Invalid QR handling | FAIL | `errors/qr-invalid-404.png` | Validation request returned HTTP 404 instead of a domain-level invalid-token response | HIGH |
| M4-03 | M4 | Provider/Admin | Directory onboarding/moderation | BLOCKED | `M4-directories-qr/directory-404.png` | Obvious `/directory` route is 404 and no public directory link/account was available | HIGH |
| M4-04 | M4 | Guard/Admin | QR lifecycle and scan history | BLOCKED | None | No valid seeded QR token, guard account, or authenticated admin session | HIGH |
| M5-01 | M5 | PM/Owner | Property manager | Dashboard, owners, property scope | BLOCKED | None | No PM/owner account or reachable PM route was available from the public UI | CRITICAL |
| M5-02 | M5 | PM/Owner | Finance/maintenance/utilities | Core operations and owner isolation | BLOCKED | None | Missing role accounts and test data | CRITICAL |
| M5-03 | M5 | PM/Owner/Staff | Documents/community/gate/subscriptions | Operational workflows | BLOCKED | None | Missing role accounts, provider configuration, and test data | HIGH |
| INS-01 | InsuraGuest | Host/PM/Admin | Protected insurance screens | Plan catalog, policy lifecycle, claims | BLOCKED | None | No authenticated role account; no real insurer certification permitted | HIGH |
| CAL-01 | Calendar | Host/PM | Protected calendar | Month view/manual blocks/ICS sync | BLOCKED | None | No authenticated host/PM account or safe external ICS fixture | HIGH |
| RESP-01 | Responsive | Guest | Public routes | Desktop 1440×1000 | PASS | Desktop evidence set | Key public pages captured | LOW |
| RESP-02 | Responsive | Guest | Explore | Tablet 1024×768 | PASS | `00-public/tablet-explore.png` | Layout remained usable with six-card grid | LOW |
| RESP-03 | Responsive | Guest | Public routes | Mobile 390×844 | PARTIAL | `mobile/home.png`, `mobile/explore.png`, `mobile/property-detail.png` | Public views captured; authenticated mobile operational flows remain untested | MEDIUM |
| A11Y-01 | Accessibility | Guest | Public routes | Basic labels, image alt, heading scan | PARTIAL | Browser DOM checks | No missing `alt` attributes or obvious horizontal overflow; Help/Trust use level-2 page headings and full screen-reader/keyboard certification was not performed | MEDIUM |

## Bug reports

### BUG-STG-001 — Health endpoints return 404

- Severity: HIGH
- Milestone: Platform readiness
- Role: System/monitoring
- Route: `/api/health/live`, `/api/health/ready`
- Viewport: Not applicable
- Preconditions: Public staging host reachable
- Steps:
  1. Open `https://staging.nestystay.net/api/health/live`.
  2. Open `https://staging.nestystay.net/api/health/ready`.
- Expected: HTTP 200 health responses.
- Actual: HTTP 404 for both endpoints.
- Likely category: STAGING_CONFIGURATION or BACKEND_ROUTE.
- Reproducible: YES.

### BUG-STG-002 — Homepage search popovers expand without controls

- Severity: MEDIUM
- Milestone: M1 discovery
- Role: Guest
- Route: `/`
- Steps:
  1. Open the homepage.
  2. Click `Where to?`, `Check in — Check out`, or `Guests`.
- Expected: Destination input, calendar, or guest stepper appears.
- Actual: The button receives `expanded` state, but no picker content appears in the visible DOM or screenshot.
- Likely category: FRONTEND_BUG.
- Reproducible: YES.

### BUG-STG-003 — Explore location search does not narrow results

- Severity: HIGH
- Milestone: M1 discovery
- Role: Guest
- Route: `/explore`
- Steps:
  1. Open Explore.
  2. Enter `Kingston` in `Search by location, parish, or property name`.
  3. Click `Search`.
- Expected: Results are filtered to matching location/parish/property data.
- Actual: All six cards remain visible, including Montego Bay and Ocho Rios cards. The text appears in the local filter field but does not change the result set.
- Likely category: FRONTEND_BUG or search-query wiring.
- Reproducible: YES.

### BUG-STG-004 — Deployed booking quote still exposes Alibaba eKYC

- Severity: HIGH
- Milestone: M1 identity verification
- Role: Guest
- Route: `/properties/22222222-2222-4222-8222-222222222222`
- Steps:
  1. Open the Kingston Business Stay detail page.
  2. Click `Book this stay`.
  3. Click `Get quote`.
- Expected: The configured Stripe Identity flow is named consistently.
- Actual: Quote text says `Alibaba Cloud eKYC verification required` while the detail page separately says eKYC required and the stated product decision is Stripe Identity.
- Likely category: STAGING_CONFIGURATION or stale FRONTEND/BACKEND deployment.
- Reproducible: YES.

### BUG-STG-005 — Cookie and refund policy URLs are 404

- Severity: HIGH
- Milestone: Legal/client readiness
- Role: Guest
- Routes: `/cookies`, `/refund-policy`
- Expected: Published policy pages.
- Actual: Both render the 404 page `Dis page gone a sea`.
- Likely category: FRONTEND_ROUTE or missing content deployment.
- Reproducible: YES.

### BUG-STG-006 — Invalid QR validation returns 404

- Severity: HIGH
- Milestone: M4 QR/Gate
- Role: Gate guard
- Route: `/gate`
- Preconditions: None; use harmless token `invalid-test-token`.
- Steps:
  1. Open Gate validator.
  2. Enter `invalid-test-token`.
  3. Click `Validate access`.
- Expected: A domain-level invalid, expired, revoked, or malformed-token message.
- Actual: Alert says `Request failed with status 404`.
- Likely category: BACKEND_ROUTE or STAGING_CONFIGURATION.
- Reproducible: YES.

### BUG-STG-007 — Robots and sitemap paths fall through to SPA HTML

- Severity: HIGH for public release; staging `noindex` itself is expected.
- Milestone: SEO/deployment
- Routes: `/robots.txt`, `/sitemap.xml`
- Expected: Plain-text robots rules and XML sitemap with appropriate content types.
- Actual: HTTP 200 HTML SPA response with `X-Robots-Tag: noindex, nofollow`.
- Likely category: STAGING_CONFIGURATION or reverse-proxy/static-file routing.
- Reproducible: YES.

### BUG-STG-008 — Generic titles on several public legal/support pages

- Severity: MEDIUM
- Milestone: SEO/public quality
- Routes: `/trust`, `/help`, `/terms`, `/privacy`
- Expected: Page-specific titles and a primary H1.
- Actual: Title is `Public content · NestyStay`; Trust/Help/Terms/Privacy expose level-2 page headings in the observed DOM.
- Likely category: FRONTEND_SEO/accessibility.
- Reproducible: YES.

## What is working

- HTTPS staging is reachable and the public SPA loads.
- `/api/properties` returns six seeded properties as JSON.
- Homepage hero, navigation, imagery, Explore CTA, and public sections render.
- Explore cards show title, location, bedroom/guest metadata, host badge, nightly price, estimated total, Details, Book, and Save controls.
- Free, Verified, and Trusted filters change the visible result count.
- Wellness filter produces a clear zero-results state.
- Price sorting reorders cards correctly.
- Map/list discovery loads an OpenStreetMap iframe and six live stays.
- Unauthenticated Save redirects to login rather than silently mutating state.
- Property detail loads gallery images, host/badge information, verification messaging, cancellation copy, pricing, and booking entry.
- Booking modal opens and quote totals/fees render.
- Experience list and detail pages render.
- Invalid login produces a user-facing error.
- Registration exposes the expected role choices and consent checkboxes.
- Password reset entry screen renders.
- Anonymous access to Host, Wellness, Admin, and Messages routes is denied with a sign-in requirement.
- Public desktop, tablet Explore, mobile homepage, mobile Explore, and mobile property detail screenshots were captured.
- Basic DOM checks found no missing `alt` attributes on the sampled pages and no obvious horizontal overflow at the captured public layouts.

## What is not working

- Health endpoints return 404.
- Homepage search popovers do not display their controls.
- Explore location/property search does not narrow results.
- Deployed quote contains Alibaba Cloud eKYC text.
- Invalid QR validation returns 404 instead of a domain-level invalid-token result.
- Cookie and refund-policy routes are 404.
- Robots and sitemap endpoints are not serving their required file formats.
- Several public pages use a generic SEO title and lack a level-1 page heading.

## What is blocked

- Successful guest registration/login/2FA/recovery/Google/passwordless flows: no dedicated staging test account, OTP, or callback session was supplied.
- Persisted bookings, host approval/rejection, guest-visible rejection, Stripe Identity, payment authorization/capture/refund, and notifications: require authenticated role accounts and safe test provider credentials.
- M2 admin badge lifecycle and Stripe-backed badge payment: requires admin/host credentials and test Stripe configuration.
- M3 officer onboarding, assignment, reports, payouts, and privacy workflows: requires role accounts and provider configuration.
- M4 directory onboarding/moderation and valid/expired/revoked QR lifecycle: requires provider/admin/guard accounts and seeded tokens.
- M5 PM/owner/staff dashboard, finance, maintenance, utilities, documents, community, subscriptions, and owner-isolation tests: requires dedicated PM/owner/staff accounts and test data.
- InsuraGuest: external insurer certification was not attempted and must remain blocked until safe staging credentials/test mode are confirmed.
- Real Stripe Identity, Connect payouts, email/SMS delivery, external ICS feeds, and physical camera scanning were not certified.

## Final client-style status

| Area | Status |
|---|---|
| STAGING | PARTIAL |
| M1 Core Booking | PARTIAL |
| M2 Badges | PARTIAL |
| M3 Wellness | BLOCKED |
| M4 Directories + QR | PARTIAL |
| M5 Property Manager | BLOCKED |
| InsuraGuest | BLOCKED |
| Calendar | BLOCKED |
| Responsive desktop | PASS for sampled public routes |
| Responsive tablet | PASS for sampled Explore route |
| Responsive mobile | PARTIAL |
| Authorization | PASS for anonymous protected-route checks; cross-role checks blocked |
| Basic accessibility | PARTIAL |

## Totals

The matrix contains 62 acceptance checks:

- Passed: 27
- Failed: 10
- Partial: 8
- Blocked: 15
- Not applicable: 0
- Critical bugs: 0 confirmed application bugs; M5 remains critically blocked for acceptance because PM/owner/staff accounts and data were unavailable
- High bugs: 6
- Medium bugs: 2
- Low bugs: 0

Counts are workflow-level acceptance results, not code-coverage claims. Blocked is not counted as pass.

## Release verdict

- **Staging functionally ready for client review:** NO — public browsing can be reviewed, but the release has material M1/M4/configuration failures and missing role coverage.
- **Staging ready for public launch:** NO — health endpoints, legal routes, robots/sitemap, provider wording, booking search, and privileged workflows are not certified.

Exact next actions for the deployment owner:

1. Restore `/api/health/live` and `/api/health/ready` with 200 responses through nginx.
2. Deploy the Stripe Identity build/config consistently and remove the Alibaba wording from the reachable frontend/backend responses.
3. Fix the homepage picker rendering and Explore location/property filtering.
4. Fix the Gate validation route/error mapping so malformed/expired/revoked tokens return a clear application result instead of 404.
5. Publish `/cookies` and `/refund-policy` and give Trust/Help/Terms/Privacy page-specific titles/H1s.
6. Serve real `robots.txt` and `sitemap.xml` files. Keep staging `noindex`; only remove it for the production domain after approval.
7. Provide dedicated staging credentials for guest, host, admin, officer, provider, guard, PM, owner, and staff roles, plus deterministic test data and safe Stripe/InsuraGuest/ICS test configuration.
8. Re-run this same acceptance matrix after deployment; do not treat blocked workflows as passed.

## Remediation pass — 2026-09-18

The existing staging findings were compared with the current source before making changes. The following source-level remediation was completed on the protected frontend feature branch:

- Public content SEO now uses page-specific titles for About, Trust, Help, Contact, Terms, Privacy, Cookies, and Refund Policy instead of allowing the generic route-manifest title to overwrite them.
- Registration now uses visible inline validation for display name, email, password requirements, password confirmation, Terms consent, and Privacy consent. The fields expose `aria-invalid` and `aria-describedby` when invalid.
- The current source already contains the health controller routes, Stripe Identity wording, homepage search popovers, Explore search/filter behavior, legal routes, and static robots/sitemap files. Their continued failure on staging should be treated as a stale deployment/proxy issue until the protected branch is deployed and the public endpoints are retested.

Local validation for this remediation:

- Frontend typecheck: PASS.
- Frontend unit tests: 48/48 PASS across 9 files.
- Frontend production build: PASS.
- Frontend lint: PASS with 0 errors; existing repository warnings remain.
- Local browser smoke: Terms page showed a page-specific H1/title; empty registration submission showed all expected inline errors.

Staging has not been re-certified after these changes because deployment requires the protected-branch review/merge workflow. Until that happens, the original staging failures remain open in this report and must not be marked resolved based on local results alone.
