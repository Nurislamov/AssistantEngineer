using System.Reflection;
using AssistantEngineer.Api;
using AssistantEngineer.Api.Security.ApiKey;
using AssistantEngineer.Api.Security.Authentication;
using AssistantEngineer.Api.Security.Authorization;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Application.Services.Access;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AssistantEngineer.Tests.Api.EngineeringWorkflow;

internal static class EngineeringWorkflowControllerCharacterizationHelper
{
    public const string HeaderName = "X-AssistantEngineer-Api-Key";
    public const string ValidApiKey = "p8-03d-workflow-controller-characterization-key";

    public static EngineeringWorkflowCalculationPreparationRequestDto CreatePreparationRequest()
    {
        return new EngineeringWorkflowCalculationPreparationRequestDto(CreateWorkflowState(), ExecuteCalculation: false);
    }

    public static EngineeringWorkflowStateDto CreateWorkflowState()
    {
        return new EngineeringWorkflowStateDto(
            ProjectId: 1,
            ProjectName: "P8-03D characterization project",
            BuildingId: 11,
            CurrentStep: "Validation",
            Steps:
            [
                new EngineeringWorkflowStepDto("Project", "Completed", true),
                new EngineeringWorkflowStepDto("Validation", "Pending", false)
            ],
            AvailableModules: ["ThermalTopology"],
            BuildingMetadata: new EngineeringWorkflowBuildingMetadataDto(
                BuildingName: "P8-03D characterization building",
                LocationText: "Test",
                FloorAreaM2: null,
                VolumeM3: null,
                NumberOfZones: null,
                Notes: null),
            Zones: [],
            Boundaries: [],
            WeatherSolarSettings: new EngineeringWorkflowWeatherSolarSettingsDto("Unknown", "Unknown", "Unknown"),
            VentilationSettings: new EngineeringWorkflowVentilationSettingsDto(0, "Unknown", "Unknown", []),
            GroundSettings: new EngineeringWorkflowGroundSettingsDto(0, "Unknown", "Unknown"),
            DomesticHotWaterSettings: new EngineeringWorkflowDomesticHotWaterSettingsDto("Unknown", "Unknown", "Unknown", "Unknown"),
            SystemEnergySettings: new EngineeringWorkflowSystemEnergySettingsDto("Unknown", "Unknown", "Unknown"),
            Diagnostics: [],
            Assumptions: [],
            Links: [],
            CalculationTraceSummary: null,
            ReportSummary: null,
            Metadata: new Dictionary<string, string>(StringComparer.Ordinal));
    }
}

internal sealed record EngineeringWorkflowControllerCharacterizationOptions(
    bool ApiAuthenticationEnabled = false,
    bool ApiAuthenticationAllowAnonymousInDevelopment = true,
    bool ApiAuthorizationEnabled = false,
    bool EnableExecutionEndpointProtectionPilot = false,
    bool RequireWorkflowExecuteAuthorization = false,
    bool RequireCalculationRunAuthorization = false,
    bool EnableWorkflowReadEndpointProtectionPilot = false,
    bool RequireWorkflowReadAuthorization = false,
    bool EnableReportArtifactEndpointProtectionPilot = false,
    bool RequireReportReadAuthorization = false,
    bool RequireReportWriteAuthorization = false,
    bool RequireArtifactReadAuthorization = false,
    bool RequireArtifactWriteAuthorization = false,
    bool ReturnNotFoundForTenantMismatch = false,
    bool ReturnNotFoundForWorkflowTenantMismatch = false,
    bool ApiAuthorizationAllowAnonymousInDevelopment = true,
    int PrincipalOrganizationId = 2001,
    int? ProjectScopeOrganizationId = null,
    int? BuildingScopeOrganizationId = null,
    IReadOnlySet<string>? PrincipalPermissions = null)
{
    public IReadOnlySet<string> EffectivePrincipalPermissions =>
        PrincipalPermissions ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}

internal sealed class EngineeringWorkflowControllerCharacterizationFactory : WebApplicationFactory<Program>
{
    private readonly EngineeringWorkflowControllerCharacterizationOptions _options;
    private readonly AuthenticatedPrincipal _principal;
    private readonly IReadOnlyList<Project> _projects;
    private readonly IReadOnlyList<Building> _buildings;

