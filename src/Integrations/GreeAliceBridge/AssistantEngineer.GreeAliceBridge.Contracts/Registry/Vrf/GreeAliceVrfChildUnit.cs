namespace AssistantEngineer.GreeAliceBridge.Contracts.Registry.Vrf;

public sealed record GreeAliceVrfChildUnit(
    string ChildUnitId,
    string ParentGatewayId,
    string StableYandexDeviceId,
    string DisplayName,
    string RoomId,
    string RoomName,
    string? IndoorUnitAddress,
    string? IndoorUnitModel,
    decimal? CapacityKw,
    bool ExposeToYandex,
    string DeviceKind,
    string RuntimeMode);
