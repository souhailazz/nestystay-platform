# M5 property-manager acceptance

| Area | Acceptance evidence | Status |
|---|---|---|
| Manager dashboard and subscription | Real `GET /api/property-manager/dashboard`, renew action and responsive UI | PASS |
| Owners and units | Registered-owner invite, verification action, manager-scoped property assignment | PASS |
| Invoices, statements and payments | Server-calculated totals, utility-linked invoice, local capture, idempotency and owner statement | PASS |
| Maintenance and vendors | Owner request, manager queue, start/complete controls and vendor register | PASS |
| Notices and gate communications | Persisted community notice and gate message endpoints/UI | PASS |
| Governance | Anonymous ballot, eligibility, quorum input, duplicate-vote rejection and proxy grant API/UI | PASS |
| Documents | PDF/JPEG/PNG file picker, base64 upload, 25 MB/type/name checks and scoped listing | PASS |
| QR | Secure random token, hashed persistence, public validation, wrong-property/expiry/revoke states and scan log | PASS |
| Isolation | Cross-owner invoice/portal leakage test and manager portfolio scope checks | PASS |
| Real browser | Desktop, tablet and mobile PM/owner/gate tests | PASS (6/6) |

The signed agreement does not state a fixed Property Manager price. The local profile uses a configurable Portfolio tier with amount `0` until commercial configuration is supplied.
