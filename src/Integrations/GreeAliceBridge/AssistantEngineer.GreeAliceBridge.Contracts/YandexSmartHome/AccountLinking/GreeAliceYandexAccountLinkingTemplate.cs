namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.AccountLinking;

public sealed record GreeAliceYandexAccountLinkingTemplate(
    GreeAliceYandexAccountLinkingSession Session,
    GreeAliceYandexUserBinding ActiveBinding,
    GreeAliceBridgeAccountScope RegistryScope,
    GreeAliceYandexAccountUnlinkRequest UnlinkRequest,
    GreeAliceYandexAccountUnlinkResult UnlinkResult);
