namespace AssistantEngineer.Api.Security.Authorization;

public readonly record struct ProtectedEndpointAuthorizationDecision(ProtectedEndpointAuthorizationOutcome Outcome)
{
    public static ProtectedEndpointAuthorizationDecision Allowed => new(ProtectedEndpointAuthorizationOutcome.Allowed);

    public static ProtectedEndpointAuthorizationDecision Unauthorized => new(ProtectedEndpointAuthorizationOutcome.Unauthorized);

    public static ProtectedEndpointAuthorizationDecision Forbidden => new(ProtectedEndpointAuthorizationOutcome.Forbidden);

    public static ProtectedEndpointAuthorizationDecision NotFound => new(ProtectedEndpointAuthorizationOutcome.NotFound);

    public bool IsAllowed => Outcome == ProtectedEndpointAuthorizationOutcome.Allowed;
}
