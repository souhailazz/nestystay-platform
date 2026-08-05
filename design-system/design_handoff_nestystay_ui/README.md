# Handoff: NestyStay Platform UI — Design System v2 Re-skin

## Overview
Complete UI redesign for the NestyStay platform (souhailazz/nestystay-platform). 54 designed screens covering every role (public, guest, host, PM, officer, admin), all derived from the approved PUB-01 landing page. The repo frontend already implements **all routes and logic** (see `M1_M2_ROUTE_INVENTORY.md` in the repo) — this handoff is a **re-skin, not a rebuild**: apply the Design System v2 visual language to the existing React components.

## About the Design Files
The `screens/` folder contains **design references created in HTML** — prototypes showing intended look and behavior, not production code to copy. The task is to **recreate these designs inside the existing codebase**: React 19 + Vite + Tailwind CSS v4 (`@tailwindcss/vite`), framer-motion/gsap available, lucide-react icons, no router lib (in-house routing in `App.tsx`). Open any screen in a browser to inspect it (each is standalone; `support.js` must sit next to them).

## Fidelity
**High-fidelity.** Colors, typography, spacing, radii, shadows and copy are final. Recreate pixel-perfectly with the codebase's existing patterns (`components/ui/*`, `features/*`).

## Where to start (implementation order)
1. **Tokens** — drop `nestystay-theme.css` into `frontend/src/`, import at the top of `index.css`, add the Google Fonts `<link>` (Fraunces + Sora) to `frontend/index.html`. Then migrate/remove conflicting legacy tokens from `index.css` (137KB — prune as screens migrate).
2. **UI kit** (`frontend/src/components/ui/`) — restyle to DS v2 (recipes below): Button, Badge, Card, Input, Modal, EmptyState, ErrorState, LoadingState, PageHeader, PatoisToast + `layout/WorkspaceFrame.tsx` (sidebar shell).
3. **Public** — `/` (PUB-01), `/explore` (PUB-02), `/properties/:id` (PUB-04), `/explore/map` (PUB-MAP), `/coming-soon` (PUB-SOON).
4. **Auth** — AUTH-01/POST/LOGOUT over `features/auth/*` + auth spec pages.
5. **Booking** — BOOK-01→CONF over `features/booking/*`.
6. **Traveler + Messaging** — TRAV-*/MSG-* over `features/traveler/*`, `features/messaging/*`.
7. **Host, PM, Officer, Directories, Admin** — HOST-*/PM-*/OFC-*/DIR-*/ADM-* over `features/host/*`, `features/admin/*`, `pages/SpecScreens.tsx`.
8. **Errors** — ERR-* over `pages/CompletionPages.tsx`.

## Route → screen map
| Repo route | Design reference |
| --- | --- |
| `/` | screens/PUB-01.dc.html |
| `/explore` | screens/PUB-02.dc.html |
| `/properties/:propertyId` | screens/PUB-04.dc.html |
| `/explore/map` | screens/PUB-MAP.html |
| `/coming-soon` | screens/PUB-SOON.dc.html |
| `/login`, `/register` | screens/AUTH-01.dc.html |
| `/auth/post-login-toast` | screens/AUTH-POST.dc.html |
| `/logout` | screens/AUTH-LOGOUT.dc.html |
| `/booking/:id/review` | screens/BOOK-01.dc.html (dates) + BOOK-02.dc.html (quote) |
| eKYC document choice | screens/BOOK-03.dc.html |
| `/booking/:id/checkout` | screens/BOOK-05.dc.html |
| `/booking/:id/pending` | screens/BOOK-07.dc.html (Nuh Fret + 60-min hold) |
| `/booking/:id/approved` + `/receipt` | screens/BOOK-CONF.dc.html |
| `/guest-dashboard` | screens/TRAV-01.dc.html |
| `/traveler/favorites`, `/wishlist` | screens/TRAV-COL.dc.html |
| `/traveler/invoices` | screens/TRAV-INV.dc.html |
| `/traveler/reviews/pending` | screens/TRAV-PEND.dc.html |
| `/traveler/notifications` | screens/TRAV-NOTIF.dc.html |
| `/traveler/suggestions` | screens/TRAV-SUGG.dc.html |
| `/profile` | screens/TRAV-12.dc.html (patois toggle card — prominent, default ON) |
| `/messages`, `/messages/:id` | screens/MSG-01.dc.html |
| `/messages/document` | screens/MSG-DOC.dc.html |
| `/host-dashboard` | screens/HOST-01.dc.html |
| Host property wizard | screens/HOST-05.dc.html (step 8 = "NEVER AUTOMATIC" verification toggle) |
| `/host/properties/edit` | screens/HOST-EDIT.dc.html |
| `/host/reports`, `/host/exports` | screens/HOST-RPT.dc.html |
| `/host/wellness` | screens/HOST-WELL.dc.html |
| `/host/badges` | screens/HOST-BADGE.dc.html ("$120/year or $12/month" locked copy) |
| `/pm/gates` | screens/PM-GATE.dc.html |
| `/pm/utilities` | screens/PM-UTIL.dc.html |
| `/pm/verification` | screens/PM-VERIFY.dc.html |
| `/pm/reports` | screens/PM-RPT.dc.html |
| `/pm/insurance` | screens/PM-INS.dc.html ($50/$69/$99 + disclosure) |
| Officer onboarding | screens/OFC-01.dc.html (retired = auto-reject) |
| Officer visit report | screens/OFC-02.dc.html |
| `/host/wellness/directory` | screens/OFC-DIR.dc.html (badge-ID rows, zero names) |
| `/host/wellness/book` | screens/OFC-BOOK.dc.html |
| `/directory/trades` | screens/DIR-02.dc.html (EITA credit) |
| `/directory/businesses` | screens/DIR-BIZ.dc.html |
| `/directory/provider` | screens/DIR-PROV.dc.html |
| `/admin` | screens/ADM-01.dc.html |
| `/admin/kpis` | screens/ADM-KPI.dc.html |
| `/admin/reports` | screens/ADM-RPT.dc.html |
| `/admin/officer-id-reset` | screens/ADM-RESET.dc.html (No Override / Zero Trace) |
| `/401 /403 /404 /500 /empty/*` | screens/ERR-*.dc.html |
| Design system reference | screens/NestyStay Design System v2.dc.html |
| Screen hub (browse everything) | screens/INDEX.dc.html |

