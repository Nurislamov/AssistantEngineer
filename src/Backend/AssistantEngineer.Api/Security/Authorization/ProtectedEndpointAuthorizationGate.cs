using AssistantEngineer.Api.Options.Security;
using AssistantEngineer.Api.Security.Authentication;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Application.Services.Access;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Api.Security.Authorization;

public sealed class ProtectedEndpointAuthorizationGate : IProtectedEndpointAuthorizationGate
{
    private readonly IOptionsMonitor<ApiAuthorizationOptions> _optionsMonitor;
    private readonly IWebHostEnvironment _environment;
    private readonly IProtectedEndpointAuthorizationDecisionFactory _decisionFactory;
    private readonly IProtectedEndpointPermissionEvaluator _permissionEvaluator;
    private readonly IProtectedEndpointScopeEvaluationService _scopeEvaluationService;
    private readonly IProtectedEndpointTenantMismatchPolicy _tenantMismatchPolicy;
    private readonly IProtectedEndpointAuthorizationLogger _authorizationLogger;

    [ActivatorUtilitiesConstructor]
    public ProtectedEndpointAuthorizationGate(
        IOptionsMonitor<ApiAuthorizationOptions> optionsMonitor,
        IWebHostEnvironment environment,
        IProtectedEndpointAuthorizationDecisionFactory decisionFactory,
        IProtectedEndpointPermissionEvaluator permissionEvaluator,
        IProtectedEndpointScopeEvaluationService scopeEvaluationService,
        IProtectedEndpointTenantMismatchPolicy tenantMismatchPolicy,
        IProtectedEndpointAuthorizationLogger authorizationLogger)
    {
        _optionsMonitor = optionsMonitor;
        _environment = environment;
        _decisionFactory = decisionFactory;
        _permissionEvaluator = permissionEvaluator;
        _scopeEvaluationService = scopeEvaluationService;
        _tenantMismatchPolicy = tenantMismatchPolicy;
        _authorizationLogger = authorizationLogger;
    }

    public ProtectedEndpointAuthorizationGate(
        IOptionsMonitor<ApiAuthorizationOptions> optionsMonitor,
        IWebHostEnvironment environment,
        IAuthenticatedPrincipalProvider principalProvider,
        IProjectReadAccessScopeResolver projectScopeResolver,
        IBuildingReadAccessScopeResolver buildingScopeResolver,
        IFloorAccessScopeResolver floorScopeResolver,
        IRoomAccessScopeResolver roomScopeResolver,
        IWorkflowAccessScopeResolver workflowScopeResolver,
        ProjectTenantAccessPolicy accessPolicy,
        ILogger<ProtectedEndpointAuthorizationGate> logger)
        : this(
            optionsMonitor,
            environment,
            new ProtectedEndpointAuthorizationDecisionFactory(),
            new ProtectedEndpointPermissionEvaluator(principalProvider),
            new ProtectedEndpointScopeEvaluationService(
                projectScopeResolver,
                buildingScopeResolver,
                floorScopeResolver,
                roomScopeResolver,
                workflowScopeResolver,
                accessPolicy),
            new ProtectedEndpointTenantMismatchPolicy(),
            new ProtectedEndpointAuthorizationLogger(logger))
    {
    }

    public Task<ProtectedEndpointAuthorizationDecision> RequirePermissionAsync(
        Permission permission,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var options = _optionsMonitor.CurrentValue;
        if (!IsProtectionRequired(options, permission))
        {
            return Task.FromResult(_decisionFactory.Allowed());
        }

        if (CanBypassDevelopmentAnonymous(options))
        {
            return Task.FromResult(_decisionFactory.Allowed());
        }

        var evaluation = _permissionEvaluator.Evaluate(permission);
        if (!evaluation.IsAuthenticated)
        {
            return Task.FromResult(_decisionFactory.Unauthorized());
        }

        return Task.FromResult(
            evaluation.HasPermission
                ? _decisionFactory.Allowed()
                : _decisionFactory.Forbidden());
    }

