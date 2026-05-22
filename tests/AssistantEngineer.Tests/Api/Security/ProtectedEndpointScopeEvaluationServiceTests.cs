using AssistantEngineer.Api.Security.Authentication;
using AssistantEngineer.Api.Security.Authorization;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Application.Services.Access;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Api.Security;

public sealed class ProtectedEndpointScopeEvaluationServiceTests
{
    [Fact]
    public async Task ProjectScope_SameTenant_ReturnsAllowed()
    {
        var service = CreateService(
            projectScope: CreateProjectScope(organizationId: 1001));

        var result = await service.EvaluateProjectScopeAsync(
            CreatePrincipal(organizationId: 1001, Permission.ProjectsRead),
            projectId: 10,
            Permission.ProjectsRead,
            CancellationToken.None);

        Assert.Equal(ProtectedEndpointScopeEvaluationKind.Allowed, result.Kind);
        Assert.Equal(ProtectedEndpointScopeKind.Project, result.ScopeKind);
    }

    [Fact]
    public async Task BuildingScope_SameTenant_ReturnsAllowed()
    {
        var service = CreateService(
            buildingScope: CreateBuildingScope(organizationId: 1001));

        var result = await service.EvaluateBuildingScopeAsync(
            CreatePrincipal(organizationId: 1001, Permission.BuildingsRead),
            buildingId: 20,
            Permission.BuildingsRead,
            CancellationToken.None);

        Assert.Equal(ProtectedEndpointScopeEvaluationKind.Allowed, result.Kind);
        Assert.Equal(ProtectedEndpointScopeKind.Building, result.ScopeKind);
    }

    [Fact]
    public async Task WorkflowScope_ReadAndExecute_ResolveSameAsResolver()
    {
        var service = CreateService(
            workflowScope: CreateWorkflowScope("wf-1", organizationId: 1001));
        var principal = CreatePrincipal(organizationId: 1001, Permission.WorkflowsRead, Permission.WorkflowsExecute);

        var readResult = await service.EvaluateWorkflowScopeAsync(
            principal,
            "wf-1",
            Permission.WorkflowsRead,
            CancellationToken.None);
        var executeResult = await service.EvaluateWorkflowScopeAsync(
            principal,
            "wf-1",
            Permission.WorkflowsExecute,
            CancellationToken.None);

        Assert.Equal(ProtectedEndpointScopeEvaluationKind.Allowed, readResult.Kind);
        Assert.Equal(ProtectedEndpointScopeEvaluationKind.Allowed, executeResult.Kind);
    }

    [Fact]
    public async Task MissingProjectOrBuildingScope_ReturnsScopeMissing_NotTenantMismatch()
    {
        var service = CreateService(projectScope: null, buildingScope: null);
        var principal = CreatePrincipal(organizationId: 1001, Permission.ProjectsRead, Permission.BuildingsRead);

        var projectResult = await service.EvaluateProjectScopeAsync(
            principal,
            projectId: 10,
            Permission.ProjectsRead,
            CancellationToken.None);
        var buildingResult = await service.EvaluateBuildingScopeAsync(
            principal,
            buildingId: 20,
            Permission.BuildingsRead,
            CancellationToken.None);

        Assert.Equal(ProtectedEndpointScopeEvaluationKind.ScopeMissing, projectResult.Kind);
        Assert.Equal(ProtectedEndpointScopeEvaluationKind.ScopeMissing, buildingResult.Kind);
        Assert.False(projectResult.TenantMismatch);
        Assert.False(buildingResult.TenantMismatch);
    }

    [Fact]
    public async Task TenantMismatch_ReturnsTenantMismatch()
    {
        var service = CreateService(
            projectScope: CreateProjectScope(organizationId: 3001),
            buildingScope: CreateBuildingScope(organizationId: 3001),
            workflowScope: CreateWorkflowScope("wf-1", organizationId: 3001));
        var principal = CreatePrincipal(organizationId: 1001, Permission.ProjectsRead, Permission.BuildingsRead, Permission.WorkflowsRead);

        var projectResult = await service.EvaluateProjectScopeAsync(
            principal,
            10,
            Permission.ProjectsRead,
            CancellationToken.None);
        var buildingResult = await service.EvaluateBuildingScopeAsync(
            principal,
            20,
            Permission.BuildingsRead,
            CancellationToken.None);
        var workflowResult = await service.EvaluateWorkflowScopeAsync(
            principal,
            "wf-1",
            Permission.WorkflowsRead,
            CancellationToken.None);

        Assert.Equal(ProtectedEndpointScopeEvaluationKind.TenantMismatch, projectResult.Kind);
        Assert.Equal(ProtectedEndpointScopeEvaluationKind.TenantMismatch, buildingResult.Kind);
        Assert.Equal(ProtectedEndpointScopeEvaluationKind.TenantMismatch, workflowResult.Kind);
    }

