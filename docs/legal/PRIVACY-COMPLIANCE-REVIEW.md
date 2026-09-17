# NestyStay privacy and consumer-information review

This is an engineering baseline and implementation record, not legal advice or a certification of compliance. The business owner and qualified Jamaican counsel must replace the marked business details and approve the notices before production bookings are accepted.

## Implemented in the frontend

- `/privacy`, `/terms`, `/cookies`, and `/refund-policy` are public routes.
- The public footer links to all four policies and provides a persistent Cookie settings control.
- A conservative cookie-choice dialog is shown until a visitor chooses. Optional analytics is not loaded by this build and is disabled by default.
- Account registration requires an explicit, unchecked-by-default acknowledgement of the Terms of Service and Privacy Policy. The links open the actual policy pages.
- Policy copy avoids universal promises about verification, insurance, refunds, security, response times, reviews, or listing accuracy.
- Public FAQ answers now defer to the specific listing and booking record.
- Informative map-card imagery has text alternatives; decorative brand/story imagery remains intentionally hidden from assistive technology because equivalent text is present.

## Tracking and third-party audit

No analytics SDK or tag was found in the frontend package, `index.html`, or source scan for common providers (Google Analytics, Plausible, Mixpanel, Segment, Hotjar, Amplitude, and Meta Pixel). Do not add one without updating the Cookie Policy and consent gate.

Current external requests/features requiring disclosure and licensing review:

- Stripe.js, Stripe Payments, and Stripe Identity.
- Google OAuth and Google Fonts.
- OpenStreetMap iframe maps.
- Remote Unsplash image URLs in public marketing/demo screens.
- Configured Brevo, Zoho, hosting, and object-storage providers where enabled on the server.

## Copyright and image provenance

The repository contains remote `images.unsplash.com` URLs but no checked-in licence/attribution register proving that each selected image is approved for this commercial product. The images must be reviewed against the applicable Unsplash licence and any photographer/model/property restrictions, or replaced with assets for which NestyStay has written commercial rights. This review does not claim that remote images are cleared.

## Jamaica launch actions still required

The Jamaica Data Protection Act 2020 and Office of the Information Commissioner guidance should be reviewed with counsel. Before production, confirm:

1. Registered legal entity name, service address, privacy email, and data-protection contact/DPO.
2. OIC controller registration and whether a DPO and DPIA are required for the final processing scope.
3. A retention schedule for accounts, bookings, identity metadata, messages, support, payments, consent, audit logs, and backups.
4. Processor agreements and international-transfer safeguards for Stripe, Brevo, Zoho, hosting, storage, maps, fonts, and OAuth.
5. Stripe Identity biometric/identity notice and consent wording, including the exact data returned to NestyStay and deletion/retention responsibilities.
6. Minimum-age/guardian rules, consumer/refund/tax wording, cancellation terms, insurance/protection claims, dispute handling, and governing law.
7. Incident response, breach assessment, and the operator's required notification workflow.
8. Accessibility review with keyboard, screen reader, forced-colour, reduced-motion, and zoom testing across every public and authenticated route.

## Evidence sources

- Jamaica [Data Protection Act 2020](https://laws.moj.gov.jm/legislation/aop/2/7_2020-The%20Data%20Protection%20Act.pdf)
- Jamaica Office of the Information Commissioner [controller obligations](https://www.oic.gov.jm/content/what-are-obligations-data-controllers)
- OIC [data protection standards](https://oic.gov.jm/page/data-protection-standards)
- ICO [cookies and similar technologies](https://ico.org.uk/for-organisations/direct-marketing-and-privacy-and-electronic-communications/guide-to-pecr/cookies-and-similar-technologies/)
- Stripe [Privacy Center](https://stripe.com/legal/privacy-center) and [Identity explanation](https://docs.stripe.com/identity/explaining-identity)
