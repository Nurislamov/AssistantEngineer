namespace AssistantEngineer.GreeAliceBridge.Contracts;

public sealed record GreeAliceDeviceState(
    string DeviceId,
    bool? On,
    string Mode,
    int? TargetTemperatureC,
    string FanSpeed,
    bool Online,
    string Source,
    string Status);