    public async Task<ProtectedEndpointAuthorizationDecision> RequireProjectPermissionAsync(
        int projectId,
        Permission permission,
        CancellationToken cancellationToken)
    {
        var options = _optionsMonitor.CurrentValue;
        if (!IsProtectionRequired(options, permission))
        {
            return _decisionFactory.Allowed();
        }

        if (CanBypassDevelopmentAnonymous(options))
        {
            return _decisionFactory.Allowed();
        }

        if (!TryEvaluatePermission(permission, out var principal, out var denialDecision))
        {
            return denialDecision!.Value;
        }

        var scopeResult = await _scopeEvaluationService.EvaluateProjectScopeAsync(
            principal,
            projectId,
            permission,
            cancellationToken);

        return ResolveScopedDecision(scopeResult, options, permission);
    }

    public async Task<ProtectedEndpointAuthorizationDecision> RequireBuildingPermissionAsync(
        int buildingId,
        Permission permission,
        CancellationToken cancellationToken)
    {
        var options = _optionsMonitor.CurrentValue;
        if (!IsProtectionRequired(options, permission))
        {
            return _decisionFactory.Allowed();
        }

        if (CanBypassDevelopmentAnonymous(options))
        {
            return _decisionFactory.Allowed();
        }

        if (!TryEvaluatePermission(permission, out var principal, out var denialDecision))
        {
            return denialDecision!.Value;
        }

        var scopeResult = await _scopeEvaluationService.EvaluateBuildingScopeAsync(
            principal,
            buildingId,
            permission,
            cancellationToken);

        return ResolveScopedDecision(scopeResult, options, permission);
    }

    public async Task<ProtectedEndpointAuthorizationDecision> RequireWorkflowPermissionAsync(
        Permission permission,
        string? workflowId,
        int? projectId,
        int? buildingId,
        CancellationToken cancellationToken)
    {
        var options = _optionsMonitor.CurrentValue;
        if (!IsWorkflowProtectionRequired(options))
        {
            return _decisionFactory.Allowed();
        }

        if (CanBypassDevelopmentAnonymous(options))
        {
            return _decisionFactory.Allowed();
        }

        if (!TryEvaluatePermission(permission, out var principal, out var denialDecision))
        {
            return denialDecision!.Value;
        }

        if (!string.IsNullOrWhiteSpace(workflowId))
        {
            var workflowScopeResult = await _scopeEvaluationService.EvaluateWorkflowScopeAsync(
                principal,
                workflowId,
                permission,
                cancellationToken);

            if (workflowScopeResult.Kind == ProtectedEndpointScopeEvaluationKind.Allowed)
            {
                return _decisionFactory.Allowed();
            }

            if (workflowScopeResult.Kind == ProtectedEndpointScopeEvaluationKind.TenantMismatch)
            {
                return ResolveTenantMismatchDecision(workflowScopeResult, options, permission, workflowId, artifactId: null);
            }
        }

        if (buildingId.HasValue)
        {
            var buildingScopeResult = await _scopeEvaluationService.EvaluateBuildingScopeAsync(
                principal,
                buildingId.Value,
                permission,
                cancellationToken);

            return ResolveScopedDecision(buildingScopeResult, options, permission);
        }

        if (projectId.HasValue)
        {
            var projectScopeResult = await _scopeEvaluationService.EvaluateProjectScopeAsync(
                principal,
                projectId.Value,
                permission,
                cancellationToken);

            return ResolveScopedDecision(projectScopeResult, options, permission);
        }

        return _decisionFactory.Allowed();
    }

