namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.AccountLinking;

public static class GreeAliceYandexAccountLinkingBoundary
{
    public const string AccountLinkingMode = GreeAliceYandexAccountLinkingMode.OfflineTemplate;
    public const string AccountLinkingStatus = GreeAliceYandexAccountLinkingStatus.NotApproved;

    public const bool RealOAuthImplemented = false;
    public const bool RealYandexProviderRegistrationImplemented = false;
    public const bool RealYandexAppCredentialsAllowed = false;
    public const bool RealTokensIssued = false;
    public const bool RefreshTokensIssued = false;
    public const bool AccessTokensIssued = false;
    public const bool TokenStorageImplemented = false;
    public const bool TokenRevocationImplemented = false;
    public const bool RequiresManualReview = true;
    public const bool RequiresRegistryScopeBinding = true;
    public const bool RequiresMaskedYandexUserId = true;
    public const bool RequiresDummyOrTemplateData = true;
    public const bool AllowsSecretsInRepository = false;
    public const bool AllowsRealYandexUserIdentifiersInRepository = false;
    public const bool AllowsRealBridgeAccountIdentifiersInRepository = false;
    public const bool AllowsProductionWrite = false;
    public const bool AllowsDeviceControl = false;
    public const bool AllowsMqtt = false;
    public const bool ProductionWiringAllowed = false;
}
