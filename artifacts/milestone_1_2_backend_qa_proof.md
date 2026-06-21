# NestyStay Backend QA Proof: Milestone 1 and Milestone 2

## Summary Verdict

Milestone 1 backend status: PASS.

The backend covers registration, secure login, 2FA challenge verification, property listing and creation, booking quote API, PENDING / APPROVED / REJECTED booking flow, Alibaba Cloud eKYC transaction/webhook handling, date holds, overlap blocking, rejection date release, approval/rejection notifications, and Stripe authorization/capture with capture blocked until APPROVED.

Milestone 2 backend status: PASS.

The backend covers badge levels, eligibility rules, pricebook administration, badge purchase payment outcomes, annual renewals, campaign pricing, founding benefits, transfer checks, commission quotes, guest verification upsell eligibility, feature locking/unlocking, and API-level acceptance coverage.

Frontend status: untouched and out of scope for this pass.

## Exact Commands Run

```powershell
dotnet test tests\NestyStay.Application.Tests\NestyStay.Application.Tests.csproj --no-restore
dotnet test tests\NestyStay.Api.Tests\NestyStay.Api.Tests.csproj --no-restore
dotnet test NestyStay.sln --no-restore
```

Local API smoke test:

```powershell
$env:ASPNETCORE_URLS = "http://127.0.0.1:5098"
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src\NestyStay.Api\NestyStay.Api.csproj --no-launch-profile
```

Smoke test endpoints exercised:

```text
GET  /api/health
POST /api/auth/register
POST /api/auth/login
POST /api/auth/2fa/verify
POST /api/properties
POST /api/bookings/quote
POST /api/bookings
POST /api/bookings/{id}/capture-payment
POST /api/webhooks/alibaba-ekyc
GET  /api/badges-pricing/badges/features/{subjectType}/{subjectId}
POST /api/badges-pricing/badges/purchase
POST /api/badges-pricing/campaigns/trusted-host-pdf-campaign/enroll
POST /api/badges-pricing/renewals/{assignmentId}/pay
POST /api/badges-pricing/badges/assignments/{assignmentId}/expire
POST /api/badges-pricing/founding-benefits
POST /api/badges-pricing/commission-quote
```

## Test Counts

Full backend suite:

```text
Domain tests:          5 passed, 0 failed
Application tests:    16 passed, 0 failed
Infrastructure tests:  3 passed, 0 failed
API tests:             5 passed, 0 failed
Total:                29 passed, 0 failed
```

Known warning:

```text
NU1903: Package System.Security.Cryptography.Xml 9.0.0 has known high severity vulnerabilities.
Recommendation: upgrade the package dependency in a dedicated dependency/security update pass.
```

## Live API Smoke Result

```json
{
  "health": "OK",
  "duplicateRegistrationBlocked": true,
  "sessionUserMatches": true,
  "propertyGuestVerification": true,
  "quoteTotal": 610.5,
  "bookingStatus": "PENDING",
  "pendingCaptureBlocked": true,
  "approvedStatus": "APPROVED",
  "duplicateWebhookNotificationCount": 2,
  "capturedPaymentStatus": "CAPTURED",
  "defaultBadgeLevel": "Free",
  "verifiedPaymentStatus": "CAPTURED",
  "trustedCampaignCharge": 49,
  "trustedFeatureLevel": "Trusted",
  "renewedPaymentStatus": "CAPTURED",
  "expiredTrustedStatus": "Expired",
  "afterExpireFeatureLevel": "Verified",
  "foundingTier": "Silver",
  "foundingGuestFee": 45,
  "commissionRevenue": 75
}
```

## Files Changed

```text
backend/src/NestyStay.Application/DependencyInjection.cs
backend/src/NestyStay.Application/PhaseOne/PhaseOneModels.cs
backend/src/NestyStay.Application/PhaseOne/PhaseOneStore.cs
backend/src/NestyStay.Application/PhaseTwo/PhaseTwoModels.cs
backend/src/NestyStay.Application/PhaseTwo/PhaseTwoStore.cs
backend/src/NestyStay.Api/Controllers/BadgesPricingController.cs
backend/src/NestyStay.Api/Controllers/PropertiesController.cs
backend/tests/NestyStay.Application.Tests/PhaseOneWorkflowTests.cs
backend/tests/NestyStay.Application.Tests/PhaseTwoWorkflowTests.cs
backend/tests/NestyStay.Api.Tests/HealthEndpointTests.cs
backend/tests/NestyStay.Api.Tests/PhaseTwoEndpointTests.cs
artifacts/milestone_1_2_backend_qa_proof.md
```

