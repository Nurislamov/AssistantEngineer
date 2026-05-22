using AssistantEngineer.Api.Security.Authentication;
using AssistantEngineer.Modules.Identity.Domain.Enums;

namespace AssistantEngineer.Api.Security.Authorization;

public sealed record ProtectedEndpointPermissionEvaluationResult(
    AuthenticatedPrincipal Principal,
    bool IsAuthenticated,
    bool HasPermission,
    Permission? MissingPermission)
{
    public bool MissingRequiredPermission => IsAuthenticated && !HasPermission;
}
