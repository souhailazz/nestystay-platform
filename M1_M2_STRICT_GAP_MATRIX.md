# M1/M2 Strict Gap Matrix

Audit date: 2026-07-23

Baseline commit: `5465ba77308c17245cfa06b1a29f231104e037b9`

Branch: `audit/m1-m2-remediation`

Remediation commit: `214f623ef6dd71e3564f9744d7a3cf3d05f886ef`

CI evidence: Not run in this session. Command evidence below is local only.

Primary source: `NestyStay_Complete_Figma_Spec_v2.docx` found at `C:\Users\Administrator\AppData\Local\Microsoft\Olk\Attachments\ooa-b2703ee8-368e-47a2-a0d9-08a0052ffb86\c5a161b89a7abe719d056c6449117c3898bfebfa50f68589ce66a0e2f9e60405\NestyStay_Complete_Figma_Spec_v2.docx`.

Important: prior reports (`M1_M2_FULL_COMPLETION_REPORT.md`, `M1_M2_ROUTE_INVENTORY.md`, `M1_M2_API_INVENTORY.md`, `artifacts/m1_m2_spec_inventory.json`) were treated as inputs only, not proof.

## Audit Scope And Evidence Rules

PASS means every specified element and interaction is implemented, connected, authorized where required, tested, and has visual evidence.

PARTIAL means some real implementation exists, but at least one required element, interaction, state, authorization rule, test, or screenshot is missing.

FAIL means the implementation is generic, static, disconnected, insecure, materially different from the DOCX, or not present.

No audited screen receives PASS in this phase because the repository does not contain screen-by-screen visual evidence, frontend tests, Playwright tests, or complete protected interaction coverage for every DOCX screen.

## Global Evidence

| Field | Evidence |
| --- | --- |
| Frontend router | `frontend/src/App.tsx` contains hand-rolled route parsing and maps many DOCX routes to `CompletionPages.tsx`, `ProductPages.tsx`, and `SpecScreens.tsx`. |
| Generic completion pages | `frontend/src/pages/CompletionPages.tsx` contains broad family renderers such as `BookingSpecStatePage`, `TravelerSpecPage`, `MessagesPage`, `HostSpecPage`, and `AdminOpsSpecPage`. |
| Static spec screens | `frontend/src/pages/SpecScreens.tsx` contains many static screens with local arrays, fake charts, and buttons without backend actions. |
| API client | `frontend/src/lib/api.ts` exposes booking, spec, traveler, messaging, directory, host, and admin calls. Booking list/detail/create/capture and protected spec calls now accept/pass bearer tokens. |
| Backend booking API | Baseline exposed booking list/detail/create/verification/capture without `[Authorize]`. This pass added claim-based access checks to `BookingsController`, admin-only direct verification-result access, authenticated booking creation from principal, and host/admin payment capture. |
| Local bearer tokens | Baseline accepted `local-phase1-token-{userId}` and `local-google-token-{userId}` by parsing GUIDs. This pass removed that parser and added signed `nsty.v1.*` bearer tokens. |
| Admin token | Baseline frontend defaulted to `dev-admin-token` and stored it in `localStorage`. This pass removed those defaults/persistence and changed tracked recording scripts to require `NESTYSTAY_ADMIN_TOKEN`. |
| Auth secrets in responses | `frontend/src/lib/api.ts` models `twoFactorCode` in register/login responses. `ProductPages.tsx` displays the demo/local milestone code. |
| 2FA implementation | `CompletionPages.tsx` shows a `NESTY-2FA` placeholder div, not a real `otpauth://` URI, QR code, TOTP verification, or one-time recovery-code flow. |
| Messaging upload | `CompletionPages.tsx` creates a PDF attachment when message text contains `pdf`; backend stores attachment metadata JSON with no signed upload/download URL, MIME allowlist, upload progress, or 10 MB validation. |
| Host analytics | `EfSpecCompletionStore.BuildAnalytics` returns hardcoded revenue/occupancy/ADR series except booking count. |
| Tests | Backend Debug/Release tests pass with API test count now 17. Frontend `typecheck`, `lint`, `test`, and `build` now pass locally. Playwright E2E and per-screen tests are still missing. |
| Visual evidence | Existing screenshots are broad QA artifacts, not one desktop/tablet/mobile screenshot per DOCX screen. Missing for all rows below unless explicitly noted. |

## Required Command Results

| Command | Result |
| --- | --- |
| `dotnet restore backend\NestyStay.sln` | Exit 0. Output: `All projects are up-to-date for restore.` |
| `dotnet build backend\NestyStay.sln` | First run exit 1 due to locked `NestyStay.Api (12252)` DLLs. After stopping local API process, exit 0: `Build succeeded. 0 Warning(s) 0 Error(s)`. |
| `dotnet test backend\NestyStay.sln` | Exit 0. Passed: Domain 5, Application 16, Infrastructure 5, Api 17. |
| `dotnet build backend\NestyStay.sln -c Release` | Exit 0. `Build succeeded. 0 Warning(s) 0 Error(s)`. |
| `dotnet test backend\NestyStay.sln -c Release` | Exit 0. Passed: Domain 5, Application 16, Infrastructure 5, Api 17. |
| `dotnet list backend\NestyStay.sln package --vulnerable --include-transitive` | Exit 0. All listed projects report no vulnerable packages. |
| `cd frontend; npm ci` | Earlier rerun exit 0 after stopping a stale Vite/Node lock on `lightningcss.win32-x64-msvc.node`. Subsequent dependency update added Vitest/ESLint tooling. |
| `cd frontend; npm audit` | Exit 0. `found 0 vulnerabilities`. |
| `cd frontend; npm run typecheck` | Exit 0. `tsc -b`. |
| `cd frontend; npm run lint` | Exit 0. `eslint "src/**/*.{ts,tsx}"`. |
| `cd frontend; npm test` | Exit 0. Vitest 1 file / 3 tests passed. |
| `cd frontend; npm run build` | Exit 0. `tsc -b && vite build`, 2016 modules transformed. |
| `cd frontend; npx playwright test` | Not rerun after remediation; no Playwright E2E suite exists in the repository. |

