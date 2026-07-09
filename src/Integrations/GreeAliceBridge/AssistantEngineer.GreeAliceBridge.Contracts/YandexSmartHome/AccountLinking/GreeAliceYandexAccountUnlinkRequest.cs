namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.AccountLinking;

public sealed record GreeAliceYandexAccountUnlinkRequest(
    string YandexUserReference,
    string BridgeAccountReference,
    string RegistryScopeReference,
    bool IsDummyOrTemplate);
