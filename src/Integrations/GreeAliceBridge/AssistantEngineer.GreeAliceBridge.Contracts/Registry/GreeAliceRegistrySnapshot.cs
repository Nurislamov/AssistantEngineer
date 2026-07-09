namespace AssistantEngineer.GreeAliceBridge.Contracts.Registry;

public sealed record GreeAliceRegistrySnapshot(
    GreeAliceBridgeAccount Account,
    IReadOnlyList<GreeAliceHome> Homes,
    IReadOnlyList<GreeAliceRoom> Rooms,
    IReadOnlyList<GreeAliceRegisteredDevice> Devices,
    string RegistryMode);
