using AssistantEngineer.Modules.Identity.Domain.Enums;

namespace AssistantEngineer.Modules.Identity.Application.Services;

public sealed class OrganizationPermissionPolicy
{
    private static readonly IReadOnlySet<Permission> EmptyPermissions = new HashSet<Permission>();

    private static readonly IReadOnlySet<Permission> OwnerPermissions = new HashSet<Permission>
    {
        Permission.ProjectsRead,
        Permission.ProjectsWrite,
        Permission.BuildingsRead,
        Permission.BuildingsWrite,
        Permission.WorkflowsRead,
        Permission.WorkflowsExecute,
        Permission.ReportsRead,
        Permission.ReportsWrite,
        Permission.AdministrationManage
    };

    private static readonly IReadOnlySet<Permission> AdminPermissions = new HashSet<Permission>
    {
        Permission.ProjectsRead,
        Permission.ProjectsWrite,
        Permission.BuildingsRead,
        Permission.BuildingsWrite,
        Permission.WorkflowsRead,
        Permission.WorkflowsExecute,
        Permission.ReportsRead,
        Permission.ReportsWrite
    };

    private static readonly IReadOnlySet<Permission> EngineerPermissions = new HashSet<Permission>
    {
        Permission.ProjectsRead,
        Permission.BuildingsRead,
        Permission.BuildingsWrite,
        Permission.WorkflowsRead,
        Permission.WorkflowsExecute,
        Permission.ReportsRead,
        Permission.ReportsWrite
    };

    private static readonly IReadOnlySet<Permission> ViewerPermissions = new HashSet<Permission>
    {
        Permission.ProjectsRead,
        Permission.BuildingsRead,
        Permission.WorkflowsRead,
        Permission.ReportsRead
    };

    public IReadOnlySet<Permission> GetPermissions(OrganizationRole role)
    {
        return role switch
        {
            OrganizationRole.Owner => OwnerPermissions,
            OrganizationRole.Admin => AdminPermissions,
            OrganizationRole.Engineer => EngineerPermissions,
            OrganizationRole.Viewer => ViewerPermissions,
            _ => EmptyPermissions
        };
    }
}
