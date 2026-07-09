namespace AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud;

public sealed record GreeCloudDeviceStateSnapshot(
    string DeviceId,
    string Status,
    bool Online,
    bool? On,
    string Mode,
    int? TargetTemperatureC,
    string FanSpeed,
    string Source,
    string AdapterMode);
