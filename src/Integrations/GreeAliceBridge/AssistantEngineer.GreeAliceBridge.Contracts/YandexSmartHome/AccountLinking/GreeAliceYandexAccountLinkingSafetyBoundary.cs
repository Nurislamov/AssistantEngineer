namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.AccountLinking;

public static class GreeAliceYandexAccountLinkingSafetyBoundary
{
    public const bool RealOAuthImplemented = GreeAliceYandexAccountLinkingBoundary.RealOAuthImplemented;
    public const bool RealYandexProviderRegistrationImplemented = GreeAliceYandexAccountLinkingBoundary.RealYandexProviderRegistrationImplemented;
    public const bool RealYandexAppCredentialsAllowed = GreeAliceYandexAccountLinkingBoundary.RealYandexAppCredentialsAllowed;
    public const bool RealTokensIssued = GreeAliceYandexAccountLinkingBoundary.RealTokensIssued;
    public const bool TokenStorageImplemented = GreeAliceYandexAccountLinkingBoundary.TokenStorageImplemented;
    public const bool TokenRevocationImplemented = GreeAliceYandexAccountLinkingBoundary.TokenRevocationImplemented;
    public const bool AllowsProductionWrite = GreeAliceYandexAccountLinkingBoundary.AllowsProductionWrite;
    public const bool AllowsDeviceControl = GreeAliceYandexAccountLinkingBoundary.AllowsDeviceControl;
    public const bool AllowsMqtt = GreeAliceYandexAccountLinkingBoundary.AllowsMqtt;
    public const bool ProductionWiringAllowed = GreeAliceYandexAccountLinkingBoundary.ProductionWiringAllowed;
}
