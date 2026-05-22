using AssistantEngineer.Api.Security.Authentication;
using AssistantEngineer.Modules.Identity.Domain.Enums;

namespace AssistantEngineer.Api.Security.Authorization;

public sealed class ProtectedEndpointPermissionEvaluator : IProtectedEndpointPermissionEvaluator
{
    private readonly IAuthenticatedPrincipalProvider _principalProvider;

    public ProtectedEndpointPermissionEvaluator(IAuthenticatedPrincipalProvider principalProvider)
    {
        _principalProvider = principalProvider;
    }

    public ProtectedEndpointPermissionEvaluationResult Evaluate(Permission permission)
    {
        var principal = _principalProvider.GetCurrentPrincipal();
        if (!principal.IsAuthenticated)
        {
            return new ProtectedEndpointPermissionEvaluationResult(
                principal,
                IsAuthenticated: false,
                HasPermission: false,
                MissingPermission: permission);
        }

        var requiredPermission = permission.ToString();
        var hasPermission = principal.Permissions.Any(candidate =>
            string.Equals(candidate, requiredPermission, StringComparison.OrdinalIgnoreCase));

        return new ProtectedEndpointPermissionEvaluationResult(
            principal,
            IsAuthenticated: true,
            HasPermission: hasPermission,
            MissingPermission: hasPermission ? null : permission);
    }
}
