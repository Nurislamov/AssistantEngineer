namespace AssistantEngineer.Api.Security.Authorization;

public enum ProtectedEndpointAuthorizationOutcome
{
    Allowed = 0,
    Unauthorized = 1,
    Forbidden = 2,
    NotFound = 3
}
