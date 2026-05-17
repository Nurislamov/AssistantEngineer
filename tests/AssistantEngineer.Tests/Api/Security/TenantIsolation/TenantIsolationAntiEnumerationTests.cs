using AssistantEngineer.Api.Security.Authorization;
using AssistantEngineer.Modules.Identity.Domain.Enums;

namespace AssistantEngineer.Tests.Api.Security.TenantIsolation;

public sealed class TenantIsolationAntiEnumerationTests
{
    [Fact]
    public async Task CrossTenantProject_WithNotFoundDisabled_ReturnsForbidden()
    {
        var gate = CreateCrossTenantGate(returnNotFoundForTenantMismatch: false);

        var decision = await gate.RequireProjectPermissionAsync(
            TenantIsolationScenario.ProjectAId,
            Permission.ProjectsRead,
            CancellationToken.None);

        Assert.Equal(ProtectedEndpointAuthorizationOutcome.Forbidden, decision.Outcome);
    }

    [Fact]
    public async Task CrossTenantProject_WithNotFoundEnabled_ReturnsNotFound()
    {
        var gate = CreateCrossTenantGate(returnNotFoundForTenantMismatch: true);

        var decision = await gate.RequireProjectPermissionAsync(
            TenantIsolationScenario.ProjectAId,
            Permission.ProjectsRead,
            CancellationToken.None);

        Assert.Equal(ProtectedEndpointAuthorizationOutcome.NotFound, decision.Outcome);
    }

    [Fact]
    public async Task TrulyMissingProject_ReturnsNotFound()
    {
        var gate = TenantIsolationTestResourceFactory.CreateGate(
            TenantIsolationTestResourceFactory.CreateAllProtectionOptions(returnNotFoundForTenantMismatch: true),
            TenantIsolationTestPrincipalFactory.TenantAWithAllPermissions(),
            projectOrganizationId: null,
            buildingOrganizationId: null,
            workflowOrganizationId: null);

        var decision = await gate.RequireProjectPermissionAsync(
            TenantIsolationScenario.ProjectAId,
            Permission.ProjectsRead,
            CancellationToken.None);

        Assert.Equal(ProtectedEndpointAuthorizationOutcome.NotFound, decision.Outcome);
    }

    [Fact]
    public void AuthorizationDecisionNames_DoNotDiscloseTenantOwnershipDetails()
    {
        var safeDecisionNames = new[]
        {
            ProtectedEndpointAuthorizationOutcome.Unauthorized.ToString(),
            ProtectedEndpointAuthorizationOutcome.Forbidden.ToString(),
            ProtectedEndpointAuthorizationOutcome.NotFound.ToString()
        };

        foreach (var decisionName in safeDecisionNames)
        {
            Assert.DoesNotContain("belongs", decisionName, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("tenant", decisionName, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(TenantIsolationScenario.TenantAOrganizationId.ToString(), decisionName, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(TenantIsolationScenario.TenantBOrganizationId.ToString(), decisionName, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static ProtectedEndpointAuthorizationGate CreateCrossTenantGate(bool returnNotFoundForTenantMismatch)
    {
        return TenantIsolationTestResourceFactory.CreateGate(
            TenantIsolationTestResourceFactory.CreateAllProtectionOptions(returnNotFoundForTenantMismatch),
            TenantIsolationTestPrincipalFactory.TenantAWithAllPermissions(),
            projectOrganizationId: TenantIsolationScenario.TenantBOrganizationId,
            buildingOrganizationId: TenantIsolationScenario.TenantBOrganizationId,
            workflowOrganizationId: TenantIsolationScenario.TenantBOrganizationId);
    }
}
