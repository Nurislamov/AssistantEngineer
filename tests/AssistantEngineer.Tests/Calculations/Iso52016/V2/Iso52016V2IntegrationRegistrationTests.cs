using AssistantEngineer.Modules.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.V2;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.V2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.Calculations.Iso52016.V2;

public class Iso52016V2IntegrationRegistrationTests
{
    [Fact]
    public void AddCalculationsModule_RegistersV2ReducedRoomModelBuilder()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        services.AddCalculationsModule(configuration);

        var descriptor = services.LastOrDefault(service =>
            service.ServiceType == typeof(IIso52016V2ReducedRoomModelBuilder));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(Iso52016V2ReducedRoomModelBuilder), descriptor.ImplementationType);
    }

    [Fact]
    public void AddCalculationsModule_RegistersV2RoomEnergySimulationService()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        services.AddCalculationsModule(configuration);

        var descriptor = services.LastOrDefault(service =>
            service.ServiceType == typeof(IIso52016V2RoomEnergySimulationService));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(Iso52016V2RoomEnergySimulationService), descriptor.ImplementationType);
    }
}