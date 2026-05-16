using AssistantEngineer.Modules.Identity.Application.Contracts;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Application.Services.Access;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Identity;

public sealed class ProjectTenantAccessPolicyTests
{
    [Fact]
    public void UnauthenticatedPrincipal_CannotAccessProject()
    {
        var policy = CreatePolicy();
        var principal = CreatePrincipal(isAuthenticated: false, organizationId: 10, Permission.ProjectsRead);
        var scope = ProjectTenantAccessScopeFactory.CreateProjectScope(
            projectId: 42,
            organizationId: 10,
            ownerUserId: 7,
            isTenantScoped: true);

        var allowed = policy.CanAccessProject(principal, scope, Permission.ProjectsRead);

        Assert.False(allowed);
    }

    [Fact]
    public void PrincipalWithoutPermission_CannotAccessProject()
    {
        var policy = CreatePolicy();
        var principal = CreatePrincipal(isAuthenticated: true, organizationId: 10, Permission.BuildingsRead);
        var scope = ProjectTenantAccessScopeFactory.CreateProjectScope(
            projectId: 42,
            organizationId: 10,
            ownerUserId: 7,
            isTenantScoped: true);

        var allowed = policy.CanAccessProject(principal, scope, Permission.ProjectsWrite);

        Assert.False(allowed);
    }

    [Fact]
    public void TenantMismatch_DeniesProjectAccess()
    {
        var policy = CreatePolicy();
        var principal = CreatePrincipal(isAuthenticated: true, organizationId: 20, Permission.ProjectsRead);
        var scope = ProjectTenantAccessScopeFactory.CreateProjectScope(
            projectId: 42,
            organizationId: 10,
            ownerUserId: 7,
            isTenantScoped: true);

        var allowed = policy.CanAccessProject(principal, scope, Permission.ProjectsRead);

        Assert.False(allowed);
    }

    [Fact]
    public void MatchingTenantAndPermission_AllowsProjectAccess()
    {
        var policy = CreatePolicy();
        var principal = CreatePrincipal(isAuthenticated: true, organizationId: 10, Permission.ProjectsRead);
        var scope = ProjectTenantAccessScopeFactory.CreateProjectScope(
            projectId: 42,
            organizationId: 10,
            ownerUserId: 7,
            isTenantScoped: true,
            tenantScope: new TenantScope(10, "tenant-10", IsActive: true));

        var allowed = policy.CanAccessProject(principal, scope, Permission.ProjectsRead);

        Assert.True(allowed);
    }

    [Fact]
    public void InactiveTenantScope_DeniesAccess()
    {
        var policy = CreatePolicy();
        var principal = CreatePrincipal(isAuthenticated: true, organizationId: 10, Permission.ProjectsRead);
        var scope = ProjectTenantAccessScopeFactory.CreateProjectScope(
            projectId: 42,
            organizationId: 10,
            ownerUserId: 7,
            isTenantScoped: true,
            tenantScope: new TenantScope(10, "tenant-10", IsActive: false));

        var allowed = policy.CanAccessProject(principal, scope, Permission.ProjectsRead);

        Assert.False(allowed);
    }

    [Fact]
    public void UnscopedProjectTransitionBehavior_FollowsOptions()
    {
        var legacyScope = ProjectTenantAccessScopeFactory.CreateProjectScope(
            projectId: 99,
            organizationId: null,
            ownerUserId: null,
            isTenantScoped: false);

        var principal = CreatePrincipal(isAuthenticated: true, organizationId: 10, Permission.ProjectsRead);

        var allowPolicy = CreatePolicy(new ProjectTenantAccessOptions
        {
            AllowUnscopedProjectsDuringTransition = true,
            TreatMissingTenantAsBlocking = false,
            EnableStrictTenantMatch = true
        });

        var denyPolicy = CreatePolicy(new ProjectTenantAccessOptions
        {
            AllowUnscopedProjectsDuringTransition = false,
            TreatMissingTenantAsBlocking = false,
            EnableStrictTenantMatch = true
        });

        Assert.True(allowPolicy.CanAccessProject(principal, legacyScope, Permission.ProjectsRead));
        Assert.False(denyPolicy.CanAccessProject(principal, legacyScope, Permission.ProjectsRead));
    }

    [Fact]
    public void BuildingAccess_UsesTenantAndPermissionChecks()
    {
        var policy = CreatePolicy();
        var principal = CreatePrincipal(isAuthenticated: true, organizationId: 20, Permission.BuildingsRead);
        var scope = ProjectTenantAccessScopeFactory.CreateBuildingScope(
            buildingId: 501,
            projectId: 42,
            organizationId: 20,
            ownerUserId: 7,
            isTenantScoped: true,
            tenantScope: new TenantScope(20, "tenant-20", IsActive: true));

        var allowed = policy.CanAccessBuilding(principal, scope, Permission.BuildingsRead);
        var deniedByPermission = policy.CanAccessBuilding(principal, scope, Permission.BuildingsWrite);

        Assert.True(allowed);
        Assert.False(deniedByPermission);
    }

    [Fact]
    public void WorkflowAccess_HandlesTenantScopedResource()
    {
        var policy = CreatePolicy();
        var principal = CreatePrincipal(isAuthenticated: true, organizationId: 31, Permission.WorkflowsRead);
        var scope = ProjectTenantAccessScopeFactory.CreateWorkflowScope(
            workflowId: "wf-001",
            projectId: 42,
            buildingId: 501,
            organizationId: 31,
            ownerUserId: 7,
            isTenantScoped: true,
            tenantScope: new TenantScope(31, "tenant-31", IsActive: true));

        var allowed = policy.CanAccessWorkflow(principal, scope, Permission.WorkflowsRead);
        var deniedForOtherTenant = policy.CanAccessWorkflow(
            CreatePrincipal(isAuthenticated: true, organizationId: 99, Permission.WorkflowsRead),
            scope,
            Permission.WorkflowsRead);

        Assert.True(allowed);
        Assert.False(deniedForOtherTenant);
    }

    private static ProjectTenantAccessPolicy CreatePolicy(ProjectTenantAccessOptions? options = null)
    {
        return new ProjectTenantAccessPolicy(Options.Create(options ?? new ProjectTenantAccessOptions()));
    }

    private static PrincipalAccessContext CreatePrincipal(
        bool isAuthenticated,
        int? organizationId,
        params Permission[] permissions)
    {
        var permissionSet = permissions
            .Select(permission => permission.ToString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new PrincipalAccessContext(
            UserId: 7,
            OrganizationId: organizationId,
            ExternalSubjectId: "sub-7",
            Roles: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Engineer" },
            Permissions: permissionSet,
            IsAuthenticated: isAuthenticated);
    }
}