    [Fact]
    public async Task ScenarioAndJobScopes_AreSupported()
    {
        var scenarioScope = CreateWorkflowScope("sc-1", organizationId: 1001);
        var jobScope = CreateWorkflowScope("job-1", organizationId: 1001);
        var service = CreateService(
            workflowScope: null,
            scenarioScope: scenarioScope,
            jobScope: jobScope);
        var principal = CreatePrincipal(organizationId: 1001, Permission.WorkflowsRead);

        var scenarioResult = await service.EvaluateScenarioScopeAsync(
            principal,
            "sc-1",
            Permission.WorkflowsRead,
            CancellationToken.None);
        var jobResult = await service.EvaluateJobScopeAsync(
            principal,
            "job-1",
            Permission.WorkflowsRead,
            CancellationToken.None);

        Assert.Equal(ProtectedEndpointScopeEvaluationKind.Allowed, scenarioResult.Kind);
        Assert.Equal(ProtectedEndpointScopeEvaluationKind.Allowed, jobResult.Kind);
        Assert.Equal(ProtectedEndpointScopeKind.WorkflowScenario, scenarioResult.ScopeKind);
        Assert.Equal(ProtectedEndpointScopeKind.WorkflowJob, jobResult.ScopeKind);
    }

    [Fact]
    public async Task RoomAndFloorScopes_UseBuildingFallbackSemantics()
    {
        var service = CreateService(
            buildingScope: CreateBuildingScope(organizationId: 1001),
            floorScope: CreateFloorScope(buildingId: 20, projectId: 10),
            roomScope: CreateRoomScope(floorId: 30, buildingId: 20, projectId: 10));
        var principal = CreatePrincipal(organizationId: 1001, Permission.WorkflowsExecute);

        var floorResult = await service.EvaluateFloorScopeAsync(
            principal,
            floorId: 30,
            Permission.WorkflowsExecute,
            CancellationToken.None);
        var roomResult = await service.EvaluateRoomScopeAsync(
            principal,
            roomId: 40,
            Permission.WorkflowsExecute,
            CancellationToken.None);

        Assert.Equal(ProtectedEndpointScopeEvaluationKind.Allowed, floorResult.Kind);
        Assert.Equal(ProtectedEndpointScopeEvaluationKind.Allowed, roomResult.Kind);
    }

    private static ProtectedEndpointScopeEvaluationService CreateService(
        ProjectAccessScope? projectScope = null,
        BuildingAccessScope? buildingScope = null,
        WorkflowAccessScope? workflowScope = null,
        WorkflowAccessScope? scenarioScope = null,
        WorkflowAccessScope? jobScope = null,
        FloorAccessScope? floorScope = null,
        RoomAccessScope? roomScope = null)
    {
        return new ProtectedEndpointScopeEvaluationService(
            new StubProjectScopeResolver(projectScope),
            new StubBuildingScopeResolver(buildingScope),
            new StubFloorScopeResolver(floorScope),
            new StubRoomScopeResolver(roomScope),
            new StubWorkflowScopeResolver(workflowScope, scenarioScope, jobScope),
            new ProjectTenantAccessPolicy(Options.Create(new ProjectTenantAccessOptions
            {
                AllowUnscopedProjectsDuringTransition = false,
                TreatMissingTenantAsBlocking = true,
                EnableStrictTenantMatch = true
            })));
    }

    private static AuthenticatedPrincipal CreatePrincipal(int organizationId, params Permission[] permissions)
    {
        return new AuthenticatedPrincipal(
            UserId: 101,
            OrganizationId: organizationId,
            ExternalSubjectId: "p8-03c-scope-evaluator-principal",
            AuthenticationScheme: "Test",
            Roles: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            Permissions: new HashSet<string>(
                permissions.Select(permission => permission.ToString()),
                StringComparer.OrdinalIgnoreCase),
            IsAuthenticated: true);
    }

    private static ProjectAccessScope CreateProjectScope(int organizationId)
    {
        return ProjectTenantAccessScopeFactory.CreateProjectScope(
            projectId: 10,
            organizationId: organizationId,
            ownerUserId: null,
            isTenantScoped: true,
            tenantScope: new TenantScope(organizationId, $"org-{organizationId}", IsActive: true));
    }

