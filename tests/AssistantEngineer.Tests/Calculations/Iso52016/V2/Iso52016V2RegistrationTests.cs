using AssistantEngineer.Modules.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.V2;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.V2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.Calculations.Iso52016.V2;

public class Iso52016V2RegistrationTests
{
    [Fact]
    public void AddCalculationsModule_RegistersIso52016V2Services()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        services.AddCalculationsModule(configuration);

        AssertScoped<IIso52016V2HourlySolver, Iso52016V2HourlySolver>(services);
        AssertScoped<IIso52016InternalGainReferenceDataProvider, Iso52016InternalGainReferenceDataProvider>(services);
        AssertScoped<IIso52016AdjacentUnconditionedZoneTemperatureSolver, Iso52016AdjacentUnconditionedZoneTemperatureSolver>(services);
    }

    private static void AssertScoped<TService, TImplementation>(IServiceCollection services)
    {
        var descriptor = services.LastOrDefault(service =>
            service.ServiceType == typeof(TService));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(TImplementation), descriptor.ImplementationType);
    }
}