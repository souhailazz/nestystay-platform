# NestyStay complete frontend / UX / responsive audit

**Audit date:** 2026-09-12
**Repository:** `C:\Users\Administrator\Desktop\nestystayPLATFORM`
**Scope:** frontend routes, screen composition, UX completeness, role coverage, responsive behavior, shared UI, API/static boundaries, and a future Figma-ready information architecture.
**Change policy followed:** analysis-only. Product source files were not changed and no commit was created. The only new file is this report.

## 1. Executive summary

NestyStay has broad frontend coverage and a visually coherent direction, but it is not yet a single product system. It is a hybrid of a strong design-system shell, live API-backed workflows, and a large set of spec/demo/legacy screens. The most important work is consolidation: one route authority, one implementation per workflow, one role-aware navigation model, and clear separation between production screens and sample screens.

The strongest product foundations are the deep/cream/yellow visual language, the shared workspace shell, the auth/booking/wellness/gate flows, typed API access, loading and empty-state primitives, and the host property workflow. The largest UX risks are the default-Guest shell on unauthenticated role routes, role-agnostic public navigation, a mobile bottom bar that only exposes the first five filtered items, PM modules that reuse one generic summary page, the current messages route rendering local sample threads, hard-coded host profiles/wishlists/reports, and a route/screen registry that is already out of sync.

### Exact inventory counts

| Measure | Count | Definition / evidence |
|---|---:|---|
| Typed route variants | **79** | Unique `Route` union variants in `frontend/src/App.tsx:76-170`. Several variants carry a `view`, `kind`, `state`, slug, or ID. |
| Parser branches | **119** | `if` branches in `parseRoute()` in `frontend/src/App.tsx:172-301`. |
| Unique quoted path entries | **131** | Unique path literals/prefixes in `parseRoute()`, including `/`, wildcards, and aliases. |
| Existing route evidence cases | **128** | `testing-evidence/final-hardening/07-browser/route-coverage.json`; 128 tested, 0 recorded failures, generated 2026-09-02. This is test evidence, not proof that every workflow is functionally complete. |
| Design-screen registry IDs | **57** | `implementedScreens` in `frontend/src/App.tsx:601-658`. |
| Registry IDs with normal mapping cases | **53** | `componentRouteForScreen()` in `frontend/src/App.tsx:661-790`. |
| Registry IDs without normal mapping cases | **4** | `INDEX`, `DIR-ADM`, `ADM-WELLNESS`, `ADM-BADGES`; `INDEX` is intentional, the other three are registry drift/defect risks. |
| Role values | **8** | `Guest`, `Host`, `Officer`, `ServiceProvider`, `LocalBusiness`, `Admin`, `PropertyManager`, `Owner` in `frontend/src/components/layout/WorkspaceFrame.tsx:47-74`. |
| Main page orchestration files | **5** | `frontend/src/pages/ProductPages.tsx`, `CompletionPages.tsx`, `SpecScreens.tsx`, `PropertyManagerPages.tsx`, and `PropertyManagerPmsPage.tsx`. |
| Largest page files | **183,072 / 144,207 / 127,662 bytes** | `ProductPages.tsx`, `CompletionPages.tsx`, `SpecScreens.tsx`; they are too large to remain the long-term route authority. |
| Clearly unreferenced inspected component bodies | **at least 9** | `ContactForm`, `TravelerWorkspaceView`, `MessagesWorkspace`, `HostProfileCard`, `HostProfileDetail`, `HostProfileEditor`, `PublicLanding`, `PropertyManagerUtilitiesPage`, and `PropertyManagerVerificationPage` have no current reference outside their defining files. |
| Booking modal implementations | **2** | `frontend/src/components/booking/BookingModal.tsx` and `frontend/src/features/booking/BookingModal.tsx`. |
| Existing responsive evidence routes | **12 per viewport** | `testing-evidence/final-hardening/17-mobile/responsive-*.json`: home, explore, login/register, four directories, gate QR, help, 401, 404. No PM dashboard, booking state, workspace table, or admin workflow is represented in those JSON snapshots. |
| Unit test result | **5 files / 35 tests passed** | Fresh `npm run test -- --run` on 2026-09-12. |
| Typecheck result | **Passed** | Fresh `npm run typecheck` on 2026-09-12. |

### API/static classification

At the **79 typed route-variant level**, classification is:

| Classification | Count | Meaning |
|---|---:|---|
| API-backed by default | **51** | The default route component makes one or more explicit `api.*` calls for its primary data or mutation path. |
| Hybrid / partial | **7** | A live shell or API call is mixed with hard-coded copy, local-only state, fallback sample data, or a reused route that does not match the route’s promise. |
| Static/spec/error | **21** | Deliberately static content, a spec/demo page, a controlled error/empty state, or a design reference. |

These counts are by typed render variant, not by every nested panel or every dynamic URL. A single `traveler-spec` variant contains multiple `view` screens; a single `booking-state` variant contains multiple booking states. The most meaningful conclusion is not the ratio: **many API-backed screens still contain static sub-flows, while several static screens are exposed on production-looking routes without an unmistakable demo boundary.**

### Priority conclusion

| Priority | Count | Audit conclusion |
|---|---:|---|
| P0 launch blockers | **6** | Routing/registry drift, incorrect role shell, PM workflow duplication/genericity, current messaging route using local samples, host profile route/data mismatch, and infrastructure/auth observability failure. |
| P1 before serious pilot | **10** | Mobile IA, public navigation, table/dialog behavior, static traveler/admin/host surfaces, error handling, and booking/calendar consistency. |
| P2 polish / scale | **4** | Token consolidation, analytics/charting, dead-code cleanup, and Figma/source parity. |
| Strong carry-forward screen families | **18** | Auth, core booking state, live traveler panels, directories, wellness, gate validation, subscription, and basic financial surfaces have the best foundations. |
| Screen families needing mobile IA/layout redesign | **23** | These need more than a breakpoint tweak because navigation, density, action priority, or interaction model changes on small screens. |
| Screen families needing complete product/UX redesign | **11** | The current screen is materially a placeholder, sample-only flow, or wrong implementation for the promised job. This overlaps with the mobile set. |

## 2. Audit method and constraints

### Inspected