    private static BuildingAccessScope CreateBuildingScope(int organizationId)
    {
        return ProjectTenantAccessScopeFactory.CreateBuildingScope(
            buildingId: 20,
            projectId: 10,
            organizationId: organizationId,
            ownerUserId: null,
            isTenantScoped: true,
            tenantScope: new TenantScope(organizationId, $"org-{organizationId}", IsActive: true));
    }

    private static WorkflowAccessScope CreateWorkflowScope(string workflowId, int organizationId)
    {
        return ProjectTenantAccessScopeFactory.CreateWorkflowScope(
            workflowId: workflowId,
            projectId: 10,
            buildingId: 20,
            organizationId: organizationId,
            ownerUserId: null,
            isTenantScoped: true,
            tenantScope: new TenantScope(organizationId, $"org-{organizationId}", IsActive: true));
    }

    private static FloorAccessScope CreateFloorScope(int buildingId, int projectId)
    {
        return new FloorAccessScope(
            FloorId: 30,
            BuildingId: buildingId,
            ProjectId: projectId,
            OrganizationId: null,
            OwnerUserId: null,
            IsTenantScoped: false);
    }

    private static RoomAccessScope CreateRoomScope(int floorId, int buildingId, int projectId)
    {
        return new RoomAccessScope(
            RoomId: 40,
            FloorId: floorId,
            BuildingId: buildingId,
            ProjectId: projectId,
            OrganizationId: null,
            OwnerUserId: null,
            IsTenantScoped: false);
    }

    private sealed class StubProjectScopeResolver : IProjectReadAccessScopeResolver
    {
        private readonly ProjectAccessScope? _scope;

        public StubProjectScopeResolver(ProjectAccessScope? scope)
        {
            _scope = scope;
        }

        public Task<ProjectAccessScope?> ResolveProjectScopeAsync(int projectId, CancellationToken cancellationToken)
        {
            _ = projectId;
            _ = cancellationToken;
            return Task.FromResult(_scope);
        }
    }

    private sealed class StubBuildingScopeResolver : IBuildingReadAccessScopeResolver
    {
        private readonly BuildingAccessScope? _scope;

        public StubBuildingScopeResolver(BuildingAccessScope? scope)
        {
            _scope = scope;
        }

        public Task<BuildingAccessScope?> ResolveBuildingScopeAsync(int buildingId, CancellationToken cancellationToken)
        {
            _ = buildingId;
            _ = cancellationToken;
            return Task.FromResult(_scope);
        }
    }

    private sealed class StubFloorScopeResolver : IFloorAccessScopeResolver
    {
        private readonly FloorAccessScope? _scope;

        public StubFloorScopeResolver(FloorAccessScope? scope)
        {
            _scope = scope;
        }

        public Task<FloorAccessScope?> ResolveFloorScopeAsync(int floorId, CancellationToken cancellationToken)
        {
            _ = floorId;
            _ = cancellationToken;
            return Task.FromResult(_scope);
        }
    }

    private sealed class StubRoomScopeResolver : IRoomAccessScopeResolver
    {
        private readonly RoomAccessScope? _scope;

        public StubRoomScopeResolver(RoomAccessScope? scope)
        {
            _scope = scope;
        }

        public Task<RoomAccessScope?> ResolveRoomScopeAsync(int roomId, CancellationToken cancellationToken)
        {
            _ = roomId;
            _ = cancellationToken;
            return Task.FromResult(_scope);
        }
    }

    private sealed class StubWorkflowScopeResolver : IWorkflowAccessScopeResolver
    {
        private readonly WorkflowAccessScope? _workflowScope;
        private readonly WorkflowAccessScope? _scenarioScope;
        private readonly WorkflowAccessScope? _jobScope;

        public StubWorkflowScopeResolver(
            WorkflowAccessScope? workflowScope,
            WorkflowAccessScope? scenarioScope,
            WorkflowAccessScope? jobScope)
        {
            _workflowScope = workflowScope;
            _scenarioScope = scenarioScope;
            _jobScope = jobScope;
        }

        public Task<WorkflowAccessScope?> ResolveWorkflowScopeAsync(string workflowId, CancellationToken cancellationToken)
        {
            _ = workflowId;
            _ = cancellationToken;
            return Task.FromResult(_workflowScope);
        }

        public Task<WorkflowAccessScope?> ResolveScenarioScopeAsync(string scenarioId, CancellationToken cancellationToken)
        {
            _ = scenarioId;
            _ = cancellationToken;
            return Task.FromResult(_scenarioScope);
        }

        public Task<WorkflowAccessScope?> ResolveJobScopeAsync(string jobId, CancellationToken cancellationToken)
        {
            _ = jobId;
            _ = cancellationToken;
            return Task.FromResult(_jobScope);
        }
    }
}
