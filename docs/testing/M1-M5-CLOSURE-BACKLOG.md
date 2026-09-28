# NestyStay M1–M5 closure backlog

**Date:** 2026-09-27  
**Purpose:** identify every remaining non-PASS status after the implementation-closure pass. The full requirement definition and evidence are in [`M1-M5-MASTER-REQUIREMENTS-MATRIX.md`](M1-M5-MASTER-REQUIREMENTS-MATRIX.md).

There are no `MISSING` rows and no code-caused `PARTIAL` rows after this pass. Every row below is blocked by deployment configuration, an external provider/device, or signed contract change control. `Code Work Required` is deliberately explicit so external blockers are not misreported as unfinished application code.

| ID | Requirement | Current Status | Root Cause | Code Work Required | External Work Required | Files | Tests Required | Final Target |
|---|---|---|---|---|---|---|---|---|
| M1-05 | Password reset/passwordless | PROVIDER_BLOCKED | Brevo delivery not verified | None | Enable/test Brevo sender, delivery and retry | `src/NestyStay.Infrastructure/Email`; auth tests | Controlled mailbox delivery | PASS after provider proof |
| M1-06 | Google login | PROVIDER_BLOCKED | Environment OAuth origin/redirect not replayed | None | Configure and test target-domain OAuth | auth provider/config | OAuth login on target domain | PASS after provider proof |
| M1-09 | Property gallery/photos | CONFIG_BLOCKED | Deployed private MinIO/S3 not evidenced | None | Configure bucket, TLS, permissions, persistence and restart test | storage provider/upload routes | Staging upload/download/delete/restart | PASS after storage proof |
| M1-30 | Payment authorization/capture | PROVIDER_BLOCKED | External Stripe account replay not performed | None | Confirm safe Stripe test mode and replay PaymentIntent | payment provider/webhook code | Staging Stripe test flow | PASS after provider proof |
| M1-31 | Payment failure/recovery | PROVIDER_BLOCKED | External provider failure path not replayed | None | Replay safe test failure/recovery | payment state service | Staging failure/retry | PASS after provider proof |
| M1-32 | Refund/idempotent refund | PROVIDER_BLOCKED | External webhook replay not evidenced | None | Replay signed Stripe refund event and duplicate | webhook/refund code | Signed duplicate webhook test | PASS after provider proof |
| M1-35 | Notifications/unread/deep links | PROVIDER_BLOCKED | External delivery worker/provider not verified | None | Verify Brevo/SMS/push delivery and deployed worker | notification/outbox code | Delivery/outbox/retry test | PASS after provider proof |
| M1-36 | eKYC required/optional branch | CONTRACT_CHANGE_REQUIRED | Signed PDF names Alibaba; approved runtime uses Stripe | None | Signed amendment/change record for Stripe Identity | identity abstraction/provider tests | Contract decision + safe staging session | PASS after change control |
| M1-37 | Contract-specified Alibaba verification | CONTRACT_CHANGE_REQUIRED | Same signed-provider mismatch | Do not reintroduce Alibaba without approval | Signed provider decision | identity docs/history | Change-control evidence | Closed by signed amendment or explicitly removed scope |
| M1-38 | Identity processing/webhook correlation | PROVIDER_BLOCKED | External Stripe Identity event not replayed | None | Confirm target return URL/events/signatures | identity webhook code | Staging test session/webhook | PASS after provider proof |
| M1-39 | Host-paid verification upsell | PROVIDER_BLOCKED | External charge/session not verified | None | Safe Stripe Identity/payment replay | verification pricing code | Staging upsell test | PASS after provider proof |
| M2-10 | Badge purchase | PROVIDER_BLOCKED | External Stripe PaymentIntent not verified | None | Confirm test account and replay purchase | badge payment service/UI | Staging PaymentElement flow | PASS after provider proof |
| M2-11 | Badge payment webhook lifecycle | PROVIDER_BLOCKED | External webhook account proof absent | None | Replay signed success/failure/reversal events | badge webhook code | Duplicate/replay webhook test | PASS after provider proof |
| M2-12 | Badge renewal lifecycle | PROVIDER_BLOCKED | External Stripe billing/deployed worker evidence absent | None | Verify scheduled worker and renewal charge in test mode | maintenance/badge renewal code | Staging renewal run | PASS after provider proof |
| M2-14 | Admin badge management | CONFIG_BLOCKED | Controlled staging Admin not provisioned | None | Provision one QA Admin securely and disable bootstrap | admin badge route/controller | Admin browser replay | PASS after staging access |
| M3-10 | Photo reports/visibility | CONFIG_BLOCKED | Deployed private storage not verified | None | Configure storage and re-run report upload/read | wellness report/storage code | Staging report photo flow | PASS after storage proof |
| M3-11 | Report photos/drafts/retry | CONFIG_BLOCKED | Deployed storage/retention unavailable | None | Configure storage lifecycle and upload checks | report upload code | Valid/invalid/oversize staging tests | PASS after storage proof |
| M3-13 | Fees/commission/ledger | PROVIDER_BLOCKED | Real payout rail not verified | None | Configure/replay Stripe Connect or agreed payout provider | wellness ledger/payout code | Safe payout-state test | PASS after provider proof |
| M3-14 | Escrow/payout/dispute | PROVIDER_BLOCKED | External payout/dispute rails absent | None | Confirm provider capabilities and test mode | wellness payout code | Provider failure/dispute test | PASS after provider proof |
| M3-15 | Wellness notifications | PROVIDER_BLOCKED | External delivery not verified | None | Enable/test Brevo/SMS/push | wellness notification code | Controlled delivery/retry | PASS after provider proof |
| M4-07 | Provider documents/private download | CONFIG_BLOCKED | Deployed object storage not verified | None | Configure private bucket and authorized download | provider documents/storage | Owner/admin/cross-user staging test | PASS after storage proof |
| M4-11 | Guest verification upsell | PROVIDER_BLOCKED | External Stripe Identity/charge not verified | None | Safe provider replay | verification upsell code | Staging start/decline flow | PASS after provider proof |
| M4-15 | Gate/property communication | PROVIDER_BLOCKED | External delivery channel not verified | None | Enable/test Brevo/SMS delivery | gate message/outbox code | Controlled send/read/retry | PASS after provider proof |
| M5-03 | Owner invitation/verification | PROVIDER_BLOCKED | Real invite email not delivered | None | Enable Brevo and replay invitation acceptance | PM invitation code | Controlled inbox invite flow | PASS after provider proof |
| M5-11 | Bulk/overdue/reminders | PROVIDER_BLOCKED | Deployed worker/email evidence absent | None | Verify worker schedule and Brevo delivery | billing worker/outbox code | Batch/overdue/reminder run | PASS after provider proof |
| M5-12 | PM payments/refunds/retries | PROVIDER_BLOCKED | External Stripe billing not verified | None | Safe test-mode billing/retry/refund replay | PM finance/payment code | Staging billing matrix | PASS after provider proof |
| M5-14 | Document metadata/versions/archive | CONFIG_BLOCKED | Production MinIO lifecycle not verified | None | Configure bucket, lifecycle and retention | PM document code | Staging version/archive/expiry | PASS after storage proof |
| M5-15 | Secure object storage | CONFIG_BLOCKED | Production MinIO credentials/persistence absent from evidence | None | Configure private MinIO/S3, TLS, backup and restart | `MinioStorageProvider.cs` | Real upload/download/auth/restart | PASS after storage proof |
| M5-18 | Gate communication/QR lifecycle | PROVIDER_BLOCKED | Camera/device and external delivery not verified | None | Test physical camera/device and delivery | QR/gate code | Device + delivery matrix | PASS after external proof |
| M5-19 | Staff invitations/permission scope | PROVIDER_BLOCKED | Real invite email not delivered | None | Enable Brevo and replay PM staff invite | staff invitation/auth code | Invite/accept/scope denial | PASS after provider proof |
| M5-20 | PM subscription lifecycle | PROVIDER_BLOCKED | External billing webhooks not verified | None | Safe Stripe subscription webhook replay | subscription code | Full billing transition matrix | PASS after provider proof |
| M5-21 | Tenant/owner verification/balances | PROVIDER_BLOCKED | External identity/email not verified | None | Confirm provider path and test target environment | PM verification code | Staging verification flow | PASS after provider proof |

## Explicitly not backlog items

`M1-41`, `M1-42`, `M1-43`, `M1-44`, `M5-24`, and `M5-25` are `NOT_APPLICABLE` to the signed M1–M5 scope. `M4-17` and `M5-26` are `OUT_OF_SIGNED_SCOPE` Phase 6 items. They are not application defects for this release and must not be reopened without a scope decision.