No frontend files were changed.

## Scenario Coverage Table

| Area | Coverage | Evidence |
|---|---|---|
| Registration | Valid registration, duplicate email rejection, invalid email, empty fields, weak password, PBKDF2 password hash, default 2FA requirement | `PhaseOneWorkflowTests.RegistrationRejectsInvalidDuplicateAndWeakInputsAndStoresPasswordHash`; API duplicate/weak checks |
| Login | Valid login, wrong password, unknown email, consistent invalid credential rejection | `PhaseOneWorkflowTests.LoginAndTwoFactorRejectWrongUnknownExpiredInvalidAndReusedChallenges` |
| 2FA | Challenge creation, correct code success, wrong code rejection, missing challenge rejection, expired challenge rejection, reused challenge rejection | `PhaseOneWorkflowTests.LoginAndTwoFactorRejectWrongUnknownExpiredInvalidAndReusedChallenges` |
| Property listings | Seeded listing fetch, property detail fetch, valid property creation, missing fields, bad rate, Free host guest-verification upsell rejection | `PhaseOneWorkflowTests.PropertyCreationListingAndValidationRespectGuestVerificationUpsellRules`; API property creation checks |
| Booking quote | Summary, dates, nights, fees, total, verification requirement, invalid same-day date rejection, unknown property rejection, overlap rejection | `PhaseOneWorkflowTests.BookingWithGuestVerificationMovesThroughPendingApprovedAndIdempotentPaymentCapture`; `BookingQuotesAndCreationRejectInvalidUnavailableAndUnknownInputs` |
| Booking creation | PENDING when eKYC required, APPROVED when no eKYC required, guest/property/date/totals/provider state stored, overlap blocking | `PhaseOneWorkflowTests.BookingWithGuestVerificationMovesThroughPendingApprovedAndIdempotentPaymentCapture`; `NonVerificationPropertyApprovesImmediatelyAndDoesNotStartEkyc` |
| Booking statuses | API returns exact `PENDING`, `APPROVED`, `REJECTED` milestone statuses | Phase One service and API tests |
| Alibaba eKYC | Transaction start, valid approval webhook, valid rejection webhook, rejection releases dates, approval reserves dates, unknown/mismatched transaction rejection, duplicate webhook idempotency, conflicting webhook rejection | `PhaseOneWorkflowTests.BookingWithGuestVerificationMovesThroughPendingApprovedAndIdempotentPaymentCapture`; `RejectionFlowReleasesDatesAndPreventsPaymentOrConflictingWebhook`; API duplicate/conflict checks |
| Stripe/payment | Authorization after approval, capture blocked while PENDING, capture blocked for REJECTED, capture allowed after APPROVED, duplicate capture idempotent, local adapter test-safe | `PhaseOneWorkflowTests.BookingWithGuestVerificationMovesThroughPendingApprovedAndIdempotentPaymentCapture`; `RejectionFlowReleasesDatesAndPreventsPaymentOrConflictingWebhook` |
| Notifications | Approval and rejection queue guest and host notifications; duplicate webhooks do not duplicate notifications | Phase One service and API webhook idempotency checks |
| Badge levels | Free default state, Verified/Trusted/Wellness unlock tiers, expired/suspended badges remove locked features | `PhaseTwoWorkflowTests.HostsDefaultToFreeFeaturesAndEligibilityControlsBadgeProgression`; `BadgePaymentsDuplicatesRenewalsExpiryAndSuspensionKeepFeatureAccessConsistent` |
| Eligibility | Verified requires host eKYC flag, Trusted requires active Verified plus booking count, Wellness requires active Verified plus address/subscription | `PhaseTwoWorkflowTests.HostsDefaultToFreeFeaturesAndEligibilityControlsBadgeProgression` |
| Pricebook | Expected keys, seeded prices, admin update, negative price rejection, missing key behavior, amount precision rounding | `PhaseTwoWorkflowTests.PricebookContainsExpectedKeysAndRejectsInvalidUpdates`; API invalid price check |
| Badge purchases/payments | Valid purchase success, failed payment creates non-unlocking suspended assignment, duplicate active purchase idempotency | `PhaseTwoWorkflowTests.BadgePaymentsDuplicatesRenewalsExpiryAndSuspensionKeepFeatureAccessConsistent` |
| Annual renewals | Renewal queues, payment extends expiry, invalid/inactive renewal guarded | `PhaseTwoWorkflowTests.BadgePaymentsDuplicatesRenewalsExpiryAndSuspensionKeepFeatureAccessConsistent` |
| Campaign pricing | Campaign discount only after enrollment, invalid/expired campaign rejection, minimum campaign amount guard, campaign purchase price consistency | `PhaseTwoWorkflowTests.CampaignPricingRequiresEligibilityAndValidActiveCampaigns` |
| Founding benefits | Eligible benefits granted, non-eligible rejected, one-time property claim guarded, flat fees correct | `PhaseTwoWorkflowTests.FoundingBenefitsResolveDiscountsTransfersAndPreventInvalidClaims` |
| Transfer checks | Valid transfer succeeds, invalid transfer returns missing requirements | `PhaseTwoWorkflowTests.FoundingBenefitsResolveDiscountsTransfersAndPreventInvalidClaims` |
| Commission quotes | 97% host payout / 3% commission, standard guest fee, founding flat fee, zero/negative/large decimal cases | `PhaseTwoWorkflowTests.CommissionQuotesCoverStandardFoundingAndEdgeCases` |
| Guest verification upsell | Free host cannot enable, Verified+ can enable, per-property flag controls booking eKYC requirement, host-paid verification cost is not charged to guest | Phase One property/booking quote tests |
| Admin/API acceptance | API tests cover Phase 1 and Phase 2 endpoints; expected business failures return non-500 responses; DI and startup validated by test host and smoke run | `HealthEndpointTests`; `PhaseTwoEndpointTests`; live API smoke |
| Security/robustness | No plain-text password storage, invalid credentials/challenges rejected, provider secrets not emitted, malformed business inputs return 400, overlap risk covered by held-date checks | Phase One tests and API tests |

