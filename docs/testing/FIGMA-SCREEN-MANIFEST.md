# Figma screen manifest

This handoff table is derived from the canonical route/screen inventory in [frontend/src/app/routeManifest.ts](../../frontend/src/app/routeManifest.ts). The machine-readable projection is [frontend/src/app/figmaScreenManifest.ts](../../frontend/src/app/figmaScreenManifest.ts); it intentionally strips executable route factories while preserving IDs, routes, roles, shell metadata, navigation metadata, and future Figma references.

The table has one row per canonical screen. “Yes” indicates the screen family is required to have an intentional desktop/tablet/mobile treatment in the later Figma phase; this phase establishes architecture and state requirements without redesigning the visual identity.

| Screen ID | Canonical route | Role | Product area | Component / file | API status | Desktop | Tablet | Mobile | Key states required | Redesign priority | Future Figma reference |
|---|---|---|---|---|---|---|---|---|---|---|---|
| INDEX | /screens | Public | Internal | App.tsx -> design-screen | Internal reference | Yes | Yes | Yes | loading, empty, API error, retry | P2 | TBD |
| DS-V2 | /design-system | Public | Internal | App.tsx -> design-system | Internal reference | Yes | Yes | Yes | loading, empty, API error, retry | P2 | TBD |
| PUB-01 | / | Public | Public discovery | App.tsx -> home | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, retry | P1 | TBD |
| PUB-02 | /explore | Public | Public discovery | App.tsx -> explore | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, retry | P1 | TBD |
| PUB-MAP | /explore/map | Public | Public discovery | App.tsx -> map-search | Preview / migration | Yes | Yes | Yes | loading, empty, API error, retry | P2 | TBD |
| PUB-04 | /properties/:propertyId | Public | Public discovery | App.tsx -> property | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, retry | P1 | TBD |
| PUB-SOON | /coming-soon | Public | Public discovery | App.tsx -> coming-soon | Preview / migration | Yes | Yes | Yes | loading, empty, API error, retry | P2 | TBD |
| PUB-CONTENT | /about | Public | Public content | App.tsx -> public-content | Static content | Yes | Yes | Yes | static content, error | P1 | TBD |
| AUTH-01 | /login | Public | Authentication | App.tsx -> login | API-backed auth | Yes | Yes | Yes | loading, empty, API error, retry | P1 | TBD |
| AUTH-FLOW | /auth/role | Public | Authentication | App.tsx -> auth-spec | API-backed auth | Yes | Yes | Yes | loading, empty, API error, retry | P1 | TBD |
| AUTH-PWLESS | /auth/passwordless | Public | Authentication | App.tsx -> passwordless-complete | API-backed auth | Yes | Yes | Yes | loading, empty, API error, retry | P1 | TBD |
| AUTH-POST | /auth/post-login-toast | Public | Authentication | App.tsx -> auth-post | Internal reference | Yes | Yes | Yes | loading, empty, API error, retry | P2 | TBD |
| AUTH-LOGOUT | /logout | Public | Authentication | App.tsx -> logout | API-backed auth | Yes | Yes | Yes | loading, empty, API error, retry | P1 | TBD |
| OWNER-INVITE | /owner/invitation | Public | Authentication | App.tsx -> owner-invitation | API-backed auth | Yes | Yes | Yes | loading, empty, API error, retry | P1 | TBD |
| PUB-EXP | /experiences | Public | Public discovery | App.tsx -> experiences | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, retry | P1 | TBD |
| PUB-JOURNAL | /journal | Public | Public content | App.tsx -> journal | Static content | Yes | Yes | Yes | static content, error | P1 | TBD |
| BOOK-01 | /booking/:bookingId/review | Public | Booking | App.tsx -> booking-state | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, retry | P1 | TBD |
| BOOK-02 | /booking/:bookingId/quote | Public | Booking | App.tsx -> booking-state | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, retry | P1 | TBD |
| BOOK-03 | /booking/:bookingId/identity | Public | Booking | App.tsx -> booking-state | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, retry | P1 | TBD |
| BOOK-05 | /booking/:bookingId/checkout | Public | Booking | App.tsx -> booking-state | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, retry | P1 | TBD |
| BOOK-07 | /booking/:bookingId/pending | Public | Booking | App.tsx -> booking-state | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, retry | P1 | TBD |
| BOOK-CONF | /booking/:bookingId/success | Public | Booking | App.tsx -> booking-state | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, retry | P1 | TBD |
| BOOK-FLOW | /booking/:bookingId/:state | Public | Booking | App.tsx -> booking-state | Internal reference | Yes | Yes | Yes | loading, empty, API error, retry | P2 | TBD |
| TRAV-01 | /guest-dashboard | Guest, Host, PropertyManager, Owner, ServiceProvider, LocalBusiness, Officer, Admin | Traveler | App.tsx -> guest-dashboard | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| TRAV-RES | /traveler/reservations | Guest, Host, PropertyManager, Owner, ServiceProvider, LocalBusiness, Officer, Admin | Traveler | App.tsx -> traveler-spec | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| TRAV-QR | /traveler/qr | Guest, Host, PropertyManager, Owner, ServiceProvider, LocalBusiness, Officer, Admin | Traveler | App.tsx -> traveler-spec | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| TRAV-12 | /traveler/preferences | Guest, Host, PropertyManager, Owner, ServiceProvider, LocalBusiness, Officer, Admin | Traveler | App.tsx -> traveler-spec | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| TRAV-COL | /traveler/favorites | Guest, Host, PropertyManager, Owner, ServiceProvider, LocalBusiness, Officer, Admin | Traveler | App.tsx -> traveler-spec | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P0 | TBD |
| TRAV-INV | /traveler/invoices | Guest, Host, PropertyManager, Owner, ServiceProvider, LocalBusiness, Officer, Admin | Traveler | App.tsx -> traveler-spec | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| TRAV-PAY | /traveler/payment-methods | Guest, Host, PropertyManager, Owner, ServiceProvider, LocalBusiness, Officer, Admin | Traveler | App.tsx -> traveler-spec | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| TRAV-ID | /traveler/identity | Guest, Host, PropertyManager, Owner, ServiceProvider, LocalBusiness, Officer, Admin | Traveler | App.tsx -> traveler-spec | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| TRAV-REV | /traveler/reviews/pending | Guest, Host, PropertyManager, Owner, ServiceProvider, LocalBusiness, Officer, Admin | Traveler | App.tsx -> traveler-spec | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P0 | TBD |
| TRAV-NOTIF | /traveler/notifications | Guest, Host, PropertyManager, Owner, ServiceProvider, LocalBusiness, Officer, Admin | Traveler | App.tsx -> traveler-spec | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| TRAV-SUGG | /traveler/suggestions | Guest, Host, PropertyManager, Owner, ServiceProvider, LocalBusiness, Officer, Admin | Traveler | App.tsx -> trav-suggestions | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| MSG-DOC | /messages/document | Guest, Host, PropertyManager, Owner, ServiceProvider, LocalBusiness, Officer, Admin | Messaging | App.tsx -> messages | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P0 | TBD |
| MSG-01 | /messages | Guest, Host, PropertyManager, Owner, ServiceProvider, LocalBusiness, Officer, Admin | Messaging | App.tsx -> messages | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P0 | TBD |
| DIR-02 | /directory/trades | Public | Directories | App.tsx -> directory-spec | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, retry | P1 | TBD |
| DIR-BIZ | /directory/businesses | Public | Directories | App.tsx -> business-directory | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, retry | P1 | TBD |
| DIR-PROFILE | /directory/providers/:slug | Public | Directories | App.tsx -> directory-spec | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, retry | P1 | TBD |
| DIR-PROV | /directory/provider | Role: ServiceProvider, LocalBusiness | Directories | App.tsx -> provider-dashboard | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, retry | P1 | TBD |
| HOST-PROFILE | /hosts | Public | Host | App.tsx -> host-profile | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, retry | P0 | TBD |
| HOST-PROFILE-EDIT | /host/profile/edit | Host | Host | App.tsx -> host-profile | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P0 | TBD |
| HOST-01 | /host-dashboard | Host | Host | App.tsx -> host-dashboard | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P0 | TBD |
| HOST-05 | /host/properties | Host | Host | App.tsx -> property-management | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P0 | TBD |
| HOST-EDIT | /host/properties/edit | Host | Host | App.tsx -> host-spec | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| HOST-NEW | /host/properties/new | Host | Host | App.tsx -> host-spec | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| HOST-OPS | /host/analytics | Host | Host | App.tsx -> host-spec | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P0 | TBD |
| HOST-RPT | /host/reports | Host | Host | App.tsx -> host-spec | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P0 | TBD |
| HOST-WELL | /host/wellness | Host | Wellness | App.tsx -> host-wellness | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| OFC-DIR | /host/wellness/directory | Host, Officer | Wellness | App.tsx -> officer-directory | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| OFC-BOOK | /host/wellness/book | Host | Wellness | App.tsx -> wellness-booking | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| OFC-01 | /officer/wellness | Officer | Wellness | App.tsx -> officer-wellness | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| PM-GATE | /pm/gates | PropertyManager | Property Manager | App.tsx -> pm-gates | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| PM-DASH | /pm/dashboard | PropertyManager | Property Manager | App.tsx -> pm-dashboard | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| PM-INV | /pm/invoices | PropertyManager | Property Manager | App.tsx -> pm-invoices | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| PM-MAINT | /pm/maintenance | PropertyManager | Property Manager | App.tsx -> pm-maintenance | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| PM-GOV | /pm/governance | PropertyManager | Property Manager | App.tsx -> pm-governance | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| PM-DOCS | /pm/documents | PropertyManager | Property Manager | App.tsx -> pm-documents | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| PM-PAY | /pm/payments | PropertyManager | Property Manager | App.tsx -> pm-payments | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| PM-VEND | /pm/vendors | PropertyManager | Property Manager | App.tsx -> pm-vendors | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| PM-COMM | /pm/community | PropertyManager | Property Manager | App.tsx -> pm-community | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| PM-SUB | /pm/subscription | PropertyManager | Property Manager | App.tsx -> pm-subscription | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| PM-CAL | /pm/calendar | PropertyManager | Property Manager | App.tsx -> pm-calendar | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| PM-WO | /pm/work-orders | PropertyManager | Property Manager | App.tsx -> pm-work-orders | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| PM-AGR | /pm/agreements | PropertyManager | Property Manager | App.tsx -> pm-agreements | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| PM-APP | /pm/approvals | PropertyManager | Property Manager | App.tsx -> pm-approvals | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| PM-TEAM | /pm/team | PropertyManager | Property Manager | App.tsx -> pm-team | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| PM-INS | /pm/inspections | PropertyManager | Property Manager | App.tsx -> pm-inspections | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| PM-CLEAN | /pm/cleaning | PropertyManager | Property Manager | App.tsx -> pm-cleaning | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| PM-UTIL | /pm/utilities | PropertyManager | Property Manager | App.tsx -> pm-utilities | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| PM-VERIFY | /pm/verification | PropertyManager | Property Manager | App.tsx -> pm-verification | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| PM-RPT | /pm/reports | PropertyManager | Property Manager | App.tsx -> pm-reports | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| PM-GUARD | /gate | Public | Property Manager | App.tsx -> pm-gate | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, retry | P1 | TBD |
| PM-INSURANCE | /pm/insurance | PropertyManager | Property Manager | App.tsx -> pm-insurance | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P0 | TBD |
| OWNER-01 | /owner/dashboard | Owner | Owner | App.tsx -> owner-dashboard | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| CAL-01 | /calendar | Host | Host | App.tsx -> calendar | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| BOOKINGS-01 | /bookings | Host | Host | App.tsx -> bookings | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| PAYMENT-01 | /payment-confirmation | Guest, Host, PropertyManager, Owner, ServiceProvider, LocalBusiness, Officer, Admin | Booking | App.tsx -> payment | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| PROFILE-01 | /profile | Guest, Host, PropertyManager, Owner, ServiceProvider, LocalBusiness, Officer, Admin | Account | App.tsx -> profile | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| DIR-ADM | /admin/ops/directory | Admin | Admin | App.tsx -> admin-ops | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P0 | TBD |
| ADM-WELLNESS | /admin/ops/wellness | Admin | Admin | App.tsx -> admin-ops | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P0 | TBD |
| ADM-BADGES | /admin/ops/badges | Admin | Admin | App.tsx -> admin-ops | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P0 | TBD |
| ADM-01 | /admin/ops/disputes | Admin | Admin | App.tsx -> admin-ops | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| ADM-ROOT | /admin | Admin | Admin | App.tsx -> admin | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P1 | TBD |
| ADM-KPI | /admin/kpis | Admin | Admin | App.tsx -> admin-kpis | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P0 | TBD |
| ADM-RPT | /admin/reports | Admin | Admin | App.tsx -> admin-reports | API-backed / guarded | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P0 | TBD |
| ADM-RESET | /admin/officer-id-reset | Admin | Admin | App.tsx -> officer-id-reset | Preview / migration | Yes | Yes | Yes | loading, empty, API error, 401/403, retry | P2 | TBD |
| ERR-401 | /401 | Public | System states | App.tsx -> sign-in-required | Error state | Yes | Yes | Yes | error, focus, recovery | P2 | TBD |
| ERR-403 | /403 | Public | System states | App.tsx -> access-restricted | Error state | Yes | Yes | Yes | error, focus, recovery | P2 | TBD |
| ERR-404 | /404 | Public | System states | App.tsx -> not-found | Error state | Yes | Yes | Yes | error, focus, recovery | P2 | TBD |
| ERR-500 | /500 | Public | System states | App.tsx -> server-error | Error state | Yes | Yes | Yes | error, focus, recovery | P2 | TBD |
| ERR-LOAD | /loading | Public | System states | App.tsx -> loading-state | Internal reference | Yes | Yes | Yes | loading, empty, API error, retry | P2 | TBD |
| ERR-NOFAV | /empty/favorites | Guest, Host, PropertyManager, Owner, ServiceProvider, LocalBusiness, Officer, Admin | System states | App.tsx -> no-favorites | Error state | Yes | Yes | Yes | error, focus, recovery | P2 | TBD |
| ERR-NORES | /empty/reservations | Guest, Host, PropertyManager, Owner, ServiceProvider, LocalBusiness, Officer, Admin | System states | App.tsx -> no-reservations | Error state | Yes | Yes | Yes | error, focus, recovery | P2 | TBD |
| SCREEN-ROUTE | /screens/:screenId | Public | Internal | App.tsx -> design-screen | Internal reference | Yes | Yes | Yes | loading, empty, API error, retry | P2 | TBD |
