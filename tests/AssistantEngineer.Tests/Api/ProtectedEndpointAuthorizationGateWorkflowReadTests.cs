using AssistantEngineer.Api.Options.Security;
using AssistantEngineer.Api.Security.Authentication;
using AssistantEngineer.Api.Security.Authorization;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Application.Services.Access;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Api;

public sealed class ProtectedEndpointAuthorizationGateWorkflowReadTests
{
    [Fact]
    public async Task WorkflowReadPilotDisabled_Allows()
    {
        var gate = CreateGate(
            options: new ApiAuthorizationOptions
            {
                Enabled = true,
                EnableWorkflowReadEndpointProtectionPilot = false,
                RequireWorkflowReadAuthorization = false
            },
            principal: AuthenticatedPrincipal.Anonymous,
            projectScope: CreateProjectScope(2001),
            buildingScope: CreateBuildingScope(2001),
            workflowScope: CreateWorkflowScope("wf-1", 2001),
            scenarioScope: CreateWorkflowScope("sc-1", 2001),
            jobScope: CreateWorkflowScope("job-1", 2001));

        var decision = await gate.RequireWorkflowReadPermissionAsync(
            workflowId: "wf-1",
            scenarioId: null,
            jobId: null,
            projectId: null,
            buildingId: null,
            CancellationToken.None);

        Assert.Equal(ProtectedEndpointAuthorizationOutcome.Allowed, decision.Outcome);
    }

    [Fact]
    public async Task WorkflowReadPilotEnabled_Unauthenticated_ReturnsUnauthorized()
    {
        var gate = CreateGate(
            options: new ApiAuthorizationOptions
            {
                Enabled = true,
                EnableWorkflowReadEndpointProtectionPilot = true,
                RequireWorkflowReadAuthorization = true,
                AllowAnonymousInDevelopment = false
            },
            principal: AuthenticatedPrincipal.Anonymous,
            projectScope: CreateProjectScope(2001),
            buildingScope: CreateBuildingScope(2001),
            workflowScope: null,
            scenarioScope: null,
            jobScope: null);

        var decision = await gate.RequireWorkflowReadPermissionAsync(
            workflowId: null,
            scenarioId: "sc-1",
            jobId: null,
            projectId: 10,
            buildingId: null,
            CancellationToken.None);

        Assert.Equal(ProtectedEndpointAuthorizationOutcome.Unauthorized, decision.Outcome);
    }

    [Fact]
    public async Task WorkflowReadPilotEnabled_MissingPermission_ReturnsForbidden()
    {
        var gate = CreateGate(
            options: new ApiAuthorizationOptions
            {
                Enabled = true,
                EnableWorkflowReadEndpointProtectionPilot = true,
                RequireWorkflowReadAuthorization = true,
                AllowAnonymousInDevelopment = false
            },
            principal: CreatePrincipal(2001, [Permission.WorkflowsExecute.ToString()]),
            projectScope: CreateProjectScope(2001),
            buildingScope: CreateBuildingScope(2001),
            workflowScope: null,
            scenarioScope: null,
            jobScope: null);

        var decision = await gate.RequireWorkflowReadPermissionAsync(
            workflowId: null,
            scenarioId: null,
            jobId: "job-1",
            projectId: null,
            buildingId: null,
            CancellationToken.None);

        Assert.Equal(ProtectedEndpointAuthorizationOutcome.Forbidden, decision.Outcome);
    }

    [Fact]
    public async Task WorkflowReadPilotEnabled_MatchingPermissionAndScope_Allows()
    {
        var gate = CreateGate(
            options: new ApiAuthorizationOptions
            {
                Enabled = true,
                EnableWorkflowReadEndpointProtectionPilot = true,
                RequireWorkflowReadAuthorization = true,
                AllowAnonymousInDevelopment = false
            },
            principal: CreatePrincipal(2001, [Permission.WorkflowsRead.ToString()]),
            projectScope: CreateProjectScope(2001),
            buildingScope: CreateBuildingScope(2001),
            workflowScope: CreateWorkflowScope("wf-1", 2001),
            scenarioScope: CreateWorkflowScope("sc-1", 2001),
            jobScope: CreateWorkflowScope("job-1", 2001));

        var decision = await gate.RequireWorkflowReadPermissionAsync(
            workflowId: null,
            scenarioId: "sc-1",
            jobId: null,
            projectId: null,
            buildingId: null,
            CancellationToken.None);

        Assert.Equal(ProtectedEndpointAuthorizationOutcome.Allowed, decision.Outcome);
    }