- Route parsing, route-to-component dispatch, public navigation, role-aware workspace navigation, auth gate, error routes, and design-screen registry in `frontend/src/App.tsx`.
- Shared shell and navigation in `frontend/src/components/layout/PublicShell.tsx` and `frontend/src/components/layout/WorkspaceFrame.tsx`.
- Shared controls in `frontend/src/components/ui/ActionBar.tsx`, `Badge.tsx`, `Button.tsx`, `Card.tsx`, `EmptyState.tsx`, `ErrorState.tsx`, `Input.tsx`, `ListControls.tsx`, `LoadingState.tsx`, `Modal.tsx`, `PageHeader.tsx`, `PatoisToast.tsx`, and `StatusChip.tsx`.
- Live feature areas under `frontend/src/features/`: `admin`, `auth`, `booking`, `host`, `hostProfile`, `messaging`, `public`, and `traveler`.
- The page orchestration layers `ProductPages.tsx`, `CompletionPages.tsx`, `SpecScreens.tsx`, `PropertyManagerPages.tsx`, and `PropertyManagerPmsPage.tsx`.
- Existing test/evidence artifacts under `frontend/e2e`, `testing-evidence/milestones-1-5`, `testing-evidence/final-hardening`, and `docs/testing/ACCESSIBILITY-REPORT.md`.
- Fresh local checks: `npm run typecheck` and `npm run test -- --run`.
- Read-only browser inspection at 1440px for `/`, `/explore`, `/login`, and `/pm/dashboard`, plus existing responsive evidence at 360/390/1024/1440px.

### Runtime limitation

The local frontend dev server was available at `http://127.0.0.1:5173`. The local backend was listening at `http://localhost:5019`, but its PostgreSQL connection failed because no database password was provided for `nestystay_dev`. This produced the visible `Property API unavailable` state on the landing/explore surfaces and prevented authenticated API-state verification. It is itself a launch-readiness finding: environment failure is surfaced inconsistently and the product cannot be meaningfully tested end-to-end without a known-good seeded backend.

The existing accessibility report is useful implementation evidence, but it explicitly says it is not WCAG certification and that no axe/conformance score was claimed. The current package does include `@axe-core/playwright` as a dev dependency; the report should be updated only after an actual axe run and deployed keyboard/screen-reader pass.

## 3. Complete screen and route registry

The following is the current addressable registry, grouped by user goal. `API`, `HYB`, and `SPEC` use the classification above. `Reuse` means the route is rendered by another screen’s component rather than having a dedicated UX.

### Public discovery and content

| Route(s) | Typed variant / current implementation | Data | UX audit |
|---|---|---|---|
| `/` | `home` → `App.tsx:481` `LandingPage`, assembled from `components/landing/*` | HYB | Strong brand landing and clear scroll story. It contains a live `PropertyShowcase` that can expose a backend error inside an otherwise editorial page. Long scroll, motion, and dense hero imagery need performance and mobile hierarchy review. |
| `/explore` | `explore` → `ProductPages.tsx:212` → `PublicStateContainer` → `PublicSearchMap` | API | Good search/list foundation, filters, sort, pagination, saved affordance, and booking entry. Error handling currently collapses a failed property request into a no-results experience in `PublicSearchMap.tsx:34-45`; ratings are explicitly placeholder values at `PublicSearchMap.tsx:223`. |
| `/explore/map` | `map-search` → `SpecScreens.tsx` `MapSearchPage` | SPEC | Static illustrative map/list screen. It should not sit beside live explore without a clear “preview” distinction. Full map interaction, clustering, geolocation, map loading, and map error states are not evidenced. |
| `/properties/:propertyId` | `property` → `PropertyDetailPage` | API | Strong visual detail pattern and booking CTA. Invalid IDs fall back to the first returned property in the feature implementation, which is a trust-breaking behavior; the route needs a true not-found state. Save-to-wishlist is local-only in the current detail path. |
| `/experiences`, `/experiences/:slug` | `experiences` → `CompletionPages.tsx:557-627` | API | List/detail API shape exists, but the adjacent `features/public/ExperiencesPage.tsx` contains an `alert()` booking placeholder. Ensure only one implementation is reachable and that booking uses the real booking flow. |
| `/journal`, `/blog`, `/journal/:slug`, `/blog/:slug` | `journal` → `CompletionPages.tsx:627` | API | Good editorial route shape. `/blog` is an alias and should have canonical URL/metadata behavior. Detail loading, missing article, share, and related-content states need explicit QA. |
| `/about`, `/trust`, `/help`, `/help/*`, `/contact`, `/terms`, `/privacy`, `/maintenance` | `public-content` → `LegalHelpPages` | SPEC/static | Clear content coverage, but contact is currently a WhatsApp-style static CTA while a separate `ContactForm` exists in `CompletionPages.tsx:143` without a current caller. Choose one support/contact model. |
| `/coming-soon` | `coming-soon` → `SpecScreens.tsx` | SPEC | Presentational launch/countdown surface. Keep only if it is a deliberate marketing state; otherwise it is a placeholder route exposed as a product route. |

### Auth, invitation, and security

| Route(s) | Typed variant / current implementation | Data | UX audit |
|---|---|---|---|
| `/login`, `/register` | `login`, `register` → `AuthModalSuite` | API | One polished split auth surface, useful social/passwordless/passkey/2FA affordances, good labels and target sizing. The same suite serves both modes; role choice and role-specific onboarding are not clear enough for eight roles. Backend errors are displayed verbatim in `AuthModalSuite.tsx:722`, which risks exposing implementation text. |
| `/auth/role` | `auth-spec(kind=role)` | API | Role-selection flow exists, but must be aligned with the actual role matrix and post-registration destination. |
| `/auth/email-verification`, `/auth/phone-verification`, `/auth/otp` | `auth-spec` verification states | API | Good security-state coverage. Needs resend cooldown, expired-code recovery, copy/paste behavior, and anti-enumeration content verification. |
| `/auth/forgot-password`, `/auth/reset-password` | `auth-spec` recovery states | API | Flow is present. Validate token-expired, invalid-token, success, password rules, and return-to-login states against the real backend. |
| `/auth/passwordless` | `passwordless-complete` | API | Dedicated completion path with tests. Must be tested with invalid/expired/consumed links and browser back behavior. |
| `/auth/2fa-setup`, `/auth/recovery-codes`, `/auth/social-consent` | `auth-spec` security states | API | Broad coverage; recovery-code download/copy, device trust, and social consent need explicit confirmation language and audit events. |
| `/owner/invitation` | `owner-invitation` | API | Good specialized invitation route. Needs a true expired/revoked/already-accepted state and role-specific handoff. |
| `/auth/post-login-toast` | `auth-post` | SPEC/static | Demo/validation surface, not a real workflow. Keep under internal design validation only. |
| `/logout` | `logout` | HYB | Performs logout in `App.tsx` and shows a static result. Workspace also has a native confirmation dialog. Align logout confirmation, redirect, session cleanup, and back-button behavior. |

### Booking and payment