    public EngineeringWorkflowControllerCharacterizationFactory(EngineeringWorkflowControllerCharacterizationOptions options)
    {
        _options = options;
        _principal = new AuthenticatedPrincipal(
            UserId: 1001,
            OrganizationId: options.PrincipalOrganizationId,
            ExternalSubjectId: "p8-03d-workflow-controller-characterization-principal",
            AuthenticationScheme: ApiKeyAuthenticationHandler.SchemeName,
            Roles: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            Permissions: options.EffectivePrincipalPermissions,
            IsAuthenticated: true);

        var project = Project.Create("P8-03D characterization project").Value;
        SetEntityId(project, 1);

        var building = Building.Create("P8-03D characterization building", project).Value;
        SetEntityId(building, 11);
        Assert.True(project.AddBuilding(building).IsSuccess);

        _projects = [project];
        _buildings = [building];
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=localhost;Port=5432;Database=AssistantEngineerTests;Username=postgres");

        builder.ConfigureAppConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=AssistantEngineerTests;Username=postgres",
                ["EngineeringWorkflowPersistence:Provider"] = "InMemory",
                ["EnergyPlus:UseDocker"] = "false",
                ["EnergyPlus:ExecutablePath"] = "energyplus",
                ["Authentication:ApiKey:Enabled"] = "true",
                ["Authentication:ApiKey:HeaderName"] = EngineeringWorkflowControllerCharacterizationHelper.HeaderName,
                ["Authentication:ApiKey:Key"] = EngineeringWorkflowControllerCharacterizationHelper.ValidApiKey,
                ["ApiAuthentication:Enabled"] = _options.ApiAuthenticationEnabled ? "true" : "false",
                ["ApiAuthentication:AllowAnonymousInDevelopment"] = _options.ApiAuthenticationAllowAnonymousInDevelopment ? "true" : "false",
                ["ApiAuthentication:ApiKeyHeaderName"] = EngineeringWorkflowControllerCharacterizationHelper.HeaderName,
                ["ApiAuthentication:EnableApiKeyAuthentication"] = "true",
                ["ApiAuthentication:EnableJwtBearerAuthentication"] = "false",
                ["ApiAuthorization:Enabled"] = _options.ApiAuthorizationEnabled ? "true" : "false",
                ["ApiAuthorization:EnableExecutionEndpointProtectionPilot"] = _options.EnableExecutionEndpointProtectionPilot ? "true" : "false",
                ["ApiAuthorization:RequireWorkflowExecuteAuthorization"] = _options.RequireWorkflowExecuteAuthorization ? "true" : "false",
                ["ApiAuthorization:RequireCalculationRunAuthorization"] = _options.RequireCalculationRunAuthorization ? "true" : "false",
                ["ApiAuthorization:EnableWorkflowReadEndpointProtectionPilot"] = _options.EnableWorkflowReadEndpointProtectionPilot ? "true" : "false",
                ["ApiAuthorization:RequireWorkflowReadAuthorization"] = _options.RequireWorkflowReadAuthorization ? "true" : "false",
                ["ApiAuthorization:EnableReportArtifactEndpointProtectionPilot"] = _options.EnableReportArtifactEndpointProtectionPilot ? "true" : "false",
                ["ApiAuthorization:RequireReportReadAuthorization"] = _options.RequireReportReadAuthorization ? "true" : "false",
                ["ApiAuthorization:RequireReportWriteAuthorization"] = _options.RequireReportWriteAuthorization ? "true" : "false",
                ["ApiAuthorization:RequireArtifactReadAuthorization"] = _options.RequireArtifactReadAuthorization ? "true" : "false",
                ["ApiAuthorization:RequireArtifactWriteAuthorization"] = _options.RequireArtifactWriteAuthorization ? "true" : "false",
                ["ApiAuthorization:ReturnNotFoundForTenantMismatch"] = _options.ReturnNotFoundForTenantMismatch ? "true" : "false",
                ["ApiAuthorization:ReturnNotFoundForWorkflowTenantMismatch"] = _options.ReturnNotFoundForWorkflowTenantMismatch ? "true" : "false",
                ["ApiAuthorization:AllowAnonymousInDevelopment"] = _options.ApiAuthorizationAllowAnonymousInDevelopment ? "true" : "false"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IApiKeyValidator>();
            services.AddSingleton<IApiKeyValidator>(
                new StubApiKeyValidator(
                    EngineeringWorkflowControllerCharacterizationHelper.ValidApiKey,
                    _principal));

            services.RemoveAll<IProjectRepository>();
            services.RemoveAll<IBuildingRepository>();
            services.AddScoped<IProjectRepository>(_ => new StubProjectRepository(_projects));
            services.AddScoped<IBuildingRepository>(_ => new StubBuildingRepository(_buildings));

            if (_options.ProjectScopeOrganizationId.HasValue)
            {
                services.RemoveAll<IProjectReadAccessScopeResolver>();
                services.AddScoped<IProjectReadAccessScopeResolver>(_ =>
                    new FixedProjectScopeResolver(_options.ProjectScopeOrganizationId.Value));
            }

            if (_options.BuildingScopeOrganizationId.HasValue)
            {
                services.RemoveAll<IBuildingReadAccessScopeResolver>();
                services.AddScoped<IBuildingReadAccessScopeResolver>(_ =>
                    new FixedBuildingScopeResolver(_options.BuildingScopeOrganizationId.Value));
            }
        });
    }

    private static void SetEntityId(object entity, int id)
    {
        var field = entity.GetType().GetField("<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(entity, id);
    }

    private sealed class StubApiKeyValidator : IApiKeyValidator
    {
        private readonly string _expectedApiKey;
        private readonly AuthenticatedPrincipal _principal;

        public StubApiKeyValidator(string expectedApiKey, AuthenticatedPrincipal principal)
        {
            _expectedApiKey = expectedApiKey;
            _principal = principal;
        }

        public Task<ApiKeyValidationResult> ValidateAsync(string apiKey, CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;

            if (!string.Equals(apiKey, _expectedApiKey, StringComparison.Ordinal))
            {
                return Task.FromResult(ApiKeyValidationResult.Failure("InvalidApiKey"));
            }

            return Task.FromResult(ApiKeyValidationResult.Success(_principal));
        }
    }

    private sealed class StubProjectRepository : IProjectRepository
    {
        private readonly IReadOnlyList<Project> _projects;

        public StubProjectRepository(IReadOnlyList<Project> projects)
        {
            _projects = projects;
        }

        public Task<Project?> GetByIdAsync(int id, bool includeBuildings = false, CancellationToken cancellationToken = default)
        {
            _ = includeBuildings;
            _ = cancellationToken;
            return Task.FromResult(_projects.FirstOrDefault(project => project.Id == id));
        }

        public Task<IReadOnlyList<Project>> ListAsync(CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            return Task.FromResult<IReadOnlyList<Project>>(_projects);
        }

        public void Add(Project project) => throw new NotSupportedException();

        public void Remove(Project project) => throw new NotSupportedException();
    }

    private sealed class StubBuildingRepository : IBuildingRepository
    {
        private readonly IReadOnlyList<Building> _buildings;

        public StubBuildingRepository(IReadOnlyList<Building> buildings)
        {
            _buildings = buildings;
        }

        public Task<Building?> GetByIdAsync(int id, bool includeClimateZone = false, CancellationToken cancellationToken = default)
        {
            _ = includeClimateZone;
            _ = cancellationToken;
            return Task.FromResult(_buildings.FirstOrDefault(building => building.Id == id));
        }

        public Task<Building?> GetWithFloorsAsync(int id, CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            return Task.FromResult(_buildings.FirstOrDefault(building => building.Id == id));
        }

        public Task<Building?> GetWithThermalZonesAndRoomsAsync(int id, CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            return Task.FromResult(_buildings.FirstOrDefault(building => building.Id == id));
        }

        public Task<Building?> GetByThermalZoneIdAsync(int thermalZoneId, CancellationToken cancellationToken = default)
        {
            _ = thermalZoneId;
            _ = cancellationToken;
            return Task.FromResult<Building?>(null);
        }

        public Task<Building?> GetForCalculationAsync(int id, CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            return Task.FromResult(_buildings.FirstOrDefault(building => building.Id == id));
        }

        public Task<Building?> GetForReportAsync(int id, CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            return Task.FromResult(_buildings.FirstOrDefault(building => building.Id == id));
        }

        public Task<Building?> GetForValidationAsync(int id, bool asTracking = false, CancellationToken cancellationToken = default)
        {
            _ = asTracking;
            _ = cancellationToken;
            return Task.FromResult(_buildings.FirstOrDefault(building => building.Id == id));
        }

        public Task<IReadOnlyList<Building>> ListByProjectIdAsync(int projectId, CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            return Task.FromResult<IReadOnlyList<Building>>(_buildings.Where(building => building.ProjectId == projectId).ToArray());
        }

        public void Add(Building building) => throw new NotSupportedException();

        public void Remove(Building building) => throw new NotSupportedException();
    }

    private sealed class FixedProjectScopeResolver : IProjectReadAccessScopeResolver
    {
        private readonly int _organizationId;

        public FixedProjectScopeResolver(int organizationId)
        {
            _organizationId = organizationId;
        }

        public Task<ProjectAccessScope?> ResolveProjectScopeAsync(int projectId, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            return Task.FromResult<ProjectAccessScope?>(
                ProjectTenantAccessScopeFactory.CreateProjectScope(
                    projectId: projectId,
                    organizationId: _organizationId,
                    ownerUserId: null,
                    isTenantScoped: true,
                    tenantScope: new TenantScope(_organizationId, $"org-{_organizationId}", IsActive: true)));
        }
    }

    private sealed class FixedBuildingScopeResolver : IBuildingReadAccessScopeResolver
    {
        private readonly int _organizationId;

        public FixedBuildingScopeResolver(int organizationId)
        {
            _organizationId = organizationId;
        }

        public Task<BuildingAccessScope?> ResolveBuildingScopeAsync(int buildingId, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            return Task.FromResult<BuildingAccessScope?>(
                ProjectTenantAccessScopeFactory.CreateBuildingScope(
                    buildingId: buildingId,
                    projectId: 1,
                    organizationId: _organizationId,
                    ownerUserId: null,
                    isTenantScoped: true,
                    tenantScope: new TenantScope(_organizationId, $"org-{_organizationId}", IsActive: true)));
        }
    }
}
