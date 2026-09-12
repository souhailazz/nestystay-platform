# Frontend route migration

The route migration consolidates legacy screen branches into `frontend/src/app/routeManifest.ts`. The manifest is now the only authority for parsing, screen IDs, titles, access metadata, shell mode, aliases, and navigation references.

## Consolidated paths

| Legacy / duplicate path | Canonical owner | Migration result |
|---|---|---|
| `/traveler/favorites`, `/wishlist` | `TRAV-COL` → traveler workspace `wishlist` | Removed static collections production owner. |
| `/traveler/reviews`, `/traveler/reviews/pending`, `/traveler/reviews/given` | `TRAV-REV` → traveler workspace reviews | Removed static pending-review production owner. |
| `/directory/provider` | `DIR-PROV` → provider dashboard | Removed collision with generic directory parser. |
| `/directory/businesses` | `DIR-BIZ` → business directory | Removed collision with `DIR-02`. |
| `/directory/providers/:slug` | `DIR-PROFILE` → provider detail | Parameterized provider profile owner. |
| `/profile` | `PROFILE-01` → profile settings | Traveler preferences are now `/traveler/preferences` (`TRAV-12`). |
| `/host/profile/edit` | `HOST-PROFILE-EDIT` | API-backed editor. |
| `/host/profile/preview` | `HOST-PROFILE-EDIT` | API-backed public preview view. |
| `/hosts`, `/hosts/:slug` | `HOST-PROFILE` | API-backed directory/detail owner. |
| `/messages/document` | `MSG-DOC` → `MessagesWorkspace` | Secure document messages use the canonical API-backed messaging workspace. |
| `/pm/insurance` | `PM-INSURANCE` → Property Manager coverage module | Replaced static priced concept cards with an API-backed readiness/empty-state surface; no fabricated policy plans or limits. |
| Booking modal implementations | `components/booking/BookingModal.tsx` | Feature duplicate removed; public discovery uses one modal. |

## Orphan repairs

`DIR-ADM`, `ADM-WELLNESS`, and `ADM-BADGES` now have explicit canonical routes and component mappings. `INDEX` remains the intentional internal index exception. No migration alias points at a removed production owner.

## Compatibility rules

- Existing business API contracts and backend domain behavior remain unchanged.
- Aliases continue to parse during migration; new links use canonical paths.
- Internal screen previews can still be opened with `/screens/:screenId`.
- Feature containers retained for useful local specifications are not imported by canonical production routes unless they are the chosen implementation owner.
