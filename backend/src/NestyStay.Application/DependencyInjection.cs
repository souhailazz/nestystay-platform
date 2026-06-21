using Microsoft.Extensions.DependencyInjection;
using NestyStay.Application.PhaseOne;
using NestyStay.Application.PhaseTwo;
using NestyStay.Application.Services;

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

        return services;
    }
}
