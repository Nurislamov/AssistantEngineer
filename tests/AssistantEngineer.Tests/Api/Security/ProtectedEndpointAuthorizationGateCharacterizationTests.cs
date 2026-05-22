using AssistantEngineer.Api.Security.Authentication;
using AssistantEngineer.Api.Security.Authorization;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace AssistantEngineer.Tests.Api.Security;

public sealed class ProtectedEndpointAuthorizationGateCharacterizationTests
{
    private static readonly string[] RequiredCapabilities =
    [
        "ProjectsRead",
        "ProjectsWrite",
        "BuildingsRead",
        "BuildingsWrite",
        "WorkflowsRead",
        "WorkflowsExecute",
        "CalculationRun",
        "ReportsRead",
        "ReportsWrite",
        "ArtifactRead"
    ];

    [Fact]
    public async Task OptionsDisabled_DefaultCompatibility_AllCapabilitiesAreAllowed()
    {
        var gate = ProtectedEndpointAuthorizationGateCharacterizationHelper.CreateGate(
            ProtectedEndpointAuthorizationGateCharacterizationHelper.CreateCompatibilityDisabledOptions(),
            AuthenticatedPrincipal.Anonymous);

        foreach (var capability in RequiredCapabilities)
        {
            var decision = await InvokeCapabilityAsync(gate, capability);
            Assert.Equal(ProtectedEndpointAuthorizationOutcome.Allowed, decision.Outcome);
            Assert.Equal("Allowed", decision.Outcome.ToString());
        }
    }

    [Fact]
    public async Task ProtectionEnabled_UnauthenticatedPrincipal_ReturnsUnauthorizedForProtectedCapabilities()
    {
        var gate = ProtectedEndpointAuthorizationGateCharacterizationHelper.CreateGate(
            ProtectedEndpointAuthorizationGateCharacterizationHelper.CreateAllProtectionEnabledOptions(),
            AuthenticatedPrincipal.Anonymous);

        foreach (var capability in RequiredCapabilities)
        {
            var decision = await InvokeCapabilityAsync(gate, capability);
            Assert.Equal(ProtectedEndpointAuthorizationOutcome.Unauthorized, decision.Outcome);
            Assert.Equal("Unauthorized", decision.Outcome.ToString());
        }
    }

    [Fact]
    public async Task ProtectionEnabled_MissingPermissions_ReturnsForbiddenForProtectedCapabilities()
    {
        var principal = ProtectedEndpointAuthorizationGateCharacterizationHelper.CreatePrincipal(
            ProtectedEndpointAuthorizationGateCharacterizationHelper.DefaultOrganizationId);
        var gate = ProtectedEndpointAuthorizationGateCharacterizationHelper.CreateGate(
            ProtectedEndpointAuthorizationGateCharacterizationHelper.CreateAllProtectionEnabledOptions(),
            principal);

        foreach (var capability in RequiredCapabilities)
        {
            var decision = await InvokeCapabilityAsync(gate, capability);
            Assert.Equal(ProtectedEndpointAuthorizationOutcome.Forbidden, decision.Outcome);
            Assert.Equal("Forbidden", decision.Outcome.ToString());
        }
    }

