namespace AssistantEngineer.Modules.Identity.Application.Contracts.Access;

public sealed record BuildingAccessScope(
    int BuildingId,
    int ProjectId,
    int? OrganizationId,
    int? OwnerUserId,
    bool IsTenantScoped,
    TenantScope? TenantScope = null);
