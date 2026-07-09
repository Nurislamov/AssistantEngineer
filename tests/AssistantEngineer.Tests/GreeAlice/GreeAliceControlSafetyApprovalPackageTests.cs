using System;
using System.IO;
using System.Linq;
using AssistantEngineer.GreeAliceBridge.Application.GreeCloud;
using AssistantEngineer.GreeAliceBridge.Application.GreeCloud.ControlApproval;
using AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud;
using AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.ControlApproval;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class GreeAliceControlSafetyApprovalPackageTests
{
    [Fact]
    public void ControlApprovalBoundaryDefaultsToNotApprovedAndFailClosed()
    {
        Assert.Equal("not-approved", GreeCloudControlApprovalBoundary.ControlApprovalStatus);
        Assert.False(GreeCloudControlApprovalBoundary.LiveControlApproved);
        Assert.False(GreeCloudControlApprovalBoundary.LiveControlImplemented);
        Assert.False(GreeCloudControlApprovalBoundary.LiveControlEnabled);
        Assert.False(GreeCloudControlApprovalBoundary.ControlAdapterEnabled);
        Assert.True(GreeCloudControlApprovalBoundary.ControlAdapterFailClosed);
        Assert.False(GreeCloudControlApprovalBoundary.MqttAllowed);
        Assert.False(GreeCloudControlApprovalBoundary.ProductionWiringAllowed);
        Assert.False(GreeCloudControlApprovalBoundary.SingleDevicePilotApproved);
        Assert.True(GreeCloudControlApprovalBoundary.RequiresManualApproval);
        Assert.True(GreeCloudControlApprovalBoundary.RequiresAuditLogging);
        Assert.True(GreeCloudControlApprovalBoundary.RequiresKillSwitchPlan);
        Assert.True(GreeCloudControlApprovalBoundary.RequiresRollbackPlan);
    }

    [Fact]
    public void ControlApprovalBoundaryListsCandidateAndForbiddenOperations()
    {
        Assert.Equal(
            [
                "power_on_off",
                "set_mode",
                "set_target_temperature",
                "set_fan_speed",
                "set_swing_vertical",
                "set_swing_horizontal"
            ],
            GreeCloudControlApprovalBoundary.CandidateOperations);

        foreach (string operation in new[]
        {
            "firmware_update",
            "device_binding",
            "device_unbinding",
            "account_mutation",
            "schedule_mutation",
            "timer_mutation",
            "scene_execution",
            "bulk_control",
            "mqtt_connect",
            "mqtt_subscribe",
            "mqtt_publish",
            "production_deploy"
        })
        {
            Assert.Contains(operation, GreeCloudControlApprovalBoundary.ForbiddenOperations);
        }
    }

    [Fact]
    public void ControlApprovalSafetyLimitsRemainConservative()
    {
        Assert.Equal(18, GreeCloudControlSafetyLimit.MinTargetTemperatureC);
        Assert.Equal(30, GreeCloudControlSafetyLimit.MaxTargetTemperatureC);
        Assert.True(GreeCloudControlSafetyLimit.RequiresSingleDeviceScope);
        Assert.True(GreeCloudControlSafetyLimit.RequiresRateLimit);
        Assert.True(GreeCloudControlSafetyLimit.RequiresAuditEvent);
    }

    [Fact]
    public void ControlApprovalEvaluatorReturnsNotApprovedAndUnmetRequirements()
    {
        IGreeCloudControlApprovalEvaluator evaluator = new OfflineGreeCloudControlApprovalEvaluator();

        GreeCloudControlPilotDecision decision = evaluator.Evaluate();

        Assert.Equal("not-approved", decision.Status);
        Assert.False(decision.LiveControlApproved);
        Assert.False(decision.LiveControlEnabled);
        Assert.False(decision.ControlAdapterEnabled);
        Assert.True(decision.ControlAdapterFailClosed);
        Assert.False(decision.MqttAllowed);
        Assert.False(decision.ProductionWiringAllowed);
        Assert.False(decision.SingleDevicePilotApproved);
        Assert.Equal(GreeCloudControlApprovalBoundary.RequiredRequirements, decision.UnmetRequirements);
    }

    [Fact]
    public void ControlApprovalEvaluatorKeepsControlBlockedWithPartialManualMetadata()
    {
        IGreeCloudControlApprovalEvaluator evaluator = new OfflineGreeCloudControlApprovalEvaluator();
        var input = new GreeCloudControlApprovalEvaluation(new HashSet<string>(StringComparer.Ordinal)
        {
            GreeCloudControlPilotRequirement.RepositoryCleanAndSynced,
            GreeCloudControlPilotRequirement.AllTestsPass
        });

        GreeCloudControlPilotDecision decision = evaluator.Evaluate(input);

        Assert.Equal("not-approved", decision.Status);
        Assert.False(decision.LiveControlApproved);
        Assert.False(decision.LiveControlEnabled);
        Assert.DoesNotContain("RepositoryCleanAndSynced", decision.UnmetRequirements);
        Assert.DoesNotContain("AllTestsPass", decision.UnmetRequirements);
        Assert.Contains("ManualOperatorApprovalRecorded", decision.UnmetRequirements);
    }

    [Fact]
    public void ControlApprovalDocsExistAndDefaultToNotApproved()
    {
        string approvalPackage = ReadRepoFile("docs", "integrations", "gree-alice", "control-safety-approval-package.md");
        string checklist = ReadRepoFile("docs", "integrations", "gree-alice", "control-pilot-approval-checklist.md");
        string decisionTemplate = ReadRepoFile("docs", "integrations", "gree-alice", "control-pilot-decision-record-template.md");
        string combined = string.Join(Environment.NewLine, approvalPackage, checklist, decisionTemplate);

        Assert.Contains("Control approval status: NOT APPROVED", approvalPackage, StringComparison.Ordinal);
        Assert.Contains("Control adapter: disabled / fail-closed", approvalPackage, StringComparison.Ordinal);
        Assert.Contains("candidate list only", approvalPackage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MQTT: blocked", approvalPackage, StringComparison.Ordinal);
        Assert.Contains("Production wiring: blocked", approvalPackage, StringComparison.Ordinal);
        Assert.Contains("credentials stored only outside repo", approvalPackage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("device/account identifiers masked", approvalPackage, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("Approval status: NOT APPROVED", checklist, StringComparison.Ordinal);
        Assert.Contains("Control adapter remains fail-closed", checklist, StringComparison.Ordinal);
        Assert.Contains("MQTT remains blocked", checklist, StringComparison.Ordinal);
        Assert.Contains("Credentials stored only outside repo", checklist, StringComparison.Ordinal);

        Assert.Contains("Decision status: NOT APPROVED", decisionTemplate, StringComparison.Ordinal);
        Assert.Contains("Credential material is stored only outside the repository", decisionTemplate, StringComparison.Ordinal);
        Assert.Contains("masked identifiers", decisionTemplate, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("NOT APPROVED", combined, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExistingControlAdapterRemainsFailClosedAndSendsNothing()
    {
        IGreeCloudControlAdapter adapter = new OfflineGreeCloudControlAdapter();

        GreeCloudControlResult result = await adapter.ExecuteControlAsync(new GreeCloudControlRequest(
            "dummy-gree-ac-001",
            "devices.capabilities.on_off",
            "set",
            "true"));

        Assert.Equal("dry-run-fail-closed", result.Status);
        Assert.False(result.SentToGreeCloud);
        Assert.False(result.SentToMqtt);
        Assert.False(result.SentToDevice);
        Assert.Equal("offline-fake", result.AdapterMode);
        Assert.False(GreeCloudControlApprovalBoundary.ControlAdapterEnabled);
        Assert.True(GreeCloudControlApprovalBoundary.ControlAdapterFailClosed);
    }

    [Fact]
    public void GreeAliceBridgeSourceContainsNoLiveControlNetworkOrProductionWiringImplementation()
    {
        string combined = ReadBridgeSource()
            .Replace("CredentialsStoredOutsideRepository", string.Empty, StringComparison.Ordinal);

        Assert.DoesNotContain("HttpClient", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".GetAsync(", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".PostAsync(", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MqttClient", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".ConnectAsync(", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".SubscribeAsync(", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".PublishAsync(", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DeviceControlService", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SendToDevice", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UseProductionRuntimeWiring", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EnableProductionRuntimeWiring", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AddProductionRuntimeWiring", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConnectionString", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("clientSecret", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("access-token", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refresh-token", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deviceKey", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("macAddress", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("real-account-", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("real-device-", combined, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GreeAliceBridgeProjectsRemainIsolatedFromProductionApiTelegramDeploymentsAndMigrations()
    {
        string root = FindRepositoryRoot();
        string bridgeRoot = Path.Combine(root, "src", "Integrations", "GreeAliceBridge");
        string combinedProjects = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(bridgeRoot, "*.csproj", SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        Assert.DoesNotContain("src\\Backend\\AssistantEngineer.Api", combinedProjects, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AssistantEngineer.Api.csproj", combinedProjects, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Telegram", combinedProjects, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Migration", combinedProjects, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deploy", combinedProjects, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GreeAlicePublicDocsDoNotMentionForbiddenSourceNames()
    {
        string root = FindRepositoryRoot();
        string docsRoot = Path.Combine(root, "docs", "integrations", "gree-alice");
        string combined = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(docsRoot, "*.md", SearchOption.TopDirectoryOnly)
                .Select(File.ReadAllText));

        Assert.DoesNotContain("openhab", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gree-remote", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("st-gree-driver", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("github.com/", combined, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadBridgeSource()
    {
        string root = FindRepositoryRoot();
        string bridgeRoot = Path.Combine(root, "src", "Integrations", "GreeAliceBridge");

        return string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(bridgeRoot, "*.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));
    }

    private static string ReadRepoFile(params string[] relativeParts)
    {
        string root = FindRepositoryRoot();
        string path = Path.Combine(new[] { root }.Concat(relativeParts).ToArray());

        Assert.True(File.Exists(path), "Expected repository file to exist: " + path);

        return File.ReadAllText(path);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AssistantEngineer.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate AssistantEngineer.sln from " + AppContext.BaseDirectory);
    }
}
