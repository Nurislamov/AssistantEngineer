namespace AssistantEngineer.GreeAliceBridge.Contracts;

public sealed record GreeAliceDevice(
    string Id,
    string Name,
    string Room,
    string Type,
    bool Online,
    IReadOnlyList<string> Capabilities,
    string Source);
