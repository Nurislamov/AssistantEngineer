using AssistantEngineer.GreeAliceBridge.Application;
using AssistantEngineer.GreeAliceBridge.Contracts;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class GreeAliceOfflineBridgeProjectSkeletonTests
{
    [Fact]
    public void SafetyBoundaryKeepsAllLiveGatesClosed()
    {
        Assert.False(GreeAliceBridgeSafetyBoundary.LiveMqttConnectEnabled);
        Assert.False(GreeAliceBridgeSafetyBoundary.MqttSubscribeEnabled);
        Assert.False(GreeAliceBridgeSafetyBoundary.MqttPublishEnabled);
        Assert.False(GreeAliceBridgeSafetyBoundary.DeviceControlEnabled);
        Assert.False(GreeAliceBridgeSafetyBoundary.GreeRuntimeControlEnabled);
    }

    [Fact]
    public void SafetyBoundaryUsesOfflineFixtureRuntimeMode()
    {
        Assert.Equal("offline-fixture", GreeAliceBridgeSafetyBoundary.RuntimeMode);
    }

    [Fact]
    public void DevicesReturnDummyFixtureDevices()
    {
        IGreeAliceOfflineBridgeService service = new OfflineGreeAliceBridgeService();

        IReadOnlyList<GreeAliceDevice> devices = service.GetDevices();

        GreeAliceDevice device = Assert.Single(devices, item => item.Id == "dummy-gree-ac-001");
        Assert.Equal("dummy-gree-ac-001", device.Id);
        Assert.Equal("Demo Gree AC", device.Name);
        Assert.Equal("offline-fixture", device.Source);
        Assert.Contains("on_off", device.Capabilities);
        Assert.Contains("mode", device.Capabilities);
        Assert.Contains("temperature", device.Capabilities);
        Assert.Contains("fan_speed", device.Capabilities);
        Assert.Contains(devices, item => item.Id == "yandex-dummy-vrf-child-living-001");
        Assert.Contains(devices, item => item.Id == "yandex-dummy-vrf-child-bedroom-001");
    }

    [Fact]
    public void QueryReturnsOfflineFixtureStateForDummyDevice()
    {
        IGreeAliceOfflineBridgeService service = new OfflineGreeAliceBridgeService();

        GreeAliceDeviceState state = service.QueryDeviceState("dummy-gree-ac-001");

        Assert.Equal("dummy-gree-ac-001", state.DeviceId);
        Assert.True(state.On);
        Assert.Equal("cool", state.Mode);
        Assert.Equal(24, state.TargetTemperatureC);
        Assert.Equal("auto", state.FanSpeed);
        Assert.True(state.Online);
        Assert.Equal("offline-fixture", state.Source);
        Assert.Equal("offline-fixture", state.Status);
    }

    [Fact]
    public void QueryReturnsOfflineUnknownStateForUnknownDevice()
    {
        IGreeAliceOfflineBridgeService service = new OfflineGreeAliceBridgeService();

        GreeAliceDeviceState state = service.QueryDeviceState("unknown-device");

        Assert.Equal("unknown-device", state.DeviceId);
        Assert.Null(state.On);
        Assert.Equal("unknown", state.Mode);
        Assert.Null(state.TargetTemperatureC);
        Assert.Equal("unknown", state.FanSpeed);
        Assert.False(state.Online);
        Assert.Equal("offline-fixture", state.Source);
        Assert.Equal("offline-unknown", state.Status);
    }

    [Fact]
    public void ActionReturnsDryRunFailClosedWithoutSendingAnything()
    {
        IGreeAliceOfflineBridgeService service = new OfflineGreeAliceBridgeService();

        GreeAliceActionResult result = service.ApplyAction(new GreeAliceActionRequest(
            "dummy-gree-ac-001",
            "on_off",
            "set",
            "true"));

        Assert.Equal("dry-run-fail-closed", result.Status);
        Assert.False(result.SentToGreeCloud);
        Assert.False(result.SentToMqtt);
        Assert.False(result.SentToDevice);
        Assert.Equal("offline-fixture", result.RuntimeMode);
    }

    [Fact]
    public void UnlinkDoesNotClearProductionAssistantEngineerData()
    {
        IGreeAliceOfflineBridgeService service = new OfflineGreeAliceBridgeService();

        GreeAliceUnlinkResult result = service.Unlink("offline-user");

        Assert.Equal("offline-user", result.UserId);
        Assert.Equal("offline-no-production-data-touched", result.Status);
        Assert.False(result.ClearedBridgeSessionState);
        Assert.False(result.ClearedProductionAssistantEngineerData);
    }
}
