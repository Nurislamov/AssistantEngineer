using System.Net;
using System.Net.Http.Json;
using AssistantEngineer.Api;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
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

namespace AssistantEngineer.Tests.Api;

public sealed class ProtectedReportArtifactEndpointsPilotTests
{
    private const string HeaderName = "X-AssistantEngineer-Api-Key";
    private const string ValidApiKey = "p5-13-protected-report-artifact-test-key";

    [Fact]
    public async Task ProtectionDisabled_PreservesExistingBehavior()
    {
        await using var factory = new ProtectedReportArtifactFactory(
            apiAuthenticationEnabled: false,
            apiAuthenticationAllowAnonymousInDevelopment: true,
            apiAuthorizationEnabled: false,
            enableReportArtifactEndpointProtectionPilot: false,
            requireReportReadAuthorization: false,
            requireReportWriteAuthorization: false,
            requireArtifactReadAuthorization: false,
            requireArtifactWriteAuthorization: false,
            returnNotFoundForTenantMismatch: false,
            apiAuthorizationAllowAnonymousInDevelopment: true,
            principalOrganizationId: 2001,
            principalPermissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/engineering-workflow/report", CreateWorkflowReportRequest());

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReportGenerateProtectionEnabled_WithoutCredentials_ReturnsUnauthorized()
    {
        await using var factory = new ProtectedReportArtifactFactory(
            apiAuthenticationEnabled: true,
            apiAuthenticationAllowAnonymousInDevelopment: false,
            apiAuthorizationEnabled: true,
            enableReportArtifactEndpointProtectionPilot: true,
            requireReportReadAuthorization: true,
            requireReportWriteAuthorization: true,
            requireArtifactReadAuthorization: true,
            requireArtifactWriteAuthorization: true,
            returnNotFoundForTenantMismatch: false,
            apiAuthorizationAllowAnonymousInDevelopment: false,
            principalOrganizationId: 2001,
            principalPermissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/engineering-workflow/report", CreateWorkflowReportRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReportGenerateProtectionEnabled_MissingReportsWrite_ReturnsForbidden()
    {
        await using var factory = new ProtectedReportArtifactFactory(
            apiAuthenticationEnabled: true,
            apiAuthenticationAllowAnonymousInDevelopment: false,
            apiAuthorizationEnabled: true,
            enableReportArtifactEndpointProtectionPilot: true,
            requireReportReadAuthorization: true,
            requireReportWriteAuthorization: true,
            requireArtifactReadAuthorization: true,
            requireArtifactWriteAuthorization: true,
            returnNotFoundForTenantMismatch: false,
            apiAuthorizationAllowAnonymousInDevelopment: false,
            principalOrganizationId: 2001,
            principalPermissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Permission.ReportsRead.ToString()
            });

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.PostAsJsonAsync("/api/v1/engineering-workflow/report", CreateWorkflowReportRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReportGenerateProtectionEnabled_ReportsWriteAndMatchingScope_Succeeds()
    {
        await using var factory = new ProtectedReportArtifactFactory(
            apiAuthenticationEnabled: true,
            apiAuthenticationAllowAnonymousInDevelopment: false,
            apiAuthorizationEnabled: true,
            enableReportArtifactEndpointProtectionPilot: true,
            requireReportReadAuthorization: true,
            requireReportWriteAuthorization: true,
            requireArtifactReadAuthorization: true,
            requireArtifactWriteAuthorization: true,
            returnNotFoundForTenantMismatch: false,
            apiAuthorizationAllowAnonymousInDevelopment: false,
            principalOrganizationId: 2001,
            principalPermissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Permission.ReportsWrite.ToString()
            },
            projectScopeOrganizationId: 2001,
            buildingScopeOrganizationId: 2001);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.PostAsJsonAsync("/api/v1/engineering-workflow/report", CreateWorkflowReportRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReportReadProtection_RequiresReportsReadPermission()
    {
        await using var withoutPermissionFactory = new ProtectedReportArtifactFactory(
            apiAuthenticationEnabled: true,
            apiAuthenticationAllowAnonymousInDevelopment: false,
            apiAuthorizationEnabled: true,
            enableReportArtifactEndpointProtectionPilot: true,
            requireReportReadAuthorization: true,
            requireReportWriteAuthorization: true,
            requireArtifactReadAuthorization: true,
            requireArtifactWriteAuthorization: true,
            returnNotFoundForTenantMismatch: false,
            apiAuthorizationAllowAnonymousInDevelopment: false,
            principalOrganizationId: 2001,
            principalPermissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Permission.WorkflowsRead.ToString()
            },
            buildingScopeOrganizationId: 2001);

        var unauthorizedClient = withoutPermissionFactory.CreateClient();
        unauthorizedClient.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);
        var forbidden = await unauthorizedClient.PostAsJsonAsync(
            "/api/v1/engineering-workflow/trace-preview",
            CreateTracePreviewRequest());
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        await using var withPermissionFactory = new ProtectedReportArtifactFactory(
            apiAuthenticationEnabled: true,
            apiAuthenticationAllowAnonymousInDevelopment: false,
            apiAuthorizationEnabled: true,
            enableReportArtifactEndpointProtectionPilot: true,
            requireReportReadAuthorization: true,
            requireReportWriteAuthorization: true,
            requireArtifactReadAuthorization: true,
            requireArtifactWriteAuthorization: true,
            returnNotFoundForTenantMismatch: false,
            apiAuthorizationAllowAnonymousInDevelopment: false,
            principalOrganizationId: 2001,
            principalPermissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Permission.ReportsRead.ToString()
            },
            buildingScopeOrganizationId: 2001);

        var authorizedClient = withPermissionFactory.CreateClient();
        authorizedClient.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);
        var success = await authorizedClient.PostAsJsonAsync(
            "/api/v1/engineering-workflow/trace-preview",
            CreateTracePreviewRequest());

        Assert.NotEqual(HttpStatusCode.Unauthorized, success.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, success.StatusCode);
    }

    [Theory]
    [InlineData(false, HttpStatusCode.Forbidden)]
    [InlineData(true, HttpStatusCode.NotFound)]
    public async Task ReportReadTenantMismatch_RespectsNotFoundOption(bool returnNotFoundForTenantMismatch, HttpStatusCode expected)
    {
        await using var factory = new ProtectedReportArtifactFactory(
            apiAuthenticationEnabled: true,
            apiAuthenticationAllowAnonymousInDevelopment: false,
            apiAuthorizationEnabled: true,
            enableReportArtifactEndpointProtectionPilot: true,
            requireReportReadAuthorization: true,
            requireReportWriteAuthorization: true,
            requireArtifactReadAuthorization: true,
            requireArtifactWriteAuthorization: true,
            returnNotFoundForTenantMismatch: returnNotFoundForTenantMismatch,
            apiAuthorizationAllowAnonymousInDevelopment: false,
            principalOrganizationId: 2001,
            principalPermissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Permission.ReportsRead.ToString()
            },
            buildingScopeOrganizationId: 3001);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.GetAsync("/api/v1/reports/buildings/11/heating?method=En12831");

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task ArtifactReadProtection_RequiresReportsReadPermission()
    {
        await using var withoutPermissionFactory = new ProtectedReportArtifactFactory(
            apiAuthenticationEnabled: true,
            apiAuthenticationAllowAnonymousInDevelopment: false,
            apiAuthorizationEnabled: true,
            enableReportArtifactEndpointProtectionPilot: true,
            requireReportReadAuthorization: true,
            requireReportWriteAuthorization: true,
            requireArtifactReadAuthorization: true,
            requireArtifactWriteAuthorization: true,
            returnNotFoundForTenantMismatch: false,
            apiAuthorizationAllowAnonymousInDevelopment: false,
            principalOrganizationId: 2001,
            principalPermissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Permission.WorkflowsRead.ToString()
            });

        var unauthorizedClient = withoutPermissionFactory.CreateClient();
        unauthorizedClient.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);
        var forbidden = await unauthorizedClient.GetAsync("/api/v1/engineering-workflow/scenarios/non-existent/artifacts");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        await using var withPermissionFactory = new ProtectedReportArtifactFactory(
            apiAuthenticationEnabled: true,
            apiAuthenticationAllowAnonymousInDevelopment: false,
            apiAuthorizationEnabled: true,
            enableReportArtifactEndpointProtectionPilot: true,
            requireReportReadAuthorization: true,
            requireReportWriteAuthorization: true,
            requireArtifactReadAuthorization: true,
            requireArtifactWriteAuthorization: true,
            returnNotFoundForTenantMismatch: false,
            apiAuthorizationAllowAnonymousInDevelopment: false,
            principalOrganizationId: 2001,
            principalPermissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Permission.ReportsRead.ToString()
            });

