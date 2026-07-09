extern alias GreeAliceBridgeApi;

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using AssistantEngineer.GreeAliceBridge.Application.GreeCloud;
using AssistantEngineer.GreeAliceBridge.Application.GreeCloud.ControlPilot;
using AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud;
using AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.ControlPilot;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class GreeAliceSingleDeviceControlPilotSkeletonTests
{
    [Fact]
    public void PilotBoundaryDefaultsToNotApprovedDryRunAndFailClosed()
    {
        Assert.Equal("not-approved", GreeCloudSingleDeviceControlPilotBoundary.PilotStatus);
        Assert.True(GreeCloudSingleDeviceControlPilotBoundary.SingleDevicePilotImplemented);
        Assert.False(GreeCloudSingleDeviceControlPilotBoundary.SingleDevicePilotApproved);
        Assert.False(GreeCloudSingleDeviceControlPilotBoundary.LiveControlEnabled);
        Assert.True(GreeCloudSingleDeviceControlPilotBoundary.DryRunOnly);
        Assert.True(GreeCloudSingleDeviceControlPilotBoundary.FailClosed);
        Assert.False(GreeCloudSingleDeviceControlPilotBoundary.CommandSendingEnabled);
        Assert.False(GreeCloudSingleDeviceControlPilotBoundary.MqttAllowed);
        Assert.False(GreeCloudSingleDeviceControlPilotBoundary.ProductionWiringAllowed);
        Assert.True(GreeCloudSingleDeviceControlPilotBoundary.RequiresExactSingleDeviceScope);
        Assert.True(GreeCloudSingleDeviceControlPilotBoundary.RequiresManualApproval);
        Assert.True(GreeCloudSingleDeviceControlPilotBoundary.RequiresAuditEvent);
        Assert.True(GreeCloudSingleDeviceControlPilotBoundary.RequiresKillSwitch);
        Assert.True(GreeCloudSingleDeviceControlPilotBoundary.RequiresRollbackPlan);
    }

    [Fact]
    public void PilotBoundaryUsesDummyOnlyScope()
    {
        GreeCloudSingleDeviceControlPilotScope scope = GreeCloudSingleDeviceControlPilotBoundary.DummyScope;
        string combinedScope = string.Join("|", scope.PilotAccountId, scope.PilotDeviceId, scope.PilotDeviceKind, scope.PilotScopeKind);
        Regex macLike = new(@"(?:[0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}", RegexOptions.Compiled);

        Assert.Equal("dummy-account-001", scope.PilotAccountId);
        Assert.Equal("dummy-gree-ac-001", scope.PilotDeviceId);
        Assert.Equal("split-ac", scope.PilotDeviceKind);
        Assert.Equal("single-device-offline-fixture", scope.PilotScopeKind);
        Assert.False(macLike.IsMatch(combinedScope), "Pilot scope must not contain a MAC-like identifier.");
        Assert.DoesNotContain("credential", combinedScope, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", combinedScope, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", combinedScope, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", combinedScope, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("real-account-", combinedScope, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("real-device-", combinedScope, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CandidateCommandPlanContainsOnlyDryRunFailClosedCommands()
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
            GreeCloudSingleDeviceControlPilotBoundary.CandidateCommandPlan.Select(command => command.Name));

        foreach (GreeCloudSingleDeviceControlPilotCommand command in GreeCloudSingleDeviceControlPilotBoundary.CandidateCommandPlan)
        {
            Assert.False(command.IsApproved);
            Assert.True(command.DryRunOnly);
            Assert.False(command.WillSendToGreeCloud);
            Assert.False(command.WillSendToMqtt);
            Assert.False(command.WillSendDeviceCommand);
            Assert.True(command.RequiresManualApproval);
            Assert.True(command.RequiresAuditEvent);
            Assert.True(command.RequiresKillSwitchClear);
        }
    }

    [Fact]
    public void PilotSafetyLimitsRemainConservative()
    {
        Assert.Equal(18, GreeCloudSingleDeviceControlPilotBoundary.MinTargetTemperatureC);
        Assert.Equal(30, GreeCloudSingleDeviceControlPilotBoundary.MaxTargetTemperatureC);
        Assert.Equal(["auto", "cool", "heat", "dry", "fan"], GreeCloudSingleDeviceControlPilotBoundary.AllowedModesCandidate);
        Assert.Equal(["auto", "low", "medium", "high"], GreeCloudSingleDeviceControlPilotBoundary.AllowedFanSpeedsCandidate);
        Assert.True(GreeCloudSingleDeviceControlPilotBoundary.RateLimitRequired);
        Assert.True(GreeCloudSingleDeviceControlPilotBoundary.SingleDeviceScopeRequired);
        Assert.True(GreeCloudSingleDeviceControlPilotBoundary.AuditEventRequired);
        Assert.True(GreeCloudSingleDeviceControlPilotBoundary.KillSwitchRequired);
        Assert.True(GreeCloudSingleDeviceControlPilotBoundary.RollbackRequired);
    }

    [Fact]
    public void OfflinePlannerReturnsNotApprovedDummyOnlyDryRunDecision()
    {
        IGreeCloudSingleDeviceControlPilotPlanner planner = new OfflineGreeCloudSingleDeviceControlPilotPlanner();

        GreeCloudSingleDeviceControlPilotDecision decision = planner.Plan();

        Assert.Equal("not-approved", decision.PilotStatus);
        Assert.Equal(GreeCloudSingleDeviceControlPilotBoundary.DummyScope, decision.Scope);
        Assert.Equal(decision.Scope, decision.CommandPlan.Scope);
        Assert.Equal(GreeCloudSingleDeviceControlPilotBoundary.CandidateCommandPlan, decision.CommandPlan.CandidateCommands);
        Assert.True(decision.DryRunResult.DryRunOnly);
        Assert.True(decision.DryRunResult.FailClosed);
        Assert.False(decision.DryRunResult.WillSendToGreeCloud);
        Assert.False(decision.DryRunResult.WillSendToMqtt);
        Assert.False(decision.DryRunResult.WillSendDeviceCommand);
        Assert.False(decision.LiveControlEnabled);
        Assert.False(decision.MqttAllowed);
        Assert.False(decision.ProductionWiringAllowed);
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
    public void SingleDeviceControlPilotSkeletonDocExistsAndKeepsBlockedBoundary()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "single-device-control-pilot-skeleton.md");

        Assert.Contains("Pilot status: NOT APPROVED", text, StringComparison.Ordinal);
        Assert.Contains("Live control: disabled", text, StringComparison.Ordinal);
        Assert.Contains("Command sending: disabled", text, StringComparison.Ordinal);
        Assert.Contains("Control adapter: fail-closed", text, StringComparison.Ordinal);
        Assert.Contains("MQTT: blocked", text, StringComparison.Ordinal);
        Assert.Contains("Production wiring: blocked", text, StringComparison.Ordinal);
        Assert.Contains("No real account id", text, StringComparison.Ordinal);
        Assert.Contains("must use masked account/device labels", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No Gree+ command is sent.", text, StringComparison.Ordinal);
        Assert.Contains("No MQTT is used.", text, StringComparison.Ordinal);
        Assert.Contains("No device receives a command.", text, StringComparison.Ordinal);
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
