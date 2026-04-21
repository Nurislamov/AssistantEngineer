using AssistantEngineer.Modules.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Abstractions;
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
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