        var authorizedClient = withPermissionFactory.CreateClient();
        authorizedClient.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);
        var response = await authorizedClient.GetAsync("/api/v1/engineering-workflow/scenarios/non-existent/artifacts");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static EngineeringWorkflowReportRequestDto CreateWorkflowReportRequest()
    {
        var state = new EngineeringWorkflowStateDto(
            ProjectId: 1,
            ProjectName: "Report protection test project",
            BuildingId: 11,
            CurrentStep: "Validation",
            Steps:
            [
                new EngineeringWorkflowStepDto("Project", "Completed", true)
            ],
            AvailableModules: ["Reporting"],
            BuildingMetadata: new EngineeringWorkflowBuildingMetadataDto(
                BuildingName: "Report protection test building",
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

        return new EngineeringWorkflowReportRequestDto(state);
    }

    private static EngineeringWorkflowTracePreviewRequestDto CreateTracePreviewRequest()
    {
        return new EngineeringWorkflowTracePreviewRequestDto(
            CreateWorkflowReportRequest().State,
            DetailLevel: "Standard");
    }

    private sealed class ProtectedReportArtifactFactory : WebApplicationFactory<Program>
    {
        private readonly bool _apiAuthenticationEnabled;
        private readonly bool _apiAuthenticationAllowAnonymousInDevelopment;
        private readonly bool _apiAuthorizationEnabled;
        private readonly bool _enableReportArtifactEndpointProtectionPilot;
        private readonly bool _requireReportReadAuthorization;
        private readonly bool _requireReportWriteAuthorization;
        private readonly bool _requireArtifactReadAuthorization;
        private readonly bool _requireArtifactWriteAuthorization;
        private readonly bool _returnNotFoundForTenantMismatch;
        private readonly bool _apiAuthorizationAllowAnonymousInDevelopment;
        private readonly int? _projectScopeOrganizationId;
        private readonly int? _buildingScopeOrganizationId;
        private readonly AuthenticatedPrincipal _principal;
        private readonly IReadOnlyList<Project> _projects;
        private readonly IReadOnlyList<Building> _buildings;

        public ProtectedReportArtifactFactory(
            bool apiAuthenticationEnabled,
            bool apiAuthenticationAllowAnonymousInDevelopment,
            bool apiAuthorizationEnabled,
            bool enableReportArtifactEndpointProtectionPilot,
            bool requireReportReadAuthorization,
            bool requireReportWriteAuthorization,
            bool requireArtifactReadAuthorization,
            bool requireArtifactWriteAuthorization,
            bool returnNotFoundForTenantMismatch,
            bool apiAuthorizationAllowAnonymousInDevelopment,
            int principalOrganizationId,
            IReadOnlySet<string> principalPermissions,
            int? projectScopeOrganizationId = null,
            int? buildingScopeOrganizationId = null)
        {
            _apiAuthenticationEnabled = apiAuthenticationEnabled;
            _apiAuthenticationAllowAnonymousInDevelopment = apiAuthenticationAllowAnonymousInDevelopment;
            _apiAuthorizationEnabled = apiAuthorizationEnabled;
            _enableReportArtifactEndpointProtectionPilot = enableReportArtifactEndpointProtectionPilot;
            _requireReportReadAuthorization = requireReportReadAuthorization;
            _requireReportWriteAuthorization = requireReportWriteAuthorization;
            _requireArtifactReadAuthorization = requireArtifactReadAuthorization;
            _requireArtifactWriteAuthorization = requireArtifactWriteAuthorization;
            _returnNotFoundForTenantMismatch = returnNotFoundForTenantMismatch;
            _apiAuthorizationAllowAnonymousInDevelopment = apiAuthorizationAllowAnonymousInDevelopment;
            _projectScopeOrganizationId = projectScopeOrganizationId;
            _buildingScopeOrganizationId = buildingScopeOrganizationId;

            _principal = new AuthenticatedPrincipal(
                UserId: 1001,
                OrganizationId: principalOrganizationId,
                ExternalSubjectId: "p5-13-protected-report-artifact-test-principal",
                AuthenticationScheme: ApiKeyAuthenticationHandler.SchemeName,
                Roles: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                Permissions: principalPermissions,
                IsAuthenticated: true);

            var project = Project.Create("Protected report/artifact integration project").Value;
            var building = Building.Create("Protected report/artifact integration building", project).Value;
            SetEntityId(project, 1);
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
                    ["Authentication:ApiKey:HeaderName"] = HeaderName,
                    ["Authentication:ApiKey:Key"] = ValidApiKey,
                    ["ApiAuthentication:Enabled"] = _apiAuthenticationEnabled ? "true" : "false",
                    ["ApiAuthentication:AllowAnonymousInDevelopment"] = _apiAuthenticationAllowAnonymousInDevelopment ? "true" : "false",
                    ["ApiAuthentication:ApiKeyHeaderName"] = HeaderName,
                    ["ApiAuthentication:EnableApiKeyAuthentication"] = "true",
                    ["ApiAuthentication:EnableJwtBearerAuthentication"] = "false",
                    ["ApiAuthorization:Enabled"] = _apiAuthorizationEnabled ? "true" : "false",
                    ["ApiAuthorization:EnableReportArtifactEndpointProtectionPilot"] = _enableReportArtifactEndpointProtectionPilot ? "true" : "false",
                    ["ApiAuthorization:RequireReportReadAuthorization"] = _requireReportReadAuthorization ? "true" : "false",
                    ["ApiAuthorization:RequireReportWriteAuthorization"] = _requireReportWriteAuthorization ? "true" : "false",
                    ["ApiAuthorization:RequireArtifactReadAuthorization"] = _requireArtifactReadAuthorization ? "true" : "false",
                    ["ApiAuthorization:RequireArtifactWriteAuthorization"] = _requireArtifactWriteAuthorization ? "true" : "false",
                    ["ApiAuthorization:ReturnNotFoundForTenantMismatch"] = _returnNotFoundForTenantMismatch ? "true" : "false",
                    ["ApiAuthorization:AllowAnonymousInDevelopment"] = _apiAuthorizationAllowAnonymousInDevelopment ? "true" : "false"
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

                if (_projectScopeOrganizationId.HasValue)
                {
                    services.RemoveAll<IProjectReadAccessScopeResolver>();
                    services.AddScoped<IProjectReadAccessScopeResolver>(_ =>
                        new FixedProjectScopeResolver(_projectScopeOrganizationId.Value));
                }

                if (_buildingScopeOrganizationId.HasValue)
                {
                    services.RemoveAll<IBuildingReadAccessScopeResolver>();
                    services.AddScoped<IBuildingReadAccessScopeResolver>(_ =>
                        new FixedBuildingScopeResolver(_buildingScopeOrganizationId.Value));
                }
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