## Public Experience Screens

| Field | PUB-01 | PUB-02 | PUB-03 | PUB-04 | PUB-05 | PUB-06 | PUB-07 | PUB-08 | PUB-09 | PUB-10 | PUB-11 | PUB-12 | PUB-13 | PUB-14 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Screen name | Homepage | Search results / Listing grid | Map search view | Property listing detail | Experiences listing | Sorting view | No results state | Experience detail | About / Trust page | Help center | Blog / Journal | Contact page | Terms of service | Privacy policy |
| Required elements | Hero search, destinations, carousel, trust, prelaunch strip | Responsive property cards, filters, sort | Map pins, hover updates, drawer | Gallery, counts, host card, amenities, expandable copy, rules, calendar, map, cancellation, price sidebar/bar, 119 | Patois tagline, experience cards, category/parish filters | Sort drawer and active sort pill | No-results CTAs and 3 suggestions | Provider, description, included, pricing, calendar, booking, badges, messaging | Story, pillars, founder bio | Patois returning greeting, searchable FAQ, SLA | Image cards, category, date, read time, featured/related/article | Contact form, WhatsApp | Florida law, required sections, Jamaica addendum | GDPR/CCPA, retention, no sale, officer reset deletion |
| Route | `/` | `/explore` | `/explore/map` | `/properties/{id}` | `/experiences` | None distinct | `/explore` empty branch | `/experiences/{slug}` | `/about`, `/trust` | `/help`, `/help/*` | `/journal`, `/blog` | `/contact` | `/terms` | `/privacy` |
| Frontend source | `App.tsx` `LandingPage`; landing components | `ProductPages.tsx` `ExplorePage` | `SpecScreens.tsx` `MapSearchPage` | `ProductPages.tsx` `PropertyDetailsPage` | `CompletionPages.tsx` `ExperiencesPage` | Missing distinct component | `ProductPages.tsx` `ExplorePage` empty state | `CompletionPages.tsx` `ExperienceDetail` | `CompletionPages.tsx` `PublicContentRoute` | `CompletionPages.tsx` `PublicContentRoute` | `CompletionPages.tsx` `JournalPage` | `CompletionPages.tsx` `ContactForm` | `CompletionPages.tsx` `PublicContentRoute` | `CompletionPages.tsx` `PublicContentRoute` |
| Backend dependencies | Partial property/search only | `GET /api/properties` | None real map API | `GET /api/properties/{id}` | `GET /api/spec/experiences` | None | `GET /api/properties` | `GET /api/spec/experiences/{slug}` | `GET /api/spec/public/pages/{slug}` | `GET /api/spec/public/pages/{slug}` | `GET /api/spec/journal` | `POST /api/spec/public/contact` | `GET /api/spec/public/pages/terms` | `GET /api/spec/public/pages/privacy` |
| Persistence | Property seed/API | Property tables | None; static pins | Property tables | `MilestoneExperiences` | None | Property tables | `MilestoneExperiences` | `MilestonePublicContentPages` | `MilestonePublicContentPages` | `MilestoneJournalArticles` | `MilestoneContactRequests` | `MilestonePublicContentPages` | `MilestonePublicContentPages` |
| Authentication | Public | Public | Public | Public | Public | Public | Public | Public | Public | Public | Public | Public | Public | Public |
| Ownership rules | N/A | N/A | N/A | N/A | N/A | N/A | N/A | N/A | N/A | N/A | N/A | N/A | N/A | N/A |
| Actions tested | No screen tests | No frontend tests | No tests | No full booking tests | Backend list tests only | Not tested | Not tested | Backend detail tested indirectly | Not tested | One backend help article test | Backend journal query test | Backend contact validation test | Not tested | Not tested |
| Loading state | Missing evidence | Present `LoadingState` | Missing | Present | Present | Missing | N/A | Present | Present | Present | Present | Form only | Present | Present |
| Empty state | Missing evidence | Present but not spec-complete | Missing | Missing | Present generic | Missing | Present but missing suggestions | Missing | Missing | Missing | Missing | N/A | Missing | Missing |
| Error state | Missing evidence | Present generic | Missing | Present generic | Present generic | Missing | Missing | Present generic | Present generic | Present generic | Present generic | Present generic | Present generic | Present generic |
| Mobile state | No per-screen proof | No per-screen proof | No per-screen proof | No per-screen proof | No per-screen proof | No per-screen proof | No per-screen proof | No per-screen proof | No per-screen proof | No per-screen proof | No per-screen proof | No per-screen proof | No per-screen proof | No per-screen proof |
| Visual evidence | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing |
| Automated tests | None | None | None | None | Broad backend only | None | None | Broad backend only | None | `PublicContentExperiencesJournalAndAuthFlowsArePersistedAndValidated` | Same broad test | Same broad test | None | None |
| Status | PARTIAL | PARTIAL | FAIL | PARTIAL | PARTIAL | FAIL | PARTIAL | PARTIAL | PARTIAL | PARTIAL | PARTIAL | PARTIAL | PARTIAL | PARTIAL |
| Gaps | Search/destination/prelaunch exactness and visual proof missing | Missing full filter set/sort and responsive proof | Static map, no hover/pin API | Missing gallery/lightbox/counts/sidebar/bar/full quote | Missing full filters, booking persistence | No distinct sort drawer | Missing required suggested properties | Missing persisted booking/schedules/reviews | Content generic | SLA/search/returning greeting incomplete | No featured image/read time/featured related article proof | Works partially; no screen tests | Content not fully verified against DOCX | Content not fully verified against DOCX |

