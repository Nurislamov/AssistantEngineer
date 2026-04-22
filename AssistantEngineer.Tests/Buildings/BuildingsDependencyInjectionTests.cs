using AssistantEngineer.Modules.Buildings;
using AssistantEngineer.Modules.Buildings.Application.Facades;
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
        AssertServiceLifetime<EpwAnnualClimateDataImportService>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IProjectsFacade>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IBuildingArchetypesFacade>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IBuildingReadinessFacade>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IAnnualClimateDataFacade>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IValidateOptions<BuildingArchetypeCatalogOptions>>(services, ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddBuildingsModuleBindsArchetypeCatalogOptions()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(CreateValidArchetypeConfiguration())
            .Build();

        services.AddBuildingsModule(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<BuildingArchetypeCatalogOptions>>().Value;
        Assert.Single(options.Archetypes);
        Assert.Equal("test-office", options.Archetypes[0].Code);
        Assert.Equal(2, options.Archetypes[0].RoomsCount);
        Assert.Equal(25, options.Archetypes[0].RoomAreaM2);
    }

    [Fact]
    public void AddBuildingsModuleRejectsInvalidArchetypeCatalogOptions()
    {
        var services = new ServiceCollection();
        var configurationValues = CreateValidArchetypeConfiguration();
        configurationValues["Buildings:ArchetypeCatalog:Archetypes:0:RoomsCount"] = "0";
        configurationValues["Buildings:ArchetypeCatalog:Archetypes:0:RoomAreaM2"] = "-25";
        configurationValues["Buildings:ArchetypeCatalog:Archetypes:0:WindowShgc"] = "1.2";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();

        services.AddBuildingsModule(configuration);

        using var provider = services.BuildServiceProvider();
        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<BuildingArchetypeCatalogOptions>>().Value);

        Assert.Contains(exception.Failures, failure => failure.Contains("Archetypes[0].RoomsCount"));
        Assert.Contains(exception.Failures, failure => failure.Contains("Archetypes[0].RoomAreaM2"));
        Assert.Contains(exception.Failures, failure => failure.Contains("Archetypes[0].WindowShgc"));
    }

    private static void AssertServiceLifetime<TService>(
        IServiceCollection services,
        ServiceLifetime expectedLifetime)
    {
        var descriptor = services.LastOrDefault(service => service.ServiceType == typeof(TService));

        Assert.NotNull(descriptor);
        Assert.Equal(expectedLifetime, descriptor.Lifetime);
    }

    private static Dictionary<string, string?> CreateValidArchetypeConfiguration() =>
        new()
        {
            ["Buildings:ArchetypeCatalog:Archetypes:0:Code"] = "test-office",
            ["Buildings:ArchetypeCatalog:Archetypes:0:DisplayName"] = "Test office",
            ["Buildings:ArchetypeCatalog:Archetypes:0:Type"] = "Office",
            ["Buildings:ArchetypeCatalog:Archetypes:0:RoomsCount"] = "2",
            ["Buildings:ArchetypeCatalog:Archetypes:0:RoomAreaM2"] = "25",
            ["Buildings:ArchetypeCatalog:Archetypes:0:RoomHeightM"] = "3",
            ["Buildings:ArchetypeCatalog:Archetypes:0:IndoorTemperatureC"] = "22",
            ["Buildings:ArchetypeCatalog:Archetypes:0:PeopleCount"] = "1",
            ["Buildings:ArchetypeCatalog:Archetypes:0:EquipmentLoadWPerM2"] = "8",
            ["Buildings:ArchetypeCatalog:Archetypes:0:LightingLoadWPerM2"] = "7",
            ["Buildings:ArchetypeCatalog:Archetypes:0:ExternalWallAreaFactor"] = "0.8",
            ["Buildings:ArchetypeCatalog:Archetypes:0:ExternalWallUValue"] = "1.2",
            ["Buildings:ArchetypeCatalog:Archetypes:0:WindowAreaM2Minimum"] = "1.5",
            ["Buildings:ArchetypeCatalog:Archetypes:0:WindowAreaFactor"] = "0.12",
            ["Buildings:ArchetypeCatalog:Archetypes:0:WindowUValue"] = "2.2",
            ["Buildings:ArchetypeCatalog:Archetypes:0:WindowShgc"] = "0.5",
            ["Buildings:ArchetypeCatalog:Archetypes:0:OddRoomOrientation"] = "South",
            ["Buildings:ArchetypeCatalog:Archetypes:0:EvenRoomOrientation"] = "East"
        };
}
