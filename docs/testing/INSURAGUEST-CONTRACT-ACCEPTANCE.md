# InsuraGuest Contract Acceptance

Date: 2026-09-17 (Africa/Casablanca)

This record covers the InsuraGuest add-on described in the signed NestyStay agreement. It is a local application acceptance record. The local adapter is deterministic and must not be represented as a live InsuraGuest insurer decision.

## Contract plan catalog

| Plan | Market | Monthly price | Property damage | Accidental medical |
|---|---|---:|---:|---:|
| `non-us-50` | Non-US | $50 USD | $10,000 | Not included |
| `us-69` | US | $69 USD | $10,000 | $10,000 |
| `us-99` | US | $99 USD | $25,000 | $25,000 |

The plans are returned by `GET /api/insurance/plans` from the `IInsuranceProvider` seam. The current local implementation is `InsuraGuestProvider` in `backend/src/NestyStay.Infrastructure/DependencyInjection.cs`.

## Accepted local workflow

- Host plan comparison is available at `/host/insurance`.
- A host can activate coverage for an owned property only.
- The policy is persisted with plan code, market, monthly amount, coverage limits, provider reference and idempotency key.
- Lifecycle history persists `NO_COVERAGE → PLAN_SELECTED → PENDING → ACTIVE` or `FAILED`.
- A host can cancel active coverage, mark renewal due, and renew it.
- An active policy sets the property coverage flag used by booking protection rules; the host UI explicitly states that damage-deposit/waiver forms are not requested for covered properties.
- Claims require an active policy, a non-future incident and submission within 72 hours. Claims are persisted as `SUBMITTED` and are visible again after reload.
- Property managers receive read-only scoped coverage state, plan, provider reference, renewal date and failure reason at the existing insurance module. Admins can list policy records.
- Hosts, managers, owners and admins can read only the property records allowed by the existing authorization rules; host policy mutations require ownership.

## API acceptance checks

`backend/tests/NestyStay.Api.Tests/InsuranceEndpointTests.cs` covers:

- exact plan catalog and coverage limits;
- host activation and idempotent replay;
- persisted lifecycle history;
- cancellation and renewal-due/renewal transitions;
- deterministic provider failure (`PENDING → FAILED`) and persisted failure state;
- cross-host read protection;
- 72-hour claim rejection and valid claim persistence.

Focused result on 2026-09-17: **5 passed, 0 failed, 0 skipped**.

The existing full backend suite also passed: **188 passed, 0 failed, 0 skipped** across Domain (5), Application (23), Infrastructure (27) and API (133) test projects after the InsuraGuest migrations were applied.

## Browser evidence

The real local frontend/backend were exercised through Chromium with a registered host, an owned property and a genuine UI activation click:

- Desktop viewport: 1440 × 1000 — [screenshot](../../testing-evidence/insuraguest-contract/desktop-chromium-viewport.png)
- Tablet viewport: 1024 × 768 — [screenshot](../../testing-evidence/insuraguest-contract/tablet-chromium-viewport.png)
- Mobile viewport: 390 × 844 — [screenshot](../../testing-evidence/insuraguest-contract/mobile-chromium-viewport.png)

Focused browser result: **3 passed, 0 failed**. The full configured browser regression also passed **161 tests with 36 intentional skips and 0 failures** across desktop Chromium, Firefox/WebKit smoke, tablet Chromium and mobile Chromium. The screenshots show the real plan catalog, active policy, provider reference and persisted lifecycle timeline. The full-page variants are retained beside the viewport captures.

## Provider and production boundary

- Local InsuraGuest mode: deterministic in-process adapter; activation normally returns `ACTIVE` with a local provider reference. Setting `Insurance:LocalActivationOutcome=FAILED` exercises the failure branch.
- Real InsuraGuest account/API: not configured or externally verified in this local environment.
- Real insurer claims adjudication, policy issuance, billing collection and production renewal events: not claimed as complete.
- No Alibaba eKYC code or configuration is used for this requirement; identity verification remains the separately agreed Stripe Identity path.

## Acceptance conclusion

**Contract behavior is complete for local deterministic application acceptance. Live InsuraGuest provider acceptance remains external and is required before production coverage is advertised as insurer-backed.**

Candidate implementation SHAs: backend `9bcbb8c739d5fc755d3a381bce883da7fb85153a`, frontend `d83cdd453f9e0496ea85c03efb9783c2b222967e`. These are feature-branch SHAs and still require the client's protected-main PR review/merge before staging deployment.
