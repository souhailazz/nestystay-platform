# NestyStay final screen certification

Date: 2026-09-16

This is the final local certification matrix for the critical canonical product surfaces. Alias routes that intentionally render the same canonical screen are represented by their canonical route; the complete alias inventory remains in `frontend/src/app/routeManifest.ts`. Every row below has desktop, mobile and tablet evidence in `testing-evidence/final-ui/`.

`PASS` means the real route rendered in Chromium with the listed role and its API-backed loaded/empty state was captured without page errors. `N/A` means persistence is not a mutation concern for that read-only surface. External provider certification and human accessibility certification are documented separately rather than represented as local proof.

| Route | Milestone | Role | Desktop screenshot | Mobile screenshot | Functional | API-backed | Persistence | Final |
|---|---|---|---|---|---|---|---|---|
| `/explore` | M1 | Public | [desktop](../../testing-evidence/final-ui/M1/desktop/01-explore.png) | [mobile](../../testing-evidence/final-ui/M1/mobile/01-explore.png) | PASS | PASS | N/A | PASS |
| `/properties/:propertyId` | M1 | Public | [desktop](../../testing-evidence/final-ui/M1/desktop/02-property-detail.png) | [mobile](../../testing-evidence/final-ui/M1/mobile/02-property-detail.png) | PASS | PASS | N/A | PASS |
| `/register` | M1 | Public | [desktop](../../testing-evidence/final-ui/M1/desktop/03-registration.png) | [mobile](../../testing-evidence/final-ui/M1/mobile/03-registration.png) | PASS | PASS | PASS | PASS |
| `/guest-dashboard` | M1 | Guest | [desktop](../../testing-evidence/final-ui/M1/desktop/04-guest-dashboard.png) | [mobile](../../testing-evidence/final-ui/M1/mobile/04-guest-dashboard.png) | PASS | PASS | PASS | PASS |
| `/host/properties` | M1 | Host | [desktop](../../testing-evidence/final-ui/M1/desktop/05-host-properties.png) | [mobile](../../testing-evidence/final-ui/M1/mobile/05-host-properties.png) | PASS | PASS | PASS | PASS |
| `/calendar` | M1 | Host | [desktop](../../testing-evidence/final-ui/M1/desktop/06-host-calendar.png) | [mobile](../../testing-evidence/final-ui/M1/mobile/06-host-calendar.png) | PASS | PASS | PASS | PASS |
| `/booking/:bookingId/pending` | M1 | Guest | [desktop](../../testing-evidence/final-ui/M1/desktop/07-booking-pending.png) | [mobile](../../testing-evidence/final-ui/M1/mobile/07-booking-pending.png) | PASS | PASS | PASS | PASS |
| `/host/badges` | M2 | Host | [desktop](../../testing-evidence/final-ui/M2/desktop/01-badge-levels.png) | [mobile](../../testing-evidence/final-ui/M2/mobile/01-badge-levels.png) | PASS | PASS | PASS | PASS |
| `/admin/ops/pricebook` | M2 | Admin | [desktop](../../testing-evidence/final-ui/M2/desktop/02-badge-admin.png) | [mobile](../../testing-evidence/final-ui/M2/mobile/02-badge-admin.png) | PASS | PASS | PASS | PASS |
| `/host/wellness` | M3 | Host | [desktop](../../testing-evidence/final-ui/M3/desktop/01-wellness-host.png) | [mobile](../../testing-evidence/final-ui/M3/mobile/01-wellness-host.png) | PASS | PASS | PASS | PASS |
| `/officer/wellness` | M3 | Officer | [desktop](../../testing-evidence/final-ui/M3/desktop/02-wellness-officer.png) | [mobile](../../testing-evidence/final-ui/M3/mobile/02-wellness-officer.png) | PASS | PASS | PASS | PASS |
| `/admin/ops/wellness` | M3 | Admin | [desktop](../../testing-evidence/final-ui/M3/desktop/03-wellness-admin.png) | [mobile](../../testing-evidence/final-ui/M3/mobile/03-wellness-admin.png) | PASS | PASS | PASS | PASS |
| `/directory/trades` | M4 | Public | [desktop](../../testing-evidence/final-ui/M4/desktop/01-trades-directory.png) | [mobile](../../testing-evidence/final-ui/M4/mobile/01-trades-directory.png) | PASS | PASS | N/A | PASS |
| `/directory/provider` | M4 | Service Provider | [desktop](../../testing-evidence/final-ui/M4/desktop/02-provider-workspace.png) | [mobile](../../testing-evidence/final-ui/M4/mobile/02-provider-workspace.png) | PASS | PASS | PASS | PASS |
| `/admin/ops/directories` | M4 | Admin | [desktop](../../testing-evidence/final-ui/M4/desktop/03-directory-admin.png) | [mobile](../../testing-evidence/final-ui/M4/mobile/03-directory-admin.png) | PASS | PASS | PASS | PASS |
| `/gate/qr` | M4 | Public | [desktop](../../testing-evidence/final-ui/M4/desktop/04-gate-qr.png) | [mobile](../../testing-evidence/final-ui/M4/mobile/04-gate-qr.png) | PASS | PASS | PASS | PASS |
| `/pm/dashboard` | M5 | Property Manager | [desktop](../../testing-evidence/final-ui/M5/desktop/01-manager-dashboard.png) | [mobile](../../testing-evidence/final-ui/M5/mobile/01-manager-dashboard.png) | PASS | PASS | PASS | PASS |
| `/owner/dashboard` | M5 | Owner | [desktop](../../testing-evidence/final-ui/M5/desktop/02-owner-portal.png) | [mobile](../../testing-evidence/final-ui/M5/mobile/02-owner-portal.png) | PASS | PASS | PASS | PASS |
| `/pm/invoices` | M5 | Property Manager | [desktop](../../testing-evidence/final-ui/M5/desktop/03-invoices.png) | [mobile](../../testing-evidence/final-ui/M5/mobile/03-invoices.png) | PASS | PASS | PASS | PASS |
| `/pm/utilities` | M5 | Property Manager | [desktop](../../testing-evidence/final-ui/M5/desktop/04-utilities.png) | [mobile](../../testing-evidence/final-ui/M5/mobile/04-utilities.png) | PASS | PASS | PASS | PASS |
| `/pm/maintenance` | M5 | Property Manager | [desktop](../../testing-evidence/final-ui/M5/desktop/05-maintenance.png) | [mobile](../../testing-evidence/final-ui/M5/mobile/05-maintenance.png) | PASS | PASS | PASS | PASS |
| `/pm/gates` | M5 | Property Manager | [desktop](../../testing-evidence/final-ui/M5/desktop/08-gate-messages.png) | [mobile](../../testing-evidence/final-ui/M5/mobile/08-gate-messages.png) | PASS | PASS | PASS | PASS |
| `/pm/governance` | M5 | Property Manager | [desktop](../../testing-evidence/final-ui/M5/desktop/09-governance.png) | [mobile](../../testing-evidence/final-ui/M5/mobile/09-governance.png) | PASS | PASS | PASS | PASS |
| `/pm/documents` | M5 | Property Manager | [desktop](../../testing-evidence/final-ui/M5/desktop/10-documents.png) | [mobile](../../testing-evidence/final-ui/M5/mobile/10-documents.png) | PASS | PASS | PASS | PASS |
| `/pm/calendar` | M5 | Property Manager | [desktop](../../testing-evidence/final-ui/M5/desktop/11-calendar.png) | [mobile](../../testing-evidence/final-ui/M5/mobile/11-calendar.png) | PASS | PASS | PASS | PASS |

