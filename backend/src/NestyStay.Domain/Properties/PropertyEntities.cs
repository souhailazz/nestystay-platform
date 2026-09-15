using NestyStay.Domain.Common;

namespace NestyStay.Domain.Properties;

public sealed class Property : BaseEntity
{
    public Guid HostUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? Parish { get; set; }
    public string Country { get; set; } = "Jamaica";
    public PropertyStatus Status { get; set; } = PropertyStatus.Draft;
    public BadgeLevel HighestBadge { get; set; } = BadgeLevel.Free;
    public bool IsVerificationOptedOut { get; set; }
    public bool IsGuestVerificationEnabled { get; set; }
    public bool IsInsuraGuestEnabled { get; set; }
    public CancellationPolicyType CancellationPolicy { get; set; } = CancellationPolicyType.Flexible;
    public string? CustomCancellationTerms { get; set; }
}

public sealed class PropertyUnit : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Guid? CommunityId { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public int Bedrooms { get; set; }
    public int Bathrooms { get; set; }
}

public sealed class PropertyMedia : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Guid StorageObjectId { get; set; }
    public string MediaType { get; set; } = "Photo";
    public int SortOrder { get; set; }
}

public sealed class PropertyAvailability : BaseEntity
{
    public Guid PropertyId { get; set; }
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public string AvailabilityType { get; set; } = "Available";
    public Guid? BookingId { get; set; }
    public DateTimeOffset? HoldExpiresAt { get; set; }
}

public sealed class PropertyPricingRule : BaseEntity
{
    public Guid PropertyId { get; set; }
    public decimal NightlyRate { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal SevenNightDiscountPercent { get; set; }
    public decimal FourteenNightDiscountPercent { get; set; }
    public decimal TwentyEightNightDiscountPercent { get; set; }
    public string? MarketOverrideKey { get; set; }
}

public sealed class PropertyFoundingBenefit : BaseEntity
{
    public Guid PropertyId { get; set; }
    public FoundingTier Tier { get; set; } = FoundingTier.Standard;
    public decimal GuestFlatFee { get; set; }
    public decimal HostCommissionPercent { get; set; } = 3m;
    public bool IsLifetimeGuestFee { get; set; }
    public bool IsTransferableWithProperty { get; set; }
    public bool IsForfeited { get; set; }
}

public sealed class PropertyTransferRequest : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Guid PreviousOwnerUserId { get; set; }
    public Guid NewOwnerUserId { get; set; }
    public Guid TaxReceiptStorageObjectId { get; set; }
    public bool PreviousOwnerVerifiedAndTrusted { get; set; }
    public string Status { get; set; } = "Pending";
    public string? AdminNotes { get; set; }
}
