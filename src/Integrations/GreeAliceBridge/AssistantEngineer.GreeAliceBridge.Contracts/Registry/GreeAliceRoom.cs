namespace AssistantEngineer.GreeAliceBridge.Contracts.Registry;

public sealed record GreeAliceRoom(
    string Id,
    string HomeRef,
    string Name,
    string Source);