## Acceptance summary

| Milestone | Visual desktop | Visual mobile | UX | Frontend | Backend | Database | Authorization | Responsive | Accessibility automated | Browser E2E | Final |
|---|---|---|---|---|---|---|---|---|---|---|---|
| M1 Core Booking | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| M2 Badges | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| M3 Wellness | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| M4 Directories / Trust / QR | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| M5 Property Manager | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |

These are local acceptance results. They do not claim that external Stripe Identity, production Stripe webhooks, Brevo, Zoho, external object storage, production backups/monitoring, TLS/domain routing, or human screen-reader certification have been independently verified from this workstation.

## Deliverable verdict

- M1 DELIVERABLE READY: YES for local delivery; staging provider smoke test remains external.
- M2 DELIVERABLE READY: YES for local delivery; live Stripe billing/Identity remains external.
- M3 DELIVERABLE READY: YES for local delivery; external notification/storage/provider checks remain external.
- M4 DELIVERABLE READY: YES for local delivery; deployment/provider checks remain external.
- M5 DELIVERABLE READY: YES for local delivery; staging calendar/storage/backup/monitoring checks remain external.
- NESTYSTAY FULL LOCAL DELIVERABLE READY: YES.

Production/staging release is a separate gate: the three repository PRs still require the client's protected-branch review/merge, followed by the client's staging deployment and external-provider smoke tests.
