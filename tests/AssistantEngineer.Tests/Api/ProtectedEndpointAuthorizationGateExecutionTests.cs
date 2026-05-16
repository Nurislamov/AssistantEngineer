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

public sealed class ProtectedEndpointAuthorizationGateExecutionTests
{
    [Fact]
    public async Task ExecutionPilotDisabled_AllowsWorkflowAndCalculation()
    {
        var gate = CreateGate(
            options: new ApiAuthorizationOptions
            {
                Enabled = true,
                EnableExecutionEndpointProtectionPilot = false,
                RequireWorkflowExecuteAuthorization = false,
                RequireCalculationRunAuthorization = false
            },
            principal: AuthenticatedPrincipal.Anonymous,
            projectScope: CreateProjectScope(organizationId: 2001),
            buildingScope: CreateBuildingScope(organizationId: 2001),
            workflowScope: CreateWorkflowScope(organizationId: 2001),
            floorScope: CreateFloorScope(buildingId: 20, projectId: 10),
            roomScope: CreateRoomScope(floorId: 30, buildingId: 20, projectId: 10));

        var workflowDecision = await gate.RequireWorkflowPermissionAsync(
            Permission.WorkflowsExecute,
            workflowId: "wf-1",
            projectId: 10,
            buildingId: 20,
            CancellationToken.None);
        var calculationDecision = await gate.RequireCalculationPermissionAsync(
            Permission.WorkflowsExecute,
            projectId: null,
            buildingId: null,
            floorId: null,
            roomId: 40,
            CancellationToken.None);

        Assert.Equal(ProtectedEndpointAuthorizationOutcome.Allowed, workflowDecision.Outcome);
        Assert.Equal(ProtectedEndpointAuthorizationOutcome.Allowed, calculationDecision.Outcome);
    }

    [Fact]
    public async Task ExecutionPilotEnabled_Unauthenticated_ReturnsUnauthorized()
    {
        var gate = CreateGate(
            options: new ApiAuthorizationOptions
            {
                Enabled = true,
                EnableExecutionEndpointProtectionPilot = true,
                RequireWorkflowExecuteAuthorization = true,
                RequireCalculationRunAuthorization = true,
                AllowAnonymousInDevelopment = false
            },
            principal: AuthenticatedPrincipal.Anonymous,
            projectScope: CreateProjectScope(organizationId: 2001),
            buildingScope: CreateBuildingScope(organizationId: 2001),
            workflowScope: CreateWorkflowScope(organizationId: 2001),
            floorScope: CreateFloorScope(buildingId: 20, projectId: 10),
            roomScope: CreateRoomScope(floorId: 30, buildingId: 20, projectId: 10));

        var workflowDecision = await gate.RequireWorkflowPermissionAsync(
            Permission.WorkflowsExecute,
            workflowId: "wf-2",
            projectId: 10,
            buildingId: 20,
            CancellationToken.None);
        var calculationDecision = await gate.RequireCalculationPermissionAsync(
            Permission.WorkflowsExecute,
            projectId: null,
            buildingId: 20,
            floorId: null,
            roomId: null,
            CancellationToken.None);

        Assert.Equal(ProtectedEndpointAuthorizationOutcome.Unauthorized, workflowDecision.Outcome);
        Assert.Equal(ProtectedEndpointAuthorizationOutcome.Unauthorized, calculationDecision.Outcome);
    }

