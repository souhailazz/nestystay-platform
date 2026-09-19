using Microsoft.EntityFrameworkCore;
using NestyStay.Application.Abstractions;
using NestyStay.Domain.Integrations;

namespace NestyStay.Infrastructure.Persistence;

public sealed class EfProviderEventStore(NestyStayDbContext db) : IProviderEventStore
{
    public async Task<ProviderEventReceipt> RecordReceivedAsync(ProviderEventRecord record, CancellationToken cancellationToken)
    {
        var providerName = Normalize(record.ProviderName);
        var eventId = Normalize(record.EventId);
        var existing = await db.ProviderEvents.SingleOrDefaultAsync(
            providerEvent =>
                providerEvent.Kind == record.Kind &&
                providerEvent.ProviderName == providerName &&
                providerEvent.EventId == eventId,
            cancellationToken);

        if (existing is not null)
        {
            return new ProviderEventReceipt(existing.Id, true, existing.Status, existing.ReceivedAt);
        }

        var providerEvent = new ProviderEvent
        {
            Kind = record.Kind,
            ProviderName = providerName,
            EventId = eventId,
            EventType = Normalize(record.EventType),
            PayloadJson = string.IsNullOrWhiteSpace(record.PayloadJson) ? "{}" : record.PayloadJson,
            PayloadSha256 = Normalize(record.PayloadSha256),
            ReceivedAt = record.ReceivedAt,
            Status = "Received"
        };

        db.ProviderEvents.Add(providerEvent);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.Entry(providerEvent).State = EntityState.Detached;
            existing = await db.ProviderEvents.SingleAsync(
                stored =>
                    stored.Kind == record.Kind &&
                    stored.ProviderName == providerName &&
                    stored.EventId == eventId,
                cancellationToken);
            return new ProviderEventReceipt(existing.Id, true, existing.Status, existing.ReceivedAt);
        }

        return new ProviderEventReceipt(providerEvent.Id, false, providerEvent.Status, providerEvent.ReceivedAt);
    }

    public async Task MarkProcessedAsync(Guid providerEventId, ProviderEventProcessingResult result, CancellationToken cancellationToken)
    {
        var providerEvent = await db.ProviderEvents.SingleOrDefaultAsync(item => item.Id == providerEventId, cancellationToken);
        if (providerEvent is null)
        {
            return;
        }

        providerEvent.Status = Normalize(result.Status);
        providerEvent.SubjectType = Normalize(result.SubjectType);
        providerEvent.SubjectId = result.SubjectId;
        providerEvent.ProcessingResult = Normalize(result.Message);
        providerEvent.ProcessedAt = result.ProcessedAt;
        providerEvent.UpdatedAt = result.ProcessedAt;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string Normalize(string value) => value.Trim();
}
