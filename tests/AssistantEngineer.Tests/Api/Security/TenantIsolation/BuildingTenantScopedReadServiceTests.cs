using AssistantEngineer.Api.Security.TenantIsolation;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Application.Services.Access;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Tests.Api.Security.TenantIsolation;

public sealed class BuildingTenantScopedReadServiceTests
{
    [Fact]
    public async Task SameTenantCanGetBuildingThroughParentProject()
    {
        var (project, building) = CreateBuilding(20, 10, "Tenant A building project", "Tenant A building", TenantIsolationScenario.TenantAOrganizationId);
        var service = CreateService([project], [building]);

        var result = await service.GetBuildingForTenantAsync(building.Id, TenantAContext(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(building.Id, result.Value!.Id);
    }

    [Fact]
    public async Task CrossTenantCannotGetBuilding()
    {
        var (project, building) = CreateBuilding(21, 11, "Tenant B building project", "Tenant B building", TenantIsolationScenario.TenantBOrganizationId);
        var service = CreateService([project], [building]);

        var result = await service.GetBuildingForTenantAsync(building.Id, TenantAContext(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Failure, result.ErrorType);
        Assert.Equal(TenantQueryFailureReasons.TenantMismatch, result.Error);
    }

    [Fact]
    public async Task CrossTenantCanReturnNotFoundForAntiEnumeration()
    {
        var (project, building) = CreateBuilding(22, 12, "Tenant B hidden building project", "Tenant B hidden building", TenantIsolationScenario.TenantBOrganizationId);
        var service = CreateService([project], [building]);

        var result = await service.GetBuildingForTenantAsync(
            building.Id,
            TenantAContext(returnNotFoundForTenantMismatch: true),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task ListBuildingsForProjectReturnsOnlySameTenantProjectBuildings()
    {
        var (tenantAProject, tenantABuilding) = CreateBuilding(23, 13, "Tenant A list building project", "Tenant A list building", TenantIsolationScenario.TenantAOrganizationId);
        var (tenantBProject, tenantBBuilding) = CreateBuilding(24, 14, "Tenant B list building project", "Tenant B list building", TenantIsolationScenario.TenantBOrganizationId);
        var service = CreateService([tenantAProject, tenantBProject], [tenantABuilding, tenantBBuilding]);

        var result = await service.ListBuildingsForProjectForTenantAsync(
            tenantAProject.Id,
            TenantAContext(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var building = Assert.Single(result.Value);
        Assert.Equal(tenantABuilding.Id, building.Id);
    }

    [Fact]
    public async Task ListBuildingsForCrossTenantProjectIsDenied()
    {
        var (tenantBProject, tenantBBuilding) = CreateBuilding(25, 15, "Tenant B denied building project", "Tenant B denied building", TenantIsolationScenario.TenantBOrganizationId);
        var service = CreateService([tenantBProject], [tenantBBuilding]);

        var result = await service.ListBuildingsForProjectForTenantAsync(
            tenantBProject.Id,
            TenantAContext(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(TenantQueryFailureReasons.TenantMismatch, result.Error);
    }

    [Fact]
    public async Task BuildingUnderTransitionUnscopedProjectFollowsCompatibilityOption()
    {
        var (project, building) = CreateBuilding(26, 16, "Transition building project", "Transition building", organizationId: null);
        var service = CreateService([project], [building]);

        var allowed = await service.GetBuildingForTenantAsync(
            building.Id,
            TenantAContext(allowUnscopedResourcesDuringTransition: true),
            CancellationToken.None);
        var denied = await service.GetBuildingForTenantAsync(
            building.Id,
            TenantAContext(allowUnscopedResourcesDuringTransition: false),
            CancellationToken.None);

        Assert.True(allowed.IsSuccess);
        Assert.True(denied.IsFailure);
        Assert.Equal(TenantQueryFailureReasons.UnscopedResourceDenied, denied.Error);
    }

    [Fact]
    public async Task MissingBuildingReturnsNotFound()
    {
        var service = CreateService([], []);

        var result = await service.GetBuildingForTenantAsync(404, TenantAContext(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
    }

    private static BuildingTenantScopedReadService CreateService(
        IReadOnlyList<Project> projects,
        IReadOnlyList<Building> buildings) =>
        new(
            new StubBuildingRepository(buildings),
            new StubProjectRepository(projects),
            new TenantQueryIsolationPolicy());

    private static (Project Project, Building Building) CreateBuilding(
        int buildingId,
        int projectId,
        string projectName,
        string buildingName,
        int? organizationId)
    {
        var project = Project.Create(projectName).Value;
        if (organizationId.HasValue)
        {
            Assert.True(project.AssignOrganization(organizationId.Value).IsSuccess);
        }

        SetEntityId(project, projectId);
        var building = Building.Create(buildingName, project).Value;
        SetEntityId(building, buildingId);
        SetPrivateProperty(building, "<ProjectId>k__BackingField", projectId);
        return (project, building);
    }

    private static TenantQueryContext TenantAContext(
        bool allowUnscopedResourcesDuringTransition = true,
        bool returnNotFoundForTenantMismatch = false) =>
        new(
            UserId: TenantIsolationScenario.TenantAUserId,
            OrganizationId: TenantIsolationScenario.TenantAOrganizationId,
            IsAuthenticated: true,
            Permissions: new HashSet<string>([Permission.BuildingsRead.ToString()], StringComparer.OrdinalIgnoreCase),
            AllowUnscopedResourcesDuringTransition: allowUnscopedResourcesDuringTransition,
            StrictTenantMatch: true,
            ReturnNotFoundForTenantMismatch: returnNotFoundForTenantMismatch);

    private static void SetEntityId(object entity, int id) =>
        SetPrivateProperty(entity, "<Id>k__BackingField", id);

    private static void SetPrivateProperty(object entity, string fieldName, object value)
    {
        var field = entity.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(entity, value);
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
            return Task.FromResult(_projects.SingleOrDefault(project => project.Id == id));
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
            return Task.FromResult(_buildings.SingleOrDefault(building => building.Id == id));
        }

        public Task<IReadOnlyList<Building>> ListByProjectIdAsync(int projectId, CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            return Task.FromResult<IReadOnlyList<Building>>(
                _buildings.Where(building => building.ProjectId == projectId).OrderBy(building => building.Id).ToArray());
        }

        public Task<Building?> GetWithFloorsAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Building?> GetWithThermalZonesAndRoomsAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Building?> GetByThermalZoneIdAsync(int thermalZoneId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Building?> GetForCalculationAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Building?> GetForReportAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Building?> GetForValidationAsync(int id, bool asTracking = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Add(Building building) => throw new NotSupportedException();
        public void Remove(Building building) => throw new NotSupportedException();
    }
}
