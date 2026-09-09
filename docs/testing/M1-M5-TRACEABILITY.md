# M1–M5 traceability

Primary contractual source: `docs/contracts/NestyStay-Signed-Agreement-April-2026.pdf` (11 pages, SHA-256 `0C4AAD0B1A2D015433C0875107DD171C93C4191281D7CCCBBE748B37DB4FD28D`). M5 includes the signed Phase 5 property-manager suite plus the client-added governance, anonymous voting and proxy controls.

| Milestone | Contractual outcome | Backend/API | Frontend | Browser/API evidence | Decision |
|---|---|---|---|---|---|
| M1 Core | Auth/2FA, property, booking, payment, eKYC, persisted dashboards | Implemented and regression-tested | Connected routes and workflows | Existing M1–M4 suites plus 19-route inventory | PASS locally |
| M2 Badges | Four badge levels, pricing/eligibility/renewal and restrictions | Implemented and regression-tested | Connected badge/dashboard surfaces | Existing M1–M4 suites | PASS locally |
| M3 Wellness | Officer lifecycle, assignment/report, subscription/commission state | Implemented and regression-tested | Connected host/officer/admin routes with guided wizard, coverage coordinates, secure documents, live quote/availability, conflict-safe rescheduling, report drafts/photos, privacy controls and payout desk | Wellness lifecycle/enhancement suites, 84 API tests and PostgreSQL-backed browser runs | PASS locally |
| M4 Directories + QR | Four directories, provider moderation, badge gates, QR lifecycle | Implemented and regression-tested | Connected directory/provider/QR routes, provider analytics/quote responses/review replies, recent views and lifecycle history | M1–M5 browser suites, provider dashboard browser test and API/PostgreSQL regression coverage | PASS locally |
| M5 Property Manager | Multi-owner portfolio, owner portal, finance, maintenance, utilities, governance, documents, gate and subscription | Implemented with manager/owner scope, idempotency and reviewed FK relationships | `/pm/*`, `/owner/dashboard`, `/gate` call real API with HttpOnly cookie sessions and CSRF protection; notices support scheduling, audience targeting and acknowledgement deadlines | 84 API tests, 35 Vitest, combined M1–M5 workflow 12/12 desktop Chromium; hardening/security 4 passed with 5 intentional project skips | PASS locally |

## Provider and launch separation

| Area | Local application | Real provider / production |
|---|---|---|
| Stripe | PASS (adapter, local capture, idempotency) | BLOCKED pending live credentials/webhook/Connect validation |
| eKYC | PASS (application boundary) | BLOCKED pending Alibaba credentials and signed callback validation |
| Security | PASS for local scope and authorization tests | Production review, WAF, secret manager and monitoring required |
| Launch | Not claimed | NO until deployment checklist is complete |

## M3 usability upgrade coverage

- Host: three-step booking flow (service → schedule → confirm), real quote and available-officer lookup, property selector, subscription comparison/renewal/cancellation, persisted visit timeline, cancellation/rebooking action, report print/save, and explicit local-payment mode copy.
- Officer: three-step onboarding wizard with autosaved draft/resume, document checklist, coverage preview, availability calendar, consent/privacy copy, assignment timeline, offline report draft, multi-photo upload with progress/retry/cancel, client-side image compression, captions, and secure visibility messaging.
- Admin: live officer search/status/parish filters, side-by-side review checklist, approval templates, notification/audit messaging, bulk approval/rejection/suspension, SLA/open-report/payout KPIs, assignment and completion controls, and payout desk.
- Honest boundaries: coverage coordinates, server-rendered report PDFs, payout statements and dispute state are now available locally. Live provider payment rails, external bank verification, SMS/push delivery and production reminder/expiry workers remain separate certification/deployment gates.

## M4/M5 enhancement checklist (2026-09-02)

