# Staging integration readiness

Audit date: 2026-09-21. Scope: current NestyStay M1–M5 web/API implementation and the deployment owner’s staging findings. Staging and production were not modified by this audit. QA passwords are intentionally not recorded.

## Readiness matrix

| Area | Code exists | Config present | Local tested | Staging tested | Result | Action |
| --- | --- | --- | --- | --- | --- | --- |
| Stripe Identity | Yes; Stripe-only DI and session/result boundary | Deployment owner says active; values not read here | Application/provider/security tests pass; no live provider claim | Not run with a credential in this workspace | READY FOR STAGING QA | Run one safe Stripe test-mode session and signed event on staging |
| Stripe webhook | Yes; signature validation, persistent event receipt, duplicate protection and event mapping | Deployment owner says secret active | Webhook security/persistence tests pass | Deployment owner reports functional delivery | READY | Exercise Identity events and replay/idempotency in test mode |
| Brevo | Yes; unified templates, outbox, worker, retry and dead-letter handling | API key exists but provider was reported disabled | Template/outbox tests pass; no mailbox delivery claim | Not run | BLOCKED_STAGING_EMAIL_PROVIDER | Set selector/enable flag/sender, restart worker, verify mailbox delivery |
| Object storage | Yes; private server-local adapter, scoped upload routes, and signed API downloads | Server-local root and signing secret still required | Local round-trip/path/signature tests pass | Not run | BLOCKED_STAGING_OBJECT_STORAGE | Configure persistent private directory and run upload/download matrix |
| Admin QA | Bootstrap exists; public registration correctly rejects Admin | No Admin account reported | Bootstrap/authorization code exists; no staging admin provisioned here | Not run | BLOCKED | Deployment owner provisions one QA Admin in a controlled window, then disables bootstrap |
| Gate Guard | Enum/blueprint exists; no provisioning or role-auth path | N/A | QR validation exists, but not Gate Guard role flow | Not run | UNCLEAR_SCOPE | Clarify whether current web M4/M5 requires a dedicated role; current contract places dedicated interface later |
| Maps | Partial: OpenStreetMap embeds and a public map-search route | External embed requires network/provider policy | Code inspected; no provider certification | Not run | PARTIAL | Verify attribution, availability and privacy policy before production |
| Geocoding | Partial: Nominatim-compatible host suggestions with manual fallback | Optional `VITE_GEOCODER_URL` | Code inspected; no live rate-limit/provider certification | Not run | PARTIAL | Confirm permitted provider/usage policy or keep manual entry |
| QA accounts | Eight seeded role accounts reported by deployment owner; PM Staff is invited | Credentials intentionally not stored here | Login matrix run with runtime-only secret injection; no credentials persisted | Login responses and role claims verified; PM Staff invitation remains pending | PARTIAL | Redeploy the current frontend, then repeat protected-route and invitation-acceptance checks |

## Evidence-based findings

### Stripe Identity and Alibaba

The default and supported eKYC selector is `stripe_identity`. DI throws for unsupported providers, and no active Alibaba/Aliyun reference was found in `backend/src` or `frontend/src`. Historical migration/contract text is not an active provider. Stripe raw webhooks validate signatures with the configured signing secret, persist provider event receipts, sanitize Identity payloads, and map `verified`, `canceled`, `requires_input`, and `processing`. The local suite covers these boundaries; real Stripe Identity acceptance still requires a safe staging test-mode session.

### Upload and storage readiness

The current upload architecture is server-mediated. A feature first creates a scoped metadata row, then uploads through an authenticated API `PUT`; the application performs size/content-type/magic-byte/hash validation before saving. Downloads are time-limited and authorization-checked. The storage abstraction has no physical delete or bucket-list operation. PM archive/version states are logical lifecycle operations, so a persistent object-retention policy and backup policy are needed on the server.

The local storage provider has deterministic round-trip, path-traversal, size-limit, expiry, and signature-isolation tests. Staging still requires a persistent server directory and a real upload/download smoke test. Restart persistence, filesystem ownership/ACLs, backup/restore, wrong-role access, and public-web-root exposure are not claimed as staging-tested until the deployment owner verifies them.

Insurance claim evidence is not an object upload workflow in the current code; claims persist an `EvidenceJson` field and no claim-evidence upload endpoint was found. It should not be listed as a storage defect without a separate signed requirement.

### Brevo and email workflows

