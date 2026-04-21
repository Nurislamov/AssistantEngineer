using AssistantEngineer.Modules.Buildings;
using AssistantEngineer.Modules.Buildings.Application.Services.Buildings;
using AssistantEngineer.Modules.Buildings.Application.Services.Climate;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests;

public class BuildingsDependencyInjectionTests
{
    [Fact]
    public void AddBuildingsModuleRegistersBuildingsServicesAsScoped()
    {
        var services = new ServiceCollection();

        services.AddBuildingsModule();

        AssertServiceLifetime<BuildingCalculationReadinessService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<BuildingArchetypeService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<EpwWeatherImportService>(services, ServiceLifetime.Scoped);
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
