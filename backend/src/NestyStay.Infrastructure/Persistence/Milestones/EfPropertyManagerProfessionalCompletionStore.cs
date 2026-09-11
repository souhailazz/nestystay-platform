using System.Text.Json;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NestyStay.Application.PropertyManager;

namespace NestyStay.Infrastructure.Persistence.Milestones;

/// <summary>
/// Completes the remaining M5 operational workflows with one consistent contract:
/// relational manager/owner/property scope, JSON business payload, explicit state,
/// optimistic concurrency, idempotent commands and append-only history.  The
/// payload is intentionally extensible while all security boundaries remain
/// relational and server-authoritative.
/// </summary>
public sealed class EfPropertyManagerProfessionalCompletionStore(NestyStayDbContext db, TimeProvider timeProvider) : IPropertyManagerProfessionalCompletionStore
{
    private static readonly HashSet<string> Areas = new(StringComparer.OrdinalIgnoreCase)
    {
        "utilities", "documents", "governance", "team", "rbac", "reporting", "audit", "vendors", "assets", "inventory", "incidents", "community", "bulk", "notifications"
    };

    private static readonly HashSet<string> FinanceAreas = new(StringComparer.OrdinalIgnoreCase) { "utilities", "reporting", "bulk" };

    public async Task<IReadOnlyList<ProfessionalRecordDto>> ListAsync(Guid actorUserId, string area, Guid? propertyId, Guid? ownerUserId, string? status, string? search, CancellationToken cancellationToken)
    {
        var context = await ResolveContextAsync(actorUserId, area, propertyId, ownerUserId, requireMutation: false, cancellationToken);
        var query = db.MilestonePmProfessionalRecords.AsNoTracking().Where(x => x.ManagerUserId == context.ManagerId && x.Area == NormalizeArea(area) && !x.IsDeleted);
        if (propertyId is { } property) query = query.Where(x => x.PropertyId == property);
        if (ownerUserId is { } owner) query = query.Where(x => x.OwnerUserId == owner);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status.Trim().ToUpperInvariant());
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.SearchText != null && x.SearchText.Contains(search.Trim()));
        if (context.Staff is { } staff) query = ApplyStaffScope(query, staff);
        var rows = await query.OrderByDescending(x => x.UpdatedAt).Take(500).ToListAsync(cancellationToken);
        return rows.Select(ToDto).ToList();
    }

    public async Task<ProfessionalRecordDto> CreateAsync(Guid actorUserId, string area, ProfessionalRecordRequest request, CancellationToken cancellationToken)
    {
        var normalizedArea = NormalizeArea(area);
        ValidateRequest(normalizedArea, request);
        var context = await ResolveContextAsync(actorUserId, normalizedArea, request.PropertyId, request.OwnerUserId, requireMutation: true, cancellationToken);
        await using var transaction = db.Database.IsNpgsql() ? await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken) : null;
        if (transaction is not null && !string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var lockKey = $"nesty-pm:professional:{context.ManagerId}:{normalizedArea}:{request.IdempotencyKey.Trim()}";
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", cancellationToken);
        }
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var replay = await db.MilestonePmProfessionalRecords.AsNoTracking().SingleOrDefaultAsync(x => x.ManagerUserId == context.ManagerId && x.IdempotencyKey == request.IdempotencyKey && !x.IsDeleted, cancellationToken);
            if (replay is not null)
            {
                if (!MatchesRequest(replay, request)) throw new InvalidOperationException("The idempotency key was already used with a different request.");
                return ToDto(replay);
            }
        }
        var now = timeProvider.GetUtcNow();
        var item = new MilestonePmProfessionalRecord
        {
            ManagerUserId = context.ManagerId,
            OwnerUserId = request.OwnerUserId,
            PropertyId = request.PropertyId,
            Area = normalizedArea,
            ResourceType = request.ResourceType.Trim().ToUpperInvariant(),
            Status = request.Status.Trim().ToUpperInvariant(),
            PayloadJson = request.PayloadJson,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? null : request.Currency.Trim().ToUpperInvariant(),
            IdempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? null : request.IdempotencyKey.Trim(),
            SearchText = BuildSearchText(request),
            ExpiresAt = request.ExpiresAt,
            CreatedByUserId = actorUserId,
            UpdatedByUserId = actorUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.MilestonePmProfessionalRecords.Add(item);
        AddEvent(item, actorUserId, "CREATED", request.Reason ?? "Record created", request.IdempotencyKey, request);
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return ToDto(item);
    }

    public async Task<ProfessionalRecordDto?> UpdateAsync(Guid actorUserId, string area, Guid id, ProfessionalRecordRequest request, CancellationToken cancellationToken)
    {
        var normalizedArea = NormalizeArea(area);
        ValidateRequest(normalizedArea, request);
        var item = await db.MilestonePmProfessionalRecords.SingleOrDefaultAsync(x => x.Id == id && x.Area == normalizedArea && !x.IsDeleted, cancellationToken);
        if (item is null) return null;
        var context = await ResolveContextAsync(actorUserId, normalizedArea, item.PropertyId, item.OwnerUserId, requireMutation: true, cancellationToken, item.ManagerUserId);
        if (request.PropertyId != item.PropertyId || request.OwnerUserId != item.OwnerUserId)
        {
            await ResolveContextAsync(actorUserId, normalizedArea, request.PropertyId, request.OwnerUserId, requireMutation: true, cancellationToken, item.ManagerUserId);
            if (request.PropertyId is { } requestedProperty && request.OwnerUserId is { } requestedOwner && !await db.MilestoneManagerProperties.AnyAsync(x => x.Id == requestedProperty && x.OwnerUserId == requestedOwner && x.ManagerUserId == context.ManagerId && !x.IsDeleted, cancellationToken))
                throw new InvalidOperationException("The property is not assigned to the requested owner.");
        }
        if (request.ExpectedVersion is { } expected && expected != item.RowVersion) throw new DbUpdateConcurrencyException("This record changed. Reload it before saving.");
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var replay = await db.MilestonePmProfessionalRecordEvents.AsNoTracking().FirstOrDefaultAsync(x => x.RecordId == item.Id && x.IdempotencyKey == request.IdempotencyKey, cancellationToken);
            if (replay is not null)
            {
                using var metadata = JsonDocument.Parse(replay.MetadataJson);
                if (!metadata.RootElement.TryGetProperty("requestHash", out var hash) || !string.Equals(hash.GetString(), RequestHash(request), StringComparison.Ordinal))
                    throw new InvalidOperationException("The idempotency key was already used with a different request.");
                return ToDto(item);
            }
        }
        item.Status = request.Status.Trim().ToUpperInvariant();
        item.PayloadJson = request.PayloadJson;
        item.OwnerUserId = request.OwnerUserId ?? item.OwnerUserId;
        item.PropertyId = request.PropertyId ?? item.PropertyId;
        item.Currency = string.IsNullOrWhiteSpace(request.Currency) ? item.Currency : request.Currency.Trim().ToUpperInvariant();
        item.ExpiresAt = request.ExpiresAt ?? item.ExpiresAt;
        item.SearchText = BuildSearchText(request);
        item.RowVersion++;
        item.UpdatedAt = timeProvider.GetUtcNow();
        item.UpdatedByUserId = actorUserId;
        AddEvent(item, actorUserId, "UPDATED", request.Reason ?? "Record updated", request.IdempotencyKey, request);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(item);
    }

    public async Task<IReadOnlyList<ProfessionalRecordEventDto>> HistoryAsync(Guid actorUserId, Guid id, CancellationToken cancellationToken)
    {
        var item = await db.MilestonePmProfessionalRecords.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (item is null) return [];
        await ResolveContextAsync(actorUserId, item.Area, item.PropertyId, item.OwnerUserId, requireMutation: false, cancellationToken, item.ManagerUserId);
        return await db.MilestonePmProfessionalRecordEvents.AsNoTracking().Where(x => x.RecordId == id).OrderByDescending(x => x.CreatedAt).Select(x => new ProfessionalRecordEventDto(x.Id, x.RecordId, x.ActorUserId, x.Action, x.Reason, x.MetadataJson, x.IdempotencyKey, x.CreatedAt)).ToListAsync(cancellationToken);
    }

    public async Task<ProfessionalReportDto> ReportAsync(Guid actorUserId, DateOnly from, DateOnly to, Guid? propertyId, Guid? ownerUserId, CancellationToken cancellationToken)
    {
        if (to < from) throw new InvalidOperationException("Report end date must be on or after the start date.");
        var context = await ResolveContextAsync(actorUserId, "reporting", propertyId, ownerUserId, requireMutation: false, cancellationToken);
        var start = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var query = db.MilestonePmProfessionalRecords.AsNoTracking().Where(x => x.ManagerUserId == context.ManagerId && !x.IsDeleted && x.CreatedAt >= start && x.CreatedAt < end);
        if (propertyId is { } property) query = query.Where(x => x.PropertyId == property);
        if (ownerUserId is { } owner) query = query.Where(x => x.OwnerUserId == owner);
        if (context.Staff is { } staff) query = ApplyStaffScope(query, staff);
        var rows = await query.OrderBy(x => x.CreatedAt).Take(1000).ToListAsync(cancellationToken);
        var totals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Currency)) continue;
            if (!TryReadAmount(row.PayloadJson, out var amount)) continue;
            totals[row.Currency] = totals.GetValueOrDefault(row.Currency) + amount;
        }
        return new ProfessionalReportDto(from, to, totals, rows.GroupBy(x => x.Area).ToDictionary(x => x.Key, x => x.Count()), rows.GroupBy(x => x.Status).ToDictionary(x => x.Key, x => x.Count()), rows.Select(ToDto).ToList());
    }

    private async Task<ActorContext> ResolveContextAsync(Guid actorUserId, string area, Guid? propertyId, Guid? ownerUserId, bool requireMutation, CancellationToken cancellationToken, Guid? expectedManagerId = null)
    {
        var manager = await db.MilestonePropertyManagers.AsNoTracking().SingleOrDefaultAsync(x => x.ManagerUserId == actorUserId && !x.IsDeleted, cancellationToken);
        MilestoneP0StaffMembership? staff = null;
        var managerId = manager?.ManagerUserId;
        if (managerId is null)
        {
            staff = await db.MilestoneP0StaffMemberships.AsNoTracking().SingleOrDefaultAsync(x => x.StaffUserId == actorUserId && x.Status == "ACTIVE" && !x.IsDeleted, cancellationToken);
            managerId = staff?.ManagerUserId;
        }
        if (managerId is null || (expectedManagerId is { } expected && expected != managerId)) throw new UnauthorizedAccessException("You are not an active member of this manager portfolio.");
        if (staff is not null)
        {
            if (requireMutation && staff.Role.Equals("READONLY", StringComparison.OrdinalIgnoreCase)) throw new UnauthorizedAccessException("Read-only staff cannot change portfolio records.");
            if (FinanceAreas.Contains(area) && !staff.CanManageFinance) throw new UnauthorizedAccessException("Finance capability is required for this workflow.");
            if (propertyId is { } property && !ParseIds(staff.PropertyScopeJson).Contains(property)) throw new UnauthorizedAccessException("Property is outside your assigned scope.");
            if (ownerUserId is { } owner && staff.OwnerScopeJson != "[]" && !ParseIds(staff.OwnerScopeJson).Contains(owner)) throw new UnauthorizedAccessException("Owner is outside your assigned scope.");
        }
        if (propertyId is { } p && !await db.MilestoneManagerProperties.AnyAsync(x => x.ManagerUserId == managerId && x.Id == p && !x.IsDeleted, cancellationToken)) throw new UnauthorizedAccessException("Property is outside the manager portfolio.");
        if (ownerUserId is { } o && !await db.MilestoneManagerOwners.AnyAsync(x => x.ManagerUserId == managerId && x.OwnerUserId == o && !x.IsDeleted, cancellationToken)) throw new UnauthorizedAccessException("Owner is outside the manager portfolio.");
        if (propertyId is { } scopedProperty && ownerUserId is { } scopedOwner && !await db.MilestoneManagerProperties.AnyAsync(x => x.ManagerUserId == managerId && x.Id == scopedProperty && x.OwnerUserId == scopedOwner && !x.IsDeleted, cancellationToken)) throw new UnauthorizedAccessException("The property is not assigned to the requested owner.");
        return new ActorContext(managerId.Value, staff);
    }

    private static IQueryable<MilestonePmProfessionalRecord> ApplyStaffScope(IQueryable<MilestonePmProfessionalRecord> query, MilestoneP0StaffMembership staff)
    {
        var properties = ParseIds(staff.PropertyScopeJson);
        var owners = ParseIds(staff.OwnerScopeJson);
        if (properties.Count > 0) query = query.Where(x => x.PropertyId == null || properties.Contains(x.PropertyId.Value));
        if (owners.Count > 0) query = query.Where(x => x.OwnerUserId == null || owners.Contains(x.OwnerUserId.Value));
        return query;
    }

    private void AddEvent(MilestonePmProfessionalRecord item, Guid actor, string action, string reason, string? idempotencyKey, ProfessionalRecordRequest request)
    {
        var requestHash = RequestHash(request);
        db.MilestonePmProfessionalRecordEvents.Add(new MilestonePmProfessionalRecordEvent { RecordId = item.Id, ManagerUserId = item.ManagerUserId, ActorUserId = actor, Action = action, Reason = reason.Trim(), IdempotencyKey = idempotencyKey, MetadataJson = JsonSerializer.Serialize(new { item.Area, item.ResourceType, item.Status, item.RowVersion, requestHash }) });
        db.MilestoneAuditEvents.Add(new MilestoneAuditEvent { ManagerUserId = item.ManagerUserId, ActorUserId = actor, ActorRole = "PropertyManager", Action = $"Pms{action}", SubjectType = item.ResourceType, SubjectId = item.Id, Reason = reason.Trim(), MetadataJson = JsonSerializer.Serialize(new { item.Area, item.PropertyId, item.OwnerUserId, idempotencyKey, requestHash }) });
    }

    private static string NormalizeArea(string area)
    {
        var normalized = area.Trim().ToLowerInvariant();
        if (!Areas.Contains(normalized)) throw new InvalidOperationException($"Unknown Property Manager workflow area '{area}'.");
        return normalized;
    }

    private static void ValidateRequest(string area, ProfessionalRecordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ResourceType) || string.IsNullOrWhiteSpace(request.Status)) throw new InvalidOperationException("Resource type and status are required.");
        if (string.IsNullOrWhiteSpace(request.PayloadJson) || request.PayloadJson.Length > 500_000) throw new InvalidOperationException("A bounded JSON payload is required.");
        try { using var document = JsonDocument.Parse(request.PayloadJson); if (document.RootElement.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("Payload must be a JSON object."); }
        catch (JsonException) { throw new InvalidOperationException("Payload must be valid JSON."); }
        if (area == "utilities" && request.PropertyId is null) throw new InvalidOperationException("Utility records require a property.");
        if (area is "assets" or "inventory" or "incidents" && request.PropertyId is null) throw new InvalidOperationException($"{area} records require a property.");
        if (request.ExpiresAt is { } expires && expires <= DateTimeOffset.UtcNow) throw new InvalidOperationException("Expiry must be in the future.");
        if (!string.IsNullOrWhiteSpace(request.Currency))
        {
            var currency = request.Currency.Trim().ToUpperInvariant();
            if (currency.Length != 3 || currency.Any(ch => ch is < 'A' or > 'Z')) throw new InvalidOperationException("Currency must be a three-letter ISO code.");
        }
    }

    private static bool MatchesRequest(MilestonePmProfessionalRecord item, ProfessionalRecordRequest request) =>
        string.Equals(item.ResourceType, request.ResourceType.Trim().ToUpperInvariant(), StringComparison.Ordinal) &&
        string.Equals(item.Status, request.Status.Trim().ToUpperInvariant(), StringComparison.Ordinal) &&
        string.Equals(item.PayloadJson, request.PayloadJson, StringComparison.Ordinal) &&
        item.OwnerUserId == request.OwnerUserId && item.PropertyId == request.PropertyId &&
        string.Equals(item.Currency, string.IsNullOrWhiteSpace(request.Currency) ? null : request.Currency.Trim().ToUpperInvariant(), StringComparison.Ordinal) && item.ExpiresAt == request.ExpiresAt;

    private static string RequestHash(ProfessionalRecordRequest request)
    {
        var canonical = string.Join("|", request.ResourceType.Trim().ToUpperInvariant(), request.Status.Trim().ToUpperInvariant(), request.PayloadJson, request.OwnerUserId, request.PropertyId, request.Currency?.Trim().ToUpperInvariant(), request.ExpiresAt?.ToUniversalTime().ToString("O"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static bool TryReadAmount(string json, out decimal amount)
    {
        amount = 0;
        try
        {
            using var doc = JsonDocument.Parse(json);
            foreach (var name in new[] { "amount", "total", "value", "cost" }) if (doc.RootElement.TryGetProperty(name, out var value) && value.TryGetDecimal(out amount)) return true;
        }
        catch (JsonException) { }
        return false;
    }

    private static string BuildSearchText(ProfessionalRecordRequest request) => $"{request.ResourceType} {request.Status} {request.PayloadJson}".ToLowerInvariant()[..Math.Min(4000, ($"{request.ResourceType} {request.Status} {request.PayloadJson}").Length)];
    private static HashSet<Guid> ParseIds(string json) { try { return JsonSerializer.Deserialize<HashSet<Guid>>(json) ?? []; } catch (JsonException) { return []; } }
    private static ProfessionalRecordDto ToDto(MilestonePmProfessionalRecord x) => new(x.Id, x.Area, x.ResourceType, x.Status, x.PayloadJson, x.OwnerUserId, x.PropertyId, x.Currency, x.ExpiresAt, x.RowVersion, x.CreatedAt, x.UpdatedAt);
    private sealed record ActorContext(Guid ManagerId, MilestoneP0StaffMembership? Staff);
}
