# Handwritten Contract Notes

Source: `docs/contracts/NestyStay-Signed-Agreement-April-2026.pdf`, page 4 (visually inspected from the rendered signed PDF). This is a transcription and classification record, not a new pricing decision. Ambiguous handwriting is deliberately not hard-coded.

## Readable notes

| # | Page 4 note | Classification | Current decision |
|---:|---|---|---|
| 1 | “MISSING” | CLEAR / COMMERCIAL_ONLY | A heading or reminder, not an independently actionable software requirement. No feature is inferred from it. |
| 2 | “Nesty Stay coming soon” | CLEAR / COMMERCIAL_ONLY | Launch/positioning note. It does not define a product workflow. |
| 3 | “Gold” and “Platinum” | CLEAR / FEATURE-ADJACENT / COMMERCIAL_ONLY | Additional membership/tier concepts beyond the four official FREE, VERIFIED, TRUSTED and WELLNESS levels. Not assigned to M1–M5 without written scope confirmation. |
| 4 | Values around “150 / 150” | AMBIGUOUS / COMMERCIAL_ONLY | Appears alongside Gold/Platinum tier notes, but the unit, currency, cadence and target are not unambiguous. Not implemented as pricing. |
| 5 | “Business logic” | CLEAR / COMMERCIAL_ONLY | General instruction to define rules; it does not specify a testable rule by itself. |
| 6 | “POST LAUNCH — HOST 3% FEE” | CLEAR / COMMERCIAL_ONLY | Consistent with the typed 3% host commission, but marked post-launch. The current pricebook retains the typed 3% baseline; launch timing remains a commercial decision. |
| 7 | “POST LAUNCH — GUEST 10% FEE” | CLEAR / COMMERCIAL_ONLY | Conflicts with the typed 9% guest platform fee. The application uses the typed 9% baseline; the 10% post-launch note is recorded as unresolved commercial policy. |
| 8 | “YEARLY BADGE SYSTEM” | CLEAR / FEATURE_REQUIREMENT | Supports annual badge renewal/lifecycle logic. Current local implementation includes renewal records and expiry/renewal maintenance. |
| 9 | “TRUSTED” reference | CLEAR / FEATURE_REQUIREMENT | Reinforces the official TRUSTED tier; it does not create a second Trusted rule or price. Official page 3/9 rules remain authoritative. |
| 10 | “WELLNESS VERIFIED BADGE — SEEN BY GUEST” | CLEAR / FEATURE_REQUIREMENT | Guest-visible Wellness badge is required. Current property/listing/host surfaces expose the badge state where the API provides it. |
| 11 | Note that the owner/host should see Wellness visibility/status | AMBIGUOUS-to-CLEAR / FEATURE_REQUIREMENT | Interpreted narrowly as host/owner visibility of the Wellness status, not a new Wellness operational workflow. Local host/owner surfaces expose badge/status information; exact wording/placement may be confirmed by the client. |
| 12 | “FOUNDING MEMBER” | CLEAR / COMMERCIAL_ONLY | Founding-member benefit/tier concept. Existing campaign/founding-benefit code is retained, but no new entitlement is inferred from the handwriting. |
| 13 | Gold/Platinum duration and payment concepts | AMBIGUOUS / COMMERCIAL_ONLY | The page appears to discuss 18/36-month concepts, but exact mapping to Gold vs Platinum, payment timing and entitlement duration are unclear. Not hard-coded. |
| 14 | “Fee for life of property” / property-linked fee language | AMBIGUOUS / COMMERCIAL_ONLY | Property-vs-host ownership, transferability and “for life” semantics are not sufficiently legible/defined. Existing founding-benefit transfer evaluation is not expanded from this note. |
| 15 | Handwritten booking amounts, including “$36 per booking” and “$29 per booking” near Gold/Platinum | AMBIGUOUS / COMMERCIAL_ONLY | The beneficiary, currency, cadence and relation to guest verification are unclear. Not substituted for the typed $0.14 optional verification charge or typed 9% guest fee. |
| 16 | “150 + 150 … property and guest for life of property” and “18 month / 36 month” references | AMBIGUOUS / COMMERCIAL_ONLY | Recorded as commercial sketches only; no exact product behavior can be safely derived. |

## Page 5

Page 5 was visually inspected and is blank apart from a signature mark at the bottom. **NO ADDITIONAL FUNCTIONAL REQUIREMENT IDENTIFIED.**

## What is implemented without inventing handwriting

- The four official badge levels remain FREE, VERIFIED, TRUSTED and WELLNESS.
- Annual renewal, expiry, suspension and reactivation are implemented in the local badge lifecycle.
- Founding/campaign benefit structures already present in the code remain configurable and are not reinterpreted as Gold/Platinum.
- The guest fee remains the typed 9% baseline, while the handwritten post-launch 10% note is documented for client confirmation.
- The typed Trusted `$49 one-time`, Wellness `$25–$50/visit` or `$19/month`, and host 3% commission baselines remain the coded contract values.

## Client confirmations required

Written confirmation is required before implementing any of the following commercial changes:

1. Whether Gold and Platinum are actual launch tiers or future concepts.
2. The currency, amount, payer, cadence and entitlement for the handwritten 150/150, $36 and $29 values.
3. Whether “for life of property” means transferable on property sale and how ownership changes affect entitlement.
4. The exact Gold/Platinum durations and whether the 18/36-month notes are correct.
5. Whether the guest fee changes from typed 9% to post-launch 10%, and when that change takes effect.