The templates have one unified NestyStay HTML/text shell and include authentication, booking pending/approved/rejected, receipts, identity, Wellness, provider moderation, PM invoices/maintenance/community/governance, and gate-pass notifications. `EmailOutboxSender` persists the business notification before delivery. `EmailDeliveryWorker` retries pending/retrying records up to five attempts; a provider outage does not roll back the underlying booking or other business action. Local template/outbox tests pass. Actual Brevo delivery is blocked until Terrence selects Brevo, enables the worker, confirms a verified sender/domain, restarts, and tests a real mailbox.

### Admin and Gate Guard

Admin cannot be self-registered. Startup bootstrap is the only discovered provisioning mechanism; it is disabled by default and should be enabled only for one controlled restart window. GateGuard is an enum value and appears in the product blueprint, but there is no way to assign it through registration, invitation, admin management, or a role policy. The QR validator is a separate public/token workflow. Because the signed agreement’s dedicated gate-guard interface is in the later mobile-app phase while web M4/M5 cover QR and manager gate communication, this audit does not add a new role-provisioning feature.

## QA classification

- `BLOCKED_STAGING_OBJECT_STORAGE`: property photos, profile photos, PM documents, maintenance attachments, Wellness report/officer documents, directory provider documents, message attachments, admin case evidence, and PM exports cannot be professionally verified until the selected bucket is configured.
- `BLOCKED_STAGING_EMAIL_PROVIDER`: external mailbox delivery cannot be verified while Brevo is disabled. Local generation/outbox behavior is not classified as failed.
- `BLOCKED_STAGING_ADMIN`: the available QA role set does not include Admin; use the controlled bootstrap procedure.
- `UNCLEAR_SCOPE_GATE_GUARD`: do not report a current M4/M5 application failure until the scope decision is written.
- Real Stripe Identity / Stripe payment provider acceptance is safe only after confirming staging is test mode. No live-mode transaction was attempted.

## Local change made in this pass

`ProductionIntegrationValidator` now resolves the email provider through the same `ProviderFeatureFlags` path used by DI. This closes a configuration-consistency bug where `EMAIL_PROVIDER=brevo` could activate Brevo while production startup validation looked only at the legacy selector. The local storage provider now exposes a real readiness check (private root plus write/delete probe), production rejects storage signing secrets shorter than 32 bytes, and email transport exceptions use the existing retry/dead-letter policy instead of leaving records in `PROCESSING`. No staging configuration was changed.

The frontend branch also fixes role-aware post-login routing. The previous deployed build routed every successful password login through `/guest-dashboard` because it read React auth state before the asynchronous state update completed. The fix reads the synchronously persisted session and is covered by `postAuthRoute` tests. Commit `424a90b4d1a69703654d34436894f87d5021c5a8` is pushed to `codex/m1-m2-runtime-hardening`.

## Runtime QA performed on staging

Using the deployment owner's staging-only QA accounts through a temporary runtime secret (never written to source, logs, screenshots, or this document):

- Guest, Host, Owner, Property Manager, Wellness Officer, Service Provider, and Local Business login requests returned HTTP 200 with the expected role claim.
- PM Staff returned HTTP 200 with the seeded `Guest` role; the account is still in the intentionally invited state, so invitation acceptance and scoped staff permissions remain unverified.
- The current staging frontend routed every non-Guest role to `/guest-dashboard` first, although its workspace navigation later reflected the authenticated role. This is fixed in the pushed frontend branch but is not yet visible on staging.
- Direct protected-route checks on the current staging frontend are therefore not accepted as a client authorization pass. The backend boundary was checked separately: Guest received HTTP 403 from `/api/property-manager/dashboard`, while Property Manager received HTTP 200.
- After the frontend branch is merged and redeployed, repeat the login landing, forbidden-route, PM Staff invitation, and mobile checks before professional QA.

## Required sequence before professional QA

1. Merge the storage/provider changes through the protected PR workflow and redeploy staging.
2. Configure the private server directory and signing secret using `docs/deployment/STAGING-INTEGRATION-CONFIG.md`; restart and run the upload/download matrix.
3. Enable Brevo, verify sender/domain DNS using the records Brevo provides, restart the worker, and confirm representative mailbox delivery.
4. Provision one QA Admin through the temporary bootstrap plan, then disable bootstrap and rotate the temporary credential.
5. Inject all QA credentials only through a secure runtime mechanism and execute the role-isolation matrix, including the invited PM Staff acceptance flow.
6. Confirm Stripe test mode and run the Stripe Identity, webhook replay, payment, and refund checks.

Until those steps pass, professional QA is **not ready**, even though the core local application seams are implemented and tested.
