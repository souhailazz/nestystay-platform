# NestyStay — Platform UI Spec & Screen Inventory
v2 · July 2026 · Companion to `NestyStay Design System v2.dc.html` (visual source of truth = approved PUB-01).
Requirements source: `uploads/BRIEF_CLAUDE_DESIGN-f5bc2d9a.md` (route inventory, backend capabilities, gap matrix §5/§7/§9).

## 0. Global conventions (apply to every screen)
- **Shells**: Public = floating pill navbar (DS-12). Authenticated = deep-green sidebar + sand canvas. Routing is the in-house SPA router; URLs are frozen (see brief §7).
- **Data**: `[CORE]` = live API (DESIGN_BRIEF §2 fields verbatim) · `[SPEC]` = designed against founder brief, backend to follow · `[VISION]` = API blueprint portals. Every SPEC/VISION screen states its dependency in a code comment.
- **States trio**: every list/page ships empty / loading (Tek Time skeleton) / error (verbatim backend message + retry; WhatsApp link on 5xx) / success.
- **Permissions**: roles = visitor, guest, host, pm, officer, admin. Unauthorized → ERR-401; wrong role → ERR-403. No permission detail leaked on 403.
- **Forms**: client validation inline (coral), server errors verbatim in error zones. Password ≥8 + upper+lower+digit. All money `Intl.NumberFormat en-US` USD; PERCENT pricebook items shown as %.
- **Navigation**: every CTA lists its destination route in the design; no dead ends — every terminal state offers "next" (Explore / Dashboard / support).
- **Responsive**: desktop 1440 / tablet ~900 (sidebar collapses to icons) / mobile 390 (sidebar → bottom nav; tables → stacked cards; overlaps/parallax off).
- **Non-negotiables** on every applicable screen: 119 badge rule, officer badge-ID-only (NST-OFC-XXXX), "NEVER AUTOMATIC" verification toggle, Trusted Badge "$120/year or $12/month", patois lexicon only + paired translation, yellow-on-dark only, 44px targets, footer `nestystay.net · 754-248-2435`.

## 1. Route & screen inventory (by batch)

### Batch 2 — Public discovery (restyle existing to v2 language)
| ID | Route | Data | Status |
|---|---|---|---|
| PUB-01 `/` | showcase properties GET /properties | ✅ approved (source of truth) |
| PUB-02 `/explore` | GET /properties, client filters | built — restyle to Fraunces/sand |
| PUB-04 `/property/:id` | GET /properties/:id | built — restyle |
| PUB-MAP `/explore/map` | properties + static SVG map (no GPS in DB — §9.6) | built — restyle |
| PUB-SOON `/coming-soon` | static + email capture POST (to define) | built — restyle |

### Batch 3 — Auth & identity
AUTH-01 `/login` `/register` (POST /auth/login|register, Google overlay, errors verbatim) · AUTH-2FA (6-digit, 10-min expiry, resend) · AUTH-RECOVER (email flow) · AUTH-POST toast (Yuh Gud?, patois-toggle aware) · AUTH-LOGOUT (Likkle More full-page) · eKYC flows are external redirects (Alibaba URL) — design the hand-off + return states only.

### Batch 4 — Booking & payments
BOOK-01→06 (dates → quote 12/10/8% degressive fees, refundable vs non-refundable lines → eKYC doc choice Passport/National ID/Driver license → create) · BOOK-07 pending + "Nuh Fret" verification state with **60-min hold countdown (holdExpiresAt)** + "Open eKYC transaction" link · payment (Stripe Elements slot, paymentClientSecret; Authorized→Captured) · cancel/refund modal (refundable split) · invoice/receipt (NST-2026-XXXX).

### Batch 5 — Traveler portal `/trips…`
TRAV-01 dashboard (triple statuses booking/verification/payment + event timeline) · TRAV-COL collections · TRAV-INV invoices table + downloads · TRAV-PEND reviews (deadline countdown, closed state) · TRAV-NOTIF notifications (filters, unread) · TRAV-SUGG suggestions (match-reason tags) · TRAV-12 settings (**prominent patois toggle card**, session, logout) · MSG inbox/threads + MSG-DOC secure file bubble.

### Batch 6 — Host portal "Manage Your Yard"
HOST-01 dashboard (MetricCards, property cards) · HOST-05 creation wizard (exact backend fields; step 8 = verification toggle rule 2 + 4 price options ⚠§9.2) · HOST-EDIT inline edit (no endpoint yet ⚠§9.8) · HOST-RPT reports/exports/tax · HOST-WELL wellness (quote incl. officer payout, visit statuses Requested→Scheduled→Completed, payment Authorized→Captured→PaidOut) · badge upsell (eligibility reasons verbatim; prices rule 5; renewal J-30 countdown) · calendar/pricing.

### Batch 7 — PM & Officer portals
PM-GATE guard threads · PM-UTIL utility bill-back · PM-VERIFY tenant/owner eKYC · PM-RPT portfolio reports · PM-INS InsuraGuest plans ($50/$69/$99 + disclosure) — all [VISION].
OFC-01 onboarding (retired = auto-reject; annual-reset disclosure) · OFC-02 visit report (photos+notes; assigned-only, not before scheduled time) · OFC-DIR police directory (wellness hosts only; "WELLNESS ACCESS REQUIRED"; badge-ID rows, zero names) · OFC-BOOK visit booking (3 types ⚠ pricing conflict §9.1 — flag, don't decide). Traveler-facing copy says "private security" never "police" (§9.4).

### Batch 8 — Directories, Admin, Errors
DIR-02 trades (EITA credit) · DIR-BIZ local business · DIR-PROV provider dashboard.
ADM-01 ops dashboard (wellness ops actions, pricebook 16 items, bearer-token field, officer-ID-reset tile) · ADM-KPI analytics (4 charts + periods + CSV) · ADM-RPT compliance reports · ADM-RESET (No Override / Zero Trace).
ERR-401/403/404 ("Dis page gone a sea")/500 (WhatsApp) /NOFAV/NORES/LOAD skeletons.

## 2. Open items requiring client decision (never decided in design)
Wellness visit pricing & naming (§9.1) · host-side verification pricing options (§9.2) · Trusted monthly $12 (§9.3) · traveler-side "police" leakage audit (§9.4) · officer ID generation (§9.5) · map coordinates (§9.6) · property photos/R2 (§9.7) · property edit endpoint (§9.8) · open Qs 1/3/9/10/13/16bis/17 (brief §8).