    [Fact]
    public async Task ExecutionPilotEnabled_MissingPermission_ReturnsForbidden()
    {
        var gate = CreateGate(
            options: new ApiAuthorizationOptions
            {
                Enabled = true,
                EnableExecutionEndpointProtectionPilot = true,
                RequireWorkflowExecuteAuthorization = true,
                RequireCalculationRunAuthorization = true,
                AllowAnonymousInDevelopment = false
            },
            principal: CreatePrincipal(organizationId: 2001, permissions: [Permission.WorkflowsRead.ToString()]),
            projectScope: CreateProjectScope(organizationId: 2001),
            buildingScope: CreateBuildingScope(organizationId: 2001),
            workflowScope: CreateWorkflowScope(organizationId: 2001),
            floorScope: CreateFloorScope(buildingId: 20, projectId: 10),
            roomScope: CreateRoomScope(floorId: 30, buildingId: 20, projectId: 10));

        var workflowDecision = await gate.RequireWorkflowPermissionAsync(
            Permission.WorkflowsExecute,
            workflowId: "wf-3",
            projectId: 10,
            buildingId: 20,
            CancellationToken.None);
        var calculationDecision = await gate.RequireCalculationPermissionAsync(
            Permission.WorkflowsExecute,
            projectId: null,
            buildingId: 20,
            floorId: null,
            roomId: null,
            CancellationToken.None);

        Assert.Equal(ProtectedEndpointAuthorizationOutcome.Forbidden, workflowDecision.Outcome);
        Assert.Equal(ProtectedEndpointAuthorizationOutcome.Forbidden, calculationDecision.Outcome);
    }

    [Fact]
    public async Task WorkflowScopeAndCalculationRoomScope_WithMatchingPermission_Allows()
    {
        var gate = CreateGate(
            options: new ApiAuthorizationOptions
            {
                Enabled = true,
                EnableExecutionEndpointProtectionPilot = true,
                RequireWorkflowExecuteAuthorization = true,
                RequireCalculationRunAuthorization = true,
                AllowAnonymousInDevelopment = false
            },
            principal: CreatePrincipal(organizationId: 2001, permissions: [Permission.WorkflowsExecute.ToString()]),
            projectScope: CreateProjectScope(organizationId: 2001),
            buildingScope: CreateBuildingScope(organizationId: 2001),
            workflowScope: CreateWorkflowScope(organizationId: 2001),
            floorScope: CreateFloorScope(buildingId: 20, projectId: 10),
            roomScope: CreateRoomScope(floorId: 30, buildingId: 20, projectId: 10));

        var workflowDecision = await gate.RequireWorkflowPermissionAsync(
            Permission.WorkflowsExecute,
            workflowId: "wf-4",
            projectId: 10,
            buildingId: 20,
            CancellationToken.None);
        var calculationDecision = await gate.RequireCalculationPermissionAsync(
            Permission.WorkflowsExecute,
            projectId: null,
            buildingId: null,
            floorId: null,
            roomId: 40,
            CancellationToken.None);

        Assert.Equal(ProtectedEndpointAuthorizationOutcome.Allowed, workflowDecision.Outcome);
        Assert.Equal(ProtectedEndpointAuthorizationOutcome.Allowed, calculationDecision.Outcome);
    }

    [Theory]
    [InlineData(false, ProtectedEndpointAuthorizationOutcome.Forbidden)]
    [InlineData(true, ProtectedEndpointAuthorizationOutcome.NotFound)]
    public async Task TenantMismatch_RespectsReturnNotFoundOption(
        bool returnNotFoundForTenantMismatch,
        ProtectedEndpointAuthorizationOutcome expected)
    {
        var gate = CreateGate(
            options: new ApiAuthorizationOptions
            {
                Enabled = true,
                EnableExecutionEndpointProtectionPilot = true,
                RequireWorkflowExecuteAuthorization = true,
                RequireCalculationRunAuthorization = true,
                ReturnNotFoundForTenantMismatch = returnNotFoundForTenantMismatch,
                AllowAnonymousInDevelopment = false
            },
            principal: CreatePrincipal(organizationId: 2001, permissions: [Permission.WorkflowsExecute.ToString()]),
            projectScope: CreateProjectScope(organizationId: 3001),
            buildingScope: CreateBuildingScope(organizationId: 3001),
            workflowScope: CreateWorkflowScope(organizationId: 3001),
            floorScope: CreateFloorScope(buildingId: 20, projectId: 10),
            roomScope: CreateRoomScope(floorId: 30, buildingId: 20, projectId: 10));

        var workflowDecision = await gate.RequireWorkflowPermissionAsync(
            Permission.WorkflowsExecute,
            workflowId: "wf-5",
            projectId: 10,
            buildingId: null,
            CancellationToken.None);
        var calculationDecision = await gate.RequireCalculationPermissionAsync(
            Permission.WorkflowsExecute,
            projectId: null,
            buildingId: 20,
            floorId: null,
            roomId: null,
            CancellationToken.None);

        Assert.Equal(expected, workflowDecision.Outcome);
        Assert.Equal(expected, calculationDecision.Outcome);
    }