| Route(s) | Typed variant / current implementation | Data | UX audit |
|---|---|---|---|
| `/booking/:bookingId/:state` | `booking-state` with review, quote, identity, checkout, pending, success, failure, rejected, cancelled, invoice, receipt states | API | One of the strongest modeled workflows: explicit status states, identity gate, checkout, success/failure, and documents. Needs payment-provider failure QA, back/refresh persistence, duplicate-submit idempotency messaging, and mobile checkout review. |
| `/payment-confirmation?bookingId=...` | `payment` | API | Dedicated confirmation exists. Ensure it is not a second confirmation UX diverging from `booking-state/success`. |
| Booking modal from `/explore` and property detail | Two implementations: `components/booking/BookingModal.tsx` and `features/booking/BookingModal.tsx` | HYB | Consolidate. Current modal is a centered dialog with max height; it is not a mobile bottom sheet and long date/guest/protection content can feel heavy at 360–390px. |

### Traveler / guest

| Route(s) | Typed variant / current implementation | Data | UX audit |
|---|---|---|---|
| `/guest-dashboard` | `guest-dashboard` → `TravelerStateContainer(view=dashboard)` | API | Good triple-status model for booking/verification/payment, clear next-trip CTA, loading and empty states. Dashboard needs role-aware unauth handling: current workspace wrapper defaults to Guest even when a different route is requested without a session. |
| `/traveler/reservations`, `/traveler/reservations/upcoming`, `/past`, `/cancelled`, `/:id` | `traveler-spec` reservation views | API | Strong persisted reservation basis, invoice/receipt downloads and QR issuance. Detail routing is represented as a generic `reservation-detail` view; ensure the selected ID is actually consumed rather than only changing the label. |
| `/traveler/qr*` | `traveler-spec(view=qr)` | API | Useful gate-pass flow. Needs expired/revoked/offline/duplicate scan states in the user-facing contract. |
| `/traveler/payment-methods` | `traveler-spec(view=payment-methods)` | API | API setup-intent path exists. The add/remove/default lifecycle needs explicit success, cancellation, provider failure, and no-method states. |
| `/traveler/payments`, `/traveler/invoices` | `traveler-spec` payment/invoice views | API | Good document download basis. Responsive presentation should prefer cards at mobile rather than forcing a wide ledger. |
| `/traveler/preferences`, `/profile` | preference/profile variants | API | Profile route is API-backed; preferences are much thinner than identity/security. Align one settings IA. |
| `/traveler/identity` | `traveler-spec(view=identity)` | API | Strong document upload architecture with prepare/upload steps. Needs clear privacy retention, scan status, retry, file validation, and verification decision states. |
| `/traveler/reviews/given`, `/traveler/reviews/pending`, `/traveler/reviews` | live `traveler-spec` plus static `trav-reviews` alias | HYB | Same user goal has different implementations. `/traveler/reviews` currently renders `SpecScreens.tsx` `PendingReviewsPage`, while `/traveler/reviews/pending` uses the live state container. Canonicalize. |
| `/traveler/favorites`, `/wishlist` | `trav-favorites` → `FavoritesCollectionsPage` | SPEC/static | Explicit source comment says “No wishlist API yet (spec): local state” at `SpecScreens.tsx:738`. This is a product trust issue because the route appears to be a real account surface. The live API wishlist panel exists in `CompletionPages.tsx:1015` but is not the current route. |
| `/traveler/notifications`, `/notifications` | `trav-notifications` → `NotificationsCenterPage` | HYB | API reads/marks read exist, but local initial channels and silent catch fallbacks blur data provenance. Add unread counts to navigation and an explicit offline/error state. |
| `/traveler/suggestions` | `trav-suggestions` | API/HYB | API recommendation/dismiss/preferences calls exist. Empty, stale recommendation, and dismissed/restored states need full product copy. |

### Messaging and document exchange

| Route(s) | Typed variant / current implementation | Data | UX audit |
|---|---|---|---|
| `/messages`, `/messages/:conversationId` | `messages` → `MessagesPage` → `MessagingStateContainer` → `MessagingCenter` | SPEC/static in current route | The current production-looking route renders local sample threads and appends messages locally; `MessagingCenter.tsx:13` states that the messaging API is not wired. A complete API-backed `MessagesWorkspace` with inbox, read state, attachments, retries, and downloads exists in `CompletionPages.tsx:1521` but is unreferenced. This is a P0 implementation-selection defect. |
| `/messages/document` | `document-message` → `SpecScreens.tsx` | SPEC/static | Secure document-message concept exists, but it is not aligned with the API-backed attachment implementation. Decide whether this is a specialized secure compose surface or a message thread mode. |

### Directories, providers, and service roles

| Route(s) | Typed variant / current implementation | Data | UX audit |
|---|---|---|---|
| `/directory/custodians` | `directory-spec(kind=Custodian)` | API/HYB | Directory list structure, filters, badges, parish/service content are strong. Empty/error/filter persistence and real provider detail links need verification. |
| `/directory/trades` | `directory-spec(kind=Trades)` | API/HYB | Good role value and search pattern; preserve safety language and emergency distinction. |
| `/directory/businesses` | `directory-spec(kind=LocalBusiness)` / dead `business-directory` variant | API | Live M4 directory path exists, including provider details, quotes, reviews and business directions. The extra typed `business-directory` route is not returned by `parseRoute()` and should be removed or wired. |
| `/directory/police` | `directory-spec(kind=Police)` and `officer-directory` reuse | API/HYB | Current `/host/wellness/directory` reuses the Police directory. It is semantically understandable but should have a dedicated officer assignment view rather than an alias. |
| `/directory/guest-verification` | `directory-spec(kind=Verification)` | API/HYB | Guest verification concept is present. Needs explicit permission and status copy because it handles sensitive identity information. |
| `/directory/provider/onboarding` | `directory-spec(kind=Provider)` | API | Provider onboarding has structured services, hours, radius, documents, moderation status and messaging. This is a strong foundation; add step progress and draft recovery. |
| `/directory/providers/:slug` | `directory-spec` detail | API | Provider detail has quote, review, documents and contact concepts. Ensure review/quote mutations have disabled, error, and success feedback consistently. |
| `/directory/provider` | `directory-spec(kind=ProviderDashboard)` plus `provider-dashboard` screen mapping | API | Provider dashboard/self-management is live in `CompletionPages.tsx`, but there are two route names for the same concept. Canonicalize. |

### Host

