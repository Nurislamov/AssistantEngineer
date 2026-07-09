namespace AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.Mapping;

public sealed record GreeCloudMaskedRawStateSnapshot(
    string DeviceId,
    bool IsKnownDevice,
    IReadOnlyList<GreeCloudMaskedRawStateField> Fields,
    string SourceKind,
    string RuntimeMode);
