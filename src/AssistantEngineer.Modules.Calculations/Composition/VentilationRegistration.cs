using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Calculations.Composition;

internal static class VentilationRegistration
{
    public static IServiceCollection AddVentilationCalculations(
        this IServiceCollection services)
    {
        services.AddScoped<IVentilationHeatTransferCalculator, VentilationHeatTransferCalculator>();

        services.AddScoped<INaturalVentilationOpeningControlService, NaturalVentilationOpeningControlService>();
        services.AddScoped<INaturalVentilationAirflowService, NaturalVentilationAirflowService>();
        services.AddScoped<NaturalVentilationPreviewService>();

        return services;
    }
}