    public async Task<ProtectedEndpointAuthorizationDecision> RequireCalculationPermissionAsync(
        Permission permission,
        int? projectId,
        int? buildingId,
        int? floorId,
        int? roomId,
        CancellationToken cancellationToken)
    {
        var options = _optionsMonitor.CurrentValue;
        if (!IsCalculationProtectionRequired(options))
        {
            return _decisionFactory.Allowed();
        }

        if (CanBypassDevelopmentAnonymous(options))
        {
            return _decisionFactory.Allowed();
        }

        if (!TryEvaluatePermission(permission, out var principal, out var denialDecision))
        {
            return denialDecision!.Value;
        }

        if (roomId.HasValue)
        {
            var roomScopeResult = await _scopeEvaluationService.EvaluateRoomScopeAsync(
                principal,
                roomId.Value,
                permission,
                cancellationToken);

            return ResolveScopedDecision(roomScopeResult, options, permission);
        }

        if (floorId.HasValue)
        {
            var floorScopeResult = await _scopeEvaluationService.EvaluateFloorScopeAsync(
                principal,
                floorId.Value,
                permission,
                cancellationToken);

            return ResolveScopedDecision(floorScopeResult, options, permission);
        }

        if (buildingId.HasValue)
        {
            var buildingScopeResult = await _scopeEvaluationService.EvaluateBuildingScopeAsync(
                principal,
                buildingId.Value,
                permission,
                cancellationToken);

            return ResolveScopedDecision(buildingScopeResult, options, permission);
        }

        if (projectId.HasValue)
        {
            var projectScopeResult = await _scopeEvaluationService.EvaluateProjectScopeAsync(
                principal,
                projectId.Value,
                permission,
                cancellationToken);

            return ResolveScopedDecision(projectScopeResult, options, permission);
        }

        return _decisionFactory.Allowed();
    }

    public Task<ProtectedEndpointAuthorizationDecision> RequireReportReadPermissionAsync(
        int? projectId,
        int? buildingId,
        string? workflowId,
        CancellationToken cancellationToken)
    {
        return RequireReportArtifactPermissionAsync(
            Permission.ReportsRead,
            IsReportReadProtectionRequired,
            projectId,
            buildingId,
            workflowId,
            artifactId: null,
            cancellationToken);
    }

    public Task<ProtectedEndpointAuthorizationDecision> RequireReportWritePermissionAsync(
        int? projectId,
        int? buildingId,
        string? workflowId,
        CancellationToken cancellationToken)
    {
        return RequireReportArtifactPermissionAsync(
            Permission.ReportsWrite,
            IsReportWriteProtectionRequired,
            projectId,
            buildingId,
            workflowId,
            artifactId: null,
            cancellationToken);
    }

    public Task<ProtectedEndpointAuthorizationDecision> RequireArtifactReadPermissionAsync(
        int? projectId,
        int? buildingId,
        string? workflowId,
        string? artifactId,
        CancellationToken cancellationToken)
    {
        return RequireReportArtifactPermissionAsync(
            Permission.ReportsRead,
            IsArtifactReadProtectionRequired,
            projectId,
            buildingId,
            workflowId,
            artifactId,
            cancellationToken);
    }

    public Task<ProtectedEndpointAuthorizationDecision> RequireArtifactWritePermissionAsync(
        int? projectId,
        int? buildingId,
        string? workflowId,
        string? artifactId,
        CancellationToken cancellationToken)
    {
        return RequireReportArtifactPermissionAsync(
            Permission.ReportsWrite,
            IsArtifactWriteProtectionRequired,
            projectId,
            buildingId,
            workflowId,
            artifactId,
            cancellationToken);
    }

