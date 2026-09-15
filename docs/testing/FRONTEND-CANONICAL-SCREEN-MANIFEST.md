# NestyStay canonical frontend screen manifest

Phase 1 frontend authority lives in [`frontend/src/app/routeManifest.ts`](../../frontend/src/app/routeManifest.ts). `App.tsx`, `WorkspaceFrame`, route guards, the internal screen index, tests, and the Figma projection consume this module. No second route parser or role-navigation list is permitted.

## Inventory

| Field | Count / policy |
|---|---:|
| Screen IDs | 95 |
| Declared path patterns | 146 |
| Secondary path patterns / aliases | 51 |
| Production screen definitions | 80 |
| Preview / migration definitions | 3 |
| Error-state definitions | 6 |
| Internal-only definitions | 6 |
| Supported roles | 8 |
| Canonical path authority | `canonicalPath` on each manifest entry |

Every entry has an ID, canonical path template, one or more parser patterns, title, product area, component key, shell mode, auth mode, role access, screen type, and route factory. Dynamic parameters are decoded by the shared matcher; there are no ad-hoc `if (pathname === ...)` branches in the app shell.

## Canonical workflow owners

| Workflow | Canonical owner | Notes |
|---|---|---|
| Public discovery | `/explore` → `PUB-02` | Search/detail stay API-backed. |
| Booking | `/booking/:bookingId/:state` → `BOOK-FLOW` | Outcome paths are explicit aliases under `BOOK-CONF`. |
| Traveler reservations | `/traveler/reservations` → `TRAV-RES` | Past, cancelled, upcoming, and detail are views of one workspace. |
| Traveler saved stays | `/traveler/favorites` → `TRAV-COL` | `/wishlist` is an alias; both use the API traveler workspace. |
| Traveler reviews | `/traveler/reviews/pending` → `TRAV-REV` | `/traveler/reviews` and `/given` resolve to the same API workflow. |
| Messaging | `/messages` → `MSG-01` | Thread detail and `/messages/document` use the API-backed `MessagesWorkspace`, including secure attachment actions. |
| Host profile | `/hosts` → `HOST-PROFILE` | Directory, detail, edit, and preview use the API host-profile contract. |
| Host operations | `/host/analytics` → `HOST-OPS` | Pricing, promotions, reviews, exports, badges, and archive are views of one API owner. |
| Property manager | `/pm/dashboard` and `/pm/<module>` | The page owns data loading; dedicated module components own module UI. |
| Admin operations | `/admin/ops/:view` → `ADM-01` | Fixed directory/wellness/badges IDs close the previously orphaned mappings. |

## Guard contract

`getRouteAccess()` runs before the workspace shell is selected:

1. Public definitions render normally.
2. Authenticated definitions render `SignInRequiredPage` and preserve the complete intended destination in `returnTo`.
3. Role definitions render `AccessRestrictedPage` for an authenticated user with the wrong role.
4. Only an allowed route can render `WorkspaceFrame`.

The backend remains the authorization authority; frontend role metadata controls the shell and navigation experience only.

## Validation

`frontend/src/app/routeManifest.test.ts` checks unique IDs and canonical paths, path resolution, alias shape, navigation references, and public/signed-out/wrong-role/correct-role access states. This is the source inventory for future browser route coverage; `/design-system`, `/screens`, and `/screens/:screenId` remain explicitly internal.
