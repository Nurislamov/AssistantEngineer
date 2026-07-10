extern alias GreeAliceBridgeApi;

using System;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome.Smoke;
using AssistantEngineer.GreeAliceBridge.Contracts.Pilot;
using AssistantEngineer.GreeAliceBridge.Contracts.Registry.Import;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.AccountLinking;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.ProviderReadiness;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.Smoke;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class GreeAliceLocalYandexProviderSmokeHarnessTests
{
    [Fact]
    public void SmokeHarnessDefaultsAreOfflineLocalAndNotProduction()
    {
        Assert.Equal("offline-local", GreeAliceYandexProviderSmokeHarnessBoundary.SmokeHarnessMode);
        Assert.Equal("not-production", GreeAliceYandexProviderSmokeHarnessBoundary.SmokeHarnessStatus);
        Assert.False(GreeAliceYandexProviderSmokeHarnessBoundary.RunsAgainstRealYandex);
        Assert.False(GreeAliceYandexProviderSmokeHarnessBoundary.RunsAgainstRealOAuth);
        Assert.False(GreeAliceYandexProviderSmokeHarnessBoundary.RequiresRealYandexCredentials);
        Assert.False(GreeAliceYandexProviderSmokeHarnessBoundary.RequiresRealGreeCredentials);
        Assert.False(GreeAliceYandexProviderSmokeHarnessBoundary.RunsAgainstProductionEndpoint);
        Assert.False(GreeAliceYandexProviderSmokeHarnessBoundary.RunsAgainstLiveGreeCloud);
        Assert.False(GreeAliceYandexProviderSmokeHarnessBoundary.RunsAgainstMqtt);
        Assert.False(GreeAliceYandexProviderSmokeHarnessBoundary.AllowsDeviceControl);
        Assert.False(GreeAliceYandexProviderSmokeHarnessBoundary.AllowsCommandExecution);
        Assert.False(GreeAliceYandexProviderSmokeHarnessBoundary.AllowsProductionWrites);
        Assert.True(GreeAliceYandexProviderSmokeHarnessBoundary.RequiresDummyOrTemplateData);
        Assert.True(GreeAliceYandexProviderSmokeHarnessBoundary.RequiresFailClosedActions);
        Assert.True(GreeAliceYandexProviderSmokeHarnessBoundary.RequiresUnknownUserFailClosed);
        Assert.True(GreeAliceYandexProviderSmokeHarnessBoundary.RequiresUnknownDeviceFailClosed);
    }

    [Fact]
    public void ScenarioListContainsRequiredOfflineDummyScenarios()
    {
        IReadOnlyList<GreeAliceYandexProviderSmokeScenario> scenarios = CreateHarness().GetScenarios();

        AssertScenario(scenarios, "linked-user-devices");
        AssertScenario(scenarios, "linked-user-query");
        AssertScenario(scenarios, "linked-user-action-fail-closed");
        AssertScenario(scenarios, "linked-user-unlink");
        AssertScenario(scenarios, "unknown-user-devices-fail-closed");
        AssertScenario(scenarios, "unknown-device-query-fail-closed");
        AssertScenario(scenarios, "unknown-device-action-fail-closed");
        AssertScenario(scenarios, "vrf-child-unit-exposure");
        AssertScenario(scenarios, "gateway-not-exposed");
        AssertScenario(scenarios, "account-linking-template");
        AssertScenario(scenarios, "registry-scope-template");
        AssertScenario(scenarios, "provider-readiness-not-ready");
        Assert.All(scenarios, scenario => Assert.True(scenario.IsOfflineOnly));
        Assert.All(scenarios, scenario => Assert.True(scenario.UsesDummyOrTemplateData));
    }

    [Fact]
    public void HarnessRunReturnsOfflinePassAndAllScenarios()
    {
        GreeAliceYandexProviderSmokeResult result = CreateHarness().Run();

        Assert.Equal("offline-local", result.Mode);
        Assert.Equal("offline-pass", result.Status);
        Assert.True(result.AllPassed);
        Assert.Null(result.StartedAtUtc);
        Assert.Null(result.CompletedAtUtc);
        Assert.NotEmpty(result.SafetyBoundary);
        Assert.Contains("Offline local Yandex provider smoke passed", result.Summary, StringComparison.Ordinal);
        Assert.Equal(CreateHarness().GetScenarios().Count, result.Scenarios.Count);
        Assert.All(result.Scenarios, scenario => Assert.True(scenario.Passed));
        Assert.Contains(result.Scenarios, scenario => scenario.ScenarioId == "linked-user-devices");
        Assert.Contains(result.Scenarios, scenario => scenario.ScenarioId == "provider-readiness-not-ready");
    }

    [Fact]
    public void LinkedUserDevicesSmokeValidatesScopedDevicesAndStableNames()
    {
        GreeAliceYandexProviderSmokeScenarioResult scenario = ScenarioResult("linked-user-devices");

        Assert.True(scenario.Passed);
        AssertPassedStep(scenario, "resolve-dummy-linked-user");
        AssertPassedStep(scenario, "resolve-registry-scope");
        AssertPassedStep(scenario, "devices-contain-split");
        AssertPassedStep(scenario, "devices-contain-vrf-living");
        AssertPassedStep(scenario, "devices-contain-vrf-bedroom");
        AssertPassedStep(scenario, "gateway-not-exposed");
        AssertPassedStep(scenario, "stable-yandex-ids");
    }

    [Fact]
    public void QuerySmokeValidatesKnownAndUnknownOfflineStates()
    {
        GreeAliceYandexProviderSmokeScenarioResult linked = ScenarioResult("linked-user-query");
        GreeAliceYandexProviderSmokeScenarioResult unknown = ScenarioResult("unknown-device-query-fail-closed");

        Assert.True(linked.Passed);
        AssertPassedStep(linked, "query-split");
        AssertPassedStep(linked, "query-vrf-child");
        AssertPassedStep(linked, "query-no-live-gree");
        AssertPassedStep(linked, "query-no-mqtt");
        Assert.True(unknown.Passed);
        AssertPassedStep(unknown, "query-unknown-device");
        AssertPassedStep(unknown, "unknown-device-offline");
    }

    [Fact]
    public void ActionSmokeValidatesDryRunFailClosedForKnownAndUnknownDevices()
    {
        GreeAliceYandexProviderSmokeScenarioResult linked = ScenarioResult("linked-user-action-fail-closed");
        GreeAliceYandexProviderSmokeScenarioResult unknown = ScenarioResult("unknown-device-action-fail-closed");
        GreeAliceYandexProviderSmokeScenarioResult vrf = ScenarioResult("vrf-child-unit-exposure");

        Assert.True(linked.Passed);
        AssertPassedStep(linked, "action-split-fail-closed");
        AssertPassedStep(linked, "action-vrf-child-fail-closed");
        AssertPassedStep(linked, "no-command-execution");
        Assert.True(unknown.Passed);
        AssertPassedStep(unknown, "action-unknown-device");
        AssertPassedStep(vrf, "vrf-living-action-fail-closed");
        AssertPassedStep(vrf, "vrf-bedroom-action-fail-closed");
        Assert.False(GreeAliceYandexProviderSmokeSafetyBoundary.AllowsCommandExecution);
    }

    [Fact]
    public void UnlinkSmokeValidatesOfflineTemplateResult()
    {
        GreeAliceYandexProviderSmokeScenarioResult scenario = ScenarioResult("linked-user-unlink");

        Assert.True(scenario.Passed);
        AssertPassedStep(scenario, "call-offline-unlink");
        AssertPassedStep(scenario, "unlink-template-result");
        AssertPassedStep(scenario, "unlink-revokes-scope");
        AssertPassedStep(scenario, "unlink-no-real-state");
    }

    [Fact]
    public void UnknownUserSmokeDoesNotLeakGlobalRegistry()
    {
        GreeAliceYandexProviderSmokeScenarioResult scenario = ScenarioResult("unknown-user-devices-fail-closed");

        Assert.True(scenario.Passed);
        AssertPassedStep(scenario, "resolve-unknown-user");
        AssertPassedStep(scenario, "unknown-user-empty-scope");
        AssertPassedStep(scenario, "unknown-user-no-split");
        AssertPassedStep(scenario, "unknown-user-no-vrf");
    }

    [Fact]
    public void VrfChildExposureAndGatewayHiddenSmokePass()
    {
        GreeAliceYandexProviderSmokeScenarioResult vrf = ScenarioResult("vrf-child-unit-exposure");
        GreeAliceYandexProviderSmokeScenarioResult gateway = ScenarioResult("gateway-not-exposed");

        Assert.True(vrf.Passed);
        AssertPassedStep(vrf, "vrf-living-exposed");
        AssertPassedStep(vrf, "vrf-bedroom-exposed");
        AssertPassedStep(vrf, "vrf-gateway-not-exposed");
        Assert.True(gateway.Passed);
        AssertPassedStep(gateway, "gateway-not-exposed");
    }

    [Fact]
    public void ProviderReadinessSmokeKeepsProductionBlocked()
    {
        GreeAliceYandexProviderSmokeScenarioResult scenario = ScenarioResult("provider-readiness-not-ready");

        Assert.True(scenario.Passed);
        AssertPassedStep(scenario, "provider-readiness-not-ready");
        AssertPassedStep(scenario, "provider-registration-not-approved");
        AssertPassedStep(scenario, "real-oauth-not-implemented");
        AssertPassedStep(scenario, "production-endpoint-disabled");
        AssertPassedStep(scenario, "production-deploy-disabled");
        AssertPassedStep(scenario, "live-control-disabled");
    }

    [Fact]
    public void LocalSmokeHarnessDocsExistAndStateSafetyBoundaries()
    {
        string harness = ReadRepoFile("docs", "integrations", "gree-alice", "local-yandex-provider-smoke-harness.md");
        string expectations = ReadRepoFile("docs", "integrations", "gree-alice", "local-yandex-provider-smoke-expectations.md");
        string combined = harness + Environment.NewLine + expectations;

        Assert.Contains("Harness is offline-local only", harness, StringComparison.Ordinal);
        Assert.Contains("Harness does not call real Yandex", harness, StringComparison.Ordinal);
        Assert.Contains("Harness does not implement OAuth", harness, StringComparison.Ordinal);
        Assert.Contains("Harness does not use real credentials/tokens", harness, StringComparison.Ordinal);
        Assert.Contains("Harness does not call live Gree+ Cloud", harness, StringComparison.Ordinal);
        Assert.Contains("Harness does not use MQTT", harness, StringComparison.Ordinal);
        Assert.Contains("Harness does not control devices", harness, StringComparison.Ordinal);
        Assert.Contains("Harness does not deploy anything", harness, StringComparison.Ordinal);
        Assert.Contains("Provider readiness remains NOT READY", harness, StringComparison.Ordinal);
        Assert.Contains("Production pilot remains NOT APPROVED", harness, StringComparison.Ordinal);
        Assert.Contains("Scenario Matrix", expectations, StringComparison.Ordinal);
        Assert.Contains("linked user /devices", expectations, StringComparison.Ordinal);
        Assert.Contains("unknown user /devices", expectations, StringComparison.Ordinal);
        AssertNoMacLikeValue(combined);
    }

    [Fact]
    public async Task ExistingYandexEndpointsAndBoundariesRemainOfflineStable()
    {
        Assert.Equal("not-ready", GreeAliceYandexProviderReadinessBoundary.ProviderReadinessStatus);
        Assert.Equal("offline-template", GreeAliceYandexAccountLinkingBoundary.AccountLinkingMode);
        Assert.Equal("offline-template", GreeAliceRegistryImportBoundary.ImportMode);
        Assert.False(GreeAliceMinimalProductionPilotBoundary.ProductionPilotApproved);

        await using WebApplicationFactory<GreeAliceBridgeApi::Program> factory = new();
        using HttpClient client = factory.CreateClient();

        YandexDevicesResponse? devices = await client.GetFromJsonAsync<YandexDevicesResponse>("/v1.0/user/devices");
        Assert.NotNull(devices);
        Assert.Contains(devices.Devices, device => device.Id == "dummy-gree-ac-001");
        Assert.Contains(devices.Devices, device => device.Id == "yandex-dummy-vrf-child-living-001");
        Assert.Contains(devices.Devices, device => device.Id == "yandex-dummy-vrf-child-bedroom-001");

        YandexQueryResponse? query = await (await client.PostAsJsonAsync(
            "/v1.0/user/devices/query",
            new YandexQueryRequest([new YandexDeviceRequestDto("dummy-gree-ac-001")]))).Content.ReadFromJsonAsync<YandexQueryResponse>();
        Assert.Equal("offline-fixture", Assert.Single(query!.Devices).Status);

        YandexActionResponse? action = await (await client.PostAsJsonAsync(
            "/v1.0/user/devices/action",
            new YandexActionRequest(
                [new YandexActionDeviceRequestDto(
                    "dummy-gree-ac-001",
                    [new YandexActionCapabilityRequestDto("devices.capabilities.on_off", "set", "true")])]))).Content.ReadFromJsonAsync<YandexActionResponse>();
        YandexActionDeviceResultDto result = Assert.Single(action!.Devices);
        Assert.Equal("dry-run-fail-closed", result.Status);
        Assert.False(result.SentToGreeCloud);
        Assert.False(result.SentToMqtt);
        Assert.False(result.SentToDevice);

        YandexUnlinkResponse? unlink = await (await client.PostAsync("/v1.0/user/unlink", content: null)).Content.ReadFromJsonAsync<YandexUnlinkResponse>();
        Assert.Equal("offline-no-production-data-touched", unlink!.Status);
    }

    [Fact]
    public void GreeAliceBridgeSourceHasNoLiveOAuthYandexGreeMqttControlCommandExecutionOrProductionWiring()
    {
        string source = NormalizeAllowedBoundaryTerms(ReadBridgeSource());
        string projects = ReadBridgeProjects();
        string docsRoot = Path.Combine(FindRepositoryRoot(), "docs", "integrations", "gree-alice");

        Assert.Contains("MapGet(\"/oauth/authorize", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MapPost(\"/oauth/token", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HttpClient", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".GetAsync(", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".PostAsync(", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MqttClient", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".ConnectAsync(", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".SubscribeAsync(", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".PublishAsync(", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DeviceControlService", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SendToDevice", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Process.Start", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UseProductionRuntimeWiring", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EnableProductionRuntimeWiring", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AddProductionRuntimeWiring", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("clientSecret", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("access-token", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refresh-token", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deviceKey", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("macAddress", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("real-account-", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("real-device-", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AssistantEngineer.Api.csproj", projects, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Telegram", projects, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Migration", projects, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(Directory.EnumerateFiles(docsRoot, "*.csv", SearchOption.AllDirectories));
    }

    private static OfflineGreeAliceYandexProviderSmokeHarness CreateHarness()
    {
        return new OfflineGreeAliceYandexProviderSmokeHarness();
    }

    private static GreeAliceYandexProviderSmokeScenarioResult ScenarioResult(string scenarioId)
    {
        return Assert.Single(CreateHarness().Run().Scenarios, scenario => scenario.ScenarioId == scenarioId);
    }

    private static void AssertScenario(IReadOnlyList<GreeAliceYandexProviderSmokeScenario> scenarios, string scenarioId)
    {
        GreeAliceYandexProviderSmokeScenario scenario = Assert.Single(scenarios, item => item.ScenarioId == scenarioId);

        Assert.True(scenario.IsOfflineOnly);
        Assert.True(scenario.UsesDummyOrTemplateData);
        Assert.NotEmpty(scenario.Steps);
    }

    private static void AssertPassedStep(GreeAliceYandexProviderSmokeScenarioResult scenario, string stepId)
    {
        GreeAliceYandexProviderSmokeStepResult step = Assert.Single(scenario.Steps, item => item.StepId == stepId);

        Assert.True(step.Passed, step.Message);
    }

    private static void AssertNoMacLikeValue(string value)
    {
        Regex macLike = new(@"(?:[0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}", RegexOptions.Compiled);

        Assert.False(macLike.IsMatch(value), "Value must not look like a hardware identifier: " + value);
    }

    private static string NormalizeAllowedBoundaryTerms(string value)
    {
        return value
            .Replace("RealYandexAppCredentialsAllowed", string.Empty, StringComparison.Ordinal)
            .Replace("RealYandexClientCredentialsConfigured", string.Empty, StringComparison.Ordinal)
            .Replace("RealYandexClientCredentialsAllowedInRepository", string.Empty, StringComparison.Ordinal)
            .Replace("ProductionCredentialsConfigured", string.Empty, StringComparison.Ordinal)
            .Replace("RequiresRealYandexCredentials", string.Empty, StringComparison.Ordinal)
            .Replace("RequiresRealGreeCredentials", string.Empty, StringComparison.Ordinal)
            .Replace("AllowsRealYandexCredentialsInRepository", string.Empty, StringComparison.Ordinal)
            .Replace("RealTokensIssued", string.Empty, StringComparison.Ordinal)
            .Replace("RefreshTokensIssued", string.Empty, StringComparison.Ordinal)
            .Replace("AccessTokensIssued", string.Empty, StringComparison.Ordinal)
            .Replace("TokenStorageImplemented", string.Empty, StringComparison.Ordinal)
            .Replace("TokenRevocationImplemented", string.Empty, StringComparison.Ordinal)
            .Replace("AllowsSecretsInRepository", string.Empty, StringComparison.Ordinal)
            .Replace("DeletedSecrets", string.Empty, StringComparison.Ordinal)
            .Replace("DeletedTokens", string.Empty, StringComparison.Ordinal)
            .Replace("No real Gree credentials", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("Credentials rotation plan required", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("OAuth secrets storage plan reviewed", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("No real Yandex client secret in repository", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("No secrets in repository", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("Production secrets must be stored outside repository", string.Empty, StringComparison.OrdinalIgnoreCase);
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

    private static string ReadBridgeProjects()
    {
        string root = FindRepositoryRoot();
        string bridgeRoot = Path.Combine(root, "src", "Integrations", "GreeAliceBridge");

        return string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(bridgeRoot, "*.csproj", SearchOption.AllDirectories)
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
