# NestyStay M1–M5 production acceptance

## Final decision

**NESTYSTAY PRODUCTION GO: NO**

All current local application workflows are implemented and have automated evidence, but the strict definition of done is not met. No milestone is marked `COMPLETE` until its staging, provider and required human/device gates pass.

| Milestone | Code | Local E2E | Staging E2E | External/provider | Manual/device | Final status |
|---|---|---|---|---|---|---|
| M1 | YES | NO — final one-run matrix pending | NO | Stripe, Stripe Identity, Brevo and payment webhooks pending | Accessibility pending | NOT COMPLETE |
| M2 | YES | NO — all-device/manual evidence pending | NO | Live badge PaymentIntent/lifecycle pending | Accessibility/visual evidence pending | NOT COMPLETE |
| M3 | YES | YES for covered lifecycle | NO | Connect, storage and notifications pending | Physical officer journey pending | NOT COMPLETE |
| M4 | YES | YES for covered directory/QR paths | NO | Map/geocoder and gate delivery pending | Physical camera/accessibility pending | NOT COMPLETE |
| M5 | YES for accepted local matrix | NO — exhaustive manual operations pending | NO | Billing, storage, notifications and workers pending | Full mobile/operations review pending | NOT COMPLETE |

## Required final evidence package

- Deployed frontend and backend SHAs
- Staging role-by-role results and screenshots
- Stripe/Identity/Connect event and webhook evidence with secrets redacted
- Brevo delivery and failure/retry evidence
- Storage authorization and restart evidence
- Backup/restore evidence
- Monitoring and alert-delivery evidence
- Security final certification
- Human/device accessibility and QR certification
