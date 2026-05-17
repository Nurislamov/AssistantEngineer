using AssistantEngineer.Api.Security.Authentication;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;

namespace AssistantEngineer.Api.Security.TenantIsolation;

public interface ITenantQueryContextFactory
{
    TenantQueryContext CreateCurrent(
        bool? includeUnscopedResourcesInTenantLists = null,
        bool? returnNotFoundForTenantMismatch = null);

    TenantQueryContext CreateFromPrincipal(
        AuthenticatedPrincipal principal,
        bool? includeUnscopedResourcesInTenantLists = null,
        bool? returnNotFoundForTenantMismatch = null);
}
