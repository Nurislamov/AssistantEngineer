using AssistantEngineer.Api.Security.Authorization;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Entities;

namespace AssistantEngineer.Tests.Api;

public sealed class BuildingAccessScopeResolverTenantOwnershipTests
{
    [Fact]
    public async Task BuildingUnderTenantOwnedProject_ResolvesOrganizationFromProject()
    {
        var project = CreateProject(id: 10);
        Assert.True(project.AssignOrganization(1001).IsSuccess);
        Assert.True(project.AssignOwnerUser(2001).IsSuccess);
        var building = CreateBuilding(id: 20, project);
        var projectResolver = new DefaultProjectReadAccessScopeResolver(new StubProjectRepository(project));
        var resolver = new DefaultBuildingReadAccessScopeResolver(new StubBuildingRepository(building), projectResolver);

        var scope = await resolver.ResolveBuildingScopeAsync(20, CancellationToken.None);

        Assert.NotNull(scope);
        Assert.Equal(20, scope.BuildingId);
        Assert.Equal(10, scope.ProjectId);
        Assert.Equal(1001, scope.OrganizationId);
        Assert.Equal(2001, scope.OwnerUserId);
        Assert.True(scope.IsTenantScoped);
        Assert.NotNull(scope.TenantScope);
    }

    [Fact]
    public async Task BuildingUnderUnscopedProject_RemainsUnscoped()
    {
        var project = CreateProject(id: 10);
        var building = CreateBuilding(id: 20, project);
        var projectResolver = new DefaultProjectReadAccessScopeResolver(new StubProjectRepository(project));
        var resolver = new DefaultBuildingReadAccessScopeResolver(new StubBuildingRepository(building), projectResolver);

        var scope = await resolver.ResolveBuildingScopeAsync(20, CancellationToken.None);

        Assert.NotNull(scope);
        Assert.Null(scope.OrganizationId);
        Assert.Null(scope.OwnerUserId);
        Assert.False(scope.IsTenantScoped);
        Assert.Null(scope.TenantScope);
    }

    [Fact]
    public async Task MissingBuilding_ReturnsNull()
    {
        var projectResolver = new DefaultProjectReadAccessScopeResolver(new StubProjectRepository(project: null));
        var resolver = new DefaultBuildingReadAccessScopeResolver(new StubBuildingRepository(building: null), projectResolver);

        var scope = await resolver.ResolveBuildingScopeAsync(404, CancellationToken.None);

        Assert.Null(scope);
    }

    private static Project CreateProject(int id)
    {
        var project = Project.Create("Building resolver project").Value;
        SetEntityId(project, id);
        return project;
    }

    private static Building CreateBuilding(int id, Project project)
    {
        var building = Building.Create("Building resolver building", project).Value;
        SetEntityId(building, id);
        return building;
    }

    private static void SetEntityId(object entity, int id)
    {
        var field = entity.GetType().GetField("<Id>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(entity, id);
    }

    private sealed class StubProjectRepository : IProjectRepository
    {
        private readonly Project? _project;

        public StubProjectRepository(Project? project)
        {
            _project = project;
        }

        public Task<Project?> GetByIdAsync(int id, bool includeBuildings = false, CancellationToken cancellationToken = default)
        {
            _ = includeBuildings;
            _ = cancellationToken;
            return Task.FromResult(_project?.Id == id ? _project : null);
        }

        public Task<IReadOnlyList<Project>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Project>>(_project is null ? [] : [_project]);

        public void Add(Project project) => throw new NotSupportedException();

        public void Remove(Project project) => throw new NotSupportedException();
    }

    private sealed class StubBuildingRepository : IBuildingRepository
    {
        private readonly Building? _building;

        public StubBuildingRepository(Building? building)
        {
            _building = building;
        }

        public Task<Building?> GetByIdAsync(int id, bool includeClimateZone = false, CancellationToken cancellationToken = default)
        {
            _ = includeClimateZone;
            _ = cancellationToken;
            return Task.FromResult(_building?.Id == id ? _building : null);
        }

        public Task<Building?> GetWithFloorsAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Building?> GetWithThermalZonesAndRoomsAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Building?> GetByThermalZoneIdAsync(int thermalZoneId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Building?> GetForCalculationAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Building?> GetForReportAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Building>> ListByProjectIdAsync(int projectId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Building?> GetForValidationAsync(int id, bool asTracking = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Add(Building building) => throw new NotSupportedException();
        public void Remove(Building building) => throw new NotSupportedException();
    }
}
