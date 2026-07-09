namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.ProviderReadiness;

public static class GreeAliceYandexProviderReadinessBoundary
{
    public const string ProviderReadinessMode = GreeAliceYandexProviderReadinessMode.OfflineReadinessPackage;
    public const string ProviderReadinessStatus = GreeAliceYandexProviderReadinessStatus.NotReady;

    public const bool ProviderRegistrationApproved = false;
    public const bool ProviderPublicationApproved = false;
    public const bool RealYandexProviderCreated = false;
    public const bool RealOAuthImplemented = false;
    public const bool RealOAuthEndpointsImplemented = false;
    public const bool RealYandexClientCredentialsConfigured = false;
    public const bool RealYandexClientCredentialsAllowedInRepository = false;
    public const bool RealTokensIssued = false;
    public const bool TokenStorageImplemented = false;
    public const bool ProductionEndpointConfigured = false;
    public const bool ProductionDeploymentWiringEnabled = false;
    public const bool ManualSmokeRequired = true;
    public const bool SecurityReviewRequired = true;
    public const bool AccountLinkingReviewRequired = true;
    public const bool DeviceContractReviewRequired = true;
    public const bool QueryContractReviewRequired = true;
    public const bool ActionContractReviewRequired = true;
    public const bool UnlinkContractReviewRequired = true;
    public const bool RegistryScopeReviewRequired = true;
    public const bool OperatorApprovalRequired = true;
    public const bool AllowsSecretsInRepository = false;
    public const bool AllowsRealYandexCredentialsInRepository = false;
    public const bool AllowsLiveGreeControl = false;
    public const bool AllowsMqtt = false;
    public const bool AllowsDeviceControl = false;
}
