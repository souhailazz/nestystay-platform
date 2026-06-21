using NestyStay.Domain.Common;

namespace NestyStay.Domain.Pricing;

public sealed class PricebookEntry : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CurrencyOrUnit { get; set; } = "USD";
    public string Cadence { get; set; } = string.Empty;
    public string AppliesTo { get; set; } = string.Empty;
    public DateTimeOffset? ActiveFrom { get; set; }
    public DateTimeOffset? ActiveTo { get; set; }
    public bool IsConfigurable { get; set; } = true;
}

public sealed class Campaign : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CampaignType { get; set; } = string.Empty;
    public DateTimeOffset? OpensAt { get; set; }
    public DateTimeOffset? ClosesAt { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CampaignEnrollment : BaseEntity
{
    public Guid CampaignId { get; set; }
    public string SubjectType { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public DateTimeOffset EnrolledAt { get; set; } = DateTimeOffset.UtcNow;
}
