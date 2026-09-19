using NestyStay.Domain;
using NestyStay.Domain.Common;

namespace NestyStay.Domain.Tests;

public sealed class BusinessRulesTests
{
    [Fact]
    public void PaymentCaptureIsOnlyAllowedAfterApproval()
    {
        Assert.False(NestyStayBusinessRules.CanCapturePayment(BookingStatus.PendingVerification));
        Assert.True(NestyStayBusinessRules.CanCapturePayment(BookingStatus.Approved));
    }

    [Fact]
    public void FoundingTransferRequiresGoodStandingAndReceipt()
    {
        Assert.True(NestyStayBusinessRules.CanTransferFoundingBenefit(
            previousOwnerVerified: true,
            previousOwnerTrusted: true,
            hasPropertyId: true,
            hasCurrentTaxReceipt: true));

        Assert.False(NestyStayBusinessRules.CanTransferFoundingBenefit(
            previousOwnerVerified: true,
            previousOwnerTrusted: false,
            hasPropertyId: true,
            hasCurrentTaxReceipt: true));
    }

    [Fact]
    public void BusinessRatingPolicyMatchesMasterSpec()
    {
        Assert.Equal(BusinessStanding.TopRated, NestyStayBusinessRules.ResolveBusinessStanding(4.8m, 5));
        Assert.Equal(BusinessStanding.Warning, NestyStayBusinessRules.ResolveBusinessStanding(3.7m, 5));
        Assert.Equal(BusinessStanding.FinalWarning, NestyStayBusinessRules.ResolveBusinessStanding(3.2m, 5));
        Assert.Equal(BusinessStanding.Removed, NestyStayBusinessRules.ResolveBusinessStanding(2.9m, 5));
    }

    [Fact]
    public void OfficerPrivacyAndEligibilityRulesAreHardCoded()
    {
        Assert.True(NestyStayBusinessRules.IsOfficerEligible(isActiveJcf: true, isRetired: false));
        Assert.False(NestyStayBusinessRules.IsOfficerEligible(isActiveJcf: true, isRetired: true));
        Assert.Equal("Private security / body guard", NestyStayBusinessRules.GuestFacingWellnessLabel());
        Assert.Equal("Off-duty police officer", NestyStayBusinessRules.OwnerFacingWellnessLabel());
    }

    [Fact]
    public void ManagerCannotViewUnitRentalIncome()
    {
        Assert.False(NestyStayBusinessRules.CanManagerViewUnitRentalIncome());
    }
}
