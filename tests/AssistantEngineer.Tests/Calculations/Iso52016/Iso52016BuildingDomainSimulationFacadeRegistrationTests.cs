using AssistantEngineer.Modules.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016BuildingDomainSimulationFacadeRegistrationTests
{
    [Fact]
    public void AddCalculationsModule_RegistersBuildingDomainSimulationFacadeServices()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        services.AddCalculationsModule(configuration);

        AssertRegistered<ISo52016BuildingRoomCollector, Iso52016BuildingRoomCollector>(services);
        AssertRegistered<ISo52016BuildingDomainSimulationFacade, Iso52016BuildingDomainSimulationFacade>(services);
    }

    private static void AssertRegistered<TService, TImplementation>(
        IServiceCollection services)
    {
        var descriptor = services.LastOrDefault(service =>
            service.ServiceType == typeof(TService));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(TImplementation), descriptor.ImplementationType);
    }
}