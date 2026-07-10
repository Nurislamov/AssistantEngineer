namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.OAuth;

public sealed record GreeAliceYandexOAuthAccountLink(
    string MaskedYandexUserId,
    string BridgeAccountId,
    string RegistryScope,
    bool IsDevOnly,
    bool UsesDummyOrTemplateData);