    public async Task<ProtectedEndpointAuthorizationDecision> RequireWorkflowReadPermissionAsync(
        string? workflowId,
        string? scenarioId,
        string? jobId,
        int? projectId,
        int? buildingId,
        CancellationToken cancellationToken)
    {
        var options = _optionsMonitor.CurrentValue;
        if (!IsWorkflowReadProtectionRequired(options))
        {
            return _decisionFactory.Allowed();
        }

        if (CanBypassDevelopmentAnonymous(options))
        {
            return _decisionFactory.Allowed();
        }

        if (!TryEvaluatePermission(Permission.WorkflowsRead, out var principal, out var denialDecision))
        {
            return denialDecision!.Value;
        }

        var workflowDecision = await EvaluateWorkflowScopeByAnyIdentifierAsync(
            principal,
            Permission.WorkflowsRead,
            options,
            workflowId,
            scenarioId,
            jobId,
            cancellationToken);
        if (workflowDecision.HasValue)
        {
            return workflowDecision.Value;
        }

        if (buildingId.HasValue)
        {
            var buildingScopeResult = await _scopeEvaluationService.EvaluateBuildingScopeAsync(
                principal,
                buildingId.Value,
                Permission.WorkflowsRead,
                cancellationToken);

            return ResolveScopedDecision(buildingScopeResult, options, Permission.WorkflowsRead);
        }

        if (projectId.HasValue)
        {
            var projectScopeResult = await _scopeEvaluationService.EvaluateProjectScopeAsync(
                principal,
                projectId.Value,
                Permission.WorkflowsRead,
                cancellationToken);

            return ResolveScopedDecision(projectScopeResult, options, Permission.WorkflowsRead);
        }

        return _decisionFactory.Allowed();
    }

    private bool CanBypassDevelopmentAnonymous(ApiAuthorizationOptions options)
    {
        return _environment.IsDevelopment() && options.AllowAnonymousInDevelopment;
    }

    private static bool IsProtectionRequired(ApiAuthorizationOptions options, Permission permission)
    {
        if (!options.Enabled)
        {
            return false;
        }

        if (IsReadPermission(permission))
        {
            if (!options.EnableReadEndpointProtectionPilot)
            {
                return false;
            }

            return permission switch
            {
                Permission.ProjectsRead => options.RequireProjectReadAuthorization,
                Permission.BuildingsRead => options.RequireBuildingReadAuthorization,
                _ => true
            };
        }

        if (IsWritePermission(permission))
        {
            if (!options.EnableWriteEndpointProtectionPilot)
            {
                return false;
            }

            return permission switch
            {
                Permission.ProjectsWrite => options.RequireProjectWriteAuthorization,
                Permission.BuildingsWrite => options.RequireBuildingWriteAuthorization,
                _ => true
            };
        }

        return true;
    }

    private static bool IsReadPermission(Permission permission)
    {
        return permission switch
        {
            Permission.ProjectsRead => true,
            Permission.BuildingsRead => true,
            Permission.WorkflowsRead => true,
            Permission.ReportsRead => true,
            _ => false
        };
    }

    private static bool IsWritePermission(Permission permission)
    {
        return permission switch
        {
            Permission.ProjectsWrite => true,
            Permission.BuildingsWrite => true,
            Permission.ReportsWrite => true,
            Permission.AdministrationManage => true,
            _ => false
        };
    }

    private static bool IsWorkflowProtectionRequired(ApiAuthorizationOptions options)
    {
        return options.Enabled &&
               options.EnableExecutionEndpointProtectionPilot &&
               options.RequireWorkflowExecuteAuthorization;
    }

    private static bool IsCalculationProtectionRequired(ApiAuthorizationOptions options)
    {
        return options.Enabled &&
               options.EnableExecutionEndpointProtectionPilot &&
               options.RequireCalculationRunAuthorization;
    }

    private static bool IsWorkflowReadProtectionRequired(ApiAuthorizationOptions options)
    {
        return options.Enabled &&
               options.EnableWorkflowReadEndpointProtectionPilot &&
               options.RequireWorkflowReadAuthorization;
    }

    private static bool IsReportReadProtectionRequired(ApiAuthorizationOptions options)
    {
        return options.Enabled &&
               options.EnableReportArtifactEndpointProtectionPilot &&
               options.RequireReportReadAuthorization;
    }

    private static bool IsReportWriteProtectionRequired(ApiAuthorizationOptions options)
    {
        return options.Enabled &&
               options.EnableReportArtifactEndpointProtectionPilot &&
               options.RequireReportWriteAuthorization;
    }

