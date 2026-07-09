using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.Smoke;

namespace AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome.Smoke;

public interface IGreeAliceYandexProviderSmokeHarness
{
    IReadOnlyList<GreeAliceYandexProviderSmokeScenario> GetScenarios();

    GreeAliceYandexProviderSmokeResult Run();
}