| Route(s) | Typed variant / current implementation | Data | UX audit |
|---|---|---|---|
| `/host-dashboard` | `host-dashboard` → `HostStateContainer(view=dashboard)` → `HostAnalytics` | API | Good operational starting point with property/booking metrics and loading/error/empty patterns. It should be the sole host home rather than sharing a public “Host” nav link with no role explanation. |
| `/host/properties`, `/host/properties/archived` | `property-management` and `host-spec(view=archived)` | API | Host list has archive/restore/duplicate/publish/history/bulk actions and shared list controls. Keep this as a primary reference implementation. |
| `/host/properties/new` | `host-spec(view=properties-new)` → 10-step `HostPropertyWizard` | API | Strongest form workflow: autosave, geocoding, photos, preview, sticky action bar, upload progress. Ten steps plus a sticky footer needs a mobile task map and better resume/recovery. |
| `/host/properties/edit?id=...` | `host-spec(view=properties-edit)` → `HostPropertyEditor` | API | API editor is much thinner than the wizard. Users need a consistent section-by-section editor with dirty state, unsaved exit guard, validation summary, and revision comparison. |
| `/host/analytics` | `host-spec(view=analytics)` → `HostAnalytics` | API | Good KPI/list basis; charts are CSS bars or metric cards rather than a true analytics system. Add time range, export parity, and no-data interpretation. |
| `/host/pricing`, `/host/promotions` | `host-spec` → `HostPricingPromotions` | HYB/static | Hard-coded pricing/promotion records, local delete, and `alert("Rule created.")` at `features/host/HostPricingPromotions.tsx:47`. This looks live but does not persist. P0/P1 rebuild. |
| `/host/exports`, `/host/reports` | `host-spec(view=exports)` plus static `host-reports` | SPEC/static | `HostReportsExports.tsx` generates hard-coded CSV rows and print output. `/host/reports` is another static screen. Consolidate around API report data and honest export availability. |
| `/host/reviews` | `host-spec(view=reviews)` | HYB | Current review is hard-coded and reply is local state. Badge/settings path is API-backed, but review management is not. |
| `/host/badges`, `/host/settings` | `host-spec` → `HostReviewsBadgesSettings` | API/HYB | Badge definitions/assignments/renewals/feature access have a solid API path. Auto-renew preference is local browser storage, which should be labeled as device-only or persisted server-side. |
| `/hosts`, `/hosts/:slug`, `/host/profile/edit`, `/host/profile/preview` | `host-profile` → `HostProfileStateContainer` | SPEC/static | `HostProfileDirectory`, detail, and edit use hard-coded local state. A separate API-based host profile implementation exists in `CompletionPages.tsx` but is not the route path. `HostProfileDirectory.tsx:80` links to `/host-profile/:id`, a prefix not recognized by `parseRoute()`; this is a concrete broken-link defect. |
| `/host/wellness`, `/host/wellness/book` | `host-wellness`, `wellness-booking` → `HostWellnessPage` | API/HYB | Strong domain workflow with quotes, officer selection, booking, subscription, report, cancel/reschedule. `/host/wellness/book` reuses the full host wellness page instead of opening a focused booking step. |

### Officer / wellness

| Route(s) | Typed variant / current implementation | Data | UX audit |
|---|---|---|---|
| `/officer/wellness` | `officer-wellness` | API/HYB | Includes onboarding, availability, document/photo evidence, visit completion and report submission. Strong domain depth, but needs a mobile field-work mode, offline queue, camera permissions QA, and upload retry clarity. |
| `/host/wellness/directory` | `officer-directory` | API/HYB | Current route reuses Police directory rather than a focused officer scheduling/assignment screen. |

### Property manager and owner

| Route(s) | Typed variant / current implementation | Data | UX audit |
|---|---|---|---|
| `/pm/dashboard` | `pm-dashboard` → `PropertyManagerDashboardPage` | API | Dashboard has live portfolio metrics and enhancement hub. In an unauthenticated browser, `WorkspaceFrame` defaults to Guest and the page appears inside Guest navigation before the PM sign-in requirement; this is a P0 role/shell mismatch. |
| `/pm/gates`, `/pm/invoices`, `/pm/maintenance`, `/pm/governance`, `/pm/documents` | dedicated PM route names → `PropertyManagerPmsPage` | API/partial | Live dashboard data, errors, refresh, invoices, maintenance, documents, gates and history exist. These are routed as separate screens but share one large module renderer and duplicated PM nav. |
| `/pm/payments`, `/pm/vendors`, `/pm/community`, `/pm/subscription`, `/pm/calendar`, `/pm/work-orders`, `/pm/agreements`, `/pm/approvals`, `/pm/team`, `/pm/inspections`, `/pm/cleaning` | PM module variants → same `PropertyManagerPmsPage` | API/partial | The generic branch at `PropertyManagerPmsPage.tsx:75` displays counts and, for many modules, only one first-record action. It is not a complete workflow for approvals, team, inspections, cleaning, agreements or work orders. These need dedicated information architecture and task screens. |
| `/pm/utilities`, `/pm/verification`, `/pm/reports` | PM module variants | API | Useful live operations and API mutations. Utilities has a reading form and list; reports has totals. Add validation, history, anomaly explanation, export, and mobile review. |
| `/pm/insurance` | `pm-insurance` → `InsuraGuestPage` | SPEC/static | Static insurance concept. It is not a live policy or claims workspace and should be marked as concept/roadmap or removed from production navigation. |
| `/gate` | `pm-gate` → `PropertyManagerGatePage` | API | Strongest field/guard surface: camera-image QR decode where supported, manual fallback, offline signal, validation result, property confirmation and minimal notes. Needs live camera stream support or an explicit image-only expectation. |
| `/owner/dashboard` | `owner-dashboard` → `OwnerPortalPage` | API | Live owner-scoped properties, invoices, statements, maintenance, notices, governance and proxy actions. Good role separation; needs owner-focused navigation instead of inheriting traveler items. |

### Admin and system states

| Route(s) | Typed variant / current implementation | Data | UX audit |
|---|---|---|---|
| `/admin` | `admin` → `AdminPage` | HYB | Admin route is permission-gated, but the page mixes live health/integration data with hard-coded KPI/admin content. Create a real admin IA by operational job, not one broad dashboard. |
| `/admin/ops/:view` | `admin-ops` → `AdminOpsSpecPage`, except `wellness` → `AdminPage` | API/HYB | Explicit permission mapping exists. The route family is broad and needs canonical view names, per-queue tables, bulk actions, audit context and consistent error states. |
| `/admin/kpis` | `admin-kpis` → `AdminKpiPage` | SPEC/static | Hard-coded figures; not a live KPI product. Rebuild around time range, denominator, data freshness, drill-down and export. |
| `/admin/reports` | `admin-reports` → `AdminReportsPage` | SPEC/static | Static report concepts. It overlaps with Admin Ops reports and `Activity & audit` in workspace navigation. |
| `/admin/officer-id-reset` | `officer-id-reset` → `OfficerIdResetPage` | SPEC/static | Sensitive admin action is presented as a spec workflow. Needs real permission/audit/confirmation/irreversibility copy before production. |
| `/401` | `sign-in-required` | SPEC/state | Good controlled state. Make it the canonical unauthenticated result instead of allowing every role route to render a mixed shell first. |
| `/403` | `access-restricted` | SPEC/state | Admin permission gate has a clear restricted state. Non-admin role gates are not consistently centralized. |
| `/404` | `not-found` | SPEC/state | Present and tested. Add context-preserving return CTA from dynamic detail URLs. |
| `/500` | `server-error` | SPEC/state | Present. Ensure API failures route to an actionable inline state rather than a console-only catch. |
| `/empty/favorites`, `/empty/reservations` | `no-favorites`, `no-reservations` | SPEC/state | Useful reusable states, but favorite/reservation live routes do not consistently use them. |
| `/loading` | `loading-state` | SPEC/state | Shared skeleton page exists. Suspense fallback in `App.tsx:1006` is still a bare text block rather than `LoadingState`. |
| `/design-system`, `/screens`, `/screens/:screenId` | `design-system`, `design-screen` | SPEC/internal | Valuable implementation index, but registry drift proves it is not yet a reliable source of truth. Keep internal, add automated registry validation. |

