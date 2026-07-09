namespace AssistantEngineer.GreeAliceBridge.Contracts.Registry;

public sealed record GreeAliceRegisteredDevice(
    string Id,
    string DisplayName,
    string Kind,
    string RoomRef,
    string? ParentGatewayRef,
    bool YandexExposed,
    GreeAliceDeviceCapabilities Capabilities,
    string Source);
