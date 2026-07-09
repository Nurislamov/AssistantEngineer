using AssistantEngineer.GreeAliceBridge.Application;
using AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class GreeAliceYandexSmartHomeOfflineMappingTests
{
    [Fact]
    public void DevicesResponseContainsDummyFixtureDevice()
    {
        IYandexSmartHomeOfflineService service = CreateService();

        YandexDeviceDto device = Assert.Single(service.GetDevices().Devices);

        Assert.Equal("dummy-gree-ac-001", device.Id);
        Assert.Equal("Demo Gree AC", device.Name);
        Assert.Equal("Demo Room", device.Room);
        Assert.Equal("devices.types.thermostat.ac", device.Type);
        Assert.Equal("offline-fixture", device.Source);
        Assert.Contains(device.Capabilities, capability => capability.Type == "devices.capabilities.on_off");
    }

    [Fact]
    public void QueryResponseContainsOfflineFixtureState()
    {
        IYandexSmartHomeOfflineService service = CreateService();

        YandexQueryResponse response = service.QueryDevices(new YandexQueryRequest(
            [new YandexDeviceRequestDto("dummy-gree-ac-001")]));

        YandexQueryDeviceDto state = Assert.Single(response.Devices);
        Assert.Equal("dummy-gree-ac-001", state.Id);
        Assert.Equal("offline-fixture", state.Status);
        Assert.True(state.Online);
        Assert.True(state.On);
        Assert.Equal("cool", state.Mode);
        Assert.Equal(24, state.TargetTemperatureC);
        Assert.Equal("auto", state.FanSpeed);
        Assert.Equal("offline-fixture", state.Source);
    }

    [Fact]
    public void ActionResponseFailsClosedAndSendsNothing()
    {
        IYandexSmartHomeOfflineService service = CreateService();

        YandexActionResponse response = service.ExecuteAction(new YandexActionRequest(
            [new YandexActionDeviceRequestDto(
                "dummy-gree-ac-001",
                [new YandexActionCapabilityRequestDto("devices.capabilities.on_off", "set", "true")])]));

        YandexActionDeviceResultDto result = Assert.Single(response.Devices);
        Assert.Equal("dummy-gree-ac-001", result.Id);
        Assert.Equal("dry-run-fail-closed", result.Status);
        Assert.False(result.SentToGreeCloud);
        Assert.False(result.SentToMqtt);
        Assert.False(result.SentToDevice);
        Assert.Equal("offline-fixture", result.RuntimeMode);
    }

    [Fact]
    public void UnlinkDoesNotClearProductionData()
    {
        IYandexSmartHomeOfflineService service = CreateService();

        YandexUnlinkResponse response = service.Unlink("offline-user");

        Assert.Equal("offline-user", response.UserId);
        Assert.Equal("offline-no-production-data-touched", response.Status);
        Assert.False(response.ClearedBridgeSessionState);
        Assert.False(response.ClearedProductionAssistantEngineerData);
    }

    private static IYandexSmartHomeOfflineService CreateService()
    {
        return new YandexSmartHomeOfflineService(new OfflineGreeAliceBridgeService());
    }
}