## Authentication Screens

| Field | AUTH-01 | AUTH-02 | AUTH-03 | AUTH-04 | AUTH-05 | AUTH-06 | AUTH-07 | AUTH-08 | AUTH-09 | AUTH-10 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Screen name | Login | Register role | Register email + password | Social auth consent | Email verification | Phone verification | OTP entry | Forgot password | Reset password success | 2FA setup |
| Required elements | Whaah Gwaan, email/password, social auth, Yuh Gud toast | Come Een role cards/social | Full name/email/password visibility/confirm/terms/success | Provider consent and role selection | 6-digit OTP/link, Respek success | International phone, Jamaica default | Six auto-advance inputs, 5-min timer, resend | Email, 30-min reset link, patois helper | New/confirm password, expired/invalid states, success | Real TOTP QR, otpauth URI, valid code before enable, recovery confirmation |
| Route | `/login`, `/auth/post-login-toast` | `/auth/role`, `/register?role=*` | `/register` | `/auth/social-consent` | `/auth/email-verification` | `/auth/phone-verification` | `/auth/otp` | `/auth/forgot-password` | `/auth/reset-password` | `/auth/2fa-setup`, `/auth/recovery-codes` |
| Frontend source | `ProductPages.tsx` `AuthPage`; `SpecScreens.tsx` toast | `CompletionPages.tsx` `AuthSpecFlowPage`; `AuthPage` mode toggle | `ProductPages.tsx` `AuthPage` | `CompletionPages.tsx` `AuthSpecFlowPage` | Same | Same | Same | Same | Same | `RecoveryCodesPanel` |
| Backend dependencies | `POST /api/auth/login`; `POST /api/auth/2fa/verify`; social config | Partial | `POST /api/auth/register` | `GET /api/spec/auth/social-config`; `POST /api/auth/google` | `POST /api/spec/auth/flows` | Same | Same | Same | Same | `POST /api/spec/auth/{userId}/recovery-codes` |
| Persistence | Users/challenges | None for role | Users/challenges | Users or config | `MilestoneAuthFlows` | `MilestoneAuthFlows` | `MilestoneAuthFlows` | `MilestoneAuthFlows` | `MilestoneAuthFlows` | `MilestoneRecoveryCodes` |
| Authentication | Public, then signed user session | Public | Public | Public | Public | Public | Public | Public | Public | Traveler/host via signed session token |
| Ownership rules | Signed principal exists for protected user paths; OTP/code exposure remains | N/A | N/A | Provider fallback issue | Flow not owner-bound | Flow not owner-bound | Flow not owner-bound | Flow not owner-bound | Flow not owner-bound | Signed principal id match only; recovery-code storage still plaintext |
| Actions tested | Broad backend login tests | No frontend tests | Broad backend register tests | Social config test | Broad flow test | Broad flow test | Broad flow test | Broad flow test | Broad flow test | Recovery generation test |
| Loading state | Missing evidence | Missing | Missing | Generic | Generic | Generic | Generic | Generic | Generic | Missing |
| Empty state | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing |
| Error state | Generic | Missing | Generic | Generic | Generic | Generic | Generic | Generic | Generic | Generic |
| Mobile state | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing |
| Visual evidence | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing |
| Automated tests | Backend broad tests | None | Backend broad tests | Backend config only | Broad flow test | Broad flow test | Broad flow test | Broad flow test | Broad flow test | Broad recovery test |
| Status | FAIL | PARTIAL | FAIL | FAIL | FAIL | FAIL | FAIL | FAIL | FAIL | FAIL |
| Gaps | Demo 2FA code returned/displayed; no Patois hero in real login; no Apple/Facebook flows | Role UI exists but not complete registration flow | No confirm password/terms/show-hide; response exposes codes | No real OAuth consent | Codes returned by API | No international verification provider | No six inputs/timer/resend behavior | No email delivery/reset link | No expired/invalid states | Placeholder QR, no TOTP, recovery codes not hashed/consumed |

## Booking Screens

