using AssistantEngineer.Api.Options.Security;
using AssistantEngineer.Api.Security.Authentication;
using AssistantEngineer.Api.Security.Authorization;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Application.Services.Access;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Api.Security;

internal static class ProtectedEndpointAuthorizationGateCharacterizationHelper
{
    public const int DefaultOrganizationId = 1001;
    public const int ProjectId = 10;
    public const int BuildingId = 20;
    public const int FloorId = 30;
    public const int RoomId = 40;
    public const string WorkflowId = "workflow-a";
    public const string ScenarioId = "scenario-a";
    public const string JobId = "job-a";
    public const string ArtifactId = "artifact-a";

    public static ApiAuthorizationOptions CreateAllProtectionEnabledOptions(
        bool returnNotFoundForTenantMismatch = false,
        bool returnNotFoundForWorkflowTenantMismatch = false)
    {
        return new ApiAuthorizationOptions
        {
            Enabled = true,
            EnableReadEndpointProtectionPilot = true,
            RequireProjectReadAuthorization = true,
            RequireBuildingReadAuthorization = true,
            EnableWriteEndpointProtectionPilot = true,
            RequireProjectWriteAuthorization = true,
            RequireBuildingWriteAuthorization = true,
            EnableExecutionEndpointProtectionPilot = true,
            RequireWorkflowExecuteAuthorization = true,
            RequireCalculationRunAuthorization = true,
            EnableReportArtifactEndpointProtectionPilot = true,
            RequireReportReadAuthorization = true,
            RequireReportWriteAuthorization = true,
            RequireArtifactReadAuthorization = true,
            RequireArtifactWriteAuthorization = true,
            EnableWorkflowReadEndpointProtectionPilot = true,
            RequireWorkflowReadAuthorization = true,
            ReturnNotFoundForTenantMismatch = returnNotFoundForTenantMismatch,
            ReturnNotFoundForWorkflowTenantMismatch = returnNotFoundForWorkflowTenantMismatch,
            AllowAnonymousInDevelopment = false
        };
    }

    public static ApiAuthorizationOptions CreateCompatibilityDisabledOptions()
    {
        return new ApiAuthorizationOptions
        {
            Enabled = false,
            EnableReadEndpointProtectionPilot = false,
            RequireProjectReadAuthorization = false,
            RequireBuildingReadAuthorization = false,
            EnableWriteEndpointProtectionPilot = false,
            RequireProjectWriteAuthorization = false,
            RequireBuildingWriteAuthorization = false,
            EnableExecutionEndpointProtectionPilot = false,
            RequireWorkflowExecuteAuthorization = false,
            RequireCalculationRunAuthorization = false,
            EnableReportArtifactEndpointProtectionPilot = false,
            RequireReportReadAuthorization = false,
            RequireReportWriteAuthorization = false,
            RequireArtifactReadAuthorization = false,
            RequireArtifactWriteAuthorization = false,
            EnableWorkflowReadEndpointProtectionPilot = false,
            RequireWorkflowReadAuthorization = false
        };
    }

    public static AuthenticatedPrincipal CreatePrincipal(
        int organizationId,
        params Permission[] permissions)
    {
        return new AuthenticatedPrincipal(
            UserId: 101,
            OrganizationId: organizationId,
            ExternalSubjectId: "p8-03a-characterization-principal",
            AuthenticationScheme: "Test",
            Roles: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            Permissions: new HashSet<string>(
                permissions.Select(permission => permission.ToString()),
                StringComparer.OrdinalIgnoreCase),
            IsAuthenticated: true);
    }

    public static ProtectedEndpointAuthorizationGate CreateGate(
        ApiAuthorizationOptions options,
        AuthenticatedPrincipal principal,
        int? projectOrganizationId = DefaultOrganizationId,
        int? buildingOrganizationId = DefaultOrganizationId,
        int? workflowOrganizationId = DefaultOrganizationId,
        bool includeFloorScope = true,
        bool includeRoomScope = true,
        ILogger<ProtectedEndpointAuthorizationGate>? logger = null)
    {
        var projectScope = projectOrganizationId.HasValue
            ? CreateProjectScope(projectOrganizationId.Value)
            : null;
        var buildingScope = buildingOrganizationId.HasValue
            ? CreateBuildingScope(buildingOrganizationId.Value)
            : null;
        var workflowScope = workflowOrganizationId.HasValue
            ? CreateWorkflowScope(workflowOrganizationId.Value)
            : null;
        var floorScope = includeFloorScope ? CreateFloorScope() : null;
        var roomScope = includeRoomScope ? CreateRoomScope() : null;

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
            logger ?? NullLogger<ProtectedEndpointAuthorizationGate>.Instance);
    }

    private static ProjectAccessScope CreateProjectScope(int organizationId)
    {
        return ProjectTenantAccessScopeFactory.CreateProjectScope(
            projectId: ProjectId,
            organizationId: organizationId,
            ownerUserId: null,
            isTenantScoped: true,
            tenantScope: new TenantScope(organizationId, $"org-{organizationId}", IsActive: true));
    }

    private static BuildingAccessScope CreateBuildingScope(int organizationId)
    {
        return ProjectTenantAccessScopeFactory.CreateBuildingScope(
            buildingId: BuildingId,
            projectId: ProjectId,
            organizationId: organizationId,
            ownerUserId: null,
            isTenantScoped: true,
            tenantScope: new TenantScope(organizationId, $"org-{organizationId}", IsActive: true));
    }

    private static WorkflowAccessScope CreateWorkflowScope(int organizationId)
    {
        return ProjectTenantAccessScopeFactory.CreateWorkflowScope(
            workflowId: WorkflowId,
            projectId: ProjectId,
            buildingId: BuildingId,
            organizationId: organizationId,
            ownerUserId: null,
            isTenantScoped: true,
            tenantScope: new TenantScope(organizationId, $"org-{organizationId}", IsActive: true));
    }

    private static FloorAccessScope CreateFloorScope()
    {
        return new FloorAccessScope(
            FloorId: FloorId,
            BuildingId: BuildingId,
            ProjectId: ProjectId,
            OrganizationId: null,
            OwnerUserId: null,
            IsTenantScoped: false);
    }

    private static RoomAccessScope CreateRoomScope()
    {
        return new RoomAccessScope(
            RoomId: RoomId,
            FloorId: FloorId,
            BuildingId: BuildingId,
            ProjectId: ProjectId,
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

        public Task<WorkflowAccessScope?> ResolveScenarioScopeAsync(string scenarioId, CancellationToken cancellationToken)
        {
            _ = scenarioId;
            _ = cancellationToken;
            return Task.FromResult(_scope);
        }

        public Task<WorkflowAccessScope?> ResolveJobScopeAsync(string jobId, CancellationToken cancellationToken)
        {
            _ = jobId;
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
