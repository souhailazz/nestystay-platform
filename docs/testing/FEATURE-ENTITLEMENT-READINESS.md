# Feature Entitlement Readiness

## Current authoritative controls

| Product area | Current source of truth | Status |
| --- | --- | --- |
| Host badge feature access | Backend badge/feature access store and ownership checks | READY for current badge rules |
| FREE / VERIFIED / TRUSTED / WELLNESS | Persisted badge assignments, eligibility, benefits, expiry and suspension paths | READY for current rules |
| Badge purchases and renewals | Server-created payment records/PaymentIntent references and webhook-driven state | Implemented; real Stripe verification remains staging-only |
| Property Manager subscription | Persisted PM subscription tier, status, pending changes, renewal and event history | READY for current lifecycle |
| PM operational data | Manager/owner/property scope checks, independent from rental bookings | READY for local PM workflows |
| Generic plan-to-feature catalogue | No client-approved commercial matrix currently exists | NOT GATED to avoid inventing arbitrary prices or entitlements |

## Acceptance rules

1. The frontend may display availability, but it cannot grant badge or PM
   entitlements by setting a success flag.
2. Backend ownership, role, badge level, subscription status and expiry checks
   remain authoritative.
3. Pricing and discount rules are property-scoped and validated before being
   used by quote calculation.
4. A future commercial matrix should be added centrally and versioned; it
   should not be scattered through individual screens.

## Remaining external decision

The client must approve the mapping from each PM subscription tier and badge
level to paid features, limits, prices, currencies, grace periods and renewal
behavior. Until that policy exists, current implemented access rules remain
active and no new arbitrary restriction is introduced.
