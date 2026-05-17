using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AssistantEngineer.Api;
using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Api.Security.ApiKey;
using AssistantEngineer.Api.Security.Authentication;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AssistantEngineer.Tests.Api.Security.TenantIsolation;

public sealed class ProtectedReadControllersTenantScopedQueryIntegrationTests
{
    private const string HeaderName = "X-AssistantEngineer-Api-Key";
    private const string ValidApiKey = "p5-16c-protected-read-test-key";
    private const int TenantAOrganizationId = 1001;
    private const int TenantBOrganizationId = 1002;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task Projects_ProtectionDisabled_PreservesGetAndListBehavior()
    {
        await using var factory = new ProtectedReadControllerFactory(
            apiAuthenticationEnabled: false,
            apiAuthorizationEnabled: false,
            returnNotFoundForTenantMismatch: false,
            allowUnscopedProjectsDuringTransition: true,
            principalOrganizationId: TenantAOrganizationId,
            principalPermissions: PermissionSet());

        var client = factory.CreateClient();

        var getResponse = await client.GetAsync("/api/v1/projects/11");
        var listResponse = await client.GetAsync("/api/v1/projects");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var listPayload = await listResponse.Content.ReadFromJsonAsync<PagedResponse<ProjectResponse>>(JsonOptions);
        Assert.NotNull(listPayload);
        Assert.Equal(3, listPayload.Items.Count);
    }

