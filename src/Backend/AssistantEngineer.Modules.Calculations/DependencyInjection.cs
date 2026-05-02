using AssistantEngineer.Modules.Calculations.Composition;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AssistantEngineer.Modules.Calculations;

public static class DependencyInjection
{
    public static IServiceCollection AddCalculationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.TryAddSingleton(TimeProvider.System);

        services.AddCalculationOptions(configuration);
        services.AddCalculationReferenceData();
        services.AddCalculationProfiles();

        services.AddCoolingLoadCalculations();
        services.AddHeatingLoadCalculations();

        services.AddSolarCalculations();
        services.AddWeatherCalculations();
        services.AddWeatherSolarCalculations();

        services.AddIso52016Calculations();
        services.AddVentilationCalculations();
        services.AddGroundCalculations();
        services.AddEnergyAnalysisCalculations();
        services.AddSizingCalculations();

        services.AddCalculationFacades();

        return services;
    }
}
