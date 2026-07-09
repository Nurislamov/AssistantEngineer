using AssistantEngineer.GreeAliceBridge.Contracts;

namespace AssistantEngineer.GreeAliceBridge.Application;

public interface IGreeAliceOfflineBridgeService
{
    IReadOnlyList<GreeAliceDevice> GetDevices();

    GreeAliceDeviceState QueryDeviceState(string deviceId);

    GreeAliceActionResult ApplyAction(GreeAliceActionRequest request);

    GreeAliceUnlinkResult Unlink(string userId);
}
