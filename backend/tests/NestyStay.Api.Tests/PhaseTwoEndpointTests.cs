using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NestyStay.Domain;

namespace NestyStay.Api.Tests;

public sealed class PhaseTwoEndpointTests : IClassFixture<NestyStayApiFactory>
{
    private readonly NestyStayApiFactory _factory;

    public PhaseTwoEndpointTests(NestyStayApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task BadgesPricingEndpointsSupportPricebookBadgesCampaignsRenewalsAndFoundingBenefits()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.AdminToken);
        var hostId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        await _factory.SeedBadgePaymentFactsAsync(hostId, approvedBookingCount: 3, hasPropertyAddress: true);

        var pricebook = await client.GetFromJsonAsync<List<PricebookResponse>>("/api/badges-pricing/pricebook");
        Assert.NotNull(pricebook);
        Assert.Contains(pricebook, item => item.Key == "verified-host-standard-annual");
        Assert.Contains(pricebook, item => item.Key == "trusted-host-pdf-campaign");
        Assert.Contains(pricebook, item => item.Key == "wellness-subscription-pdf");

        var updatedResponse = await client.PutAsJsonAsync("/api/badges-pricing/pricebook/verified-host-standard-annual", new
        {
            amount = 82,
            currency = "USD",
            cadence = "Annual"
        });
        Assert.Equal(HttpStatusCode.OK, updatedResponse.StatusCode);
        var updated = await updatedResponse.Content.ReadFromJsonAsync<PricebookResponse>();
        Assert.NotNull(updated);
        Assert.Equal(82m, updated.Amount);

        var badges = await client.GetFromJsonAsync<List<BadgeDefinitionResponse>>("/api/badges-pricing/badges");
        Assert.NotNull(badges);
        Assert.Contains(badges, item => item.Level == "Verified" && item.AnnualPrice == 82m);
        Assert.Contains(badges, item => item.Level == "Trusted");

