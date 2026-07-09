namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.Smoke;

public static class GreeAliceYandexProviderSmokeSafetyBoundary
{
    public const bool RunsAgainstRealYandex = GreeAliceYandexProviderSmokeHarnessBoundary.RunsAgainstRealYandex;
    public const bool RunsAgainstRealOAuth = GreeAliceYandexProviderSmokeHarnessBoundary.RunsAgainstRealOAuth;
    public const bool RunsAgainstProductionEndpoint = GreeAliceYandexProviderSmokeHarnessBoundary.RunsAgainstProductionEndpoint;
    public const bool RunsAgainstLiveGreeCloud = GreeAliceYandexProviderSmokeHarnessBoundary.RunsAgainstLiveGreeCloud;
    public const bool RunsAgainstMqtt = GreeAliceYandexProviderSmokeHarnessBoundary.RunsAgainstMqtt;
    public const bool AllowsDeviceControl = GreeAliceYandexProviderSmokeHarnessBoundary.AllowsDeviceControl;
    public const bool AllowsCommandExecution = GreeAliceYandexProviderSmokeHarnessBoundary.AllowsCommandExecution;
    public const bool AllowsProductionWrites = GreeAliceYandexProviderSmokeHarnessBoundary.AllowsProductionWrites;
}
