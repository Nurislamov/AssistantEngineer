extern alias GreeAliceBridgeApi;

using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using AssistantEngineer.GreeAliceBridge.Application.Registry;
using AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud;
using AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.Mapping;
using AssistantEngineer.GreeAliceBridge.Contracts.Registry;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class GreeAliceOfflineEndToEndBridgeFlowTests
{
    [Fact]
    public async Task HealthFlowReportsOfflineSafeState()
    {
        await using WebApplicationFactory<GreeAliceBridgeApi::Program> factory = new();
        using HttpClient client = factory.CreateClient();

        string json = await client.GetStringAsync("/health");

        Assert.Contains("\"status\":\"healthy\"", json, StringComparison.Ordinal);
        Assert.Contains("\"runtimeMode\":\"offline-fixture\"", json, StringComparison.Ordinal);
        Assert.Contains("\"safetyDecision\"", json, StringComparison.Ordinal);
        Assert.Contains("\"liveMqttConnectEnabled\":false", json, StringComparison.Ordinal);
        Assert.Contains("\"mqttSubscribeEnabled\":false", json, StringComparison.Ordinal);
        Assert.Contains("\"mqttPublishEnabled\":false", json, StringComparison.Ordinal);
        Assert.Contains("\"deviceControlEnabled\":false", json, StringComparison.Ordinal);
        Assert.Contains("\"greeRuntimeControlEnabled\":false", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeviceDiscoveryFlowReturnsOnlyOfflineDummySplitAcAndSafePayload()
    {
        await using WebApplicationFactory<GreeAliceBridgeApi::Program> factory = new();
        using HttpClient client = factory.CreateClient();

        YandexDevicesResponse? response = await client.GetFromJsonAsync<YandexDevicesResponse>("/v1.0/user/devices");

        Assert.NotNull(response);
        Assert.True(response.Devices.Count >= 3);
        YandexDeviceDto device = Assert.Single(response.Devices, item => item.Id == "dummy-gree-ac-001");
        Assert.Equal("dummy-gree-ac-001", device.Id);
        Assert.Equal("offline-fixture", device.Source);
        Assert.Equal("offline-fixture", response.RuntimeMode);

        AssertSafePayloadValue(device.Id);
        AssertSafePayloadValue(device.Name);
        AssertSafePayloadValue(device.Room);
        Assert.DoesNotContain("dummy-vrf-gateway-001", response.Devices.Select(item => item.Id));
        Assert.DoesNotContain("dummy-vrf-child-001", response.Devices.Select(item => item.Id));
        Assert.Contains(response.Devices, item => item.Id == "yandex-dummy-vrf-child-living-001");
        Assert.Contains(response.Devices, item => item.Id == "yandex-dummy-vrf-child-bedroom-001");

        GreeAliceRegistrySnapshot registry = new OfflineGreeAliceRegistryProvider().GetSnapshot();
        Assert.Contains(registry.Devices, item => item.Id == "dummy-vrf-child-001" && item.Kind == GreeAliceDeviceKind.VrfChildIndoorUnit);
    }

    [Fact]
    public async Task QueryFlowReturnsOfflineKnownAndUnknownStatesWithoutLiveCalls()
    {
        await using WebApplicationFactory<GreeAliceBridgeApi::Program> factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage knownHttpResponse = await client.PostAsJsonAsync(
            "/v1.0/user/devices/query",
            new YandexQueryRequest([new YandexDeviceRequestDto("dummy-gree-ac-001")]));
        HttpResponseMessage unknownHttpResponse = await client.PostAsJsonAsync(
            "/v1.0/user/devices/query",
            new YandexQueryRequest([new YandexDeviceRequestDto("unknown-device")]));

        Assert.Equal(HttpStatusCode.OK, knownHttpResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, unknownHttpResponse.StatusCode);

        YandexQueryResponse? knownResponse = await knownHttpResponse.Content.ReadFromJsonAsync<YandexQueryResponse>();
        YandexQueryResponse? unknownResponse = await unknownHttpResponse.Content.ReadFromJsonAsync<YandexQueryResponse>();

        Assert.NotNull(knownResponse);
        YandexQueryDeviceDto knownState = Assert.Single(knownResponse.Devices);
        Assert.Equal("dummy-gree-ac-001", knownState.Id);
        Assert.Equal("offline-fixture", knownState.Status);
        Assert.Equal("offline-fixture", knownState.RuntimeMode);
        Assert.Null(knownResponse.ErrorCode);

        Assert.NotNull(unknownResponse);
        YandexQueryDeviceDto unknownState = Assert.Single(unknownResponse.Devices);
        Assert.Equal("unknown-device", unknownState.Id);
        Assert.Equal("offline-unknown", unknownState.Status);
        Assert.False(unknownState.Online);
        Assert.Equal("offline-fixture", unknownState.RuntimeMode);

        AssertNoLiveSendFlagsInQuery(knownResponse);
        AssertNoLiveSendFlagsInQuery(unknownResponse);
    }

    [Fact]
    public async Task ActionFlowAlwaysFailsClosedForKnownUnknownAndUnsupportedRequests()
    {
        await using WebApplicationFactory<GreeAliceBridgeApi::Program> factory = new();
        using HttpClient client = factory.CreateClient();

        YandexActionDeviceResultDto known = await ExecuteSingleActionAsync(
            client,
            "dummy-gree-ac-001",
            "devices.capabilities.on_off");
        YandexActionDeviceResultDto unknownDevice = await ExecuteSingleActionAsync(
            client,
            "unknown-device",
            "devices.capabilities.on_off");
        YandexActionDeviceResultDto unknownCapability = await ExecuteSingleActionAsync(
            client,
            "dummy-gree-ac-001",
            "devices.capabilities.unknown");

        AssertFailClosed(known, "dummy-gree-ac-001", "devices.capabilities.on_off");
        AssertFailClosed(unknownDevice, "unknown-device", "devices.capabilities.on_off");
        AssertFailClosed(unknownCapability, "dummy-gree-ac-001", "devices.capabilities.unknown");
    }

    [Fact]
    public async Task UnlinkFlowIsOfflineOnlyAndDoesNotClearProductionData()
    {
        await using WebApplicationFactory<GreeAliceBridgeApi::Program> factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage httpResponse = await client.PostAsync("/v1.0/user/unlink", content: null);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        YandexUnlinkResponse? response = await httpResponse.Content.ReadFromJsonAsync<YandexUnlinkResponse>();

        Assert.NotNull(response);
        Assert.Equal("offline-no-production-data-touched", response.Status);
        Assert.False(response.ClearedBridgeSessionState);
        Assert.False(response.ClearedProductionAssistantEngineerData);
        Assert.Equal("offline-fixture", response.RuntimeMode);
    }

    [Fact]
    public void OfflineEndToEndBoundariesKeepLiveOperationsBlocked()
    {
        Assert.False(GreeCloudAdapterSafetyBoundary.UsesLiveGreeCloud);
        Assert.False(GreeCloudAdapterSafetyBoundary.UsesHttpNetwork);
        Assert.False(GreeCloudAdapterSafetyBoundary.UsesMqttNetwork);
        Assert.False(GreeCloudAdapterSafetyBoundary.AllowsMqttConnect);
        Assert.False(GreeCloudAdapterSafetyBoundary.AllowsMqttSubscribe);
        Assert.False(GreeCloudAdapterSafetyBoundary.AllowsMqttPublish);
        Assert.False(GreeCloudAdapterSafetyBoundary.AllowsDeviceControl);
        Assert.False(GreeCloudAdapterSafetyBoundary.AllowsRuntimeControl);
        Assert.False(GreeCloudStateMappingSafetyBoundary.UsesLiveGreeCloud);
        Assert.False(GreeCloudStateMappingSafetyBoundary.AllowsDeviceControl);
        Assert.False(GreeCloudStateMappingSafetyBoundary.AllowsRuntimeControl);
    }

    private static async Task<YandexActionDeviceResultDto> ExecuteSingleActionAsync(
        HttpClient client,
        string deviceId,
        string capability)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/v1.0/user/devices/action",
            new YandexActionRequest(
                [new YandexActionDeviceRequestDto(
                    deviceId,
                    [new YandexActionCapabilityRequestDto(capability, "set", "true")])]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        YandexActionResponse? actionResponse = await response.Content.ReadFromJsonAsync<YandexActionResponse>();

        Assert.NotNull(actionResponse);
        Assert.Equal("dry-run-fail-closed", actionResponse.Status);

        return Assert.Single(actionResponse.Devices);
    }

    private static void AssertFailClosed(YandexActionDeviceResultDto result, string deviceId, string capability)
    {
        Assert.Equal(deviceId, result.Id);
        Assert.Equal(capability, result.Capability);
        Assert.Equal("dry-run-fail-closed", result.Status);
        Assert.False(result.SentToGreeCloud);
        Assert.False(result.SentToMqtt);
        Assert.False(result.SentToDevice);
        Assert.Equal("offline-fixture", result.RuntimeMode);
    }

    private static void AssertNoLiveSendFlagsInQuery(YandexQueryResponse response)
    {
        string text = string.Join(Environment.NewLine, response.Devices.Select(device => string.Join(
            "|",
            device.Id,
            device.Status,
            device.Source,
            device.RuntimeMode)));

        Assert.DoesNotContain("SentToGreeCloud", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SentToMqtt", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SentToDevice", text, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertSafePayloadValue(string value)
    {
        Regex macLike = new(@"(?:[0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}", RegexOptions.Compiled);

        Assert.False(macLike.IsMatch(value), "Payload value must not look like a MAC identifier: " + value);
        Assert.DoesNotContain("token", value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("key", value, StringComparison.OrdinalIgnoreCase);
    }
}