## 4. Role and navigation audit

### Role coverage

| Role | Current primary surfaces | Shared surfaces | Main gap |
|---|---|---|---|
| Guest / Traveler | `guest-dashboard`, reservations, invoices, identity, QR, reviews, suggestions, favorites, notifications | Explore, property detail, booking, messages, directories, profile | Guest and Owner share traveler nav; unauthenticated route behavior defaults to Guest. |
| Host | Host dashboard, properties, wizard/editor, analytics, pricing, promotions, reports, reviews, badges, wellness | Messages, notifications, directories, profile | Pricing, promotions, reports, profile, and reviews are still sample/local. |
| Property Manager | PM dashboard, 19 module route variants, gate, owner-scoped portfolio | Messages, notifications, profile | Many modules are generic count cards rather than task-complete workflows; PM navigation is duplicated in shell and page. |
| Owner | Owner portal, invoices, statements, maintenance, notices, governance/proxy | Traveler items, messages, notifications, profile | Owner gets traveler navigation because `travelerItems` includes Owner; needs a clean owner IA. |
| Service Provider | Provider onboarding/profile/dashboard, directory detail, quote/review responses | Messages, notifications, directories, profile | Role is modeled, but primary entry and onboarding progression are not prominent in global navigation. |
| Local Business | Business/provider listing, directions, quote/review flow | Messages, notifications, directories, profile | Same provider route family needs clearer distinction between business and individual service provider. |
| Officer / Wellness | Officer onboarding, availability, document/photo evidence, visits/reports, directory | Messages, notifications, profile | `/host/wellness/directory` is reused for officer directory; field mode/offline behavior is incomplete. |
| Admin | Admin dashboard, ops queues, reports, KPI, badge, officer reset, audit | All shared items because admin sees all | Admin has broad access but no coherent information architecture; registry mapping has admin drift. |

### Navigation findings

1. **Public nav is role-agnostic.** `Navbar` in `App.tsx:318-479` exposes Explore, Host, Wellness and a mobile set including Guest, Calendar, and Bookings. It does not explain which role owns each area or gate entries before rendering.
2. **Workspace nav is filtered, but the shell still assumes Guest without a session.** `WorkspaceFrame.tsx:117-121` uses `roles = session?.roles ... ?? ["Guest"]`. A direct `/pm/*`, `/owner/*`, or `/admin/*` URL therefore initially receives Guest semantics and quick actions.
3. **The mobile workspace nav is not a complete mobile IA.** `WorkspaceFrame.tsx:191` renders `allVisibleItems.slice(0, 5)`. On a PM or Admin session, the first five are mostly traveler/common items; important operational destinations are only in the horizontally scrollable top rail or page-local links.
4. **PM navigation is duplicated.** `PropertyManagerPmsPage.tsx:23` renders a 14-link PM nav while the global `WorkspaceFrame` also renders role-filtered navigation. This increases cognitive load and active-state ambiguity.
5. **Search results use `role=listbox` without explicit option semantics.** The results are links inside the listbox. Use a list of navigation links or implement proper `role=option`, keyboard selection, and focus management.
6. **Admin and activity are separate links to overlapping reports.** `workspaceItems` labels both `Admin` and `Activity & audit`, while `/admin/reports` is also a static report screen. Consolidate the information architecture.
7. **There is no navigation-level unread indicator.** Alerts and Notifications links exist, but the shared shell does not expose unread count or severity.

## 5. Visual language and design-system audit

### What is working

- `frontend/src/nestystay-theme.css` establishes a clear DS v2: deep teal, yellow, warm sand/cream, semantic green/coral/amber/info/mint, Fraunces/Sora, and consistent radii/shadows.
- `frontend/src/components/layout/PublicShell.tsx` gives public content a recognizable pattern background, roundel, tier badges, and deep footer.
- Shared primitives have a coherent pill/card/field grammar and generally target 44–48px controls.
- `LoadingState.tsx` is a good reusable skeleton with `role=status`, `aria-busy`, reduced-motion support, and Patois/English copy.
- `Modal.tsx` has `role=dialog`, `aria-modal`, labelled heading, Escape close, focus return, and Tab wrapping.
- `StatusChip.tsx` and semantic colors provide explicit text in addition to color; QR results also render `ACCESS APPROVED`/`ACCESS DENIED` text.
- `index.css:1947` includes forced-colors support and the base focus-visible rule is present.

### Drift and inconsistency

- `frontend/src/index.css` is 3,629 lines and contains multiple generations: legacy aliases/tokens, older surface classes, a large legacy responsive block, then DS v2 overrides. The comments indicate migration, but the CSS still contains the migration burden.
- Legacy values (`--sun`, older deep/sand aliases, `card-box`, `.btn-*`) coexist with DS v2 utilities and token names. A designer cannot reliably tell which token is canonical from the code.
- `ProductPages.tsx`, `CompletionPages.tsx`, and `SpecScreens.tsx` use different content/page patterns and naming conventions (`page-container`, `product-page`, `CompletionShell`, `card-box`, utility-class cards).
- The design-system reference route is static and does not validate the components actually used by all routes. Add token/component snapshots or a generated manifest.
- The app uses both a hand-written `AppLink` SPA navigation layer and native anchors. `AppLink.tsx:3-29` handles popstate and smooth scrolling, while native anchors cause full document navigation. This is not inherently wrong, but it creates inconsistent state preservation, scroll behavior, and test semantics.

## 6. Responsive audit

### Breakpoint system

Current breakpoint logic is a mixture of Tailwind defaults (`sm` 640, `md` 768, `lg` 1024, `xl` 1280) and custom CSS at approximately 1080, 900, 860, 767, 760, 680, and 580px. This is enough to avoid obvious horizontal overflow in the tested public set, but it is too fragmented for a predictable tablet strategy.

### Desktop: 1440–1920px

**Strengths**

- Public hero, deep floating navbar, split auth, workspace sidebar, cards, and page headers have strong desktop composition.
- Workspace content uses `max-w-[1120px]` with a 230px sidebar, preserving readable content width.
- Cards and action bars generally wrap rather than clip.

