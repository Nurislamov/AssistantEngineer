namespace AssistantEngineer.Modules.Identity.Application.Contracts.Access;

public interface ITenantQueryIsolationPolicy
{
    TenantScopedQueryDecision CanReadResource(
        TenantQueryContext context,
        int? resourceOrganizationId,
        string requiredPermission);

    TenantScopedQueryDecision CanWriteResource(
        TenantQueryContext context,
        int? resourceOrganizationId,
        string requiredPermission);
}
