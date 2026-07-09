namespace AssistantEngineer.GreeAliceBridge.Contracts.Registry.Import;

public sealed record GreeAliceRegistryImportDeviceDraft(
    string ImportDeviceId,
    string DeviceKind,
    string DisplayName,
    string? RoomId,
    string? StableYandexDeviceId,
    bool ExposeToYandex,
    bool IsMasked,
    bool IsDummyOrTemplate);
