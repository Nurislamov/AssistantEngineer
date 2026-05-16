using AssistantEngineer.Modules.Identity.Application.Contracts.Access;

namespace AssistantEngineer.Modules.Identity.Application.Services.Access;

public static class ProjectTenantAccessScopeFactory
{
    public static ProjectAccessScope CreateProjectScope(
        int projectId,
        int? organizationId,
        int? ownerUserId,
        bool isTenantScoped,
        string accessLevel = "Project",
        TenantScope? tenantScope = null)
    {
        return new ProjectAccessScope(
            ProjectId: projectId,
            OrganizationId: organizationId,
            OwnerUserId: ownerUserId,
            AccessLevel: accessLevel,
            IsTenantScoped: isTenantScoped,
            TenantScope: tenantScope);
    }

    public static BuildingAccessScope CreateBuildingScope(
        int buildingId,
        int projectId,
        int? organizationId,
        int? ownerUserId,
        bool isTenantScoped,
        TenantScope? tenantScope = null)
    {
        return new BuildingAccessScope(
            BuildingId: buildingId,
            ProjectId: projectId,
            OrganizationId: organizationId,
            OwnerUserId: ownerUserId,
            IsTenantScoped: isTenantScoped,
            TenantScope: tenantScope);
    }

    public static WorkflowAccessScope CreateWorkflowScope(
        string workflowId,
        int? projectId,
        int? buildingId,
        int? organizationId,
        int? ownerUserId,
        bool isTenantScoped,
        TenantScope? tenantScope = null)
    {
        return new WorkflowAccessScope(
            WorkflowId: workflowId,
            ProjectId: projectId,
            BuildingId: buildingId,
            OrganizationId: organizationId,
            OwnerUserId: ownerUserId,
            IsTenantScoped: isTenantScoped,
            TenantScope: tenantScope);
    }
}