    [Fact]
    public async Task ScopeNotFoundAndFallbackCases_AreCharacterized()
    {
        var principal = ProtectedEndpointAuthorizationGateCharacterizationHelper.CreatePrincipal(
            ProtectedEndpointAuthorizationGateCharacterizationHelper.DefaultOrganizationId,
            Permission.ProjectsRead,
            Permission.BuildingsRead,
            Permission.WorkflowsRead,
            Permission.WorkflowsExecute,
            Permission.ReportsRead,
            Permission.ReportsWrite,
            Permission.ProjectsWrite,
            Permission.BuildingsWrite);

        var missingProjectGate = ProtectedEndpointAuthorizationGateCharacterizationHelper.CreateGate(
            ProtectedEndpointAuthorizationGateCharacterizationHelper.CreateAllProtectionEnabledOptions(),
            principal,
            projectOrganizationId: null);
        var missingProjectDecision = await missingProjectGate.RequireProjectPermissionAsync(
            ProtectedEndpointAuthorizationGateCharacterizationHelper.ProjectId,
            Permission.ProjectsRead,
            CancellationToken.None);
        Assert.Equal(ProtectedEndpointAuthorizationOutcome.NotFound, missingProjectDecision.Outcome);

        var missingBuildingGate = ProtectedEndpointAuthorizationGateCharacterizationHelper.CreateGate(
            ProtectedEndpointAuthorizationGateCharacterizationHelper.CreateAllProtectionEnabledOptions(),
            principal,
            buildingOrganizationId: null);
        var missingBuildingDecision = await missingBuildingGate.RequireBuildingPermissionAsync(
            ProtectedEndpointAuthorizationGateCharacterizationHelper.BuildingId,
            Permission.BuildingsRead,
            CancellationToken.None);
        Assert.Equal(ProtectedEndpointAuthorizationOutcome.NotFound, missingBuildingDecision.Outcome);

        var missingRoomGate = ProtectedEndpointAuthorizationGateCharacterizationHelper.CreateGate(
            ProtectedEndpointAuthorizationGateCharacterizationHelper.CreateAllProtectionEnabledOptions(),
            principal,
            includeRoomScope: false);
        var missingRoomDecision = await missingRoomGate.RequireCalculationPermissionAsync(
            Permission.WorkflowsExecute,
            projectId: null,
            buildingId: null,
            floorId: null,
            roomId: ProtectedEndpointAuthorizationGateCharacterizationHelper.RoomId,
            CancellationToken.None);
        Assert.Equal(ProtectedEndpointAuthorizationOutcome.NotFound, missingRoomDecision.Outcome);

        var workflowScopeMissingNoFallbackGate = ProtectedEndpointAuthorizationGateCharacterizationHelper.CreateGate(
            ProtectedEndpointAuthorizationGateCharacterizationHelper.CreateAllProtectionEnabledOptions(),
            principal,
            projectOrganizationId: null,
            buildingOrganizationId: null,
            workflowOrganizationId: null);
        var workflowScopeMissingNoFallbackDecision = await workflowScopeMissingNoFallbackGate.RequireWorkflowPermissionAsync(
            Permission.WorkflowsExecute,
            workflowId: ProtectedEndpointAuthorizationGateCharacterizationHelper.WorkflowId,
            projectId: null,
            buildingId: null,
            CancellationToken.None);
        Assert.Equal(ProtectedEndpointAuthorizationOutcome.Allowed, workflowScopeMissingNoFallbackDecision.Outcome);

        var reportScopeMissingNoFallbackDecision = await workflowScopeMissingNoFallbackGate.RequireReportReadPermissionAsync(
            projectId: null,
            buildingId: null,
            workflowId: ProtectedEndpointAuthorizationGateCharacterizationHelper.WorkflowId,
            CancellationToken.None);
        Assert.Equal(ProtectedEndpointAuthorizationOutcome.Allowed, reportScopeMissingNoFallbackDecision.Outcome);
    }

    [Theory]
    [InlineData(false, ProtectedEndpointAuthorizationOutcome.Forbidden)]
    [InlineData(true, ProtectedEndpointAuthorizationOutcome.NotFound)]
    public async Task TenantMismatch_ForProjectScope_RespectsReturnNotFoundOption(
        bool returnNotFoundForTenantMismatch,
        ProtectedEndpointAuthorizationOutcome expected)
    {
        var options = ProtectedEndpointAuthorizationGateCharacterizationHelper.CreateAllProtectionEnabledOptions(
            returnNotFoundForTenantMismatch: returnNotFoundForTenantMismatch);
        var principal = ProtectedEndpointAuthorizationGateCharacterizationHelper.CreatePrincipal(
            ProtectedEndpointAuthorizationGateCharacterizationHelper.DefaultOrganizationId,
            Permission.ProjectsRead);
        var gate = ProtectedEndpointAuthorizationGateCharacterizationHelper.CreateGate(
            options,
            principal,
            projectOrganizationId: 3001);

        var decision = await gate.RequireProjectPermissionAsync(
            ProtectedEndpointAuthorizationGateCharacterizationHelper.ProjectId,
            Permission.ProjectsRead,
            CancellationToken.None);

        Assert.Equal(expected, decision.Outcome);
        Assert.Equal(expected.ToString(), decision.Outcome.ToString());
    }

