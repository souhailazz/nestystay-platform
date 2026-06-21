using NestyStay.Application.Services;
using NestyStay.Domain;

namespace NestyStay.Application.Tests;

public sealed class BlueprintTests
{
    [Fact]
    public void BlueprintIncludesEveryPlannedPortal()
    {
        var service = new PlatformBlueprintService();

        var roles = service.GetPortals().Select(portal => portal.Role).ToHashSet();

        Assert.Equal(Enum.GetValues<UserRole>().Length, roles.Count);
        Assert.Contains(UserRole.GateGuard, roles);
        Assert.Contains(UserRole.Officer, roles);
    }

    [Fact]
    public void BookingFlowKeepsPaymentAfterApproval()
    {
        var service = new BookingWorkflowService();

        var flow = service.GetPendingVerificationFlow();
        var approval = flow.Single(step => step.Status == BookingStatus.Approved);
        var payment = flow.Single(step => step.Status == BookingStatus.PaymentCaptured);

        Assert.True(approval.Order < payment.Order);
    }

    [Fact]
    public void PricebookKeepsConflictingSpecPricingConfigurable()
    {
        var service = new PricebookService();

        Assert.All(service.GetDefaultPricebook(), item => Assert.True(item.IsConfigurable));
    }
}
