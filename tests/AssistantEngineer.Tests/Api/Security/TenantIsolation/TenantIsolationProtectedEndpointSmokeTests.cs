using System.Net;
using System.Net.Http.Json;
using AssistantEngineer.Api;
using AssistantEngineer.Api.Contracts.Calculations;
using AssistantEngineer.Api.Security.ApiKey;
using AssistantEngineer.Api.Security.Authentication;
using AssistantEngineer.Api.Security.Authorization;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Application.Services.Access;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AssistantEngineer.Tests.Api.Security.TenantIsolation;

public sealed class TenantIsolationProtectedEndpointSmokeTests
{
    private const string HeaderName = "X-AssistantEngineer-Api-Key";
    private const string ValidApiKey = "p5-15-tenant-isolation-smoke-test-key";

    public static TheoryData<RepresentativeEndpoint> RepresentativeEndpoints => new()
    {
        new RepresentativeEndpoint("ProjectsRead", Permission.ProjectsRead, HttpMethod.Get, "/api/v1/projects/10"),
        new RepresentativeEndpoint("BuildingsRead", Permission.BuildingsRead, HttpMethod.Get, "/api/v1/buildings/20"),
        new RepresentativeEndpoint("WorkflowsRead", Permission.WorkflowsRead, HttpMethod.Get, "/api/v1/engineering-workflow/10/state?buildingId=20"),
        new RepresentativeEndpoint("WorkflowsExecute", Permission.WorkflowsExecute, HttpMethod.Post, "/api/v1/engineering-workflow/prepare-calculation"),
        new RepresentativeEndpoint("CalculationRun", Permission.WorkflowsExecute, HttpMethod.Get, "/api/v1/buildings/20/load-calculations/cooling-load"),
        new RepresentativeEndpoint("ReportsRead", Permission.ReportsRead, HttpMethod.Get, "/api/v1/reports/buildings/20/heating?method=En12831")
    };

    [Theory]
    [MemberData(nameof(RepresentativeEndpoints))]
    public async Task RepresentativeProtectedEndpoint_EnforcesTenantMatrix(RepresentativeEndpoint endpoint)
    {
        await using var anonymousFactory = new TenantIsolationSmokeFactory(
            principal: TenantIsolationTestPrincipalFactory.Anonymous(),
            resourceOrganizationId: TenantIsolationScenario.TenantAOrganizationId,
            returnNotFoundForTenantMismatch: false);
        var anonymousResponse = await SendAsync(anonymousFactory.CreateClient(), endpoint, includeCredentials: false);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        await using var crossTenantForbiddenFactory = new TenantIsolationSmokeFactory(
            principal: TenantIsolationTestPrincipalFactory.TenantAWithAllPermissions(),
            resourceOrganizationId: TenantIsolationScenario.TenantBOrganizationId,
            returnNotFoundForTenantMismatch: false);
        var crossTenantForbiddenClient = crossTenantForbiddenFactory.CreateClient();
        crossTenantForbiddenClient.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);
        var crossTenantForbiddenResponse = await SendAsync(crossTenantForbiddenClient, endpoint, includeCredentials: true);
        Assert.Equal(HttpStatusCode.Forbidden, crossTenantForbiddenResponse.StatusCode);

