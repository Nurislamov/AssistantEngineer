namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.HttpSmoke;

public static class GreeAliceLocalHttpSmokeBoundary
{
    public const string HttpSmokeMode = GreeAliceLocalHttpSmokeMode.LocalhostOnly;
    public const string HttpSmokeStatus = "not-production";

    public const bool AllowedHostLocalhost = true;
    public const bool AllowedHostLoopback = true;
    public const bool AllowedPublicHosts = false;
    public const bool AllowsRealYandexCalls = false;
    public const bool AllowsRealOAuth = false;
    public const bool AllowsRealCredentials = false;
    public const bool AllowsLiveGreeCloudCalls = false;
    public const bool AllowsMqtt = false;
    public const bool AllowsProductionEndpoint = false;
    public const bool AllowsDeviceControl = false;
    public const bool AllowsCommandExecution = false;
    public const bool RequiresFailClosedActions = true;
    public const bool RequiresDummyOrTemplateResponses = true;
    public const bool ProviderReadinessMustRemainNotReady = true;
    public const bool ProductionPilotMustRemainNotApproved = true;
}
