# NestyStay performance baseline

This is the pre-optimization baseline for the current frontend production build. It must not be overwritten by the optimization report.

## Method

- Lighthouse 12.8.2
- Local Vite production preview (`npm run build` followed by `npm run preview`)
- Simulated mobile throttling (`formFactor: mobile`, `throttlingMethod: simulate`)
- Audited routes: `/`, `/explore`, `/experiences`
- Measurements are local baselines, not live-production field data.

## Lighthouse results

| Route | Performance | Accessibility | SEO | FCP | LCP | TBT | CLS | Speed Index |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| `/` | 68 | 99 | 100 | 2.6s | 6.1s | 240ms | 0.076 | 2.6s |
| `/explore` | 65 | 99 | 100 | 3.0s | 5.4s | 80ms | 0.198 | 3.0s |
| `/experiences` | 64 | 98 | 100 | 4.0s | 10.7s | 60ms | 0.087 | 4.0s |

Average: Performance 66, Accessibility 99, SEO 100.

## Initial transfer summary

Homepage Lighthouse resource summary:

- Total transfer: 1,332 KiB
- JavaScript transfer: 492,784 bytes (481.2 KiB)
- Image transfer: 731,815 bytes (714.7 KiB)
- Requests: 23 total, including 7 scripts and 5 images

## Largest generated assets before optimization

Measured from `frontend/dist/assets` after the baseline build:

| Asset | Bytes |
| --- | ---: |
| `motion-DXB1IJ-B.js` | 248,941 |
| `vendor-Dcc77I9S.js` | 234,521 |
| `index-BfuogMTF.js` | 217,282 |
| `CompletionPages-fbt8go4q.js` | 188,691 |
| `PublicStateContainer-C4rOO572.js` | 159,677 |
| `index-BqIyaJBx.css` | 152,679 |
| `ProductPages-DHLDiMe6.js` | 116,459 |
| `PropertyManagerProfessionalPage-DsGCjyRQ.js` | 59,447 |
| `PropertyManagerPages-DYy5liUG.js` | 55,426 |
| `PropertyManagerP0Page-AmbdFL3U.js` | 39,394 |

## Baseline findings

- The homepage and Explore route load Framer Motion through the application shell.
- GSAP is imported by the landing scroll story and is grouped into the motion chunk.
- QR code generation is statically imported by completion/property-manager modules.
- The emblem is a 360,660-byte PNG displayed at approximately 38px.
- Experiences has the worst LCP at 10.7s and needs a route-specific image/render investigation.
- Explore CLS is 0.198 and needs stable loading geometry.
- Lighthouse reports two accessibility issues: cookie consent dialog semantics and hero search accessible-name mismatches.
- The unauthenticated profile request returns expected 401 responses that Lighthouse records as console errors.
