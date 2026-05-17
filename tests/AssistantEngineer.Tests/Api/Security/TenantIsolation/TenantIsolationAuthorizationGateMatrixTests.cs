using AssistantEngineer.Api.Security.Authorization;
using AssistantEngineer.Modules.Identity.Domain.Enums;

namespace AssistantEngineer.Tests.Api.Security.TenantIsolation;

public sealed class TenantIsolationAuthorizationGateMatrixTests
{
    [Fact]
    public Task ProjectsRead_Matrix() => AssertTenantMatrixAsync(
        TenantIsolationScenario.EndpointGroups.Single(group => group.Group == "ProjectsRead"));

    [Fact]
    public Task ProjectsWrite_Matrix() => AssertTenantMatrixAsync(
        TenantIsolationScenario.EndpointGroups.Single(group => group.Group == "ProjectsWrite"));

    [Fact]
    public Task BuildingsRead_Matrix() => AssertTenantMatrixAsync(
        TenantIsolationScenario.EndpointGroups.Single(group => group.Group == "BuildingsRead"));

    [Fact]
    public Task BuildingsWrite_Matrix() => AssertTenantMatrixAsync(
        TenantIsolationScenario.EndpointGroups.Single(group => group.Group == "BuildingsWrite"));

    [Fact]
    public Task WorkflowsRead_Matrix() => AssertTenantMatrixAsync(
        TenantIsolationScenario.EndpointGroups.Single(group => group.Group == "WorkflowsRead"));

    [Fact]
    public Task WorkflowsExecute_Matrix() => AssertTenantMatrixAsync(
        TenantIsolationScenario.EndpointGroups.Single(group => group.Group == "WorkflowsExecute"));

    [Fact]
    public Task CalculationRun_Matrix() => AssertTenantMatrixAsync(
        TenantIsolationScenario.EndpointGroups.Single(group => group.Group == "CalculationRun"));

    [Fact]
    public Task ReportsRead_Matrix() => AssertTenantMatrixAsync(
        TenantIsolationScenario.EndpointGroups.Single(group => group.Group == "ReportsRead"));

    [Fact]
    public Task ReportsWrite_Matrix() => AssertTenantMatrixAsync(
        TenantIsolationScenario.EndpointGroups.Single(group => group.Group == "ReportsWrite"));

    [Fact]
    public Task ArtifactRead_Matrix() => AssertTenantMatrixAsync(
        TenantIsolationScenario.EndpointGroups.Single(group => group.Group == "ArtifactRead"));

    [Fact]
    public Task WorkflowScenarioRead_Matrix() => AssertTenantMatrixAsync(
        TenantIsolationScenario.EndpointGroups.Single(group => group.Group == "WorkflowScenarioRead"));

    [Fact]
    public Task WorkflowJobRead_Matrix() => AssertTenantMatrixAsync(
        TenantIsolationScenario.EndpointGroups.Single(group => group.Group == "WorkflowJobRead"));

    [Fact]
    public Task WorkflowJobEventsRead_Matrix() => AssertTenantMatrixAsync(
        TenantIsolationScenario.EndpointGroups.Single(group => group.Group == "WorkflowJobEventsRead"));

    [Fact]
    public async Task MissingProjectOrBuildingScope_ReturnsNotFound()
    {
        var options = TenantIsolationTestResourceFactory.CreateAllProtectionOptions(returnNotFoundForTenantMismatch: false);
        var principal = TenantIsolationTestPrincipalFactory.TenantAWithAllPermissions();
        var gate = TenantIsolationTestResourceFactory.CreateGate(
            options,
            principal,
            projectOrganizationId: null,
            buildingOrganizationId: null,
            workflowOrganizationId: null);

        var projectDecision = await gate.RequireProjectPermissionAsync(
            TenantIsolationScenario.ProjectAId,
            Permission.ProjectsRead,
            CancellationToken.None);
        var buildingDecision = await gate.RequireBuildingPermissionAsync(
            TenantIsolationScenario.BuildingAId,
            Permission.BuildingsRead,
            CancellationToken.None);
        var calculationDecision = await gate.RequireCalculationPermissionAsync(
            Permission.WorkflowsExecute,
            projectId: null,
            buildingId: TenantIsolationScenario.BuildingAId,
            floorId: null,
            roomId: null,
            CancellationToken.None);

        Assert.Equal(ProtectedEndpointAuthorizationOutcome.NotFound, projectDecision.Outcome);
        Assert.Equal(ProtectedEndpointAuthorizationOutcome.NotFound, buildingDecision.Outcome);
        Assert.Equal(ProtectedEndpointAuthorizationOutcome.NotFound, calculationDecision.Outcome);
    }

