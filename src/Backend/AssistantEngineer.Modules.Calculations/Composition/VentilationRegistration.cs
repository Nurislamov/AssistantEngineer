using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation.Iso16798;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Calculations.Composition;

internal static class VentilationRegistration
{
    public static IServiceCollection AddVentilationCalculations(
        this IServiceCollection services)
    {
        services.AddNaturalVentilationFoundation();

        services.AddSingleton<VentilationAndInfiltrationLoadEngine>();
        services.AddScoped<IVentilationHeatTransferCalculator, VentilationHeatTransferCalculator>();

        services.AddScoped<Iso16798NaturalVentilationCalculator>();
        services.AddScoped<Iso16798NaturalVentilationApplicationAdapter>();
        services.AddScoped<INaturalVentilationOpeningControlService, NaturalVentilationOpeningControlService>();
        services.AddScoped<INaturalVentilationAirflowService, NaturalVentilationAirflowService>();
        services.AddScoped<NaturalVentilationPreviewService>();

        return services;
    }
}
