using NestyStay.Domain.Common;

namespace NestyStay.Domain.AssociationManagement;

public sealed class Meeting : BaseEntity
{
    public Guid CommunityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset MeetingAt { get; set; }
    public MeetingStatus Status { get; set; } = MeetingStatus.Draft;
    public string? ZoomArchiveUrl { get; set; }
}

public sealed class MeetingDocument : BaseEntity
{
    public Guid MeetingId { get; set; }
    public Guid StorageObjectId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
}

public sealed class FinancialStatementVersion : BaseEntity
{
    public Guid MeetingId { get; set; }
    public string VersionType { get; set; } = string.Empty;
    public Guid StorageObjectId { get; set; }
    public DateTimeOffset SentAt { get; set; }
}

public sealed class Vote : BaseEntity
{
    public Guid MeetingId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string EncryptedVotePayload { get; set; } = string.Empty;
}

public sealed class VoteResult : BaseEntity
{
    public Guid MeetingId { get; set; }
    public string AggregateResultJson { get; set; } = "{}";
    public DateTimeOffset PublishedAt { get; set; }
}

public sealed class Proxy : BaseEntity
{
    public Guid MeetingId { get; set; }
    public Guid OwnerUserId { get; set; }
    public ProxyCutoffOption CutoffOption { get; set; }
    public DateTimeOffset? CustomCutoffAt { get; set; }
    public bool IsEligible { get; set; }
    public bool IsSealed { get; set; } = true;
}

public sealed class BidOpening : BaseEntity
{
    public Guid MeetingId { get; set; }
    public string SealedBidsJson { get; set; } = "[]";
    public DateTimeOffset? RevealedAt { get; set; }
}

public sealed class AssociationStoragePlan : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public decimal AnnualPrice { get; set; }
    public int StorageMegabytes { get; set; }
    public int RetentionYears { get; set; }
    public bool IncludesZoomArchive { get; set; }
}

public sealed class DocumentRetentionRule : BaseEntity
{
    public string DocumentType { get; set; } = string.Empty;
    public int RetentionYears { get; set; }
}
