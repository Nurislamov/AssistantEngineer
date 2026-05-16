using AssistantEngineer.Api.Options.Security;
using AssistantEngineer.Api.Security.Authentication;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Application.Services.Access;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Api.Security.Authorization;

public sealed class ProtectedEndpointAuthorizationGate : IProtectedEndpointAuthorizationGate
{
    private readonly IOptionsMonitor<ApiAuthorizationOptions> _optionsMonitor;
    private readonly IWebHostEnvironment _environment;
    private readonly IAuthenticatedPrincipalProvider _principalProvider;
    private readonly IProjectReadAccessScopeResolver _projectScopeResolver;
    private readonly IBuildingReadAccessScopeResolver _buildingScopeResolver;
    private readonly IFloorAccessScopeResolver _floorScopeResolver;
    private readonly IRoomAccessScopeResolver _roomScopeResolver;
    private readonly IWorkflowAccessScopeResolver _workflowScopeResolver;
    private readonly ProjectTenantAccessPolicy _accessPolicy;
    private readonly ILogger<ProtectedEndpointAuthorizationGate> _logger;

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
    {
        _optionsMonitor = optionsMonitor;
        _environment = environment;
        _principalProvider = principalProvider;
        _projectScopeResolver = projectScopeResolver;
        _buildingScopeResolver = buildingScopeResolver;
        _floorScopeResolver = floorScopeResolver;
        _roomScopeResolver = roomScopeResolver;
        _workflowScopeResolver = workflowScopeResolver;
        _accessPolicy = accessPolicy;
        _logger = logger;
    }

    public Task<ProtectedEndpointAuthorizationDecision> RequirePermissionAsync(
        Permission permission,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var options = _optionsMonitor.CurrentValue;
        if (!IsProtectionRequired(options, permission))
        {
            return Task.FromResult(ProtectedEndpointAuthorizationDecision.Allowed);
        }

        if (CanBypassDevelopmentAnonymous(options))
        {
            return Task.FromResult(ProtectedEndpointAuthorizationDecision.Allowed);
        }

        var principal = _principalProvider.GetCurrentPrincipal();
        if (!principal.IsAuthenticated)
        {
            return Task.FromResult(ProtectedEndpointAuthorizationDecision.Unauthorized);
        }

        return Task.FromResult(
            HasPermission(principal, permission)
                ? ProtectedEndpointAuthorizationDecision.Allowed
                : ProtectedEndpointAuthorizationDecision.Forbidden);
    }

    public async Task<ProtectedEndpointAuthorizationDecision> RequireProjectPermissionAsync(
        int projectId,
        Permission permission,
        CancellationToken cancellationToken)
    {
        var options = _optionsMonitor.CurrentValue;
        if (!IsProtectionRequired(options, permission))
        {
            return ProtectedEndpointAuthorizationDecision.Allowed;
        }

        if (CanBypassDevelopmentAnonymous(options))
        {
            return ProtectedEndpointAuthorizationDecision.Allowed;
        }

        var principal = _principalProvider.GetCurrentPrincipal();
        if (!principal.IsAuthenticated)
        {
            return ProtectedEndpointAuthorizationDecision.Unauthorized;
        }

        if (!HasPermission(principal, permission))
        {
            return ProtectedEndpointAuthorizationDecision.Forbidden;
        }

        return await AuthorizeProjectScopeAsync(principal, projectId, permission, options, cancellationToken);
    }

    public async Task<ProtectedEndpointAuthorizationDecision> RequireBuildingPermissionAsync(
        int buildingId,
        Permission permission,
        CancellationToken cancellationToken)
    {
        var options = _optionsMonitor.CurrentValue;
        if (!IsProtectionRequired(options, permission))
        {
            return ProtectedEndpointAuthorizationDecision.Allowed;
        }

        if (CanBypassDevelopmentAnonymous(options))
        {
            return ProtectedEndpointAuthorizationDecision.Allowed;
        }

        var principal = _principalProvider.GetCurrentPrincipal();
        if (!principal.IsAuthenticated)
        {
            return ProtectedEndpointAuthorizationDecision.Unauthorized;
        }

        if (!HasPermission(principal, permission))
        {
            return ProtectedEndpointAuthorizationDecision.Forbidden;
        }

        return await AuthorizeBuildingScopeAsync(principal, buildingId, permission, options, cancellationToken);
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
            return ProtectedEndpointAuthorizationDecision.Allowed;
        }

