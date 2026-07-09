namespace AssistantEngineer.GreeAliceBridge.Contracts.Registry.Import;

public sealed record GreeAliceRegistryImportRoomDraft(
    string ImportRoomId,
    string DisplayName,
    string HomeId,
    bool IsMasked,
    bool IsDummyOrTemplate);
