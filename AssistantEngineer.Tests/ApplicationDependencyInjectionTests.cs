using AssistantEngineer.Application;
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
using AssistantEngineer.Application.Services.Calculations.Performance;
using AssistantEngineer.Application.Services.Calculations.ReferenceData;
using AssistantEngineer.Application.Services.Calculations.Ventilation;
using AssistantEngineer.Application.Services.Climate;
using AssistantEngineer.Application.Services.Equipment;
using AssistantEngineer.Application.Services.Reports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests;

public class ApplicationDependencyInjectionTests
{
    [Fact]
    public void AddApplicationRegistersCalculationServicesAsScoped()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddApplication(configuration);

        AssertServiceLifetime<TimeProvider>(services, ServiceLifetime.Singleton);
        Assert.All(
            services.Where(service => service.ServiceType == typeof(IRoomCoolingLoadCalculationStrategy)),
            service => Assert.Equal(ServiceLifetime.Scoped, service.Lifetime));
        AssertServiceLifetime<IRoomCoolingLoadCalculator>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IAggregateLoadCalculator>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IAnnualProfileGenerator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<IIso16798ReferenceData>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<En12831HeatingLoadCalculator>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IRoomHeatingLoadCalculator>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IBuildingHeatingLoadCalculator>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<ICoolingEquipmentSelector>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IIso52016ReferenceDataProvider>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<ISolarRadiationService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IWindowShadingService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IVentilationHeatTransferCalculator>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<Iso52016HourlySteadyStateCalculator>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<EnergySignatureService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<DomesticHotWaterDemandService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<HeatingSystemEnergyService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<CoolingSystemEnergyService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IEnergyCarrierFactorProvider>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<BuildingEnergyPerformanceSummaryService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IBuildingEnergyCalculator>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<EnergyPlusModelExportService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<VerificationService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<Iso52016ReferenceBenchmarkService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<BuildingCoolingLoadService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<BuildingHeatingLoadService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<BuildingEnergyBalanceService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<BuildingCalculationReadinessService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<BuildingArchetypeService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<BuildingReportCalculationService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<BuildingReportGenerator>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<BuildingReportDataService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<EpwWeatherImportService>(services, ServiceLifetime.Scoped);
    }

    private static void AssertServiceLifetime<TService>(
        IServiceCollection services,
        ServiceLifetime expectedLifetime)
    {
        var descriptor = services.LastOrDefault(service => service.ServiceType == typeof(TService));

        Assert.NotNull(descriptor);
        Assert.Equal(expectedLifetime, descriptor.Lifetime);
    }
}