    private static bool IsArtifactReadProtectionRequired(ApiAuthorizationOptions options)
    {
        return options.Enabled &&
               options.EnableReportArtifactEndpointProtectionPilot &&
               options.RequireArtifactReadAuthorization;
    }

    private static bool IsArtifactWriteProtectionRequired(ApiAuthorizationOptions options)
    {
        return options.Enabled &&
               options.EnableReportArtifactEndpointProtectionPilot &&
               options.RequireArtifactWriteAuthorization;
    }

    private async Task<ProtectedEndpointAuthorizationDecision> RequireReportArtifactPermissionAsync(
        Permission permission,
        Func<ApiAuthorizationOptions, bool> isProtectionRequired,
        int? projectId,
        int? buildingId,
        string? workflowId,
        string? artifactId,
        CancellationToken cancellationToken)
    {
        var options = _optionsMonitor.CurrentValue;
        if (!isProtectionRequired(options))
        {
            return _decisionFactory.Allowed();
        }

        if (CanBypassDevelopmentAnonymous(options))
        {
            return _decisionFactory.Allowed();
        }

        if (!TryEvaluatePermission(permission, out var principal, out var denialDecision))
        {
            return denialDecision!.Value;
        }

        if (!string.IsNullOrWhiteSpace(workflowId))
        {
            var workflowScopeResult = await _scopeEvaluationService.EvaluateWorkflowScopeAsync(
                principal,
                workflowId,
                permission,
                cancellationToken);

            if (workflowScopeResult.Kind == ProtectedEndpointScopeEvaluationKind.Allowed)
            {
                return _decisionFactory.Allowed();
            }

            if (workflowScopeResult.Kind == ProtectedEndpointScopeEvaluationKind.TenantMismatch)
            {
                return ResolveTenantMismatchDecision(workflowScopeResult, options, permission, workflowId, artifactId);
            }
        }

        if (buildingId.HasValue)
        {
            var buildingScopeResult = await _scopeEvaluationService.EvaluateBuildingScopeAsync(
                principal,
                buildingId.Value,
                permission,
                cancellationToken);

            return ResolveScopedDecision(buildingScopeResult, options, permission);
        }

        if (projectId.HasValue)
        {
            var projectScopeResult = await _scopeEvaluationService.EvaluateProjectScopeAsync(
                principal,
                projectId.Value,
                permission,
                cancellationToken);

            return ResolveScopedDecision(projectScopeResult, options, permission);
        }

        return _decisionFactory.Allowed();
    }

    private async Task<ProtectedEndpointAuthorizationDecision?> EvaluateWorkflowScopeByAnyIdentifierAsync(
        AuthenticatedPrincipal principal,
        Permission permission,
        ApiAuthorizationOptions options,
        string? workflowId,
        string? scenarioId,
        string? jobId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(workflowId))
        {
            var workflowScopeResult = await _scopeEvaluationService.EvaluateWorkflowScopeAsync(
                principal,
                workflowId,
                permission,
                cancellationToken);

            if (workflowScopeResult.Kind == ProtectedEndpointScopeEvaluationKind.Allowed)
            {
                return _decisionFactory.Allowed();
            }

            if (workflowScopeResult.Kind == ProtectedEndpointScopeEvaluationKind.TenantMismatch)
            {
                return ResolveTenantMismatchDecision(workflowScopeResult, options, permission, workflowId, artifactId: null);
            }
        }

        if (!string.IsNullOrWhiteSpace(scenarioId))
        {
            var scenarioScopeResult = await _scopeEvaluationService.EvaluateScenarioScopeAsync(
                principal,
                scenarioId,
                permission,
                cancellationToken);

            if (scenarioScopeResult.Kind == ProtectedEndpointScopeEvaluationKind.Allowed)
            {
                return _decisionFactory.Allowed();
            }

            if (scenarioScopeResult.Kind == ProtectedEndpointScopeEvaluationKind.TenantMismatch)
            {
                return ResolveTenantMismatchDecision(scenarioScopeResult, options, permission, scenarioId, artifactId: null);
            }
        }

