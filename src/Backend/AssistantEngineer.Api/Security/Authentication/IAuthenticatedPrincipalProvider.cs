namespace AssistantEngineer.Api.Security.Authentication;

public interface IAuthenticatedPrincipalProvider
{
    AuthenticatedPrincipal GetCurrentPrincipal();
}
