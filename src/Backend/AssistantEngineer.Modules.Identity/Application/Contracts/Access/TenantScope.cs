namespace AssistantEngineer.Modules.Identity.Application.Contracts.Access;

public sealed record TenantScope(
    int OrganizationId,
    string OrganizationSlug,
    bool IsActive);
