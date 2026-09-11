namespace NestyStay.Application.PropertyManager;

public sealed record ProfessionalRecordRequest(string ResourceType, string Status, string PayloadJson, Guid? OwnerUserId = null, Guid? PropertyId = null, string? Currency = null, string? IdempotencyKey = null, DateTimeOffset? ExpiresAt = null, long? ExpectedVersion = null, string? Reason = null);
public sealed record ProfessionalRecordDto(Guid Id, string Area, string ResourceType, string Status, string PayloadJson, Guid? OwnerUserId, Guid? PropertyId, string? Currency, DateTimeOffset? ExpiresAt, long RowVersion, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record ProfessionalRecordEventDto(Guid Id, Guid RecordId, Guid ActorUserId, string Action, string Reason, string MetadataJson, string? IdempotencyKey, DateTimeOffset CreatedAt);
public sealed record ProfessionalReportDto(DateOnly From, DateOnly To, IReadOnlyDictionary<string, decimal> TotalsByCurrency, IReadOnlyDictionary<string, int> CountsByArea, IReadOnlyDictionary<string, int> CountsByStatus, IReadOnlyList<ProfessionalRecordDto> Records);

public interface IPropertyManagerProfessionalCompletionStore
{
    Task<IReadOnlyList<ProfessionalRecordDto>> ListAsync(Guid actorUserId, string area, Guid? propertyId, Guid? ownerUserId, string? status, string? search, CancellationToken cancellationToken);
    Task<ProfessionalRecordDto> CreateAsync(Guid actorUserId, string area, ProfessionalRecordRequest request, CancellationToken cancellationToken);
    Task<ProfessionalRecordDto?> UpdateAsync(Guid actorUserId, string area, Guid id, ProfessionalRecordRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProfessionalRecordEventDto>> HistoryAsync(Guid actorUserId, Guid id, CancellationToken cancellationToken);
    Task<ProfessionalReportDto> ReportAsync(Guid actorUserId, DateOnly from, DateOnly to, Guid? propertyId, Guid? ownerUserId, CancellationToken cancellationToken);
}