        if (CanBypassDevelopmentAnonymous(options))
        {
            return ProtectedEndpointAuthorizationDecision.Allowed;
        }

        var principal = _principalProvider.GetCurrentPrincipal();
        if (!principal.IsAuthenticated)
        {
            return ProtectedEndpointAuthorizationDecision.Unauthorized;
        }

        if (!HasPermission(principal, permission))
        {
            return ProtectedEndpointAuthorizationDecision.Forbidden;
        }

        if (!string.IsNullOrWhiteSpace(workflowId))
        {
            var workflowScope = await _workflowScopeResolver.ResolveWorkflowScopeAsync(workflowId, cancellationToken);
            if (workflowScope is not null)
            {
                var principalContext = AuthenticatedPrincipalMapper.ToPrincipalAccessContext(principal);
                if (_accessPolicy.CanAccessWorkflow(principalContext, workflowScope, permission))
                {
                    return ProtectedEndpointAuthorizationDecision.Allowed;
                }

                _logger.LogInformation(
                    "Workflow authorization denied for principal. WorkflowId={WorkflowId}, Permission={Permission}, ReturnNotFound={ReturnNotFound}.",
                    workflowId,
                    permission,
                    options.ReturnNotFoundForTenantMismatch);

                return options.ReturnNotFoundForTenantMismatch
                    ? ProtectedEndpointAuthorizationDecision.NotFound
                    : ProtectedEndpointAuthorizationDecision.Forbidden;
            }
        }

        if (buildingId.HasValue)
        {
            return await AuthorizeBuildingScopeAsync(principal, buildingId.Value, permission, options, cancellationToken);
        }

        if (projectId.HasValue)
        {
            return await AuthorizeProjectScopeAsync(principal, projectId.Value, permission, options, cancellationToken);
        }