`PLATFORM_SPEC.md` (included) documents per-screen data sources, API dependencies, permissions, validation and states.

## Design Tokens
All tokens ship as Tailwind v4 `@theme` variables in **`nestystay-theme.css`** (drop-in file). Summary:
- **Brand**: Deep `#062B2B` · Yellow `#FFD21F` (dark grounds ONLY — never on light; hover `#F5C400`) · Deep Hover `#0E4A45` · Emergency `#DC2626` (119 badge only) · Night `#04201F`/`#041F1F` · Ember `#D66E22` (atmosphere only)
- **Light neutrals (warm)**: bg `#FAF3E4` · cards `#FBF7EC` · fills/table headers `#F3EAC8` · borders `#E7DCC2` · input borders `#C7BC9C` · meta `#8A7F5E` · secondary text `#5A6B63` · ink `#12241F`
- **On-dark ramp**: `#FBF7EC` headings · `#E9DFC4` warm · `#DCE6E1` body · `#C9DAD5` nav · `#9FB3AC` muted · `#8FA8A2` faint
- **Semantic** (StatusBadge = tint bg + dark text, uppercase Sora 700 12px pill): green `#E3F2E9`/`#135A38` (approved/captured/verified) · coral `#FBE7E6`/`#9E2B23` (rejected/failed/cancelled) · amber `#FBF1CE`/`#7A5800` (pending/authorized) · blue `#E5EDFB`/`#1D4FA8` (scheduled/assigned) · mint `#DFF6EF`/`#0E6B57` (wellness)
- **Type**: Fraunces (400–500, optical sizing) for ALL display — one italic accent word per title (yellow on dark, `#0E4A45` on light); Sora for UI/body 15px/1.55; eyebrows Sora 600 12px +0.28em with leading 34px dash
- **Radii**: pills 999px (all buttons/badges) · cards 20px · photo cards 26px · inputs 14px · sidebar items 12px
- **Shadows (warm)**: card `0 10px 26px rgba(96,74,20,0.08)` · photo `0 18px 40px rgba(96,74,20,0.16)` · navbar `0 12px 32px rgba(4,31,31,0.35)` · modal `0 40px 90px rgba(4,31,31,0.4)`
- **Touch targets ≥44px**; primary buttons 48–50px.