**Risks**

- Long editorial landing scroll and motion-heavy hero compete with task-oriented routes.
- PM pages duplicate top navigation and expose dense cards rather than clear task grouping.
- Wide tables are structurally desktop-first; responsive wrappers preserve usability but not scanability.
- Repeated page shells create inconsistent header spacing and breadcrumb/action placement.

### Tablet: 768–1024px

This is the most fragile breakpoint. At `md` (768px), `WorkspaceFrame` switches to a 230px desktop sidebar, leaving approximately 538px at 768px before content padding. At `lg` (1024px), public global search becomes visible. There is no real tablet-specific IA between mobile rail and desktop sidebar.

Likely pressure points:

- PM forms using `sm:grid-cols-2` become two-column layouts as soon as the viewport is 640px, even when the content area is much narrower after the sidebar.
- Five-column utility forms use `lg:grid-cols-5`; the transition is abrupt and should be planned around task grouping, not only grid count.
- Workspace table wrappers and dense card action rows can become visually compressed.
- Existing 1024px evidence passed overflow checks, but that does not cover the 768px hardening viewport or the task-completion quality of PM/admin tables.

### Mobile: 360–390px

**Observed / evidenced strengths**

- Existing responsive JSON records `scrollWidth === viewportWidth` and no undersized controls for the 12 sampled routes at 360/390px.
- Public search, auth, directories, gate QR, help, and error states have good basic wrapping.
- Base CSS sets `min-width: 320px`, and shared interactive controls are generally large enough.

**Structural weaknesses**

- The workspace becomes a horizontally scrolling top rail plus a fixed five-item bottom nav. This is a mobile adaptation, not a mobile information architecture.
- The bottom nav is hard-coded to the first five filtered items, so PM/Admin/Owner actions are not guaranteed to surface.
- Tables keep `min-width: 680px` (`.table-styled`) or `640px` (`.responsive-table`), which is acceptable only when the wrapper exposes a clear scroll affordance and the row action remains reachable. A card transformation is preferable for most mobile workflows.
- `Modal.tsx` always uses a centered desktop-style dialog with `p-6`, `p-7`, `max-h-[calc(100vh-48px)]`, and no mobile bottom-sheet mode. Booking, preview, and document dialogs need a mobile presentation.
- 10-step host property wizard, PM forms, subscription controls, report cards, and message attachment rows need task-level mobile redesign, not just wrapping.
- Fixed bottom navigation overlays workspace content; the main has `pb-24`, but page-local sticky action bars and upload panels need collision QA.

### Mobile redesign count

**23 typed route families need a mobile IA/layout redesign:** `map-search`, `trav-favorites`, `trav-reviews`, `messages`, `document-message`, `host-profile`, `host-reports`, `pm-gates`, `pm-governance`, `pm-documents`, `pm-vendors`, `pm-community`, `pm-calendar`, `pm-work-orders`, `pm-agreements`, `pm-approvals`, `pm-team`, `pm-inspections`, `pm-cleaning`, `admin`, `admin-kpis`, `admin-reports`, and `officer-id-reset`.

This count is based on density, navigation reachability, table/dialog behavior, or product mismatch at small screens; it is not a claim that the other 56 routes need no responsive QA.

### Complete product/UX redesign count

**11 typed route families need a complete product/UX redesign or explicit removal from production:** `map-search`, `trav-favorites`, `trav-reviews`, `messages`, `host-profile`, `host-reports`, `pm-insurance`, `admin-kpis`, `admin-reports`, `officer-id-reset`, and `document-message`.

The reason is not aesthetics. These surfaces are either sample-only, hard-coded, partially implemented, semantically wrong for their route, or divergent from a more complete implementation already present elsewhere in the source tree.

## 7. Workflow and state completeness

### Loading

**Good:** `LoadingState.tsx` is structured, semantic, and reusable. Many API-backed features use `LoadingState` or a `DataGate` wrapper.

**Gap:** the global Suspense fallback in `App.tsx:1006` is only `Loading this workspace…`. A route-level code-split failure and a data-loading state are visually indistinguishable. Use one shell-aware loading pattern with route context and a retry path.

### Empty

**Good:** shared `EmptyState`, `NoFavoritesPage`, and `NoReservationsPage` exist. Live traveler/PM flows often provide next actions.

**Gap:** static/spec empty screens and API failures can look similar. Every empty state needs a provenance and action contract: “no records yet”, “filtered to zero”, “not available”, or “request failed”.

### Error

**Good:** `ErrorState`, `ErrorNotice`, `DataGate`, `role=alert`, `role=status`, and retry buttons exist.

**Gap:** several components catch errors with `console.error` or `.catch(() => undefined)` and simply show an empty list. `PublicSearchMap.tsx` is a concrete example. The local missing PostgreSQL password also surfaced as a raw-ish unavailable message in the public showcase and as 401/API errors elsewhere. Define a shared error taxonomy: auth, permission, validation, network, server, stale, and unknown.

### 401 / 403 / 500

- Admin routes use `AdminRoute` with session, admin-role, and permission checks.
- Non-admin role routes do not have one central route guard; pages often render a shell and let the API return an error.
- `/401`, `/403`, and `/500` are present, but they should be reachable from consistent guards and inline error recovery rather than only as manually navigated screens.

### Forms

- Auth and booking forms are the most complete.
- Host wizard has the best form architecture but needs better mobile navigation, draft recovery and validation summary.
- PM forms are broad but compressed and inconsistent across modules.
- Host pricing/promotions, host profile edit, static admin screens, and parts of wellness are visually complete but not consistently persisted.
- Native `alert()` is still used for product actions in `ExperiencesPage`, `HostPricingPromotions`, `HostReservations`, `AdminPricebookCampaigns`, `AdminFinancials`, `AdminProperties`, and booking invoice error handling. Replace with shared inline/toast feedback and confirmation patterns.

### Tables, lists, and bulk actions

- `ListControls` is a strong reusable base: search, sort, export, pagination, selection, and live result text.
- CSS deliberately preserves minimum table widths (`680px`/`640px`). For desktop this is fine; for mobile it needs per-screen card transformation and a clear column priority model.
- Bulk action and select-page patterns exist in the shared control, but many screens are simple card lists without consistent selection, filter persistence, or saved view semantics.
- PM invoices/payments/maintenance and admin ops should be first-class data tables with row details, bulk state changes, audit context, and mobile cards.

### Cards, dialogs, sheets, uploads