        await using var crossTenantNotFoundFactory = new TenantIsolationSmokeFactory(
            principal: TenantIsolationTestPrincipalFactory.TenantAWithAllPermissions(),
            resourceOrganizationId: TenantIsolationScenario.TenantBOrganizationId,
            returnNotFoundForTenantMismatch: true);
        var crossTenantNotFoundClient = crossTenantNotFoundFactory.CreateClient();
        crossTenantNotFoundClient.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);
        var crossTenantNotFoundResponse = await SendAsync(crossTenantNotFoundClient, endpoint, includeCredentials: true);
        Assert.Equal(HttpStatusCode.NotFound, crossTenantNotFoundResponse.StatusCode);

        await using var sameTenantFactory = new TenantIsolationSmokeFactory(
            principal: TenantIsolationTestPrincipalFactory.TenantAWithAllPermissions(),
            resourceOrganizationId: TenantIsolationScenario.TenantAOrganizationId,
            returnNotFoundForTenantMismatch: false);
        var sameTenantClient = sameTenantFactory.CreateClient();
        sameTenantClient.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);
        var sameTenantResponse = await SendAsync(sameTenantClient, endpoint, includeCredentials: true);

        Assert.NotEqual(HttpStatusCode.Unauthorized, sameTenantResponse.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, sameTenantResponse.StatusCode);
        Assert.NotEqual(HttpStatusCode.NotFound, sameTenantResponse.StatusCode);
    }

    [Fact]
    public async Task MissingPermission_ReturnsForbidden_ForRepresentativeProjectReadEndpoint()
    {
        await using var factory = new TenantIsolationSmokeFactory(
            principal: TenantIsolationTestPrincipalFactory.TenantAWithout(Permission.ProjectsRead),
            resourceOrganizationId: TenantIsolationScenario.TenantAOrganizationId,
            returnNotFoundForTenantMismatch: false);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.GetAsync("/api/v1/projects/10");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        RepresentativeEndpoint endpoint,
        bool includeCredentials)
    {
        _ = includeCredentials;

        if (endpoint.Method == HttpMethod.Post)
        {
            return client.PostAsJsonAsync(endpoint.Path, CreatePreparationRequest());
        }

        return client.GetAsync(endpoint.Path);
    }

    public sealed record RepresentativeEndpoint(
        string Group,
        Permission Permission,
        HttpMethod Method,
        string Path);

    private sealed class TenantIsolationSmokeFactory : WebApplicationFactory<Program>
    {
        private readonly AuthenticatedPrincipal _principal;
        private readonly int _resourceOrganizationId;
        private readonly bool _returnNotFoundForTenantMismatch;
        private readonly IReadOnlyList<Project> _projects;
        private readonly IReadOnlyList<Building> _buildings;

        public TenantIsolationSmokeFactory(
            AuthenticatedPrincipal principal,
            int resourceOrganizationId,
            bool returnNotFoundForTenantMismatch)
        {
            _principal = principal;
            _resourceOrganizationId = resourceOrganizationId;
            _returnNotFoundForTenantMismatch = returnNotFoundForTenantMismatch;

            var project = Project.Create("Tenant isolation smoke project").Value;
            SetEntityId(project, TenantIsolationScenario.ProjectAId);
            var building = Building.Create("Tenant isolation smoke building", project).Value;
            SetEntityId(building, TenantIsolationScenario.BuildingAId);
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
                    ["Authentication:ApiKey:HeaderName"] = HeaderName,
                    ["Authentication:ApiKey:Key"] = ValidApiKey,
                    ["ApiAuthentication:Enabled"] = "true",
                    ["ApiAuthentication:AllowAnonymousInDevelopment"] = "false",
                    ["ApiAuthentication:ApiKeyHeaderName"] = HeaderName,
                    ["ApiAuthentication:EnableApiKeyAuthentication"] = "true",
                    ["ApiAuthentication:EnableJwtBearerAuthentication"] = "false",
                    ["ApiAuthorization:Enabled"] = "true",
                    ["ApiAuthorization:EnableReadEndpointProtectionPilot"] = "true",
                    ["ApiAuthorization:RequireProjectReadAuthorization"] = "true",
                    ["ApiAuthorization:RequireBuildingReadAuthorization"] = "true",
                    ["ApiAuthorization:EnableWriteEndpointProtectionPilot"] = "true",
                    ["ApiAuthorization:RequireProjectWriteAuthorization"] = "true",
                    ["ApiAuthorization:RequireBuildingWriteAuthorization"] = "true",
                    ["ApiAuthorization:EnableExecutionEndpointProtectionPilot"] = "true",
                    ["ApiAuthorization:RequireWorkflowExecuteAuthorization"] = "true",
                    ["ApiAuthorization:RequireCalculationRunAuthorization"] = "true",
                    ["ApiAuthorization:EnableReportArtifactEndpointProtectionPilot"] = "true",
                    ["ApiAuthorization:RequireReportReadAuthorization"] = "true",
                    ["ApiAuthorization:RequireReportWriteAuthorization"] = "true",
                    ["ApiAuthorization:RequireArtifactReadAuthorization"] = "true",
                    ["ApiAuthorization:EnableWorkflowReadEndpointProtectionPilot"] = "true",
                    ["ApiAuthorization:RequireWorkflowReadAuthorization"] = "true",
                    ["ApiAuthorization:ReturnNotFoundForTenantMismatch"] = _returnNotFoundForTenantMismatch ? "true" : "false",
                    ["ApiAuthorization:ReturnNotFoundForWorkflowTenantMismatch"] = _returnNotFoundForTenantMismatch ? "true" : "false",
                    ["ApiAuthorization:AllowAnonymousInDevelopment"] = "false"
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IApiKeyValidator>();
                services.AddSingleton<IApiKeyValidator>(new StubApiKeyValidator(ValidApiKey, _principal));

                services.RemoveAll<IProjectRepository>();
                services.RemoveAll<IBuildingRepository>();
                services.AddScoped<IProjectRepository>(_ => new StubProjectRepository(_projects));
                services.AddScoped<IBuildingRepository>(_ => new StubBuildingRepository(_buildings));

                services.RemoveAll<IProjectReadAccessScopeResolver>();
                services.RemoveAll<IBuildingReadAccessScopeResolver>();
                services.RemoveAll<IWorkflowAccessScopeResolver>();
                services.AddScoped<IProjectReadAccessScopeResolver>(_ => new FixedProjectScopeResolver(_resourceOrganizationId));
                services.AddScoped<IBuildingReadAccessScopeResolver>(_ => new FixedBuildingScopeResolver(_resourceOrganizationId));
                services.AddScoped<IWorkflowAccessScopeResolver>(_ => new FixedWorkflowScopeResolver(_resourceOrganizationId));
            });
        }

        private static void SetEntityId(object entity, int id)
        {
            var field = entity.GetType().GetField("<Id>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(field);
            field.SetValue(entity, id);
        }
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
            return Task.FromResult(
                string.Equals(apiKey, _expectedApiKey, StringComparison.Ordinal)
                    ? ApiKeyValidationResult.Success(_principal)
                    : ApiKeyValidationResult.Failure("InvalidApiKey"));
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
            return Task.FromResult(_projects);
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

        public Task<IReadOnlyList<Building>> ListByProjectIdAsync(int projectId, CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            return Task.FromResult<IReadOnlyList<Building>>(_buildings.Where(building => building.ProjectId == projectId).ToArray());
        }

        public void Add(Building building) => throw new NotSupportedException();

        public void Remove(Building building) => throw new NotSupportedException();

        public Task<Building?> GetForValidationAsync(int id, bool asTracking = false, CancellationToken cancellationToken = default)
        {
            _ = asTracking;
            _ = cancellationToken;
            return Task.FromResult(_buildings.FirstOrDefault(building => building.Id == id));
        }
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
            return Task.FromResult<ProjectAccessScope?>(ProjectTenantAccessScopeFactory.CreateProjectScope(
                projectId,
                _organizationId,
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
            return Task.FromResult<BuildingAccessScope?>(ProjectTenantAccessScopeFactory.CreateBuildingScope(
                buildingId,
                TenantIsolationScenario.ProjectAId,
                _organizationId,
                ownerUserId: null,
                isTenantScoped: true,
                tenantScope: new TenantScope(_organizationId, $"org-{_organizationId}", IsActive: true)));
        }
    }

    private sealed class FixedWorkflowScopeResolver : IWorkflowAccessScopeResolver
    {
        private readonly int _organizationId;

        public FixedWorkflowScopeResolver(int organizationId)
        {
            _organizationId = organizationId;
        }

        public Task<WorkflowAccessScope?> ResolveWorkflowScopeAsync(string workflowId, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            return Task.FromResult<WorkflowAccessScope?>(CreateScope(workflowId));
        }

        public Task<WorkflowAccessScope?> ResolveScenarioScopeAsync(string scenarioId, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            return Task.FromResult<WorkflowAccessScope?>(CreateScope(scenarioId));
        }

        public Task<WorkflowAccessScope?> ResolveJobScopeAsync(string jobId, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            return Task.FromResult<WorkflowAccessScope?>(CreateScope(jobId));
        }

        private WorkflowAccessScope CreateScope(string id)
        {
            return ProjectTenantAccessScopeFactory.CreateWorkflowScope(
                id,
                TenantIsolationScenario.ProjectAId,
                TenantIsolationScenario.BuildingAId,
                _organizationId,
                ownerUserId: null,
                isTenantScoped: true,
                tenantScope: new TenantScope(_organizationId, $"org-{_organizationId}", IsActive: true));
        }
    }

    private static EngineeringWorkflowCalculationPreparationRequestDto CreatePreparationRequest()
    {
        var state = CreateWorkflowState();
        return new EngineeringWorkflowCalculationPreparationRequestDto(state, ExecuteCalculation: false);
    }

    private static EngineeringWorkflowStateDto CreateWorkflowState()
    {
        return new EngineeringWorkflowStateDto(
            ProjectId: TenantIsolationScenario.ProjectAId,
            ProjectName: "Tenant isolation smoke project",
            BuildingId: TenantIsolationScenario.BuildingAId,
            CurrentStep: "Validation",
            Steps:
            [
                new EngineeringWorkflowStepDto("Project", "Completed", true)
            ],
            AvailableModules: ["ThermalTopology"],
            BuildingMetadata: new EngineeringWorkflowBuildingMetadataDto(
                BuildingName: "Tenant isolation smoke building",
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
