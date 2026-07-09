namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.AccountLinking;

public sealed record GreeAliceYandexAccountLinkingSession(
    string LinkingSessionId,
    string Mode,
    string Status,
    DateTimeOffset? RequestedAtUtc,
    string YandexUserReference,
    string BridgeAccountReference,
    string RegistryScopeReference,
    bool IsMasked,
    bool IsDummyOrTemplate);
