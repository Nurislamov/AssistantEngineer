using AssistantEngineer.Api;
using AssistantEngineer.Api.Services.Calculations;
using AssistantEngineer.Api.Services.Calculations.Idempotency;
using AssistantEngineer.Api.Services.Calculations.Persistence;
using AssistantEngineer.Api.Services.Calculations.Workflow;
using AssistantEngineer.Api.Security.Authorization;
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
        services.AddSingleton<IConfiguration>(configuration);

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
        services.AddEngineeringIdempotency();
        services.AddEngineeringWorkflowServices();
        services.AddScoped<IEngineeringCalculationScenarioModuleExecutor, EngineeringCalculationScenarioModuleExecutor>();
        services.AddScoped<IEngineeringCalculationVentilationScenarioStep, EngineeringCalculationVentilationScenarioStep>();
        services.AddScoped<IEngineeringCalculationDomesticHotWaterScenarioStep, EngineeringCalculationDomesticHotWaterScenarioStep>();
        services.AddScoped<IEngineeringCalculationSystemEnergyScenarioStep, EngineeringCalculationSystemEnergyScenarioStep>();
        services.AddScoped<IEngineeringCalculationGroundScenarioStep, EngineeringCalculationGroundScenarioStep>();
        services.AddScoped<IEngineeringCalculationWeatherSolarScenarioStep, EngineeringCalculationWeatherSolarScenarioStep>();
        services.AddScoped<IEngineeringCalculationScenarioResultBuilder, EngineeringCalculationScenarioResultBuilder>();
        services.AddScoped<IEngineeringCalculationScenarioRequestValidator, EngineeringCalculationScenarioRequestValidator>();
        services.AddScoped<IEngineeringCalculationScenarioRunner, EngineeringCalculationScenarioRunner>();
        services.AddScoped<EngineeringCalculationJobPayloadCodec>();
        services.AddScoped<EngineeringCalculationJobStatusTransitionPolicy>();
        services.AddScoped<EngineeringCalculationJobEventRecorder>();
        services.AddScoped<IEngineeringCalculationJobService, EngineeringCalculationJobService>();
        services.AddScoped<IAssistantEngineerAuthorizationService, AllowAllAuthorizationService>();
        services.AddScoped<IProtectedEndpointAuthorizationGate, AllowAllProtectedEndpointAuthorizationGate>();

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

    private sealed class AllowAllAuthorizationService : IAssistantEngineerAuthorizationService
    {
        public AssistantEngineerAuthorizationDecision AuthorizePilotPermission(string requiredPermission) =>
            AssistantEngineerAuthorizationDecision.Allowed;
    }

    private sealed class AllowAllProtectedEndpointAuthorizationGate : IProtectedEndpointAuthorizationGate
    {
        public Task<ProtectedEndpointAuthorizationDecision> RequirePermissionAsync(
            AssistantEngineer.Modules.Identity.Domain.Enums.Permission permission,
            CancellationToken cancellationToken)
        {
            _ = permission;
            _ = cancellationToken;
            return Task.FromResult(ProtectedEndpointAuthorizationDecision.Allowed);
        }

        public Task<ProtectedEndpointAuthorizationDecision> RequireProjectPermissionAsync(
            int projectId,
            AssistantEngineer.Modules.Identity.Domain.Enums.Permission permission,
            CancellationToken cancellationToken)
        {
            _ = projectId;
            _ = permission;
            _ = cancellationToken;
            return Task.FromResult(ProtectedEndpointAuthorizationDecision.Allowed);
        }

        public Task<ProtectedEndpointAuthorizationDecision> RequireBuildingPermissionAsync(
            int buildingId,
            AssistantEngineer.Modules.Identity.Domain.Enums.Permission permission,
            CancellationToken cancellationToken)
        {
            _ = buildingId;
            _ = permission;
            _ = cancellationToken;
            return Task.FromResult(ProtectedEndpointAuthorizationDecision.Allowed);
        }

        public Task<ProtectedEndpointAuthorizationDecision> RequireWorkflowPermissionAsync(
            AssistantEngineer.Modules.Identity.Domain.Enums.Permission permission,
            string? workflowId,
            int? projectId,
            int? buildingId,
            CancellationToken cancellationToken)
        {
            _ = permission;
            _ = workflowId;
            _ = projectId;
            _ = buildingId;
            _ = cancellationToken;
            return Task.FromResult(ProtectedEndpointAuthorizationDecision.Allowed);
        }

        public Task<ProtectedEndpointAuthorizationDecision> RequireCalculationPermissionAsync(
            AssistantEngineer.Modules.Identity.Domain.Enums.Permission permission,
            int? projectId,
            int? buildingId,
            int? floorId,
            int? roomId,
            CancellationToken cancellationToken)
        {
            _ = permission;
            _ = projectId;
            _ = buildingId;
            _ = floorId;
            _ = roomId;
            _ = cancellationToken;
            return Task.FromResult(ProtectedEndpointAuthorizationDecision.Allowed);
        }

        public Task<ProtectedEndpointAuthorizationDecision> RequireReportReadPermissionAsync(
            int? projectId,
            int? buildingId,
            string? workflowId,
            CancellationToken cancellationToken)
        {
            _ = projectId;
            _ = buildingId;
            _ = workflowId;
            _ = cancellationToken;
            return Task.FromResult(ProtectedEndpointAuthorizationDecision.Allowed);
        }

        public Task<ProtectedEndpointAuthorizationDecision> RequireReportWritePermissionAsync(
            int? projectId,
            int? buildingId,
            string? workflowId,
            CancellationToken cancellationToken)
        {
            _ = projectId;
            _ = buildingId;
            _ = workflowId;
            _ = cancellationToken;
            return Task.FromResult(ProtectedEndpointAuthorizationDecision.Allowed);
        }

        public Task<ProtectedEndpointAuthorizationDecision> RequireArtifactReadPermissionAsync(
            int? projectId,
            int? buildingId,
            string? workflowId,
            string? artifactId,
            CancellationToken cancellationToken)
        {
            _ = projectId;
            _ = buildingId;
            _ = workflowId;
            _ = artifactId;
            _ = cancellationToken;
            return Task.FromResult(ProtectedEndpointAuthorizationDecision.Allowed);
        }

        public Task<ProtectedEndpointAuthorizationDecision> RequireArtifactWritePermissionAsync(
            int? projectId,
            int? buildingId,
            string? workflowId,
            string? artifactId,
            CancellationToken cancellationToken)
        {
            _ = projectId;
            _ = buildingId;
            _ = workflowId;
            _ = artifactId;
            _ = cancellationToken;
            return Task.FromResult(ProtectedEndpointAuthorizationDecision.Allowed);
        }
    }
}
