using System.Reflection;
using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Application.Services.Benchmarks;
using AssistantEngineer.Application.Services.Buildings;
using AssistantEngineer.Application.Services.Calculations;
using AssistantEngineer.Application.Services.Calculations.Analytics;
using AssistantEngineer.Application.Services.Calculations.Common.Profiles;
using AssistantEngineer.Application.Services.Calculations.CoolingSystems;
using AssistantEngineer.Application.Services.Calculations.DomesticHotWater;
using AssistantEngineer.Application.Services.Calculations.HeatingSystems;
using AssistantEngineer.Application.Services.Calculations.Iso52016;
using AssistantEngineer.Application.Services.Calculations.CoolingLoads.Iso52016;
using AssistantEngineer.Application.Services.Calculations.Performance;
using AssistantEngineer.Application.Services.Calculations.ReferenceData;
using AssistantEngineer.Application.Services.Calculations.Ventilation;
using AssistantEngineer.Application.Services.Climate;
using AssistantEngineer.Application.Services.Equipment;
using AssistantEngineer.Application.Services.Floors;
using AssistantEngineer.Application.Services.Projects;
using AssistantEngineer.Application.Services.Reports;
using AssistantEngineer.Application.Services.Rooms;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddSingleton(TimeProvider.System);
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddSingleton(new CoolingLoadCalculationOptions());
        services.AddSingleton(new Iso52016CoolingLoadOptions());
        services.AddSingleton(new Iso52016EnergyNeedOptions());
        services.AddSingleton<IHourlyProfileAggregator, HourlyProfileAggregator>();
        services.AddSingleton<IAnnualProfileGenerator, AnnualProfileGenerator>();
        services.AddSingleton<IIso16798ReferenceData, Iso16798ReferenceData>();
        services.AddSingleton<ICoolingLoadReferenceData, CoolingLoadReferenceData>();
        services.AddScoped<IRoomCoolingLoadCalculationStrategy, SimplifiedCoolingLoadCalculator>();
        services.AddScoped<IRoomCoolingLoadCalculationStrategy, Iso52016CoolingLoadCalculator>();
        services.AddScoped<IRoomCoolingLoadCalculator, RoomCoolingLoadCalculator>();
        services.AddScoped<IAggregateLoadCalculator, AggregateCalculator>();
        services.AddSingleton(new En12831HeatingLoadOptions());
        services.AddScoped<En12831HeatingLoadCalculator>();
        services.AddScoped<IRoomHeatingLoadCalculator>(sp => sp.GetRequiredService<En12831HeatingLoadCalculator>());
        services.AddScoped<IBuildingHeatingLoadCalculator>(sp => sp.GetRequiredService<En12831HeatingLoadCalculator>());
        services.AddScoped<ICoolingEquipmentSelector, CoolingEquipmentSelector>();
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
        services.AddScoped<IBuildingEnergyCalculator, Iso52016BuildingEnergyCalculator>();
        services.AddScoped<IVerificationComparator, VerificationComparator>();
        services.AddScoped<EnergyPlusModelExportService>();
        services.AddScoped<VerificationService>();
        services.AddScoped<Iso52016ReferenceBenchmarkService>();
        services.Configure<VerificationTolerance>(options =>
        {
            configuration.GetSection("Verification").Bind(options);
        });

        services.AddScoped<ProjectCommandService>();
        services.AddScoped<ProjectQueryService>();
        services.AddScoped<BuildingCommandService>();
        services.AddScoped<BuildingQueryService>();
        services.AddScoped<BuildingCoolingLoadService>();
        services.AddScoped<BuildingHeatingLoadService>();
        services.AddScoped<BuildingEnergyBalanceService>();
        services.AddScoped<BuildingCalculationReadinessService>();
        services.AddScoped<BuildingArchetypeService>();
        services.AddScoped<FloorCommandService>();
        services.AddScoped<FloorQueryService>();
        services.AddScoped<RoomCommandService>();
        services.AddScoped<RoomQueryService>();
        services.AddScoped<CoolingEquipmentCatalogCommandService>();
        services.AddScoped<CoolingEquipmentCatalogQueryService>();
        services.AddScoped<EquipmentSelectionService>();
        services.AddScoped<BuildingReportCalculationService>();
        services.AddScoped<BuildingReportGenerator>();
        services.AddScoped<BuildingReportDataService>();
        services.AddScoped<EpwWeatherImportService>();

        return services;
    }
}


