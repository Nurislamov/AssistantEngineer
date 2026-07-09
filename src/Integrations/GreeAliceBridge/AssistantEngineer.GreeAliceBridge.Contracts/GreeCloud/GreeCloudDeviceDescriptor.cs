using AssistantEngineer.GreeAliceBridge.Contracts.Registry;

namespace AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud;

public sealed record GreeCloudDeviceDescriptor(
    string DeviceId,
    string DisplayName,
    string Kind,
    string RoomRef,
    string? ParentGatewayRef,
    bool YandexExposed,
    GreeAliceDeviceCapabilities Capabilities,
    string Source,
    string AdapterMode);
