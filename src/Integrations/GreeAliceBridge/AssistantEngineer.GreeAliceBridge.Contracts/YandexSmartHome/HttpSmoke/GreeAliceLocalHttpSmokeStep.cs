namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.HttpSmoke;

public sealed record GreeAliceLocalHttpSmokeStep(
    string StepId,
    string EndpointId,
    string Description,
    GreeAliceLocalHttpSmokeRequest? RequestTemplate,
    GreeAliceLocalHttpSmokeExpectation Expectation);
