extern alias GreeAliceBridgeApi;

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using AssistantEngineer.GreeAliceBridge.Application.GreeCloud;
using AssistantEngineer.GreeAliceBridge.Application.Pilot;
using AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud;
using AssistantEngineer.GreeAliceBridge.Contracts.Pilot;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class GreeAliceMinimalProductionPilotBoundaryTests
{
    [Fact]
    public void ProductionPilotBoundaryDefaultsToNotApprovedAndDisabled()
    {
        Assert.Equal("not-approved", GreeAliceMinimalProductionPilotBoundary.ProductionPilotStatus);
        Assert.False(GreeAliceMinimalProductionPilotBoundary.ProductionPilotApproved);
        Assert.False(GreeAliceMinimalProductionPilotBoundary.ProductionPilotEnabled);
        Assert.False(GreeAliceMinimalProductionPilotBoundary.ProductionDeploymentWiringEnabled);
        Assert.False(GreeAliceMinimalProductionPilotBoundary.LiveGreeRuntimeEnabled);
        Assert.False(GreeAliceMinimalProductionPilotBoundary.LiveReadOnlyPilotEnabled);
        Assert.False(GreeAliceMinimalProductionPilotBoundary.LiveControlEnabled);
        Assert.False(GreeAliceMinimalProductionPilotBoundary.MqttAllowed);
        Assert.False(GreeAliceMinimalProductionPilotBoundary.SecretsInRepositoryAllowed);
        Assert.True(GreeAliceMinimalProductionPilotBoundary.ReadOnlyFirstRequired);
        Assert.True(GreeAliceMinimalProductionPilotBoundary.RequiresSingleOperatorScope);
        Assert.True(GreeAliceMinimalProductionPilotBoundary.RequiresSingleAccountScope);
        Assert.True(GreeAliceMinimalProductionPilotBoundary.RequiresSingleHomeScope);
        Assert.True(GreeAliceMinimalProductionPilotBoundary.RequiresSingleDeviceOrChildUnitScope);
        Assert.True(GreeAliceMinimalProductionPilotBoundary.RequiresManualApproval);
        Assert.True(GreeAliceMinimalProductionPilotBoundary.RequiresAuditLogging);
        Assert.True(GreeAliceMinimalProductionPilotBoundary.RequiresMonitoringPlan);
        Assert.True(GreeAliceMinimalProductionPilotBoundary.RequiresKillSwitchPlan);
        Assert.True(GreeAliceMinimalProductionPilotBoundary.RequiresRollbackPlan);
    }

    [Fact]
    public void ProductionPilotModesContainAllowedPolicyValuesAndDefaultBlocked()
    {
        Assert.Equal(
            [
                "read_only_first",
                "single_device_read_only",
                "single_vrf_child_read_only",
                "single_device_control_candidate",
                "blocked"
            ],
            GreeAliceMinimalProductionPilotBoundary.PilotModes);
        Assert.Equal("blocked", GreeAliceMinimalProductionPilotBoundary.DefaultMode);
    }

    [Fact]
    public void ProductionPilotDefaultScopeIsDummyMaskedTemplateOnly()
    {
        GreeAliceMinimalProductionPilotScope scope = GreeAliceMinimalProductionPilotBoundary.DefaultScope;
        string combinedScope = string.Join(
            "|",
            scope.OperatorId,
            scope.AccountScope,
            scope.HomeScope,
            scope.DeviceScope,
            scope.VrfChildUnitScope,
            scope.Mode);
        Regex macLike = new(@"(?:[0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}", RegexOptions.Compiled);

        Assert.Equal("dummy-operator-001", scope.OperatorId);
        Assert.Equal("dummy-account-001", scope.AccountScope);
        Assert.Equal("dummy-home-001", scope.HomeScope);
        Assert.Equal("dummy-gree-ac-001", scope.DeviceScope);
        Assert.Equal("dummy-vrf-child-001", scope.VrfChildUnitScope);
        Assert.Equal("blocked", scope.Mode);
        Assert.True(scope.IsMasked);
        Assert.True(scope.IsDummyOrTemplate);
        Assert.False(macLike.IsMatch(combinedScope), "Pilot scope must not contain a MAC-like identifier.");
        Assert.DoesNotContain("credential", combinedScope, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", combinedScope, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", combinedScope, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", combinedScope, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("real-account-", combinedScope, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("real-device-", combinedScope, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReadinessEvaluatorReturnsNotApprovedTemplateScopeAndBlockedFlags()
    {
        IGreeAliceMinimalProductionPilotReadinessEvaluator evaluator = new OfflineGreeAliceMinimalProductionPilotReadinessEvaluator();

        GreeAliceMinimalProductionPilotDecision decision = evaluator.Evaluate();

        Assert.Equal("not-approved", decision.Status);
        Assert.Equal("blocked", decision.Mode);
        Assert.Equal(GreeAliceMinimalProductionPilotBoundary.DefaultScope, decision.Scope);
        Assert.False(decision.ProductionPilotApproved);
        Assert.False(decision.ProductionPilotEnabled);
        Assert.False(decision.ProductionDeploymentWiringEnabled);
        Assert.False(decision.LiveReadOnlyPilotEnabled);
        Assert.False(decision.LiveControlEnabled);
        Assert.False(decision.MqttAllowed);
        Assert.Equal(GreeAliceMinimalProductionPilotBoundary.RequiredRequirements, decision.UnmetRequirements);
    }

    [Fact]
    public void ReadinessEvaluatorKeepsPilotBlockedWithPartialManualMetadata()
    {
        IGreeAliceMinimalProductionPilotReadinessEvaluator evaluator = new OfflineGreeAliceMinimalProductionPilotReadinessEvaluator();
        var input = new GreeAliceMinimalProductionPilotEvaluation(new HashSet<string>(StringComparer.Ordinal)
        {
            GreeAliceMinimalProductionPilotRequirement.RepositoryCleanAndSynced,
            GreeAliceMinimalProductionPilotRequirement.AllTestsPass
        });

        GreeAliceMinimalProductionPilotDecision decision = evaluator.Evaluate(input);

        Assert.Equal("not-approved", decision.Status);
        Assert.False(decision.ProductionPilotEnabled);
        Assert.False(decision.ProductionDeploymentWiringEnabled);
        Assert.DoesNotContain("RepositoryCleanAndSynced", decision.UnmetRequirements);
        Assert.DoesNotContain("AllTestsPass", decision.UnmetRequirements);
        Assert.Contains("ManualApprovalRecorded", decision.UnmetRequirements);
    }

    [Fact]
    public void MinimalProductionPilotDocsExistAndDefaultToNotApproved()
    {
        string boundary = ReadRepoFile("docs", "integrations", "gree-alice", "minimal-production-pilot-boundary.md");
        string checklist = ReadRepoFile("docs", "integrations", "gree-alice", "minimal-production-pilot-checklist.md");
        string decisionTemplate = ReadRepoFile("docs", "integrations", "gree-alice", "minimal-production-pilot-decision-record-template.md");
        string combined = string.Join(Environment.NewLine, boundary, checklist, decisionTemplate);

        Assert.Contains("Minimal production pilot status: NOT APPROVED", boundary, StringComparison.Ordinal);
        Assert.Contains("Read-only-first rule", boundary, StringComparison.Ordinal);
        Assert.Contains("Control requires a separate approval package", boundary, StringComparison.Ordinal);
        Assert.Contains("MQTT: blocked", boundary, StringComparison.Ordinal);
        Assert.Contains("Production deployment wiring: disabled", boundary, StringComparison.Ordinal);
        Assert.Contains("Credentials must be stored only outside the repository", boundary, StringComparison.Ordinal);
        Assert.Contains("must be masked", boundary, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("Approval status: NOT APPROVED", checklist, StringComparison.Ordinal);
        Assert.Contains("Read-only-first accepted", checklist, StringComparison.Ordinal);
        Assert.Contains("MQTT remains blocked", checklist, StringComparison.Ordinal);

        Assert.Contains("Decision status: NOT APPROVED", decisionTemplate, StringComparison.Ordinal);
        Assert.Contains("Use masked identifiers", decisionTemplate, StringComparison.Ordinal);
        Assert.Contains("Credential material is stored only outside the repository", decisionTemplate, StringComparison.Ordinal);

        Assert.Contains("NOT APPROVED", combined, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExistingActionEndpointRemainsDryRunFailClosedAndSendsNothing()
    {
        await using WebApplicationFactory<GreeAliceBridgeApi::Program> factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/v1.0/user/devices/action",
            new YandexActionRequest(
                [new YandexActionDeviceRequestDto(
                    "dummy-gree-ac-001",
                    [new YandexActionCapabilityRequestDto("devices.capabilities.on_off", "set", "true")])]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        YandexActionResponse? actionResponse = await response.Content.ReadFromJsonAsync<YandexActionResponse>();

        Assert.NotNull(actionResponse);
        Assert.Equal("dry-run-fail-closed", actionResponse.Status);
        YandexActionDeviceResultDto result = Assert.Single(actionResponse.Devices);
        Assert.Equal("dry-run-fail-closed", result.Status);
        Assert.False(result.SentToGreeCloud);
        Assert.False(result.SentToMqtt);
        Assert.False(result.SentToDevice);
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
