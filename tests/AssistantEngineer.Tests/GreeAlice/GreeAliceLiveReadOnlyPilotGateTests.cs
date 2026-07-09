using System;
using System.IO;
using System.Linq;
using AssistantEngineer.GreeAliceBridge.Application.GreeCloud.LiveReadOnly;
using AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.LiveReadOnly.PilotGate;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class GreeAliceLiveReadOnlyPilotGateTests
{
    [Fact]
    public void PilotGateBoundaryDefaultsToNotApprovedAndBlocked()
    {
        Assert.Equal("not-approved", GreeCloudLiveReadOnlyPilotGateBoundary.PilotGateStatus);
        Assert.False(GreeCloudLiveReadOnlyPilotGateBoundary.PilotGateOpen);
        Assert.False(GreeCloudLiveReadOnlyPilotGateBoundary.LiveReadOnlyPilotAllowed);
        Assert.False(GreeCloudLiveReadOnlyPilotGateBoundary.LiveReadOnlyAdapterEnabled);
        Assert.False(GreeCloudLiveReadOnlyPilotGateBoundary.LiveControlAllowed);
        Assert.False(GreeCloudLiveReadOnlyPilotGateBoundary.MqttAllowed);
        Assert.False(GreeCloudLiveReadOnlyPilotGateBoundary.ProductionWiringAllowed);
        Assert.True(GreeCloudLiveReadOnlyPilotGateBoundary.RequiresManualApproval);
        Assert.True(GreeCloudLiveReadOnlyPilotGateBoundary.RequiresMaskedEvidence);
        Assert.True(GreeCloudLiveReadOnlyPilotGateBoundary.RequiresExternalSecretStore);
        Assert.True(GreeCloudLiveReadOnlyPilotGateBoundary.RequiresRollbackPlan);
        Assert.True(GreeCloudLiveReadOnlyPilotGateBoundary.RequiresKillSwitchPlan);
        Assert.True(GreeCloudLiveReadOnlyPilotGateBoundary.RequiresOperatorNamedDeviceScope);
    }

    [Fact]
    public void PilotGateRequirementsContainAllManualApprovalItems()
    {
        foreach (string requirement in new[]
        {
            "RepositoryCleanAndSynced",
            "AllTestsPass",
            "BridgeRemainsIsolated",
            "ControlAdapterBlocked",
            "MqttBlocked",
            "NoProductionDeploymentWiring",
            "NoSecretsInRepository",
            "CredentialsStoredOutsideRepository",
            "EvidenceMasksAccountAndDeviceIdentifiers",
            "OperatorApprovesExactAccountAndDeviceScope",
            "KillSwitchPlanDocumented",
            "RollbackPlanDocumented",
            "PilotLimitedToReadOnly",
            "ReadOnlyAdapterImplementationReviewed",
            "ManualApprovalRecorded"
        })
        {
            Assert.Contains(requirement, GreeCloudLiveReadOnlyPilotGateBoundary.RequiredRequirements);
        }
    }

    [Fact]
    public void DefaultPilotGateEvaluatorReturnsNotApprovedAndBlocksEverything()
    {
        IGreeCloudLiveReadOnlyPilotGateEvaluator evaluator = new OfflineGreeCloudLiveReadOnlyPilotGateEvaluator();

        GreeCloudLiveReadOnlyPilotGateDecision decision = evaluator.Evaluate();

        Assert.Equal("not-approved", decision.Status);
        Assert.False(decision.PilotGateOpen);
        Assert.False(decision.LiveReadOnlyPilotAllowed);
        Assert.False(decision.LiveReadOnlyAdapterEnabled);
        Assert.False(decision.LiveControlAllowed);
        Assert.False(decision.MqttAllowed);
        Assert.False(decision.ProductionWiringAllowed);
        Assert.Equal(GreeCloudLiveReadOnlyPilotGateBoundary.RequiredRequirements, decision.UnmetRequirements);
    }

    [Fact]
    public void PilotGateEvaluatorStillRequiresManualApprovalEvenWhenSomeRequirementsAreSatisfied()
    {
        IGreeCloudLiveReadOnlyPilotGateEvaluator evaluator = new OfflineGreeCloudLiveReadOnlyPilotGateEvaluator();
        var input = new GreeCloudLiveReadOnlyPilotGateEvaluation(new HashSet<string>(StringComparer.Ordinal)
        {
            GreeCloudLiveReadOnlyPilotGateRequirement.RepositoryCleanAndSynced,
            GreeCloudLiveReadOnlyPilotGateRequirement.AllTestsPass
        });

        GreeCloudLiveReadOnlyPilotGateDecision decision = evaluator.Evaluate(input);

        Assert.Equal("not-approved", decision.Status);
        Assert.False(decision.PilotGateOpen);
        Assert.False(decision.LiveReadOnlyPilotAllowed);
        Assert.DoesNotContain("RepositoryCleanAndSynced", decision.UnmetRequirements);
        Assert.DoesNotContain("AllTestsPass", decision.UnmetRequirements);
        Assert.Contains("ManualApprovalRecorded", decision.UnmetRequirements);
    }

    [Fact]
    public void PilotGateDocsExistAndKeepDefaultNotApprovedBoundary()
    {
        string gateDoc = ReadRepoFile("docs", "integrations", "gree-alice", "live-read-only-pilot-gate.md");
        string decisionTemplate = ReadRepoFile("docs", "integrations", "gree-alice", "live-read-only-pilot-decision-record-template.md");

        Assert.Contains("NOT APPROVED", gateDoc, StringComparison.Ordinal);
        Assert.Contains("not live adapter implementation", gateDoc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Control remains forbidden", gateDoc, StringComparison.Ordinal);
        Assert.Contains("MQTT CONNECT", gateDoc, StringComparison.Ordinal);
        Assert.Contains("Production runtime wiring", gateDoc, StringComparison.Ordinal);
        Assert.Contains("credentials", gateDoc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GREE-ALICE-43", gateDoc, StringComparison.Ordinal);

        Assert.Contains("Decision status: NOT APPROVED", decisionTemplate, StringComparison.Ordinal);
        Assert.Contains("masked identifiers", decisionTemplate, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Credential material is stored only outside the repository", decisionTemplate, StringComparison.Ordinal);
        Assert.Contains("Live read-only pilot: blocked", decisionTemplate, StringComparison.Ordinal);
        Assert.Contains("MQTT: blocked", decisionTemplate, StringComparison.Ordinal);
    }

    [Fact]
    public void GreeAliceBridgeSourceContainsNoLiveNetworkControlOrProductionWiringImplementation()
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
