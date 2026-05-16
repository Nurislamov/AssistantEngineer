using AssistantEngineer.Modules.Identity.Application.Contracts;

namespace AssistantEngineer.Tests.Identity;

public sealed class PrincipalAccessContextTests
{
    [Fact]
    public void UnauthenticatedContext_CanRepresentEmptyPrincipalState()
    {
        var context = new PrincipalAccessContext(
            UserId: null,
            OrganizationId: null,
            ExternalSubjectId: null,
            Roles: new HashSet<string>(StringComparer.Ordinal),
            Permissions: new HashSet<string>(StringComparer.Ordinal),
            IsAuthenticated: false);

        Assert.False(context.IsAuthenticated);
        Assert.Null(context.UserId);
        Assert.Null(context.OrganizationId);
        Assert.Empty(context.Roles);
        Assert.Empty(context.Permissions);
    }
}