- Card language is consistent and visually strong, but nested cards are used heavily in PM and admin surfaces, flattening hierarchy.
- `Modal` semantics are good. Its mobile interaction model is not: no bottom sheet, no click-outside policy, and no explicit body-scroll lock. Decide the mobile modal policy per content type.
- Upload architecture is strongest in host property photos, wellness evidence, traveler identity, admin case evidence, provider documents, and live message attachments. The common pattern is prepare → upload → progress → status, which is good.
- Upload UX is not universal. Every upload surface needs file limits, accepted type, scan status, cancel/retry/remove, privacy copy, and a resumable or recoverable failure state.

### Dashboards, charts, calendars, notifications

- Dashboards are primarily metric cards and lists. No charting library is used; bars are CSS-only in PM utility/spec surfaces. This is appropriate for a first summary, not for finance/operations analysis.
- Host calendar has real availability/feed API mechanics in `ProductPages.tsx:1655-1814`.
- PM “Master calendar” is a live event list, not a true calendar grid or drag/drop schedule. Treat it as a distinct operational agenda until a calendar interaction model is designed.
- Notifications have API mark-read calls but no global unread count, notification grouping policy, or consistent deep links.

## 8. Accessibility and interaction quality

### Carry forward

- Skip link and `#main-content` focus target in `WorkspaceFrame`.
- Global focus-visible outline in `index.css`.
- Large buttons/inputs, visible labels or `aria-label`, live status and alert regions.
- Dialog focus return and Tab wrapping.
- Explicit status text for QR and semantic status chips.
- Reduced-motion CSS and `useReducedMotion` scroll behavior.

### Fix before launch

- Replace `alert()` and broad `window.confirm()` with accessible in-app confirmation/feedback where the action is part of a workflow.
- Do not use a `role=listbox` wrapper for ordinary links without option semantics.
- Ensure clickable card rows, FAQ items, rating controls, and map affordances are native buttons/links or keyboard-equivalent controls.
- Add route-level focus management after SPA navigation; `AppLink.navigate()` scrolls to top but does not move focus to the new page heading/main.
- Add automated axe checks, keyboard-only passes, screen-reader passes, forced-colors checks, and reduced-motion screenshots against the deployed build.
- Avoid exposing raw backend error text in auth and operational views.

## 9. P0 / P1 / P2 action matrix

| ID | Priority | Finding | Evidence | Required action | Owner / dependency |
|---|---|---|---|---|---|
| UX-01 | P0 | Screen registry drift | `App.tsx:601-790`; 57 IDs, 53 mappings, 4 unmatched | Make a single typed manifest the source for routes, design IDs, titles, roles, test cases, and Figma references. Add a test that fails on unmatched IDs and untested routes. | Frontend architecture |
| UX-02 | P0 | Wrong role shell on direct unauthenticated role routes | `WorkspaceFrame.tsx:117-121`; defaults to Guest | Add route metadata and a centralized role/session guard. Show `/401` or a dedicated role sign-in entry before mounting another role’s shell. | Auth + frontend |
| UX-03 | P0 | Current messages route is local sample data | `MessagesPage` at `CompletionPages.tsx:1517-1519` returns `MessagingStateContainer`; `MessagingCenter.tsx:13` says sample | Switch the route to the existing API-backed `MessagesWorkspace`, then delete/retire the sample implementation and test inbox, thread, attachments, retry, and empty states. | Messaging + backend |
| UX-04 | P0 | Host profile route/data mismatch | `HostProfileStateContainer.tsx`, hard-coded feature files; broken `/host-profile/:id` link at `HostProfileDirectory.tsx:80` | Choose the API implementation, repair canonical `/hosts/:slug` routing, and remove local sample profile/edit paths. | Host profile |
| UX-05 | P0 | PM modules promise separate products but render a generic summary | `PropertyManagerPmsPage.tsx:75` | Design dedicated PM IA for maintenance, approvals, work orders, documents, inspections, cleaning, team, agreements, governance, and vendors. Keep shared data primitives, not one generic page. | PM product + frontend |
| UX-06 | P0 | Backend/runtime environment cannot verify real workflows | Local API log: no PostgreSQL password for `nestystay_dev` | Provide deterministic local seed/config or a test API profile; make environment failure actionable and block release checks when API readiness is false. | Backend / DevEx |
| UX-07 | P1 | Public/API failure becomes no-results | `PublicSearchMap.tsx:34-45` | Separate `error` from `items.length === 0`; render retry/error state and preserve filters. | Public discovery |
| UX-08 | P1 | Static-looking production routes | `SpecScreens.tsx:738`, host reports/pricing, admin KPI/report/reset, PM insurance | Add explicit “preview/sample” boundary or replace with API-backed implementations; remove dead aliases. | Product + frontend |
| UX-09 | P1 | Mobile workspace IA loses role-critical actions | `WorkspaceFrame.tsx:191` slices first five | Use a role-specific mobile tab set plus “More” drawer/action sheet; define primary actions per role. | UX + frontend |
| UX-10 | P1 | Tablet breakpoint is abrupt | `WorkspaceFrame` switches at 768px; custom CSS has 1080/900/860/767/760/680/580 | Define desktop/tablet/mobile shell states and test 768, 820, 1024, 1280, 1440. | Design system |
| UX-11 | P1 | Wide tables are technically scrollable but not mobile-optimized | `index.css:125-146`, `1933-1934` | Provide per-screen column priority, mobile cards, sticky identity/action, and scroll affordance. | Design system + feature teams |
| UX-12 | P1 | Dialog is desktop-centered on mobile | `Modal.tsx` | Add mobile bottom-sheet/full-height variant, body-scroll locking, safe-area handling, and content-specific close policy. | Design system |
| UX-13 | P1 | Native browser alerts/confirmations remain in workflows | `rg` findings in host/admin/public/booking features | Replace with `Modal`, `PatoisToast`, inline `role=status/alert`, and undo where appropriate. | Design system |
| UX-14 | P1 | Native anchors and SPA `AppLink` are mixed | `components/AppLink.tsx:3-29` plus many native `<a>` usages | Define when navigation is SPA, external, download, or full reload; add focus and analytics behavior. | Frontend architecture |
| UX-15 | P1 | No global unread/severity model | `WorkspaceFrame.tsx` alerts/notifications links | Add unread count, severity grouping, deep links, mark-read feedback, and notification preferences. | Cross-product |
| UX-16 | P1 | Authenticated API error copy is inconsistent/raw | `AuthModalSuite.tsx:722`, mixed `catch` behavior | Normalize API errors into user-safe messages with support correlation ID in dev/admin only. | Auth + backend |
| UX-17 | P2 | CSS and token generations are mixed | `index.css` 3,629 lines; `nestystay-theme.css` | Migrate legacy classes into DS v2 tokens/components, then remove aliases and duplicate responsive blocks. | Design system |
| UX-18 | P2 | No canonical chart/analytics model | Host/PM/admin surfaces use cards/CSS bars/static figures | Define chart components, time ranges, freshness, no-data, export and accessible summaries. | Analytics |
| UX-19 | P2 | Dead bodies and duplicate orchestration remain | `ProductPages.tsx`, `CompletionPages.tsx`, `SpecScreens.tsx`, feature duplicates | Remove unreferenced bodies after route migration; keep one feature module per job. | Frontend architecture |
| UX-20 | P2 | Figma/source parity is not machine-checked | 57 screen IDs and static design index | Generate screen manifest and Figma page links from route metadata; use visual snapshots only for canonical screens. | Design + frontend |

