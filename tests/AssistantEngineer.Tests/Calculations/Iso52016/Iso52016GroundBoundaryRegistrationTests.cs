using AssistantEngineer.Modules.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.Calculations.Iso52016;

public class Iso52016GroundBoundaryRegistrationTests
{
    [Fact]
    public void AddCalculationsModule_RegistersGroundBoundaryTemperatureProvider()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        services.AddCalculationsModule(configuration);

        var descriptor = services.LastOrDefault(service =>
            service.ServiceType == typeof(IIso52016GroundBoundaryTemperatureProvider));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(PeriodicIso52016GroundBoundaryTemperatureProvider), descriptor.ImplementationType);
    }
}