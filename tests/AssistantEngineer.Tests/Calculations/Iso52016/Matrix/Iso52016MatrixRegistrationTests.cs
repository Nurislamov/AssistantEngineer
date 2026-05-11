using AssistantEngineer.Modules.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.Calculations.Iso52016.Matrix;

public class Iso52016MatrixRegistrationTests
{
    [Fact]
    public void AddCalculationsModule_RegistersIso52016MatrixServices()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        services.AddCalculationsModule(configuration);

        AssertScoped<ISo52016MatrixHourlySolver, Iso52016MatrixHourlySolver>(services);
        AssertScoped<ISo52016InternalGainReferenceDataProvider, Iso52016InternalGainReferenceDataProvider>(services);
        AssertScoped<ISo52016AdjacentUnconditionedZoneTemperatureSolver, Iso52016AdjacentUnconditionedZoneTemperatureSolver>(services);
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