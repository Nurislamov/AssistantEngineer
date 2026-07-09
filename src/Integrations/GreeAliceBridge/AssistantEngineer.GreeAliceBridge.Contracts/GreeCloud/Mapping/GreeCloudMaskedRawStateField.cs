namespace AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.Mapping;

public sealed record GreeCloudMaskedRawStateField(
    string Name,
    string MaskedValue,
    bool IsMasked);
