using NestyStay.Application.PhaseTwo;
using NestyStay.Application.Services;
using NestyStay.Domain;

namespace NestyStay.Application.Tests;

public sealed class PhaseTwoWorkflowTests
{
    [Fact]
    public void PricebookContainsExpectedKeysAndRejectsInvalidUpdates()
    {
        var store = CreateStore();

        Assert.Contains(store.GetPricebook(), item => item.Key == "host-listing" && item.Amount == 0m);
        Assert.Contains(store.GetPricebook(), item => item.Key == "verified-host-standard-annual" && item.Amount == 60m);
        Assert.Contains(store.GetPricebook(), item => item.Key == "trusted-host-pdf-campaign" && item.Amount == 49m);
        Assert.Contains(store.GetPricebook(), item => item.Key == "founding-platinum-guest-flat" && item.Amount == 29m);

        var updated = store.UpdatePricebookItem("verified-host-standard-annual", new UpdatePricebookItemRequest(75.129m, "usd", "Annual"));
        Assert.Equal(75.13m, updated.Amount);
        Assert.Equal("USD", updated.Currency);
        Assert.Equal(75.13m, store.GetBadgeDefinitions().Single(item => item.Level == BadgeLevel.Verified).AnnualPrice);

        Assert.Null(store.GetPricebookItem("missing-key"));
        Assert.Throws<InvalidOperationException>(() => store.UpdatePricebookItem("verified-host-standard-annual", new UpdatePricebookItemRequest(-1m)));
        Assert.Throws<InvalidOperationException>(() => store.UpdatePricebookItem("missing-key", new UpdatePricebookItemRequest(10m)));
    }

