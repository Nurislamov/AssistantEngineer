namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.AccountLinking;

public sealed record GreeAliceYandexAccountUnlinkResult(
    string YandexUserReference,
    string BridgeAccountReference,
    string RegistryScopeReference,
    bool WasLinked,
    bool IsNowUnlinked,
    bool RevokedAccessToRegistryScope,
    bool DeletedSecrets,
    bool DeletedTokens,
    bool RealTokenStorageImplemented,
    string Reason,
    bool IsDummyOrTemplate);
