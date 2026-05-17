using AssistantEngineer.Modules.Identity.Domain.Enums;

namespace AssistantEngineer.Api.Options.Security;

public static class ApiAuthorizationOptionsReadExtensions
{
    public static bool RequiresProtectedReadAuthorization(
        this ApiAuthorizationOptions options,
        Permission permission)
    {
        if (!options.Enabled || !options.EnableReadEndpointProtectionPilot)
        {
            return false;
        }

        return permission switch
        {
            Permission.ProjectsRead => options.RequireProjectReadAuthorization,
            Permission.BuildingsRead => options.RequireBuildingReadAuthorization,
            _ => false
        };
    }

    public static bool RequiresProtectedWorkflowReadAuthorization(this ApiAuthorizationOptions options)
    {
        return options.Enabled &&
               options.EnableWorkflowReadEndpointProtectionPilot &&
               options.RequireWorkflowReadAuthorization;
    }

    public static bool ShouldReturnNotFoundForWorkflowTenantMismatch(this ApiAuthorizationOptions options)
    {
        return options.ReturnNotFoundForWorkflowTenantMismatch || options.ReturnNotFoundForTenantMismatch;
    }
}
