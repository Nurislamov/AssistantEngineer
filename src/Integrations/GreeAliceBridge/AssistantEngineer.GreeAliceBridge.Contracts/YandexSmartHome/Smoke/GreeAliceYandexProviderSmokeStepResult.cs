namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.Smoke;

public sealed record GreeAliceYandexProviderSmokeStepResult(
    string StepId,
    string StepKind,
    bool Passed,
    string Status,
    string Message);
