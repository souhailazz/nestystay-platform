using Microsoft.Extensions.DependencyInjection;
using NestyStay.Application.Abstractions;
using NestyStay.Application.PhaseOne;
using NestyStay.Application.PhaseTwo;
using NestyStay.Application.Services;
using NestyStay.Application.Wellness;

namespace NestyStay.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IPlatformBlueprintService, PlatformBlueprintService>();
        services.AddSingleton<IBookingWorkflowService, BookingWorkflowService>();
        services.AddSingleton<IPricebookService, PricebookService>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPhaseOneStore, PhaseOneStore>();
        services.AddSingleton<IPhaseTwoStore, PhaseTwoStore>();
        services.AddSingleton<IProviderEventStore, InMemoryProviderEventStore>();
        services.AddScoped<IWellnessStore, WellnessStoreUnavailable>();

        return services;
    }

    private sealed class InMemoryProviderEventStore : IProviderEventStore
    {
        private readonly Dictionary<string, ProviderEventReceipt> _receipts = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _gate = new();

        public Task<ProviderEventReceipt> RecordReceivedAsync(ProviderEventRecord record, CancellationToken cancellationToken)
        {
            var key = $"{record.Kind}:{record.ProviderName}:{record.EventId}";
            lock (_gate)
            {
                if (_receipts.TryGetValue(key, out var existing))
                {
                    return Task.FromResult(existing with { IsDuplicate = true });
                }

                var receipt = new ProviderEventReceipt(Guid.NewGuid(), false, "Received", record.ReceivedAt);
                _receipts[key] = receipt;
                return Task.FromResult(receipt);
            }
        }

        public Task MarkProcessedAsync(Guid providerEventId, ProviderEventProcessingResult result, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                var match = _receipts.SingleOrDefault(item => item.Value.Id == providerEventId);
                if (!string.IsNullOrWhiteSpace(match.Key))
                {
                    _receipts[match.Key] = match.Value with { Status = result.Status };
                }
            }

            return Task.CompletedTask;
        }
    }

    private sealed class WellnessStoreUnavailable : IWellnessStore
    {
        private static InvalidOperationException MissingInfrastructure() =>
            new("Wellness milestone storage requires infrastructure services.");

        public Task<WellnessOfficerDto> OnboardOfficerAsync(OnboardOfficerRequest request, CancellationToken cancellationToken) => throw MissingInfrastructure();
        public Task<IReadOnlyList<WellnessOfficerDto>> GetOfficersAsync(string? status, CancellationToken cancellationToken) => throw MissingInfrastructure();
        public Task<WellnessOfficerDto?> GetOfficerAsync(Guid officerId, CancellationToken cancellationToken) => throw MissingInfrastructure();
        public Task<IReadOnlyList<WellnessOfficerDto>> GetAvailableOfficersAsync(string parish, DateTimeOffset scheduledAt, CancellationToken cancellationToken) => throw MissingInfrastructure();
        public Task<WellnessOfficerDto?> ApproveOfficerAsync(Guid officerId, AdminOfficerReviewRequest request, CancellationToken cancellationToken) => throw MissingInfrastructure();
        public Task<WellnessOfficerDto?> RejectOfficerAsync(Guid officerId, AdminOfficerReviewRequest request, CancellationToken cancellationToken) => throw MissingInfrastructure();
        public Task<WellnessOfficerDto?> SuspendOfficerAsync(Guid officerId, AdminOfficerReviewRequest request, CancellationToken cancellationToken) => throw MissingInfrastructure();
        public Task<WellnessOfficerDto?> ReactivateOfficerAsync(Guid officerId, AdminOfficerReviewRequest request, CancellationToken cancellationToken) => throw MissingInfrastructure();
        public Task<WellnessQuoteDto> QuoteVisitAsync(WellnessQuoteRequest request, CancellationToken cancellationToken) => throw MissingInfrastructure();
        public Task<WellnessVisitDto> CreateVisitAsync(CreateWellnessVisitRequest request, CancellationToken cancellationToken) => throw MissingInfrastructure();
        public Task<IReadOnlyList<WellnessVisitDto>> GetVisitsAsync(Guid? hostUserId, Guid? propertyId, Guid? officerId, CancellationToken cancellationToken) => throw MissingInfrastructure();
        public Task<WellnessVisitDto?> GetVisitAsync(Guid visitId, CancellationToken cancellationToken) => throw MissingInfrastructure();
        public Task<WellnessVisitDto?> AssignOfficerAsync(Guid visitId, AssignOfficerRequest request, CancellationToken cancellationToken) => throw MissingInfrastructure();
        public Task<WellnessVisitDto?> CancelVisitAsync(Guid visitId, CancelWellnessVisitRequest request, CancellationToken cancellationToken) => throw MissingInfrastructure();
        public Task<WellnessReportPhotoUploadDto> PrepareReportPhotoUploadAsync(Guid visitId, PrepareWellnessReportPhotoUploadRequest request, bool adminOverride, CancellationToken cancellationToken) => throw MissingInfrastructure();
        public Task<WellnessReportPhotoUploadDto> UploadReportPhotoContentAsync(Guid visitId, Guid photoId, string officerBadgeNumber, string contentType, long sizeBytes, Stream content, bool adminOverride, CancellationToken cancellationToken) => throw MissingInfrastructure();
        public Task<WellnessVisitDto?> SubmitReportAsync(Guid visitId, SubmitWellnessReportRequest request, bool adminOverride, CancellationToken cancellationToken) => throw MissingInfrastructure();
        public Task<WellnessPayoutDto?> MarkPayoutPaidAsync(Guid visitId, MarkPayoutPaidRequest request, CancellationToken cancellationToken) => throw MissingInfrastructure();
        public Task<IReadOnlyList<WellnessPayoutDto>> GetPayoutsAsync(string? status, CancellationToken cancellationToken) => throw MissingInfrastructure();
        public Task<WellnessAdminDashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken) => throw MissingInfrastructure();
    }
}
