# NestyStay email template catalog

All transactional templates use the same responsive HTML shell:

- NestyStay dark-green header and Jamaica descriptor
- gold section marker and serif headline
- readable detail/status cards
- responsive CTA button with copyable URL fallback
- plain-text alternative for accessibility and mail clients
- support footer that tells recipients never to send passwords, codes or identity documents by email

The catalog is implemented in `backend/src/NestyStay.Infrastructure/Notifications/EmailDelivery.cs` and values are HTML-encoded before they are inserted.

## Catalog coverage

| Area | Template keys | Current delivery state |
|---|---|---|
| M1 account and security | `passwordless-login`, `auth-code`, `password-reset`, `two-factor-enabled`, `new-login-alert` | Passwordless, verification and password reset are connected to the outbox. Security alerts are catalog-ready. |
| M1 booking | `booking-request-received`, `booking-request-to-host`, `booking-pending-verification`, `verification-processing`, `verification-approved`, `verification-rejected`, `booking-approved`, `booking-rejected`, `booking-host-declined`, `booking-cancelled`, `booking-update`, `trip-reminder` | Templates are ready. Booking status and reminder email enqueue calls still need to be connected to the corresponding production events; current booking notifications are primarily in-app. |
| M1 payments | `payment-authorized`, `payment-failed`, `payment-confirmed`, `payment-refunded`, `receipt-issued` | Templates are ready. Payment event email enqueue calls and live Stripe delivery validation remain deployment work. |
| M2 badges | `badge-upgrade-submitted`, `badge-upgrade-approved`, `badge-upgrade-rejected`, `badge-renewal-due` | Catalog-ready; badge lifecycle email events still need explicit outbox wiring. |
| M3 wellness and finance | `officer-application-submitted`, `officer-application-approved`, `officer-application-changes-requested`, `wellness-assignment-confirmed`, `wellness-report-ready`, `wellness-booking-cancelled`, `subscription-renewal-due`, `subscription-payment-failed`, `payout-statement-ready` | Catalog-ready; workflow-specific email enqueue calls and production delivery validation remain. |
| M4 directories and access | `provider-application-submitted`, `provider-approved`, `provider-rejected`, `provider-changes-requested`, `directory-quote-received`, `review-response`, `qr-issued`, `qr-revoked` | Catalog-ready; moderation, quote, review and QR event email wiring remains. |
| M5 property management | `owner-invitation`, `invoice-issued`, `invoice-payment-received`, `invoice-payment-reminder`, `maintenance-update`, `community-notice`, `governance-vote-opened`, `gate-pass-issued`, `gate-pass-revoked`, `document-expiry` | Owner invitations and document-expiry reminders are connected to the outbox. Invoice, maintenance, community, governance and gate event wiring remains. |

## Still required before real delivery

1. Configure a verified Brevo sender and domain on the deployment server.
2. Set `EMAIL_PROVIDER=brevo`, `NESTYSTAY_EMAIL_PROVIDER=brevo`, `BREVO_ENABLED=true`, `BREVO_API_KEY` and `BREVO_SENDER_EMAIL` in the server secret store.
3. Connect the catalog-ready event handlers to `IEmailSender.QueueAsync` with the documented placeholder values.
4. Run staging tests for successful delivery, retry/dead-letter behavior, provider message IDs and clickable links.
5. Keep marketing consent/unsubscribe handling separate from mandatory transactional messages.

The catalog does not send email by itself. An email is delivered only when a real workflow enqueues an `EmailMessage` with a matching `TemplateKey` and the server's configured transport is able to deliver it.