## Bugs Found And Fixed

| Bug/gap | Fix |
|---|---|
| Duplicate registration returned the existing account instead of rejecting. | Registration now rejects duplicate email with a business-rule error. |
| Registration accepted invalid email and weak passwords. | Added email and password validation. |
| 2FA expiry was hard to verify. | Injected `TimeProvider` and added expiry/reuse QA. |
| Property creation API was missing from backend milestone coverage. | Added `POST /api/properties` with listing validation and guest-verification upsell guard. |
| Booking quote allowed overlap as a false availability result instead of rejecting. | Overlapping held/approved dates now reject quote and booking creation. |
| eKYC duplicate webhooks caused final-state errors. | Same-result duplicate webhooks are idempotent and do not duplicate notifications. |
| eKYC conflicting final-state webhooks were not explicitly covered. | Added rejection of approval-after-rejection and rejection-after-approval. |
| Badge system did not enforce eligibility. | Added Verified/Trusted/Wellness eligibility checks. |
| Failed badge payment could not be represented. | Failed payment now creates a suspended, non-unlocking assignment. |
| Feature locking/unlocking was not exposed. | Added feature access evaluation and API route. |
| Expired/suspended badge behavior was not testable. | Added downward-only expire/suspend API routes and tests. |
| Founding benefit could be overwritten across tiers. | Added one-time claim guard and non-eligible rejection. |
| Campaign pricing allowed invalid zero override. | Added badge campaign minimum amount validation. |

## Remaining Out Of Scope

| Item | Reason |
|---|---|
| Frontend booking popup and responsive UI | User explicitly requested backend-only and frontend was not touched. |
| Production Alibaba Cloud credentials/signature exchange | Backend adapter contract and webhook flow are in place; real credentials belong to deployment configuration. |
| Production Stripe live payment confirmation UX | Backend supports Stripe manual-capture PaymentIntent flow and local sandbox mode; frontend confirmation flow comes later. |
| Persistent database-backed Phase One/Two stores | Current milestone implementation uses the repo's in-memory milestone stores and existing EF schema. |
| Authentication/authorization middleware for admin endpoints | Endpoint behavior is backend-complete for milestone QA, but production role enforcement remains a future hardening task. |
| NU1903 package upgrade | Warning reported; package upgrade not performed because the task said report it unless directly requested. |

## Final Statement

Milestone 1 and Milestone 2 are backend-complete under the current repository design. Expanded automated QA passes, live API smoke testing passes, expected business-rule failures return controlled responses, and no frontend files were touched.
