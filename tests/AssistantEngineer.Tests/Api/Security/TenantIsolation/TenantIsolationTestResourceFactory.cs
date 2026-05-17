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

namespace AssistantEngineer.Tests.Api.Security.TenantIsolation;

internal static class TenantIsolationTestResourceFactory
{
    public static ApiAuthorizationOptions CreateAllProtectionOptions(bool returnNotFoundForTenantMismatch)
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
            ReturnNotFoundForWorkflowTenantMismatch = returnNotFoundForTenantMismatch,
            AllowAnonymousInDevelopment = false
        };
    }

    public static ProtectedEndpointAuthorizationGate CreateGate(
        ApiAuthorizationOptions options,
        AuthenticatedPrincipal principal,
        int? projectOrganizationId = TenantIsolationScenario.TenantAOrganizationId,
        int? buildingOrganizationId = TenantIsolationScenario.TenantAOrganizationId,
        int? workflowOrganizationId = TenantIsolationScenario.TenantAOrganizationId)
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

        return new ProtectedEndpointAuthorizationGate(
            new StaticOptionsMonitor<ApiAuthorizationOptions>(options),
            new StubEnvironment("Testing"),
            new StubPrincipalProvider(principal),
            new StubProjectScopeResolver(projectScope),
            new StubBuildingScopeResolver(buildingScope),
            new StubFloorScopeResolver(CreateFloorScope()),
            new StubRoomScopeResolver(CreateRoomScope()),
            new StubWorkflowScopeResolver(workflowScope),
            new ProjectTenantAccessPolicy(Options.Create(new ProjectTenantAccessOptions
            {
                AllowUnscopedProjectsDuringTransition = false,
                TreatMissingTenantAsBlocking = true,
                EnableStrictTenantMatch = true
            })),
            NullLogger<ProtectedEndpointAuthorizationGate>.Instance);
    }

    public static ProjectAccessScope CreateProjectScope(int organizationId)
    {
        return ProjectTenantAccessScopeFactory.CreateProjectScope(
            projectId: TenantIsolationScenario.ProjectAId,
            organizationId: organizationId,
            ownerUserId: null,
            isTenantScoped: true,
            tenantScope: new TenantScope(organizationId, $"org-{organizationId}", IsActive: true));
    }

    public static BuildingAccessScope CreateBuildingScope(int organizationId)
    {
        return ProjectTenantAccessScopeFactory.CreateBuildingScope(
            buildingId: TenantIsolationScenario.BuildingAId,
            projectId: TenantIsolationScenario.ProjectAId,
            organizationId: organizationId,
            ownerUserId: null,
            isTenantScoped: true,
            tenantScope: new TenantScope(organizationId, $"org-{organizationId}", IsActive: true));
    }

    public static WorkflowAccessScope CreateWorkflowScope(int organizationId)
    {
        return ProjectTenantAccessScopeFactory.CreateWorkflowScope(
            workflowId: TenantIsolationScenario.WorkflowAId,
            projectId: TenantIsolationScenario.ProjectAId,
            buildingId: TenantIsolationScenario.BuildingAId,
            organizationId: organizationId,
            ownerUserId: null,
            isTenantScoped: true,
            tenantScope: new TenantScope(organizationId, $"org-{organizationId}", IsActive: true));
    }

    public static FloorAccessScope CreateFloorScope()
    {
        return new FloorAccessScope(
            FloorId: TenantIsolationScenario.FloorAId,
            BuildingId: TenantIsolationScenario.BuildingAId,
            ProjectId: TenantIsolationScenario.ProjectAId,
            OrganizationId: null,
            OwnerUserId: null,
            IsTenantScoped: false);
    }

    public static RoomAccessScope CreateRoomScope()
    {
        return new RoomAccessScope(
            RoomId: TenantIsolationScenario.RoomAId,
            FloorId: TenantIsolationScenario.FloorAId,
            BuildingId: TenantIsolationScenario.BuildingAId,
            ProjectId: TenantIsolationScenario.ProjectAId,
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
