using NestyStay.Domain.Common;

namespace NestyStay.Domain.Documents;

public sealed class StorageObject : BaseEntity
{
    public string Provider { get; set; } = "CloudflareR2";
    public string Bucket { get; set; } = string.Empty;
    public string ObjectKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    public string? Checksum { get; set; }
    public StorageAccessScope AccessScope { get; set; } = StorageAccessScope.Private;
}

public sealed class DocumentVaultItem : BaseEntity
{
    public string SubjectType { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public Guid StorageObjectId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public DateTimeOffset? RetainUntil { get; set; }
}
