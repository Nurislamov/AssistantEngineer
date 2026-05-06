using AssistantEngineer.Modules.Calculations.Application.Services.Analytics;
using AssistantEngineer.Modules.Calculations.Application.Services.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.Buildings;
using AssistantEngineer.Modules.Calculations.Application.Services.Comfort;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingSystems;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater.Iso12831;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy.En15316;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingSystems;
using AssistantEngineer.Modules.Calculations.Application.Services.Performance;
using AssistantEngineer.Modules.Calculations.Application.Services.Rollup;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.Validation.BuildingInput;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Calculations.Composition;

internal static class EnergyAnalysisRegistration
{
    public static IServiceCollection AddEnergyAnalysisCalculations(
        this IServiceCollection services)
    {
        services.AddScoped<BuildingEnergyBalanceService>();
        services.AddSingleton<AnnualEnergyBalanceEngine>();

        services.AddScoped<BuildingComfortMetricsService>();
        services.AddScoped<BuildingZoneComfortMetricsService>();
        services.AddScoped<BuildingRoomComfortMetricsService>();

        services.AddScoped<EnergySignatureService>();
        services.AddSingleton<Iso12831DomesticHotWaterReferenceDataProvider>();
        services.AddSingleton<Iso12831DomesticHotWaterDrawProfileProvider>();
        services.AddSingleton<Iso12831DomesticHotWaterDemandCalculator>();
        services.AddSingleton<Iso12831DomesticHotWaterApplicationAdapter>();
        services.AddScoped<DomesticHotWaterDemandService>();

        services.AddScoped<HeatingSystemEnergyService>();
        services.AddScoped<CoolingSystemEnergyService>();
        services.AddSingleton<En15316SystemEnergyReferenceDataProvider>();
        services.AddSingleton<En15316SystemEnergyChainCalculator>();
        services.AddSingleton<En15316SystemEnergyApplicationAdapter>();
        services.AddSingleton<EngineeringCalculationModeCatalogProvider>();
        services.AddSingleton<EngineeringCalculationModeComparisonEngine>();
        services.AddSingleton<EngineeringCalculationMetricAdapter>();
        services.AddSingleton<SystemEnergyEngine>();
        services.AddScoped<BuildingInputValidationService>();

        services.AddScoped<BuildingEnergyPerformanceSummaryService>();
        services.AddScoped<BuildingPerformanceService>();

        return services;
    }
}
