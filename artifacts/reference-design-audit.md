# NestyStay Reference Design Audit

Source inspected: `C:\Users\Administrator\Downloads\NestyStay-Complete-97-Pages\NestyStay-Complete-97-Pages`

## Scope Read

- 197 HTML files across root, `pages/`, and `numbered-pages/`.
- 98 CSS files, including the shared root `styles.css` and 97 standalone numbered page styles.
- 3 JavaScript/config helper files: `app.js`, `generate-pages.js`, `generate-numbered-pages.js`.
- 5 visual assets: `nestystay-logo.png`, `landing-hero.jpg`, `property-1.jpg`, `property-2.jpg`, `property-3.jpg`.
- 97 reference frames grouped as ORIGINAL, PUB, AUTH, BOOK, TRAV, HOST, MSG, PM, OFC, ADM, DIR, and ERR.

## Core Design Tokens

- Colors: deep green `#062b2b`, black-green `#021b1b`, palm `#2f7d3a`, yellow `#ffd21f`, cream `#fff8e7`, soft green `#eaf5ea`, muted text `#637272`, line `#dfe8e2`, danger `#b93b42`, info `#1d64a7`.
- Public page extras: landing cream `#f8f5ee`, warm sand `#f3ece4`, text gray `#41484d`, gold accent `#c99800`.
- Typography: `Inter` for UI/body; `DM Serif Display` for brand, app headers, articles, detail headings, and status headings.
- Radius scale: 8px, 9px, 10px, 12px, 14px, 15px, 16px, 18px, 24px, 999px pills; expressive media radius `120px 24px 120px 24px`.
- Shadows: soft large elevation `0 18px 55px rgba(6,43,43,.13)`, card elevation `0 18px 36px -14px rgba(0,0,0,.1)`, nav shadow `0 10px 24px rgba(0,0,0,.08)`, icon shadow `0 8px 9px rgba(0,0,0,.08)`.
- Spacing: root page frames at 1280px wide; nav 76-80px high; page sections use 64px horizontal and 70-96px vertical padding; app content uses 36px/44px padding; cards use 20-32px.

## Reusable Components

- Public nav: white translucent 80px bar, logo image, two main links, Login/Register buttons, active underline.
- Buttons: pill or softly rounded, primary yellow, dark green, white outline, transparent/secondary.
- Cards and panels: white background, thin green-gray border, 15-18px radius, light shadow; property cards use 16px radius and 256px media.
- Badges: small uppercase pills, soft green default, yellow warning, red danger.
- Forms: stacked labels, 10px radius inputs/selects/textareas, 14px padding, clear focus border, segmented controls for mode switching.
- Dashboards: dark green sidebar, app top header, circular yellow avatar, 4-column stats, two-column content grid, compact activity rows.
- Status pages: centered card, large circular status icon, serif headline, paired CTA buttons.
- Detail pages: public nav, large media grid, copy column, sticky booking box with line items.
- Invoice/article pages: centered white document or narrow article layout, serif title, muted body text, strong dividers.

## Page Patterns

- Landing: nav, image hero with dark overlay, pill search bar, trust grid, featured stays, dark CTA block, cream footer.
- Public/search/detail: cream or white surfaces, property cards, detail media grid, booking box.
- Auth: split screen with dark-green art panel and centered form card.
- App/dashboard/listing/admin: persistent left sidebar, light app background, metric cards, panels, row lists, filters.
- Booking/status/error: app layout for workflow lists; centered status cards for success/failure/pending/empty/error.
- Mobile: reference collapses large grids to one column under 900px; sidebar narrows; public cards stack; nav/library panels become compact.

## Migration Strategy

- Introduce reference-aligned CSS tokens and reusable component styles in the current React app.
- Keep API hooks, routes, data types, and backend calls unchanged.
- Replace landing composition with React components modeled after the reference landing structure.
- Add a reusable workspace sidebar wrapper for dashboard/admin/operations routes.
- Restyle existing cards, buttons, badges, forms, modals, panels, metrics, rows, and page headers rather than pasting standalone HTML.
- Reuse reference image assets by copying them into the frontend public assets folder.
