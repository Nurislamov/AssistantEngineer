namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.Smoke;

public sealed record GreeAliceYandexProviderSmokeExpectation(
    string ExpectationId,
    string Description,
    bool IsRequired);
