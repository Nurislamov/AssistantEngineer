namespace AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud;

public sealed record GreeCloudControlRequest(
    string DeviceId,
    string Capability,
    string Action,
    string? Value);
