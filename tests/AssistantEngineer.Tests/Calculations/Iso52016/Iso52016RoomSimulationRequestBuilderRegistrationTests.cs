using AssistantEngineer.Modules.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016RoomSimulationRequestBuilderRegistrationTests
{
    [Fact]
    public void AddCalculationsModule_RegistersRoomSimulationRequestBuilderServices()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        services.AddCalculationsModule(configuration);

        AssertRegistered<ISo52016ScheduleProfileExpander, Iso52016ScheduleProfileExpander>(services);
        AssertRegistered<ISo52016RoomEnvelopeInputCalculator, Iso52016RoomEnvelopeInputCalculator>(services);
        AssertRegistered<ISo52016RoomWindowSolarGainInputMapper, Iso52016RoomWindowSolarGainInputMapper>(services);
        AssertRegistered<ISo52016RoomEnergySimulationRequestBuilder, Iso52016RoomEnergySimulationRequestBuilder>(services);
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