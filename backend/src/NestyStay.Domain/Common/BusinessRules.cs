namespace NestyStay.Domain.Common;

public static class NestyStayBusinessRules
{
    public const int DefaultBookingHoldMinutes = 60;
    public const int LegacyPdfBookingHoldMinutes = 30;
    public const int MessageRetentionDays = 90;
    public const int WellnessEscrowAutoReleaseHours = 48;
    public const int WellnessBadgeValidityYears = 1;
    public const int AssociationMinimumRetentionYears = 7;
    public const string JamaicaEmergencyNumber = "119";

    public static bool CanCapturePayment(BookingStatus status) =>
        status is BookingStatus.Approved or BookingStatus.PaymentCaptured or BookingStatus.Confirmed;

    public static BookingStatus ResolveVerificationResult(bool passed) =>
        passed ? BookingStatus.Approved : BookingStatus.Rejected;

    public static bool ShouldReleaseEntryDetails(BookingStatus status, decimal totalAmount, decimal paidAmount) =>
        status is (BookingStatus.PaymentCaptured or BookingStatus.Confirmed) && paidAmount >= totalAmount;

    public static PaymentScheduleType ResolveScheduleType(DateOnly checkIn, DateOnly today, PaymentScheduleType requested)
    {
        var daysUntilCheckIn = checkIn.DayNumber - today.DayNumber;
        if (daysUntilCheckIn < 7)
        {
            return PaymentScheduleType.FullAtApproval;
        }

        if (requested == PaymentScheduleType.SplitSevenDaysBefore && daysUntilCheckIn < 14)
        {
            return PaymentScheduleType.SplitFortyEightHoursBefore;
        }

        return requested;
    }

    public static decimal ResolveStandardGuestFeePercent(decimal bookingValue, int nights)
    {
        if (nights >= 7 || bookingValue >= 1500m)
        {
            return 8m;
        }

        if (nights == 1)
        {
            return 12m;
        }

        return 10m;
    }

    public static decimal ResolveFoundingGuestFlatFee(FoundingTier tier) =>
        tier switch
        {
            FoundingTier.Platinum => 29m,
            FoundingTier.Gold => 36m,
            FoundingTier.Silver => 45m,
            _ => 0m
        };

    public static bool CanTransferFoundingBenefit(bool previousOwnerVerified, bool previousOwnerTrusted, bool hasPropertyId, bool hasCurrentTaxReceipt) =>
        previousOwnerVerified && previousOwnerTrusted && hasPropertyId && hasCurrentTaxReceipt;

    public static IReadOnlyList<string> VerificationOptOutLosses() =>
    [
        "Reviews",
        "Badges",
        "Directories",
        "Search boost",
        "Police wellness",
        "Guest platform access"
    ];

    public static bool IsTrustedEligible(bool isVerified, int verifiedBookingsOrReviews) =>
        isVerified && verifiedBookingsOrReviews >= 3;

    public static bool IsWellnessEligible(bool isVerified, bool hasPropertyAddress, bool hasCompletedWellnessVisit) =>
        isVerified && hasPropertyAddress && hasCompletedWellnessVisit;

    public static bool IsSponsorRequired(decimal averageRating, int verifiedReviewCount) =>
        verifiedReviewCount < 5 || averageRating < 4.7m;

    public static BusinessStanding ResolveBusinessStanding(decimal averageRating, int verifiedReviewCount)
    {
        if (verifiedReviewCount < 5 || averageRating >= 4.0m)
        {
            return averageRating >= 4.7m ? BusinessStanding.TopRated : BusinessStanding.GoodStanding;
        }

        if (averageRating >= 3.5m)
        {
            return BusinessStanding.Warning;
        }

        if (averageRating >= 3.0m)
        {
            return BusinessStanding.FinalWarning;
        }

        return BusinessStanding.Removed;
    }

    public static bool IsOfficerEligible(bool isActiveJcf, bool isRetired) =>
        isActiveJcf && !isRetired;

    public static string GuestFacingWellnessLabel() => "Private security / body guard";

    public static string OwnerFacingWellnessLabel() => "Off-duty police officer";

    public static DateTimeOffset NextOfficerIdResetAfter(DateTimeOffset now) =>
        new(now.Year + 1, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static bool CanManagerViewUnitRentalIncome() => false;

    public static decimal ResolveManagementPlatformFeePercent(GovernanceMode mode, decimal licensedManagerPercent = 1.5m) =>
        mode == GovernanceMode.LicensedManager ? licensedManagerPercent : 4m;

    public static bool HasFullZoomArchiveAccess(int monthsInArrears) =>
        monthsInArrears < 3;

    public static bool IsProxyEligible(bool ownerCurrentWithMaintenanceFees) =>
        ownerCurrentWithMaintenanceFees;
}
