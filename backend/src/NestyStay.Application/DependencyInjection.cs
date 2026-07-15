using Microsoft.Extensions.DependencyInjection;
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
        services.AddScoped<IWellnessStore, WellnessStoreUnavailable>();

        return services;
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
        public Task<WellnessVisitDto?> SubmitReportAsync(Guid visitId, SubmitWellnessReportRequest request, bool adminOverride, CancellationToken cancellationToken) => throw MissingInfrastructure();
        public Task<WellnessPayoutDto?> MarkPayoutPaidAsync(Guid visitId, MarkPayoutPaidRequest request, CancellationToken cancellationToken) => throw MissingInfrastructure();
        public Task<IReadOnlyList<WellnessPayoutDto>> GetPayoutsAsync(string? status, CancellationToken cancellationToken) => throw MissingInfrastructure();
        public Task<WellnessAdminDashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken) => throw MissingInfrastructure();
    }
}
