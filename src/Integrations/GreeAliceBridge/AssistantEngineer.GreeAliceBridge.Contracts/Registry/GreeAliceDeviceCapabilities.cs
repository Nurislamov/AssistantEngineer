namespace AssistantEngineer.GreeAliceBridge.Contracts.Registry;

public sealed record GreeAliceDeviceCapabilities(
    bool OnOff,
    bool Mode,
    bool Temperature,
    bool FanSpeed);
