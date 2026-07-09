namespace AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud;

public sealed record GreeCloudControlResult(
    string DeviceId,
    string Capability,
    string Status,
    bool SentToGreeCloud,
    bool SentToMqtt,
    bool SentToDevice,
    string AdapterMode);