## Component recipes (map to `components/ui/`)
- **Button.tsx** — pill, min-h 48px. Primary-on-light: Deep bg / cream text, hover `#0E4A45`. Accent-on-dark: Yellow bg / Deep text, hover `#F5C400`, arrow `→` slides +4px on hover. Secondary: 1.5px `#C7BC9C` outline, hover border Deep. Destructive: coral tint bg + coral text. Disabled: `#F3EAC8` bg + `#8A7F5E` text.
- **Badge.tsx** — StatusBadge semantics above; on dark use outlined variant (1px hue border + light hue text). Host tiers: FREE (shell/gray), ✓ VERIFIED (green tint), ★ TRUSTED (Deep bg + Yellow text), ◆ WELLNESS (mint tint).
- **Card.tsx** — Cream bg, 1px `#E7DCC2`, radius 20px, warm card shadow. Photo cards: radius 26px, deep-green gradient scrim `linear-gradient(185deg, rgba(6,43,43,0) 40%, rgba(6,43,43,0.82) 100%)`, badge overlay top-left, 44px heart top-right.
- **Input.tsx** — white bg, 1.5px `#C7BC9C`, radius 14px, min-h 48px; focus: border `#0E4A45` + ring `0 0 0 3px rgba(14,74,69,0.12)`; error: border `#D64F45`, message `#9E2B23` verbatim from backend.
- **Modal.tsx** — Cream, radius 22px, modal shadow, overlay `rgba(6,43,43,0.45)`; Fraunces 500 24px title; 44px round close.
- **PatoisToast.tsx** — Deep bg card, emblem roundel, Fraunces italic Yellow patois line + smaller muted English translation below (MANDATORY pairing); slide-in 200ms ease-out, auto-dismiss 3s; plain English when patois toggle is OFF. Approved lexicon only: Yuh Gud? / Likkle More / Wi Soon Come! / Nuh Fret / Dis page gone a sea / Tek Time / Manage Your Yard / How wi can help yuh?
- **LoadingState.tsx** — structured skeletons (image rect + text lines) with shimmer (`ns-shimmer`), "Tek Time" heading; never a bare spinner; no layout shift.
- **EmptyState/ErrorState.tsx** — line-art icon, Fraunces 500 20px title, body 13px `#5A6B63`, pill CTA; errors show backend message verbatim + "↻ Try again"; 5xx adds WhatsApp link 754-248-2435.
- **PageHeader.tsx** — Fraunces 400 clamp(30px,3.4vw,40px), one italic accent word `#0E4A45`.
- **WorkspaceFrame.tsx** — Deep sidebar 230px (emblem + NESTY STAY wordmark, 44px nav items radius 12px, active = `rgba(255,210,31,0.12)` bg + Yellow text, hover = `rgba(251,247,236,0.06)`), sand canvas, footer `#052A22` "nestystay.net · 754-248-2435" (every page).
- **Public navbar** — floating Deep pill, sticky top 14px, navbar shadow, emblem roundel + wordmark left, links `#C9DAD5` (active Yellow), Yellow "Sign in" pill right; compacts slightly on scroll.

## Interactions & Behavior
- **Reveals**: IntersectionObserver adds a class; translateY(24px)+fade → 0, 700ms ease-out, 100ms stagger on card grids. Gate initial hidden state behind a `js` class on `<html>` (page must render complete without JS). Respect `prefers-reduced-motion`.
- **Loops**: slow 4–8s CSS only (hero card float ±8px, glow pulses). Hover: photos scale 1.03; arrows +4px; nav underline scaleX.
- **Parallax (landing only)**: translateY only, data-depth 0.1–0.92, ≤60px amplitude, single rAF scroll listener, off ≤700px and reduced-motion. framer-motion's `useScroll`/`useTransform` is the natural port.
- **BOOK-07**: 60-min hold countdown from `holdExpiresAt`, always visible; Yellow "Open eKYC transaction ↗" external link.
- Booking stepper: completed steps are links (✓ green outline), current = Deep pill w/ Yellow number disc, future = dashed outline.

## Non-negotiable brand rules (client contract)
1. Yellow `#FFD21F` NEVER on light backgrounds (WCAG AA). On light, patois/accents use Deep italic; "yellow" statuses render Amber.
2. Every patois line paired with its English translation directly below, smaller, gray. Approved lexicon only — never invent patois.
3. "Jamaica Emergency: 119" badge (`#DC2626`, white text, SVG siren icon, no emoji) on every property page: under header, above gallery, above the fold. Never in a footer.
4. Officers = badge ID only (NST-OFC-XXXX): no names, photos, or contact.
5. Verification toggle: "NEVER AUTOMATIC — HOST ENABLES PER PROPERTY"; pricing $0.14 / $1.26 / $2.99 / $29.99 (under client arbitration).
6. Trusted badge copy locked: "$120/year or $12/month".
7. Prices USD only, from spec — never invented. Footer everywhere: nestystay.net · 754-248-2435.
8. Touch targets ≥44px. No dark mode. No rasta/flag/ganja imagery.

## State Management
No new state architecture needed — reuse existing containers (`*StateContainer.tsx`, `hooks/*`, `lib/api.ts`). Each screen's data source, API dependency, permissions and validation are documented per-screen in `PLATFORM_SPEC.md`.

## Assets
- `assets/nestystay-emblem.png` — official logo (white bg → always display inside a cream/sand roundel, `border-radius:50%`).
- Property photos: Unsplash URLs embedded in the screens (placeholders — swap for real property images from the backend/R2 when available).
- Icons: recreate the few inline SVGs (siren, doctor-bird Vs) or use lucide-react equivalents; no icon font.

## Files
- `screens/` — all 54 HTML design references + `INDEX.dc.html` hub + `support.js` runtime (keep next to screens when opening locally)
- `nestystay-theme.css` — Tailwind v4 drop-in tokens
- `PLATFORM_SPEC.md` — per-screen data/API/permissions/states spec
- `assets/nestystay-emblem.png`
