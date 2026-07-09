using AssistantEngineer.GreeAliceBridge.Contracts;

namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;

public sealed record YandexQueryDeviceDto(
    string Id,
    string Status,
    bool Online,
    bool? On,
    string Mode,
    int? TargetTemperatureC,
    string FanSpeed,
    string Source)
{
    public string DeviceId => Id;

    public string? ErrorCode { get; init; }

    public string Message { get; init; } = "Offline fixture state.";

    public string RuntimeMode { get; init; } = GreeAliceBridgeSafetyBoundary.RuntimeMode;
}
