# NestyStay role navigation matrix

Navigation is defined once in `ROLE_NAVIGATION` in [`frontend/src/app/routeManifest.ts`](../../frontend/src/app/routeManifest.ts). `WorkspaceFrame` consumes it for desktop and mobile. Mobile always has four priority tabs plus an explicit **More** action; it never derives tabs with `.slice(0, 5)`.

| Role | Desktop destinations | Mobile priority tabs | More |
|---|---|---|---|
| Guest | Trips, Suggestions, Saved, Invoices, Messages, Alerts, Settings, Directories | Explore, Trips, Saved, Messages, Profile | — |
| Host | Home, Listings, Reservations, Calendar, Wellness, Messages, Alerts, Directories, Settings | Home, Calendar, Listings, Inbox | Host tools |
| PropertyManager | Home, Calendar, Work, Invoices, Payments, Gates, Reports, Alerts, Settings | Home, Calendar, Work, Alerts | Reports and operations |
| Owner | Home, Statements, Messages, Alerts, Settings | Home, Statements, Reviews, Alerts | Profile and settings |
| ServiceProvider | Provider home, Directories, Messages, Alerts, Settings | Home, Directory, Messages, Alerts | Profile and settings |
| LocalBusiness | Business home, Directory, Messages, Alerts, Settings | Home, Directory, Messages, Alerts | Profile and settings |
| Officer | Assignments, Directory, Messages, Alerts, Settings | Home, Assignments, Messages, Alerts | Profile and settings |
| Admin | Home, Queues, Audit, Configuration, Alerts, Settings | Home, Queues, Audit, Alerts | Configuration and settings |

The shell uses three intentional responsive modes: phone (<768px) sticky top rail plus bottom tabs, tablet (768–1023px) compact vertical rail, and desktop (≥1024px) full sidebar. Modal/sheet behavior includes scroll locking, focus trapping/return, Escape handling, safe-area padding, and an opt-in overlay close.
