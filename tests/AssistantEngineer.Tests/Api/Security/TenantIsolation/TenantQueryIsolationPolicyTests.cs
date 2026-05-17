using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Application.Services.Access;
using AssistantEngineer.Modules.Identity.Domain.Enums;

namespace AssistantEngineer.Tests.Api.Security.TenantIsolation;

public sealed class TenantQueryIsolationPolicyTests
{
    private readonly TenantQueryIsolationPolicy _policy = new();

    [Fact]
    public void UnauthenticatedPrincipal_IsDenied()
    {
        var context = CreateContext(isAuthenticated: false);

        var decision = _policy.CanReadResource(context, TenantIsolationScenario.TenantAOrganizationId, Permission.ProjectsRead.ToString());

        Assert.False(decision.Allowed);
        Assert.Equal(TenantQueryFailureReasons.Unauthenticated, decision.FailureReason);
    }

    [Fact]
    public void MissingPermission_IsDenied()
    {
        var context = CreateContext(permissions: PermissionSet(Permission.BuildingsRead));

        var decision = _policy.CanReadResource(context, TenantIsolationScenario.TenantAOrganizationId, Permission.ProjectsRead.ToString());

        Assert.False(decision.Allowed);
        Assert.Equal(TenantQueryFailureReasons.MissingPermission, decision.FailureReason);
    }

    [Fact]
    public void MatchingOrganization_IsAllowedAsTenantScoped()
    {
        var context = CreateContext();

        var decision = _policy.CanReadResource(context, TenantIsolationScenario.TenantAOrganizationId, Permission.ProjectsRead.ToString());

        Assert.True(decision.Allowed);
        Assert.True(decision.IsTenantScoped);
        Assert.False(decision.IsUnscopedTransition);
    }

    [Fact]
    public void TenantMismatch_IsDenied()
    {
        var context = CreateContext();

        var decision = _policy.CanReadResource(context, TenantIsolationScenario.TenantBOrganizationId, Permission.ProjectsRead.ToString());

        Assert.False(decision.Allowed);
        Assert.Equal(TenantQueryFailureReasons.TenantMismatch, decision.FailureReason);
    }

    [Fact]
    public void TenantMismatch_UsesNotFoundOption()
    {
        var context = CreateContext(returnNotFoundForTenantMismatch: true);

        var decision = _policy.CanReadResource(context, TenantIsolationScenario.TenantBOrganizationId, Permission.ProjectsRead.ToString());

        Assert.False(decision.Allowed);
        Assert.True(decision.ShouldReturnNotFound);
    }

    [Fact]
    public void MissingOrganizationForScopedResource_IsDenied()
    {
        var context = CreateContext(organizationId: null);

        var decision = _policy.CanReadResource(context, TenantIsolationScenario.TenantAOrganizationId, Permission.ProjectsRead.ToString());

        Assert.False(decision.Allowed);
        Assert.Equal(TenantQueryFailureReasons.MissingOrganization, decision.FailureReason);
    }

    [Fact]
    public void LegacyUnscopedResource_IsAllowedWhenTransitionAllowsIt()
    {
        var context = CreateContext(allowUnscopedResourcesDuringTransition: true);

        var decision = _policy.CanReadResource(context, resourceOrganizationId: null, Permission.ProjectsRead.ToString());

        Assert.True(decision.Allowed);
        Assert.True(decision.IsUnscopedTransition);
        Assert.False(decision.IsTenantScoped);
    }

    [Fact]
    public void LegacyUnscopedResource_IsDeniedWhenTransitionDisallowsIt()
    {
        var context = CreateContext(allowUnscopedResourcesDuringTransition: false);

        var decision = _policy.CanReadResource(context, resourceOrganizationId: null, Permission.ProjectsRead.ToString());

        Assert.False(decision.Allowed);
        Assert.Equal(TenantQueryFailureReasons.UnscopedResourceDenied, decision.FailureReason);
    }

    [Fact]
    public void WriteResource_UsesSameTenantBoundaryAndRequiredPermission()
    {
        var context = CreateContext(permissions: PermissionSet(Permission.ProjectsWrite));

        var decision = _policy.CanWriteResource(context, TenantIsolationScenario.TenantAOrganizationId, Permission.ProjectsWrite.ToString());

        Assert.True(decision.Allowed);
        Assert.True(decision.IsTenantScoped);
    }

    private static TenantQueryContext CreateContext(
        bool isAuthenticated = true,
        int? organizationId = TenantIsolationScenario.TenantAOrganizationId,
        bool allowUnscopedResourcesDuringTransition = true,
        bool returnNotFoundForTenantMismatch = false,
        IReadOnlySet<string>? permissions = null)
    {
        return new TenantQueryContext(
            UserId: isAuthenticated ? TenantIsolationScenario.TenantAUserId : null,
            OrganizationId: organizationId,
            IsAuthenticated: isAuthenticated,
            Permissions: permissions ?? PermissionSet(Permission.ProjectsRead),
            AllowUnscopedResourcesDuringTransition: allowUnscopedResourcesDuringTransition,
            StrictTenantMatch: true,
            ReturnNotFoundForTenantMismatch: returnNotFoundForTenantMismatch);
    }

    private static IReadOnlySet<string> PermissionSet(params Permission[] permissions) =>
        new HashSet<string>(
            permissions.Select(permission => permission.ToString()),
            StringComparer.OrdinalIgnoreCase);
}
