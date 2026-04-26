using AssistantEngineer.Modules.Calculations.Application.Facades;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Calculations.Composition;

internal static class CalculationFacadeRegistration
{
    public static IServiceCollection AddCalculationFacades(
        this IServiceCollection services)
    {
        services.AddScoped<ILoadCalculationsFacade, LoadCalculationsFacade>();
        services.AddScoped<IVentilationAnalysisFacade, VentilationAnalysisFacade>();
        services.AddScoped<IDomesticHotWaterFacade, DomesticHotWaterFacade>();
        services.AddScoped<IProfilesFacade, ProfilesFacade>();
        services.AddScoped<IStandardReferenceDataFacade, StandardReferenceDataFacade>();

        services.AddScoped<IBuildingEnergyAnalysisFacade, BuildingEnergyAnalysisFacade>();
        services.AddScoped<IBuildingComfortAnalysisFacade, BuildingComfortAnalysisFacade>();
        services.AddScoped<IBuildingSizingAnalysisFacade, BuildingSizingAnalysisFacade>();

        return services;
    }
}