        return ProtectedEndpointAuthorizationDecision.Allowed;
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
            return ProtectedEndpointAuthorizationDecision.Allowed;
        }

        if (CanBypassDevelopmentAnonymous(options))
        {
            return ProtectedEndpointAuthorizationDecision.Allowed;
        }

        var principal = _principalProvider.GetCurrentPrincipal();
        if (!principal.IsAuthenticated)
        {
            return ProtectedEndpointAuthorizationDecision.Unauthorized;
        }

        if (!HasPermission(principal, permission))
        {
            return ProtectedEndpointAuthorizationDecision.Forbidden;
        }

        if (roomId.HasValue)
        {
            var roomScope = await _roomScopeResolver.ResolveRoomScopeAsync(roomId.Value, cancellationToken);
            if (roomScope is null)
            {
                return ProtectedEndpointAuthorizationDecision.NotFound;
            }

            return await AuthorizeBuildingScopeAsync(principal, roomScope.BuildingId, permission, options, cancellationToken);
        }

        if (floorId.HasValue)
        {
            var floorScope = await _floorScopeResolver.ResolveFloorScopeAsync(floorId.Value, cancellationToken);
            if (floorScope is null)
            {
                return ProtectedEndpointAuthorizationDecision.NotFound;
            }

            return await AuthorizeBuildingScopeAsync(principal, floorScope.BuildingId, permission, options, cancellationToken);
        }

        if (buildingId.HasValue)
        {
            return await AuthorizeBuildingScopeAsync(principal, buildingId.Value, permission, options, cancellationToken);
        }

        if (projectId.HasValue)
        {
            return await AuthorizeProjectScopeAsync(principal, projectId.Value, permission, options, cancellationToken);
        }

        return ProtectedEndpointAuthorizationDecision.Allowed;
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

    private bool CanBypassDevelopmentAnonymous(ApiAuthorizationOptions options)
    {
        return _environment.IsDevelopment() && options.AllowAnonymousInDevelopment;
    }

    private static bool HasPermission(AuthenticatedPrincipal principal, Permission permission)
    {
        var requiredPermission = permission.ToString();
        return principal.Permissions.Any(candidate =>
            string.Equals(candidate, requiredPermission, StringComparison.OrdinalIgnoreCase));
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
            return ProtectedEndpointAuthorizationDecision.Allowed;
        }

        if (CanBypassDevelopmentAnonymous(options))
        {
            return ProtectedEndpointAuthorizationDecision.Allowed;
        }

        var principal = _principalProvider.GetCurrentPrincipal();
        if (!principal.IsAuthenticated)
        {
            return ProtectedEndpointAuthorizationDecision.Unauthorized;
        }

        if (!HasPermission(principal, permission))
        {
            return ProtectedEndpointAuthorizationDecision.Forbidden;
        }

        if (!string.IsNullOrWhiteSpace(workflowId))
        {
            var workflowScope = await _workflowScopeResolver.ResolveWorkflowScopeAsync(workflowId, cancellationToken);
            if (workflowScope is not null)
            {
                var principalContext = AuthenticatedPrincipalMapper.ToPrincipalAccessContext(principal);
                if (_accessPolicy.CanAccessWorkflow(principalContext, workflowScope, permission))
                {
                    return ProtectedEndpointAuthorizationDecision.Allowed;
                }

                _logger.LogInformation(
                    "Report/artifact authorization denied for principal. WorkflowId={WorkflowId}, ArtifactId={ArtifactId}, Permission={Permission}, ReturnNotFound={ReturnNotFound}.",
                    workflowId,
                    artifactId,
                    permission,
                    options.ReturnNotFoundForTenantMismatch);

                return options.ReturnNotFoundForTenantMismatch
                    ? ProtectedEndpointAuthorizationDecision.NotFound
                    : ProtectedEndpointAuthorizationDecision.Forbidden;
            }
        }

        if (buildingId.HasValue)
        {
            return await AuthorizeBuildingScopeAsync(principal, buildingId.Value, permission, options, cancellationToken);
        }

        if (projectId.HasValue)
        {
            return await AuthorizeProjectScopeAsync(principal, projectId.Value, permission, options, cancellationToken);
        }

        return ProtectedEndpointAuthorizationDecision.Allowed;
    }

    private async Task<ProtectedEndpointAuthorizationDecision> AuthorizeProjectScopeAsync(
        AuthenticatedPrincipal principal,
        int projectId,
        Permission permission,
        ApiAuthorizationOptions options,
        CancellationToken cancellationToken)
    {
        var scope = await _projectScopeResolver.ResolveProjectScopeAsync(projectId, cancellationToken);
        if (scope is null)
        {
            return ProtectedEndpointAuthorizationDecision.NotFound;
        }

        var principalContext = AuthenticatedPrincipalMapper.ToPrincipalAccessContext(principal);
        if (_accessPolicy.CanAccessProject(principalContext, scope, permission))
        {
            return ProtectedEndpointAuthorizationDecision.Allowed;
        }

        _logger.LogInformation(
            "Project authorization denied for principal. ProjectId={ProjectId}, Permission={Permission}, ReturnNotFound={ReturnNotFound}.",
            projectId,
            permission,
            options.ReturnNotFoundForTenantMismatch);

        return options.ReturnNotFoundForTenantMismatch
            ? ProtectedEndpointAuthorizationDecision.NotFound
            : ProtectedEndpointAuthorizationDecision.Forbidden;
    }

    private async Task<ProtectedEndpointAuthorizationDecision> AuthorizeBuildingScopeAsync(
        AuthenticatedPrincipal principal,
        int buildingId,
        Permission permission,
        ApiAuthorizationOptions options,
        CancellationToken cancellationToken)
    {
        var scope = await _buildingScopeResolver.ResolveBuildingScopeAsync(buildingId, cancellationToken);
        if (scope is null)
        {
            return ProtectedEndpointAuthorizationDecision.NotFound;
        }

        var principalContext = AuthenticatedPrincipalMapper.ToPrincipalAccessContext(principal);
        if (_accessPolicy.CanAccessBuilding(principalContext, scope, permission))
        {
            return ProtectedEndpointAuthorizationDecision.Allowed;
        }

        _logger.LogInformation(
            "Building authorization denied for principal. BuildingId={BuildingId}, Permission={Permission}, ReturnNotFound={ReturnNotFound}.",
            buildingId,
            permission,
            options.ReturnNotFoundForTenantMismatch);

        return options.ReturnNotFoundForTenantMismatch
            ? ProtectedEndpointAuthorizationDecision.NotFound
            : ProtectedEndpointAuthorizationDecision.Forbidden;
    }
}
