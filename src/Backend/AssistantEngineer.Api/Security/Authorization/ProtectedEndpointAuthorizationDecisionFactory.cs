namespace AssistantEngineer.Api.Security.Authorization;

public sealed class ProtectedEndpointAuthorizationDecisionFactory : IProtectedEndpointAuthorizationDecisionFactory
{
    public ProtectedEndpointAuthorizationDecision Allowed() =>
        ProtectedEndpointAuthorizationDecision.Allowed;

    public ProtectedEndpointAuthorizationDecision Unauthorized() =>
        ProtectedEndpointAuthorizationDecision.Unauthorized;

    public ProtectedEndpointAuthorizationDecision Forbidden() =>
        ProtectedEndpointAuthorizationDecision.Forbidden;

    public ProtectedEndpointAuthorizationDecision NotFound() =>
        ProtectedEndpointAuthorizationDecision.NotFound;
}
