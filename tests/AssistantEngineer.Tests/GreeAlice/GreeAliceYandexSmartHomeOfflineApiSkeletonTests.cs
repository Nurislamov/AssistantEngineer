extern alias GreeAliceBridgeApi;

using System.Net;
using System.Net.Http.Json;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class GreeAliceYandexSmartHomeOfflineApiSkeletonTests
{
    [Fact]
    public async Task DevicesEndpointReturnsDummyFixtureDevice()
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
        Assert.Contains(response.Devices, item => item.Id == "yandex-dummy-vrf-child-living-001");
        Assert.Contains(response.Devices, item => item.Id == "yandex-dummy-vrf-child-bedroom-001");
    }

    [Fact]
    public async Task QueryEndpointReturnsOfflineFixtureState()
    {
        await using WebApplicationFactory<GreeAliceBridgeApi::Program> factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage httpResponse = await client.PostAsJsonAsync(
            "/v1.0/user/devices/query",
            new YandexQueryRequest([new YandexDeviceRequestDto("dummy-gree-ac-001")]));

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        YandexQueryResponse? response = await httpResponse.Content.ReadFromJsonAsync<YandexQueryResponse>();
        Assert.NotNull(response);
        YandexQueryDeviceDto state = Assert.Single(response.Devices);
        Assert.Equal("offline-fixture", state.Status);
        Assert.Equal("offline-fixture", state.Source);
        Assert.Equal("offline-fixture", response.RuntimeMode);
    }

    [Fact]
    public async Task ActionEndpointFailsClosedAndSendsNothing()
    {
        await using WebApplicationFactory<GreeAliceBridgeApi::Program> factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage httpResponse = await client.PostAsJsonAsync(
            "/v1.0/user/devices/action",
            new YandexActionRequest(
                [new YandexActionDeviceRequestDto(
                    "dummy-gree-ac-001",
                    [new YandexActionCapabilityRequestDto("devices.capabilities.on_off", "set", "true")])]));

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        YandexActionResponse? response = await httpResponse.Content.ReadFromJsonAsync<YandexActionResponse>();
        Assert.NotNull(response);
        YandexActionDeviceResultDto result = Assert.Single(response.Devices);
        Assert.Equal("dry-run-fail-closed", result.Status);
        Assert.False(result.SentToGreeCloud);
        Assert.False(result.SentToMqtt);
        Assert.False(result.SentToDevice);
        Assert.Equal("dry-run-fail-closed", response.Status);
    }

    [Fact]
    public async Task UnlinkEndpointDoesNotClearProductionData()
    {
        await using WebApplicationFactory<GreeAliceBridgeApi::Program> factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage httpResponse = await client.PostAsync("/v1.0/user/unlink", content: null);

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        YandexUnlinkResponse? response = await httpResponse.Content.ReadFromJsonAsync<YandexUnlinkResponse>();
        Assert.NotNull(response);
        Assert.False(response.ClearedProductionAssistantEngineerData);
        Assert.Equal("offline-fixture", response.RuntimeMode);
    }

    [Fact]
    public async Task HealthEndpointReturnsOfflineSafeState()
    {
        await using WebApplicationFactory<GreeAliceBridgeApi::Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage httpResponse = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        string json = await httpResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"runtimeMode\":\"offline-fixture\"", json, StringComparison.Ordinal);
        Assert.Contains("\"safetyDecision\"", json, StringComparison.Ordinal);
        Assert.Contains("\"isAllowed\":true", json, StringComparison.Ordinal);
        Assert.Contains("\"liveMqttConnectEnabled\":false", json, StringComparison.Ordinal);
        Assert.Contains("\"mqttSubscribeEnabled\":false", json, StringComparison.Ordinal);
        Assert.Contains("\"mqttPublishEnabled\":false", json, StringComparison.Ordinal);
        Assert.Contains("\"deviceControlEnabled\":false", json, StringComparison.Ordinal);
        Assert.Contains("\"greeRuntimeControlEnabled\":false", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task QueryEndpointEmptyRequestReturnsControlledOfflineErrorResponse()
    {
        await using WebApplicationFactory<GreeAliceBridgeApi::Program> factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage httpResponse = await client.PostAsJsonAsync(
            "/v1.0/user/devices/query",
            new YandexQueryRequest([]));

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        YandexQueryResponse? response = await httpResponse.Content.ReadFromJsonAsync<YandexQueryResponse>();
        Assert.NotNull(response);
        Assert.Empty(response.Devices);
        Assert.Equal("offline-empty-query", response.ErrorCode);
    }

    [Fact]
    public async Task ActionEndpointEmptyRequestReturnsControlledFailClosedResponse()
    {
        await using WebApplicationFactory<GreeAliceBridgeApi::Program> factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage httpResponse = await client.PostAsJsonAsync(
            "/v1.0/user/devices/action",
            new YandexActionRequest([]));

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        YandexActionResponse? response = await httpResponse.Content.ReadFromJsonAsync<YandexActionResponse>();
        Assert.NotNull(response);
        Assert.Empty(response.Devices);
        Assert.Equal("dry-run-fail-closed", response.Status);
        Assert.Equal("offline-empty-action", response.ErrorCode);
    }
}
