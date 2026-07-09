namespace AssistantEngineer.GreeAliceBridge.Contracts.Registry.Import;

public sealed record GreeAliceRegistryImportVrfGatewayDraft(
    string ImportGatewayId,
    string HomeId,
    string SystemName,
    string DisplayName,
    bool IsMasked,
    bool IsDummyOrTemplate,
    bool ExposeToYandex = false,
    bool IsTechnicalDevice = true);
