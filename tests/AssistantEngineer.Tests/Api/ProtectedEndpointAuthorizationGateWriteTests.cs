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

public sealed class ProtectedEndpointAuthorizationGateWriteTests
{
    [Fact]
    public async Task WritePilotDisabled_AllowsWriteAuthorizationFlow()
    {
        var gate = CreateGate(
            options: new ApiAuthorizationOptions
            {
                Enabled = true,
                EnableWriteEndpointProtectionPilot = false,
                RequireProjectWriteAuthorization = false,
                RequireBuildingWriteAuthorization = false
            },
            principal: AuthenticatedPrincipal.Anonymous,
            projectScope: CreateProjectScope(organizationId: 2001),
            buildingScope: CreateBuildingScope(organizationId: 2001));

        var createProjectDecision = await gate.RequirePermissionAsync(Permission.ProjectsWrite, CancellationToken.None);
        var updateProjectDecision = await gate.RequireProjectPermissionAsync(10, Permission.ProjectsWrite, CancellationToken.None);
        var updateBuildingDecision = await gate.RequireBuildingPermissionAsync(20, Permission.BuildingsWrite, CancellationToken.None);

        Assert.Equal(ProtectedEndpointAuthorizationOutcome.Allowed, createProjectDecision.Outcome);
        Assert.Equal(ProtectedEndpointAuthorizationOutcome.Allowed, updateProjectDecision.Outcome);
        Assert.Equal(ProtectedEndpointAuthorizationOutcome.Allowed, updateBuildingDecision.Outcome);
    }

    [Fact]
    public async Task WritePilotEnabled_UnauthenticatedPrincipal_ReturnsUnauthorized()
    {
        var gate = CreateGate(
            options: new ApiAuthorizationOptions
            {
                Enabled = true,
                EnableWriteEndpointProtectionPilot = true,
                RequireProjectWriteAuthorization = true,
                RequireBuildingWriteAuthorization = true,
                AllowAnonymousInDevelopment = false
            },
            principal: AuthenticatedPrincipal.Anonymous,
            projectScope: CreateProjectScope(organizationId: 2001),
            buildingScope: CreateBuildingScope(organizationId: 2001));

        var createProjectDecision = await gate.RequirePermissionAsync(Permission.ProjectsWrite, CancellationToken.None);

        Assert.Equal(ProtectedEndpointAuthorizationOutcome.Unauthorized, createProjectDecision.Outcome);
    }

    [Fact]
    public async Task WritePilotEnabled_MissingWritePermission_ReturnsForbidden()
    {
        var gate = CreateGate(
            options: new ApiAuthorizationOptions
            {
                Enabled = true,
                EnableWriteEndpointProtectionPilot = true,
                RequireProjectWriteAuthorization = true,
                RequireBuildingWriteAuthorization = true,
                AllowAnonymousInDevelopment = false
            },
            principal: CreatePrincipal(
                organizationId: 2001,
                permissions: new[]
                {
                    Permission.ProjectsRead.ToString(),
                    Permission.BuildingsRead.ToString()
                }),
            projectScope: CreateProjectScope(organizationId: 2001),
            buildingScope: CreateBuildingScope(organizationId: 2001));

        var projectDecision = await gate.RequireProjectPermissionAsync(10, Permission.ProjectsWrite, CancellationToken.None);
        var buildingDecision = await gate.RequireBuildingPermissionAsync(20, Permission.BuildingsWrite, CancellationToken.None);

        Assert.Equal(ProtectedEndpointAuthorizationOutcome.Forbidden, projectDecision.Outcome);
        Assert.Equal(ProtectedEndpointAuthorizationOutcome.Forbidden, buildingDecision.Outcome);
    }

    [Fact]
    public async Task CreateProject_UsesPrincipalPermissionWithoutResourceScope()
    {
        var gate = CreateGate(
            options: new ApiAuthorizationOptions
            {
                Enabled = true,
                EnableWriteEndpointProtectionPilot = true,
                RequireProjectWriteAuthorization = true,
                RequireBuildingWriteAuthorization = true,
                AllowAnonymousInDevelopment = false
            },
            principal: CreatePrincipal(
                organizationId: 2001,
                permissions: new[]
                {
                    Permission.ProjectsWrite.ToString()
                }),
            projectScope: CreateProjectScope(organizationId: 9999),
            buildingScope: CreateBuildingScope(organizationId: 9999));

        var decision = await gate.RequirePermissionAsync(Permission.ProjectsWrite, CancellationToken.None);

        Assert.Equal(ProtectedEndpointAuthorizationOutcome.Allowed, decision.Outcome);
    }

