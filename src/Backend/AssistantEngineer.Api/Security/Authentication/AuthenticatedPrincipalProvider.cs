namespace AssistantEngineer.Api.Security.Authentication;

public sealed class AuthenticatedPrincipalProvider : IAuthenticatedPrincipalProvider
{
    private readonly AuthenticatedPrincipalContext _context;

    public AuthenticatedPrincipalProvider(AuthenticatedPrincipalContext context)
    {
        _context = context;
    }

    public AuthenticatedPrincipal GetCurrentPrincipal()
    {
        return _context.Principal;
    }
}
