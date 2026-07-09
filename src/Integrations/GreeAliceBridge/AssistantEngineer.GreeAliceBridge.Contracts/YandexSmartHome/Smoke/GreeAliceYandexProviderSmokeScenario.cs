namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.Smoke;

public sealed record GreeAliceYandexProviderSmokeScenario(
    string ScenarioId,
    string DisplayName,
    string Description,
    bool IsOfflineOnly,
    bool UsesDummyOrTemplateData,
    string ExpectedResult,
    IReadOnlyList<GreeAliceYandexProviderSmokeStep> Steps);
