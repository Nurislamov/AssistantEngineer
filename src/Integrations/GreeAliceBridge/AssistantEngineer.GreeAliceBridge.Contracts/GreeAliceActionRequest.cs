namespace AssistantEngineer.GreeAliceBridge.Contracts;

public sealed record GreeAliceActionRequest(
    string DeviceId,
    string Capability,
    string Action,
    string? Value);