    [Fact]
    public async Task WorkflowScopeMissing_FallsBackToBuildingScope()
    {
        var gate = CreateGate(
            options: new ApiAuthorizationOptions
            {
                Enabled = true,
                EnableExecutionEndpointProtectionPilot = true,
                RequireWorkflowExecuteAuthorization = true,
                RequireCalculationRunAuthorization = false,
                AllowAnonymousInDevelopment = false
            },
            principal: CreatePrincipal(organizationId: 2001, permissions: [Permission.WorkflowsExecute.ToString()]),
            projectScope: CreateProjectScope(organizationId: 2001),
            buildingScope: CreateBuildingScope(organizationId: 2001),
            workflowScope: null,
            floorScope: null,
            roomScope: null);

        var decision = await gate.RequireWorkflowPermissionAsync(
            Permission.WorkflowsExecute,
            workflowId: "wf-6",
            projectId: null,
            buildingId: 20,
            CancellationToken.None);

        Assert.Equal(ProtectedEndpointAuthorizationOutcome.Allowed, decision.Outcome);
    }

    private static ProtectedEndpointAuthorizationGate CreateGate(
        ApiAuthorizationOptions options,
        AuthenticatedPrincipal principal,
        ProjectAccessScope? projectScope,
        BuildingAccessScope? buildingScope,
        WorkflowAccessScope? workflowScope,
        FloorAccessScope? floorScope,
        RoomAccessScope? roomScope)
    {
        return new ProtectedEndpointAuthorizationGate(
            new StaticOptionsMonitor<ApiAuthorizationOptions>(options),
            new StubEnvironment("Testing"),
            new StubPrincipalProvider(principal),
            new StubProjectScopeResolver(projectScope),
            new StubBuildingScopeResolver(buildingScope),
            new StubFloorScopeResolver(floorScope),
            new StubRoomScopeResolver(roomScope),
            new StubWorkflowScopeResolver(workflowScope),
            new ProjectTenantAccessPolicy(Options.Create(new ProjectTenantAccessOptions
            {
                AllowUnscopedProjectsDuringTransition = false,
                TreatMissingTenantAsBlocking = true,
                EnableStrictTenantMatch = true
            })),
            NullLogger<ProtectedEndpointAuthorizationGate>.Instance);
    }

    private static AuthenticatedPrincipal CreatePrincipal(
        int organizationId,
        IReadOnlyCollection<string> permissions)
    {
        return new AuthenticatedPrincipal(
            UserId: 101,
            OrganizationId: organizationId,
            ExternalSubjectId: "protected-execution-gate-test-principal",
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

    private static WorkflowAccessScope CreateWorkflowScope(int organizationId)
    {
        return ProjectTenantAccessScopeFactory.CreateWorkflowScope(
            workflowId: "wf-1",
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
        private readonly WorkflowAccessScope? _scope;

        public StubWorkflowScopeResolver(WorkflowAccessScope? scope)
        {
            _scope = scope;
        }

        public Task<WorkflowAccessScope?> ResolveWorkflowScopeAsync(string workflowId, CancellationToken cancellationToken)
        {
            _ = workflowId;
            _ = cancellationToken;
            return Task.FromResult(_scope);
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
