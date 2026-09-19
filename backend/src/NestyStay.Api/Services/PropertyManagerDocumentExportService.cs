using System.IO.Compression;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NestyStay.Application.Abstractions;
using NestyStay.Infrastructure.Persistence;
using NestyStay.Infrastructure.Persistence.Milestones;

namespace NestyStay.Api.Services;

/// <summary>
/// Processes authorized document ZIP exports from a durable queue. The worker
/// re-checks manager ownership before reading each object and publishes a
/// short-lived signed download URL only after the archive is stored.
/// </summary>
public sealed class PropertyManagerDocumentExportService(
    IServiceScopeFactory scopeFactory,
    IStorageProvider storageProvider,
    TimeProvider timeProvider,
    ILogger<PropertyManagerDocumentExportService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private const int MaximumDocuments = 100;
    private const long MaximumArchiveBytes = 100 * 1024 * 1024;
    private static readonly SemaphoreSlim ClaimGate = new(1, 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                while (await ProcessOneAsync(stoppingToken)) { }
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Property-manager document export processing failed; the next poll will retry.");
            }

            try { await Task.Delay(PollInterval, timeProvider, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
        }
    }

    /// <summary>Processes at most one queued export and is public for deterministic tests.</summary>
    public async Task<bool> ProcessOneAsync(CancellationToken cancellationToken = default)
    {
        if (!await ClaimGate.WaitAsync(0, cancellationToken)) return false;
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NestyStayDbContext>();
            var item = await db.MilestoneManagerDocumentExports
                .Where(x => x.Status == "QUEUED" && !x.IsDeleted)
                .OrderBy(x => x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (item is null) return false;

            item.Status = "PROCESSING";
            item.UpdatedAt = timeProvider.GetUtcNow();
            await db.SaveChangesAsync(cancellationToken);

            try
            {
                var ids = JsonSerializer.Deserialize<Guid[]>(item.DocumentIdsJson) ?? [];
                if (ids.Length is 0 or > MaximumDocuments) throw new InvalidOperationException("The export document count is outside the allowed range.");
                var documents = await db.MilestoneManagerDocuments
                    .Where(x => x.ManagerUserId == item.ManagerUserId && ids.Contains(x.Id) && !x.IsDeleted && !x.IsArchived)
                    .ToListAsync(cancellationToken);
                if (documents.Count != ids.Length) throw new InvalidOperationException("One or more export documents are no longer available.");
                if (documents.Sum(x => x.SizeBytes) > MaximumArchiveBytes) throw new InvalidOperationException("The export exceeds the 100 MB archive limit.");

                await using var archiveStream = new MemoryStream();
                using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var document in documents.OrderBy(x => x.FileName, StringComparer.OrdinalIgnoreCase))
                    {
                        await using var source = await storageProvider.OpenReadAsync(document.StorageKey, cancellationToken);
                        var entryName = UniqueEntryName(Path.GetFileName(document.FileName), names);
                        await using var target = archive.CreateEntry(entryName, CompressionLevel.Fastest).Open();
                        await source.CopyToAsync(target, cancellationToken);
                    }
                }

                archiveStream.Position = 0;
                var objectKey = $"property-manager/{item.ManagerUserId:N}/exports/{item.Id:N}.zip";
                await storageProvider.SaveObjectAsync(new StorageObjectWriteRequest(objectKey, "application/zip", MaximumArchiveBytes), archiveStream, cancellationToken);
                var now = timeProvider.GetUtcNow();
                item.Status = "COMPLETED";
                item.ObjectKey = objectKey;
                item.FileName = $"nesty-documents-{item.Id:N}.zip";
                item.CompletedAt = now;
                item.ExpiresAt = now.AddHours(24);
                item.Error = null;
                item.UpdatedAt = now;
                db.MilestoneAuditEvents.Add(new MilestoneAuditEvent
                {
                    ActorUserId = item.ManagerUserId,
                    ActorRole = "PropertyManager",
                    Action = "DocumentExportCompleted",
                    SubjectType = "DocumentExport",
                    SubjectId = item.Id,
                    Reason = "Authorized document ZIP export completed.",
                    MetadataJson = JsonSerializer.Serialize(new { documentCount = documents.Count })
                });
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                item.Status = "FAILED";
                item.Error = exception.Message.Length > 2000 ? exception.Message[..2000] : exception.Message;
                item.UpdatedAt = timeProvider.GetUtcNow();
                await db.SaveChangesAsync(cancellationToken);
                logger.LogWarning(exception, "Document export {ExportId} failed.", item.Id);
            }

            return true;
        }
        finally { ClaimGate.Release(); }
    }

    private static string UniqueEntryName(string fileName, ISet<string> names)
    {
        var safe = string.IsNullOrWhiteSpace(fileName) ? "document" : fileName;
        if (names.Add(safe)) return safe;
        var stem = Path.GetFileNameWithoutExtension(safe);
        var extension = Path.GetExtension(safe);
        for (var suffix = 2; ; suffix++)
        {
            var candidate = $"{stem}-{suffix}{extension}";
            if (names.Add(candidate)) return candidate;
        }
    }
}
