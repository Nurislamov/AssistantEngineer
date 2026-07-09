using AssistantEngineer.GreeAliceBridge.Application;
using AssistantEngineer.GreeAliceBridge.Application.Registry;
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
    public void DevicesResponseWithRegistryProviderStillReturnsOnlyDummySplitAc()
    {
        IYandexSmartHomeOfflineService service = new YandexSmartHomeOfflineService(
            new OfflineGreeAliceBridgeService(),
            new OfflineGreeAliceRegistryProvider());

        YandexDevicesResponse response = service.GetDevices();

        YandexDeviceDto device = Assert.Single(response.Devices);
        Assert.Equal("dummy-gree-ac-001", device.Id);
        Assert.Equal("offline-fixture", device.Source);
    }

    [Fact]
    public void QueryResponseContainsOfflineFixtureState()
    {
        IYandexSmartHomeOfflineService service = CreateService();

        YandexQueryResponse response = service.QueryDevices(new YandexQueryRequest(
            [new YandexDeviceRequestDto("dummy-gree-ac-001")]));

        YandexQueryDeviceDto state = Assert.Single(response.Devices);
        Assert.Equal("dummy-gree-ac-001", state.Id);
        Assert.Equal("dummy-gree-ac-001", state.DeviceId);
        Assert.Equal("offline-fixture", state.Status);
        Assert.True(state.Online);
        Assert.True(state.On);
        Assert.Equal("cool", state.Mode);
        Assert.Equal(24, state.TargetTemperatureC);
        Assert.Equal("auto", state.FanSpeed);
        Assert.Equal("offline-fixture", state.Source);
        Assert.Equal("offline-fixture", state.RuntimeMode);
        Assert.Equal("offline-fixture-query", response.RequestId);
        Assert.Equal("ok", response.Status);
        Assert.Null(response.ErrorCode);
    }

    [Fact]
    public void QueryUnknownDeviceReturnsControlledOfflineUnknownState()
    {
        IYandexSmartHomeOfflineService service = CreateService();

        YandexQueryResponse response = service.QueryDevices(new YandexQueryRequest(
            [new YandexDeviceRequestDto("unknown-device")]));

        YandexQueryDeviceDto state = Assert.Single(response.Devices);
        Assert.Equal("unknown-device", state.Id);
        Assert.Equal("offline-unknown", state.Status);
        Assert.False(state.Online);
        Assert.Equal("unknown", state.Mode);
        Assert.Equal("unknown", state.FanSpeed);
        Assert.Equal("offline-fixture", state.RuntimeMode);
    }

    [Fact]
    public void QueryEmptyRequestReturnsControlledOfflineErrorResponse()
    {
        IYandexSmartHomeOfflineService service = CreateService();

        YandexQueryResponse response = service.QueryDevices(new YandexQueryRequest([]));

        Assert.Empty(response.Devices);
        Assert.Equal("offline-empty-request", response.Status);
        Assert.Equal("offline-empty-query", response.ErrorCode);
        Assert.Equal("offline-fixture", response.RuntimeMode);
    }

    [Fact]
    public void QueryNullRequestReturnsControlledOfflineErrorResponse()
    {
        IYandexSmartHomeOfflineService service = CreateService();

        YandexQueryResponse response = service.QueryDevices(null);

        Assert.Empty(response.Devices);
        Assert.Equal("offline-empty-request", response.Status);
        Assert.Equal("offline-empty-query", response.ErrorCode);
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
        Assert.Equal("dummy-gree-ac-001", result.DeviceId);
        Assert.Equal("devices.capabilities.on_off", result.Capability);
        Assert.Equal("dry-run-fail-closed", result.Status);
        Assert.False(result.SentToGreeCloud);
        Assert.False(result.SentToMqtt);
        Assert.False(result.SentToDevice);
        Assert.Equal("offline-fixture", result.RuntimeMode);
        Assert.Equal("dry-run-fail-closed", response.Status);
        Assert.Null(response.ErrorCode);
    }

    [Fact]
    public void ActionUnknownDeviceFailsClosedAndSendsNothing()
    {
        IYandexSmartHomeOfflineService service = CreateService();

        YandexActionResponse response = service.ExecuteAction(new YandexActionRequest(
            [new YandexActionDeviceRequestDto(
                "unknown-device",
                [new YandexActionCapabilityRequestDto("devices.capabilities.on_off", "set", "true")])]));

        YandexActionDeviceResultDto result = Assert.Single(response.Devices);
        Assert.Equal("unknown-device", result.Id);
        Assert.Equal("dry-run-fail-closed", result.Status);
        Assert.False(result.SentToGreeCloud);
        Assert.False(result.SentToMqtt);
        Assert.False(result.SentToDevice);
    }

    [Fact]
    public void ActionUnknownCapabilityFailsClosedAndSendsNothing()
    {
        IYandexSmartHomeOfflineService service = CreateService();

        YandexActionResponse response = service.ExecuteAction(new YandexActionRequest(
            [new YandexActionDeviceRequestDto(
                "dummy-gree-ac-001",
                [new YandexActionCapabilityRequestDto("devices.capabilities.unknown", "set", "x")])]));

        YandexActionDeviceResultDto result = Assert.Single(response.Devices);
        Assert.Equal("devices.capabilities.unknown", result.Capability);
        Assert.Equal("dry-run-fail-closed", result.Status);
        Assert.False(result.SentToGreeCloud);
        Assert.False(result.SentToMqtt);
        Assert.False(result.SentToDevice);
    }

    [Fact]
    public void ActionEmptyRequestReturnsControlledFailClosedResponse()
    {
        IYandexSmartHomeOfflineService service = CreateService();

        YandexActionResponse response = service.ExecuteAction(new YandexActionRequest([]));

        Assert.Empty(response.Devices);
        Assert.Equal("dry-run-fail-closed", response.Status);
        Assert.Equal("offline-empty-action", response.ErrorCode);
        Assert.Equal("offline-fixture", response.RuntimeMode);
    }

    [Fact]
    public void ActionNullRequestReturnsControlledFailClosedResponse()
    {
        IYandexSmartHomeOfflineService service = CreateService();

        YandexActionResponse response = service.ExecuteAction(null);

        Assert.Empty(response.Devices);
        Assert.Equal("dry-run-fail-closed", response.Status);
        Assert.Equal("offline-empty-action", response.ErrorCode);
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
        Assert.Equal("offline-fixture", response.RuntimeMode);
    }

    private static IYandexSmartHomeOfflineService CreateService()
    {
        return new YandexSmartHomeOfflineService(new OfflineGreeAliceBridgeService());
    }
}
