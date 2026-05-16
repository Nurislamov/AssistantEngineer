namespace AssistantEngineer.Modules.Identity.Application.Contracts.Access;

public sealed record WorkflowAccessScope(
    string WorkflowId,
    int? ProjectId,
    int? BuildingId,
    int? OrganizationId,
    int? OwnerUserId,
    bool IsTenantScoped,
    TenantScope? TenantScope = null);
