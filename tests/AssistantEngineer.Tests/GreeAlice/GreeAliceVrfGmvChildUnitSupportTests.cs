extern alias GreeAliceBridgeApi;

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using AssistantEngineer.GreeAliceBridge.Application.Registry;
using AssistantEngineer.GreeAliceBridge.Contracts.Pilot;
using AssistantEngineer.GreeAliceBridge.Contracts.Registry;
using AssistantEngineer.GreeAliceBridge.Contracts.Registry.Vrf;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class GreeAliceVrfGmvChildUnitSupportTests
{
    [Fact]
    public void VrfGatewayModelIsInternalAndOfflineOnly()
    {
        GreeAliceVrfGateway gateway = Assert.Single(GreeAliceVrfSystemTopology.OfflineFixture.Gateways);
        string combined = string.Join("|", gateway.GatewayId, gateway.DisplayName, gateway.HomeId, gateway.SystemName, gateway.GatewayKind);

        Assert.Equal("dummy-vrf-gateway-001", gateway.GatewayId);
        Assert.Equal("vrf-gateway", gateway.GatewayKind);
        Assert.True(gateway.IsTechnicalDevice);
        Assert.False(gateway.ExposeToYandex);
        Assert.False(GreeAliceVrfYandexExposurePolicy.ShouldExpose(gateway));
        AssertNoMacLikeOrSecretMaterial(combined);
    }

    [Fact]
    public void VrfChildUnitsHaveParentGatewayRoomBindingAndStableYandexIds()
    {
        GreeAliceVrfSystemTopology topology = GreeAliceVrfSystemTopology.OfflineFixture;
        GreeAliceVrfGateway gateway = Assert.Single(topology.Gateways);

        foreach (GreeAliceVrfChildUnit child in topology.ChildUnits)
        {
            Assert.Equal(gateway.GatewayId, child.ParentGatewayId);
            Assert.Equal("vrf-child-indoor-unit", child.DeviceKind);
            Assert.True(child.ExposeToYandex);
            Assert.True(GreeAliceVrfYandexExposurePolicy.ShouldExpose(child));
            Assert.StartsWith("dummy-room-", child.RoomId, StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(child.RoomName));
            Assert.StartsWith("yandex-dummy-vrf-child-", child.StableYandexDeviceId, StringComparison.Ordinal);
            Assert.Equal(child.StableYandexDeviceId, GreeAliceVrfChildUnitStableId.Resolve(child));
            AssertNoMacLikeOrSecretMaterial(string.Join(
                "|",
                child.ChildUnitId,
                child.ParentGatewayId,
                child.StableYandexDeviceId,
                child.DisplayName,
                child.RoomId,
                child.RoomName,
                child.DeviceKind));
        }

        Assert.Contains(topology.ChildUnits, child => child.ChildUnitId == "dummy-vrf-child-living-001");
        Assert.Contains(topology.ChildUnits, child => child.ChildUnitId == "dummy-vrf-child-bedroom-001");
    }

    [Fact]
    public void StableYandexIdDoesNotChangeWhenDisplayNameOrRoomNameChanges()
    {
        GreeAliceVrfChildUnit original = Assert.Single(
            GreeAliceVrfSystemTopology.OfflineFixture.ChildUnits,
            child => child.ChildUnitId == "dummy-vrf-child-living-001");
        GreeAliceVrfChildUnit renamed = original with
        {
            DisplayName = "Кондиционер зал",
            RoomName = "Зал"
        };

        Assert.Equal("yandex-dummy-vrf-child-living-001", GreeAliceVrfChildUnitStableId.Resolve(original));
        Assert.Equal(GreeAliceVrfChildUnitStableId.Resolve(original), GreeAliceVrfChildUnitStableId.Resolve(renamed));
        Assert.NotEqual(original.ParentGatewayId, original.StableYandexDeviceId);
        AssertNoMacLikeOrSecretMaterial(original.StableYandexDeviceId);
    }

    [Fact]
    public void OfflineRegistryContainsGatewayAndVrfChildUnits()
    {
        GreeAliceRegistrySnapshot snapshot = new OfflineGreeAliceRegistryProvider().GetSnapshot();

        GreeAliceRegisteredDevice gateway = Assert.Single(snapshot.Devices, device => device.Id == "dummy-vrf-gateway-001");
        Assert.Equal(GreeAliceDeviceKind.VrfGateway, gateway.Kind);
        Assert.False(gateway.YandexExposed);

        foreach (string childId in new[] { "dummy-vrf-child-living-001", "dummy-vrf-child-bedroom-001" })
        {
            GreeAliceRegisteredDevice child = Assert.Single(snapshot.Devices, device => device.Id == childId);
            Assert.Equal(GreeAliceDeviceKind.VrfChildIndoorUnit, child.Kind);
            Assert.Equal("dummy-vrf-gateway-001", child.ParentGatewayRef);
            Assert.True(child.YandexExposed);
            Assert.Contains(snapshot.Rooms, room => room.Id == child.RoomRef);
        }
    }

    [Fact]
    public async Task YandexDevicesExposeSplitAcAndVrfChildrenButNotGateway()
    {
        await using WebApplicationFactory<GreeAliceBridgeApi::Program> factory = new();
        using HttpClient client = factory.CreateClient();

        YandexDevicesResponse? response = await client.GetFromJsonAsync<YandexDevicesResponse>("/v1.0/user/devices");

        Assert.NotNull(response);
        Assert.Contains(response.Devices, device => device.Id == "dummy-gree-ac-001");
        Assert.Contains(response.Devices, device => device.Id == "yandex-dummy-vrf-child-living-001" && device.Name == "Кондиционер гостиная" && device.Room == "Гостиная");
        Assert.Contains(response.Devices, device => device.Id == "yandex-dummy-vrf-child-bedroom-001" && device.Name == "Кондиционер спальня" && device.Room == "Спальня");
        Assert.DoesNotContain(response.Devices, device => device.Id == "dummy-vrf-gateway-001");
        Assert.DoesNotContain(response.Devices, device => device.Id == "dummy-vrf-child-001");
    }

    [Theory]
    [InlineData("yandex-dummy-vrf-child-living-001")]
    [InlineData("yandex-dummy-vrf-child-bedroom-001")]
    public async Task QueryKnownVrfChildReturnsOfflineFixtureState(string yandexDeviceId)
    {
        await using WebApplicationFactory<GreeAliceBridgeApi::Program> factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/v1.0/user/devices/query",
            new YandexQueryRequest([new YandexDeviceRequestDto(yandexDeviceId)]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        YandexQueryResponse? body = await response.Content.ReadFromJsonAsync<YandexQueryResponse>();
        Assert.NotNull(body);
        YandexQueryDeviceDto state = Assert.Single(body.Devices);
        Assert.Equal(yandexDeviceId, state.Id);
        Assert.Equal("offline-fixture", state.Status);
        Assert.True(state.Online);
        Assert.True(state.On);
        Assert.Equal("cool", state.Mode);
        Assert.Equal("auto", state.FanSpeed);
    }

    [Fact]
    public async Task QueryUnknownVrfChildReturnsControlledUnknownState()
    {
        await using WebApplicationFactory<GreeAliceBridgeApi::Program> factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/v1.0/user/devices/query",
            new YandexQueryRequest([new YandexDeviceRequestDto("yandex-dummy-vrf-child-unknown")]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        YandexQueryResponse? body = await response.Content.ReadFromJsonAsync<YandexQueryResponse>();
        Assert.NotNull(body);
        YandexQueryDeviceDto state = Assert.Single(body.Devices);
        Assert.Equal("offline-unknown", state.Status);
        Assert.False(state.Online);
    }

    [Theory]
    [InlineData("yandex-dummy-vrf-child-living-001")]
    [InlineData("yandex-dummy-vrf-child-unknown")]
    public async Task ActionForVrfChildAlwaysFailsClosedAndSendsNothing(string yandexDeviceId)
    {
        await using WebApplicationFactory<GreeAliceBridgeApi::Program> factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/v1.0/user/devices/action",
            new YandexActionRequest(
                [new YandexActionDeviceRequestDto(
                    yandexDeviceId,
                    [new YandexActionCapabilityRequestDto("devices.capabilities.on_off", "set", "true")])]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        YandexActionResponse? body = await response.Content.ReadFromJsonAsync<YandexActionResponse>();
        Assert.NotNull(body);
        YandexActionDeviceResultDto result = Assert.Single(body.Devices);
        Assert.Equal("dry-run-fail-closed", result.Status);
        Assert.False(result.SentToGreeCloud);
        Assert.False(result.SentToMqtt);
        Assert.False(result.SentToDevice);
    }

    [Fact]
    public void ProductionPilotAndSafetyBoundariesRemainBlocked()
    {
        Assert.False(GreeAliceMinimalProductionPilotBoundary.ProductionPilotApproved);
        Assert.False(GreeAliceVrfChildUnitSafetyBoundary.UsesLiveGreeCloud);
        Assert.False(GreeAliceVrfChildUnitSafetyBoundary.UsesMqtt);
        Assert.False(GreeAliceVrfChildUnitSafetyBoundary.AllowsDeviceControl);
        Assert.False(GreeAliceVrfChildUnitSafetyBoundary.AllowsProductionWiring);
    }

    [Fact]
    public void VrfGmvChildUnitSupportDocExistsAndKeepsSafetyBoundary()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "vrf-gmv-child-unit-support.md");

        Assert.Contains("gateway is internal by default", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Child units are exposed", text, StringComparison.Ordinal);
        Assert.Contains("offline fixture only", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Live Gree+ Cloud is not used", text, StringComparison.Ordinal);
        Assert.Contains("MQTT is blocked", text, StringComparison.Ordinal);
        Assert.Contains("Control remains fail-closed", text, StringComparison.Ordinal);
        Assert.Contains("Production wiring is disabled", text, StringComparison.Ordinal);
        Assert.Contains("Secrets are forbidden", text, StringComparison.Ordinal);
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

    private static void AssertNoMacLikeOrSecretMaterial(string value)
    {
        Regex macLike = new(@"(?:[0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}", RegexOptions.Compiled);

        Assert.False(macLike.IsMatch(value), "Value must not look like a MAC identifier: " + value);
        Assert.DoesNotContain("credential", value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("device-key", value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("real-account-", value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("real-device-", value, StringComparison.OrdinalIgnoreCase);
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