| Field | BOOK-01 | BOOK-02 | BOOK-03 | BOOK-04 | BOOK-05 | BOOK-06 | BOOK-07 | BOOK-08 | BOOK-09 | BOOK-10 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Screen name | Guest selection popup | Booking confirmation | Checkout card payment | Payment success | Payment failure | Booking rejected | Booking pending | Booking cancelled | Invoice | Receipt |
| Required elements | Property image/name, dates, adult/child steppers, accessibility, protection, quote recalc | Guest/property/dates/breakdown/billing country/terms | Stripe Elements, encrypted badge | Irie confirmation, number, details, host notification | Dutty Tough, 15-min hold, retry/find | Rejected no charge, similar stays | Nuh Fret, clock, host reply/hold countdown | Refund status/timeline | Invoice detail/download | Receipt amount/date/method/download |
| Route | Modal from `/explore` or `/properties/{id}` | `/booking/{id}/review` | `/booking/{id}/checkout` | `/booking/{id}/success` | `/booking/{id}/failure` | `/booking/{id}/rejected` | `/booking/{id}/pending` | `/booking/{id}/cancelled` | `/booking/{id}/invoice` | `/booking/{id}/receipt` |
| Frontend source | `components/booking/BookingModal.tsx` | `CompletionPages.tsx` `BookingSpecStatePage` | Same | Same | Same | Same | Same | Same | Same | Same |
| Backend dependencies | `POST /api/bookings/quote`, `POST /api/bookings` | `GET /api/bookings` | `POST /api/bookings/{id}/capture-payment` | Same | Same | Same | Same | Same | `GET /api/bookings` only | `GET /api/bookings` only |
| Persistence | `MilestoneBookings` and pricing lines | `MilestoneBookings` | Payment gateway records | Booking/payment fields | Booking/payment fields | Booking status | Booking status | Booking status | Booking data only | Booking data only |
| Authentication | Signed user session required; server overrides submitted `guestUserId` | Signed traveler/host/admin session required for booking API data | Capture requires admin role or host ownership | Private | Private | Private | Private | Private | Private | Private |
| Ownership rules | Booking creation derives guest from principal | Claim-filtered traveler/host/admin access | Host/admin capture enforced; Stripe Elements still missing | Missing screen-level route proof | Missing screen-level route proof | Missing screen-level route proof | Missing screen-level route proof | Missing screen-level route proof | Missing invoice authorization/download proof | Missing receipt authorization/download proof |
| Actions tested | Backend workflow broad tests | None frontend | Backend capture tests only | Backend capture tests only | None | None | None | None | None | None |
| Loading state | Quote loading partial | Present generic | Present generic | Present generic | Present generic | Present generic | Present generic | Present generic | Present generic | Present generic |
| Empty state | Missing | Generic | Generic | Generic | Generic | Generic | Generic | Generic | Generic | Generic |
| Error state | Generic | Generic | Generic | Generic | Generic | Generic | Generic | Generic | Generic | Generic |
| Mobile state | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing |
| Visual evidence | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing |
| Automated tests | Broad backend booking tests | Broad backend only | Broad backend only | Broad backend only | None | None | None | None | None | None |
| Status | FAIL | FAIL | FAIL | FAIL | FAIL | FAIL | FAIL | FAIL | FAIL | FAIL |
| Gaps | Incomplete popup; adult/child steppers/accessibility/protection details missing | Generic state card and missing visual/frontend proof | No Stripe Elements; capture auth improved but checkout remains generic | Generic | Generic | Generic | Generic | Generic | No real download | No real download/payment method source |

## Traveler Screens

| Field | TRAV-01 | TRAV-02 | TRAV-03 | TRAV-04 | TRAV-05 | TRAV-06 | TRAV-07 | TRAV-08 | TRAV-09 | TRAV-10 | TRAV-11 | TRAV-12 | TRAV-13 | TRAV-14 | TRAV-15 | TRAV-16 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Screen name | Dashboard overview | Trip suggestions | Upcoming reservations | Past reservations | Cancelled reservations | Reservation detail | Wishlist saved stays | Favorite collections | Payment methods | Payment history | Invoices | Profile settings | Identity verification | Preferences | Reviews given | Pending reviews + notifications |
| Required elements | Welcome/KPIs/recent/notifications | Matched stays | Booking cards + prepare CTA | Completed/review/rebook | Cancel reason/refund | Breakdown/host contact/check-in/cancellation/QR | Save/unsave availability | Create/rename/delete/drag order | Stripe SetupIntents/cards/default/remove | Filters/download receipts | Year/bulk download | Photo/profile/preferences/Patois/delete | Status/doc/renewal/eKYC | Preferences/accessibility persistence | Published/edit 48h/host replies | Countdown, notification read/bulk |
| Route | `/guest-dashboard` | `/traveler/suggestions` | `/traveler/reservations/upcoming` | `/traveler/reservations/past` | `/traveler/reservations/cancelled` | `/traveler/reservations/{id}`, `/traveler/qr` | `/traveler/favorites`, `/wishlist` | Static `/traveler/favorites` plus spec workspace | `/traveler/payment-methods` | `/traveler/payments` | `/traveler/invoices` | `/profile` | `/traveler/identity` | `/traveler/preferences` | `/traveler/reviews/given` | `/traveler/reviews/pending`, `/notifications` |
| Frontend source | `ProductPages.tsx` `GuestDashboardPage` | `SpecScreens.tsx` `TripSuggestionsPage` | `CompletionPages.tsx` `TravelerSpecPage` | Same | Same | Same | Same | `SpecScreens.tsx` and `TravelerSpecPage` | `TravelerSpecPage` | Same | `SpecScreens.tsx` `InvoicesPage` | `ProductPages.tsx` `ProfileSettingsPage` | `TravelerSpecPage` | Same | Same | `SpecScreens.tsx`; `TravelerSpecPage` |
| Backend dependencies | Bookings/properties partially | None static | `GET /api/bookings?guestUserId=` | Same | Same | Same | `/api/spec/traveler/{userId}` | Wishlist endpoints | Payment method endpoint | Traveler workspace only | Static screen | Auth/profile partial | Traveler workspace only | Traveler workspace only | Review endpoints | Notification endpoints |
| Persistence | Booking/property seed | None | `MilestoneBookings` | `MilestoneBookings` | `MilestoneBookings` | `MilestoneBookings`/messages | Wishlist tables | Wishlist tables | Payment method metadata only | None real transaction table | None real invoice files | User/profile partial | None eKYC detail | None preference table beyond local/spec | Reviews | Reviews/notifications |
| Authentication | Traveler | Public/static | Traveler via signed session token and claim-filtered booking API | Same | Same | Same | Traveler via signed session token | Traveler via signed session token | Traveler via signed session token | Traveler via signed session token | Static | Traveler | Traveler | Traveler | Traveler | Traveler |
| Ownership rules | Booking API claim filtering improved; screen proof still missing | N/A | Booking API no longer trusts query ID for list/detail | Same | Same | Same | Signed principal id match | Same | No Stripe ownership | Weak | Missing | Partial | Missing | Partial | Review eligibility missing | Partial |
| Actions tested | Backend broad | None | None | None | None | None | Broad wishlist backend | Broad collection backend | Broad payment metadata backend | None | None | None | None | None | Broad review backend | Broad notification backend |
| Loading state | Partial | Missing | Present | Present | Present | Present | Present | Present | Present | Present | Missing | Partial | Present | Present | Present | Partial |
| Empty state | Partial | Missing | Generic | Generic | Generic | Generic | Generic | Static | Generic | Generic | Missing | Missing | Generic | Generic | Generic | Static |
| Error state | Partial | Missing | Generic | Generic | Generic | Generic | Generic | Missing | Generic | Generic | Missing | Generic | Generic | Generic | Generic | Missing |
| Mobile state | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing |
| Visual evidence | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Missing |
| Automated tests | Broad backend only | None | None | None | None | None | Broad backend | Broad backend | Broad metadata only | None | None | None | None | None | Broad backend | Broad backend |
| Status | PARTIAL | FAIL | FAIL | FAIL | FAIL | FAIL | PARTIAL | PARTIAL | FAIL | FAIL | FAIL | PARTIAL | FAIL | PARTIAL | PARTIAL | PARTIAL |
| Gaps | Uses limited data and no full interactions | Static suggestions | Booking privacy not protected | Missing eligibility/rebook | Missing refund logic | Missing QR gate details | No real save/unsave proof | No drag/drop proof | Random last4/UI metadata, no SetupIntent | No transactions/filters/download | Static downloads | No photo upload/persistence proof | No eKYC integration | Partial preferences | No eligibility enforcement/edit tests | Partial read behavior/countdowns |

