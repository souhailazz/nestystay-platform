# M1/M2 Visual Evidence

Audit date: 2026-07-24

Branch: `audit/m1-m2-remediation`

Baseline commit audited: `5465ba77308c17245cfa06b1a29f231104e037b9`

Current evidence scope: representative Playwright smoke screenshots for selected M1/M2 routes. This is not the full DOCX visual acceptance set.

Verdict: PARTIAL / NOT COMPLETE.

## New Evidence Set

| Artifact path | Count | Notes |
| --- | ---: | --- |
| `artifacts/m1-m2-visual/ADM` | 3 | Admin dispute route after admin token entry. |
| `artifacts/m1-m2-visual/AUTH` | 6 | Login and register representative routes. |
| `artifacts/m1-m2-visual/DIR` | 3 | Provider directory/dashboard route. |
| `artifacts/m1-m2-visual/ERR` | 3 | 404 route. |
| `artifacts/m1-m2-visual/HOST` | 9 | Host dashboard, properties, and pricing routes. |
| `artifacts/m1-m2-visual/HPRO` | 3 | Host profile edit route. |
| `artifacts/m1-m2-visual/MSG` | 3 | Messaging inbox route. |
| `artifacts/m1-m2-visual/PUB` | 12 | Home, explore, map, and experiences routes. |
| `artifacts/m1-m2-visual/TRAV` | 15 | Traveler dashboard, payments, invoices, identity, and profile-photo upload state. |

Total screenshots: 57 PNG files, captured by `npm run test:e2e` across desktop Chromium, tablet Chromium, and mobile Chromium.

## Existing Broad Artifacts

| Path | Notes |
| --- | --- |
| `frontend/qa-current-desktop-home.png` | Homepage snapshot only. Not enough for PUB-01 acceptance. |
| `frontend/qa-current-mobile-home.png` | Mobile homepage snapshot only. |
| `frontend/qa-current-desktop-explore.png` | Explore snapshot only. |
| `frontend/qa-current-mobile-login.png` | Login snapshot only. |
| `frontend/qa-current-desktop-explore-nav.png` | Navigation/explore snapshot only. |
| `artifacts/design-migration-screenshots/*` | Broad migration screenshots, not per DOCX screen/state. |

## Missing Evidence

The complete visual matrix is still missing. The repository does not yet contain desktop, tablet, and mobile screenshots for every DOCX M1/M2 screen, nor the required interaction-state screenshots for dialogs, loading states, empty states, error states, booking checkout/payment states, upload progress/retry states, admin moderation actions, protected 401/403 routes, and keyboard/focus states.

The new Playwright smoke suite covers representative routes only. It does not convert any screen family to PASS by itself.

## Visual Verdict

Do not mark M1/M2 visual acceptance PASS until every DOCX screen has matching desktop/tablet/mobile screenshots and the interaction states are reviewed against the specification.
