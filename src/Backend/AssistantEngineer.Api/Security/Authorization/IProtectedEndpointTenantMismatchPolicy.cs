using AssistantEngineer.Api.Options.Security;

namespace AssistantEngineer.Api.Security.Authorization;

public interface IProtectedEndpointTenantMismatchPolicy
{
    bool ShouldReturnNotFound(ApiAuthorizationOptions options, ProtectedEndpointScopeKind scopeKind);
}