    private static async Task AssertTenantMatrixAsync(TenantIsolationScenario scenario)
    {
        var anonymousGate = CreateGate(
            scenario,
            principal: TenantIsolationTestPrincipalFactory.Anonymous(),
            resourceOrganizationId: TenantIsolationScenario.TenantAOrganizationId,
            returnNotFoundForTenantMismatch: false);
        var anonymousDecision = await InvokeAsync(scenario, anonymousGate);
        Assert.Equal(ProtectedEndpointAuthorizationOutcome.Unauthorized, anonymousDecision.Outcome);

        var limitedGate = CreateGate(
            scenario,
            principal: TenantIsolationTestPrincipalFactory.TenantAWithout(scenario.Permission),
            resourceOrganizationId: TenantIsolationScenario.TenantAOrganizationId,
            returnNotFoundForTenantMismatch: false);
        var limitedDecision = await InvokeAsync(scenario, limitedGate);
        Assert.Equal(ProtectedEndpointAuthorizationOutcome.Forbidden, limitedDecision.Outcome);

        var sameTenantGate = CreateGate(
            scenario,
            principal: TenantIsolationTestPrincipalFactory.TenantAWithAllPermissions(),
            resourceOrganizationId: TenantIsolationScenario.TenantAOrganizationId,
            returnNotFoundForTenantMismatch: false);
        var sameTenantDecision = await InvokeAsync(scenario, sameTenantGate);
        Assert.Equal(ProtectedEndpointAuthorizationOutcome.Allowed, sameTenantDecision.Outcome);

        var crossTenantForbiddenGate = CreateGate(
            scenario,
            principal: TenantIsolationTestPrincipalFactory.TenantAWithAllPermissions(),
            resourceOrganizationId: TenantIsolationScenario.TenantBOrganizationId,
            returnNotFoundForTenantMismatch: false);
        var crossTenantForbiddenDecision = await InvokeAsync(scenario, crossTenantForbiddenGate);
        Assert.Equal(ProtectedEndpointAuthorizationOutcome.Forbidden, crossTenantForbiddenDecision.Outcome);

        var crossTenantNotFoundGate = CreateGate(
            scenario,
            principal: TenantIsolationTestPrincipalFactory.TenantAWithAllPermissions(),
            resourceOrganizationId: TenantIsolationScenario.TenantBOrganizationId,
            returnNotFoundForTenantMismatch: true);
        var crossTenantNotFoundDecision = await InvokeAsync(scenario, crossTenantNotFoundGate);
        Assert.Equal(ProtectedEndpointAuthorizationOutcome.NotFound, crossTenantNotFoundDecision.Outcome);
    }

    private static ProtectedEndpointAuthorizationGate CreateGate(
        TenantIsolationScenario scenario,
        AssistantEngineer.Api.Security.Authentication.AuthenticatedPrincipal principal,
        int resourceOrganizationId,
        bool returnNotFoundForTenantMismatch)
    {
        var options = TenantIsolationTestResourceFactory.CreateAllProtectionOptions(returnNotFoundForTenantMismatch);

        return TenantIsolationTestResourceFactory.CreateGate(
            options,
            principal,
            projectOrganizationId: scenario.UsesProjectScope ? resourceOrganizationId : TenantIsolationScenario.TenantAOrganizationId,
            buildingOrganizationId: scenario.UsesBuildingScope || scenario.UsesCalculationGate || scenario.UsesReportReadGate || scenario.UsesReportWriteGate || scenario.UsesArtifactReadGate
                ? resourceOrganizationId
                : TenantIsolationScenario.TenantAOrganizationId,
            workflowOrganizationId: scenario.UsesWorkflowReadGate || scenario.UsesWorkflowExecuteGate
                ? resourceOrganizationId
                : TenantIsolationScenario.TenantAOrganizationId);
    }

    private static Task<ProtectedEndpointAuthorizationDecision> InvokeAsync(
        TenantIsolationScenario scenario,
        ProtectedEndpointAuthorizationGate gate)
    {
        if (scenario.UsesProjectScope)
        {
            return gate.RequireProjectPermissionAsync(
                TenantIsolationScenario.ProjectAId,
                scenario.Permission,
                CancellationToken.None);
        }

        if (scenario.UsesBuildingScope)
        {
            return gate.RequireBuildingPermissionAsync(
                TenantIsolationScenario.BuildingAId,
                scenario.Permission,
                CancellationToken.None);
        }

        if (scenario.UsesWorkflowExecuteGate)
        {
            return gate.RequireWorkflowPermissionAsync(
                scenario.Permission,
                TenantIsolationScenario.WorkflowAId,
                projectId: null,
                buildingId: null,
                CancellationToken.None);
        }

        if (scenario.UsesCalculationGate)
        {
            return gate.RequireCalculationPermissionAsync(
                scenario.Permission,
                projectId: null,
                buildingId: TenantIsolationScenario.BuildingAId,
                floorId: null,
                roomId: null,
                CancellationToken.None);
        }

        if (scenario.UsesReportReadGate)
        {
            return gate.RequireReportReadPermissionAsync(
                projectId: null,
                buildingId: TenantIsolationScenario.BuildingAId,
                workflowId: null,
                CancellationToken.None);
        }

        if (scenario.UsesReportWriteGate)
        {
            return gate.RequireReportWritePermissionAsync(
                projectId: null,
                buildingId: TenantIsolationScenario.BuildingAId,
                workflowId: null,
                CancellationToken.None);
        }

        if (scenario.UsesArtifactReadGate)
        {
            return gate.RequireArtifactReadPermissionAsync(
                projectId: null,
                buildingId: TenantIsolationScenario.BuildingAId,
                workflowId: null,
                artifactId: TenantIsolationScenario.ArtifactAId,
                CancellationToken.None);
        }

        if (scenario.UsesWorkflowReadGate)
        {
            return gate.RequireWorkflowReadPermissionAsync(
                workflowId: scenario.UsesScenarioScope || scenario.UsesJobScope ? null : TenantIsolationScenario.WorkflowAId,
                scenarioId: scenario.UsesScenarioScope ? TenantIsolationScenario.ScenarioAId : null,
                jobId: scenario.UsesJobScope ? TenantIsolationScenario.JobAId : null,
                projectId: null,
                buildingId: null,
                CancellationToken.None);
        }

        throw new InvalidOperationException($"No tenant isolation matrix invocation was configured for {scenario.Group}.");
    }
}