## 10. Recommended future Figma file structure

Create one canonical Figma file with pages ordered as follows. Use variables and components first; do not redraw each route as an isolated frame.

### Page 00 — Cover, audit, and release status

- Product map, role map, route count, API/static legend, release blockers, and last verified build.
- Link each screen ID to its route and implementation file.

### Page 01 — Variables and foundations

- Color variables: deep, yellow, sand, cream, ink, semantic success/error/warning/info/mint, on-dark variants.
- Type variables: Fraunces display scale, Sora body/label scale, mono IDs.
- Radius variables: field, nav, card, photo, pill.
- Elevation, border, focus, overlay, motion, reduced-motion, forced-colors guidance.
- Breakpoint variables: mobile 360/390, tablet 768/1024, laptop 1280/1440, desktop 1920.

### Page 02 — Core components

- Buttons, links, icon buttons, inputs, select, textarea, checkbox/radio, field errors.
- StatusChip, Badge, alert/toast, loading skeleton, empty state, error state.
- Card, PageHeader, ActionBar, ListControls, table/card-list patterns.
- Modal, mobile sheet, drawer, confirmation dialog, upload item, progress/status.
- Navigation: public navbar, workspace desktop sidebar, workspace tablet shell, role-specific mobile tabs, More drawer.

### Page 03 — Shells and responsive templates

- Public editorial shell.
- Public discovery shell.
- Auth shell.
- Traveler shell.
- Host shell.
- Property Manager shell.
- Owner shell.
- Provider/Officer shell.
- Admin shell.
- 401/403/404/500/loading/empty shell states.
- Annotated responsive transitions at 360, 390, 768, 1024, 1440, and 1920.

### Page 04 — Public discovery

1. Landing.
2. Explore grid/search.
3. Explore map.
4. Property detail.
5. Booking entry modal/sheet.
6. Experiences list/detail.
7. Journal list/detail.
8. Help/contact/legal.
9. API unavailable, no results, filtered zero, stale content.

### Page 05 — Auth and trust

1. Login.
2. Register role choice.
3. Email/phone verification.
4. OTP and expired OTP.
5. Password reset.
6. Passwordless completion.
7. Passkeys.
8. 2FA setup and recovery codes.
9. Social consent.
10. Owner invitation.
11. Session/device management.

### Page 06 — Traveler / Guest

1. Dashboard.
2. Upcoming/past/cancelled/detail reservations.
3. Booking status timeline.
4. Invoices/payment history.
5. Payment methods.
6. Identity verification/upload states.
7. Collections/favorites.
8. Suggestions/preferences.
9. Reviews pending/given.
10. Notifications/unread/read.
11. QR pass and revoked/expired/offline.

### Page 07 — Host

1. Dashboard/analytics.
2. Properties list/filter/archive.
3. Property wizard 10-step flow.
4. Property editor/revisions.
5. Availability/calendar/feed sync.
6. Reservations and verification.
7. Pricing/promotion rule builder.
8. Reviews/replies.
9. Badges/renewals/feature access.
10. Wellness booking/subscription/report.
11. Host profile directory/detail/edit.
12. Reports/export with real data and no-data states.

### Page 08 — Property Manager

1. Dashboard and portfolio selector.
2. Owners/invitations/assignment.
3. Properties/units.
4. Invoices and recurring templates.
5. Payments/reconciliation/refunds.
6. Utilities/meters/anomalies.
7. Maintenance/work orders/SLA/kanban.
8. Gate messages/QR/delivery/revocation.
9. Community notices/comments/scheduling.
10. Governance/proposals/voting/proxies.
11. Documents/versioning/expiry/download.
12. Vendors/approvals.
13. Inspections/cleaning/team.
14. Subscription lifecycle/billing recovery.
15. Calendar/agenda.
16. Reports/export/freshness.

### Page 09 — Owner

1. Assigned units.
2. Statements/invoices/payment.
3. Maintenance request.
4. Community notices.
5. Governance/proxy/vote.
6. Owner-specific mobile tabs.

### Page 10 — Providers and officers

1. Provider onboarding/draft/pending/approved/rejected.
2. Provider profile/detail.
3. Quote inbox and response.
4. Reviews and response.
5. Secure documents.
6. Officer onboarding.
7. Availability and assignments.
8. Visit/report/photo evidence.
9. Offline field mode.

### Page 11 — Admin operations

1. Overview and system health.
2. User management.
3. Property moderation.
4. Directory moderation.
5. Booking/dispute/payment queues.
6. Badge management.
7. Wellness operations.
8. Audit/activity log.
9. KPI/reporting with real data.
10. Sensitive officer ID reset with permission and audit.

### Page 12 — States, accessibility, and content rules

- Loading, skeleton, partial loading, retry.
- Empty: first-use, filtered zero, no permission, no data yet.
- Error: validation, auth, permission, offline, timeout, server, stale.
- Upload: queued, uploading, scan pending, uploaded, failed, cancelled, retry.
- Payment: requires action, processing, success, failure, refund, duplicate.
- Keyboard/focus order, screen-reader names, live announcements, reduced motion, forced colors.
- Patois/English content pairing rules and emergency copy rules.

## 11. Recommended implementation order

1. Establish route/screen manifest and fix the four registry mismatches.
2. Repair session/role guards and redesign workspace mobile navigation.
3. Switch messages to the existing API-backed implementation and remove sample messaging from production routes.
4. Canonicalize host profile, traveler reviews, traveler favorites, host reports/pricing, and admin report routes.
5. Split PM modules into dedicated workflows while retaining shared API/data primitives.
6. Replace alerts and silent catches with shared feedback/error patterns.
7. Define tablet shell and mobile card/table/dialog patterns; validate 360, 390, 768, 820, 1024, 1280, 1440, 1920.
8. Restore a seeded backend/test environment and rerun route, role, workflow, accessibility, and visual evidence.
9. Consolidate CSS/token generations and remove clearly dead code after route migration.
10. Build the Figma file from the canonical manifest and capture only approved production screen families.

## 12. Final verdict

**Current status: broad prototype / partial product, not release-ready as one coherent frontend system.**

The visual direction is strong enough to preserve. The product needs an architecture and UX consolidation pass before further screen expansion. The immediate release gate is not “add more pages”; it is to make every visible route honest about its data source, role, permissions, state, and mobile interaction model.
