using AssistantEngineer.Modules.Identity.Application.Contracts;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.Identity.Application.Services.Access;

public sealed class ProjectTenantAccessPolicy
{
    private readonly ProjectTenantAccessOptions _options;

    public ProjectTenantAccessPolicy(IOptions<ProjectTenantAccessOptions> options)
    {
        _options = options.Value;
    }

    public bool CanAccessProject(
        PrincipalAccessContext principal,
        ProjectAccessScope projectScope,
        Permission requiredPermission)
    {
        if (!IsPrincipalAuthorized(principal, requiredPermission))
        {
            return false;
        }

        return IsScopeAccessible(
            principal.OrganizationId,
            projectScope.OrganizationId,
            projectScope.IsTenantScoped,
            projectScope.TenantScope);
    }

    public bool CanAccessBuilding(
        PrincipalAccessContext principal,
        BuildingAccessScope buildingScope,
        Permission requiredPermission)
    {
        if (!IsPrincipalAuthorized(principal, requiredPermission))
        {
            return false;
        }

        return IsScopeAccessible(
            principal.OrganizationId,
            buildingScope.OrganizationId,
            buildingScope.IsTenantScoped,
            buildingScope.TenantScope);
    }

    public bool CanAccessWorkflow(
        PrincipalAccessContext principal,
        WorkflowAccessScope workflowScope,
        Permission requiredPermission)
    {
        if (!IsPrincipalAuthorized(principal, requiredPermission))
        {
            return false;
        }

        return IsScopeAccessible(
            principal.OrganizationId,
            workflowScope.OrganizationId,
            workflowScope.IsTenantScoped,
            workflowScope.TenantScope);
    }

    private bool IsPrincipalAuthorized(PrincipalAccessContext principal, Permission requiredPermission)
    {
        if (!principal.IsAuthenticated)
        {
            return false;
        }

        var requiredPermissionName = requiredPermission.ToString();
        return principal.Permissions.Any(permission =>
            string.Equals(permission, requiredPermissionName, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsScopeAccessible(
        int? principalOrganizationId,
        int? resourceOrganizationId,
        bool isTenantScoped,
        TenantScope? tenantScope)
    {
        if (tenantScope is not null && !tenantScope.IsActive)
        {
            return false;
        }

        if (resourceOrganizationId.HasValue)
        {
            if (_options.EnableStrictTenantMatch)
            {
                return principalOrganizationId.HasValue &&
                       principalOrganizationId.Value == resourceOrganizationId.Value;
            }

            if (!principalOrganizationId.HasValue)
            {
                return false;
            }

            return principalOrganizationId.Value == resourceOrganizationId.Value;
        }

        if (_options.TreatMissingTenantAsBlocking || isTenantScoped)
        {
            return false;
        }

        return _options.AllowUnscopedProjectsDuringTransition;
    }
}
