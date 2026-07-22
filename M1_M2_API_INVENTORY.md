# NestyStay M1/M2 API Inventory

Base path: `/api`. Frontend proxy: Vite forwards `/api` to `http://localhost:5019`.

## Health And Documentation

| Method | Endpoint | Auth | Request | Purpose |
| --- | --- | --- | --- | --- |
| GET | `/health` | Public | None | Backend health and OpenAPI location |
| GET | `/backend-schema/rules` | Public | None | Backend rule catalog |
| GET | `/backend-schema/seed/pricebook` | Public | None | Seed pricebook read view |
| GET | `/backend-jobs` | Public | None | Planned job catalog |
| GET | `/openapi/v1.json` | Public | None | OpenAPI document |

## Authentication

| Method | Endpoint | Auth | Request | Purpose |
| --- | --- | --- | --- | --- |
| POST | `/auth/register` | Public | email, password, display name, phone | Register local user and start 2FA requirement |
| POST | `/auth/login` | Public | email, password | Start login challenge |
| POST | `/auth/2fa/verify` | Public | challenge id, code | Complete login and return local bearer token |
| POST | `/auth/google` | Public | Google identity payload | Local Google sign-in path |
| GET | `/spec/auth/social-config` | Public | None | Tell frontend which social providers are configured |
| POST | `/spec/auth/flows` | Public | user id, flow type, destination | Start email, phone, OTP, reset, and verification flows |
| POST | `/spec/auth/flows/complete` | Public | flow id, code | Complete verification/reset/OTP flow |
| POST | `/spec/auth/{userId}/recovery-codes` | Local user bearer token | None | Generate persisted 2FA recovery codes |

## Properties And Search

| Method | Endpoint | Auth | Request | Purpose |
| --- | --- | --- | --- | --- |
| GET | `/properties` | Public | Optional query filters | Property listing grid/search data |
| GET | `/properties/{id}` | Public | None | Property detail data |
| POST | `/properties` | Local workflow payload | host/listing details | Host property creation with badge/verification rules |

## Booking And Payment

| Method | Endpoint | Auth | Request | Purpose |
| --- | --- | --- | --- | --- |
| POST | `/bookings/quote` | Public | property id, dates | Availability and pricing quote |
| POST | `/bookings` | Public/local user id payload | property id, guest id, dates | Create booking, hold dates, start eKYC/payment authorization |
| GET | `/bookings` | Public/local dashboards | Optional filters | Booking/admin read views |
| GET | `/bookings/{id}` | Public/local dashboards | None | Booking detail |
| POST | `/bookings/{id}/capture-payment` | Workflow/admin surface | None | Capture authorized payment only after approval |
| POST | `/webhooks/alibaba-ekyc` | Shared secret required in production | booking id, transaction id, pass/fail | eKYC pass/reject webhook |
| POST | `/webhooks/stripe` | Shared secret required in production | provider event | Stripe-style payment webhook |

## Badge, Pricing, Campaign, Founding Benefit

| Method | Endpoint | Auth | Request | Purpose |
| --- | --- | --- | --- | --- |
| GET | `/badges-pricing/pricebook` | Public | None | Read pricebook |
| PUT | `/badges-pricing/pricebook/{key}` | Admin bearer token | amount, currency, cadence | Update pricebook |
| GET | `/badges-pricing/badges` | Public | None | Badge definitions |
| POST | `/badges-pricing/badges/eligibility` | Public | subject, level, eligibility inputs | Badge eligibility calculation |
| GET | `/badges-pricing/badges/features/{subjectType}/{subjectId}` | Public | None | Feature locking/unlocking view |
| POST | `/badges-pricing/badges/purchase` | Public/local workflow payload | badge purchase request | Badge purchase, payment state, assignment |
| POST | `/badges-pricing/badges/assignments/{id}/expire` | Admin bearer token | None | Expire assignment |
| POST | `/badges-pricing/badges/assignments/{id}/suspend` | Admin bearer token | None | Suspend assignment |
| GET | `/badges-pricing/renewals` | Public | assignment id query | Renewal queue/read view |
| POST | `/badges-pricing/renewals/{assignmentId}/pay` | Public/local workflow payload | None | Renewal payment |
| GET | `/badges-pricing/campaigns` | Public | None | Campaign list |
| POST | `/badges-pricing/campaigns` | Admin bearer token | campaign details | Create campaign |
| POST | `/badges-pricing/campaigns/{key}/enroll` | Public/local workflow payload | subject | Enroll subject in campaign |
| POST | `/badges-pricing/founding-benefits` | Admin bearer token | property id, tier | Create founding benefit |
| GET | `/badges-pricing/founding-benefits/{propertyId}` | Public | None | Founding benefit status |
| POST | `/badges-pricing/founding-benefits/transfer-evaluation` | Public | transfer checklist | Founding transfer evaluation |
| POST | `/badges-pricing/commission-quote` | Public | booking value, nights, tier | Commission and guest-fee calculation |

## Public Content, Experiences, Journal

| Method | Endpoint | Auth | Request | Purpose |
| --- | --- | --- | --- | --- |
| POST | `/spec/seed` | Public | None | Ensure M1/M2 content seed exists |
| GET | `/spec/public/pages` | Public | None | Public content index |
| GET | `/spec/public/pages/{slug}` | Public | slug may include `/help/...` | About, trust, help, legal, contact, maintenance content |
| POST | `/spec/public/contact` | Public | name, email, subject, message | Persist contact request and audit event |
| GET | `/spec/experiences` | Public | category, parish, query | Experiences listing with filters |
| GET | `/spec/experiences/{slug}` | Public | None | Experience detail |
| GET | `/spec/journal` | Public | category, query | Journal listing |
| GET | `/spec/journal/{slug}` | Public | None | Journal article detail |