## Host, Host Profile, Directory, Messaging, Admin, And Error Screens

| Screen ID | Screen name | Required elements | Route | Frontend source | Backend dependencies | Persistence | Authentication | Ownership rules | Actions tested | Loading state | Empty state | Error state | Mobile state | Visual evidence | Automated tests | Status | Gaps |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| HOST-01 | Revenue dashboard | Revenue, occupancy, guest love, tasks, chart, top properties, payouts, CSV | `/host-dashboard` | `ProductPages.tsx` `HostDashboardPage` | Mixed booking/properties | Mixed/seed | Host | Weak | Broad backend | Partial | Missing | Partial | Missing | Missing | Broad backend | PARTIAL | Analytics incomplete; export not proven |
| HOST-02 | Analytics | Occupancy, ADR, views, conversion, origin map, export | `/host/analytics` | `CompletionPages.tsx` `HostSpecPage` | `/api/spec/host/{id}/operations` | Milestone host ops | Host signed session token | Signed principal id match only | Broad backend | Present | Missing | Generic | Missing | Missing | Broad backend | FAIL | Hardcoded analytics; no views/origin/date filters |
| HOST-03 | Properties list | Cards/status/badge/occupancy/next booking/Patois empty | `/host/properties` | `ProductPages.tsx` `PropertyManagementPage` | `/api/properties` | Properties | Host | Missing create owner enforcement | Some backend | Partial | Partial | Partial | Missing | Missing | Broad | PARTIAL | Ownership and exact empty state not proven |
| HOST-04 | Archived properties | Restore/delete confirmation | `/host/properties/archived` | `CompletionPages.tsx` `HostSpecPage`; `SpecScreens.tsx` partial | Spec/static | Milestone/spec | Host | Missing property-owner proof | None | Generic | Missing | Generic | Missing | Missing | None | FAIL | No real archive/restore/delete flow |
| HOST-05 | Property creation wizard | 10-step wizard, photos, pricing, availability, verification toggle, publish toast | `/host/properties` | `ProductPages.tsx` `PropertyManagementPage` | `POST /api/properties` | Properties | Host | Frontend submits host IDs | Some backend | Partial | Missing | Partial | Missing | Missing | Broad | FAIL | Not full wizard; file upload/ownership absent |
| HOST-06 | Property editing | Inline wizard sections, draft/publish, history | `/host/properties/edit` | `SpecScreens.tsx` `HostPropertyEditPage` | Property API partial/static | Mostly static | Host | Missing | None | Missing | Missing | Missing | Missing | Missing | None | FAIL | Static edit sections; no persisted inline save/history |
| HOST-07 | Seasonal pricing | Calendar overlays, seasons, min stay | `/host/pricing` | `CompletionPages.tsx` `HostSpecPage` | `POST /api/spec/host/{id}/pricing-rules` | `MilestoneHostPricingRules` | Host signed session token | Does not verify property belongs to host | Broad backend | Present | Missing | Generic | Missing | Missing | Broad backend | PARTIAL | No real calendar/delete/edit; property ownership missing |
| HOST-08 | Promotions | Discount/dates/min nights/badge/last-minute | `/host/promotions` | `HostSpecPage` | `POST /api/spec/host/{id}/promotions` | `MilestoneHostPromotions` | Host signed session token | Does not verify property belongs to host | Broad backend | Present | Missing | Generic | Missing | Missing | Broad backend | PARTIAL | Limited create only; no edit/delete/toggle proof |
| HOST-09 | Reservation management | Table statuses, approve/reject, iCal | `/bookings` | `ProductPages.tsx` `BookingManagementPage` | Booking endpoints | Bookings | Host/admin signed session | Booking API ownership improved; screen/route proof still incomplete | Broad backend | Partial | Partial | Partial | Missing | Missing | Broad | PARTIAL | Booking endpoint auth improved; iCal not proven |
| HOST-10 | Reports | Monthly summary, PDF/CSV | `/host/reports` | `SpecScreens.tsx` `HostReportsPage` | None/static | None | Host | Missing | None | Missing | Missing | Missing | Missing | Missing | None | FAIL | Static; no generated PDF/CSV |
| HOST-11 | Exports | Booking/revenue/guest/tax exports/date range | `/host/exports` | `CompletionPages.tsx` `HostSpecPage` | Spec ops generic | None real files | Host | Missing | None | Generic | Missing | Generic | Missing | Missing | None | FAIL | Metric cards only, no exports |
| HOST-12 | Reviews management | Replies, flagging, averages, ownership | `/host/reviews` | `CompletionPages.tsx` `HostSpecPage` | `POST /api/spec/host/{id}/reviews/{reviewId}/reply` | Reviews | Host signed session token | Does not verify review/property ownership | Broad backend | Present | Missing | Generic | Missing | Missing | Broad backend | FAIL | Cross-property reply possible; flagging/averages incomplete |
| HOST-13 | Badge/account settings | Badge unlocks, Trusted CTA, verification toggle, founding status, renewal, 2FA | `/host/badges`, `/host/settings` | `HostSpecPage`, `ProductPages.tsx` badge area | Badge pricing APIs | Badge tables | Host/admin mixed | Partial | Broad backend | Partial | Missing | Partial | Missing | Missing | Broad | PARTIAL | Account settings/renewal/founding not fully screen-tested |
| HPRO-01 | Verified host profiles | DOCX references ID but no readable row; inferred directory | `/hosts` | `CompletionPages.tsx` `HostProfileSpecPage` | `/api/spec/host-profiles` | `MilestoneHostProfiles` | Public | N/A | None | Present | Missing | Generic | Missing | Missing | None | PARTIAL | Spec-source incomplete; public directory partial |
| HPRO-02 | Host profile detail inferred | Biography/trust/badges/listings/reviews/response/visibility | `/hosts/{slug}` | `HostProfileDetail` | `/api/spec/host-profiles/{slug}` | `MilestoneHostProfiles` | Public | N/A | None | Present | Missing | Generic | Missing | Missing | None | PARTIAL | Listings/reviews not rendered; Link Mi only goes to inbox |
| HPRO-03 | Host profile preview inferred | Draft/unsaved preview | `/host/profile/preview` | `HostProfileSpecPage` | `/api/spec/host-profiles/my-host-profile` | Host profiles | Host/public mixed | Hardcoded slug | None | Present | Generic | Generic | Missing | Missing | None | FAIL | Preview relies on hardcoded slug and not draft form data |
| HPRO-04 | Host profile editor inferred | Save actual entered values | `/host/profile/edit` | `HostProfileEditor` | `PUT /api/spec/host-profiles/{slug}` | Host profiles | Host signed session token | Actor matches request HostUserId but slug ownership weak on existing profile | None | Missing | Missing | Generic | Missing | Missing | None | FAIL | Inputs use defaults; save ignores entered values |
| HPRO-05 | Host contact | Link Mi contact opening/creating conversation | `/hosts/{slug}` | `HostProfileDetail` | None for creating host conversation | None | Public/traveler | Missing | None | Present | Missing | Generic | Missing | Missing | None | FAIL | `Contact host` links to `/messages`; no host-specific conversation creation |
| DIR-01 | Custodian directory | Verified hosts, cleaner providers, platform messaging | `/directory/custodians` | `DirectorySpecPage` | `/api/spec/directories/providers` | Directory providers | Should be verified host; not enforced | Missing provider-owner rules | Broad backend | Present | Generic | Generic | Missing | Missing | Broad backend | PARTIAL | Access gating and messaging request not complete |
| DIR-02 | Trades directory | Trusted hosts, trade/parish/badge filters | `/directory/trades` | `DirectorySpecPage` | Same | Same | Should be Trusted host; not enforced | Missing | Broad backend | Present | Generic | Generic | Missing | Missing | Broad | PARTIAL | Filter UI incomplete; access not enforced |
| DIR-03 | Local business listing | Categories, featured Trusted placement | `/directory/businesses` | `DirectorySpecPage` | Same | Same | Verified hosts/guests; not enforced | Missing | Broad backend | Present | Generic | Generic | Missing | Missing | Broad | PARTIAL | Featured placement and role gates incomplete |
| DIR-04 | Police directory restricted | Wellness hosts only, officer badge IDs, no names/contact | M2 table includes DIR-01-06 but DOCX row is M4; app maps provider onboarding as DIR-04 incorrectly | `DirectorySpecPage`/`PoliceDirectoryPage` | Wellness APIs partly | Wellness/officer | Wellness host/admin; not fully enforced | Partial | Wellness tests only | Mixed | Missing | Mixed | Missing | Missing | Wellness tests | FAIL | Route/component mapping confused; access not proven |
| DIR-05 | Provider dashboard | Profile/categories/parishes/availability/messages/badge/earnings | `/directory/provider`, `/directory/provider/onboarding` | `DirectorySpecPage` `ProviderPortal` | Upsert provider | Directory providers | Any local user token | No provider ownership model | Broad backend | Present | Generic | Generic | Missing | Missing | Broad | PARTIAL | Provider can upsert slug; earnings/messages incomplete |
| DIR-06 | Guest verification upsell | Host property toggle, pricing, guest pays nothing | `/directory/guest-verification` and host property UI | `DirectorySpecPage`, `VerificationToggle` | Spec provider/static | Mostly static | Host | Missing property owner enforcement | None | Present | Missing | Generic | Missing | Missing | None | PARTIAL | Pricing shown but no billing/package logic |
| MSG-01 | Inbox | Sort, name/badge/last/timestamp/unread, filters/search | `/messages` | `CompletionPages.tsx` `MessagesPage` | `/api/spec/messages/inbox` | Conversations/messages | Signed session token | Participant check after signed principal id | Broad backend | Present | Generic | Generic | Missing | Missing | Broad | PARTIAL | No filters/search UI despite requirement |
| MSG-02 | Conversation list detail | Booking reference, archived tab | `/messages/{id}` | `MessagesPage` | Conversation API | Conversations | Signed session token | Participant check | Broad | Present | Generic | Generic | Missing | Missing | Broad | PARTIAL | No archived tab; booking context minimal |
| MSG-03 | Chat view | Timestamps, read status, typing, booking context | `/messages/{id}` | `ConversationPanel` | Message APIs | Messages | Signed session token | Participant check | Broad send/read | Present | Generic | Generic | Missing | Missing | Broad | PARTIAL | No typing/presence polling; limited context |
| MSG-04 | Media sharing | Photo upload/thumbs/10 MB/WebP | `/messages/{id}` | `ConversationPanel` | Metadata only | JSON attachments | Signed session token | Participant check | PDF metadata only | Present | Missing | Generic | Missing | Missing | Broad | FAIL | No file upload, progress, retry, MIME/size validation |
| MSG-05 | Document sharing | PDF secure 24h links | `/messages/document` | `SpecScreens.tsx` and `ConversationPanel` | Metadata only | JSON attachments | Signed session token/static | Missing signed URL ownership | Metadata only | Missing | Missing | Missing | Missing | Missing | Broad | FAIL | No signed upload/download or expiry |
| MSG-06 | Online status online | Mi Deh Yah consenting hosts | `/messages` | `MessagesPage` | Conversation participant status | Participant row | Signed session token | Partial | None | Present | Generic | Generic | Missing | Missing | None | PARTIAL | Consent not modeled |
| MSG-07 | Offline status | Offline and response time | `/messages` | `MessagesPage` | Conversation participant status | Participant row | Signed session token | Partial | None | Present | Generic | Generic | Missing | Missing | None | PARTIAL | Last-seen behavior not real |
| MSG-08 | Read receipts | Read timestamp vs delivered | `/messages/{id}` | `ConversationPanel` | Mark read API | Participant/message rows | Signed session token | Participant check | Broad backend | Present | Generic | Generic | Missing | Missing | Broad | PARTIAL | UI shows status but not complete read receipt behavior |
| MSG-09 | Support thread | Automated/human support, undeletable | `/messages` | `MessagesPage` | Seed support thread | Conversations | Signed session token | Partial | Seed broad | Present | Generic | Generic | Missing | Missing | Broad | PARTIAL | Delete prevention not exposed/tested |
| ADM-01 | Admin dashboard | KPI/activity feed | `/admin` | `ProductPages.tsx` `AdminPage` | Admin/spec APIs | Admin cases/audit | Admin token | Admin policy only | Broad | Partial | Missing | Partial | Missing | Missing | Broad | PARTIAL | Static/generic metrics and no domain models |
| ADM-02 | Platform KPIs | Trend charts/export | `/admin/kpis` | `SpecScreens.tsx` `AdminKpiPage` | None/static | None | Admin expected but not enforced by route | Missing | None | Missing | Missing | Missing | Missing | Missing | None | FAIL | Static fake charts; no protected API/export |
| ADM-03 | User management | Search/filter/suspend/reinstate/delete/identity/audit | `/admin/ops/users` | `AdminOpsSpecPage` | Generic admin cases | `MilestoneAdminCases` | Admin bearer token entered by user/session state | Admin policy for API | Broad generic | Present | Generic | Generic | Missing | Missing | Broad | FAIL | Generic case, no user model actions |
| ADM-04 | Property moderation | Approve/suppress/remove/flag history | `/admin/ops/properties` expected; actual map uses `moderation` only for ADM-04 | `AdminOpsSpecPage` | Generic admin cases | `MilestoneAdminCases` | Admin | Admin policy | Broad generic | Present | Generic | Generic | Missing | Missing | Broad | FAIL | Route mapping wrong for `/properties`; no property moderation model |
| ADM-05 | Reservations overview | Timeline/override reason/audit | `/admin/ops/reservations` | `AdminOpsSpecPage` | Generic cases | Cases/audit | Admin | Admin policy | Broad generic | Present | Generic | Generic | Missing | Missing | Broad | FAIL | No reservation override service |
| ADM-06 | Payments & transactions | Stripe refunds/payout/reconciliation | `/admin/ops/refunds` required, current `payments` map | `AdminOpsSpecPage` | Generic cases | Cases/audit | Admin | Admin policy | Broad generic | Present | Generic | Generic | Missing | Missing | Broad | FAIL | Route mapping and refund domain missing |
| ADM-07 | Disputes | Evidence, full/partial/dismiss | `/admin/ops/disputes` | `AdminOpsSpecPage` | Generic cases | Cases/audit | Admin | Admin policy | Broad generic | Present | Generic | Generic | Missing | Missing | Broad | FAIL | No evidence uploads/refund decisions |
| ADM-08 | Support tickets | SLA/assign/internal notes/closure | `/admin/ops/support` | `AdminOpsSpecPage` | Generic cases | Cases/audit | Admin | Admin policy | Broad generic | Present | Generic | Generic | Missing | Missing | Broad | FAIL | No support-ticket model/SLA |
| ADM-09 | Reports & exports | Reports/compliance/scheduled email | `/admin/ops/audit`, `/admin/reports` | `AdminOpsSpecPage`, `AdminReportsPage` | Generic audit/static | Cases/audit | Admin/static | Partial | Broad audit | Mixed | Generic | Generic | Missing | Missing | Broad | FAIL | Route mapping wrong for audit; scheduled reports static |
| ADM-10 | Fraud detection | Risk scoring, ban/whitelist/case | `/admin/ops/fraud` | `AdminOpsSpecPage` | Generic cases | Cases/audit | Admin | Admin policy | Broad generic | Present | Generic | Generic | Missing | Missing | Broad | FAIL | No risk scoring/ban/whitelist |
| ADM-11 | Flagged users/properties | Resolve clear/suspend/remove | `/admin/ops/flagged` | `AdminOpsSpecPage` | Generic cases | Cases/audit | Admin | Admin policy | Broad generic | Present | Generic | Generic | Missing | Missing | Broad | FAIL | No flagged domain model/actions |
| ERR-01 | 401 login required | Sign in required, login, return safely | `/401` | `SpecScreens.tsx` `SignInRequiredPage` | None | None | Public | N/A | None | N/A | N/A | N/A | Missing | Missing | None | PARTIAL | Route exists but no auth-route tests/screenshots |
| ERR-02 | 403 restricted | Access restricted, no missing permission detail | `/403` | `AccessRestrictedPage` | None | None | Public | N/A | None | N/A | N/A | N/A | Missing | Missing | None | PARTIAL | Copy says permission; violates no-specific-permission intent |
| ERR-03 | 404 | Drifting page, return/search | `/404`, fallback | `NotFoundPage` | None | None | Public | N/A | None | N/A | N/A | N/A | Missing | Missing | None | PARTIAL | Patois error copy not centralized; screenshots missing |
| ERR-04 | 500 | Team notified, data protected, reference code | `/500` | `ServerErrorPage` | None | None | Public | N/A | None | N/A | N/A | N/A | Missing | Missing | None | PARTIAL | No reference code evidence |
| ERR-05 | Maintenance | Back shortly, bookings preserved, WhatsApp | `/maintenance` | `PublicContentRoute` | Public page | Content page | Public | N/A | None | Present | Missing | Generic | Missing | Missing | None | PARTIAL | Content generic, no standalone verified state |
| ERR-06 | Loading skeleton | Tek Time, shimmer, never blank | Component only | `LoadingState` | N/A | N/A | N/A | N/A | None | Present globally | N/A | N/A | Missing | Missing | None | PARTIAL | No 1200ms shimmer/screen tests |
| ERR-07 | No properties | Create listing/explore CTA | Component branches | `EmptyState` usages | Varies | Varies | Varies | Varies | None | N/A | Partial | N/A | Missing | Missing | None | PARTIAL | Exact state not consistently mapped |
| ERR-08 | No favorites | Save a stay copy | `/empty/favorites` | `NoFavoritesPage` | None | None | Public | N/A | None | N/A | Present | N/A | Missing | Missing | None | PARTIAL | No route auth/mobile test |
| ERR-09 | Empty inbox | Inbox clear copy | `/messages` empty branch | `MessagesPage` | Message API | Conversations | Signed session token | Partial | None | Present | Present | Generic | Missing | Missing | None | PARTIAL | No no-message test |
| ERR-10 | No reservations | Explore/adjust dates | `/empty/reservations` | `NoReservationsPage` | None/static | None | Public | N/A | None | N/A | Present | N/A | Missing | Missing | None | PARTIAL | No protected route/filter integration |