        var invalidPriceResponse = await client.PutAsJsonAsync("/api/badges-pricing/pricebook/verified-host-standard-annual", new
        {
            amount = -1,
            currency = "USD",
            cadence = "Annual"
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidPriceResponse.StatusCode);

        var defaultFeatures = await client.GetFromJsonAsync<FeatureAccessResponse>($"/api/badges-pricing/badges/features/Host/{hostId}");
        Assert.NotNull(defaultFeatures);
        Assert.Equal("Free", defaultFeatures.ActiveLevel);
        Assert.Contains("Listings", defaultFeatures.UnlockedFeatures);
        Assert.DoesNotContain("Guest verification upsell", defaultFeatures.UnlockedFeatures);

        var ineligibleTrusted = await client.PostAsJsonAsync("/api/badges-pricing/badges/eligibility", new
        {
            subjectType = "Host",
            subjectId = hostId,
            level = "Trusted",
            completedApprovedBookings = 3
        });
        Assert.Equal(HttpStatusCode.OK, ineligibleTrusted.StatusCode);
        var ineligible = await ineligibleTrusted.Content.ReadFromJsonAsync<EligibilityResponse>();
        Assert.NotNull(ineligible);
        Assert.False(ineligible.Eligible);

        var verifiedPurchase = await client.PostAsJsonAsync("/api/badges-pricing/badges/purchase-intent", new
        {
            subjectType = "Host",
            subjectId = hostId,
            level = "Verified",
            hostVerificationPassed = true,
            idempotencyKey = "verified-badge-payment"
        });
        Assert.Equal(HttpStatusCode.OK, verifiedPurchase.StatusCode);
        var verified = await verifiedPurchase.Content.ReadFromJsonAsync<BadgePaymentResponse>();
        Assert.NotNull(verified);
        Assert.Equal("Verified", verified.Level);
        Assert.Equal("CAPTURED", verified.Status);

        var duplicateVerifiedPayment = await client.PostAsJsonAsync("/api/badges-pricing/badges/purchase-intent", new
        {
            subjectType = "Host",
            subjectId = hostId,
            level = "Verified",
            hostVerificationPassed = true,
            paymentSucceeded = false,
            idempotencyKey = "verified-badge-payment"
        });
        Assert.Equal(HttpStatusCode.OK, duplicateVerifiedPayment.StatusCode);
        var duplicateVerified = await duplicateVerifiedPayment.Content.ReadFromJsonAsync<BadgePaymentResponse>();
        Assert.NotNull(duplicateVerified);
        Assert.Equal("CAPTURED", duplicateVerified.Status);

        var duplicateWithNewKey = await client.PostAsJsonAsync("/api/badges-pricing/badges/purchase-intent", new
        {
            subjectType = "Host",
            subjectId = hostId,
            level = "Verified",
            hostVerificationPassed = true,
            idempotencyKey = "verified-badge-payment-second-attempt"
        });
        Assert.Equal(HttpStatusCode.BadRequest, duplicateWithNewKey.StatusCode);

        var enrollmentResponse = await client.PostAsJsonAsync("/api/badges-pricing/campaigns/trusted-host-pdf-campaign/enroll", new
        {
            subjectType = "Host",
            subjectId = hostId
        });
        Assert.Equal(HttpStatusCode.OK, enrollmentResponse.StatusCode);

        var purchaseResponse = await client.PostAsJsonAsync("/api/badges-pricing/badges/purchase-intent", new
        {
            subjectType = "Host",
            subjectId = hostId,
            level = "Trusted",
            campaignKey = "trusted-host-pdf-campaign",
            completedApprovedBookings = 3
        });
        Assert.Equal(HttpStatusCode.OK, purchaseResponse.StatusCode);
        var trustedPayment = await purchaseResponse.Content.ReadFromJsonAsync<BadgePaymentResponse>();
        Assert.NotNull(trustedPayment);
        Assert.Equal("Trusted", trustedPayment.Level);
        Assert.Equal(49m, trustedPayment.Amount);
        Assert.Equal("CAPTURED", trustedPayment.Status);
        Assert.NotNull(trustedPayment.AssignmentId);
        var hostAssignments = await client.GetFromJsonAsync<List<BadgeAssignmentResponse>>($"/api/badges-pricing/badges/assignments?subjectType=Host&subjectId={hostId}");
        Assert.NotNull(hostAssignments);
        var assignment = Assert.Single(hostAssignments, item => item.Level == "Trusted");

        var trustedFeatures = await client.GetFromJsonAsync<FeatureAccessResponse>($"/api/badges-pricing/badges/features/Host/{hostId}");
        Assert.NotNull(trustedFeatures);
        Assert.Equal("Trusted", trustedFeatures.ActiveLevel);
        Assert.Contains("Trades directory", trustedFeatures.UnlockedFeatures);

        var renewals = await client.GetFromJsonAsync<List<RenewalResponse>>($"/api/badges-pricing/renewals?assignmentId={assignment.Id}");
        Assert.NotNull(renewals);
        Assert.Contains(renewals, item => item.PaymentStatus == "PENDING");

        var renewalPaymentResponse = await client.PostAsync($"/api/badges-pricing/renewals/{assignment.Id}/pay", null);
        Assert.Equal(HttpStatusCode.OK, renewalPaymentResponse.StatusCode);
        var renewedPayment = await renewalPaymentResponse.Content.ReadFromJsonAsync<BadgePaymentResponse>();
        Assert.NotNull(renewedPayment);
        Assert.Equal("CAPTURED", renewedPayment.Status);
        var renewedAssignments = await client.GetFromJsonAsync<List<BadgeAssignmentResponse>>($"/api/badges-pricing/badges/assignments?subjectType=Host&subjectId={hostId}");
        Assert.NotNull(renewedAssignments);
        Assert.True(renewedAssignments.Single(item => item.Level == "Trusted").ExpiresAt > assignment.ExpiresAt);

        var expireResponse = await client.PostAsync($"/api/badges-pricing/badges/assignments/{assignment.Id}/expire", null);
        Assert.Equal(HttpStatusCode.OK, expireResponse.StatusCode);
        var afterExpireFeatures = await client.GetFromJsonAsync<FeatureAccessResponse>($"/api/badges-pricing/badges/features/Host/{hostId}");
        Assert.NotNull(afterExpireFeatures);
        Assert.Equal("Verified", afterExpireFeatures.ActiveLevel);

        var badCampaignResponse = await client.PostAsJsonAsync("/api/badges-pricing/campaigns", new
        {
            key = $"bad-{Guid.NewGuid():N}",
            name = "Bad campaign",
            campaignType = "BadgePriceOverride",
            overrideAmount = 0,
            appliesTo = "Hosts"
        });
        Assert.Equal(HttpStatusCode.BadRequest, badCampaignResponse.StatusCode);

        var benefitResponse = await client.PostAsJsonAsync("/api/badges-pricing/founding-benefits", new
        {
            propertyId,
            tier = "Gold"
        });
        Assert.Equal(HttpStatusCode.OK, benefitResponse.StatusCode);
        var benefit = await benefitResponse.Content.ReadFromJsonAsync<FoundingBenefitResponse>();
        Assert.NotNull(benefit);
        Assert.Equal("Gold", benefit.Tier);
        Assert.Equal(36m, benefit.GuestFlatFee);

        var duplicateBenefitResponse = await client.PostAsJsonAsync("/api/badges-pricing/founding-benefits", new
        {
            propertyId,
            tier = "Silver"
        });
        Assert.Equal(HttpStatusCode.BadRequest, duplicateBenefitResponse.StatusCode);

        var quoteResponse = await client.PostAsJsonAsync("/api/badges-pricing/commission-quote", new
        {
            bookingValue = 1000,
            nights = 3,
            tier = "Gold"
        });
        Assert.Equal(HttpStatusCode.OK, quoteResponse.StatusCode);
        var quote = await quoteResponse.Content.ReadFromJsonAsync<CommissionQuoteResponse>();
        Assert.NotNull(quote);
        Assert.Equal(30m, quote.HostCommissionAmount);
        Assert.Equal(36m, quote.GuestFeeAmount);

        var transferResponse = await client.PostAsJsonAsync("/api/badges-pricing/founding-benefits/transfer-evaluation", new
        {
            previousOwnerVerified = true,
            previousOwnerTrusted = true,
            hasPropertyId = true,
            hasCurrentTaxReceipt = true
        });
        Assert.Equal(HttpStatusCode.OK, transferResponse.StatusCode);
        var transfer = await transferResponse.Content.ReadFromJsonAsync<TransferEvaluationResponse>();
        Assert.NotNull(transfer);
        Assert.True(transfer.CanTransfer);

        var campaignResponse = await client.PostAsJsonAsync("/api/badges-pricing/campaigns", new
        {
            key = $"audit-campaign-{Guid.NewGuid():N}",
            name = "Audit campaign",
            campaignType = "BadgePriceOverride",
            overrideAmount = 25,
            appliesTo = "Hosts"
        });
        Assert.Equal(HttpStatusCode.OK, campaignResponse.StatusCode);

        var audit = await client.GetFromJsonAsync<List<AuditEventResponse>>("/api/spec/admin/audit-log");
        Assert.NotNull(audit);
        var pricebookAudit = Assert.Single(audit, item =>
            item.Action == "PricebookItemUpdated" &&
            item.NewStateJson?.Contains("\"amount\":82", StringComparison.Ordinal) == true);
        Assert.Equal("Admin", pricebookAudit.ActorRole);
        Assert.Equal("system_configuration", pricebookAudit.EffectivePermission);
        Assert.False(string.IsNullOrWhiteSpace(pricebookAudit.CorrelationId));
        Assert.NotNull(pricebookAudit.NewStateJson);
        Assert.Contains(audit, item => item.Action == "CampaignCreated" && item.EffectivePermission == "system_configuration");
        Assert.Contains(audit, item => item.Action == "FoundingBenefitUpserted" && item.EffectivePermission == "system_configuration");
    }

