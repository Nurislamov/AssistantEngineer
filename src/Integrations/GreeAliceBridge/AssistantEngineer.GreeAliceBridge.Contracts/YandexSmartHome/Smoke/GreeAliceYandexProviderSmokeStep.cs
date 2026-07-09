namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.Smoke;

public sealed record GreeAliceYandexProviderSmokeStep(
    string StepId,
    string StepKind,
    string Description,
    IReadOnlyList<GreeAliceYandexProviderSmokeExpectation> Expectations);
