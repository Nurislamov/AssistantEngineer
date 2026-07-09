namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.Smoke;

public sealed record GreeAliceYandexProviderSmokeScenarioResult(
    string ScenarioId,
    string DisplayName,
    bool Passed,
    string Status,
    IReadOnlyList<GreeAliceYandexProviderSmokeStepResult> Steps);
