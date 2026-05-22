using AssistantEngineer.Api.Security.Authentication;
using AssistantEngineer.Api.Security.Authorization;
using AssistantEngineer.Modules.Identity.Domain.Enums;

namespace AssistantEngineer.Tests.Api.Security;

public sealed class ProtectedEndpointPermissionEvaluatorTests
{
    [Fact]
    public void AnonymousPrincipal_ReturnsUnauthenticatedResult()
    {
        var evaluator = new ProtectedEndpointPermissionEvaluator(
            new StubPrincipalProvider(AuthenticatedPrincipal.Anonymous));

        var result = evaluator.Evaluate(Permission.ProjectsRead);

        Assert.False(result.IsAuthenticated);
        Assert.False(result.HasPermission);
        Assert.Equal(Permission.ProjectsRead, result.MissingPermission);
    }

    [Fact]
    public void AuthenticatedPrincipalWithoutPermission_ReturnsMissingPermission()
    {
        var principal = CreatePrincipal(
            organizationId: 1001,
            permissions: [Permission.BuildingsRead.ToString()]);
        var evaluator = new ProtectedEndpointPermissionEvaluator(new StubPrincipalProvider(principal));

        var result = evaluator.Evaluate(Permission.ProjectsRead);

        Assert.True(result.IsAuthenticated);
        Assert.False(result.HasPermission);
        Assert.Equal(Permission.ProjectsRead, result.MissingPermission);
        Assert.True(result.MissingRequiredPermission);
    }

    [Fact]
    public void PermissionComparison_IsCaseInsensitiveAndMatchesCurrentSemantics()
    {
        var principal = CreatePrincipal(
            organizationId: 1001,
            permissions: [Permission.ProjectsRead.ToString().ToLowerInvariant()]);
        var evaluator = new ProtectedEndpointPermissionEvaluator(new StubPrincipalProvider(principal));

        var result = evaluator.Evaluate(Permission.ProjectsRead);

        Assert.True(result.IsAuthenticated);
        Assert.True(result.HasPermission);
        Assert.Null(result.MissingPermission);
        Assert.False(result.MissingRequiredPermission);
    }

    [Fact]
    public void ResultDoesNotExposeSecretLikeValues()
    {
        var principal = CreatePrincipal(
            organizationId: 1001,
            permissions: []);
        var evaluator = new ProtectedEndpointPermissionEvaluator(new StubPrincipalProvider(principal));

        var result = evaluator.Evaluate(Permission.WorkflowsExecute);
        var serialized = result.ToString() ?? string.Empty;

        Assert.DoesNotContain("Password=", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("connection-string", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", serialized, StringComparison.OrdinalIgnoreCase);
    }

    private static AuthenticatedPrincipal CreatePrincipal(
        int organizationId,
        IReadOnlyCollection<string> permissions)
    {
        return new AuthenticatedPrincipal(
            UserId: 101,
            OrganizationId: organizationId,
            ExternalSubjectId: "p8-03c-permission-evaluator-principal",
            AuthenticationScheme: "Test",
            Roles: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            Permissions: new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase),
            IsAuthenticated: true);
    }

    private sealed class StubPrincipalProvider : IAuthenticatedPrincipalProvider
    {
        private readonly AuthenticatedPrincipal _principal;

        public StubPrincipalProvider(AuthenticatedPrincipal principal)
        {
            _principal = principal;
        }

        public AuthenticatedPrincipal GetCurrentPrincipal() => _principal;
    }
}
