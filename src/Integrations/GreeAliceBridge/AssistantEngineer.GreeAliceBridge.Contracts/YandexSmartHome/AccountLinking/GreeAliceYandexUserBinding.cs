namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.AccountLinking;

public sealed record GreeAliceYandexUserBinding(
    string YandexUserReference,
    string BridgeAccountReference,
    string RegistryScopeReference,
    DateTimeOffset? LinkedAtUtc,
    DateTimeOffset? UnlinkedAtUtc,
    bool IsActive,
    bool IsMasked,
    bool IsDummyOrTemplate);
