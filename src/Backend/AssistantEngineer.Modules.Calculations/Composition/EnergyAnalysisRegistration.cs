using AssistantEngineer.Modules.Calculations.Application.Services.Analytics;
using AssistantEngineer.Modules.Calculations.Application.Services.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.Buildings;
using AssistantEngineer.Modules.Calculations.Application.Services.Comfort;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingSystems;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingSystems;
using AssistantEngineer.Modules.Calculations.Application.Services.Performance;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;
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
        services.AddScoped<DomesticHotWaterDemandService>();

        services.AddScoped<HeatingSystemEnergyService>();
        services.AddScoped<CoolingSystemEnergyService>();
        services.AddSingleton<SystemEnergyEngine>();

        services.AddScoped<BuildingEnergyPerformanceSummaryService>();
        services.AddScoped<BuildingPerformanceService>();

        return services;
    }
}