        if (!string.IsNullOrWhiteSpace(jobId))
        {
            var jobScopeResult = await _scopeEvaluationService.EvaluateJobScopeAsync(
                principal,
                jobId,
                permission,
                cancellationToken);

            if (jobScopeResult.Kind == ProtectedEndpointScopeEvaluationKind.Allowed)
            {
                return _decisionFactory.Allowed();
            }

            if (jobScopeResult.Kind == ProtectedEndpointScopeEvaluationKind.TenantMismatch)
            {
                return ResolveTenantMismatchDecision(jobScopeResult, options, permission, jobId, artifactId: null);
            }
        }

        return null;
    }

    private bool TryEvaluatePermission(
        Permission permission,
        out AuthenticatedPrincipal principal,
        out ProtectedEndpointAuthorizationDecision? denialDecision)
    {
        var evaluation = _permissionEvaluator.Evaluate(permission);
        principal = evaluation.Principal;

        if (!evaluation.IsAuthenticated)
        {
            denialDecision = _decisionFactory.Unauthorized();
            return false;
        }

        if (!evaluation.HasPermission)
        {
            denialDecision = _decisionFactory.Forbidden();
            return false;
        }

        denialDecision = null;
        return true;
    }

    private ProtectedEndpointAuthorizationDecision ResolveScopedDecision(
        ProtectedEndpointScopeEvaluationResult scopeResult,
        ApiAuthorizationOptions options,
        Permission permission)
    {
        return scopeResult.Kind switch
        {
            ProtectedEndpointScopeEvaluationKind.Allowed => _decisionFactory.Allowed(),
            ProtectedEndpointScopeEvaluationKind.ScopeMissing => _decisionFactory.NotFound(),
            ProtectedEndpointScopeEvaluationKind.TenantMismatch => ResolveTenantMismatchDecision(
                scopeResult,
                options,
                permission,
                workflowId: null,
                artifactId: null),
            _ => _decisionFactory.Allowed()
        };
    }

    private ProtectedEndpointAuthorizationDecision ResolveTenantMismatchDecision(
        ProtectedEndpointScopeEvaluationResult scopeResult,
        ApiAuthorizationOptions options,
        Permission permission,
        string? workflowId,
        string? artifactId)
    {
        var shouldReturnNotFound = _tenantMismatchPolicy.ShouldReturnNotFound(options, scopeResult.ScopeKind);
        LogTenantMismatch(scopeResult, permission, shouldReturnNotFound, workflowId, artifactId);

        return shouldReturnNotFound
            ? _decisionFactory.NotFound()
            : _decisionFactory.Forbidden();
    }

    private void LogTenantMismatch(
        ProtectedEndpointScopeEvaluationResult scopeResult,
        Permission permission,
        bool returnNotFound,
        string? workflowId,
        string? artifactId)
    {
        switch (scopeResult.ScopeKind)
        {
            case ProtectedEndpointScopeKind.Project when scopeResult.ProjectId.HasValue:
                _authorizationLogger.LogProjectDenied(scopeResult.ProjectId.Value, permission, returnNotFound);
                break;
            case ProtectedEndpointScopeKind.Building when scopeResult.BuildingId.HasValue:
                _authorizationLogger.LogBuildingDenied(scopeResult.BuildingId.Value, permission, returnNotFound);
                break;
            case ProtectedEndpointScopeKind.Workflow:
            case ProtectedEndpointScopeKind.WorkflowScenario:
            case ProtectedEndpointScopeKind.WorkflowJob:
                var resolvedWorkflowId = scopeResult.ScopeIdentifier ?? workflowId ?? string.Empty;
                _authorizationLogger.LogWorkflowDenied(resolvedWorkflowId, artifactId, permission, returnNotFound);
                break;
        }
    }
}
