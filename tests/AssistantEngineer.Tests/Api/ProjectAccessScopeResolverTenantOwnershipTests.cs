using AssistantEngineer.Api.Security.Authorization;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Entities;

namespace AssistantEngineer.Tests.Api;

public sealed class ProjectAccessScopeResolverTenantOwnershipTests
{
    [Fact]
    public async Task ProjectWithOrganizationId_ResolvesTenantScopedScope()
    {
        var project = CreateProject(id: 10);
        Assert.True(project.AssignOrganization(1001).IsSuccess);
        Assert.True(project.AssignOwnerUser(2001).IsSuccess);
        var resolver = new DefaultProjectReadAccessScopeResolver(new StubProjectRepository(project));

        var scope = await resolver.ResolveProjectScopeAsync(10, CancellationToken.None);

        Assert.NotNull(scope);
        Assert.Equal(10, scope.ProjectId);
        Assert.Equal(1001, scope.OrganizationId);
        Assert.Equal(2001, scope.OwnerUserId);
        Assert.True(scope.IsTenantScoped);
        Assert.NotNull(scope.TenantScope);
        Assert.Equal(1001, scope.TenantScope.OrganizationId);
    }

    [Fact]
    public async Task ProjectWithoutOrganizationId_ResolvesLegacyUnscopedScope()
    {
        var project = CreateProject(id: 10);
        var resolver = new DefaultProjectReadAccessScopeResolver(new StubProjectRepository(project));

        var scope = await resolver.ResolveProjectScopeAsync(10, CancellationToken.None);

        Assert.NotNull(scope);
        Assert.Null(scope.OrganizationId);
        Assert.Null(scope.OwnerUserId);
        Assert.False(scope.IsTenantScoped);
        Assert.Null(scope.TenantScope);
    }

    [Fact]
    public async Task MissingProject_ReturnsNull()
    {
        var resolver = new DefaultProjectReadAccessScopeResolver(new StubProjectRepository(project: null));

        var scope = await resolver.ResolveProjectScopeAsync(404, CancellationToken.None);

        Assert.Null(scope);
    }

    private static Project CreateProject(int id)
    {
        var project = Project.Create("Tenant resolver project").Value;
        SetEntityId(project, id);
        return project;
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
}