    [Fact]
    public async Task CreateBuilding_UsesParentProjectScope()
    {
        var gate = CreateGate(
            options: new ApiAuthorizationOptions
            {
                Enabled = true,
                EnableWriteEndpointProtectionPilot = true,
                RequireProjectWriteAuthorization = true,
                RequireBuildingWriteAuthorization = true,
                AllowAnonymousInDevelopment = false
            },
            principal: CreatePrincipal(
                organizationId: 2001,
                permissions: new[]
                {
                    Permission.BuildingsWrite.ToString()
                }),
            projectScope: CreateProjectScope(organizationId: 2001),
            buildingScope: CreateBuildingScope(organizationId: 2001));

        var decision = await gate.RequireProjectPermissionAsync(10, Permission.BuildingsWrite, CancellationToken.None);

        Assert.Equal(ProtectedEndpointAuthorizationOutcome.Allowed, decision.Outcome);
    }

    [Theory]
    [InlineData(false, ProtectedEndpointAuthorizationOutcome.Forbidden)]
    [InlineData(true, ProtectedEndpointAuthorizationOutcome.NotFound)]
    public async Task TenantMismatch_RespectsReturnNotFoundForTenantMismatch(
        bool returnNotFoundForTenantMismatch,
        ProtectedEndpointAuthorizationOutcome expected)
    {
        var gate = CreateGate(
            options: new ApiAuthorizationOptions
            {
                Enabled = true,
                EnableWriteEndpointProtectionPilot = true,
                RequireProjectWriteAuthorization = true,
                RequireBuildingWriteAuthorization = true,
                ReturnNotFoundForTenantMismatch = returnNotFoundForTenantMismatch,
                AllowAnonymousInDevelopment = false
            },
            principal: CreatePrincipal(
                organizationId: 2001,
                permissions: new[]
                {
                    Permission.ProjectsWrite.ToString(),
                    Permission.BuildingsWrite.ToString()
                }),
            projectScope: CreateProjectScope(organizationId: 3001),
            buildingScope: CreateBuildingScope(organizationId: 3001));

        var projectDecision = await gate.RequireProjectPermissionAsync(10, Permission.ProjectsWrite, CancellationToken.None);
        var buildingDecision = await gate.RequireBuildingPermissionAsync(20, Permission.BuildingsWrite, CancellationToken.None);

        Assert.Equal(expected, projectDecision.Outcome);
        Assert.Equal(expected, buildingDecision.Outcome);
    }

    private static ProtectedEndpointAuthorizationGate CreateGate(
        ApiAuthorizationOptions options,
        AuthenticatedPrincipal principal,
        ProjectAccessScope? projectScope,
        BuildingAccessScope? buildingScope)
    {
        return new ProtectedEndpointAuthorizationGate(
            new StaticOptionsMonitor<ApiAuthorizationOptions>(options),
            new StubEnvironment("Testing"),
            new StubPrincipalProvider(principal),
            new StubProjectScopeResolver(projectScope),
            new StubBuildingScopeResolver(buildingScope),
            new StubFloorScopeResolver(null),
            new StubRoomScopeResolver(null),
            new StubWorkflowScopeResolver(null),
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
            ExternalSubjectId: "protected-write-gate-test-principal",
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

        public Task<ProjectAccessScope?> ResolveProjectScopeAsync(
            int projectId,
            CancellationToken cancellationToken)
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

        public Task<BuildingAccessScope?> ResolveBuildingScopeAsync(
            int buildingId,
            CancellationToken cancellationToken)
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

        public Task<FloorAccessScope?> ResolveFloorScopeAsync(
            int floorId,
            CancellationToken cancellationToken)
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

        public Task<RoomAccessScope?> ResolveRoomScopeAsync(
            int roomId,
            CancellationToken cancellationToken)
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

        public Task<WorkflowAccessScope?> ResolveWorkflowScopeAsync(
            string workflowId,
            CancellationToken cancellationToken)
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
