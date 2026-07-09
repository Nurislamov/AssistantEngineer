namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;

public sealed record YandexActionDeviceResultDto(
    string Id,
    string Capability,
    string Status,
    bool SentToGreeCloud,
    bool SentToMqtt,
    bool SentToDevice,
    string RuntimeMode)
{
    public string DeviceId => Id;

    public string? ErrorCode { get; init; }

    public string Message { get; init; } = "Offline action was fail-closed.";
}
