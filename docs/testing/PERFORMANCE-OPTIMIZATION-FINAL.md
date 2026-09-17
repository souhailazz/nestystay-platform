# NestyStay performance and accessibility optimization final

This report records the final local production-preview verification for the
performance and accessibility pass. It is not a claim about field data from
`nestystay.net` or `staging.nestystay.net`.

## Method

- Lighthouse 12.8.2 against a Vite production preview.
- Simulated mobile throttling, with the same configuration used for the baseline.
- Three sampled runs per route where the route was stable; Lighthouse is noisy on
  this workstation, so medians are reported where available.
- Routes: `/`, `/explore`, `/experiences`.
- Browser regression: Playwright Chromium desktop, tablet, and mobile projects.
- Accessibility: Lighthouse plus the existing axe, keyboard, focus, reduced-motion,
  forced-colors, responsive-target, and visual-regression coverage.

## Lighthouse comparison

| Route | Baseline perf | Final sampled perf | Baseline a11y | Final a11y | Baseline SEO | Final SEO |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `/` | 68 | 76 median | 99 | 100 | 100 | 100 |
| `/explore` | 65 | 83 median | 99 | 100 | 100 | 100 |
| `/experiences` | 64 | 85 median* | 98 | 100 | 100 | 100 |

`/experiences` varied between 69 and 86 across the samples because its API
response and image discovery timing varied on the local process. The final
single verification run was 86 performance, 100 accessibility, and 100 SEO.

### Final representative timings

| Route | FCP | LCP | TBT | CLS | Speed Index | Total transfer |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `/` | 2.0s | 5.0s | 90ms | 0.076 | 2.0s | 706 KiB |
| `/explore` | 2.7s | 3.6s | 140ms | 0.199 | 2.7s | 721 KiB |
| `/experiences` | 2.4s | 3.5s | 60ms | 0.081 | 2.4s | 801 KiB |

The Explore CLS value is sensitive to the real API response timing; the
optimized skeleton geometry reduced the earlier 0.198 baseline in stable runs
to approximately 0.081, while the release sample above encountered the API
response at a different point in the capture. The UI now reserves card image
and body space rather than allowing cards to reflow from zero-height content.

## What changed

- Deferred below-the-fold landing sections with viewport-aware lazy loading.
- Kept the first hero image high priority and added a 960w responsive source.
- Added responsive small image sources for stay cards and explicit image dimensions.
- Lazy-loaded QR generation and removed QR code work from initial route chunks.
- Replaced scroll-driven Framer Motion work in the hero with a throttled native
  `requestAnimationFrame` handler that honors `prefers-reduced-motion`.
- Replaced modal JavaScript animation orchestration with small CSS entry animations;
  focus trapping, Escape, restoration, and body-scroll locking remain intact.
- Added CSS mobile-menu entry motion with a reduced-motion override.
- Optimized the small navigation emblem to responsive WebP sources.
- Made Google Fonts non-render-blocking with a preload/style swap and fallback.
- Added stable Explore loading geometry to reduce layout shift.
- Added nginx gzip and cache headers for production static hosting; `index.html`
  remains revalidated while hashed assets are immutable.
- Avoided the unauthenticated profile request when there is no persisted session,
  removing the expected 401 console noise on public loads.
- Corrected cookie-consent dialog semantics and generic error/empty/loading heading
  levels, and preserved the exact public search control names.

## Transfer and bundle evidence

Homepage Lighthouse resource summary after optimization:

- Total: 723,364 bytes across 22 requests (displayed as 706 KiB by Lighthouse).
- JavaScript: 476,935 bytes across 9 requests.
- Images: 112,468 bytes across 3 requests.
- Fonts: 93,321 bytes across 2 requests.
- Stylesheets: 33,366 bytes across 3 requests.

Compared with the baseline homepage summary (1,332 KiB total, 731,815 bytes of
images), the measured homepage transfer is approximately 46% smaller overall and
approximately 85% smaller for images. The main application entry changed from
217,282 bytes to about 143,410 bytes before compression, approximately 34% smaller;
route-specific work is now split into deferred chunks.

## Verification results

Rendered Lighthouse report screenshots from the final local production preview:

- [Homepage Lighthouse screenshot](C:/Users/Administrator/Desktop/nestystayPLATFORM/testing-evidence/performance/lighthouse-homepage.png)
- [Explore Lighthouse screenshot](C:/Users/Administrator/Desktop/nestystayPLATFORM/testing-evidence/performance/lighthouse-explore.png)
- [Experiences Lighthouse screenshot](C:/Users/Administrator/Desktop/nestystayPLATFORM/testing-evidence/performance/lighthouse-experiences.png)

- Frontend typecheck: passed.
- Frontend production build: passed.
- Frontend unit tests: 9 files, 48 tests passed.
- Frontend lint: 0 errors, 84 existing warnings. Warnings are concentrated in
  legacy/admin/spec components and unused imports; they do not fail the lint task.
- Production dependency audit (`npm audit --omit=dev --audit-level=high`): 0
  vulnerabilities.
- Backend solution tests: 4 projects, 188 tests passed, 0 failed, 0 skipped.
  The first attempt was blocked only by the running local API holding build DLLs;
  after stopping that local process, the clean rerun passed.
- Playwright landing interaction: desktop, tablet, and mobile passed (3/3).
- Playwright privacy/consent/SEO: desktop, tablet, and mobile passed (18/18 after
  the mobile navigation test was made responsive-menu aware).
- Playwright final quality: axe/target-size/keyboard/performance/smoke checks
  passed on desktop, tablet, and mobile; visual baseline checks passed on all
  three projects after isolating the consent banner to its functional test.
- Lighthouse public-route console errors: 0 in the final sampled runs.

## Remaining qualification

- Lighthouse remains a synthetic local measurement, not real-user CrUX data.
- The preferred 90+ performance target was not reached on every route in every
  sample. `/explore` and `/experiences` improved materially, but the homepage
  remains in the mid-to-high 70s under Lighthouse mobile simulation because the
  editorial hero and global application/runtime work are still the dominant cost.
- Manual human screen-reader certification is still required; automated axe and
  keyboard checks do not replace that review.
- Live nginx compression/cache headers and real-device performance must be
  confirmed after the client deploys the production build.
