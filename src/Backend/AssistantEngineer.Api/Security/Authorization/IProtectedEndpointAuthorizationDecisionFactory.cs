namespace AssistantEngineer.Api.Security.Authorization;

public interface IProtectedEndpointAuthorizationDecisionFactory
{
    ProtectedEndpointAuthorizationDecision Allowed();

    ProtectedEndpointAuthorizationDecision Unauthorized();

    ProtectedEndpointAuthorizationDecision Forbidden();

    ProtectedEndpointAuthorizationDecision NotFound();
}
