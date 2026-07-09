namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.Smoke;

public sealed record GreeAliceYandexProviderSmokeResult(
    string Mode,
    string Status,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    bool AllPassed,
    IReadOnlyList<GreeAliceYandexProviderSmokeScenarioResult> Scenarios,
    string SafetyBoundary,
    string Summary);
