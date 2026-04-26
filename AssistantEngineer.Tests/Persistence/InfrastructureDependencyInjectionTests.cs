using AssistantEngineer.Infrastructure;
using AssistantEngineer.Infrastructure.Integrations.Benchmarks;
using AssistantEngineer.Infrastructure.Integrations.Reports.Excel;
using AssistantEngineer.Infrastructure.Persistence;
using AssistantEngineer.Modules.Benchmarks.Application.Abstractions;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Heating;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Sizing;
using AssistantEngineer.Modules.Equipment.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Reporting.Application.Abstractions;
using AssistantEngineer.SharedKernel.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests;

public class InfrastructureDependencyInjectionTests
{
    [Fact]
    public void AddInfrastructureBindsEnergyPlusOptionsFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=AssistantEngineerTests;Username=postgres",
                ["EnergyPlus:UseDocker"] = "false",
                ["EnergyPlus:ExecutablePath"] = Environment.ProcessPath,
                ["EnergyPlus:DockerUri"] = "tcp://localhost:2375",
                ["EnergyPlus:DockerImage"] = "custom-energyplus:latest",
                ["EnergyPlus:MaxCapturedLogCharacters"] = "4096",
                ["EnergyPlus:ExecutionTimeoutSeconds"] = "1200",
                ["EnergyPlus:MaxRetryAttempts"] = "2"
            })
            .Build();

        var services = new ServiceCollection();

        services.AddInfrastructure(configuration, "Testing");

        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<EnergyPlusBenchmarkOptions>>().Value;

        Assert.False(options.UseDocker);
        Assert.Equal(Environment.ProcessPath, options.ExecutablePath);
        Assert.Equal("tcp://localhost:2375", options.DockerUri);
        Assert.Equal("custom-energyplus:latest", options.DockerImage);
        Assert.Equal(4096, options.MaxCapturedLogCharacters);
        Assert.Equal(1200, options.ExecutionTimeoutSeconds);
        Assert.Equal(2, options.MaxRetryAttempts);
    }

    [Fact]
    public void AddInfrastructureRejectsInvalidEnergyPlusOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=AssistantEngineerTests;Username=postgres",
                ["EnergyPlus:UseDocker"] = "false",
                ["EnergyPlus:ExecutablePath"] = "",
                ["EnergyPlus:ExecutionTimeoutSeconds"] = "0"
            })
            .Build();

        var services = new ServiceCollection();

        services.AddInfrastructure(configuration, "Testing");

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<EnergyPlusBenchmarkOptions>>().Value);

        Assert.Contains(
            exception.Failures,
            failure => failure.Contains("EnergyPlus:ExecutablePath", StringComparison.Ordinal));

        Assert.Contains(
            exception.Failures,
            failure => failure.Contains("EnergyPlus:ExecutionTimeoutSeconds", StringComparison.Ordinal));
    }

    [Fact]
    public void AddInfrastructureRejectsJsonBackedSecretsOutsideDevelopment()
    {
        using var json = new MemoryStream("""
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=db;Database=AssistantEngineer;Username=app;Password=secret"
  }
}
"""u8.ToArray());

        var configuration = new ConfigurationBuilder()
            .AddJsonStream(json)
            .Build();

        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddInfrastructure(configuration, "Production"));

        Assert.Contains(
            "ConnectionStrings:DefaultConnection",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void AddInfrastructureAllowsSecretOverridesFromHigherPriorityProviders()
    {
        using var json = new MemoryStream("""
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=db;Database=AssistantEngineer;Username=app;Password=secret"
  }
}
"""u8.ToArray());

        var configuration = new ConfigurationBuilder()
            .AddJsonStream(json)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=db;Database=AssistantEngineer;Username=app"
            })
            .Build();

        var services = new ServiceCollection();

        services.AddInfrastructure(configuration, "Production");

        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<EnergyPlusBenchmarkOptions>>().Value;

        Assert.NotNull(options);
    }

    [Fact]
    public void AddInfrastructureRegistersAllInfrastructureAdapters()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=AssistantEngineerTests;Username=postgres"
            })
            .Build();

        var services = new ServiceCollection();

        services.AddInfrastructure(configuration, "Testing");

        AssertServiceLifetime<AppDbContext>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IUnitOfWork>(services, ServiceLifetime.Scoped);

        AssertServiceLifetime<IProjectRepository>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IBuildingRepository>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IFloorRepository>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IRoomRepository>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IClimateZoneRepository>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IClimateDataRepository>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IBuildingHeatingReadModelRepository>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IAnnualClimateDataRepository>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<ICalculationPreferencesRepository>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IEquipmentCatalogRepository>(services, ServiceLifetime.Scoped);

        AssertServiceLifetime<IAnnualClimateDataProvider>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<ICoolingEquipmentCatalogSizingProvider>(services, ServiceLifetime.Scoped);

        AssertServiceLifetime<IBuildingCoolingReportExporter>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IBuildingEnergyBalanceReportExporter>(services, ServiceLifetime.Scoped);

        AssertServiceLifetime<IEnergyPlusArtifactStore>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IEnergyPlusModelExporter>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IEnergyPlusResultParser>(services, ServiceLifetime.Scoped);
        AssertServiceLifetime<IEnergyPlusBenchmarkRunner>(services, ServiceLifetime.Scoped);
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
