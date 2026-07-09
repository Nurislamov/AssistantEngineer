namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.Smoke;

public static class GreeAliceYandexProviderSmokeHarnessBoundary
{
    public const string SmokeHarnessMode = GreeAliceYandexProviderSmokeHarnessMode.OfflineLocal;
    public const string SmokeHarnessStatus = "not-production";

    public const bool RunsAgainstRealYandex = false;
    public const bool RunsAgainstRealOAuth = false;
    public const bool RequiresRealYandexCredentials = false;
    public const bool RequiresRealGreeCredentials = false;
    public const bool RunsAgainstProductionEndpoint = false;
    public const bool RunsAgainstLiveGreeCloud = false;
    public const bool RunsAgainstMqtt = false;
    public const bool AllowsDeviceControl = false;
    public const bool AllowsCommandExecution = false;
    public const bool AllowsProductionWrites = false;
    public const bool RequiresDummyOrTemplateData = true;
    public const bool RequiresFailClosedActions = true;
    public const bool RequiresUnknownUserFailClosed = true;
    public const bool RequiresUnknownDeviceFailClosed = true;
}