    [Fact]
    public async Task AdminMutationEndpointsRequireAdminBearerToken()
    {
        using var client = _factory.CreateClient();

        var publicResponse = await client.GetAsync("/api/badges-pricing/pricebook");
        Assert.Equal(HttpStatusCode.OK, publicResponse.StatusCode);

        var unauthenticated = await client.PutAsJsonAsync("/api/badges-pricing/pricebook/verified-host-standard-annual", new
        {
            amount = 70,
            currency = "USD",
            cadence = "Annual"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.OperatorToken);
        var nonAdmin = await client.PutAsJsonAsync("/api/badges-pricing/pricebook/verified-host-standard-annual", new
        {
            amount = 70,
            currency = "USD",
            cadence = "Annual"
        });
        Assert.Equal(HttpStatusCode.Forbidden, nonAdmin.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.AdminToken);
        var admin = await client.PutAsJsonAsync("/api/badges-pricing/pricebook/verified-host-standard-annual", new
        {
            amount = 70,
            currency = "USD",
            cadence = "Annual"
        });
        Assert.Equal(HttpStatusCode.OK, admin.StatusCode);
    }

    [Fact]
    public async Task BadgePaymentRefundWebhookSuspendsBadgeAndReplayIsIdempotent()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", NestyStayApiFactory.AdminToken);
        var hostId = Guid.NewGuid();
        await _factory.SeedBadgePaymentFactsAsync(hostId, approvedBookingCount: 3, hasPropertyAddress: true);

        var verifiedResponse = await client.PostAsJsonAsync("/api/badges-pricing/badges/purchase-intent", new
        {
            subjectType = "Host",
            subjectId = hostId,
            level = "Verified",
            idempotencyKey = $"refund-test-verified-{hostId:N}"
        });
        Assert.Equal(HttpStatusCode.OK, verifiedResponse.StatusCode);

        var trustedResponse = await client.PostAsJsonAsync("/api/badges-pricing/badges/purchase-intent", new
        {
            subjectType = "Host",
            subjectId = hostId,
            level = "Trusted",
            idempotencyKey = $"refund-test-trusted-{hostId:N}"
        });
        Assert.Equal(HttpStatusCode.OK, trustedResponse.StatusCode);
        var trustedPayment = await trustedResponse.Content.ReadFromJsonAsync<BadgePaymentResponse>();
        Assert.NotNull(trustedPayment);
        Assert.Equal("CAPTURED", trustedPayment.Status);
        Assert.False(string.IsNullOrWhiteSpace(trustedPayment.ProviderPaymentIntentId));

        var eventId = $"evt_badge_refund_{hostId:N}";
        var payload = $$"""
            {
              "id": "{{eventId}}",
              "type": "charge.refunded",
              "data": {
                "object": {
                  "id": "ch_badge_refund_{{hostId:N}}",
                  "payment_intent": "{{trustedPayment.ProviderPaymentIntentId}}",
                  "amount_refunded": 4900,
                  "currency": "usd"
                }
              }
            }
            """;

        var refundResponse = await client.PostAsync(
            "/api/webhooks/stripe/raw",
            new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Accepted, refundResponse.StatusCode);
        using var refundJson = JsonDocument.Parse(await refundResponse.Content.ReadAsStringAsync());
        Assert.Equal("REFUNDED", refundJson.RootElement.GetProperty("badgePaymentStatus").GetString());

        var replayResponse = await client.PostAsync(
            "/api/webhooks/stripe/raw",
            new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Accepted, replayResponse.StatusCode);
        using var replayJson = JsonDocument.Parse(await replayResponse.Content.ReadAsStringAsync());
        Assert.True(replayJson.RootElement.GetProperty("duplicate").GetBoolean());

        var assignments = await client.GetFromJsonAsync<List<BadgeAssignmentResponse>>($"/api/badges-pricing/badges/assignments?subjectType=Host&subjectId={hostId}");
        Assert.NotNull(assignments);
        var trustedAssignment = Assert.Single(assignments, item => item.Level == "Trusted");
        Assert.Equal("Suspended", trustedAssignment.Status);
        Assert.Equal("REFUNDED", trustedAssignment.PaymentStatus);
    }

