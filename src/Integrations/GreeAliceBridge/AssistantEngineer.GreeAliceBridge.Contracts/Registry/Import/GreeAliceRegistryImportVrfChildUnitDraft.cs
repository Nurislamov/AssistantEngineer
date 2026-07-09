namespace AssistantEngineer.GreeAliceBridge.Contracts.Registry.Import;

public sealed record GreeAliceRegistryImportVrfChildUnitDraft(
    string ImportChildUnitId,
    string ParentGatewayId,
    string DisplayName,
    string? RoomId,
    string? StableYandexDeviceId,
    bool ExposeToYandex,
    bool IsMasked,
    bool IsDummyOrTemplate,
    string? IndoorUnitAddress = null,
    string? IndoorUnitModel = null,
    decimal? CapacityKw = null);
