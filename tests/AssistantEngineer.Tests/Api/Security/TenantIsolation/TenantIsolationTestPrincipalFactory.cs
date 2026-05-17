using AssistantEngineer.Api.Security.ApiKey;
using AssistantEngineer.Api.Security.Authentication;
using AssistantEngineer.Modules.Identity.Domain.Enums;

namespace AssistantEngineer.Tests.Api.Security.TenantIsolation;

internal static class TenantIsolationTestPrincipalFactory
{
    public static AuthenticatedPrincipal TenantAWithAllPermissions()
    {
        return CreatePrincipal(
            userId: TenantIsolationScenario.TenantAUserId,
            organizationId: TenantIsolationScenario.TenantAOrganizationId,
            permissions: TenantIsolationScenario.TenantPermissions);
    }

    public static AuthenticatedPrincipal TenantBWithAllPermissions()
    {
        return CreatePrincipal(
            userId: TenantIsolationScenario.TenantBUserId,
            organizationId: TenantIsolationScenario.TenantBOrganizationId,
            permissions: TenantIsolationScenario.TenantPermissions);
    }

    public static AuthenticatedPrincipal TenantAWithout(Permission permission)
    {
        return CreatePrincipal(
            userId: TenantIsolationScenario.TenantAUserId,
            organizationId: TenantIsolationScenario.TenantAOrganizationId,
            permissions: TenantIsolationScenario.TenantPermissions.Where(candidate => candidate != permission));
    }

    public static AuthenticatedPrincipal TenantAWith(params Permission[] permissions)
    {
        return CreatePrincipal(
            userId: TenantIsolationScenario.TenantAUserId,
            organizationId: TenantIsolationScenario.TenantAOrganizationId,
            permissions: permissions);
    }

    public static AuthenticatedPrincipal Anonymous() => AuthenticatedPrincipal.Anonymous;

    private static AuthenticatedPrincipal CreatePrincipal(
        int userId,
        int organizationId,
        IEnumerable<Permission> permissions)
    {
        return new AuthenticatedPrincipal(
            UserId: userId,
            OrganizationId: organizationId,
            ExternalSubjectId: $"tenant-isolation-test-user-{userId}",
            AuthenticationScheme: ApiKeyAuthenticationHandler.SchemeName,
            Roles: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            Permissions: new HashSet<string>(
                permissions.Select(permission => permission.ToString()),
                StringComparer.OrdinalIgnoreCase),
            IsAuthenticated: true);
    }
}
