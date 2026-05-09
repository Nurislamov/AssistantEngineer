using AssistantEngineer.Modules.Calculations.Application.Abstractions.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Calculations.Composition;

internal static class DomesticHotWaterRegistration
{
    public static IServiceCollection AddDomesticHotWaterFoundation(
        this IServiceCollection services)
    {
        services.AddSingleton<IDomesticHotWaterDemandInputValidator, DomesticHotWaterDemandInputValidator>();
        services.AddSingleton<IDomesticHotWaterDemandBasisCalculator, DomesticHotWaterDemandBasisCalculator>();
        services.AddSingleton<IDomesticHotWaterDrawProfileBuilder, DomesticHotWaterDrawProfileBuilder>();
        services.AddSingleton<IDomesticHotWaterDrawOffProfileBuilder, DomesticHotWaterDrawOffProfileBuilder>();
        services.AddSingleton<IDomesticHotWaterUsefulDemandCalculator, DomesticHotWaterUsefulDemandCalculator>();
        services.AddSingleton<IDomesticHotWaterSystemLossInputValidator, DomesticHotWaterSystemLossInputValidator>();
        services.AddSingleton<IDomesticHotWaterStorageLossCalculator, DomesticHotWaterStorageLossCalculator>();
        services.AddSingleton<IDomesticHotWaterDistributionLossCalculator, DomesticHotWaterDistributionLossCalculator>();
        services.AddSingleton<IDomesticHotWaterCirculationLossCalculator, DomesticHotWaterCirculationLossCalculator>();
        services.AddSingleton<IDomesticHotWaterLossCalculator, DomesticHotWaterLossCalculator>();
        services.AddSingleton<IDomesticHotWaterSystemLoadCalculator, DomesticHotWaterSystemLoadCalculator>();
        services.AddSingleton<IDomesticHotWaterEn15316HandoffBuilder, DomesticHotWaterEn15316HandoffBuilder>();

        return services;
    }
}
