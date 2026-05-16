using System.Net;
using System.Net.Http.Json;
using AssistantEngineer.Api;
using AssistantEngineer.Api.Security.ApiKey;
using AssistantEngineer.Api.Security.Authentication;
using AssistantEngineer.Api.Security.Authorization;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Application.Services.Access;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using AssistantEngineer.SharedKernel.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AssistantEngineer.Tests.Api;

public sealed class ProtectedWriteEndpointsPilotTests
{
    private const string HeaderName = "X-AssistantEngineer-Api-Key";
    private const string ValidApiKey = "p5-11-protected-write-test-key";

    [Fact]
    public async Task WriteProtectionDisabled_PreservesExistingBehavior()
    {
        await using var factory = new ProtectedWriteEndpointFactory(
            apiAuthenticationEnabled: false,
            apiAuthenticationAllowAnonymousInDevelopment: true,
            apiAuthorizationEnabled: false,
            enableReadEndpointProtectionPilot: false,
            requireProjectReadAuthorization: false,
            requireBuildingReadAuthorization: false,
            enableWriteEndpointProtectionPilot: false,
            requireProjectWriteAuthorization: false,
            requireBuildingWriteAuthorization: false,
            returnNotFoundForTenantMismatch: false,
            apiAuthorizationAllowAnonymousInDevelopment: true,
            principalOrganizationId: 2001,
            principalPermissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/projects", new { name = string.Empty });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task WriteProtectionEnabled_MissingCredentials_ReturnsUnauthorized()
    {
        await using var factory = new ProtectedWriteEndpointFactory(
            apiAuthenticationEnabled: true,
            apiAuthenticationAllowAnonymousInDevelopment: false,
            apiAuthorizationEnabled: true,
            enableReadEndpointProtectionPilot: false,
            requireProjectReadAuthorization: false,
            requireBuildingReadAuthorization: false,
            enableWriteEndpointProtectionPilot: true,
            requireProjectWriteAuthorization: true,
            requireBuildingWriteAuthorization: true,
            returnNotFoundForTenantMismatch: false,
            apiAuthorizationAllowAnonymousInDevelopment: false,
            principalOrganizationId: 2001,
            principalPermissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        var client = factory.CreateClient();
        var response = await client.PutAsJsonAsync("/api/v1/projects/1", new { name = "Renamed project" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WriteProtectionEnabled_MissingProjectsWritePermission_ReturnsForbidden()
    {
        await using var factory = new ProtectedWriteEndpointFactory(
            apiAuthenticationEnabled: true,
            apiAuthenticationAllowAnonymousInDevelopment: false,
            apiAuthorizationEnabled: true,
            enableReadEndpointProtectionPilot: false,
            requireProjectReadAuthorization: false,
            requireBuildingReadAuthorization: false,
            enableWriteEndpointProtectionPilot: true,
            requireProjectWriteAuthorization: true,
            requireBuildingWriteAuthorization: true,
            returnNotFoundForTenantMismatch: false,
            apiAuthorizationAllowAnonymousInDevelopment: false,
            principalOrganizationId: 2001,
            principalPermissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Permission.ProjectsRead.ToString()
            });

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.PutAsJsonAsync("/api/v1/projects/1", new { name = "Renamed project" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task WriteProtectionEnabled_ProjectsWriteAndMatchingOrganization_Succeeds()
    {
        await using var factory = new ProtectedWriteEndpointFactory(
            apiAuthenticationEnabled: true,
            apiAuthenticationAllowAnonymousInDevelopment: false,
            apiAuthorizationEnabled: true,
            enableReadEndpointProtectionPilot: false,
            requireProjectReadAuthorization: false,
            requireBuildingReadAuthorization: false,
            enableWriteEndpointProtectionPilot: true,
            requireProjectWriteAuthorization: true,
            requireBuildingWriteAuthorization: true,
            returnNotFoundForTenantMismatch: false,
            apiAuthorizationAllowAnonymousInDevelopment: false,
            principalOrganizationId: 2001,
            principalPermissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Permission.ProjectsWrite.ToString()
            },
            projectScopeOrganizationId: 2001);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.PutAsJsonAsync("/api/v1/projects/1", new { name = "Renamed project" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task BuildingWriteProtection_RequiresBuildingsWritePermission()
    {
        await using var withoutPermissionFactory = new ProtectedWriteEndpointFactory(
            apiAuthenticationEnabled: true,
            apiAuthenticationAllowAnonymousInDevelopment: false,
            apiAuthorizationEnabled: true,
            enableReadEndpointProtectionPilot: false,
            requireProjectReadAuthorization: false,
            requireBuildingReadAuthorization: false,
            enableWriteEndpointProtectionPilot: true,
            requireProjectWriteAuthorization: true,
            requireBuildingWriteAuthorization: true,
            returnNotFoundForTenantMismatch: false,
            apiAuthorizationAllowAnonymousInDevelopment: false,
            principalOrganizationId: 2001,
            principalPermissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Permission.ProjectsWrite.ToString()
            },
            projectScopeOrganizationId: 2001,
            buildingScopeOrganizationId: 2001);

        var unauthorizedClient = withoutPermissionFactory.CreateClient();
        unauthorizedClient.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);
        var forbiddenResponse = await unauthorizedClient.PutAsJsonAsync("/api/v1/buildings/11", new { name = "Renamed building", climateZoneId = (int?)null });
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);

        await using var withPermissionFactory = new ProtectedWriteEndpointFactory(
            apiAuthenticationEnabled: true,
            apiAuthenticationAllowAnonymousInDevelopment: false,
            apiAuthorizationEnabled: true,
            enableReadEndpointProtectionPilot: false,
            requireProjectReadAuthorization: false,
            requireBuildingReadAuthorization: false,
            enableWriteEndpointProtectionPilot: true,
            requireProjectWriteAuthorization: true,
            requireBuildingWriteAuthorization: true,
            returnNotFoundForTenantMismatch: false,
            apiAuthorizationAllowAnonymousInDevelopment: false,
            principalOrganizationId: 2001,
            principalPermissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Permission.BuildingsWrite.ToString()
            },
            projectScopeOrganizationId: 2001,
            buildingScopeOrganizationId: 2001);

        var authorizedClient = withPermissionFactory.CreateClient();
        authorizedClient.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var createResponse = await authorizedClient.PostAsJsonAsync("/api/v1/projects/1/buildings", new { name = "New write-protected building", climateZoneId = (int?)null });
        var updateResponse = await authorizedClient.PutAsJsonAsync("/api/v1/buildings/11", new { name = "Renamed building", climateZoneId = (int?)null });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
    }

    [Theory]
    [InlineData(false, HttpStatusCode.Forbidden)]
    [InlineData(true, HttpStatusCode.NotFound)]
    public async Task WriteProtectionEnabled_TenantMismatch_RespectsNotFoundOption(
        bool returnNotFoundForTenantMismatch,
        HttpStatusCode expectedStatus)
    {
        await using var factory = new ProtectedWriteEndpointFactory(
            apiAuthenticationEnabled: true,
            apiAuthenticationAllowAnonymousInDevelopment: false,
            apiAuthorizationEnabled: true,
            enableReadEndpointProtectionPilot: false,
            requireProjectReadAuthorization: false,
            requireBuildingReadAuthorization: false,
            enableWriteEndpointProtectionPilot: true,
            requireProjectWriteAuthorization: true,
            requireBuildingWriteAuthorization: true,
            returnNotFoundForTenantMismatch: returnNotFoundForTenantMismatch,
            apiAuthorizationAllowAnonymousInDevelopment: false,
            principalOrganizationId: 2001,
            principalPermissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Permission.ProjectsWrite.ToString()
            },
            projectScopeOrganizationId: 3001);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.PutAsJsonAsync("/api/v1/projects/1", new { name = "Renamed project" });

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Fact]
    public async Task ReadProtectionRemainsEffective_WhenWritePilotEnabled()
    {
        await using var factory = new ProtectedWriteEndpointFactory(
            apiAuthenticationEnabled: true,
            apiAuthenticationAllowAnonymousInDevelopment: false,
            apiAuthorizationEnabled: true,
            enableReadEndpointProtectionPilot: true,
            requireProjectReadAuthorization: true,
            requireBuildingReadAuthorization: true,
            enableWriteEndpointProtectionPilot: true,
            requireProjectWriteAuthorization: true,
            requireBuildingWriteAuthorization: true,
            returnNotFoundForTenantMismatch: false,
            apiAuthorizationAllowAnonymousInDevelopment: false,
            principalOrganizationId: 2001,
            principalPermissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Permission.ProjectsWrite.ToString()
            },
            projectScopeOrganizationId: 2001,
            buildingScopeOrganizationId: 2001);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.GetAsync("/api/v1/projects/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task WorkflowEndpointsRemainUnchangedByWritePilot()
    {
        await using var factory = new ProtectedWriteEndpointFactory(
            apiAuthenticationEnabled: false,
            apiAuthenticationAllowAnonymousInDevelopment: true,
            apiAuthorizationEnabled: true,
            enableReadEndpointProtectionPilot: false,
            requireProjectReadAuthorization: false,
            requireBuildingReadAuthorization: false,
            enableWriteEndpointProtectionPilot: true,
            requireProjectWriteAuthorization: true,
            requireBuildingWriteAuthorization: true,
            returnNotFoundForTenantMismatch: false,
            apiAuthorizationAllowAnonymousInDevelopment: true,
            principalOrganizationId: 2001,
            principalPermissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/engineering-workflow/jobs/non-existent-job");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private sealed class ProtectedWriteEndpointFactory : WebApplicationFactory<Program>
    {
        private readonly bool _apiAuthenticationEnabled;
        private readonly bool _apiAuthenticationAllowAnonymousInDevelopment;
        private readonly bool _apiAuthorizationEnabled;
        private readonly bool _enableReadEndpointProtectionPilot;
        private readonly bool _requireProjectReadAuthorization;
        private readonly bool _requireBuildingReadAuthorization;
        private readonly bool _enableWriteEndpointProtectionPilot;
        private readonly bool _requireProjectWriteAuthorization;
        private readonly bool _requireBuildingWriteAuthorization;
        private readonly bool _returnNotFoundForTenantMismatch;
        private readonly bool _apiAuthorizationAllowAnonymousInDevelopment;
        private readonly int? _projectScopeOrganizationId;
        private readonly int? _buildingScopeOrganizationId;
        private readonly AuthenticatedPrincipal _principal;
        private readonly List<Project> _projects;
        private readonly List<Building> _buildings;

        public ProtectedWriteEndpointFactory(
            bool apiAuthenticationEnabled,
            bool apiAuthenticationAllowAnonymousInDevelopment,
            bool apiAuthorizationEnabled,
            bool enableReadEndpointProtectionPilot,
            bool requireProjectReadAuthorization,
            bool requireBuildingReadAuthorization,
            bool enableWriteEndpointProtectionPilot,
            bool requireProjectWriteAuthorization,
            bool requireBuildingWriteAuthorization,
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
            _enableReadEndpointProtectionPilot = enableReadEndpointProtectionPilot;
            _requireProjectReadAuthorization = requireProjectReadAuthorization;
            _requireBuildingReadAuthorization = requireBuildingReadAuthorization;
            _enableWriteEndpointProtectionPilot = enableWriteEndpointProtectionPilot;
            _requireProjectWriteAuthorization = requireProjectWriteAuthorization;
            _requireBuildingWriteAuthorization = requireBuildingWriteAuthorization;
            _returnNotFoundForTenantMismatch = returnNotFoundForTenantMismatch;
            _apiAuthorizationAllowAnonymousInDevelopment = apiAuthorizationAllowAnonymousInDevelopment;
            _projectScopeOrganizationId = projectScopeOrganizationId;
            _buildingScopeOrganizationId = buildingScopeOrganizationId;

            _principal = new AuthenticatedPrincipal(
                UserId: 1001,
                OrganizationId: principalOrganizationId,
                ExternalSubjectId: "p5-11-protected-write-test-principal",
                AuthenticationScheme: ApiKeyAuthenticationHandler.SchemeName,
                Roles: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                Permissions: principalPermissions,
                IsAuthenticated: true);

            var project = Project.Create("Protected write integration project").Value;
            SetEntityId(project, 1);

            var building = Building.Create("Protected write integration building", project).Value;
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
                    ["ApiAuthorization:EnableReadEndpointProtectionPilot"] = _enableReadEndpointProtectionPilot ? "true" : "false",
                    ["ApiAuthorization:RequireProjectReadAuthorization"] = _requireProjectReadAuthorization ? "true" : "false",
                    ["ApiAuthorization:RequireBuildingReadAuthorization"] = _requireBuildingReadAuthorization ? "true" : "false",
                    ["ApiAuthorization:EnableWriteEndpointProtectionPilot"] = _enableWriteEndpointProtectionPilot ? "true" : "false",
                    ["ApiAuthorization:RequireProjectWriteAuthorization"] = _requireProjectWriteAuthorization ? "true" : "false",
                    ["ApiAuthorization:RequireBuildingWriteAuthorization"] = _requireBuildingWriteAuthorization ? "true" : "false",
                    ["ApiAuthorization:ReturnNotFoundForTenantMismatch"] = _returnNotFoundForTenantMismatch ? "true" : "false",
                    ["ApiAuthorization:AllowAnonymousInDevelopment"] = _apiAuthorizationAllowAnonymousInDevelopment ? "true" : "false"
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IApiKeyValidator>();
                services.AddSingleton<IApiKeyValidator>(new StubApiKeyValidator(ValidApiKey, _principal));

                services.RemoveAll<IUnitOfWork>();
                services.AddScoped<IUnitOfWork, NoOpUnitOfWork>();

                services.RemoveAll<IProjectRepository>();
                services.RemoveAll<IBuildingRepository>();
                services.RemoveAll<IClimateZoneRepository>();

                services.AddScoped<IProjectRepository>(_ => new StubProjectRepository(_projects));
                services.AddScoped<IBuildingRepository>(_ => new StubBuildingRepository(_buildings));
                services.AddScoped<IClimateZoneRepository, StubClimateZoneRepository>();

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

    private sealed class NoOpUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            return Task.FromResult(1);
        }
    }

    private sealed class StubProjectRepository : IProjectRepository
    {
        private readonly List<Project> _projects;

        public StubProjectRepository(List<Project> projects)
        {
            _projects = projects;
        }

        public Task<Project?> GetByIdAsync(
            int id,
            bool includeBuildings = false,
            CancellationToken cancellationToken = default)
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

        public void Add(Project project)
        {
            if (project.Id <= 0)
            {
                var nextId = _projects.Count == 0 ? 1 : _projects.Max(item => item.Id) + 1;
                var field = typeof(Project).GetField("<Id>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                field?.SetValue(project, nextId);
            }

            _projects.Add(project);
        }

        public void Remove(Project project)
        {
            _projects.Remove(project);
        }
    }

    private sealed class StubBuildingRepository : IBuildingRepository
    {
        private readonly List<Building> _buildings;

        public StubBuildingRepository(List<Building> buildings)
        {
            _buildings = buildings;
        }

        public Task<Building?> GetByIdAsync(
            int id,
            bool includeClimateZone = false,
            CancellationToken cancellationToken = default)
        {
            _ = includeClimateZone;
            _ = cancellationToken;
            return Task.FromResult(_buildings.FirstOrDefault(building => building.Id == id));
        }

        public Task<Building?> GetWithFloorsAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            return Task.FromResult(_buildings.FirstOrDefault(building => building.Id == id));
        }

        public Task<Building?> GetWithThermalZonesAndRoomsAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            return Task.FromResult(_buildings.FirstOrDefault(building => building.Id == id));
        }

        public Task<Building?> GetByThermalZoneIdAsync(
            int thermalZoneId,
            CancellationToken cancellationToken = default)
        {
            _ = thermalZoneId;
            _ = cancellationToken;
            return Task.FromResult<Building?>(null);
        }

        public Task<Building?> GetForCalculationAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            return Task.FromResult(_buildings.FirstOrDefault(building => building.Id == id));
        }

        public Task<Building?> GetForReportAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            return Task.FromResult(_buildings.FirstOrDefault(building => building.Id == id));
        }

        public Task<IReadOnlyList<Building>> ListByProjectIdAsync(
            int projectId,
            CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            return Task.FromResult<IReadOnlyList<Building>>(_buildings.Where(building => building.ProjectId == projectId).ToArray());
        }

        public void Add(Building building)
        {
            if (building.Id <= 0)
            {
                var nextId = _buildings.Count == 0 ? 1 : _buildings.Max(item => item.Id) + 1;
                var field = typeof(Building).GetField("<Id>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                field?.SetValue(building, nextId);
            }

            _buildings.Add(building);
        }

        public void Remove(Building building)
        {
            _buildings.Remove(building);
        }

        public Task<Building?> GetForValidationAsync(
            int id,
            bool asTracking = false,
            CancellationToken cancellationToken = default)
        {
            _ = asTracking;
            _ = cancellationToken;
            return Task.FromResult(_buildings.FirstOrDefault(building => building.Id == id));
        }
    }

    private sealed class StubClimateZoneRepository : IClimateZoneRepository
    {
        public Task<ClimateZone?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            _ = id;
            _ = cancellationToken;
            return Task.FromResult<ClimateZone?>(null);
        }
    }

    private sealed class FixedProjectScopeResolver : IProjectReadAccessScopeResolver
    {
        private readonly int _organizationId;

        public FixedProjectScopeResolver(int organizationId)
        {
            _organizationId = organizationId;
        }

        public Task<ProjectAccessScope?> ResolveProjectScopeAsync(
            int projectId,
            CancellationToken cancellationToken)
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

        public Task<BuildingAccessScope?> ResolveBuildingScopeAsync(
            int buildingId,
            CancellationToken cancellationToken)
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
