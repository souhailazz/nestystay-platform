using NestyStay.Domain.Common;

namespace NestyStay.Domain.Directories;

public sealed class ServiceProviderProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public DirectoryProviderType ProviderType { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public decimal AverageRating { get; set; }
    public int VerifiedReviewCount { get; set; }
    public string Status { get; set; } = "Draft";
}

public sealed class ServiceProviderSponsorship : BaseEntity
{
    public Guid ServiceProviderProfileId { get; set; }
    public Guid SponsorHostUserId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public DateTimeOffset? WithdrawnAt { get; set; }
    public DateTimeOffset? ReplacementDueAt { get; set; }
}

public sealed class ServiceJob : BaseEntity
{
    public Guid ServiceProviderProfileId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public string Status { get; set; } = "Requested";
    public decimal? QuoteAmount { get; set; }
    public string Currency { get; set; } = "USD";
}

public sealed class LocalBusiness : BaseEntity
{
    public Guid UserId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public bool IsBrickAndMortar { get; set; }
    public bool HasLegalDocuments { get; set; }
    public decimal AverageRating { get; set; }
    public int VerifiedReviewCount { get; set; }
    public BusinessStanding Standing { get; set; } = BusinessStanding.GoodStanding;
}

public sealed class DirectoryReview : BaseEntity
{
    public string SubjectType { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public Guid ReviewerUserId { get; set; }
    public decimal Rating { get; set; }
    public bool IsVerified { get; set; }
    public string? Comment { get; set; }
}

public sealed class DirectoryCommission : BaseEntity
{
    public string SubjectType { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public decimal CommissionPercent { get; set; }
    public DateTimeOffset ActiveFrom { get; set; }
    public DateTimeOffset? ActiveTo { get; set; }
}
