using AssistantEngineer.Api.Options.Security;
using AssistantEngineer.Api.Security.Authentication;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Application.Services.Access;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Api.Security.TenantIsolation;

public sealed class TenantQueryContextFactory : ITenantQueryContextFactory
{
    private readonly IAuthenticatedPrincipalProvider _principalProvider;
    private readonly IOptions<ProjectTenantAccessOptions> _projectTenantAccessOptions;
    private readonly IOptionsMonitor<ApiAuthorizationOptions> _authorizationOptions;

    public TenantQueryContextFactory(
        IAuthenticatedPrincipalProvider principalProvider,
        IOptions<ProjectTenantAccessOptions> projectTenantAccessOptions,
        IOptionsMonitor<ApiAuthorizationOptions> authorizationOptions)
    {
        _principalProvider = principalProvider;
        _projectTenantAccessOptions = projectTenantAccessOptions;
        _authorizationOptions = authorizationOptions;
    }

    public TenantQueryContext CreateCurrent(
        bool? includeUnscopedResourcesInTenantLists = null,
        bool? returnNotFoundForTenantMismatch = null)
    {
        var principal = _principalProvider.GetCurrentPrincipal();
        return CreateFromPrincipal(
            principal,
            includeUnscopedResourcesInTenantLists,
            returnNotFoundForTenantMismatch);
    }

    public TenantQueryContext CreateFromPrincipal(
        AuthenticatedPrincipal principal,
        bool? includeUnscopedResourcesInTenantLists = null,
        bool? returnNotFoundForTenantMismatch = null)
    {
        var principalContext = AuthenticatedPrincipalMapper.ToPrincipalAccessContext(principal);
        var tenantAccessOptions = _projectTenantAccessOptions.Value;
        var authorizationOptions = _authorizationOptions.CurrentValue;
        var includeUnscopedResources = includeUnscopedResourcesInTenantLists ??
                                       tenantAccessOptions.AllowUnscopedProjectsDuringTransition;
        var shouldReturnNotFound = returnNotFoundForTenantMismatch ??
                                   authorizationOptions.ReturnNotFoundForTenantMismatch;

        return TenantQueryContext.FromPrincipalAccessContext(
            principalContext,
            allowUnscopedResourcesDuringTransition: tenantAccessOptions.AllowUnscopedProjectsDuringTransition,
            strictTenantMatch: tenantAccessOptions.EnableStrictTenantMatch,
            returnNotFoundForTenantMismatch: shouldReturnNotFound,
            includeUnscopedResourcesInTenantLists: includeUnscopedResources);
    }
}