| Area | Checklist coverage | Status / evidence |
|---|---|---|
| M4 directories | Custodian, Trades, Local Business and Police routes use live PostgreSQL provider data; category/parish/search filters, availability filter, sort, pagination, CSV export, list/map toggle, saved searches, favorites, ratings, verified badges, one-click contact, business directions/promotions/accessibility copy, Trades EITA labeling, Police emergency/non-emergency separation and permanent 119 tap-to-call | PASS locally; `frontend/e2e/m4-m5-enhancements.spec.ts` and route inventory across desktop/tablet/mobile |
| M4 badge gating | Server-side badge gate remains authoritative; lock screen explains required badge, indicative price, benefits and upgrade path without discarding search context | PASS locally; direct API authorization tests plus browser lock states |
| M4 guest verification | Contextual value/privacy/pricing explanation with Start verification and Not now actions | PASS locally; enhancement browser test |
| M4 provider onboarding | Three-step profile/availability/documents flow, checklist, terms acceptance, draft/resume, preview link, persisted moderation status and secure document vault | PASS locally; provider API save, scoped prepare/upload/content/download endpoints, scanner validation and browser draft/validation test. |
| M4 moderation | Admin queue endpoint (`GET /api/directories/providers/moderation`) returns unpublished records; searchable status queue supports compare/review, approve, reject and request-changes with reason and privileged audit record | PASS locally; `AdminDirectories` screen and API-backed browser test |
| M4 QR/gate | Issue templates and expiry presets, property/subject selection, QR preview/download/copy, validator camera/manual fallback, readable valid/invalid/mismatch/revoked/expired states, offline retry guidance and revoke path | PASS locally; existing QR lifecycle suite plus enhancement browser test |
| M5 manager dashboard | Role-scoped PostgreSQL dashboard, configurable KPI cards, alert strip, portfolio search/sort/pagination/export, owner invite duplicate warning/resend action, assignment, invoice templates/tax/recurring reminder, utility meter history/anomaly guidance, maintenance list/Kanban/SLA, vendor/community/governance/document/gate controls, subscription status/renew action and owner portal deep link | PASS locally; dedicated persisted M5 workflow verifies dashboard reads, maintenance update, QR issue/revoke and no console/5xx errors in `testing-evidence/final-hardening/07-browser/m5-*.json` |
| M5 owner/finance/governance | Owner portal shows units, balances, invoices/payments, printable statements, maintenance, notices, anonymous vote explanation and proxy action; manager documents accept validated files, scope records and authorized downloads | PASS locally; owner portal browser journey plus API authorization matrix |
| M5 guard interface | Large guard-first camera affordance, manual fallback, minimal visitor note, explicit property/status confirmation and offline retry | PASS locally; revoked QR is visibly denied in the M5 browser journey |

### Honest M4/M5 boundaries

- The current API stores wellness coverage coordinates and service radius; map tiles/geocoding remain configurable provider integrations with manual fallback.
- Saved-search/favorite persistence and plan selection remain configurable/client-side enhancements where no signed-contract persistence rule exists; provider analytics, quote responses, review responses, scheduled/audience-targeted notices and recurring workflow records now use real API calls. Asynchronous ZIP export/expiry-reminder workers remain a production follow-up.
- Real Stripe/Alibaba credentials, live payment rails, SMS/push delivery, geocoding network, bank reconciliation and production deployment controls remain separate production validation gates. QR image decoding is implemented with the browser `BarcodeDetector` API and keeps a manual fallback.
- Browser session storage is an HttpOnly `nestyStay.session` cookie with a strict double-submit CSRF token; legacy bearer values are cleared from localStorage. PostgreSQL relationship hardening added 45 reviewed FKs with 0 enforced M5 orphan rows; the contextual QR mismatch value is intentionally retained without an FK.

### Property listing lifecycle enhancement (2026-09-05)

- `POST /api/properties/{id}/duplicate` creates an owner-scoped draft and copies only clean uploaded photos.
- `POST /api/properties/{id}/publish` publishes an owner-scoped draft after review.
- `GET /api/properties/{id}/revisions` exposes immutable, versioned snapshots; `POST /api/properties/{id}/revisions/{revisionId}/restore` restores a selected snapshot as a new draft revision.
- `POST /api/properties/bulk/archive` performs one owner-scoped mutation for up to 100 properties and records a revision for every changed listing.
- Host UI now exposes Draft/Publish, Duplicate, History/Restore, page selection and transactional bulk archive actions. Public listing reads continue to exclude drafts and archived records, while the host-owned view includes both for management.
- Evidence: `backend/tests/NestyStay.Api.Tests/PropertyEnhancementEndpointTests.cs` (duplicate → owned draft → revision restore → publish → bulk archive) and `frontend/src/lib/api.test.ts` (API client lifecycle contract). Backend API suite: 65 passed; frontend unit suite: 32 passed; browser usability suite: 4 passed, 1 intentional mobile skip.
- The notification center now hydrates authenticated users from `GET /api/spec/traveler/{userId}` and persists read/read-all actions through the real notification endpoints; unauthenticated visitors retain a local preference-only view and are not presented with false delivery success. API client coverage includes load, read and read-all calls.
