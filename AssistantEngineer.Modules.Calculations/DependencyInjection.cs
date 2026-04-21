using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Services;
using AssistantEngineer.Modules.Calculations.Application.Services.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Services.Analytics;
using AssistantEngineer.Modules.Calculations.Application.Services.Buildings;
using AssistantEngineer.Modules.Calculations.Application.Services.Common.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads.Simplified;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingSystems;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.Floors;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads.En12831;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingSystems;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Performance;
using AssistantEngineer.Modules.Calculations.Application.Services.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Services.Rooms;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Modules.Calculations;

public static class DependencyInjection
{
    public static IServiceCollection AddCalculationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton(new CoolingLoadCalculationOptions());
        services.AddSingleton(new Iso52016CoolingLoadOptions());
        services.AddSingleton(new Iso52016EnergyNeedOptions());
        services.AddSingleton(new En12831HeatingLoadOptions());

        services.AddSingleton<IHourlyProfileAggregator, HourlyProfileAggregator>();
        services.AddSingleton<IAnnualProfileGenerator, AnnualProfileGenerator>();
        services.AddSingleton<IIso16798ReferenceData, Iso16798ReferenceData>();
        services.AddSingleton<ICoolingLoadReferenceData, CoolingLoadReferenceData>();

        services.AddScoped<IRoomCoolingLoadCalculationStrategy, SimplifiedCoolingLoadCalculator>();
        services.AddScoped<IRoomCoolingLoadCalculationStrategy, Iso52016CoolingLoadCalculator>();
        services.AddScoped<IRoomCoolingLoadCalculator, RoomCoolingLoadCalculator>();

        services.AddScoped<IAggregateLoadCalculator, AggregateCalculator>();

        services.AddScoped<En12831HeatingLoadCalculator>();
        services.AddScoped<IRoomHeatingLoadCalculator>(sp => sp.GetRequiredService<En12831HeatingLoadCalculator>());
        services.AddScoped<IBuildingHeatingLoadCalculator>(sp => sp.GetRequiredService<En12831HeatingLoadCalculator>());

        services.AddScoped<IIso52016ReferenceDataProvider, Iso52016ReferenceDataProvider>();
        services.AddScoped<Iso52016ClimateDataValidator>();
        services.AddScoped<ISolarRadiationService, SolarRadiationService>();
        services.AddScoped<IWindowShadingService, WindowShadingService>();
        services.AddScoped<IVentilationHeatTransferCalculator, VentilationHeatTransferCalculator>();
        services.AddScoped<Iso52016HourlySteadyStateCalculator>();

        services.AddScoped<EnergySignatureService>();
        services.AddScoped<DomesticHotWaterDemandService>();
        services.AddScoped<HeatingSystemEnergyService>();
        services.AddScoped<CoolingSystemEnergyService>();
        services.AddSingleton<IEnergyCarrierFactorProvider, EnergyCarrierFactorProvider>();
        services.AddScoped<BuildingEnergyPerformanceSummaryService>();
        services.AddScoped<BuildingPerformanceService>();

        services.AddScoped<IBuildingEnergyCalculator, Iso52016BuildingEnergyCalculator>();

        services.AddScoped<BuildingCoolingLoadService>();
        services.AddScoped<BuildingHeatingLoadService>();
        services.AddScoped<BuildingEnergyBalanceService>();

        services.AddScoped<FloorCalculationService>();
        services.AddScoped<RoomCalculationService>();

        return services;
    }
}