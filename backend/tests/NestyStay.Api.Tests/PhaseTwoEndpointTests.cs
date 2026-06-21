using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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

        var pricebook = await client.GetFromJsonAsync<List<PricebookResponse>>("/api/badges-pricing/pricebook");
        Assert.NotNull(pricebook);
        Assert.Contains(pricebook, item => item.Key == "verified-host-standard-annual");
        Assert.Contains(pricebook, item => item.Key == "trusted-host-pdf-campaign");

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

        var verifiedPurchase = await client.PostAsJsonAsync("/api/badges-pricing/badges/purchase", new
        {
            subjectType = "Host",
            subjectId = hostId,
            level = "Verified",
            hostVerificationPassed = true
        });
        Assert.Equal(HttpStatusCode.OK, verifiedPurchase.StatusCode);
        var verified = await verifiedPurchase.Content.ReadFromJsonAsync<BadgeAssignmentResponse>();
        Assert.NotNull(verified);
        Assert.Equal("Verified", verified.Level);

        var failedTrustedPayment = await client.PostAsJsonAsync("/api/badges-pricing/badges/purchase", new
        {
            subjectType = "Host",
            subjectId = hostId,
            level = "Trusted",
            completedApprovedBookings = 3,
            paymentSucceeded = false
        });
        Assert.Equal(HttpStatusCode.OK, failedTrustedPayment.StatusCode);
        var failedTrusted = await failedTrustedPayment.Content.ReadFromJsonAsync<BadgeAssignmentResponse>();
        Assert.NotNull(failedTrusted);
        Assert.Equal("FAILED", failedTrusted.PaymentStatus);

        var enrollmentResponse = await client.PostAsJsonAsync("/api/badges-pricing/campaigns/trusted-host-pdf-campaign/enroll", new
        {
            subjectType = "Host",
            subjectId = hostId
        });
        Assert.Equal(HttpStatusCode.OK, enrollmentResponse.StatusCode);

        var purchaseResponse = await client.PostAsJsonAsync("/api/badges-pricing/badges/purchase", new
        {
            subjectType = "Host",
            subjectId = hostId,
            level = "Trusted",
            campaignKey = "trusted-host-pdf-campaign",
            completedApprovedBookings = 3
        });
        Assert.Equal(HttpStatusCode.OK, purchaseResponse.StatusCode);
        var assignment = await purchaseResponse.Content.ReadFromJsonAsync<BadgeAssignmentResponse>();
        Assert.NotNull(assignment);
        Assert.Equal("Trusted", assignment.Level);
        Assert.Equal(49m, assignment.AmountCharged);
        Assert.Equal("CAPTURED", assignment.PaymentStatus);

        var trustedFeatures = await client.GetFromJsonAsync<FeatureAccessResponse>($"/api/badges-pricing/badges/features/Host/{hostId}");
        Assert.NotNull(trustedFeatures);
        Assert.Equal("Trusted", trustedFeatures.ActiveLevel);
        Assert.Contains("Trades directory", trustedFeatures.UnlockedFeatures);

        var renewals = await client.GetFromJsonAsync<List<RenewalResponse>>($"/api/badges-pricing/renewals?assignmentId={assignment.Id}");
        Assert.NotNull(renewals);
        Assert.Contains(renewals, item => item.PaymentStatus == "PENDING");

        var renewalPaymentResponse = await client.PostAsync($"/api/badges-pricing/renewals/{assignment.Id}/pay", null);
        Assert.Equal(HttpStatusCode.OK, renewalPaymentResponse.StatusCode);
        var renewed = await renewalPaymentResponse.Content.ReadFromJsonAsync<BadgeAssignmentResponse>();
        Assert.NotNull(renewed);
        Assert.True(renewed.ExpiresAt > assignment.ExpiresAt);

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

    private sealed record PricebookResponse(string Key, decimal Amount);

    private sealed record BadgeDefinitionResponse(string Level, decimal AnnualPrice);

    private sealed record BadgeAssignmentResponse(Guid Id, string Level, decimal AmountCharged, string PaymentStatus, DateTimeOffset ExpiresAt);

    private sealed record RenewalResponse(string PaymentStatus);

    private sealed record FoundingBenefitResponse(string Tier, decimal GuestFlatFee);

    private sealed record CommissionQuoteResponse(decimal HostCommissionAmount, decimal GuestFeeAmount);

    private sealed record TransferEvaluationResponse(bool CanTransfer);

    private sealed record EligibilityResponse(bool Eligible);

    private sealed record FeatureAccessResponse(string ActiveLevel, IReadOnlyList<string> UnlockedFeatures);
}
