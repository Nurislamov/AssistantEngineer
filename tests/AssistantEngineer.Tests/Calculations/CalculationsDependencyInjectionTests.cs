using AssistantEngineer.Modules.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Performance;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Standards;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Topology;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.Aggregation;
using AssistantEngineer.Modules.Calculations.Application.Services.Analytics;
using AssistantEngineer.Modules.Calculations.Application.Services.Buildings;
using AssistantEngineer.Modules.Calculations.Application.Services.Common.Profiles;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads.En12831;
using AssistantEngineer.Modules.Calculations.Application.Services.Governance;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Construction;
using AssistantEngineer.Modules.Calculations.Application.Services.Performance;
using AssistantEngineer.Modules.Calculations.Application.Services.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Services.Rooms;
using AssistantEngineer.Modules.Calculations.Application.Services.Validation.BuildingInput;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation.Iso16798;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests;

public class CalculationsDependencyInjectionTests
{
    [Fact]
    public void AddCalculationsModuleRegistersCalculationServices()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddCalculationsModule(configuration);

        AssertServiceLifetime<TimeProvider>(services, ServiceLifetime.Singleton);

        Assert.All(
            services.Where(service => service.ServiceType == typeof(IRoomCoolingLoadCalculationStrategy)),
            service => Assert.Equal(ServiceLifetime.Scoped, service.Lifetime));

        AssertServiceLifetime<IRoomCoolingLoadCalculator>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IAggregateLoadCalculator>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IAnnualProfileGenerator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<IAnnualProfileShapeValidator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<IStandardCalculationDisclosureFactory>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<IThermalTopologyBuilder>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<IThermalTopologyValidator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<IThermalBoundaryConditionResolver>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<IThermalZoneBoundaryCalculator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<IGroundGeometryNormalizer>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<IGroundBoundaryInputValidator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<IGroundTemperatureProfileProvider>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<IGroundBoundaryCalculator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<IGroundBoundaryTopologyMapper>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<IBuildingGroundBoundaryCalculator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<IGroundBoundaryTemperatureLookupBuilder>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<IThermalZoneGroundBoundaryInputAdapter>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<IGroundBoundaryToIso52016BoundaryProfileMapper>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<IIso16798ReferenceData>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<IBuildingEnvelopeReferenceData>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<En12831HeatingLoadCalculator>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IRoomHeatingLoadCalculator>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IBuildingHeatingLoadCalculator>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IIso52016ReferenceDataProvider>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<ISolarRadiationService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IWindowShadingService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IVentilationHeatTransferCalculator>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<INaturalVentilationOpeningGeometryNormalizer>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<INaturalVentilationInputValidator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<INaturalVentilationPressureCalculator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<INaturalVentilationAirflowCalculator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<INaturalVentilationControlRuleValidator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<INaturalVentilationOpeningControlEvaluator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<INaturalVentilationOpeningFractionProfileBuilder>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<INaturalVentilationControlledAirflowInputBuilder>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<INaturalVentilationZoneIntegrationValidator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<INaturalVentilationHourlyInputBuilder>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<INaturalVentilationZoneLoadCalculator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<IDomesticHotWaterDemandInputValidator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<IDomesticHotWaterDemandBasisCalculator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<IDomesticHotWaterDrawProfileBuilder>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<IDomesticHotWaterUsefulDemandCalculator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<IDomesticHotWaterSystemLossInputValidator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<IDomesticHotWaterStorageLossCalculator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<IDomesticHotWaterDistributionLossCalculator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<IDomesticHotWaterCirculationLossCalculator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<IDomesticHotWaterSystemLoadCalculator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<IDomesticHotWaterEn15316HandoffBuilder>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<ISystemEnergyUsefulLoadValidator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<ISystemEnergyModuleChainInputValidator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<ISystemEnergyModuleCalculator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<ISystemEnergyModuleChainCalculator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<ISystemEnergyGenerationHandoffBuilder>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<IDomesticHotWaterSystemEnergyHandoffAdapter>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<ISystemEnergyGeneratorInputValidator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<ISystemEnergyGeneratorLoadSplitter>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<ISystemEnergyGeneratorFinalEnergyCalculator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<ISystemEnergyFinalEnergyAggregator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<ISystemEnergyFinalEnergyCalculator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<ISystemEnergyFactorSetValidator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<ISystemEnergyEmissionCalculator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<ISystemEnergyPrimaryEnergyCalculator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<ISystemEnergyCalculationSummaryBuilder>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<ISystemEnergyDefaultFactorSetProvider>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<Iso16798NaturalVentilationCalculator>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<Iso16798NaturalVentilationApplicationAdapter>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<Iso52016HourlySteadyStateCalculator>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<Iso52016ConstructionReferenceDataProvider>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<Iso52016ConstructionAssemblyCalculator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<Iso52016ConstructionAssemblyApplicationAdapter>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<EngineeringStageManifestRegistryProvider>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<EngineeringStageManifestRegistryValidator>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<EngineeringClaimBoundaryScanner>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<EngineeringCoreV2ReleaseReadinessService>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<BuildingInputValidationService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<EnergySignatureService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IEnergyCarrierFactorProvider>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<BuildingEnergyPerformanceSummaryService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IBuildingEnergyCalculator>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<BuildingHeatingLoadService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<RoomCalculationService>(services, ServiceLifetime.Scoped);

