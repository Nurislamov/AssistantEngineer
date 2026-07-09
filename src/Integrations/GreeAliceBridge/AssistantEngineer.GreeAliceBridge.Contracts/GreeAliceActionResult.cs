namespace AssistantEngineer.GreeAliceBridge.Contracts;

public sealed record GreeAliceActionResult(
    string DeviceId,
    string Status,
    bool SentToGreeCloud,
    bool SentToMqtt,
    bool SentToDevice,
    string RuntimeMode);
