namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;

public sealed record YandexQueryDeviceDto(
    string Id,
    string Status,
    bool Online,
    bool? On,
    string Mode,
    int? TargetTemperatureC,
    string FanSpeed,
    string Source);