        AssertServiceLifetime<ILoadCalculationsFacade>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IVentilationAnalysisFacade>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IDomesticHotWaterFacade>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IProfilesFacade>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IStandardReferenceDataFacade>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IBuildingEnergyAnalysisFacade>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IBuildingComfortAnalysisFacade>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IBuildingSizingAnalysisFacade>(services, ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddCalculationsModuleBindsCalculationOptionsFromConfiguration()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Calculations:CoolingLoad:DefaultCoolingSafetyFactor"] = "1.25",
                ["Calculations:Iso52016Cooling:DefaultDesignMonth"] = "8",
                ["Calculations:Iso52016EnergyNeed:DefaultWeatherYear"] = "2024",
                ["Calculations:HeatingLoad:DefaultAirChangesPerHour"] = "0.7",
                ["Calculations:Iso52016Construction:UseConstructionLayerMassInput"] = "true",
                ["Calculations:Iso52016Construction:DefaultInternalSurfaceResistanceM2KPerW"] = "0.12"
            })
            .Build();

        services.AddCalculationsModule(configuration);

        using var provider = services.BuildServiceProvider();

        Assert.Equal(
            1.25,
            provider.GetRequiredService<IOptions<CoolingLoadCalculationOptions>>().Value.DefaultCoolingSafetyFactor);

        Assert.Equal(
            8,
            provider.GetRequiredService<IOptions<Iso52016CoolingLoadOptions>>().Value.DefaultDesignMonth);

        Assert.Equal(
            2024,
            provider.GetRequiredService<IOptions<Iso52016EnergyNeedOptions>>().Value.DefaultWeatherYear);

        Assert.Equal(
            0.7,
            provider.GetRequiredService<IOptions<En12831HeatingLoadOptions>>().Value.DefaultAirChangesPerHour);

        Assert.True(
            provider.GetRequiredService<IOptions<Iso52016ConstructionOptions>>().Value.UseConstructionLayerMassInput);

        Assert.Equal(
            0.12,
            provider.GetRequiredService<IOptions<Iso52016ConstructionOptions>>().Value.DefaultInternalSurfaceResistanceM2KPerW);
    }

    [Fact]
    public void AddCalculationsModuleRejectsInvalidCriticalOptions()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Calculations:CoolingLoad:DefaultCoolingSafetyFactor"] = "0",
                ["Calculations:Iso52016EnergyNeed:DefaultWeatherYear"] = "1800",
                ["Calculations:Iso52016EnergyNeed:DefaultCoolingSetpointC"] = "27",
                ["Calculations:Iso52016EnergyNeed:DefaultCoolingSetbackC"] = "25",
                ["Calculations:NaturalVentilation:MinimumOutdoorTemperatureC"] = "30",
                ["Calculations:NaturalVentilation:MaximumOutdoorTemperatureC"] = "20",
                ["Calculations:Iso52016Construction:DefaultInternalSurfaceResistanceM2KPerW"] = "0"
            })
            .Build();

        services.AddCalculationsModule(configuration);

        using var provider = services.BuildServiceProvider();

        var coolingOptionsException = Assert.IsType<OptionsValidationException>(Record.Exception(() =>
            _ = provider.GetRequiredService<IOptions<CoolingLoadCalculationOptions>>().Value));

        Assert.Contains(
            coolingOptionsException.Failures,
            failure => failure.Contains("Calculations:CoolingLoad:DefaultCoolingSafetyFactor", StringComparison.Ordinal));

        var energyOptionsException = Assert.IsType<OptionsValidationException>(Record.Exception(() =>
            _ = provider.GetRequiredService<IOptions<Iso52016EnergyNeedOptions>>().Value));

        Assert.Contains(
            energyOptionsException.Failures,
            failure => failure.Contains("Calculations:Iso52016EnergyNeed:DefaultWeatherYear", StringComparison.Ordinal));

        Assert.Contains(
            energyOptionsException.Failures,
            failure => failure.Contains("DefaultCoolingSetbackC", StringComparison.Ordinal));

        var ventilationOptionsException = Assert.IsType<OptionsValidationException>(Record.Exception(() =>
            _ = provider.GetRequiredService<IOptions<NaturalVentilationOptions>>().Value));

        Assert.Contains(
            ventilationOptionsException.Failures,
            failure => failure.Contains("MinimumOutdoorTemperatureC cannot exceed MaximumOutdoorTemperatureC", StringComparison.Ordinal));

        var constructionOptionsException = Assert.IsType<OptionsValidationException>(Record.Exception(() =>
            _ = provider.GetRequiredService<IOptions<Iso52016ConstructionOptions>>().Value));

        Assert.Contains(
            constructionOptionsException.Failures,
            failure => failure.Contains("Calculations:Iso52016Construction:DefaultInternalSurfaceResistanceM2KPerW", StringComparison.Ordinal));
    }

    [Fact]
    public void AddCalculationsModuleDoesNotDuplicateMainServiceRegistrations()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddCalculationsModule(configuration);

        AssertSingleRegistration<ILoadCalculationsFacade>(services);
        AssertSingleRegistration<IVentilationAnalysisFacade>(services);
        AssertSingleRegistration<IDomesticHotWaterFacade>(services);
        AssertSingleRegistration<IProfilesFacade>(services);
        AssertSingleRegistration<IStandardReferenceDataFacade>(services);
        AssertSingleRegistration<IBuildingEnergyAnalysisFacade>(services);
        AssertSingleRegistration<IBuildingComfortAnalysisFacade>(services);
        AssertSingleRegistration<IBuildingSizingAnalysisFacade>(services);
        AssertSingleRegistration<IBuildingEnergyCalculator>(services);
        AssertSingleRegistration<IRoomCoolingLoadCalculator>(services);
        AssertSingleRegistration<IAggregateLoadCalculator>(services);
        AssertSingleRegistration<IRoomHeatingLoadCalculator>(services);
        AssertSingleRegistration<IBuildingHeatingLoadCalculator>(services);
    }

    private static void AssertServiceLifetime<TService>(
        IServiceCollection services,
        ServiceLifetime expectedLifetime)
    {
        var descriptor = services.LastOrDefault(service => service.ServiceType == typeof(TService));

        Assert.NotNull(descriptor);
        Assert.Equal(expectedLifetime, descriptor.Lifetime);
    }

    private static void AssertSingleRegistration<TService>(
        IServiceCollection services)
    {
        var registrations = services
            .Where(service => service.ServiceType == typeof(TService))
            .ToArray();

        Assert.Single(registrations);
    }
}
