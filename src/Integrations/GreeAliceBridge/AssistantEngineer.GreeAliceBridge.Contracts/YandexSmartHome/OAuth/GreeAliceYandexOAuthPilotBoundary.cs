namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.OAuth;

public static class GreeAliceYandexOAuthPilotBoundary
{
    public const string RuntimeStatus = "dev-only";
    public const bool RealProductionOAuth = false;
    public const bool RealYandexSharedMaterialAllowedInRepository = false;
    public const bool RealTokensAllowedInRepository = false;
    public const bool InMemoryTokenStoreOnly = true;
    public const bool PersistentTokenStoreImplemented = false;
    public const bool RequiresHttpsForRealYandex = true;
    public const bool AllowsLocalhostForDev = true;
    public const string ProviderMode = "dummy-offline-devices";
    public const string ActionMode = "dry-run-fail-closed";
    public const bool LiveGreeReadAllowed = false;
    public const bool LiveGreeControlAllowed = false;
    public const bool MqttAllowed = false;
    public const bool ProductionDeploymentAllowed = false;
    public const int MaxTokenLength = 2048;
    public const int DefaultAuthorizationCodeLifetimeSeconds = 300;
    public const int DefaultAccessTokenLifetimeSeconds = 3600;
    public const int DefaultRefreshTokenLifetimeSeconds = 2592000;
}
