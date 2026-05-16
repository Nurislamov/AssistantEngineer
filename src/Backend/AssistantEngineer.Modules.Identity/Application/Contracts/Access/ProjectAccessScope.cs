namespace AssistantEngineer.Modules.Identity.Application.Contracts.Access;

public sealed record ProjectAccessScope(
    int ProjectId,
    int? OrganizationId,
    int? OwnerUserId,
    string AccessLevel,
    bool IsTenantScoped,
    TenantScope? TenantScope = null);
