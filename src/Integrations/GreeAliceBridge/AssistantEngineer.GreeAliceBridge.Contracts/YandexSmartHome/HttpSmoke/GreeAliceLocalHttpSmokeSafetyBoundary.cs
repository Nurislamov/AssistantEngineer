namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.HttpSmoke;

public static class GreeAliceLocalHttpSmokeSafetyBoundary
{
    public const bool IsLocalhostOnly = true;
    public const bool AllowsPublicNetwork = false;
    public const bool AllowsHttps = false;
    public const bool AllowsRealYandexCalls = GreeAliceLocalHttpSmokeBoundary.AllowsRealYandexCalls;
    public const bool AllowsRealOAuth = GreeAliceLocalHttpSmokeBoundary.AllowsRealOAuth;
    public const bool AllowsRealCredentials = GreeAliceLocalHttpSmokeBoundary.AllowsRealCredentials;
    public const bool AllowsLiveGreeCloudCalls = GreeAliceLocalHttpSmokeBoundary.AllowsLiveGreeCloudCalls;
    public const bool AllowsMqtt = GreeAliceLocalHttpSmokeBoundary.AllowsMqtt;
    public const bool AllowsProductionEndpoint = GreeAliceLocalHttpSmokeBoundary.AllowsProductionEndpoint;
    public const bool AllowsDeviceControl = GreeAliceLocalHttpSmokeBoundary.AllowsDeviceControl;
    public const bool AllowsCommandExecution = GreeAliceLocalHttpSmokeBoundary.AllowsCommandExecution;
}
