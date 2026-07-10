namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.HttpSmoke;

public sealed record GreeAliceLocalHttpSmokeExpectation(
    string EndpointId,
    string ExpectedStatus,
    IReadOnlyList<string> RequiredEvidence,
    bool RequiresNoExternalCalls,
    bool RequiresFailClosedAction);
