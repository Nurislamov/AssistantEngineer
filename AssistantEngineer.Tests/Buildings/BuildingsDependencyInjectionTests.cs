using AssistantEngineer.Modules.Buildings;
using AssistantEngineer.Modules.Buildings.Application.Options;
using AssistantEngineer.Modules.Buildings.Application.Services.Buildings;
using AssistantEngineer.Modules.Buildings.Application.Services.Climate;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests;

public class BuildingsDependencyInjectionTests
{
    [Fact]
    public void AddBuildingsModuleRegistersBuildingsServicesAsScoped()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddBuildingsModule(configuration);

        AssertServiceLifetime<BuildingCalculationReadinessService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<BuildingArchetypeService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<EpwWeatherImportService>(services, ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddBuildingsModuleBindsArchetypeCatalogOptions()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Buildings:ArchetypeCatalog:Archetypes:0:Code"] = "test-office",
                ["Buildings:ArchetypeCatalog:Archetypes:0:DisplayName"] = "Test office",
                ["Buildings:ArchetypeCatalog:Archetypes:0:RoomsCount"] = "2",
                ["Buildings:ArchetypeCatalog:Archetypes:0:RoomAreaM2"] = "25"
            })
            .Build();

        services.AddBuildingsModule(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<BuildingArchetypeCatalogOptions>>().Value;
        Assert.Single(options.Archetypes);
        Assert.Equal("test-office", options.Archetypes[0].Code);
        Assert.Equal(2, options.Archetypes[0].RoomsCount);
        Assert.Equal(25, options.Archetypes[0].RoomAreaM2);
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
