namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.ProviderReadiness;

public static class GreeAliceYandexProviderSafetyBoundary
{
    public const bool ProviderRegistrationApproved = GreeAliceYandexProviderReadinessBoundary.ProviderRegistrationApproved;
    public const bool ProviderPublicationApproved = GreeAliceYandexProviderReadinessBoundary.ProviderPublicationApproved;
    public const bool RealOAuthImplemented = GreeAliceYandexProviderReadinessBoundary.RealOAuthImplemented;
    public const bool RealOAuthEndpointsImplemented = GreeAliceYandexProviderReadinessBoundary.RealOAuthEndpointsImplemented;
    public const bool ProductionEndpointConfigured = GreeAliceYandexProviderReadinessBoundary.ProductionEndpointConfigured;
    public const bool ProductionDeploymentWiringEnabled = GreeAliceYandexProviderReadinessBoundary.ProductionDeploymentWiringEnabled;
    public const bool AllowsSecretsInRepository = GreeAliceYandexProviderReadinessBoundary.AllowsSecretsInRepository;
    public const bool AllowsLiveGreeControl = GreeAliceYandexProviderReadinessBoundary.AllowsLiveGreeControl;
    public const bool AllowsMqtt = GreeAliceYandexProviderReadinessBoundary.AllowsMqtt;
    public const bool AllowsDeviceControl = GreeAliceYandexProviderReadinessBoundary.AllowsDeviceControl;
}
