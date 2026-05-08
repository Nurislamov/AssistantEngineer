using AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Calculations.Composition;

internal static class SystemEnergyRegistration
{
    public static IServiceCollection AddSystemEnergyFoundation(
        this IServiceCollection services)
    {
        services.AddSingleton<ISystemEnergyUsefulLoadValidator, SystemEnergyUsefulLoadValidator>();
        services.AddSingleton<ISystemEnergyModuleChainInputValidator, SystemEnergyModuleChainInputValidator>();
        services.AddSingleton<ISystemEnergyModuleCalculator, SystemEnergyModuleCalculator>();
        services.AddSingleton<ISystemEnergyModuleChainCalculator, SystemEnergyModuleChainCalculator>();
        services.AddSingleton<ISystemEnergyGenerationHandoffBuilder, SystemEnergyGenerationHandoffBuilder>();
        services.AddSingleton<IDomesticHotWaterSystemEnergyHandoffAdapter, DomesticHotWaterSystemEnergyHandoffAdapter>();
        services.AddSingleton<ISystemEnergyGeneratorInputValidator, SystemEnergyGeneratorInputValidator>();
        services.AddSingleton<ISystemEnergyGeneratorLoadSplitter, SystemEnergyGeneratorLoadSplitter>();
        services.AddSingleton<ISystemEnergyGeneratorFinalEnergyCalculator, SystemEnergyGeneratorFinalEnergyCalculator>();
        services.AddSingleton<ISystemEnergyFinalEnergyAggregator, SystemEnergyFinalEnergyAggregator>();
        services.AddSingleton<ISystemEnergyFinalEnergyCalculator, SystemEnergyFinalEnergyCalculator>();

        return services;
    }
}
