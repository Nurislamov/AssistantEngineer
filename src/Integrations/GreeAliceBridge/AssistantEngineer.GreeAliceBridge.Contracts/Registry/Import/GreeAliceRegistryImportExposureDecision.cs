namespace AssistantEngineer.GreeAliceBridge.Contracts.Registry.Import;

public sealed record GreeAliceRegistryImportExposureDecision(
    string ImportObjectId,
    string ObjectKind,
    bool ExposeToYandex,
    bool Reviewed,
    string? StableYandexDeviceId,
    string? RoomId);