    [Fact]
    public async Task TenantMismatch_ForWorkflowScope_UsesWorkflowSpecificNotFoundOption()
    {
        var options = ProtectedEndpointAuthorizationGateCharacterizationHelper.CreateAllProtectionEnabledOptions(
            returnNotFoundForTenantMismatch: false,
            returnNotFoundForWorkflowTenantMismatch: true);
        var principal = ProtectedEndpointAuthorizationGateCharacterizationHelper.CreatePrincipal(
            ProtectedEndpointAuthorizationGateCharacterizationHelper.DefaultOrganizationId,
            Permission.WorkflowsRead,
            Permission.WorkflowsExecute);
        var gate = ProtectedEndpointAuthorizationGateCharacterizationHelper.CreateGate(
            options,
            principal,
            workflowOrganizationId: 3001);

        var readDecision = await gate.RequireWorkflowReadPermissionAsync(
            workflowId: ProtectedEndpointAuthorizationGateCharacterizationHelper.WorkflowId,
            scenarioId: null,
            jobId: null,
            projectId: null,
            buildingId: null,
            CancellationToken.None);
        var executeDecision = await gate.RequireWorkflowPermissionAsync(
            Permission.WorkflowsExecute,
            workflowId: ProtectedEndpointAuthorizationGateCharacterizationHelper.WorkflowId,
            projectId: null,
            buildingId: null,
            CancellationToken.None);

        Assert.Equal(ProtectedEndpointAuthorizationOutcome.NotFound, readDecision.Outcome);
        Assert.Equal(ProtectedEndpointAuthorizationOutcome.NotFound, executeDecision.Outcome);
    }

    [Fact]
    public async Task MatchingPermissionsAndTenantScope_AllCapabilitiesAreAllowed()
    {
        var principal = ProtectedEndpointAuthorizationGateCharacterizationHelper.CreatePrincipal(
            ProtectedEndpointAuthorizationGateCharacterizationHelper.DefaultOrganizationId,
            Permission.ProjectsRead,
            Permission.ProjectsWrite,
            Permission.BuildingsRead,
            Permission.BuildingsWrite,
            Permission.WorkflowsRead,
            Permission.WorkflowsExecute,
            Permission.ReportsRead,
            Permission.ReportsWrite);
        var gate = ProtectedEndpointAuthorizationGateCharacterizationHelper.CreateGate(
            ProtectedEndpointAuthorizationGateCharacterizationHelper.CreateAllProtectionEnabledOptions(),
            principal);

        foreach (var capability in RequiredCapabilities)
        {
            var decision = await InvokeCapabilityAsync(gate, capability);
            Assert.Equal(ProtectedEndpointAuthorizationOutcome.Allowed, decision.Outcome);
            Assert.Equal("Allowed", decision.Outcome.ToString());
        }
    }

