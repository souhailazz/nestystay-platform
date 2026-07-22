# NestyStay M1/M2 Route Inventory

Generated from `frontend/src/App.tsx` after the M1/M2 completion pass.

## Public

| Route | Purpose | Source |
| --- | --- | --- |
| `/` | Landing page with restored original hero/scroll experience | `App.tsx`, landing components |
| `/explore` | Listing grid, filters, sort, booking entry | `ExplorePage` |
| `/explore/map` | Map search state | `MapSearchPage` |
| `/properties/:propertyId` | Property detail, gallery, booking sidebar/modal | `PropertyDetailsPage` |
| `/about` | Public content page | `PublicContentRoute` |
| `/trust` | Trust and safety page | `PublicContentRoute` |
| `/help` | Help center | `PublicContentRoute` |
| `/help/:slug` | Help category/article detail | `PublicContentRoute` |
| `/contact` | Contact page and submission | `PublicContentRoute` |
| `/terms` | Terms of service | `PublicContentRoute` |
| `/privacy` | Privacy policy | `PublicContentRoute` |
| `/maintenance` | Maintenance state | `PublicContentRoute` |
| `/coming-soon` | Coming soon state | `ComingSoonPage` |

## Authentication

| Route | Purpose | Source |
| --- | --- | --- |
| `/login` | Core login | `AuthPage` |
| `/register` | Core registration | `AuthPage` |
| `/auth/role` | Come Een role selection | `AuthSpecFlowPage` |
| `/auth/email-verification` | Respek email verification | `AuthSpecFlowPage` |
| `/auth/phone-verification` | Phone verification | `AuthSpecFlowPage` |
| `/auth/otp` | OTP entry/resend | `AuthSpecFlowPage` |
| `/auth/forgot-password` | Forgot password | `AuthSpecFlowPage` |
| `/auth/reset-password` | Reset password state | `AuthSpecFlowPage` |
| `/auth/2fa-setup` | QR 2FA setup | `AuthSpecFlowPage` |
| `/auth/recovery-codes` | Recovery code generation/copy/download surface | `AuthSpecFlowPage` |
| `/auth/social-consent` | Social auth config and disabled states | `AuthSpecFlowPage` |
| `/auth/post-login-toast` | Auth post-login state | `AuthPostLoginToastPage` |
| `/logout` | Logout state | `LogoutScreenPage` |

## Booking And Payment

| Route | Purpose | Source |
| --- | --- | --- |
| `/booking/:bookingId/review` | Booking review | `BookingSpecStatePage` |
| `/booking/:bookingId/checkout` | Card checkout | `BookingSpecStatePage` |
| `/booking/:bookingId/payment-processing` | Processing state | `BookingSpecStatePage` |
| `/booking/:bookingId/payment-success` | Payment success | `BookingSpecStatePage` |
| `/booking/:bookingId/payment-failure` | Payment failure/retry | `BookingSpecStatePage` |
| `/booking/:bookingId/pending` | Pending approval/eKYC | `BookingSpecStatePage` |
| `/booking/:bookingId/approved` | Approved booking | `BookingSpecStatePage` |
| `/booking/:bookingId/rejected` | Rejected booking | `BookingSpecStatePage` |
| `/booking/:bookingId/cancelled` | Cancelled/refund state | `BookingSpecStatePage` |
| `/booking/:bookingId/expired` | Expired hold state | `BookingSpecStatePage` |
| `/booking/:bookingId/invoice` | Invoice view | `BookingSpecStatePage` |
| `/booking/:bookingId/receipt` | Receipt view | `BookingSpecStatePage` |
| `/bookings` | Booking/admin management | `BookingManagementPage` |
| `/payment-confirmation` | Payment capture/confirmation | `PaymentConfirmationPage` |

## Traveler

| Route | Purpose | Source |
| --- | --- | --- |
| `/guest-dashboard` | Traveler dashboard | `GuestDashboardPage` |
| `/traveler/reservations` | Upcoming reservations | `TravelerSpecPage` |
| `/traveler/reservations/upcoming` | Upcoming reservations | `TravelerSpecPage` |
| `/traveler/reservations/past` | Past reservations | `TravelerSpecPage` |
| `/traveler/reservations/cancelled` | Cancelled reservations | `TravelerSpecPage` |
| `/traveler/reservations/:id` | Reservation detail/status timeline | `TravelerSpecPage` |
| `/traveler/payment-methods` | Add/remove/default payment methods | `TravelerSpecPage` |
| `/traveler/payments` | Payment history | `TravelerSpecPage` |
| `/traveler/invoices` | Invoice access | `InvoicesPage` |
| `/traveler/preferences` | Notification/communication/language preferences | `TravelerSpecPage` |
| `/traveler/identity` | Identity verification status | `TravelerSpecPage` |
| `/traveler/reviews/given` | Reviews given | `TravelerSpecPage` |
| `/traveler/reviews/pending` | Pending reviews | `TravelerSpecPage` |
| `/traveler/qr/:id` | QR gate access state | `TravelerSpecPage` |
| `/traveler/favorites` | Favorites collections | `FavoritesCollectionsPage` |
| `/wishlist` | Wishlist alias | `FavoritesCollectionsPage` |
| `/traveler/reviews` | Reviews hub | `PendingReviewsPage` |
| `/traveler/notifications` | Notifications center | `NotificationsCenterPage` |
| `/notifications` | Notifications alias | `NotificationsCenterPage` |
| `/traveler/suggestions` | Trip suggestions | `TripSuggestionsPage` |
| `/profile` | Profile settings | `ProfileSettingsPage` |

