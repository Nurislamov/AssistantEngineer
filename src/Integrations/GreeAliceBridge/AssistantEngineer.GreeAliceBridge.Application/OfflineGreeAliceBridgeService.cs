using AssistantEngineer.GreeAliceBridge.Contracts;

namespace AssistantEngineer.GreeAliceBridge.Application;

public sealed class OfflineGreeAliceBridgeService : IGreeAliceOfflineBridgeService
{
    public const string DummyDeviceId = "dummy-gree-ac-001";

    private static readonly GreeAliceDevice DummyDevice = new(
        DummyDeviceId,
        "Demo Gree AC",
        "Demo Room",
        "devices.types.thermostat.ac",
        Online: true,
        ["on_off", "mode", "temperature", "fan_speed"],
        GreeAliceBridgeSafetyBoundary.RuntimeMode);

    private static readonly GreeAliceDeviceState DummyState = new(
        DummyDeviceId,
        On: true,
        "cool",
        TargetTemperatureC: 24,
        "auto",
        Online: true,
        GreeAliceBridgeSafetyBoundary.RuntimeMode,
        "offline-fixture");

    public IReadOnlyList<GreeAliceDevice> GetDevices()
    {
        return [DummyDevice];
    }

    public GreeAliceDeviceState QueryDeviceState(string deviceId)
    {
        if (string.Equals(deviceId, DummyDeviceId, StringComparison.Ordinal))
        {
            return DummyState;
        }

        return new GreeAliceDeviceState(
            deviceId,
            On: null,
            "unknown",
            TargetTemperatureC: null,
            "unknown",
            Online: false,
            GreeAliceBridgeSafetyBoundary.RuntimeMode,
            "offline-unknown");
    }

    public GreeAliceActionResult ApplyAction(GreeAliceActionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new GreeAliceActionResult(
            request.DeviceId,
            "dry-run-fail-closed",
            SentToGreeCloud: false,
            SentToMqtt: false,
            SentToDevice: false,
            GreeAliceBridgeSafetyBoundary.RuntimeMode);
    }

    public GreeAliceUnlinkResult Unlink(string userId)
    {
        return new GreeAliceUnlinkResult(
            userId,
            "offline-no-production-data-touched",
            ClearedBridgeSessionState: false,
            ClearedProductionAssistantEngineerData: false);
    }
}
