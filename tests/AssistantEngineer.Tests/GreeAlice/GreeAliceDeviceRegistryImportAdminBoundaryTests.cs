extern alias GreeAliceBridgeApi;

using System;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using AssistantEngineer.GreeAliceBridge.Application;
using AssistantEngineer.GreeAliceBridge.Application.Registry.Import;
using AssistantEngineer.GreeAliceBridge.Contracts;
using AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.ControlPilot;
using AssistantEngineer.GreeAliceBridge.Contracts.Pilot;
using AssistantEngineer.GreeAliceBridge.Contracts.Registry;
using AssistantEngineer.GreeAliceBridge.Contracts.Registry.Import;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class GreeAliceDeviceRegistryImportAdminBoundaryTests
{
    [Fact]
    public void ImportBoundaryDefaultsAreOfflineTemplateAndFailClosed()
    {
        Assert.Equal("offline-template", GreeAliceRegistryImportBoundary.ImportMode);
        Assert.False(GreeAliceRegistryImportBoundary.RealImportEnabled);
        Assert.False(GreeAliceRegistryImportBoundary.AdminUiImplemented);
        Assert.False(GreeAliceRegistryImportBoundary.LiveGreeCloudDiscoveryEnabled);
        Assert.False(GreeAliceRegistryImportBoundary.AutoExposeDiscoveredDevices);
        Assert.True(GreeAliceRegistryImportBoundary.RequiresManualReview);
        Assert.True(GreeAliceRegistryImportBoundary.RequiresStableYandexDeviceId);
        Assert.True(GreeAliceRegistryImportBoundary.RequiresRoomBinding);
        Assert.True(GreeAliceRegistryImportBoundary.RequiresGatewayChildMappingForVrf);
        Assert.False(GreeAliceRegistryImportBoundary.AllowsSecretsInImport);
        Assert.False(GreeAliceRegistryImportBoundary.AllowsMacLikeIdentifiersInRepo);
        Assert.False(GreeAliceRegistryImportBoundary.AllowsRealAccountIdentifiersInRepo);
        Assert.False(GreeAliceRegistryImportBoundary.AllowsProductionWrite);
        Assert.False(GreeAliceRegistryImportBoundary.AllowsDeviceControl);
        Assert.False(GreeAliceRegistryImportBoundary.AllowsMqtt);
        Assert.False(GreeAliceRegistryImportBoundary.ProductionWiringAllowed);
    }

    [Fact]
    public void TemplateProviderReturnsDummyOnlyDraft()
    {
        GreeAliceRegistryImportDraft draft = CreateTemplate();
        string combined = CombineDraftText(draft);

        Assert.Equal("offline-template", draft.ImportMode);
        Assert.Equal("dummy-import-account-001", draft.Account.ImportAccountId);
        Assert.Equal("dummy-import-home-001", draft.Home.ImportHomeId);
        Assert.Contains(draft.Rooms, room => room.ImportRoomId == "dummy-import-room-living-001");
        Assert.Contains(draft.Rooms, room => room.ImportRoomId == "dummy-import-room-bedroom-001");
        Assert.Contains(draft.Devices, device => device.ImportDeviceId == "dummy-import-split-ac-001");
        Assert.Contains(draft.VrfGateways, gateway => gateway.ImportGatewayId == "dummy-import-vrf-gateway-001");
        Assert.Contains(draft.VrfChildUnits, child => child.ImportChildUnitId == "dummy-import-vrf-child-living-001");
        Assert.Contains(draft.VrfChildUnits, child => child.ImportChildUnitId == "dummy-import-vrf-child-bedroom-001");
        Assert.All(CollectDraftIds(draft), id => Assert.True(IsDummyOrTemplateValue(id), "Expected dummy/template value: " + id));
        AssertNoMacLikeOrSensitiveMaterial(combined);
    }

    [Fact]
    public void ValidatorAcceptsValidDummyTemplateDraft()
    {
        GreeAliceRegistryImportDraft draft = CreateTemplate();
        GreeAliceRegistryImportValidationResult result = Validate(draft);

        Assert.True(result.IsAccepted);
        Assert.Empty(result.Issues);
        Assert.All(draft.Devices.Where(device => device.ExposeToYandex), device =>
        {
            Assert.False(string.IsNullOrWhiteSpace(device.StableYandexDeviceId));
            Assert.False(string.IsNullOrWhiteSpace(device.RoomId));
        });
        Assert.All(draft.VrfChildUnits.Where(child => child.ExposeToYandex), child =>
        {
            Assert.False(string.IsNullOrWhiteSpace(child.StableYandexDeviceId));
            Assert.False(string.IsNullOrWhiteSpace(child.RoomId));
            Assert.Contains(draft.VrfGateways, gateway => gateway.ImportGatewayId == child.ParentGatewayId);
        });
        Assert.All(draft.VrfGateways, gateway => Assert.False(gateway.ExposeToYandex));
    }

    [Theory]
    [InlineData("stableYandexDeviceId.required")]
    [InlineData("room.required")]
    [InlineData("vrfChild.unknownGateway")]
    [InlineData("stableYandexDeviceId.duplicate")]
    [InlineData("import.id")]
    [InlineData("id.macLike")]
    [InlineData("value.sensitive")]
    [InlineData("id.realLike")]
    [InlineData("gateway.exposed")]
    public void ValidatorReturnsControlledIssuesForUnsafeDrafts(string expectedCode)
    {
        GreeAliceRegistryImportDraft draft = expectedCode switch
        {
            "stableYandexDeviceId.required" => WithFirstDevice(CreateTemplate(), device => device with { StableYandexDeviceId = null }),
            "room.required" => WithFirstDevice(CreateTemplate(), device => device with { RoomId = null }),
            "vrfChild.unknownGateway" => WithFirstChild(CreateTemplate(), child => child with { ParentGatewayId = "dummy-import-vrf-gateway-missing" }),
            "stableYandexDeviceId.duplicate" => WithFirstChild(CreateTemplate(), child => child with { StableYandexDeviceId = "yandex-dummy-import-split-ac-001" }),
            "import.id" => WithFirstChild(CreateTemplate(), child => child with { ImportChildUnitId = "dummy-import-split-ac-001" }),
            "id.macLike" => WithFirstDevice(CreateTemplate(), device => device with { ImportDeviceId = "dummy-import-aa:bb:cc:dd:ee:ff" }),
            "value.sensitive" => WithFirstDevice(CreateTemplate(), device => device with { DisplayName = "dummy password marker" }),
            "id.realLike" => WithAccount(CreateTemplate(), account => account with { ImportAccountId = "real-account-001" }),
            "gateway.exposed" => WithFirstGateway(CreateTemplate(), gateway => gateway with { ExposeToYandex = true }),
            _ => throw new InvalidOperationException("Unexpected validation case: " + expectedCode)
        };

        GreeAliceRegistryImportValidationResult result = Validate(draft);

        Assert.False(result.IsAccepted);
        Assert.Contains(result.Issues, issue => issue.Code == expectedCode);
    }

    [Fact]
    public void ValidatorRejectsUnknownRoomBindingAndRealLookingStableId()
    {
        GreeAliceRegistryImportDraft unknownRoomDraft = WithFirstDevice(
            CreateTemplate(),
            device => device with { RoomId = "dummy-import-room-missing-001" });
        GreeAliceRegistryImportDraft realLikeStableIdDraft = WithFirstDevice(
            CreateTemplate(),
            device => device with { StableYandexDeviceId = "real-device-001" });

        Assert.Contains(Validate(unknownRoomDraft).Issues, issue => issue.Code == "room.unknown");
        Assert.Contains(Validate(realLikeStableIdDraft).Issues, issue => issue.Code == "stableYandexDeviceId.realLike");
    }

    [Fact]
    public void ExposurePolicyRequiresExplicitValidExposure()
    {
        GreeAliceRegistryImportDraft draft = CreateTemplate();
        GreeAliceRegistryImportDeviceDraft split = Assert.Single(draft.Devices);
        GreeAliceRegistryImportVrfGatewayDraft gateway = Assert.Single(draft.VrfGateways);
        GreeAliceRegistryImportVrfChildUnitDraft child = Assert.Single(
            draft.VrfChildUnits,
            unit => unit.ImportChildUnitId == "dummy-import-vrf-child-living-001");

        Assert.False(GreeAliceRegistryImportExposurePolicy.CanAutoExposeDiscoveredDevice);
        Assert.True(GreeAliceRegistryImportExposurePolicy.CanExposeDevice(split));
        Assert.False(GreeAliceRegistryImportExposurePolicy.CanExposeDevice(split with { ExposeToYandex = false }));
        Assert.False(GreeAliceRegistryImportExposurePolicy.CanExposeVrfGateway(gateway));
        Assert.True(GreeAliceRegistryImportExposurePolicy.CanExposeVrfChildUnit(child));
        Assert.False(GreeAliceRegistryImportExposurePolicy.CanExposeVrfChildUnit(child with { StableYandexDeviceId = null }));
        Assert.False(GreeAliceRegistryImportExposurePolicy.CanExposeUnknownOrInternalDevice(explicitExposure: true, isInternal: true));
    }

    [Fact]
    public async Task ExistingYandexDevicesQueryAndActionRemainOfflineAndStable()
    {
        await using WebApplicationFactory<GreeAliceBridgeApi::Program> factory = new();
        using HttpClient client = factory.CreateClient();

        YandexDevicesResponse? devices = await client.GetFromJsonAsync<YandexDevicesResponse>("/v1.0/user/devices");

        Assert.NotNull(devices);
        Assert.Contains(devices.Devices, device => device.Id == "dummy-gree-ac-001");
        Assert.Contains(devices.Devices, device => device.Id == "yandex-dummy-vrf-child-living-001");
        Assert.Contains(devices.Devices, device => device.Id == "yandex-dummy-vrf-child-bedroom-001");

        HttpResponseMessage queryResponse = await client.PostAsJsonAsync(
            "/v1.0/user/devices/query",
            new YandexQueryRequest([new YandexDeviceRequestDto("dummy-gree-ac-001")]));
        YandexQueryResponse? queryBody = await queryResponse.Content.ReadFromJsonAsync<YandexQueryResponse>();

        Assert.NotNull(queryBody);
        Assert.Equal("offline-fixture", Assert.Single(queryBody.Devices).Status);

        HttpResponseMessage actionResponse = await client.PostAsJsonAsync(
            "/v1.0/user/devices/action",
            new YandexActionRequest(
                [new YandexActionDeviceRequestDto(
                    "dummy-gree-ac-001",
                    [new YandexActionCapabilityRequestDto("devices.capabilities.on_off", "set", "true")])]));
        YandexActionResponse? actionBody = await actionResponse.Content.ReadFromJsonAsync<YandexActionResponse>();
        YandexActionDeviceResultDto result = Assert.Single(actionBody!.Devices);

        Assert.Equal("dry-run-fail-closed", result.Status);
        Assert.False(result.SentToGreeCloud);
        Assert.False(result.SentToMqtt);
        Assert.False(result.SentToDevice);
    }

    [Fact]
    public void PilotAndControlBoundariesRemainNotApproved()
    {
        Assert.False(GreeAliceMinimalProductionPilotBoundary.ProductionPilotApproved);
        Assert.Equal("not-approved", GreeCloudSingleDeviceControlPilotBoundary.PilotStatus);
        Assert.False(GreeAliceRegistryImportSafetyBoundary.AllowsDeviceControl);
        Assert.False(GreeAliceRegistryImportSafetyBoundary.AllowsMqtt);
        Assert.False(GreeAliceRegistryImportSafetyBoundary.ProductionWiringAllowed);
    }

    [Fact]
    public void ImportBoundaryDocsExistAndExplainReviewDrivenRegistryExposure()
    {
        string boundary = ReadRepoFile("docs", "integrations", "gree-alice", "device-registry-import-admin-boundary.md");
        string template = ReadRepoFile("docs", "integrations", "gree-alice", "device-registry-import-template.md");
        string combined = boundary + Environment.NewLine + template;

        Assert.Contains("Yandex devices come from our bridge registry", boundary, StringComparison.Ordinal);
        Assert.Contains("Gree Cloud discovery must not auto-expose devices", boundary, StringComparison.Ordinal);
        Assert.Contains("Manual review is required", boundary, StringComparison.Ordinal);
        Assert.Contains("Stable Yandex IDs are required", boundary, StringComparison.Ordinal);
        Assert.Contains("Room binding is required", boundary, StringComparison.Ordinal);
        Assert.Contains("No real credentials/secrets/account IDs/device IDs in repo", boundary, StringComparison.Ordinal);
        Assert.Contains("live Gree+ Cloud integration", boundary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MQTT", boundary, StringComparison.Ordinal);
        Assert.Contains("device control", boundary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("production wiring", boundary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Account Section Fields", template, StringComparison.Ordinal);
        Assert.Contains("VRF Child Unit Section Fields", template, StringComparison.Ordinal);
        AssertNoMacLikeValue(combined);
        Assert.DoesNotContain("access-token", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refresh-token", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("clientSecret", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deviceKey", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("real-account-", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("real-device-", combined, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GreeAliceBridgeSourceHasNoLiveNetworkControlProductionWiringOrDataArtifacts()
    {
        string source = ReadBridgeSource()
            .Replace("AllowsSecretsInImport", string.Empty, StringComparison.Ordinal)
            .Replace("GreeAliceRegistryImportSafetyBoundary", string.Empty, StringComparison.Ordinal);
        string bridgeProjects = ReadBridgeProjects();
        string root = FindRepositoryRoot();
        string docsRoot = Path.Combine(root, "docs", "integrations", "gree-alice");

        Assert.DoesNotContain("HttpClient", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".GetAsync(", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".PostAsync(", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MqttClient", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".ConnectAsync(", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".SubscribeAsync(", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".PublishAsync(", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DeviceControlService", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SendToDevice", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UseProductionRuntimeWiring", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EnableProductionRuntimeWiring", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AddProductionRuntimeWiring", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConnectionString", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("clientSecret", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("access-token", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refresh-token", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deviceKey", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("macAddress", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("real-account-", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("real-device-", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AssistantEngineer.Api.csproj", bridgeProjects, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Telegram", bridgeProjects, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Migration", bridgeProjects, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(Directory.EnumerateFiles(docsRoot, "*.csv", SearchOption.AllDirectories));
    }

    private static GreeAliceRegistryImportDraft CreateTemplate()
    {
        return new OfflineGreeAliceRegistryImportTemplateProvider().GetTemplateDraft();
    }

    private static GreeAliceRegistryImportValidationResult Validate(GreeAliceRegistryImportDraft draft)
    {
        return new OfflineGreeAliceRegistryImportValidator().Validate(draft);
    }

    private static GreeAliceRegistryImportDraft WithAccount(
        GreeAliceRegistryImportDraft draft,
        Func<GreeAliceRegistryImportAccountDraft, GreeAliceRegistryImportAccountDraft> update)
    {
        return draft with { Account = update(draft.Account) };
    }

    private static GreeAliceRegistryImportDraft WithFirstDevice(
        GreeAliceRegistryImportDraft draft,
        Func<GreeAliceRegistryImportDeviceDraft, GreeAliceRegistryImportDeviceDraft> update)
    {
        return draft with { Devices = draft.Devices.Select((device, index) => index == 0 ? update(device) : device).ToArray() };
    }

    private static GreeAliceRegistryImportDraft WithFirstGateway(
        GreeAliceRegistryImportDraft draft,
        Func<GreeAliceRegistryImportVrfGatewayDraft, GreeAliceRegistryImportVrfGatewayDraft> update)
    {
        return draft with { VrfGateways = draft.VrfGateways.Select((gateway, index) => index == 0 ? update(gateway) : gateway).ToArray() };
    }

    private static GreeAliceRegistryImportDraft WithFirstChild(
        GreeAliceRegistryImportDraft draft,
        Func<GreeAliceRegistryImportVrfChildUnitDraft, GreeAliceRegistryImportVrfChildUnitDraft> update)
    {
        return draft with { VrfChildUnits = draft.VrfChildUnits.Select((child, index) => index == 0 ? update(child) : child).ToArray() };
    }

    private static string CombineDraftText(GreeAliceRegistryImportDraft draft)
    {
        return string.Join("|", CollectDraftIds(draft).Concat(
        [
            draft.Account.DisplayName,
            draft.Home.DisplayName,
            .. draft.Rooms.Select(room => room.DisplayName),
            .. draft.Devices.Select(device => device.DisplayName),
            .. draft.VrfGateways.Select(gateway => gateway.DisplayName),
            .. draft.VrfGateways.Select(gateway => gateway.SystemName),
            .. draft.VrfChildUnits.Select(child => child.DisplayName)
        ]));
    }

    private static string[] CollectDraftIds(GreeAliceRegistryImportDraft draft)
    {
        return
        [
            draft.Account.ImportAccountId,
            draft.Home.ImportHomeId,
            .. draft.Rooms.Select(room => room.ImportRoomId),
            .. draft.Rooms.Select(room => room.HomeId),
            .. draft.Devices.Select(device => device.ImportDeviceId),
            .. draft.Devices.Select(device => device.RoomId ?? string.Empty),
            .. draft.Devices.Select(device => device.StableYandexDeviceId ?? string.Empty),
            .. draft.VrfGateways.Select(gateway => gateway.ImportGatewayId),
            .. draft.VrfGateways.Select(gateway => gateway.HomeId),
            .. draft.VrfChildUnits.Select(child => child.ImportChildUnitId),
            .. draft.VrfChildUnits.Select(child => child.ParentGatewayId),
            .. draft.VrfChildUnits.Select(child => child.RoomId ?? string.Empty),
            .. draft.VrfChildUnits.Select(child => child.StableYandexDeviceId ?? string.Empty),
            .. draft.ExposureDecisions.Select(decision => decision.ImportObjectId),
            .. draft.ExposureDecisions.Select(decision => decision.StableYandexDeviceId ?? string.Empty),
            .. draft.ExposureDecisions.Select(decision => decision.RoomId ?? string.Empty)
        ];
    }

    private static bool IsDummyOrTemplateValue(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            || value.StartsWith("dummy-", StringComparison.Ordinal)
            || value.StartsWith("template-", StringComparison.Ordinal)
            || value.StartsWith("yandex-dummy-", StringComparison.Ordinal)
            || value.StartsWith("yandex-template-", StringComparison.Ordinal);
    }

    private static void AssertNoMacLikeOrSensitiveMaterial(string value)
    {
        AssertNoMacLikeValue(value);
        Assert.DoesNotContain("credential", value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("access-token", value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refresh-token", value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("clientSecret", value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deviceKey", value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("real-account-", value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("real-device-", value, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertNoMacLikeValue(string value)
    {
        Regex macLike = new(@"(?:[0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}", RegexOptions.Compiled);

        Assert.False(macLike.IsMatch(value), "Value must not look like a hardware identifier: " + value);
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