    [Fact]
    public async Task Projects_ProtectionEnabled_AnonymousGet_ReturnsUnauthorized()
    {
        await using var factory = CreateProtectedFactory(
            permissions: PermissionSet(Permission.ProjectsRead));

        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/projects/10");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Projects_ProtectionEnabled_MissingProjectsRead_ReturnsForbidden()
    {
        await using var factory = CreateProtectedFactory(
            permissions: PermissionSet());

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.GetAsync("/api/v1/projects/10");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Projects_ProtectionEnabled_SameTenantWithProjectsRead_CanGetProject()
    {
        await using var factory = CreateProtectedFactory(
            permissions: PermissionSet(Permission.ProjectsRead));

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.GetAsync("/api/v1/projects/10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(false, HttpStatusCode.Forbidden)]
    [InlineData(true, HttpStatusCode.NotFound)]
    public async Task Projects_ProtectionEnabled_CrossTenantGet_RespectsNotFoundOption(
        bool returnNotFoundForTenantMismatch,
        HttpStatusCode expectedStatus)
    {
        await using var factory = CreateProtectedFactory(
            permissions: PermissionSet(Permission.ProjectsRead),
            returnNotFoundForTenantMismatch: returnNotFoundForTenantMismatch);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.GetAsync("/api/v1/projects/11");

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Fact]
    public async Task Projects_List_ReturnsOnlySameTenantProjects_AndExcludesOtherTenantProjects()
    {
        await using var factory = CreateProtectedFactory(
            permissions: PermissionSet(Permission.ProjectsRead),
            allowUnscopedProjectsDuringTransition: false);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.GetAsync("/api/v1/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PagedResponse<ProjectResponse>>(JsonOptions);
        Assert.NotNull(payload);

        var ids = payload.Items.Select(project => project.Id).ToHashSet();
        Assert.Contains(10, ids);
        Assert.DoesNotContain(11, ids);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task Projects_List_LegacyUnscopedBehavior_FollowsOption(
        bool allowUnscopedProjectsDuringTransition,
        bool expectedLegacyInclusion)
    {
        await using var factory = CreateProtectedFactory(
            permissions: PermissionSet(Permission.ProjectsRead),
            allowUnscopedProjectsDuringTransition: allowUnscopedProjectsDuringTransition);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.GetAsync("/api/v1/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PagedResponse<ProjectResponse>>(JsonOptions);
        Assert.NotNull(payload);

        var ids = payload.Items.Select(project => project.Id).ToHashSet();
        Assert.Equal(expectedLegacyInclusion, ids.Contains(12));
    }

    [Fact]
    public async Task Buildings_ProtectionDisabled_PreservesGetAndListBehavior()
    {
        await using var factory = new ProtectedReadControllerFactory(
            apiAuthenticationEnabled: false,
            apiAuthorizationEnabled: false,
            returnNotFoundForTenantMismatch: false,
            allowUnscopedProjectsDuringTransition: true,
            principalOrganizationId: TenantAOrganizationId,
            principalPermissions: PermissionSet());

        var client = factory.CreateClient();

        var getResponse = await client.GetAsync("/api/v1/buildings/22");
        var listResponse = await client.GetAsync("/api/v1/projects/11/buildings");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
    }

    [Fact]
    public async Task Buildings_ProtectionEnabled_AnonymousGet_ReturnsUnauthorized()
    {
        await using var factory = CreateProtectedFactory(
            permissions: PermissionSet(Permission.BuildingsRead));

        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/buildings/20");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Buildings_ProtectionEnabled_MissingBuildingsRead_ReturnsForbidden()
    {
        await using var factory = CreateProtectedFactory(
            permissions: PermissionSet(Permission.ProjectsRead));

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.GetAsync("/api/v1/buildings/20");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Buildings_ProtectionEnabled_SameTenantWithBuildingsRead_CanGetBuilding()
    {
        await using var factory = CreateProtectedFactory(
            permissions: PermissionSet(Permission.BuildingsRead));

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.GetAsync("/api/v1/buildings/20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(false, HttpStatusCode.Forbidden)]
    [InlineData(true, HttpStatusCode.NotFound)]
    public async Task Buildings_ProtectionEnabled_CrossTenantGet_RespectsNotFoundOption(
        bool returnNotFoundForTenantMismatch,
        HttpStatusCode expectedStatus)
    {
        await using var factory = CreateProtectedFactory(
            permissions: PermissionSet(Permission.BuildingsRead),
            returnNotFoundForTenantMismatch: returnNotFoundForTenantMismatch);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.GetAsync("/api/v1/buildings/22");

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Fact]
    public async Task Buildings_ListByProject_ReturnsOnlyRequestedProjectBuildingsForSameTenant()
    {
        await using var factory = CreateProtectedFactory(
            permissions: PermissionSet(Permission.BuildingsRead));

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.GetAsync("/api/v1/projects/10/buildings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PagedResponse<BuildingResponse>>(JsonOptions);
        Assert.NotNull(payload);

        var ids = payload.Items.Select(building => building.Id).ToHashSet();
        Assert.Contains(20, ids);
        Assert.Contains(21, ids);
        Assert.DoesNotContain(22, ids);
        Assert.DoesNotContain(23, ids);
    }

    [Theory]
    [InlineData(true, HttpStatusCode.OK, true)]
    [InlineData(false, HttpStatusCode.Forbidden, false)]
    public async Task Buildings_LegacyUnscopedProjectBehavior_FollowsOption(
        bool allowUnscopedProjectsDuringTransition,
        HttpStatusCode expectedStatus,
        bool expectLegacyBuilding)
    {
        await using var factory = CreateProtectedFactory(
            permissions: PermissionSet(Permission.BuildingsRead),
            allowUnscopedProjectsDuringTransition: allowUnscopedProjectsDuringTransition,
            returnNotFoundForTenantMismatch: false);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.GetAsync("/api/v1/projects/12/buildings");

        Assert.Equal(expectedStatus, response.StatusCode);

        if (expectedStatus == HttpStatusCode.OK)
        {
            var payload = await response.Content.ReadFromJsonAsync<PagedResponse<BuildingResponse>>(JsonOptions);
            Assert.NotNull(payload);
            var ids = payload.Items.Select(building => building.Id).ToHashSet();
            Assert.Equal(expectLegacyBuilding, ids.Contains(23));
        }
    }

    private static ProtectedReadControllerFactory CreateProtectedFactory(
        IReadOnlySet<string> permissions,
        bool returnNotFoundForTenantMismatch = false,
        bool allowUnscopedProjectsDuringTransition = true)
    {
        return new ProtectedReadControllerFactory(
            apiAuthenticationEnabled: true,
            apiAuthorizationEnabled: true,
            returnNotFoundForTenantMismatch: returnNotFoundForTenantMismatch,
            allowUnscopedProjectsDuringTransition: allowUnscopedProjectsDuringTransition,
            principalOrganizationId: TenantAOrganizationId,
            principalPermissions: permissions);
    }

    private static IReadOnlySet<string> PermissionSet(params Permission[] permissions)
    {
        return permissions
            .Select(permission => permission.ToString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class ProtectedReadControllerFactory : WebApplicationFactory<Program>
    {
        private readonly bool _apiAuthenticationEnabled;
        private readonly bool _apiAuthorizationEnabled;
        private readonly bool _returnNotFoundForTenantMismatch;
        private readonly bool _allowUnscopedProjectsDuringTransition;
        private readonly AuthenticatedPrincipal _principal;
        private readonly IReadOnlyList<Project> _projects;
        private readonly IReadOnlyList<Building> _buildings;

        public ProtectedReadControllerFactory(
            bool apiAuthenticationEnabled,
            bool apiAuthorizationEnabled,
            bool returnNotFoundForTenantMismatch,
            bool allowUnscopedProjectsDuringTransition,
            int principalOrganizationId,
            IReadOnlySet<string> principalPermissions)
        {
            _apiAuthenticationEnabled = apiAuthenticationEnabled;
            _apiAuthorizationEnabled = apiAuthorizationEnabled;
            _returnNotFoundForTenantMismatch = returnNotFoundForTenantMismatch;
            _allowUnscopedProjectsDuringTransition = allowUnscopedProjectsDuringTransition;

            _principal = new AuthenticatedPrincipal(
                UserId: 2001,
                OrganizationId: principalOrganizationId,
                ExternalSubjectId: "p5-16c-protected-read-principal",
                AuthenticationScheme: ApiKeyAuthenticationHandler.SchemeName,
                Roles: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                Permissions: principalPermissions,
                IsAuthenticated: true);

            var tenantAProject = CreateProject(10, "Tenant A project", TenantAOrganizationId);
            var tenantBProject = CreateProject(11, "Tenant B project", TenantBOrganizationId);
            var legacyProject = CreateProject(12, "Legacy unscoped project", organizationId: null);

            var tenantABuildingOne = CreateBuilding(20, "Tenant A building one", tenantAProject);
            var tenantABuildingTwo = CreateBuilding(21, "Tenant A building two", tenantAProject);
            var tenantBBuilding = CreateBuilding(22, "Tenant B building", tenantBProject);
            var legacyBuilding = CreateBuilding(23, "Legacy unscoped building", legacyProject);

            Assert.True(tenantAProject.AddBuilding(tenantABuildingOne).IsSuccess);
            Assert.True(tenantAProject.AddBuilding(tenantABuildingTwo).IsSuccess);
            Assert.True(tenantBProject.AddBuilding(tenantBBuilding).IsSuccess);
            Assert.True(legacyProject.AddBuilding(legacyBuilding).IsSuccess);

            _projects = [tenantAProject, tenantBProject, legacyProject];
            _buildings = [tenantABuildingOne, tenantABuildingTwo, tenantBBuilding, legacyBuilding];
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
                    ["ApiAuthentication:AllowAnonymousInDevelopment"] = "false",
                    ["ApiAuthentication:ApiKeyHeaderName"] = HeaderName,
                    ["ApiAuthentication:EnableApiKeyAuthentication"] = "true",
                    ["ApiAuthentication:EnableJwtBearerAuthentication"] = "false",
                    ["ApiAuthorization:Enabled"] = _apiAuthorizationEnabled ? "true" : "false",
                    ["ApiAuthorization:EnableReadEndpointProtectionPilot"] = _apiAuthorizationEnabled ? "true" : "false",
                    ["ApiAuthorization:RequireProjectReadAuthorization"] = _apiAuthorizationEnabled ? "true" : "false",
                    ["ApiAuthorization:RequireBuildingReadAuthorization"] = _apiAuthorizationEnabled ? "true" : "false",
                    ["ApiAuthorization:ReturnNotFoundForTenantMismatch"] = _returnNotFoundForTenantMismatch ? "true" : "false",
                    ["ApiAuthorization:AllowAnonymousInDevelopment"] = "false",
                    ["Identity:ProjectTenantAccess:AllowUnscopedProjectsDuringTransition"] = _allowUnscopedProjectsDuringTransition ? "true" : "false",
                    ["Identity:ProjectTenantAccess:TreatMissingTenantAsBlocking"] = "false",
                    ["Identity:ProjectTenantAccess:EnableStrictTenantMatch"] = "true"
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
            });
        }

        private static Project CreateProject(int id, string name, int? organizationId)
        {
            var project = Project.Create(name).Value;
            SetEntityId(project, id);

            if (organizationId.HasValue)
            {
                Assert.True(project.AssignOrganization(organizationId.Value).IsSuccess);
            }

            return project;
        }

        private static Building CreateBuilding(int id, string name, Project project)
        {
            var building = Building.Create(name, project).Value;
            SetEntityId(building, id);
            return building;
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

        public Task<Building?> GetByIdAsync(
            int id,
            bool includeClimateZone = false,
            CancellationToken cancellationToken = default)
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
            return Task.FromResult<IReadOnlyList<Building>>(
                _buildings.Where(building => building.ProjectId == projectId).ToArray());
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
}
