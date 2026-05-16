using AssistantEngineer.Modules.Identity.Application.Services;
using AssistantEngineer.Modules.Identity.Domain.Enums;

namespace AssistantEngineer.Tests.Identity;

public sealed class OrganizationPermissionPolicyTests
{
    private readonly OrganizationPermissionPolicy _policy = new();

    [Fact]
    public void Owner_HasAdministrationAndAllCorePermissions()
    {
        var permissions = _policy.GetPermissions(OrganizationRole.Owner);

        Assert.Contains(Permission.AdministrationManage, permissions);
        Assert.Contains(Permission.ProjectsWrite, permissions);
        Assert.Contains(Permission.WorkflowsExecute, permissions);
        Assert.Contains(Permission.ReportsWrite, permissions);
    }

    [Fact]
    public void Engineer_HasWorkflowExecuteButNotAdministrationManage()
    {
        var permissions = _policy.GetPermissions(OrganizationRole.Engineer);

        Assert.Contains(Permission.WorkflowsExecute, permissions);
        Assert.DoesNotContain(Permission.AdministrationManage, permissions);
    }

    [Fact]
    public void Viewer_HasReadOnlyPermissions()
    {
        var permissions = _policy.GetPermissions(OrganizationRole.Viewer);

        Assert.Contains(Permission.ProjectsRead, permissions);
        Assert.Contains(Permission.BuildingsRead, permissions);
        Assert.Contains(Permission.WorkflowsRead, permissions);
        Assert.Contains(Permission.ReportsRead, permissions);

        Assert.DoesNotContain(Permission.ProjectsWrite, permissions);
        Assert.DoesNotContain(Permission.BuildingsWrite, permissions);
        Assert.DoesNotContain(Permission.WorkflowsExecute, permissions);
        Assert.DoesNotContain(Permission.ReportsWrite, permissions);
        Assert.DoesNotContain(Permission.AdministrationManage, permissions);
    }

    [Fact]
    public void UnknownRole_ReturnsSafeEmptySet()
    {
        var permissions = _policy.GetPermissions((OrganizationRole)999);

        Assert.Empty(permissions);
    }
}
