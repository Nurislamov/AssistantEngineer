using AssistantEngineer.Api.Security.Authentication;

namespace AssistantEngineer.Tests.Api;

public sealed class AuthenticatedPrincipalContextTests
{
    [Fact]
    public void DefaultPrincipal_IsUnauthenticated()
    {
        var context = new AuthenticatedPrincipalContext();

        Assert.False(context.Principal.IsAuthenticated);
    }

    [Fact]
    public void ContextSetPrincipal_IsReturnedByProvider()
    {
        var context = new AuthenticatedPrincipalContext();
        var provider = new AuthenticatedPrincipalProvider(context);
        var principal = new AuthenticatedPrincipal(
            UserId: 7,
            OrganizationId: 11,
            ExternalSubjectId: "sub-7",
            AuthenticationScheme: "ApiKey",
            Roles: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Engineer" },
            Permissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ProjectsRead" },
            IsAuthenticated: true);

        context.SetPrincipal(principal);
        var resolved = provider.GetCurrentPrincipal();

        Assert.True(resolved.IsAuthenticated);
        Assert.Equal(7, resolved.UserId);
        Assert.Equal(11, resolved.OrganizationId);
    }

    [Fact]
    public void Mapper_ConvertsToPrincipalAccessContext()
    {
        var principal = new AuthenticatedPrincipal(
            UserId: 9,
            OrganizationId: 3,
            ExternalSubjectId: "external-subject",
            AuthenticationScheme: "ApiKey",
            Roles: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Admin" },
            Permissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ProjectsWrite" },
            IsAuthenticated: true);

        var mapped = AuthenticatedPrincipalMapper.ToPrincipalAccessContext(principal);

        Assert.True(mapped.IsAuthenticated);
        Assert.Equal(9, mapped.UserId);
        Assert.Equal(3, mapped.OrganizationId);
        Assert.Contains("ProjectsWrite", mapped.Permissions);
    }
}