## Critical Security Findings From Phase 1

1. Booking private data and mutations were unauthenticated at baseline. This pass added claim-based booking list/detail/create/capture protection and admin-only direct verification-result access.
2. Booking creation trusted `guestUserId` from the frontend at baseline. This pass overrides it from the authenticated principal.
3. Predictable local bearer tokens were accepted at baseline. This pass removes runtime acceptance of `local-phase1-token-*` and `local-google-token-*` and adds signed session tokens.
4. Admin token handling was unsafe in the frontend at baseline. This pass removes the `dev-admin-token` default and `localStorage` persistence.
5. OTP and 2FA secrets are still returned and displayed. Register/login responses expose codes, auth flow responses expose codes/tokens, and the UI instructs users to use demo/local codes.
6. TOTP is not implemented. The UI placeholder is not a real QR/URI, and recovery codes are generated with GUID-derived values and returned in plaintext.
7. Messaging file upload is metadata-only. There is no upload endpoint, allowlist validation, size enforcement, signed URL, expiry, progress, or retry.
8. Several ownership requirements are still missing or incomplete: host review replies do not verify property ownership, pricing/promotions do not verify property ownership, provider profile upsert lacks provider ownership, and property creation/editing still need host ownership enforcement.
9. Admin operations are generic cases rather than domain-specific user/property/refund/dispute/support/fraud services.
10. Visual and frontend automated evidence is missing for all DOCX screens.

## Phase 1 Verdict

M1/M2 are not complete. The implementation contains useful scaffolding and some persisted milestone data, but the strict DOCX criteria are not met. Critical security blockers must be fixed before visual/page completion can be credibly claimed.