## Traveler Workspace

| Method | Endpoint | Auth | Request | Purpose |
| --- | --- | --- | --- | --- |
| GET | `/spec/traveler/{userId}` | Matching local user bearer token | None | Traveler workspace with wishlist, payments, reviews, notifications |
| POST | `/spec/traveler/{userId}/wishlist/collections` | Matching local user bearer token | name, sort order | Create wishlist collection |
| PUT | `/spec/traveler/{userId}/wishlist/collections/{collectionId}` | Matching local user bearer token | name, sort order | Rename/reorder collection |
| DELETE | `/spec/traveler/{userId}/wishlist/collections/{collectionId}` | Matching local user bearer token | None | Delete collection and items |
| POST | `/spec/traveler/{userId}/wishlist/collections/{collectionId}/items` | Matching local user bearer token | property id, title, status, order | Add saved stay |
| DELETE | `/spec/traveler/{userId}/wishlist/items/{itemId}` | Matching local user bearer token | None | Remove saved stay |
| POST | `/spec/traveler/{userId}/payment-methods` | Matching local user bearer token | card brand, last4, expiry, default | Add tokenized payment method |
| POST | `/spec/traveler/{userId}/payment-methods/{paymentMethodId}/default` | Matching local user bearer token | None | Set default card |
| DELETE | `/spec/traveler/{userId}/payment-methods/{paymentMethodId}` | Matching local user bearer token | None | Remove card |
| POST | `/spec/traveler/{userId}/reviews` | Matching local user bearer token | property/booking id, rating, text | Submit review |
| POST | `/spec/traveler/{userId}/notifications/{notificationId}/read` | Matching local user bearer token | None | Mark one notification read |
| POST | `/spec/traveler/{userId}/notifications/read-all` | Matching local user bearer token | None | Mark all notifications read |

## Messaging

| Method | Endpoint | Auth | Request | Purpose |
| --- | --- | --- | --- | --- |
| GET | `/spec/messages/inbox?userId={userId}` | Matching local user bearer token | user id query | Persisted inbox summary |
| GET | `/spec/messages/conversations/{conversationId}?userId={userId}` | Matching local user bearer token and participant | None | Conversation detail |
| POST | `/spec/messages/conversations?userId={userId}` | Matching local user bearer token | subject, booking id, participants, initial message | Create support/booking conversation |
| POST | `/spec/messages/conversations/{conversationId}/messages?userId={userId}` | Matching local user bearer token and participant | body, attachments metadata | Send message with attachment metadata |
| POST | `/spec/messages/conversations/{conversationId}/read?userId={userId}` | Matching local user bearer token and participant | None | Mark thread read and update receipts |

## Directories And Host Profiles

| Method | Endpoint | Auth | Request | Purpose |
| --- | --- | --- | --- | --- |
| GET | `/spec/directories/providers` | Public | kind, category, parish, query | Custodian, trades, local business, verification provider list |
| GET | `/spec/directories/providers/{slug}` | Public | None | Provider detail |
| POST | `/spec/directories/providers` | Any local user bearer token | provider profile fields | Provider onboarding/profile update |
| GET | `/spec/host-profiles` | Public | None | Public host profile directory |
| GET | `/spec/host-profiles/{slug}` | Public | None | Public host profile detail |
| PUT | `/spec/host-profiles/{slug}` | Matching host local bearer token | host profile fields | Host profile edit/preview persistence |

## Host Operations

| Method | Endpoint | Auth | Request | Purpose |
| --- | --- | --- | --- | --- |
| GET | `/spec/host/{hostUserId}/operations` | Matching host local bearer token | None | Analytics, pricing rules, promotions, reviews |
| POST | `/spec/host/{hostUserId}/pricing-rules` | Matching host local bearer token | property, date range, rate, min stay | Create seasonal pricing rule |
| POST | `/spec/host/{hostUserId}/promotions` | Matching host local bearer token | property, discount, dates, min nights, badge | Create promotion |
| POST | `/spec/host/{hostUserId}/reviews/{reviewId}/reply` | Matching host local bearer token | reply | Reply to traveler review |

## Admin Operations

| Method | Endpoint | Auth | Request | Purpose |
| --- | --- | --- | --- | --- |
| GET | `/spec/admin/operations` | Admin bearer token | None | Admin cases, audit events, metrics |
| POST | `/spec/admin/cases` | Admin bearer token | case type, subject, priority, reason, assignee | Create moderation/refund/fraud/support case and audit record |
| POST | `/spec/admin/cases/{caseId}/resolve` | Admin bearer token | resolution notes, status | Resolve case and audit record |
| GET | `/spec/admin/audit-log` | Admin bearer token | None | Admin audit log viewer |

## Authorization Notes

- Admin mutation endpoints use `NESTYSTAY_ADMIN_TOKEN_SHA256` in real environments. Local development uses the configured dev token.
- Operator tokens authenticate but are forbidden on admin-only endpoints.
- Local user protected endpoints require `local-phase1-token-{userId:N}` or `local-google-token-{userId:N}` and enforce owner id matching.
- Production startup guards still require real Stripe, Alibaba Cloud eKYC, R2, InsuraGuest, webhook, and admin secrets before launch.
