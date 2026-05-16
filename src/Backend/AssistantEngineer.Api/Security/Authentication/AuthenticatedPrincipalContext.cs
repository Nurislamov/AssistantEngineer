namespace AssistantEngineer.Api.Security.Authentication;

public sealed class AuthenticatedPrincipalContext
{
    public AuthenticatedPrincipal Principal { get; private set; } = AuthenticatedPrincipal.Anonymous;

    public void SetPrincipal(AuthenticatedPrincipal principal)
    {
        Principal = principal;
    }
}
