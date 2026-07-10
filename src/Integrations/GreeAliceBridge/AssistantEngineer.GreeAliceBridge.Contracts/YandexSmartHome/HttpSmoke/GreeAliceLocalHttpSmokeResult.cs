namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.HttpSmoke;

public sealed record GreeAliceLocalHttpSmokeResult(
    string Mode,
    string Status,
    IReadOnlyList<GreeAliceLocalHttpSmokeEndpoint> Endpoints,
    IReadOnlyList<GreeAliceLocalHttpSmokeRequest> Requests,
    IReadOnlyList<GreeAliceLocalHttpSmokeExpectation> Expectations,
    IReadOnlyList<GreeAliceLocalHttpSmokeStep> Steps,
    string SafetyBoundary);