    [Theory]
    [InlineData(false, ProtectedEndpointAuthorizationOutcome.Forbidden)]
    [InlineData(true, ProtectedEndpointAuthorizationOutcome.NotFound)]
    public async Task WorkflowTenantMismatch_RespectsReturnNotFoundOption(
        bool returnNotFoundForWorkflowTenantMismatch,
        ProtectedEndpointAuthorizationOutcome expected)
    {
        var gate = CreateGate(
            options: new ApiAuthorizationOptions
            {
                Enabled = true,
                EnableWorkflowReadEndpointProtectionPilot = true,
                RequireWorkflowReadAuthorization = true,
                ReturnNotFoundForWorkflowTenantMismatch = returnNotFoundForWorkflowTenantMismatch,
                AllowAnonymousInDevelopment = false
            },
            principal: CreatePrincipal(2001, [Permission.WorkflowsRead.ToString()]),
            projectScope: CreateProjectScope(3001),
            buildingScope: CreateBuildingScope(3001),
            workflowScope: CreateWorkflowScope("wf-1", 3001),
            scenarioScope: CreateWorkflowScope("sc-1", 3001),
            jobScope: CreateWorkflowScope("job-1", 3001));

        var decision = await gate.RequireWorkflowReadPermissionAsync(
            workflowId: "wf-1",
            scenarioId: null,
            jobId: null,
            projectId: null,
            buildingId: null,
            CancellationToken.None);

        Assert.Equal(expected, decision.Outcome);
    }

    [Fact]
    public async Task WorkflowScopeWithoutTenantContext_FallsBackToProjectScope()
    {
        var gate = CreateGate(
            options: new ApiAuthorizationOptions
            {
                Enabled = true,
                EnableWorkflowReadEndpointProtectionPilot = true,
                RequireWorkflowReadAuthorization = true,
                AllowAnonymousInDevelopment = false
            },
            principal: CreatePrincipal(2001, [Permission.WorkflowsRead.ToString()]),
            projectScope: CreateProjectScope(2001),
            buildingScope: CreateBuildingScope(2001),
            workflowScope: new WorkflowAccessScope(
                WorkflowId: "wf-1",
                ProjectId: 10,
                BuildingId: 20,
                OrganizationId: null,
                OwnerUserId: null,
                IsTenantScoped: false),
            scenarioScope: null,
            jobScope: null);

        var decision = await gate.RequireWorkflowReadPermissionAsync(
            workflowId: "wf-1",
            scenarioId: null,
            jobId: null,
            projectId: 10,
            buildingId: null,
            CancellationToken.None);

        Assert.Equal(ProtectedEndpointAuthorizationOutcome.Allowed, decision.Outcome);
    }

    private static ProtectedEndpointAuthorizationGate CreateGate(
        ApiAuthorizationOptions options,
        AuthenticatedPrincipal principal,
        ProjectAccessScope? projectScope,
        BuildingAccessScope? buildingScope,
        WorkflowAccessScope? workflowScope,
        WorkflowAccessScope? scenarioScope,
        WorkflowAccessScope? jobScope)
    {
        return new ProtectedEndpointAuthorizationGate(
            new StaticOptionsMonitor<ApiAuthorizationOptions>(options),
            new StubEnvironment("Testing"),
            new StubPrincipalProvider(principal),
            new StubProjectScopeResolver(projectScope),
            new StubBuildingScopeResolver(buildingScope),
            new StubFloorScopeResolver(null),
            new StubRoomScopeResolver(null),
            new StubWorkflowScopeResolver(workflowScope, scenarioScope, jobScope),
            new ProjectTenantAccessPolicy(Options.Create(new ProjectTenantAccessOptions
            {
                AllowUnscopedProjectsDuringTransition = false,
                TreatMissingTenantAsBlocking = true,
                EnableStrictTenantMatch = true
            })),
            NullLogger<ProtectedEndpointAuthorizationGate>.Instance);
    }

    private static AuthenticatedPrincipal CreatePrincipal(int organizationId, IReadOnlyCollection<string> permissions)
    {
        return new AuthenticatedPrincipal(
            UserId: 101,
            OrganizationId: organizationId,
            ExternalSubjectId: "protected-workflow-read-gate-test-principal",
            AuthenticationScheme: "Test",
            Roles: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            Permissions: new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase),
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

    private sealed class StubPrincipalProvider : IAuthenticatedPrincipalProvider
    {
        private readonly AuthenticatedPrincipal _principal;

        public StubPrincipalProvider(AuthenticatedPrincipal principal)
        {
            _principal = principal;
        }

        public AuthenticatedPrincipal GetCurrentPrincipal() => _principal;
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

    private sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T> where T : class
    {
        public StaticOptionsMonitor(T value)
        {
            CurrentValue = value;
        }

        public T CurrentValue { get; }

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private sealed class StubEnvironment : IWebHostEnvironment
    {
        public StubEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string ApplicationName { get; set; } = "AssistantEngineer.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; }
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
