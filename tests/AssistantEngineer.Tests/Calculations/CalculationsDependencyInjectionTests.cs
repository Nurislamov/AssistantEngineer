using AssistantEngineer.Modules.Calculations;
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
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingLoads.En12831;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Performance;
using AssistantEngineer.Modules.Calculations.Application.Services.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Services.Rooms;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;
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
        AssertServiceLifetime<IIso16798ReferenceData>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<IBuildingEnvelopeReferenceData>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<En12831HeatingLoadCalculator>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IRoomHeatingLoadCalculator>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IBuildingHeatingLoadCalculator>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IIso52016ReferenceDataProvider>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<ISolarRadiationService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IWindowShadingService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IVentilationHeatTransferCalculator>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<Iso52016HourlySteadyStateCalculator>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<EnergySignatureService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IEnergyCarrierFactorProvider>(services, ServiceLifetime.Singleton);
        AssertServiceLifetime<BuildingEnergyPerformanceSummaryService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IBuildingEnergyCalculator>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<BuildingCoolingLoadService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<BuildingHeatingLoadService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<BuildingEnergyBalanceService>(services, ServiceLifetime.Scoped);
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
                ["Calculations:HeatingLoad:DefaultAirChangesPerHour"] = "0.7"
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
                ["Calculations:NaturalVentilation:MaximumOutdoorTemperatureC"] = "20"
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