## Messaging

| Route | Purpose | Source |
| --- | --- | --- |
| `/messages` | Persisted inbox/conversation workspace | `MessagesPage` |
| `/messages/:conversationId` | Persisted chat thread | `MessagesPage` |
| `/messages/document` | Document/message attachment state | `DocumentMessagePage` |

## Host

| Route | Purpose | Source |
| --- | --- | --- |
| `/host-dashboard` | Host revenue/dashboard | `HostDashboardPage` |
| `/host/analytics` | Analytics and chart series | `HostSpecPage` |
| `/host/pricing` | Seasonal pricing rules | `HostSpecPage` |
| `/host/promotions` | Promotions | `HostSpecPage` |
| `/host/exports` | CSV/export controls | `HostSpecPage` |
| `/host/reviews` | Review management and replies | `HostSpecPage` |
| `/host/badges` | Badge progress/history/account settings | `HostSpecPage` |
| `/host/settings` | Host account/notification settings | `HostSpecPage` |
| `/host/properties` | Host property list | `PropertyManagementPage` |
| `/host/properties/edit` | Property editing | `HostPropertyEditPage` |
| `/host/properties/archived` | Archived properties/restore | `HostSpecPage` |
| `/host/reports` | Host reports | `HostReportsPage` |
| `/calendar` | Host calendar/bookings view | `CalendarPage` |

## Host Profiles

| Route | Purpose | Source |
| --- | --- | --- |
| `/hosts` | Public host profile directory | `HostProfileSpecPage` |
| `/hosts/:slug` | Public host profile detail | `HostProfileSpecPage` |
| `/host/profile/edit` | Host profile editor | `HostProfileSpecPage` |
| `/host/profile/preview` | Host profile preview | `HostProfileSpecPage` |

## Directories And Providers

| Route | Purpose | Source |
| --- | --- | --- |
| `/directory/custodians` | Custodian directory | `DirectorySpecPage` |
| `/directory/trades` | Trades directory | `DirectorySpecPage` |
| `/directory/businesses` | Local-business directory | `DirectorySpecPage` |
| `/directory/guest-verification` | Guest-verification upsell | `DirectorySpecPage` |
| `/directory/provider/onboarding` | Protected provider onboarding | `DirectorySpecPage` |
| `/directory/provider` | Protected provider self-management dashboard | `DirectorySpecPage` |
| `/directory/providers/:slug` | Provider detail | `DirectorySpecPage` |
| `/business-directory` | Legacy public business directory screen | `BusinessDirectoryPage` |

## Admin

| Route | Purpose | Source |
| --- | --- | --- |
| `/admin` | Admin dashboard controls | `AdminPage` |
| `/admin/kpis` | Platform KPI charts | `AdminKpiPage` |
| `/admin/reports` | Reports/export hub | `AdminReportsPage` |
| `/admin/officer-id-reset` | Officer ID reset view | `OfficerIdResetPage` |
| `/admin/ops/users` | User management/flagged users | `AdminOpsSpecPage` |
| `/admin/ops/properties` | Property moderation | `AdminOpsSpecPage` |
| `/admin/ops/reservations` | Reservation override queue | `AdminOpsSpecPage` |
| `/admin/ops/refunds` | Refund management | `AdminOpsSpecPage` |
| `/admin/ops/disputes` | Disputes | `AdminOpsSpecPage` |
| `/admin/ops/support` | Support tickets | `AdminOpsSpecPage` |
| `/admin/ops/fraud` | Fraud queue | `AdminOpsSpecPage` |
| `/admin/ops/audit` | Audit log viewer | `AdminOpsSpecPage` |

## Error And Empty States

| Route | Purpose | Source |
| --- | --- | --- |
| `/401` | Login required | `SignInRequiredPage` |
| `/403` | Access restricted | `AccessRestrictedPage` |
| `/404` | Not found | `NotFoundPage` |
| `/500` | Server error | `ServerErrorPage` |
| `/empty/favorites` | No favorites | `NoFavoritesPage` |
| `/empty/reservations` | No reservations | `NoReservationsPage` |

## M3/Adjacent Routes Kept Working

| Route | Purpose |
| --- | --- |
| `/host/wellness` | Host wellness milestone |
| `/host/wellness/directory` | Officer directory |
| `/host/wellness/book` | Wellness visit booking |
| `/officer/wellness` | Officer wellness portal |
| `/pm/gates` | Property-manager gate access |
| `/pm/utilities` | Property-manager utilities |
| `/pm/verification` | Property-manager verification |
| `/pm/reports` | Property-manager reports |
| `/pm/insurance` | InsuraGuest screen |
