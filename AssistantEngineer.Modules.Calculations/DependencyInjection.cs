using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Performance;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Services.Analytics;
using AssistantEngineer.Modules.Calculations.Application.Services.Buildings;
using AssistantEngineer.Modules.Calculations.Application.Services.Common.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Services.Comfort;
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
using AssistantEngineer.Modules.Calculations.Application.Services.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Services.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Services.Rooms;
using AssistantEngineer.Modules.Calculations.Application.Services.Sizing;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Calculations;

public static class DependencyInjection
{
    public static IServiceCollection AddCalculationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<IValidateOptions<CoolingLoadCalculationOptions>, CoolingLoadCalculationOptionsValidator>();
        services.AddSingleton<IValidateOptions<Iso52016CoolingLoadOptions>, Iso52016CoolingLoadOptionsValidator>();
        services.AddSingleton<IValidateOptions<Iso52016EnergyNeedOptions>, Iso52016EnergyNeedOptionsValidator>();
        services.AddSingleton<IValidateOptions<Iso52016MonthlyEnergyNeedOptions>, Iso52016MonthlyEnergyNeedOptionsValidator>();
        services.AddSingleton<IValidateOptions<En12831HeatingLoadOptions>, En12831HeatingLoadOptionsValidator>();
        services.AddSingleton<IValidateOptions<En16798ProfileOptions>, En16798ProfileOptionsValidator>();
        services.AddSingleton<IValidateOptions<NaturalVentilationOptions>, NaturalVentilationOptionsValidator>();

        services
            .AddOptions<CoolingLoadCalculationOptions>()
            .Bind(configuration.GetSection("Calculations:CoolingLoad"))
            .ValidateOnStart();

        services
            .AddOptions<Iso52016CoolingLoadOptions>()
            .Bind(configuration.GetSection("Calculations:Iso52016Cooling"))
            .ValidateOnStart();

        services
            .AddOptions<Iso52016EnergyNeedOptions>()
            .Bind(configuration.GetSection("Calculations:Iso52016EnergyNeed"))
            .ValidateOnStart();

        services
            .AddOptions<Iso52016MonthlyEnergyNeedOptions>()
            .Bind(configuration.GetSection("Calculations:Iso52016MonthlyEnergyNeed"))
            .ValidateOnStart();

        services
            .AddOptions<En12831HeatingLoadOptions>()
            .Bind(configuration.GetSection("Calculations:HeatingLoad"))
            .ValidateOnStart();

        services
            .AddOptions<En16798ProfileOptions>()
            .Bind(configuration.GetSection("Calculations:En16798Profiles"))
            .ValidateOnStart();

        services
            .AddOptions<NaturalVentilationOptions>()
            .Bind(configuration.GetSection("Calculations:NaturalVentilation"))
            .ValidateOnStart();

        services.AddSingleton<IHourlyProfileAggregator, HourlyProfileAggregator>();
        services.AddSingleton<IAnnualProfileGenerator, AnnualProfileGenerator>();
        services.AddSingleton<IIso16798ReferenceData, Iso16798ReferenceData>();
        services.AddSingleton<IEn16798ProfileCatalog, En16798ProfileCatalog>();
        services.AddSingleton<IBuildingEnvelopeReferenceData, BuildingEnvelopeReferenceData>();
        services.AddSingleton<ICoolingLoadReferenceData, CoolingLoadReferenceData>();

        services.AddScoped<IRoomCoolingLoadCalculationStrategy, SimplifiedCoolingLoadCalculator>();
        services.AddScoped<IRoomCoolingLoadCalculationStrategy, Iso52016CoolingLoadCalculator>();
        services.AddScoped<IRoomCoolingLoadCalculator, RoomCoolingLoadCalculator>();

        services.AddScoped<IAggregateLoadCalculator, AggregateCalculator>();

        services.AddScoped<En12831HeatingLoadCalculator>();
        services.AddScoped<BuildingHeatingReadModelCalculator>();
        services.AddScoped<IRoomHeatingLoadCalculator>(sp => sp.GetRequiredService<En12831HeatingLoadCalculator>());
        services.AddScoped<IBuildingHeatingLoadCalculator>(sp => sp.GetRequiredService<En12831HeatingLoadCalculator>());

        services.AddScoped<IIso52016ReferenceDataProvider, Iso52016ReferenceDataProvider>();
        services.AddScoped<Iso52016ClimateDataValidator>();
        services.AddScoped<ISolarRadiationService, SolarRadiationService>();
        services.AddScoped<IWindowShadingService, WindowShadingService>();
        services.AddScoped<IVentilationHeatTransferCalculator, VentilationHeatTransferCalculator>();
        services.AddScoped<INaturalVentilationOpeningControlService, NaturalVentilationOpeningControlService>();
        services.AddScoped<INaturalVentilationAirflowService, NaturalVentilationAirflowService>();
        services.AddScoped<NaturalVentilationPreviewService>();
        services.AddScoped<Iso52016HourlySteadyStateCalculator>();
        services.AddScoped<Iso52016MonthlyQuasiSteadyStateCalculator>();

        services.AddScoped<En16798ProfileService>();
        services.AddScoped<BuildingComfortMetricsService>();
        services.AddScoped<BuildingZoneComfortMetricsService>();
        services.AddScoped<BuildingRoomComfortMetricsService>();
        services.AddScoped<EnergySignatureService>();
        services.AddScoped<DomesticHotWaterDemandService>();
        services.AddScoped<HeatingSystemEnergyService>();
        services.AddScoped<CoolingSystemEnergyService>();
        services.AddSingleton<IEnergyCarrierFactorProvider, EnergyCarrierFactorProvider>();
        services.AddScoped<BuildingEnergyPerformanceSummaryService>();
        services.AddScoped<BuildingPerformanceService>();
        services.AddScoped<BuildingPeakSizingService>();
        services.AddScoped<BuildingReferenceDesignDayService>();
        services.AddScoped<BuildingSyntheticDesignDayService>();
        services.AddScoped<BuildingAutosizingService>();
        services.AddScoped<CatalogAutosizingRankingService>();
        services.AddScoped<BuildingCatalogAutosizingService>();
        services.AddScoped<EquipmentRecommendationService>();
        services.AddScoped<EquipmentRecommendationComparisonService>();
        services.AddScoped<EquipmentRecommendationReportService>();
        services.AddScoped<EquipmentRecommendationComparisonReportService>();
        
        services.AddScoped<IBuildingEnergyCalculator, Iso52016BuildingEnergyCalculator>();
        services.AddScoped<ICalculationsFacade, CalculationsFacade>();
        services.AddScoped<IBuildingEnergyAnalysisFacade, BuildingEnergyAnalysisFacade>();
        services.AddScoped<IBuildingComfortAnalysisFacade, BuildingComfortAnalysisFacade>();
        services.AddScoped<IBuildingSizingAnalysisFacade, BuildingSizingAnalysisFacade>();
        
        services.AddScoped<BuildingCoolingLoadService>();
        services.AddScoped<BuildingHeatingLoadService>();
        services.AddScoped<BuildingEnergyBalanceService>();

        services.AddScoped<FloorCalculationService>();
        services.AddScoped<RoomCalculationService>();

        return services;
    }
}
