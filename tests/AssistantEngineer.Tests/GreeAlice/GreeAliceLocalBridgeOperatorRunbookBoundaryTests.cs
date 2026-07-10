extern alias GreeAliceBridgeApi;

using System;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using AssistantEngineer.GreeAliceBridge.Contracts.Pilot;
using AssistantEngineer.GreeAliceBridge.Contracts.Registry.Import;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.AccountLinking;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.ProviderReadiness;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class GreeAliceLocalBridgeOperatorRunbookBoundaryTests
{
    [Fact]
    public void LocalBridgeOperatorDocumentsExist()
    {
        AssertRepoFileExists("docs", "integrations", "gree-alice", "local-bridge-operator-runbook.md");
        AssertRepoFileExists("docs", "integrations", "gree-alice", "local-bridge-operator-smoke-checklist.md");
        AssertRepoFileExists("docs", "integrations", "gree-alice", "local-bridge-smoke-evidence-template.md");
        AssertRepoFileExists("docs", "integrations", "gree-alice", "local-bridge-forbidden-commands.md");
    }

    [Fact]
    public void RunbookStatesOfflineLocalSafetyLanguage()
    {
        string runbook = ReadRepoFile("docs", "integrations", "gree-alice", "local-bridge-operator-runbook.md");

        Assert.Contains("Runbook is offline/local only.", runbook, StringComparison.Ordinal);
        Assert.Contains("It does not call real Yandex.", runbook, StringComparison.Ordinal);
        Assert.Contains("It does not implement real OAuth.", runbook, StringComparison.Ordinal);
        Assert.Contains("It does not use real credentials/tokens.", runbook, StringComparison.Ordinal);
        Assert.Contains("It does not call live Gree+ Cloud.", runbook, StringComparison.Ordinal);
        Assert.Contains("It does not use MQTT.", runbook, StringComparison.Ordinal);
        Assert.Contains("It does not control devices.", runbook, StringComparison.Ordinal);
        Assert.Contains("It does not deploy to production.", runbook, StringComparison.Ordinal);
        Assert.Contains("Provider readiness remains NOT READY.", runbook, StringComparison.Ordinal);
        Assert.Contains("Production pilot remains NOT APPROVED.", runbook, StringComparison.Ordinal);
        Assert.Contains("/action returns dry-run fail-closed", runbook, StringComparison.Ordinal);
        AssertNoMacLikeValue(runbook);
    }

    [Fact]
    public void ChecklistDefaultsToNotApprovedAndRequiresSafetyChecks()
    {
        string checklist = ReadRepoFile("docs", "integrations", "gree-alice", "local-bridge-operator-smoke-checklist.md");

        Assert.Contains("Operator smoke status: NOT APPROVED", checklist, StringComparison.Ordinal);
        Assert.Contains("dotnet restore PASS", checklist, StringComparison.Ordinal);
        Assert.Contains("dotnet build PASS", checklist, StringComparison.Ordinal);
        Assert.Contains("dotnet test PASS", checklist, StringComparison.Ordinal);
        Assert.Contains("Static safety scans PASS", checklist, StringComparison.Ordinal);
        Assert.Contains("Local smoke harness PASS", checklist, StringComparison.Ordinal);
        Assert.Contains("No real Yandex calls performed", checklist, StringComparison.Ordinal);
        Assert.Contains("No real OAuth used", checklist, StringComparison.Ordinal);
        Assert.Contains("No live Gree+ Cloud calls performed", checklist, StringComparison.Ordinal);
        Assert.Contains("No MQTT performed", checklist, StringComparison.Ordinal);
        Assert.Contains("No device control performed", checklist, StringComparison.Ordinal);
        Assert.Contains("No production deployment performed", checklist, StringComparison.Ordinal);
        Assert.Contains("Evidence masking checked", checklist, StringComparison.Ordinal);
    }

    [Fact]
    public void EvidenceTemplateContainsRequiredFieldsAndMaskingWarnings()
    {
        string evidence = ReadRepoFile("docs", "integrations", "gree-alice", "local-bridge-smoke-evidence-template.md");

        Assert.Contains("## Repository commit", evidence, StringComparison.Ordinal);
        Assert.Contains("## Validation result", evidence, StringComparison.Ordinal);
        Assert.Contains("## Smoke harness result", evidence, StringComparison.Ordinal);
        Assert.Contains("## Action fail-closed result", evidence, StringComparison.Ordinal);
        Assert.Contains("## Unknown user/device result", evidence, StringComparison.Ordinal);
        Assert.Contains("Do not paste real credentials.", evidence, StringComparison.Ordinal);
        Assert.Contains("Do not paste real tokens.", evidence, StringComparison.Ordinal);
        Assert.Contains("Do not paste real Yandex user IDs.", evidence, StringComparison.Ordinal);
        Assert.Contains("Do not paste real Gree account/device IDs.", evidence, StringComparison.Ordinal);
        Assert.Contains("Do not paste MAC-like identifiers.", evidence, StringComparison.Ordinal);
    }

    [Fact]
    public void ForbiddenCommandsDocumentBlocksLiveOperations()
    {
        string forbidden = ReadRepoFile("docs", "integrations", "gree-alice", "local-bridge-forbidden-commands.md");

        Assert.Contains("real Yandex production endpoints", forbidden, StringComparison.Ordinal);
        Assert.Contains("real Gree+ Cloud endpoints", forbidden, StringComparison.Ordinal);
        Assert.Contains("MQTT CONNECT", forbidden, StringComparison.Ordinal);
        Assert.Contains("MQTT SUBSCRIBE", forbidden, StringComparison.Ordinal);
        Assert.Contains("MQTT PUBLISH", forbidden, StringComparison.Ordinal);
        Assert.Contains("Running production deployment scripts", forbidden, StringComparison.Ordinal);
        Assert.Contains("Adding OAuth client secrets", forbidden, StringComparison.Ordinal);
        Assert.Contains("Adding access/refresh tokens", forbidden, StringComparison.Ordinal);
        Assert.Contains("Running commands that send device control", forbidden, StringComparison.Ordinal);
    }

    [Fact]
    public void PowerShellSmokeScriptExistsAndIsSafeByDefault()
    {
        string script = ReadRepoFile("scripts", "integrations", "gree-alice", "run-local-yandex-provider-smoke.ps1");

        Assert.Contains("[string]$RepoRoot", script, StringComparison.Ordinal);
        Assert.Contains("AssistantEngineer.sln", script, StringComparison.Ordinal);
        Assert.Contains("Safety boundary: offline/local only", script, StringComparison.Ordinal);
        Assert.Contains("dotnet", script, StringComparison.Ordinal);
        Assert.Contains("restore", script, StringComparison.Ordinal);
        Assert.Contains("build", script, StringComparison.Ordinal);
        Assert.Contains("test", script, StringComparison.Ordinal);
        Assert.Contains("git diff", script, StringComparison.Ordinal);
        Assert.Contains("Running static safety scans", script, StringComparison.Ordinal);
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
    public async Task ExistingSmokeHarnessAndBridgeContractsRemainOfflineCompatible()
    {
        Assert.Equal("not-ready", GreeAliceYandexProviderReadinessBoundary.ProviderReadinessStatus);
        Assert.Equal("offline-template", GreeAliceYandexAccountLinkingBoundary.AccountLinkingMode);
        Assert.Equal("offline-template", GreeAliceRegistryImportBoundary.ImportMode);
        Assert.False(GreeAliceMinimalProductionPilotBoundary.ProductionPilotApproved);

        await using WebApplicationFactory<GreeAliceBridgeApi::Program> factory = new();
        using HttpClient client = factory.CreateClient();

        YandexDevicesResponse? devices = await client.GetFromJsonAsync<YandexDevicesResponse>("/v1.0/user/devices");
        Assert.NotNull(devices);
        Assert.Contains(devices.Devices, device => device.Id == "yandex-dummy-vrf-child-living-001");
        Assert.Contains(devices.Devices, device => device.Id == "yandex-dummy-vrf-child-bedroom-001");
        Assert.DoesNotContain(devices.Devices, device => device.Id.Contains("gateway", StringComparison.OrdinalIgnoreCase));

        YandexQueryResponse? query = await (await client.PostAsJsonAsync(
            "/v1.0/user/devices/query",
            new YandexQueryRequest([new YandexDeviceRequestDto("unknown-dummy-device")]))).Content.ReadFromJsonAsync<YandexQueryResponse>();
        Assert.Equal("offline-unknown", Assert.Single(query!.Devices).Status);

        YandexActionResponse? action = await (await client.PostAsJsonAsync(
            "/v1.0/user/devices/action",
            new YandexActionRequest(
                [new YandexActionDeviceRequestDto(
                    "unknown-dummy-device",
                    [new YandexActionCapabilityRequestDto("devices.capabilities.on_off", "set", "true")])]))).Content.ReadFromJsonAsync<YandexActionResponse>();
        YandexActionDeviceResultDto result = Assert.Single(action!.Devices);
        Assert.Equal("dry-run-fail-closed", result.Status);
        Assert.False(result.SentToGreeCloud);
        Assert.False(result.SentToMqtt);
        Assert.False(result.SentToDevice);
    }

    [Fact]
    public void GreeAliceBridgeSourceRemainsFreeOfLiveEndpointsControlCommandExecutionAndProductionWiring()
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

    [Fact]
    public void ReadmeIndexesLocalRunbookAndSmokeScriptBoundary()
    {
        string readme = ReadRepoFile("docs", "integrations", "gree-alice", "README.md");

        Assert.Contains("local-bridge-operator-runbook.md", readme, StringComparison.Ordinal);
        Assert.Contains("local-bridge-operator-smoke-checklist.md", readme, StringComparison.Ordinal);
        Assert.Contains("local-bridge-smoke-evidence-template.md", readme, StringComparison.Ordinal);
        Assert.Contains("local-bridge-forbidden-commands.md", readme, StringComparison.Ordinal);
        Assert.Contains("scripts/integrations/gree-alice/run-local-yandex-provider-smoke.ps1", readme, StringComparison.Ordinal);
        Assert.Contains("Local operator runbook exists.", readme, StringComparison.Ordinal);
        Assert.Contains("Local smoke script boundary exists.", readme, StringComparison.Ordinal);
        Assert.Contains("Script is offline/local only.", readme, StringComparison.Ordinal);
        Assert.Contains("Script does not call real Yandex.", readme, StringComparison.Ordinal);
        Assert.Contains("Script does not implement OAuth.", readme, StringComparison.Ordinal);
        Assert.Contains("Script does not use real credentials/tokens.", readme, StringComparison.Ordinal);
        Assert.Contains("Script does not call live Gree+ Cloud.", readme, StringComparison.Ordinal);
        Assert.Contains("Script does not use MQTT.", readme, StringComparison.Ordinal);
        Assert.Contains("Script does not control devices.", readme, StringComparison.Ordinal);
        Assert.Contains("Script does not deploy production.", readme, StringComparison.Ordinal);
        Assert.Contains("Provider readiness: NOT READY by default", readme, StringComparison.Ordinal);
        Assert.Contains("Minimal production pilot boundary: exists, NOT APPROVED by default", readme, StringComparison.Ordinal);
    }

    private static void AssertRepoFileExists(params string[] relativeParts)
    {
        string root = FindRepositoryRoot();
        string path = Path.Combine(new[] { root }.Concat(relativeParts).ToArray());

        Assert.True(File.Exists(path), "Expected repository file to exist: " + path);
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
