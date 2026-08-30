# M1–M5 traceability

Primary contractual source: `docs/contracts/NestyStay-Signed-Agreement-April-2026.pdf` (11 pages, SHA-256 `0C4AAD0B1A2D015433C0875107DD171C93C4191281D7CCCBBE748B37DB4FD28D`). M5 includes the signed Phase 5 property-manager suite plus the client-added governance, anonymous voting and proxy controls.

| Milestone | Contractual outcome | Backend/API | Frontend | Browser/API evidence | Decision |
|---|---|---|---|---|---|
| M1 Core | Auth/2FA, property, booking, payment, eKYC, persisted dashboards | Implemented and regression-tested | Connected routes and workflows | Existing M1–M4 suites plus 19-route inventory | PASS locally |
| M2 Badges | Four badge levels, pricing/eligibility/renewal and restrictions | Implemented and regression-tested | Connected badge/dashboard surfaces | Existing M1–M4 suites | PASS locally |
| M3 Wellness | Officer lifecycle, assignment/report, subscription/commission state | Implemented and regression-tested | Connected host/officer/admin routes | Existing wellness lifecycle suite | PASS locally |
| M4 Directories + QR | Four directories, provider moderation, badge gates, QR lifecycle | Implemented and regression-tested | Connected directory/provider/QR routes | Existing M1–M4 suites and route inventory | PASS locally |
| M5 Property Manager | Multi-owner portfolio, owner portal, finance, maintenance, utilities, governance, documents, gate and subscription | Implemented with manager/owner scope and idempotency | `/pm/*`, `/owner/dashboard`, `/gate` call real API | 3 API tests, 25 Vitest, 6 responsive browser tests | PASS locally |

## Provider and launch separation

| Area | Local application | Real provider / production |
|---|---|---|
| Stripe | PASS (adapter, local capture, idempotency) | BLOCKED pending live credentials/webhook/Connect validation |
| eKYC | PASS (application boundary) | BLOCKED pending Alibaba credentials and signed callback validation |
| Security | PASS for local scope and authorization tests | Production review, WAF, secret manager and monitoring required |
| Launch | Not claimed | NO until deployment checklist is complete |
