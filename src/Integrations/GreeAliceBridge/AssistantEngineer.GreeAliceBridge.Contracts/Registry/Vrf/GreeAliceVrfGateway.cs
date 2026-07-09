namespace AssistantEngineer.GreeAliceBridge.Contracts.Registry.Vrf;

public sealed record GreeAliceVrfGateway(
    string GatewayId,
    string DisplayName,
    string HomeId,
    string? RoomId,
    string SystemName,
    string GatewayKind,
    bool IsTechnicalDevice,
    bool ExposeToYandex,
    string RuntimeMode);
