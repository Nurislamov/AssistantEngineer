using AssistantEngineer.Modules.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Solar;
using AssistantEngineer.Modules.Calculations.Application.Services.Solar;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.Calculations.Solar;

public class SolarRegistrationTests
{
    [Fact]
    public void AddCalculationsModule_RegistersSolarTimeCalculator()
    {
        var services = CreateServices();

        var descriptor = services.LastOrDefault(service =>
            service.ServiceType == typeof(ISolarTimeCalculator));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(SolarTimeCalculator), descriptor.ImplementationType);
    }

    [Fact]
    public void AddCalculationsModule_RegistersSolarPositionCalculator()
    {
        var services = CreateServices();

        var descriptor = services.LastOrDefault(service =>
            service.ServiceType == typeof(ISolarPositionCalculator));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(SolarPositionCalculator), descriptor.ImplementationType);
    }

    [Fact]
    public void AddCalculationsModule_RegistersPerezSurfaceIrradianceCalculatorAsProductionCalculator()
    {
        var services = CreateServices();

        var descriptor = services.LastOrDefault(service =>
            service.ServiceType == typeof(ISurfaceIrradianceCalculator));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(PerezAnisotropicSurfaceIrradianceCalculator), descriptor.ImplementationType);
    }

    private static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        services.AddCalculationsModule(configuration);

        return services;
    }
}