    [Fact]
    public void HostsDefaultToFreeFeaturesAndEligibilityControlsBadgeProgression()
    {
        var store = CreateStore();
        var hostId = Guid.NewGuid();

        var initialAccess = store.GetFeatureAccess("Host", hostId);
        Assert.Equal(BadgeLevel.Free, initialAccess.ActiveLevel);
        Assert.Contains("Listings", initialAccess.UnlockedFeatures);
        Assert.DoesNotContain("Guest verification upsell", initialAccess.UnlockedFeatures);

        var verifiedEligibility = store.GetBadgeEligibility(new PurchaseBadgeRequest("Host", hostId, BadgeLevel.Verified));
        Assert.False(verifiedEligibility.Eligible);
        Assert.Contains(verifiedEligibility.MissingRequirements, item => item.Contains("eKYC", StringComparison.OrdinalIgnoreCase));

        var verified = store.PurchaseBadge(new PurchaseBadgeRequest("Host", hostId, BadgeLevel.Verified, HostVerificationPassed: true));
        Assert.Equal(BadgeLevel.Verified, verified.Level);
        Assert.Equal("CAPTURED", verified.PaymentStatus);

        var access = store.GetFeatureAccess("Host", hostId);
        Assert.Equal(BadgeLevel.Verified, access.ActiveLevel);
        Assert.Contains("Guest verification upsell", access.UnlockedFeatures);
        Assert.DoesNotContain("Trades directory", access.UnlockedFeatures);

        Assert.Throws<InvalidOperationException>(() =>
            store.PurchaseBadge(new PurchaseBadgeRequest("Host", hostId, BadgeLevel.Trusted, CompletedApprovedBookings: 2)));

        var wellnessEligibility = store.GetBadgeEligibility(new PurchaseBadgeRequest("Host", hostId, BadgeLevel.Wellness, HasPropertyAddress: true));
        Assert.False(wellnessEligibility.Eligible);
        Assert.Contains(wellnessEligibility.MissingRequirements, item => item.Contains("wellness subscription", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BadgePaymentsDuplicatesRenewalsExpiryAndSuspensionKeepFeatureAccessConsistent()
    {
        var store = CreateStore();
        var hostId = Guid.NewGuid();
        var verified = store.PurchaseBadge(new PurchaseBadgeRequest("Host", hostId, BadgeLevel.Verified, HostVerificationPassed: true));

        var failedTrusted = store.PurchaseBadge(new PurchaseBadgeRequest(
            "Host",
            hostId,
            BadgeLevel.Trusted,
            CompletedApprovedBookings: 3,
            PaymentSucceeded: false));

        Assert.Equal("FAILED", failedTrusted.PaymentStatus);
        Assert.Equal("Suspended", failedTrusted.Status);
        Assert.Equal(BadgeLevel.Verified, store.GetFeatureAccess("Host", hostId).ActiveLevel);

        var enrollment = store.EnrollCampaign("trusted-host-pdf-campaign", new EnrollCampaignRequest("Host", hostId));
        var trusted = store.PurchaseBadge(new PurchaseBadgeRequest(
            "Host",
            hostId,
            BadgeLevel.Trusted,
            "trusted-host-pdf-campaign",
            CompletedApprovedBookings: 3));
        var duplicateTrusted = store.PurchaseBadge(new PurchaseBadgeRequest(
            "Host",
            hostId,
            BadgeLevel.Trusted,
            "trusted-host-pdf-campaign",
            CompletedApprovedBookings: 3));

        Assert.Equal(hostId, enrollment.SubjectId);
        Assert.Equal(trusted.Id, duplicateTrusted.Id);
        Assert.Equal(49m, trusted.AmountCharged);
        Assert.Equal(BadgeLevel.Trusted, store.GetFeatureAccess("Host", hostId).ActiveLevel);
        Assert.Contains("Trades directory", store.GetFeatureAccess("Host", hostId).UnlockedFeatures);

        var renewal = store.GetRenewals(trusted.Id).Single(item => item.PaymentStatus == "PENDING");
        var renewed = store.PayRenewal(trusted.Id);

        Assert.True(renewal.ReminderDueAt < trusted.ExpiresAt);
        Assert.True(renewed.ExpiresAt > trusted.ExpiresAt);
        Assert.Contains(store.GetRenewals(trusted.Id), item => item.PaymentStatus == "CAPTURED");
        Assert.Contains(store.GetRenewals(trusted.Id), item => item.PaymentStatus == "PENDING");

        var expired = store.ExpireBadge(trusted.Id);
        Assert.Equal("Expired", expired.Status);
        Assert.Equal(BadgeLevel.Verified, store.GetFeatureAccess("Host", hostId).ActiveLevel);

        var suspendedVerified = store.SuspendBadge(verified.Id);
        Assert.Equal("Suspended", suspendedVerified.Status);
        Assert.Equal(BadgeLevel.Free, store.GetFeatureAccess("Host", hostId).ActiveLevel);
    }

    [Fact]
    public void CampaignPricingRequiresEligibilityAndValidActiveCampaigns()
    {
        var store = CreateStore();
        var hostId = Guid.NewGuid();

        store.PurchaseBadge(new PurchaseBadgeRequest("Host", hostId, BadgeLevel.Verified, HostVerificationPassed: true));

        Assert.Throws<InvalidOperationException>(() =>
            store.PurchaseBadge(new PurchaseBadgeRequest("Host", hostId, BadgeLevel.Trusted, "missing-campaign", CompletedApprovedBookings: 3)));
        Assert.Throws<InvalidOperationException>(() =>
            store.CreateCampaign(new CreateCampaignRequest("bad-free-campaign", "Bad", "BadgePriceOverride", 0m, "Hosts")));

        var expiredCampaign = store.CreateCampaign(new CreateCampaignRequest(
            "expired-trusted",
            "Expired trusted",
            "BadgePriceOverride",
            30m,
            "Hosts",
            DateTimeOffset.UtcNow.AddDays(-10),
            DateTimeOffset.UtcNow.AddDays(-1)));

        Assert.False(expiredCampaign.IsActive && expiredCampaign.ClosesAt > DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(() =>
            store.EnrollCampaign("expired-trusted", new EnrollCampaignRequest("Host", hostId)));

        store.EnrollCampaign("trusted-host-pdf-campaign", new EnrollCampaignRequest("Host", hostId));
        var trusted = store.PurchaseBadge(new PurchaseBadgeRequest("Host", hostId, BadgeLevel.Trusted, "trusted-host-pdf-campaign", CompletedApprovedBookings: 3));

        Assert.Equal(49m, trusted.AmountCharged);
    }

    [Fact]
    public void FoundingBenefitsResolveDiscountsTransfersAndPreventInvalidClaims()
    {
        var store = CreateStore();
        var propertyId = Guid.NewGuid();

        Assert.Throws<InvalidOperationException>(() =>
            store.UpsertFoundingBenefit(new FoundingBenefitRequest(propertyId, FoundingTier.Gold, IsEligible: false)));

        var benefit = store.UpsertFoundingBenefit(new FoundingBenefitRequest(propertyId, FoundingTier.Platinum));
        var sameClaim = store.UpsertFoundingBenefit(new FoundingBenefitRequest(propertyId, FoundingTier.Platinum));

        Assert.Equal(29m, benefit.GuestFlatFee);
        Assert.Equal(benefit.GuestFlatFee, sameClaim.GuestFlatFee);
        Assert.True(benefit.IsLifetimeGuestFee);
        Assert.True(benefit.IsTransferableWithProperty);
        Assert.Throws<InvalidOperationException>(() =>
            store.UpsertFoundingBenefit(new FoundingBenefitRequest(propertyId, FoundingTier.Gold)));

        var transfer = store.EvaluateFoundingTransfer(new FoundingTransferEvaluationRequest(true, true, true, true));
        var failedTransfer = store.EvaluateFoundingTransfer(new FoundingTransferEvaluationRequest(true, false, true, false));

        Assert.True(transfer.CanTransfer);
        Assert.False(failedTransfer.CanTransfer);
        Assert.Contains(failedTransfer.MissingRequirements, item => item.Contains("trusted", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(failedTransfer.MissingRequirements, item => item.Contains("tax receipt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CommissionQuotesCoverStandardFoundingAndEdgeCases()
    {
        var store = CreateStore();

        var standard = store.QuoteCommission(new CommissionQuoteRequest(1_000m, 3));
        var platinum = store.QuoteCommission(new CommissionQuoteRequest(1_000m, 4, FoundingTier.Platinum));
        var zero = store.QuoteCommission(new CommissionQuoteRequest(0m, 1));
        var huge = store.QuoteCommission(new CommissionQuoteRequest(1_000_000.55m, 14));

        Assert.Equal(3m, standard.HostCommissionPercent);
        Assert.Equal(30m, standard.HostCommissionAmount);
        Assert.Equal(100m, standard.GuestFeeAmount);
        Assert.Equal(29m, platinum.GuestFeeAmount);
        Assert.Equal(0m, zero.HostCommissionAmount);
        Assert.Equal(0m, zero.GuestFeeAmount);
        Assert.Equal(30_000.02m, huge.HostCommissionAmount);

        Assert.Throws<InvalidOperationException>(() => store.QuoteCommission(new CommissionQuoteRequest(-1m, 1)));
        Assert.Throws<InvalidOperationException>(() => store.QuoteCommission(new CommissionQuoteRequest(100m, 0)));
    }

    private static PhaseTwoStore CreateStore() => new(new PricebookService(), TimeProvider.System);
}
