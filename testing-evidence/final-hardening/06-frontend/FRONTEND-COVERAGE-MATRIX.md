# Frontend coverage matrix

This matrix separates source/component presence from exercised, API-backed UI journeys. “Browser evidence” refers to the Playwright suites named in the evidence index; route inventory covered 127 defined route cases with no unexpected 5xx responses.

| Capability | UI route/surface | Backend/API exercised | Unit/component | Browser / responsive / a11y evidence | Status |
|---|---|---:|---:|---|---|
| Registration, login, 2FA, recovery | `/register`, `/login`, `/auth/*` | Yes | Partial | Contract workflows, route inventory, keyboard, axe | Pass with live-provider follow-up |
| Property create/edit/list/archive | `/host/properties`, `/host/properties/edit` | Yes | Partial | Contract validation and usability suites; edit race regression fixed | Pass |
| Booking quote and status journey | `/explore`, `/booking/*`, `/traveler/reservations` | Yes | Partial | M1/M2 real workflows, contract validation, route inventory | Pass locally |
| Stripe checkout/test-mode adapter | `/booking/*/checkout`, payment surfaces | Yes | Partial | Stripe test-mode browser test | Application integration pass; live provider not validated |
| eKYC pending/approved/rejected | `/booking/*/identity`, traveler identity | Yes | Partial | Contract eKYC workflow and M1/M2 real workflow | Pass locally; Alibaba external validation pending |
| Guest dashboard/persisted data | `/guest-dashboard`, traveler routes | Yes | Partial | Authenticated acceptance and route inventory | Pass |
| Host dashboard/persisted data | `/host/*` | Yes | Partial | Host real workflow and route inventory | Pass |
| Badge dashboard, pricing, renewal, restrictions | `/host/badges`, `/admin/badges` | Yes | Partial | M2 management and real workflow suites | Pass |
| Officer onboarding/approval | `/officer/*`, `/admin/ops/wellness` | Yes | Partial | M3 enhancement, lifecycle, route inventory | Pass locally |
| Wellness booking, assignment, report, payout | `/wellness/*`, host/officer/admin workspaces | Yes | Partial | M3 lifecycle and enhancement suites | Pass locally |
| Custodian/trades/business/police directories | `/directory/*` | Yes | Partial | M4 directory suite, route inventory, 12-page axe | Pass |
| Provider onboarding/moderation/dashboard | `/directory/provider/*`, admin providers | Yes | Partial | M4/M5 enhancement and moderation suites | Pass |
| QR issue/validate/revoke/expiry | `/gate`, `/gate/qr`, PM gate surface | Yes | Partial | M4 QR gate suite, cross-browser smoke, concurrency | Pass locally |
| Manager/owner portal, invoices/statements/payments | `/pm/*`, `/owner/dashboard` | Yes | Partial | M5 manager and route inventory suites | Pass locally |
| Maintenance, governance, anonymous/proxy voting | `/pm/maintenance`, `/pm/governance` | Yes | Partial | M5 manager suite and authorization matrix | Pass locally |
| Documents, notices, messaging, notifications | `/pm/documents`, `/messages`, `/notifications` | Yes | Partial | Acceptance/usability suites and route inventory | Pass locally |
| Responsive interaction | all major public and workspace routes | N/A | N/A | 5 Chromium viewports; overflow/target assertions | Pass |
| Keyboard and accessible dialogs | auth/modal/workspace controls | N/A | Partial | Tab, Shift+Tab, Enter, focus, Escape harness; axe critical/serious = 0 | Pass for exercised surfaces |
| Visual regression | six stable representative screens | N/A | N/A | laptop Chromium baselines, maxDiffPixelRatio 0.01 | Pass |

## Known test-scope limits

- Vitest is not a proxy for browser coverage: the historical full-source run had 26 tests with statements 4.97%, branches 4.36%, functions 3.81%, lines 5.66%; the resumed included-source run has 30 tests with statements 44.73%, branches 44.29%, functions 29.61%, lines 44.22%. Both scopes are retained explicitly.
- Real external Stripe/Connect, Alibaba Cloud eKYC, email/SMS/push, and payout-provider calls require production credentials and were not claimed as locally verified.
- Browser route inventory treats expected API 404/403 responses for placeholder IDs as handled application states; it fails on unexpected 5xx and uncaught application crashes.
- M5 Property Manager evidence is a dedicated persisted end-to-end journey: 7/7 configured browser/viewport projects passed with 0 console errors, 0 unexpected 5xx responses, and 0 axe violations.
