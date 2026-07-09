namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;

public sealed record YandexActionDeviceResultDto(
    string Id,
    string Status,
    bool SentToGreeCloud,
    bool SentToMqtt,
    bool SentToDevice,
    string RuntimeMode);