    [Fact]
    public async Task AllAdminOnlyMutationEndpointsRejectMissingBearerToken()
    {
        using var client = _factory.CreateClient();
        var assignmentId = Guid.NewGuid();

        var requests = new[]
        {
            new HttpRequestMessage(HttpMethod.Put, "/api/badges-pricing/pricebook/verified-host-standard-annual")
            {
                Content = JsonContent.Create(new { amount = 70, currency = "USD", cadence = "Annual" })
            },
            new HttpRequestMessage(HttpMethod.Post, "/api/badges-pricing/badges/eligibility")
            {
                Content = JsonContent.Create(new { subjectType = "Host", subjectId = Guid.NewGuid(), level = "Verified" })
            },
            new HttpRequestMessage(HttpMethod.Post, "/api/badges-pricing/badges/purchase")
            {
                Content = JsonContent.Create(new { subjectType = "Host", subjectId = Guid.NewGuid(), level = "Verified", hostVerificationPassed = true })
            },
            new HttpRequestMessage(HttpMethod.Post, $"/api/badges-pricing/renewals/{assignmentId}/pay"),
            new HttpRequestMessage(HttpMethod.Post, $"/api/badges-pricing/badges/assignments/{assignmentId}/expire"),
            new HttpRequestMessage(HttpMethod.Post, $"/api/badges-pricing/badges/assignments/{assignmentId}/suspend"),
            new HttpRequestMessage(HttpMethod.Post, "/api/badges-pricing/campaigns")
            {
                Content = JsonContent.Create(new
                {
                    key = $"admin-required-{Guid.NewGuid():N}",
                    name = "Admin required",
                    campaignType = "BadgePriceOverride",
                    overrideAmount = 19,
                    appliesTo = "Hosts"
                })
            },
            new HttpRequestMessage(HttpMethod.Post, "/api/badges-pricing/founding-benefits")
            {
                Content = JsonContent.Create(new { propertyId = Guid.NewGuid(), tier = "Silver" })
            }
        };

        foreach (var request in requests)
        {
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    private sealed record PricebookResponse(string Key, decimal Amount);

    private sealed record BadgeDefinitionResponse(string Level, decimal AnnualPrice);

    private sealed record BadgeAssignmentResponse(Guid Id, string Level, string Status, decimal AmountCharged, string PaymentStatus, DateTimeOffset ExpiresAt);

    private sealed record BadgePaymentResponse(Guid Id, string Level, decimal Amount, string ProviderPaymentIntentId, string Status, Guid? AssignmentId);

    private sealed record RenewalResponse(string PaymentStatus);

    private sealed record FoundingBenefitResponse(string Tier, decimal GuestFlatFee);

    private sealed record CommissionQuoteResponse(decimal HostCommissionAmount, decimal GuestFeeAmount);

    private sealed record TransferEvaluationResponse(bool CanTransfer);

    private sealed record EligibilityResponse(bool Eligible);

    private sealed record FeatureAccessResponse(string ActiveLevel, IReadOnlyList<string> UnlockedFeatures);

    private sealed record AuditEventResponse(
        string Action,
        string ActorRole,
        string? EffectivePermission,
        string? CorrelationId,
        string? NewStateJson);
}
