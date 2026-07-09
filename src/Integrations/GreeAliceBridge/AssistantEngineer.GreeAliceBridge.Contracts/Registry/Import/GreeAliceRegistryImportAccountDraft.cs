namespace AssistantEngineer.GreeAliceBridge.Contracts.Registry.Import;

public sealed record GreeAliceRegistryImportAccountDraft(
    string ImportAccountId,
    string DisplayName,
    bool IsMasked,
    bool IsDummyOrTemplate);
