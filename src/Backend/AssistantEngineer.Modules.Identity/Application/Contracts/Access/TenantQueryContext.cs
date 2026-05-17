using AssistantEngineer.Modules.Identity.Application.Contracts;

namespace AssistantEngineer.Modules.Identity.Application.Contracts.Access;

public sealed record TenantQueryContext(
    int? UserId,
    int? OrganizationId,
    bool IsAuthenticated,
    IReadOnlySet<string> Permissions,
    bool AllowUnscopedResourcesDuringTransition,
    bool StrictTenantMatch,
    bool ReturnNotFoundForTenantMismatch = false,
    bool IncludeUnscopedResourcesInTenantLists = false)
{
    public static TenantQueryContext FromPrincipalAccessContext(
        PrincipalAccessContext principal,
        bool allowUnscopedResourcesDuringTransition,
        bool strictTenantMatch,
        bool returnNotFoundForTenantMismatch = false,
        bool includeUnscopedResourcesInTenantLists = false)
    {
        return new TenantQueryContext(
            UserId: principal.UserId,
            OrganizationId: principal.OrganizationId,
            IsAuthenticated: principal.IsAuthenticated,
            Permissions: new HashSet<string>(principal.Permissions, StringComparer.OrdinalIgnoreCase),
            AllowUnscopedResourcesDuringTransition: allowUnscopedResourcesDuringTransition,
            StrictTenantMatch: strictTenantMatch,
            ReturnNotFoundForTenantMismatch: returnNotFoundForTenantMismatch,
            IncludeUnscopedResourcesInTenantLists: includeUnscopedResourcesInTenantLists);
    }
}
