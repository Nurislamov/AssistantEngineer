using AssistantEngineer.Api.Security.TenantIsolation;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Application.Services.Access;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Tests.Api.Security.TenantIsolation;

public sealed class ProjectTenantScopedReadServiceTests
{
    [Fact]
    public async Task SameTenantCanGetProject()
    {
        var project = CreateProject(id: 10, "Tenant A project", TenantIsolationScenario.TenantAOrganizationId);
        var service = CreateService(project);

        var result = await service.GetProjectForTenantAsync(project.Id, TenantAContext(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(project.Id, result.Value!.Id);
    }

    [Fact]
    public async Task CrossTenantCannotGetProject()
    {
        var project = CreateProject(id: 11, "Tenant B project", TenantIsolationScenario.TenantBOrganizationId);
        var service = CreateService(project);

        var result = await service.GetProjectForTenantAsync(project.Id, TenantAContext(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Failure, result.ErrorType);
        Assert.Equal(TenantQueryFailureReasons.TenantMismatch, result.Error);
    }

    [Fact]
    public async Task CrossTenantCanReturnNotFoundForAntiEnumeration()
    {
        var project = CreateProject(id: 12, "Tenant B anti-enumeration project", TenantIsolationScenario.TenantBOrganizationId);
        var service = CreateService(project);

        var result = await service.GetProjectForTenantAsync(
            project.Id,
            TenantAContext(returnNotFoundForTenantMismatch: true),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task ListProjectsReturnsOnlyTenantProjects()
    {
        var tenantAProject = CreateProject(id: 13, "Tenant A list project", TenantIsolationScenario.TenantAOrganizationId);
        var tenantBProject = CreateProject(id: 14, "Tenant B list project", TenantIsolationScenario.TenantBOrganizationId);
        var service = CreateService(tenantAProject, tenantBProject);

        var result = await service.ListProjectsForTenantAsync(TenantAContext(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var project = Assert.Single(result.Value);
        Assert.Equal(tenantAProject.Id, project.Id);
    }

    [Fact]
    public async Task ListProjectsDoesNotIncludeTransitionUnscopedByDefault()
    {
        var tenantAProject = CreateProject(id: 15, "Tenant A without transition list project", TenantIsolationScenario.TenantAOrganizationId);
        var transitionProject = CreateProject(id: 16, "Transition unscoped project", organizationId: null);
        var service = CreateService(tenantAProject, transitionProject);

        var result = await service.ListProjectsForTenantAsync(TenantAContext(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var project = Assert.Single(result.Value);
        Assert.Equal(tenantAProject.Id, project.Id);
    }

    [Fact]
    public async Task ListProjectsCanIncludeTransitionUnscopedWhenExplicitlyRequested()
    {
        var tenantAProject = CreateProject(id: 17, "Tenant A with transition list project", TenantIsolationScenario.TenantAOrganizationId);
        var transitionProject = CreateProject(id: 18, "Explicit transition unscoped project", organizationId: null);
        var service = CreateService(tenantAProject, transitionProject);

        var result = await service.ListProjectsForTenantAsync(
            TenantAContext(includeUnscopedResourcesInTenantLists: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task TransitionUnscopedProjectFollowsCompatibilityOption()
    {
        var transitionProject = CreateProject(id: 19, "Transition get project", organizationId: null);
        var service = CreateService(transitionProject);

        var allowed = await service.GetProjectForTenantAsync(
            transitionProject.Id,
            TenantAContext(allowUnscopedResourcesDuringTransition: true),
            CancellationToken.None);
        var denied = await service.GetProjectForTenantAsync(
            transitionProject.Id,
            TenantAContext(allowUnscopedResourcesDuringTransition: false),
            CancellationToken.None);

        Assert.True(allowed.IsSuccess);
        Assert.True(denied.IsFailure);
        Assert.Equal(TenantQueryFailureReasons.UnscopedResourceDenied, denied.Error);
    }

    [Fact]
    public async Task MissingProjectReturnsNotFound()
    {
        var service = CreateService();

        var result = await service.GetProjectForTenantAsync(404, TenantAContext(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
    }

    private static ProjectTenantScopedReadService CreateService(params Project[] projects) =>
        new(
            new StubProjectRepository(projects),
            new TenantQueryIsolationPolicy());

    private static Project CreateProject(int id, string name, int? organizationId)
    {
        var project = Project.Create(name).Value;
        if (organizationId.HasValue)
        {
            Assert.True(project.AssignOrganization(organizationId.Value).IsSuccess);
        }

        SetEntityId(project, id);
        return project;
    }

    private static TenantQueryContext TenantAContext(
        bool allowUnscopedResourcesDuringTransition = true,
        bool returnNotFoundForTenantMismatch = false,
        bool includeUnscopedResourcesInTenantLists = false) =>
        new(
            UserId: TenantIsolationScenario.TenantAUserId,
            OrganizationId: TenantIsolationScenario.TenantAOrganizationId,
            IsAuthenticated: true,
            Permissions: new HashSet<string>([Permission.ProjectsRead.ToString()], StringComparer.OrdinalIgnoreCase),
            AllowUnscopedResourcesDuringTransition: allowUnscopedResourcesDuringTransition,
            StrictTenantMatch: true,
            ReturnNotFoundForTenantMismatch: returnNotFoundForTenantMismatch,
            IncludeUnscopedResourcesInTenantLists: includeUnscopedResourcesInTenantLists);

    private static void SetEntityId(object entity, int id)
    {
        var field = entity.GetType().GetField("<Id>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(entity, id);
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
}
