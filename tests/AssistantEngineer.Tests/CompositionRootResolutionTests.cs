using AssistantEngineer.Api;
using AssistantEngineer.Api.Services.Calculations;
using AssistantEngineer.Api.Services.Calculations.Persistence;
using AssistantEngineer.Api.Services.Calculations.Workflow;
using AssistantEngineer.Infrastructure;
using AssistantEngineer.Modules.Benchmarks;
using AssistantEngineer.Modules.Benchmarks.Application.Facades;
using AssistantEngineer.Modules.Buildings;
using AssistantEngineer.Modules.Buildings.Application.Facades;
using AssistantEngineer.Modules.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using AssistantEngineer.Modules.Equipment;
using AssistantEngineer.Modules.Equipment.Application.Facades;
using AssistantEngineer.Modules.Reporting;
using AssistantEngineer.Modules.Reporting.Application.Facades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests;

public class CompositionRootResolutionTests
{
    [Fact]
    public void FullStartupContainerCanResolvePublicFacadesAndInstantiateControllers()
    {
        var apiProjectPath = global::AssistantEngineer.Tests.TestPaths.ApiProjectPath;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiProjectPath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("Config/building-archetypes.json", optional: false)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=AssistantEngineerTests;Username=postgres",
                ["EnergyPlus:UseDocker"] = "false",
                ["EnergyPlus:ExecutablePath"] = "energyplus"
            })
            .Build();

        var services = new ServiceCollection();

        services.AddLogging();
        services.AddHttpClient();

        services.AddBuildingsModule(configuration);
        services.AddCalculationsModule(configuration);
        services.AddEquipmentModule();
        services.AddReportingModule();
        services.AddBenchmarksModule(configuration);
        services.AddInfrastructure(configuration, "Testing");
        services.AddSingleton<EngineeringWorkflowMemoryStore>();
        services.AddScoped<IEngineeringProjectRepository, InMemoryEngineeringProjectRepository>();
        services.AddScoped<IEngineeringWorkflowStateRepository, InMemoryEngineeringWorkflowStateRepository>();
        services.AddScoped<IEngineeringCalculationScenarioRepository, InMemoryEngineeringCalculationScenarioRepository>();
        services.AddScoped<IEngineeringCalculationArtifactRepository, InMemoryEngineeringCalculationArtifactRepository>();
        services.AddScoped<IEngineeringScenarioHistoryRepository, InMemoryEngineeringScenarioHistoryRepository>();
        services.AddScoped<IEngineeringCalculationJobRepository, InMemoryEngineeringCalculationJobRepository>();
        services.AddScoped<IEngineeringCalculationJobEventRepository, InMemoryEngineeringCalculationJobEventRepository>();
        services.AddOptions<EngineeringWorkflowPersistenceOptions>();
        services.AddScoped<IEngineeringWorkflowPersistenceService, EngineeringWorkflowPersistenceService>();
        services.AddEngineeringWorkflowServices();
        services.AddScoped<IEngineeringCalculationScenarioRunner, EngineeringCalculationScenarioRunner>();
        services.AddScoped<IEngineeringCalculationJobService, EngineeringCalculationJobService>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        using var scope = provider.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        foreach (var facadeType in GetPublicFacadeTypes())
        {
            var facade = serviceProvider.GetRequiredService(facadeType);
            Assert.NotNull(facade);
        }

        foreach (var controllerType in GetControllerTypes())
        {
            var controller = ActivatorUtilities.CreateInstance(serviceProvider, controllerType);
            Assert.NotNull(controller);
        }
    }

    private static IReadOnlyList<Type> GetPublicFacadeTypes() =>
    [
        typeof(IBenchmarksFacade),
        typeof(IBuildingsFacade),

        typeof(ILoadCalculationsFacade),
        typeof(IVentilationAnalysisFacade),
        typeof(IDomesticHotWaterFacade),
        typeof(IProfilesFacade),
        typeof(IStandardReferenceDataFacade),
        typeof(IBuildingEnergyAnalysisFacade),
        typeof(IBuildingComfortAnalysisFacade),
        typeof(IBuildingSizingAnalysisFacade),

        typeof(IEquipmentFacade),

        typeof(IBuildingCoolingReportsFacade),
        typeof(IBuildingHeatingReportsFacade),
        typeof(IBuildingEnergyBalanceReportsFacade)
    ];

    private static IReadOnlyList<Type> GetControllerTypes() =>
        typeof(Program).Assembly
            .GetTypes()
            .Where(type =>
                type is { IsAbstract: false, IsPublic: true } &&
                typeof(ControllerBase).IsAssignableFrom(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
}
