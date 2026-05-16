using AssistantEngineer.Modules.Identity.Application.Contracts;

namespace AssistantEngineer.Api.Security.Authentication;

public static class AuthenticatedPrincipalMapper
{
    public static PrincipalAccessContext ToPrincipalAccessContext(AuthenticatedPrincipal principal)
    {
        return new PrincipalAccessContext(
            UserId: principal.UserId,
            OrganizationId: principal.OrganizationId,
            ExternalSubjectId: principal.ExternalSubjectId,
            Roles: principal.Roles,
            Permissions: principal.Permissions,
            IsAuthenticated: principal.IsAuthenticated);
    }
}
