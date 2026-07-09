namespace AssistantEngineer.GreeAliceBridge.Contracts.Registry.Import;

public sealed record GreeAliceRegistryImportHomeDraft(
    string ImportHomeId,
    string DisplayName,
    bool IsMasked,
    bool IsDummyOrTemplate);