    [Fact]
    public async Task DenyLogging_RemainsStructuredAndRedacted()
    {
        var logger = new CapturingLogger<ProtectedEndpointAuthorizationGate>();
        var principal = ProtectedEndpointAuthorizationGateCharacterizationHelper.CreatePrincipal(
            ProtectedEndpointAuthorizationGateCharacterizationHelper.DefaultOrganizationId,
            Permission.ProjectsRead);
        var gate = ProtectedEndpointAuthorizationGateCharacterizationHelper.CreateGate(
            ProtectedEndpointAuthorizationGateCharacterizationHelper.CreateAllProtectionEnabledOptions(),
            principal,
            projectOrganizationId: 3001,
            logger: logger);

        var decision = await gate.RequireProjectPermissionAsync(
            ProtectedEndpointAuthorizationGateCharacterizationHelper.ProjectId,
            Permission.ProjectsRead,
            CancellationToken.None);

        Assert.Equal(ProtectedEndpointAuthorizationOutcome.Forbidden, decision.Outcome);
        Assert.NotEmpty(logger.Messages);
        Assert.Contains(logger.Messages, message => message.Contains("authorization denied", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(logger.Messages, message => message.Contains("Password=", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(logger.Messages, message => message.Contains("connection-string", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(logger.Messages, message => message.Contains("super-secret", StringComparison.OrdinalIgnoreCase));
    }

    private static Task<ProtectedEndpointAuthorizationDecision> InvokeCapabilityAsync(
        IProtectedEndpointAuthorizationGate gate,
        string capability)
    {
        return capability switch
        {
            "ProjectsRead" => gate.RequireProjectPermissionAsync(
                ProtectedEndpointAuthorizationGateCharacterizationHelper.ProjectId,
                Permission.ProjectsRead,
                CancellationToken.None),
            "ProjectsWrite" => gate.RequireProjectPermissionAsync(
                ProtectedEndpointAuthorizationGateCharacterizationHelper.ProjectId,
                Permission.ProjectsWrite,
                CancellationToken.None),
            "BuildingsRead" => gate.RequireBuildingPermissionAsync(
                ProtectedEndpointAuthorizationGateCharacterizationHelper.BuildingId,
                Permission.BuildingsRead,
                CancellationToken.None),
            "BuildingsWrite" => gate.RequireBuildingPermissionAsync(
                ProtectedEndpointAuthorizationGateCharacterizationHelper.BuildingId,
                Permission.BuildingsWrite,
                CancellationToken.None),
            "WorkflowsRead" => gate.RequireWorkflowReadPermissionAsync(
                workflowId: ProtectedEndpointAuthorizationGateCharacterizationHelper.WorkflowId,
                scenarioId: null,
                jobId: null,
                projectId: null,
                buildingId: null,
                CancellationToken.None),
            "WorkflowsExecute" => gate.RequireWorkflowPermissionAsync(
                Permission.WorkflowsExecute,
                workflowId: ProtectedEndpointAuthorizationGateCharacterizationHelper.WorkflowId,
                projectId: null,
                buildingId: null,
                CancellationToken.None),
            "CalculationRun" => gate.RequireCalculationPermissionAsync(
                Permission.WorkflowsExecute,
                projectId: null,
                buildingId: ProtectedEndpointAuthorizationGateCharacterizationHelper.BuildingId,
                floorId: null,
                roomId: null,
                CancellationToken.None),
            "ReportsRead" => gate.RequireReportReadPermissionAsync(
                projectId: ProtectedEndpointAuthorizationGateCharacterizationHelper.ProjectId,
                buildingId: ProtectedEndpointAuthorizationGateCharacterizationHelper.BuildingId,
                workflowId: ProtectedEndpointAuthorizationGateCharacterizationHelper.WorkflowId,
                CancellationToken.None),
            "ReportsWrite" => gate.RequireReportWritePermissionAsync(
                projectId: ProtectedEndpointAuthorizationGateCharacterizationHelper.ProjectId,
                buildingId: ProtectedEndpointAuthorizationGateCharacterizationHelper.BuildingId,
                workflowId: ProtectedEndpointAuthorizationGateCharacterizationHelper.WorkflowId,
                CancellationToken.None),
            "ArtifactRead" => gate.RequireArtifactReadPermissionAsync(
                projectId: ProtectedEndpointAuthorizationGateCharacterizationHelper.ProjectId,
                buildingId: ProtectedEndpointAuthorizationGateCharacterizationHelper.BuildingId,
                workflowId: ProtectedEndpointAuthorizationGateCharacterizationHelper.WorkflowId,
                artifactId: ProtectedEndpointAuthorizationGateCharacterizationHelper.ArtifactId,
                CancellationToken.None),
            _ => throw new InvalidOperationException($"Unsupported capability: {capability}")
        };
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _ = logLevel;
            _ = eventId;
            _ = exception;
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
