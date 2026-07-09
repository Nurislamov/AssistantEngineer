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
        YandexDeviceDto device = Assert.Single(response.Devices);
        Assert.Equal("dummy-gree-ac-001", device.Id);
        Assert.Equal("offline-fixture", device.Source);
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
    }
}
