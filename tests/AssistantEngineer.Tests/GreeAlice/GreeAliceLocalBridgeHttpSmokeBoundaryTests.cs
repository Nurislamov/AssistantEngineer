extern alias GreeAliceBridgeApi;

using System;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome.HttpSmoke;
using AssistantEngineer.GreeAliceBridge.Contracts.Pilot;
using AssistantEngineer.GreeAliceBridge.Contracts.Registry.Import;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.AccountLinking;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.HttpSmoke;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.ProviderReadiness;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class GreeAliceLocalBridgeHttpSmokeBoundaryTests
{
    [Fact]
    public void HttpSmokeDefaultsAreLocalhostOnlyAndNotProduction()
    {
        Assert.Equal("localhost-only", GreeAliceLocalHttpSmokeBoundary.HttpSmokeMode);
        Assert.Equal("not-production", GreeAliceLocalHttpSmokeBoundary.HttpSmokeStatus);
        Assert.True(GreeAliceLocalHttpSmokeBoundary.AllowedHostLocalhost);
        Assert.True(GreeAliceLocalHttpSmokeBoundary.AllowedHostLoopback);
        Assert.False(GreeAliceLocalHttpSmokeBoundary.AllowedPublicHosts);
        Assert.False(GreeAliceLocalHttpSmokeBoundary.AllowsRealYandexCalls);
        Assert.False(GreeAliceLocalHttpSmokeBoundary.AllowsRealOAuth);
        Assert.False(GreeAliceLocalHttpSmokeBoundary.AllowsRealCredentials);
        Assert.False(GreeAliceLocalHttpSmokeBoundary.AllowsLiveGreeCloudCalls);
        Assert.False(GreeAliceLocalHttpSmokeBoundary.AllowsMqtt);
        Assert.False(GreeAliceLocalHttpSmokeBoundary.AllowsProductionEndpoint);
        Assert.False(GreeAliceLocalHttpSmokeBoundary.AllowsDeviceControl);
        Assert.False(GreeAliceLocalHttpSmokeBoundary.AllowsCommandExecution);
        Assert.True(GreeAliceLocalHttpSmokeBoundary.RequiresFailClosedActions);
        Assert.True(GreeAliceLocalHttpSmokeBoundary.RequiresDummyOrTemplateResponses);
        Assert.True(GreeAliceLocalHttpSmokeBoundary.ProviderReadinessMustRemainNotReady);
        Assert.True(GreeAliceLocalHttpSmokeBoundary.ProductionPilotMustRemainNotApproved);
    }

    [Fact]
    public void EndpointPlanContainsRequiredLocalOnlyEndpoints()
    {
        GreeAliceLocalHttpSmokeResult plan = CreatePlan();

        AssertEndpoint(plan, "GET", "/health");
        AssertEndpoint(plan, "GET", "/v1.0/user/devices");
        AssertEndpoint(plan, "POST", "/v1.0/user/devices/query");
        AssertEndpoint(plan, "POST", "/v1.0/user/devices/action");
        AssertEndpoint(plan, "POST", "/v1.0/user/unlink");
        Assert.All(plan.Endpoints, endpoint => Assert.True(endpoint.IsLocalOnly));
        Assert.All(plan.Endpoints, endpoint => Assert.True(endpoint.UsesDummyOrTemplateData));
        Assert.All(plan.Expectations, expectation => Assert.True(expectation.RequiresNoExternalCalls));
        Assert.Contains(plan.Expectations, expectation => expectation.EndpointId == "action" && expectation.RequiresFailClosedAction);
    }

    [Fact]
    public void RequestTemplatesUseOnlyDummyDataAndNoSecrets()
    {
        GreeAliceLocalHttpSmokeResult plan = CreatePlan();
        string combinedTemplates = string.Join(Environment.NewLine, plan.Requests.Select(request => request.BodyJson ?? string.Empty));

        Assert.Contains("dummy-gree-ac-001", combinedTemplates, StringComparison.Ordinal);
        Assert.Contains("yandex-dummy-vrf-child-living-001", combinedTemplates, StringComparison.Ordinal);
        Assert.Contains("unknown-device-001", combinedTemplates, StringComparison.Ordinal);
        Assert.DoesNotContain("credential", combinedTemplates, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", combinedTemplates, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", combinedTemplates, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", combinedTemplates, StringComparison.OrdinalIgnoreCase);
        AssertNoMacLikeValue(combinedTemplates);
    }

    [Fact]
    public void PowerShellScriptAcceptsHttpSmokeAndRejectsNonLocalTargets()
    {
        string script = ReadRepoFile("scripts", "integrations", "gree-alice", "run-local-yandex-provider-smoke.ps1");

        Assert.Contains("[switch]$RunHttpSmoke", script, StringComparison.Ordinal);
        Assert.Contains("[string]$LocalBaseUrl", script, StringComparison.Ordinal);
        Assert.Contains("function Test-LocalBaseUrl", script, StringComparison.Ordinal);
        Assert.Contains("LocalBaseUrl must use http only.", script, StringComparison.Ordinal);
        Assert.Contains("LocalBaseUrl host must be localhost or 127.0.0.1 only.", script, StringComparison.Ordinal);
        Assert.Contains("LocalBaseUrl must include an explicit local port.", script, StringComparison.Ordinal);
        Assert.Contains("Invoke-RestMethod", script, StringComparison.Ordinal);
        Assert.DoesNotContain("api.iot.yandex", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("grih.gree.com", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gree.com/oauth", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mqtt.connect", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mqtt.subscribe", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mqtt.publish", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("git push", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deploy.ps1", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SendToDevice", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HttpSmokeDocsExistAndStateSafetyBoundaries()
    {
        string doc = ReadRepoFile("docs", "integrations", "gree-alice", "local-bridge-http-smoke-boundary.md");
        string readme = ReadRepoFile("docs", "integrations", "gree-alice", "README.md");

        Assert.Contains("HTTP smoke is localhost-only.", doc, StringComparison.Ordinal);
        Assert.Contains("It does not call real Yandex.", doc, StringComparison.Ordinal);
        Assert.Contains("It does not implement real OAuth.", doc, StringComparison.Ordinal);
        Assert.Contains("It does not use real credentials/tokens.", doc, StringComparison.Ordinal);
        Assert.Contains("It does not call live Gree+ Cloud.", doc, StringComparison.Ordinal);
        Assert.Contains("It does not use MQTT.", doc, StringComparison.Ordinal);
        Assert.Contains("It does not control devices.", doc, StringComparison.Ordinal);
        Assert.Contains("It does not deploy production.", doc, StringComparison.Ordinal);
        Assert.Contains("Provider readiness remains NOT READY.", doc, StringComparison.Ordinal);
        Assert.Contains("Production pilot remains NOT APPROVED.", doc, StringComparison.Ordinal);
        Assert.Contains("local-bridge-http-smoke-boundary.md", readme, StringComparison.Ordinal);
        Assert.Contains("HTTP smoke is localhost-only.", readme, StringComparison.Ordinal);
        AssertNoMacLikeValue(doc);
    }

    [Fact]
    public async Task ExistingOfflineHttpEndpointsRemainCompatibleAndFailClosed()
    {
        Assert.Equal("not-ready", GreeAliceYandexProviderReadinessBoundary.ProviderReadinessStatus);
        Assert.Equal("offline-template", GreeAliceYandexAccountLinkingBoundary.AccountLinkingMode);
        Assert.Equal("offline-template", GreeAliceRegistryImportBoundary.ImportMode);
        Assert.False(GreeAliceMinimalProductionPilotBoundary.ProductionPilotApproved);

        await using WebApplicationFactory<GreeAliceBridgeApi::Program> factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage health = await client.GetAsync("/health");
        Assert.True(health.IsSuccessStatusCode);

        YandexDevicesResponse? devices = await client.GetFromJsonAsync<YandexDevicesResponse>("/v1.0/user/devices");
        Assert.NotNull(devices);
        Assert.Contains(devices.Devices, device => device.Id == "dummy-gree-ac-001");
        Assert.Contains(devices.Devices, device => device.Id == "yandex-dummy-vrf-child-living-001");
        Assert.DoesNotContain(devices.Devices, device => device.Id == "dummy-vrf-gateway-001");

        YandexQueryResponse? query = await (await client.PostAsJsonAsync(
            "/v1.0/user/devices/query",
            new YandexQueryRequest([
                new YandexDeviceRequestDto("dummy-gree-ac-001"),
                new YandexDeviceRequestDto("yandex-dummy-vrf-child-living-001"),
                new YandexDeviceRequestDto("unknown-device-001")
            ]))).Content.ReadFromJsonAsync<YandexQueryResponse>();
        Assert.Contains(query!.Devices, device => device.Status == "offline-fixture");
        Assert.Contains(query.Devices, device => device.Status == "offline-unknown");

        YandexActionResponse? action = await (await client.PostAsJsonAsync(
            "/v1.0/user/devices/action",
            new YandexActionRequest([
                new YandexActionDeviceRequestDto("dummy-gree-ac-001", []),
                new YandexActionDeviceRequestDto("yandex-dummy-vrf-child-living-001", []),
                new YandexActionDeviceRequestDto("unknown-device-001", [])
            ]))).Content.ReadFromJsonAsync<YandexActionResponse>();

        Assert.All(action!.Devices, result =>
        {
            Assert.Equal("dry-run-fail-closed", result.Status);
            Assert.False(result.SentToGreeCloud);
            Assert.False(result.SentToMqtt);
            Assert.False(result.SentToDevice);
        });

        YandexUnlinkResponse? unlink = await (await client.PostAsync("/v1.0/user/unlink", content: null)).Content.ReadFromJsonAsync<YandexUnlinkResponse>();
        Assert.Equal("offline-no-production-data-touched", unlink!.Status);
    }

    [Fact]
    public void GreeAliceBridgeSourceRemainsFreeOfLiveNetworkControlAndProductionWiring()
    {
        string source = NormalizeAllowedBoundaryTerms(ReadBridgeSource());
        string projects = ReadBridgeProjects();
        string docsRoot = Path.Combine(FindRepositoryRoot(), "docs", "integrations", "gree-alice");

        Assert.DoesNotContain("MapGet(\"/oauth", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MapPost(\"/oauth", source, StringComparison.OrdinalIgnoreCase);
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

    private static GreeAliceLocalHttpSmokeResult CreatePlan()
    {
        return new OfflineGreeAliceLocalHttpSmokePlanProvider().GetPlan();
    }

    private static void AssertEndpoint(GreeAliceLocalHttpSmokeResult plan, string method, string path)
    {
        Assert.Contains(plan.Endpoints, endpoint =>
            string.Equals(endpoint.Method, method, StringComparison.Ordinal) &&
            string.Equals(endpoint.Path, path, StringComparison.Ordinal));
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
            .Replace("AllowsRealCredentials", string.Empty, StringComparison.Ordinal)
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
